namespace FluidWeather.ViewModels
{
    public static class ViewModelHolder
    {
        private static AppViewModel _viewModel;

        public static AppViewModel GetViewModel()
        {
            if (_viewModel == null)
            {
                _viewModel = new AppViewModel();
            }

            return _viewModel;
        }
    }
}
