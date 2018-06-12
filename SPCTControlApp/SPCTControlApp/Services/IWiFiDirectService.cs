using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPCTControlApp.Services
{
    public interface IWiFiDirectService
    {
        string LastErrorMessage { get; }
        Task<bool> Connect(string deviceId);
    }
}
