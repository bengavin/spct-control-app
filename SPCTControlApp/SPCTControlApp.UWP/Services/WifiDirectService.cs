using SPCTControlApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;

namespace SPCTControlApp.UWP.Services
{
    public class DiscoveredDevice
    {
        public DeviceInformation DeviceInfo { get; private set; }

        public DiscoveredDevice(DeviceInformation deviceInfo)
        {
            DeviceInfo = deviceInfo;
        }

        public string DisplayName => DeviceInfo.Name + " - " + (DeviceInfo.Pairing.IsPaired ? "Paired" : "Unpaired");
        public override string ToString() => DisplayName;

        public void UpdateDeviceInfo(DeviceInformationUpdate update)
        {
            DeviceInfo.Update(update);
        }
    }

    public class WifiDirectService : IWiFiDirectService
    {
        private bool _scanning = false;
        private string _searchingFor = null;

        private WiFiDirectDevice _wfdDevice;
        private DeviceWatcher _deviceWatcher;
        private WiFiDirectAdvertisementPublisher _publisher = new WiFiDirectAdvertisementPublisher();
        private ManualResetEvent _scanEvent = new ManualResetEvent(false);

        public string LastErrorMessage { get; private set; }
        public bool Connected => _wfdDevice?.ConnectionStatus == WiFiDirectConnectionStatus.Connected;
        private List<DiscoveredDevice> _discoveredDevices => new List<DiscoveredDevice>();

        public bool StartScan(string searchForDevice = null)
        {
            _searchingFor = searchForDevice;
            _publisher.Start();

            if (_publisher.Status != WiFiDirectAdvertisementPublisherStatus.Started)
            {
                LastErrorMessage = "Failed to start advertisement.";
                return false;
            }

            _discoveredDevices.Clear();
            LastErrorMessage = "Finding Devices...";

            String deviceSelector = WiFiDirectDevice.GetDeviceSelector(WiFiDirectDeviceSelectorType.DeviceInterface);

            _deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector, new string[] { "System.Devices.WiFiDirect.InformationElements" });

            _deviceWatcher.Added += OnDeviceAdded;
            _deviceWatcher.Removed += OnDeviceRemoved;
            _deviceWatcher.Updated += OnDeviceUpdated;
            _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
            _deviceWatcher.Stopped += OnStopped;

            _deviceWatcher.Start();
            _scanning = true;

            return true;
        }

        public void StopScan()
        {
            _scanning = false;
            _publisher.Stop();

            _deviceWatcher.Added -= OnDeviceAdded;
            _deviceWatcher.Removed -= OnDeviceRemoved;
            _deviceWatcher.Updated -= OnDeviceUpdated;
            _deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
            _deviceWatcher.Stopped -= OnStopped;

            _deviceWatcher.Stop();

            LastErrorMessage = "Device watcher stopped.";
        }

        #region DeviceWatcherEvents
        private void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            _discoveredDevices.Add(new DiscoveredDevice(deviceInfo));
            if (String.Equals(deviceInfo.Id, _searchingFor, StringComparison.OrdinalIgnoreCase))
            {
                // Found what we're looking for
                _scanEvent.Set();
            }
        }

        private void OnDeviceRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            foreach (DiscoveredDevice discoveredDevice in _discoveredDevices)
            {
                if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                {
                    _discoveredDevices.Remove(discoveredDevice);
                    break;
                }
            }
        }

        private void OnDeviceUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            foreach (DiscoveredDevice discoveredDevice in _discoveredDevices)
            {
                if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                {
                    discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                    break;
                }
            }
        }

        private void OnEnumerationCompleted(DeviceWatcher deviceWatcher, object o)
        {
            LastErrorMessage = "DeviceWatcher enumeration completed";
        }

        private void OnStopped(DeviceWatcher deviceWatcher, object o)
        {
            LastErrorMessage = "DeviceWatcher stopped";
            _searchingFor = null;
            _scanning = false;
        }
        #endregion

        public async Task<bool> Connect(string deviceId)
        {
            bool result = false;

            // Don't try to connect again if we're still scanning
            if (_scanning) { return false; }

            // No device Id specified.
            if (String.IsNullOrEmpty(deviceId))
            {
                LastErrorMessage = "Please specify a Wi- Fi Direct device Id.";
                return false;
            }

            if (!_discoveredDevices.Any(d => String.Equals(d.DeviceInfo.Id, deviceId, StringComparison.OrdinalIgnoreCase)))
            {
                StartScan(deviceId);
                if (!_scanEvent.WaitOne(TimeSpan.FromSeconds(60)))
                {
                    LastErrorMessage = "Unable to locate specified device";
                    StopScan();
                    return false;
                }

                StopScan();
            }

            try
            {
                // Connect to the selected Wi-Fi Direct device.
                _wfdDevice = await WiFiDirectDevice.FromIdAsync(deviceId);

                if (_wfdDevice == null)
                {
                    LastErrorMessage = "Connection to " + deviceId + " failed.";
                }
                else
                {
                    // Register for connection status change notification.
                    _wfdDevice.ConnectionStatusChanged += new TypedEventHandler<WiFiDirectDevice, object>(OnConnectionChanged);

                    // Get the EndpointPair information.
                    var endpointPairCollection = _wfdDevice.GetConnectionEndpointPairs();

                    if (endpointPairCollection.Count > 0)
                    {
                        var endpointPair = endpointPairCollection[0];
                        LastErrorMessage = "Local IP address " + endpointPair.LocalHostName.ToString() +
                            " connected to remote IP address " + endpointPair.RemoteHostName.ToString();
                        result = true;
                    }
                    else
                    {
                        LastErrorMessage = "Connection to " + deviceId + " failed.";
                    }
                }
            }
            catch (Exception err)
            {
                // Handle error.
                LastErrorMessage = "Error occurred: " + err.Message;
            }

            return result;
        }

        private void OnConnectionChanged(object sender, object arg)
        {
            var status = (WiFiDirectConnectionStatus)arg;

            if (status == WiFiDirectConnectionStatus.Connected)
            {
                // Connection successful.
            }
            else
            {
                // Disconnected.
                Disconnect();
            }
        }

        private void Disconnect()
        {
            if (_wfdDevice != null)
            {
                var device = _wfdDevice;
                _wfdDevice = null;
                device.Dispose();
            }
        }
    }
}
