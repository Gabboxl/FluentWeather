using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentWeather.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoadingData;

        [ObservableProperty]
        private string _appVersionText;
    }
}
