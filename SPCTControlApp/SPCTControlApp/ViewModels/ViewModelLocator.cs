using GalaSoft.MvvmLight.Ioc;
using SPCTControlApp.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPCTControlApp.ViewModels
{
    public class ViewModelLocator
    {
        private static MainPageViewModel _mainViewModel;

        public ViewModelLocator()
        {
            // Register IoC
            SimpleIoc.Default.Register<IPanelService, PanelService>();
            SimpleIoc.Default.Register<MainPageViewModel>();
        }

        public MainPageViewModel MainPageViewModel => _mainViewModel ?? (_mainViewModel = SimpleIoc.Default.GetInstance<MainPageViewModel>());
        public void Cleanup()
        {
            _mainViewModel.Cleanup();
            _mainViewModel = null;
        }
    }
}
