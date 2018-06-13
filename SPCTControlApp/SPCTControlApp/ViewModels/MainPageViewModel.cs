using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SPCTControlApp.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SPCTControlApp.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public class ButtonStatesViewModel
        {
            public ButtonStateViewModel Button10 { get; private set; } = new ButtonStateViewModel(1,0);
            public ButtonStateViewModel Button11 { get; private set; } = new ButtonStateViewModel(1,1);
            public ButtonStateViewModel Button12 { get; private set; } = new ButtonStateViewModel(1,2);
            public ButtonStateViewModel Button13 { get; private set; } = new ButtonStateViewModel(1,3);
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
        private string _connectionErrorText;

        public MainPageViewModel(IPanelService panelService)
        {
            _panelService = panelService;
            _panelService.OperationComplete += _panelService_OperationComplete;

            ConnectPanelCommand = new RelayCommand(async () => await ConnectPanel(), () => IsConnected || (!IsConnected && !String.IsNullOrWhiteSpace(PanelDeviceId)));
            SetTempoCommand = new RelayCommand(() => SetTempo());
            StartTempoCommand = new RelayCommand(() => _panelService.StartLine(0), () => !_panelService.IsLineRunning(0));
            StopTempoCommand = new RelayCommand(() => _panelService.StopLine(0), () => _panelService.IsLineRunning(0));
            ToggleLightCommand = new RelayCommand<ButtonStateViewModel>(async (args) => await ToggleLight(args));
            SelectColorCommand = new RelayCommand<CustomPinchUpdatedEventArgs>(SelectColor);
        }


        public RelayCommand ConnectPanelCommand { get; private set; }
        public RelayCommand SetTempoCommand { get; private set; }
        public RelayCommand StartTempoCommand { get; private set; }
        public RelayCommand StopTempoCommand { get; private set; }
        public RelayCommand<ButtonStateViewModel> ToggleLightCommand { get; private set; }
        public RelayCommand<CustomPinchUpdatedEventArgs> SelectColorCommand { get; private set; }

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
            }
            else
            {
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

        private int _panelTempo;
        public int PanelTempo
        {
            get { return _panelTempo; }
            set
            {
                Set(() => PanelTempo, ref _panelTempo, value);
            }
        }

        private void SetTempo()
        {
            _panelService.SetTempo(PanelTempo);
        }

        public ButtonStatesViewModel ButtonStates { get; private set; } = new ButtonStatesViewModel();

        private async Task ToggleLight(ButtonStateViewModel arg)
        {
            arg.IsOn = !arg.IsOn;
            _panelService.SetLightOnOff(arg.Row, arg.Column, arg.IsOn);
        }

        private void SelectColor(CustomPinchUpdatedEventArgs args)
        {
            if (args.EventArgs.Status == GestureStatus.Completed)
            {
                // Do something fancy
                ConnectionErrorText = $"Holy Crap - I got a gesture! {args.Button.Row}, {args.Button.Column}";
            }
        }
    }
}
