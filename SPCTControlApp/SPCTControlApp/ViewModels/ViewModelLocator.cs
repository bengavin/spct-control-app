using GalaSoft.MvvmLight.Ioc;
using SPCTControlApp.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPCTControlApp.ViewModels
{
    public static class ViewModelLocator
    {
        private static MainPageViewModel _mainViewModel;

        public static MainPageViewModel MainPageViewModel => _mainViewModel ?? (_mainViewModel = new MainPageViewModel(SimpleIoc.Default.GetInstance<IWiFiDirectService>()));
    }
}
