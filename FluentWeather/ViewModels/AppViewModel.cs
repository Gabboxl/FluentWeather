using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentWeather.ViewModels
{
    public partial class AppViewModel : ObservableObject
    {
        //delegate to request ui update
        //public delegate void RequestUiUpdateDelegate();
        //public event RequestUiUpdateDelegate RequestUiUpdate;

        public event Action UpdateUiAction;

        //https://stackoverflow.com/a/4378380/9008381
        public void UpdateUi()
        {
            UpdateUiAction?.Invoke();
        }

    }
}
