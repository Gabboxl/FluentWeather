namespace FluentWeather.ViewModels
{
    public static class AppViewModelHolder
    {
        private static AppViewModel _viewModel;

        public static AppViewModel GetViewModel()
        {
            _viewModel ??= new AppViewModel();

            return _viewModel;
        }
    }
}
