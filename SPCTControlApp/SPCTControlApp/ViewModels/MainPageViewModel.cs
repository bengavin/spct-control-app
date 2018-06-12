using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SPCTControlApp.Services;
using System;
using System.Threading.Tasks;

namespace SPCTControlApp.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly IWiFiDirectService _wifiDirectService;

        private bool _isConnected;
        private string _panelDeviceId;
        private string _connectionErrorText;

        public MainPageViewModel(IWiFiDirectService wifiDirectService)
        {
            _wifiDirectService = wifiDirectService;
            ConnectPanelCommand = new RelayCommand(async () => await ConnectPanel(), () => !IsConnected && !String.IsNullOrWhiteSpace(PanelDeviceId));
            TestProcedureCommand = new RelayCommand(async() => await TestPanel(), () => IsConnected);
        }


        public RelayCommand ConnectPanelCommand { get; private set; }
        public RelayCommand TestProcedureCommand { get; private set; }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                Set(() => IsConnected, ref _isConnected, value);
                RaisePropertyChanged(() => IsNotConnected);
                ConnectPanelCommand.RaiseCanExecuteChanged();
                TestProcedureCommand.RaiseCanExecuteChanged();
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

        private async Task ConnectPanel()
        {
            // TODO: Actually connect to the panel
            var result = await _wifiDirectService.Connect(PanelDeviceId);

            IsConnected = result;
            ConnectionErrorText = _wifiDirectService.LastErrorMessage;
        }

        private Task TestPanel()
        {
            // TODO: Trigger the panel test procedure
            return Task.CompletedTask;
        }

    }
}
