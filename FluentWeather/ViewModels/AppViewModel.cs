using System;

namespace FluentWeather.ViewModels
{
    public class AppViewModel
    {
        //delegate to request ui update
        //public delegate void RequestUiUpdateDelegate();
        //public event RequestUiUpdateDelegate RequestUiUpdate;

        public event Action? UpdateUiAction;

        //https://stackoverflow.com/a/4378380/9008381
        public void UpdateUi()
        {
            UpdateUiAction?.Invoke();
        }

        // Raised when something outside the main page (for example a protocol link) asks to show a saved location.
        public event Action<string>? SwitchLocationRequested;

        public void RequestSwitchLocation(string placeId)
        {
            SwitchLocationRequested?.Invoke(placeId);
        }

    }
}
