using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SPCTControlApp.Services
{
    public interface IPanelService
    {
        event EventHandler OperationComplete;
        bool IsConnected { get; }
        string LastErrorMessage { get; }

        Task<bool> Connect(string panelBaseAddress);
        Task Disconnect();

        Task SetTempo(int newTempo);

        Task StartLine(int lineNumber);
        Task StopLine(int lineNumber);
        bool IsLineRunning(int lineNumber);

        Task SetLightOnOff(int row, int col, bool onOff);
    }

    public class PanelService : IPanelService
    {
        private static HttpClient _httpClient = new HttpClient();
        private string _panelBaseAddress = null;

        public bool IsConnected { get; private set; } = false;

        public string LastErrorMessage { get; private set; } = String.Empty;

        public event EventHandler OperationComplete;

        public async Task<bool> Connect(string panelBaseAddress)
        {
            var requestUri = $"http://{panelBaseAddress}/connect";
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            var response = await _httpClient.SendAsync(request);

            IsConnected = false;

            if (response.IsSuccessStatusCode)
            {
                LastErrorMessage = "Panel Contacted... waiting for connected status";
            }
            else
            {
                LastErrorMessage = $"Failed to contact panel: {response.ReasonPhrase}";
                OperationComplete?.Invoke(this, EventArgs.Empty);
                return false;
            }

            int retryCount = 0;

            while (retryCount <= 5)
            {
                response = await _httpClient.GetAsync($"http://{panelBaseAddress}/state");
                var result = await response.Content.ReadAsStringAsync();
                var stateResult = Newtonsoft.Json.JsonConvert.DeserializeObject<StateResult>(result);
                if (stateResult.State > 1)
                {
                    _panelBaseAddress = panelBaseAddress;
                    LastErrorMessage = "Success - Connected!";
                    IsConnected = true;
                    OperationComplete?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            LastErrorMessage = "Panel controller could not connect to light panel";
            OperationComplete?.Invoke(this, EventArgs.Empty);
            return false;
        }

        public async Task Disconnect()
        {
            var requestUri = $"http://{_panelBaseAddress}/connect";
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            var response = await _httpClient.SendAsync(request);

            IsConnected = false;

            if (response.IsSuccessStatusCode)
            {
                LastErrorMessage = "Panel Contacted... waiting for disconnected status";
            }
            else
            {
                LastErrorMessage = $"Failed to contact panel: {response.ReasonPhrase}";
                OperationComplete?.Invoke(this, EventArgs.Empty);
                return;
            }

            int retryCount = 0;

            while (retryCount <= 5)
            {
                response = await _httpClient.GetAsync($"http://{_panelBaseAddress}/state");
                var result = await response.Content.ReadAsStringAsync();
                var stateResult = Newtonsoft.Json.JsonConvert.DeserializeObject<StateResult>(result);
                if (stateResult.State <= 1)
                {
                    LastErrorMessage = "Success - Disconnected";
                    IsConnected = false;
                    _panelBaseAddress = null;
                    OperationComplete?.Invoke(this, EventArgs.Empty);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            LastErrorMessage = "Panel controller could not disconnect from light panel";
            OperationComplete?.Invoke(this, EventArgs.Empty);
            return;
        }

        public async Task SetTempo(int newTempo)
        {
            var requestUri = $"http://{_panelBaseAddress}/tempo";
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            request.Content = new StringContent($"{{\"bpm\":{newTempo}}}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                LastErrorMessage = "Tempo set successfully";
            }
            else
            {
                LastErrorMessage = $"Failed to contact panel: {response.ReasonPhrase}";
            }

            OperationComplete?.Invoke(this, EventArgs.Empty);
            return;
        }

        // TODO: really we need to ask the panel this question, but that protocol isn't there yet
        private Dictionary<int, bool> _lineRunning = new Dictionary<int, bool>();
        public async Task StartLine(int lineNumber)
        {
            var isRunning = false;
            if (_lineRunning.TryGetValue(lineNumber, out isRunning))
            {
                if (isRunning) { return; }
            }

            var requestUri = $"http://{_panelBaseAddress}/line/{lineNumber}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                LastErrorMessage = "Line started set successfully";
                isRunning = true;
            }
            else
            {
                LastErrorMessage = $"Failed to contact panel: {response.ReasonPhrase}";
            }

            if (_lineRunning.ContainsKey(lineNumber))
            {
                _lineRunning[lineNumber] = isRunning;
            }
            else
            {
                _lineRunning.Add(lineNumber, isRunning);
            }

            OperationComplete?.Invoke(this, EventArgs.Empty);
            return;
        }

        public async Task StopLine(int lineNumber)
        {
            if (!_lineRunning.ContainsKey(lineNumber) || !_lineRunning[lineNumber]) { return; }

            var requestUri = $"http://{_panelBaseAddress}/line/{lineNumber}";
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                LastErrorMessage = "Line stopped set successfully";
            }
            else
            {
                LastErrorMessage = $"Failed to contact panel: {response.ReasonPhrase}";
            }

            _lineRunning[lineNumber] = false;

            OperationComplete?.Invoke(this, EventArgs.Empty);
            return;
        }

        public bool IsLineRunning(int lineNumber)
        {
            return _lineRunning.ContainsKey(lineNumber) && _lineRunning[lineNumber];
        }

        public async Task SetLightOnOff(int row, int col, bool onOff)
        {
            var requestUri = $"http://{_panelBaseAddress}/led";
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            request.Content = new StringContent($"{{\"row\":{row},\"column\":{col},\"state\":\"{(onOff ? "on" : "off")}\"}}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                LastErrorMessage = $"Light turned {(onOff ? "on" : "off")} successfully";
            }
            else
            {
                LastErrorMessage = $"Failed to contact panel: {response.ReasonPhrase}";
            }

            OperationComplete?.Invoke(this, EventArgs.Empty);
            return;
        }

        private class StateResult
        {
            public int State { get; set; }
        }

    }
}
