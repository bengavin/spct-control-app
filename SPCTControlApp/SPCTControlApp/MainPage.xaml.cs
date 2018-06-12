using SPCTControlApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SPCTControlApp
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

        protected override void OnAppearing()
        {
            this.BindingContext = ViewModelLocator.MainPageViewModel;

            base.OnAppearing();
        }
    }
}
