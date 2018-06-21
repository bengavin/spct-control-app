using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SPCTControlApp.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SPCTControlApp.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public class ButtonStatesViewModel
        {
            public void Off()
            {
                Button10.IsOn = Button11.IsOn =
                Button12.IsOn = Button13.IsOn =
                Button14.IsOn = Button15.IsOn =
                Button16.IsOn = Button17.IsOn =
                Button20.IsOn = Button21.IsOn =
                Button22.IsOn = Button23.IsOn =
                Button24.IsOn = Button25.IsOn =
                Button26.IsOn = Button27.IsOn = false;
            }

            public ButtonStateViewModel Button10 { get; private set; } = new ButtonStateViewModel(1,0,1);
            public ButtonStateViewModel Button11 { get; private set; } = new ButtonStateViewModel(1,1,2);
            public ButtonStateViewModel Button12 { get; private set; } = new ButtonStateViewModel(1,2,3);
            public ButtonStateViewModel Button13 { get; private set; } = new ButtonStateViewModel(1,3,4);
            public ButtonStateViewModel Button14 { get; private set; } = new ButtonStateViewModel(1, 4, 5);
            public ButtonStateViewModel Button15 { get; private set; } = new ButtonStateViewModel(1, 5, 6);
            public ButtonStateViewModel Button16 { get; private set; } = new ButtonStateViewModel(1, 6, 7);
            public ButtonStateViewModel Button17 { get; private set; } = new ButtonStateViewModel(1, 7, 8);
            public ButtonStateViewModel Button20 { get; private set; } = new ButtonStateViewModel(2, 0, 9);
            public ButtonStateViewModel Button21 { get; private set; } = new ButtonStateViewModel(2, 1, 10);
            public ButtonStateViewModel Button22 { get; private set; } = new ButtonStateViewModel(2, 2, 11);
            public ButtonStateViewModel Button23 { get; private set; } = new ButtonStateViewModel(2, 3, 12);
            public ButtonStateViewModel Button24 { get; private set; } = new ButtonStateViewModel(2, 4, 13);
            public ButtonStateViewModel Button25 { get; private set; } = new ButtonStateViewModel(2, 5, 14);
            public ButtonStateViewModel Button26 { get; private set; } = new ButtonStateViewModel(2, 6, 15);
            public ButtonStateViewModel Button27 { get; private set; } = new ButtonStateViewModel(2, 7, 16);
        }

        public class CustomPinchUpdatedEventArgs : EventArgs
        {
            public CustomPinchUpdatedEventArgs(ButtonStateViewModel button, PinchGestureUpdatedEventArgs args)
            {
                Button = button;
                EventArgs = args;
            }

            public ButtonStateViewModel Button { get; }
            public PinchGestureUpdatedEventArgs EventArgs { get; }
        }

        private readonly IPanelService _panelService;

        private bool _isConnected;
        private string _panelDeviceId;
        private TimeSpan _timeSinceStart;
        private string _connectionErrorText;
        private System.Threading.Timer _timer;

        public MainPageViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            _panelService.OperationComplete += _panelService_OperationComplete;

            _panelDeviceId = "192.168.197.1:8800";
            _timeSinceStart = TimeSpan.Zero;
            _timer = new Timer(new TimerCallback(_timer_tick), null, Timeout.Infinite, Timeout.Infinite);

            ConnectPanelCommand = new RelayCommand(async () => await ConnectPanel(), () => IsConnected || (!IsConnected && !String.IsNullOrWhiteSpace(PanelDeviceId)));
            SetTempoCommand = new RelayCommand<object>(SetTempo);
            StartTempoCommand = new RelayCommand(() => { TimeSinceStart = TimeSpan.Zero; _timer.Change(1000, 1000); _panelService.StartLine(0); }, () => !_panelService.IsLineRunning(0));
            StopTempoCommand = new RelayCommand(() => { _timer.Change(Timeout.Infinite, Timeout.Infinite); _panelService.StopLine(0, true); }, () => _panelService.IsLineRunning(0));
            ToggleLightCommand = new RelayCommand<ButtonStateViewModel>(async (args) => await ToggleLight(args));
            SelectColorCommand = new RelayCommand<CustomPinchUpdatedEventArgs>(SelectColor);
            PanelOffCommand = new RelayCommand(PanelOff);
            ModifyTempoCommand = new RelayCommand<string>(ModifyTempo);
            PlayLine1Command = new RelayCommand<string>(PlayLine1);
            PlayLine2Command = new RelayCommand<string>(PlayLine2);
        }


        public RelayCommand ConnectPanelCommand { get; private set; }
        public RelayCommand<object> SetTempoCommand { get; private set; }
        public RelayCommand StartTempoCommand { get; private set; }
        public RelayCommand StopTempoCommand { get; private set; }
        public RelayCommand<ButtonStateViewModel> ToggleLightCommand { get; private set; }
        public RelayCommand<CustomPinchUpdatedEventArgs> SelectColorCommand { get; private set; }
        public RelayCommand PanelOffCommand { get; private set; }
        public RelayCommand<string> ModifyTempoCommand { get; private set; }
        public RelayCommand<string> PlayLine1Command { get; private set; }
        public RelayCommand<string> PlayLine2Command { get; private set; }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                Set(() => IsConnected, ref _isConnected, value);
                RaisePropertyChanged(() => IsNotConnected);
                RaisePropertyChanged(() => ConnectButtonText);
                ConnectPanelCommand.RaiseCanExecuteChanged();
                SetTempoCommand.RaiseCanExecuteChanged();
            }
        }

        public TimeSpan TimeSinceStart
        {
            get { return _timeSinceStart; }
            set
            {
                Set(() => TimeSinceStart, ref _timeSinceStart, value);
            }
        }

        public bool IsNotConnected
        {
            get { return !IsConnected; }
        }

        public string PanelDeviceId
        {
            get
            {
                return _panelDeviceId;
            }
            set
            {
                Set(() => PanelDeviceId, ref _panelDeviceId, value);
                ConnectPanelCommand.RaiseCanExecuteChanged();
            }
        }

        public string ConnectionErrorText
        {
            get { return _connectionErrorText; }
            set
            {
                Set(() => ConnectionErrorText, ref _connectionErrorText, value);
            }
        }

        public string ConnectButtonText
        {
            get { return IsConnected ? "Disconnect" : "Connect"; }
        }

        private Task ConnectPanel()
        {
            if (_panelService.IsConnected)
            {
                _panelService.Disconnect();
                ButtonStates.Off();
            }
            else
            {
                ButtonStates.Off();
                _panelService.Connect(PanelDeviceId);
            }

            return Task.CompletedTask;
        }

        private void _panelService_OperationComplete(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ConnectionErrorText = _panelService.LastErrorMessage;
                IsConnected = _panelService.IsConnected;

                StartTempoCommand.RaiseCanExecuteChanged();
                StopTempoCommand.RaiseCanExecuteChanged();
            });
        }

        private void _timer_tick(object state)
        {
            TimeSinceStart = _timeSinceStart.Add(TimeSpan.FromSeconds(1));
        }

        private int _panelTempo;
        public int PanelTempo
        {
            get { return _panelTempo; }
            set
            {
                Set(() => PanelTempo, ref _panelTempo, value);
            }
        }

        private void SetTempo(object tempo)
        {
            if (tempo.GetType() == typeof(int))
            {
                _panelService.SetTempo((int)tempo);
                PanelTempo = (int)tempo;
            }
            else
            {
                var tempoVals = tempo.ToString().Split(new[] { ',' });
                var multiplier = tempoVals.Length > 1 ? Convert.ToInt32(tempoVals[1]) / 100f : 0.95;

                int tempoVal = Convert.ToInt32(Math.Round(Convert.ToInt32(tempoVals[0]) * multiplier, 0));
                _panelService.SetTempo(tempoVal);
                PanelTempo = tempoVal;
            }
        }

        private void ModifyTempo(string tempoChange)
        {
            var tempoChangeVal = Convert.ToInt32(tempoChange);
            SetTempo(PanelTempo + tempoChangeVal);
        }

        public ButtonStatesViewModel ButtonStates { get; private set; } = new ButtonStatesViewModel();

        private async Task ToggleLight(ButtonStateViewModel arg)
        {
            if (arg == null)
            {
                _panelService.StopLine(1, true)
                             .ContinueWith(t =>
                                _panelService.StopLine(2, true));
                ButtonStates.Off();
            }
            else
            {
                arg.IsOn = !arg.IsOn;
                _panelService.SetLightState(arg.Row, arg.Column, arg.Color, arg.IsOn);
            }
        }

        private void PlayLine1(string command)
        {
            _panelService.StartLine(1, command);
        }

        private void PlayLine2(string command)
        {
            _panelService.StartLine(2, command);
        }

        private void SelectColor(CustomPinchUpdatedEventArgs args)
        {
            if (args.EventArgs.Status == GestureStatus.Completed)
            {
                // Do something fancy
                ConnectionErrorText = $"Holy Crap - I got a gesture! {args.Button.Row}, {args.Button.Column}";
            }
        }

        private void PanelOff()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _panelService.TurnPanelOff();
            ButtonStates.Off();
        }
    }
}
