using CommunityToolkit.Mvvm.ComponentModel;
using WinRT;

namespace FluentWeather.ViewModels
{
    [GeneratedBindableCustomPropertyAttribute]
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsLoadingData { get; set; }

        [ObservableProperty]
        public partial string AppVersionText { get; set; }
    }
}
