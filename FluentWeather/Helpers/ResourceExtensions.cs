using Windows.ApplicationModel.Resources;

namespace FluentWeather.Helpers
{
    internal static class ResourceExtensions
    {
        private static ResourceLoader _defaultResLoader = new ResourceLoader();

        public static string GetLocalized(this string resourceKey, bool isNotTranslatable = false, string resourceFileName = null)
        {
            ResourceLoader resLoader = _defaultResLoader;

            if (isNotTranslatable)
            {
                resLoader = new ResourceLoader("untranslatable");
            }


            if (!string.IsNullOrEmpty(resourceFileName)) {
                resLoader = new ResourceLoader(resourceFileName);
            }

            return resLoader.GetString(resourceKey); 
        }
    }
}
