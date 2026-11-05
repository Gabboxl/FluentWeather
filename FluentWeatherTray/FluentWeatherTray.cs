// FluentWeather tray helper.
//
// This file builds in two ways.
//
// Packaged build (symbols PACKAGED and WINRT): FluentWeatherTray.csproj builds it, and the app
// package includes the result in its FluentWeatherTray folder. The app starts it when the user
// turns on "Tray icons" in the settings, and Windows starts it at sign-in through the package's
// startup task. It reads the app's LocalState folder directly. It exits when the app writes "off"
// to tray-enabled.txt. The Store updates it together with the app.
//
// Local build (no PACKAGED symbol): Setup compiles this file on your PC with the C# compiler that
// ships with Windows (.NET Framework 4.x). The result is FluentWeatherTray.exe in
// C:\Program Files\FluentWeatherLocal. It starts at sign-in from your Startup folder.
//
// It shows two icons side by side in the notification area, the number on the left:
//   1. the weather picture for the current conditions, one of 15: sunny, clear night (the moon in
//      its current phase), partly cloudy, partly cloudy night, cloudy, cloudy night, fog, light rain, rain,
//      heavy rain, ice or mixed, thunderstorm, severe storm, light snow and heavy snow.
//      The pictures are drawn in code (WeatherArt).
//   2. the temperature or the "feels like" temperature, as a number
// Right-click either icon to choose which number is shown, to choose which saved city the
// tray shows (without opening the app window), to check for updates, to close the app window,
// or to exit. Left-click opens the app at the city the tray shows. When the app window is the
// active window, left-click closes it again, so only the tray icons stay.
// After "Exit tray icons", start them again from the Start menu item "FluentWeather tray icons".
// "Restart FluentWeather" (tray menu and Start menu) closes the app and the tray icons, then starts both.
//
// Where the data comes from: the app writes the current conditions to
// tray-weather.txt (the app's city) and tray-weather-all.txt (every saved city)
// in its own LocalState folder after every update. The app's
// background task refreshes it about every 15 minutes, even when the app is closed.
// This helper watches and reads that file, so a city change shows within a second. It does not connect to the internet.
//
// 5-minute updates: while the app window is closed or minimized, this helper asks the app every
// 5 minutes to refresh the weather files. It does this through the app's "FluentWeatherTrayRefresh"
// app service, which Windows runs inside the app without opening its window. The app downloads the
// data. This helper still makes no internet connection. Setup compiles this part only when the
// Windows Runtime references are found (symbol WINRT). Without them the helper works as before.
//
// Language level: C# 5, so the compiler built into Windows can build it.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Threading.Tasks;
#if WINRT
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
#endif
#if PACKAGED
using Windows.ApplicationModel;
using Windows.Storage;
#endif

[assembly: System.Reflection.AssemblyTitle("FluentWeather tray icon")]
[assembly: System.Reflection.AssemblyDescription("Shows the FluentWeather temperature and weather icon in the notification area.")]
#if PACKAGED
[assembly: System.Reflection.AssemblyProduct("FluentWeather")]
#else
[assembly: System.Reflection.AssemblyProduct("FluentWeather Local")]
[assembly: System.Reflection.AssemblyCompany("Built on this PC by FluentWeatherLocal Setup")]
#endif
[assembly: System.Reflection.AssemblyVersion("1.25.1.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.25.1.0")]

namespace FluentWeatherLocal
{
    // The values that differ between the packaged build and the local build.
    internal static class TrayHost
    {
#if PACKAGED
        internal static readonly bool Packaged = true;
        internal const string Protocol = "fluentweather:";
        internal const string MutexName = @"Local\FluentWeatherTray";
        internal const string EnabledFileName = "tray-enabled.txt";

        private static string _appTitle;

        // The app's own data folder (LocalState). The tray settings are kept there too.
        internal static string WorkDir
        {
            get { return ApplicationData.Current.LocalFolder.Path; }
        }

        internal static string PackageFamilyName
        {
            get { return Package.Current.Id.FamilyName; }
        }

        // The folder the package is installed in, with a closing backslash.
        internal static string AppFolder
        {
            get { return Package.Current.InstalledLocation.Path.TrimEnd('\\') + "\\"; }
        }

        // The app's display name in the user's language. Windows uses it as the window title.
        internal static string AppTitle
        {
            get
            {
                if (_appTitle == null)
                {
                    try
                    {
                        var entries = Task.Run(async () => await Package.Current.GetAppListEntriesAsync()).Result;
                        if (entries.Count > 0)
                            _appTitle = entries[0].DisplayInfo.DisplayName;
                    }
                    catch (Exception) { }
                    if (string.IsNullOrEmpty(_appTitle))
                        _appTitle = "FluentWeather";
                }
                return _appTitle;
            }
        }

        // The app writes "on" or "off" to tray-enabled.txt when the user changes the "Tray icons" setting.
        internal static bool IsEnabled()
        {
            try
            {
                var file = Path.Combine(WorkDir, EnabledFileName);
                return File.Exists(file) && File.ReadAllText(file).Trim().Equals("on", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return true;   // a file read error must not close the icons
            }
        }

        // The packaged app window has exactly the display name as its title.
        internal static bool IsAppTitle(string title)
        {
            return string.Equals(title, AppTitle, StringComparison.Ordinal);
        }
#else
        internal static readonly bool Packaged = false;
        internal const string Protocol = "fluentweather-local:";
        internal const string MutexName = @"Local\FluentWeatherLocalTray";
        internal const string AppTitle = "FluentWeather (Local)";

        internal static string WorkDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentWeatherLocal"); }
        }

        internal static bool IsAppTitle(string title)
        {
            return title.IndexOf(AppTitle, StringComparison.Ordinal) >= 0;
        }
#endif
    }

    internal static class Program
    {
        [STAThread]
        // FluentWeatherTray.exe            start the tray icons (does nothing if they already run)
        // FluentWeatherTray.exe /restart   close the app and any running tray icons, then start both again
        private static void Main(string[] args)
        {
            var restart = Array.Exists(args, a => string.Equals(a, "/restart", StringComparison.OrdinalIgnoreCase));
            if (restart)
            {
                AppControl.CloseApp();
                AppControl.CloseOtherTrayInstances();
            }

            bool createdNew;
#if PACKAGED
            // Windows starts this program at sign-in through the startup task. Stay closed when the
            // user turned the tray icons off in the app.
            if (!TrayHost.IsEnabled())
                return;
#endif
            using (var mutex = new Mutex(true, TrayHost.MutexName, out createdNew))
            {
                if (!createdNew && restart)
                {
                    // The old tray instance was just ended. Take over its lock.
                    try { createdNew = mutex.WaitOne(5000); }
                    catch (AbandonedMutexException) { createdNew = true; }
                }
                if (!createdNew)
                    return; // already running

                if (restart)
                    AppControl.LaunchApp();

                // Draw icons at the real screen scale instead of letting Windows stretch a 16 px image.
                try { NativeMethods.SetProcessDPIAware(); } catch (Exception) { }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayContext());
                GC.KeepAlive(mutex);
            }
        }
    }

    internal sealed class WeatherReading
    {
        public string Place = string.Empty;
        public string FullPlace = string.Empty;
        public int Temp;
        public int Feels;
        public string Unit = string.Empty;
        public int IconCode = -1;
        public bool IsNight;           // "daynight=N" from the app
        public double Latitude = 45;   // north of the equator unless the app says otherwise
        public string Phrase = string.Empty;
        public DateTime ObservedLocal;
        public DateTime WrittenUtc;
        // Ready-to-show details written by the app ("d.Humidity=96%" becomes Details["Humidity"] = "96%").
        public Dictionary<string, string> Details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    // The details the hover panel can show, in display order, with their labels.
    internal static class DetailCatalog
    {
        public static readonly string[][] All =
        {
            new[] { "Conditions", "Conditions" },
            new[] { "Temperature", "Temperature" },
            new[] { "FeelsLike", "Feels like" },
            new[] { "HighLow", "24 h high / low" },
            new[] { "Change24h", "24 h change" },
            new[] { "DewPoint", "Dew point" },
            new[] { "HeatIndex", "Heat index" },
            new[] { "WindChill", "Wind chill" },
            new[] { "Humidity", "Humidity" },
            new[] { "Wind", "Wind" },
            new[] { "UV", "UV index" },
            new[] { "Pressure", "Pressure" },
            new[] { "Visibility", "Visibility" },
            new[] { "CloudCeiling", "Cloud ceiling" },
            new[] { "Rain1h", "Rain, last hour" },
            new[] { "Rain6h", "Rain, last 6 h" },
            new[] { "Rain24h", "Rain, last 24 h" },
            new[] { "Snow24h", "Snow, last 24 h" },
            new[] { "Sunrise", "Sunrise" },
            new[] { "Sunset", "Sunset" },
            // The tray works out the moon phase itself. The app writes the next moonrise and moonset.
            new[] { "Moon", "Moon phase" },
            new[] { "Moonrise", "Moonrise" },
            new[] { "Moonset", "Moonset" },
            new[] { "Observed", "Observed at" },
            // The nearest official station that reports (usually an airport). weather.com blends many
            // sources for the exact location, so the conditions are not taken from this station alone.
            new[] { "Station", "Nearest station" },
        };

        public static readonly string[] Defaults =
        {
            "Conditions", "Temperature", "FeelsLike", "HighLow", "Humidity", "Wind", "UV", "Rain24h", "Sunrise", "Sunset", "Moon", "Observed"
        };

        // Raised when a new detail joins the defaults. Saved choices from an older version get it once.
        public const int Version = 2;
    }

    // The 14 weather pictures of the tray icon and the hover panel. They are drawn in code on a
    // 100 x 100 grid and scaled, so they stay sharp at every display scale. Small sizes are drawn
    // four times larger and then reduced, which gives smooth edges.
    internal enum WeatherKind
    {
        None = -1,
        Sunny = 0,
        PartlyCloudy = 1,
        Cloudy = 2,
        LightRain = 3,
        Rain = 4,
        Thunderstorm = 5,
        LightSnow = 6,
        HeavySnow = 7,
        HeavyRain = 8,
        Ice = 9,
        Fog = 10,
        Severe = 11,
        ClearNight = 12,
        PartlyCloudyNight = 13,
        CloudyNight = 14
    }

    internal static class WeatherArt
    {
        // weather.com icon codes 0 to 47 (developer.weather.com, "Icon Codes & Icon Images").
        internal static WeatherKind FromIconCode(int code)
        {
            switch (code)
            {
                case 0: case 1: case 2: case 3:            // tornado, tropical storm, hurricane, strong storms
                    return WeatherKind.Severe;
                case 4: case 37: case 38: case 47:          // thunderstorms, isolated and scattered
                    return WeatherKind.Thunderstorm;
                // 11 is "Showers", also sent with "Light Rain". The app shows it as a cloud with two drops.
                case 9: case 11: case 39: case 45:          // drizzle, showers, scattered showers
                    return WeatherKind.LightRain;
                case 12:                                    // rain
                    return WeatherKind.Rain;
                case 40:                                    // heavy rain
                    return WeatherKind.HeavyRain;
                case 5: case 6: case 7: case 8: case 10:    // rain and snow, rain and sleet, wintry mix, freezing drizzle, freezing rain
                case 17: case 18: case 35:                  // hail, sleet, mixed rain and hail
                    return WeatherKind.Ice;
                case 13: case 14: case 41: case 46:         // flurries, snow showers, scattered snow showers
                    return WeatherKind.LightSnow;
                case 15: case 16: case 42: case 43:         // blowing snow, snow, heavy snow, blizzard
                    return WeatherKind.HeavySnow;
                case 19: case 20: case 21: case 22:         // dust, fog, haze, smoke
                    return WeatherKind.Fog;
                case 25: case 26:                           // frigid, cloudy
                    return WeatherKind.Cloudy;
                // 27 and 28 are "mostly cloudy". The app shows them with the moon or sun partly behind a cloud.
                case 23: case 24: case 28: case 30:         // breezy, windy, mostly and partly cloudy (day)
                    return WeatherKind.PartlyCloudy;
                case 27: case 29:                           // mostly and partly cloudy (night)
                    return WeatherKind.PartlyCloudyNight;
                case 32: case 34: case 36:                  // sunny, mostly sunny, hot
                    return WeatherKind.Sunny;
                case 31: case 33:                           // clear and mostly clear (night)
                    return WeatherKind.ClearNight;
                default:
                    return WeatherKind.None;
            }
        }

        // The picture for a reading: the icon code, then the condition text. The text decides when
        //   - the code shows no rain, snow or ice (sun, moon, cloud, fog) but the text names them.
        //     weather.com sends a cloud code with "Showers in the Vicinity" when rain falls nearby.
        //   - the code shows rain or snow and the text names the strength. weather.com uses code 11
        //     for "Showers" and "Light Rain", and code 12 for "Rain" and "Heavy Rain".
        // The text is English unless the app language is changed. In other languages the code decides.
        // weather.com sends the same "Cloudy" code by day and by night. At night (daynight=N from the
        // app) the picture shows the edge of the moon behind the cloud.
        internal static WeatherKind FromReading(int code, string phrase, bool night = false)
        {
            var kind = FromIconCode(code);
            if (night && kind == WeatherKind.Cloudy)
                kind = WeatherKind.CloudyNight;
            if (kind == WeatherKind.None || string.IsNullOrEmpty(phrase))
                return kind;

            var text = phrase.ToLowerInvariant();
            var thunder = text.Contains("thunder") || text.Contains("t-storm");
            var ice = text.Contains("freezing") || text.Contains("sleet") || text.Contains("hail") ||
                      text.Contains("ice pellet") || text.Contains("wintry");
            var snow = text.Contains("snow") || text.Contains("flurr") || text.Contains("blizzard");
            var rain = text.Contains("rain") || text.Contains("shower") || text.Contains("drizzle");
            var heavy = text.Contains("heavy") || text.Contains("torrential") || text.Contains("blizzard");
            var light = text.Contains("light") || text.Contains("drizzle") || text.Contains("flurr") ||
                        text.Contains("vicinity") || text.Contains("scattered") || text.Contains("isolated");

            var isRain = kind == WeatherKind.LightRain || kind == WeatherKind.Rain || kind == WeatherKind.HeavyRain;
            var isSnow = kind == WeatherKind.LightSnow || kind == WeatherKind.HeavySnow;
            var isDry = kind == WeatherKind.Sunny || kind == WeatherKind.ClearNight || kind == WeatherKind.Cloudy || kind == WeatherKind.CloudyNight ||
                        kind == WeatherKind.PartlyCloudy || kind == WeatherKind.PartlyCloudyNight || kind == WeatherKind.Fog;

            if (thunder && (isDry || isRain || isSnow || kind == WeatherKind.Ice))
                return WeatherKind.Thunderstorm;

            // A dry picture, but the text names something falling.
            if (isDry)
            {
                // "Snow Showers" is snow. "Rain and Snow" (rain or drizzle with snow) is mixed.
                var mixed = snow && (text.Contains("rain") || text.Contains("drizzle"));
                if (ice || mixed) return WeatherKind.Ice;
                if (snow) return heavy ? WeatherKind.HeavySnow : WeatherKind.LightSnow;
                if (rain) return heavy ? WeatherKind.HeavyRain : light ? WeatherKind.LightRain : WeatherKind.Rain;
                return kind;
            }

            if (isRain)
            {
                if (ice) return WeatherKind.Ice;
                if (heavy) return WeatherKind.HeavyRain;
                if (text.Contains("light") || text.Contains("drizzle")) return WeatherKind.LightRain;
            }
            if (isSnow)
            {
                if (heavy) return WeatherKind.HeavySnow;
                if (text.Contains("light") || text.Contains("flurr")) return WeatherKind.LightSnow;
            }
            return kind;
        }

        internal static string Describe(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Sunny: return "Sunny";
                case WeatherKind.PartlyCloudy: return "Partly cloudy";
                case WeatherKind.Cloudy: return "Cloudy";
                case WeatherKind.CloudyNight: return "Cloudy night";
                case WeatherKind.LightRain: return "Light rain";
                case WeatherKind.Rain: return "Rain";
                case WeatherKind.HeavyRain: return "Heavy rain";
                case WeatherKind.Thunderstorm: return "Thunderstorm";
                case WeatherKind.LightSnow: return "Light snow";
                case WeatherKind.HeavySnow: return "Heavy snow";
                case WeatherKind.Ice: return "Ice or mixed";
                case WeatherKind.Fog: return "Fog";
                case WeatherKind.Severe: return "Severe storm";
                case WeatherKind.ClearNight: return "Clear night";
                case WeatherKind.PartlyCloudyNight: return "Partly cloudy night";
                default: return string.Empty;
            }
        }

        // A picture of the given size in pixels. "light" means a light background (light taskbar or panel).
        // latitude decides which side of the moon is lit (mirrored south of the equator).
        internal static Bitmap Render(WeatherKind kind, int size, bool light, double latitude = 45)
        {
            var detailed = size >= 48;
            var factor = detailed ? 2 : 4;
            var big = size * factor;
            using (var large = new Bitmap(big, big, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(large))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.ScaleTransform(big / 100f, big / 100f);
                    Draw(g, kind, light, detailed, latitude);
                }

                var result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(result))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(large, new Rectangle(0, 0, size, size));
                }
                return result;
            }
        }

        private static void Draw(Graphics g, WeatherKind kind, bool light, bool detailed, double latitude)
        {
            switch (kind)
            {
                case WeatherKind.ClearNight:
                    if (detailed)
                    {
                        DrawStar(g, 14, 18, 5, light);
                        DrawStar(g, 86, 14, 4, light);
                        DrawStar(g, 88, 82, 5, light);
                    }
                    DrawMoon(g, 50, 50, 36, light, detailed, latitude);
                    break;
                case WeatherKind.PartlyCloudyNight:
                    DrawMoon(g, 36, 36, 28, light, detailed, latitude);
                    DrawCloud(g, 24, 38, 0.92f, false, light, detailed);
                    break;
                case WeatherKind.Fog:
                    DrawFog(g, light);
                    break;
                case WeatherKind.Ice:
                    DrawCloud(g, 25, 2, 0.68f, true, light, detailed);
                    DrawDrop(g, 14, 48, 1.35f, light, detailed);
                    DrawFlake(g, 38, 76, 11, light, detailed);
                    DrawDrop(g, 62, 48, 1.35f, light, detailed);
                    DrawFlake(g, 86, 76, 11, light, detailed);
                    break;
                case WeatherKind.Severe:
                    DrawBolt(g, light, detailed, 0.8f, -8, 0);
                    DrawWarning(g, light, detailed);
                    break;
                case WeatherKind.Sunny:
                    DrawSun(g, 50, 50, 1f, light, detailed);
                    break;
                case WeatherKind.PartlyCloudy:
                    DrawSun(g, 36, 36, 0.78f, light, detailed);
                    DrawCloud(g, 24, 38, 0.92f, false, light, detailed);
                    break;
                case WeatherKind.Cloudy:
                    DrawCloud(g, 6, 14, 1.2f, false, light, detailed);
                    break;
                case WeatherKind.CloudyNight:
                    DrawCloudyNight(g, light, detailed, latitude);
                    break;
                // In the three rain pictures the cloud ends 40% down from the top, so the drops have
                // the lower 60% and stay easy to count at 16 pixels: 2, 4 or 7 drops.
                case WeatherKind.LightRain:
                    DrawCloud(g, 25, 2, 0.68f, true, light, detailed);
                    DrawDrop(g, 34, 50, 1.7f, light, detailed);
                    DrawDrop(g, 66, 58, 1.7f, light, detailed);
                    break;
                case WeatherKind.Rain:
                    DrawCloud(g, 25, 2, 0.68f, true, light, detailed);
                    DrawDrop(g, 14, 48, 1.35f, light, detailed);
                    DrawDrop(g, 38, 62, 1.35f, light, detailed);
                    DrawDrop(g, 62, 48, 1.35f, light, detailed);
                    DrawDrop(g, 86, 62, 1.35f, light, detailed);
                    break;
                case WeatherKind.HeavyRain:
                    DrawCloud(g, 25, 2, 0.68f, true, light, detailed, true);
                    DrawDrop(g, 14, 46, 1.2f, light, detailed);
                    DrawDrop(g, 38, 46, 1.2f, light, detailed);
                    DrawDrop(g, 62, 46, 1.2f, light, detailed);
                    DrawDrop(g, 86, 46, 1.2f, light, detailed);
                    DrawDrop(g, 26, 72, 1.2f, light, detailed);
                    DrawDrop(g, 50, 72, 1.2f, light, detailed);
                    DrawDrop(g, 74, 72, 1.2f, light, detailed);
                    break;
                case WeatherKind.Thunderstorm:
                    DrawBolt(g, light, detailed);
                    DrawDrop(g, 13, 10, 0.85f, light, detailed);
                    DrawDrop(g, 21, 40, 0.85f, light, detailed);
                    DrawDrop(g, 12, 70, 0.85f, light, detailed);
                    DrawDrop(g, 82, 4, 0.85f, light, detailed);
                    DrawDrop(g, 89, 30, 0.85f, light, detailed);
                    DrawDrop(g, 80, 56, 0.85f, light, detailed);
                    DrawDrop(g, 88, 80, 0.85f, light, detailed);
                    break;
                case WeatherKind.LightSnow:
                    DrawFlake(g, 28, 30, 19, light, detailed);
                    DrawFlake(g, 72, 30, 19, light, detailed);
                    DrawFlake(g, 50, 72, 19, light, detailed);
                    break;
                case WeatherKind.HeavySnow:
                    DrawFlake(g, 30, 17, 13, light, detailed);
                    DrawFlake(g, 70, 17, 13, light, detailed);
                    DrawFlake(g, 14, 50, 13, light, detailed);
                    DrawFlake(g, 50, 50, 13, light, detailed);
                    DrawFlake(g, 86, 50, 13, light, detailed);
                    DrawFlake(g, 30, 83, 13, light, detailed);
                    DrawFlake(g, 70, 83, 13, light, detailed);
                    break;
            }
        }

        // A thin edge around each shape, so it stands out on light and dark backgrounds.
        private static Color Edge(bool light, Color dark)
        {
            return light ? dark : Color.FromArgb(160, 0, 0, 0);
        }

        private static void DrawSun(Graphics g, float cx, float cy, float s, bool light, bool detailed)
        {
            var rayColor = Color.FromArgb(255, 255, 176, 0);
            using (var edge = new Pen(Edge(light, Color.FromArgb(255, 178, 106, 0)), 11f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var ray = new Pen(rayColor, 7f * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                for (var i = 0; i < 8; i++)
                {
                    var a = i * Math.PI / 4;
                    var x1 = cx + (float)Math.Cos(a) * 33f * s;
                    var y1 = cy + (float)Math.Sin(a) * 33f * s;
                    var x2 = cx + (float)Math.Cos(a) * 45f * s;
                    var y2 = cy + (float)Math.Sin(a) * 45f * s;
                    g.DrawLine(edge, x1, y1, x2, y2);
                    g.DrawLine(ray, x1, y1, x2, y2);
                }
            }

            var r = 24f * s;
            var disc = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
            using (var edge = new Pen(Edge(light, Color.FromArgb(255, 178, 106, 0)), 3.5f * s))
                g.DrawEllipse(edge, disc);
            if (detailed)
            {
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(disc);
                    using (var brush = new PathGradientBrush(path))
                    {
                        brush.CenterPoint = new PointF(cx - r * 0.35f, cy - r * 0.35f);
                        brush.CenterColor = Color.FromArgb(255, 255, 236, 140);
                        brush.SurroundColors = new[] { Color.FromArgb(255, 255, 160, 0) };
                        g.FillEllipse(brush, disc);
                    }
                }
            }
            else
            {
                using (var brush = new SolidBrush(Color.FromArgb(255, 255, 196, 40)))
                    g.FillEllipse(brush, disc);
            }
        }

        // A cloud of about 74 x 56 grid units at scale 1, with its top left corner at (ox, oy).
        private static GraphicsPath CloudPath(float ox, float oy, float s)
        {
            var path = new GraphicsPath(FillMode.Winding);
            path.AddEllipse(0f, 30f, 26f, 26f);       // left end
            path.AddEllipse(48f, 30f, 26f, 26f);      // right end
            path.AddRectangle(new RectangleF(13f, 30f, 48f, 26f));
            path.AddEllipse(7f, 13f, 34f, 34f);       // left bump
            path.AddEllipse(24f, 0f, 44f, 44f);       // top bump
            path.AddEllipse(53f, 25f, 22f, 22f);      // right bump
            using (var m = new Matrix())
            {
                m.Translate(ox, oy);
                m.Scale(s, s);
                path.Transform(m);
            }
            return path;
        }

        // heavy = a darker rain cloud, for heavy rain.
        private static void DrawCloud(Graphics g, float ox, float oy, float s, bool rain, bool light, bool detailed, bool heavy = false)
        {
            using (var path = CloudPath(ox, oy, s))
            {
                var edgeColor = Edge(light, heavy ? Color.FromArgb(255, 56, 64, 78) : rain ? Color.FromArgb(255, 84, 96, 112) : Color.FromArgb(255, 112, 124, 140));
                using (var edge = new Pen(edgeColor, 5f) { LineJoin = LineJoin.Round })
                    g.DrawPath(edge, path);

                var top = heavy ? Color.FromArgb(255, 150, 160, 176) : rain ? Color.FromArgb(255, 214, 220, 228) : Color.White;
                var bottom = heavy ? Color.FromArgb(255, 92, 104, 122) : rain ? Color.FromArgb(255, 160, 172, 188) : Color.FromArgb(255, 214, 222, 232);
                if (detailed)
                {
                    var bounds = path.GetBounds();
                    using (var brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
                        g.FillPath(brush, path);
                }
                else
                {
                    var fill = heavy ? Color.FromArgb(255, 124, 136, 154) : rain ? Color.FromArgb(255, 196, 204, 216) : Color.FromArgb(255, 244, 246, 250);
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);
                }
            }
        }

        // A rain drop about 12 x 20 grid units at scale 1. (cx, top) is its tip.
        private static void DrawDrop(Graphics g, float cx, float top, float s, bool light, bool detailed)
        {
            var w = 12f * s;
            var h = 20f * s;
            using (var path = new GraphicsPath(FillMode.Winding))
            {
                path.AddEllipse(cx - w / 2, top + h - w, w, w);
                path.AddPolygon(new[]
                {
                    new PointF(cx, top),
                    new PointF(cx + w * 0.47f, top + h - w * 0.62f),
                    new PointF(cx - w * 0.47f, top + h - w * 0.62f)
                });

                using (var edge = new Pen(Edge(light, Color.FromArgb(255, 18, 64, 140)), 3f) { LineJoin = LineJoin.Round })
                    g.DrawPath(edge, path);

                var main = light ? Color.FromArgb(255, 31, 111, 235) : Color.FromArgb(255, 78, 163, 255);
                if (detailed)
                {
                    using (var brush = new LinearGradientBrush(new RectangleF(cx - w / 2, top, w, h),
                               Color.FromArgb(255, 140, 200, 255), main, LinearGradientMode.Vertical))
                        g.FillPath(brush, path);
                }
                else
                {
                    using (var brush = new SolidBrush(main))
                        g.FillPath(brush, path);
                }
            }
        }

        private static void DrawBolt(Graphics g, bool light, bool detailed, float s = 1f, float dx = 0f, float dy = 0f)
        {
            var points = new[]
            {
                new PointF(58f, 4f), new PointF(30f, 54f), new PointF(47f, 54f), new PointF(37f, 96f),
                new PointF(74f, 40f), new PointF(55f, 40f), new PointF(69f, 4f)
            };
            for (var i = 0; i < points.Length; i++)
                points[i] = new PointF(points[i].X * s + dx, points[i].Y * s + dy);
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(points);
                using (var edge = new Pen(Edge(light, Color.FromArgb(255, 138, 90, 0)), 5f) { LineJoin = LineJoin.Round })
                    g.DrawPath(edge, path);
                if (detailed)
                {
                    using (var brush = new LinearGradientBrush(new RectangleF(30f * s + dx, 4f * s + dy, 44f * s, 92f * s),
                               Color.FromArgb(255, 255, 236, 120), Color.FromArgb(255, 255, 170, 0), LinearGradientMode.Vertical))
                        g.FillPath(brush, path);
                }
                else
                {
                    using (var brush = new SolidBrush(Color.FromArgb(255, 255, 208, 40)))
                        g.FillPath(brush, path);
                }
            }
        }

        private static void DrawFlake(Graphics g, float cx, float cy, float r, bool light, bool detailed)
        {
            // Blue on a light background, white on a dark one. A flake has no edge: thin lines with an
            // edge look gray at tray size.
            var color = light ? Color.FromArgb(255, 37, 110, 220) : Color.FromArgb(255, 240, 246, 255);
            var width = Math.Max(4f, r * 0.3f);
            using (var pen = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                foreach (var p in new[] { pen })
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var a = Math.PI / 2 + i * Math.PI / 3;
                        var dx = (float)Math.Cos(a) * r;
                        var dy = (float)Math.Sin(a) * r;
                        g.DrawLine(p, cx - dx, cy - dy, cx + dx, cy + dy);
                    }
                    if (!detailed)
                        continue;
                    // Small branches on each of the six arms.
                    for (var i = 0; i < 6; i++)
                    {
                        var a = Math.PI / 2 + i * Math.PI / 3;
                        var bx = cx + (float)Math.Cos(a) * r * 0.58f;
                        var by = cy + (float)Math.Sin(a) * r * 0.58f;
                        for (var side = -1; side <= 1; side += 2)
                        {
                            var b = a + side * Math.PI / 4;
                            g.DrawLine(p, bx, by, bx + (float)Math.Cos(b) * r * 0.34f, by + (float)Math.Sin(b) * r * 0.34f);
                        }
                    }
                }
            }
        }

        // Overcast at night: the edge of the moon shows behind the cloud, on the moon's lit side, so the
        // visible part is the lit edge. At tray sizes more of the moon shows, so it stays visible at 16 px.
        private static void DrawCloudyNight(Graphics g, bool light, bool detailed, double latitude)
        {
            var waxing = MoonPhase(DateTime.UtcNow) < 0.5;
            var litRight = waxing != (latitude < 0);
            if (litRight)
            {
                if (detailed)
                {
                    DrawStar(g, 14, 14, 4, light);
                    DrawMoon(g, 69, 27, 21, light, true, latitude);
                    DrawCloud(g, 6, 20, 1.14f, false, light, true);
                }
                else
                {
                    DrawMoon(g, 75, 25, 22, light, false, latitude);
                    DrawCloud(g, 2, 24, 1.1f, false, light, false);
                }
            }
            else
            {
                if (detailed)
                {
                    DrawStar(g, 86, 14, 4, light);
                    DrawMoon(g, 31, 31, 21, light, true, latitude);
                    DrawCloud(g, 8, 20, 1.14f, false, light, true);
                }
                else
                {
                    DrawMoon(g, 25, 25, 22, light, false, latitude);
                    DrawCloud(g, 10, 24, 1.1f, false, light, false);
                }
            }
        }

        // The moon in its phase today. The lit part grows on the right while waxing and shrinks
        // toward the left while waning, as seen north of the equator. South of it, the sides swap.
        private static void DrawMoon(Graphics g, float cx, float cy, float r, bool light, bool detailed, double latitude)
        {
            var phase = MoonPhase(DateTime.UtcNow);                 // 0 new, 0.25 first quarter, 0.5 full, 0.75 last quarter
            var disc = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
            var edgeColor = Edge(light, Color.FromArgb(255, 96, 88, 78));
            using (var edge = new Pen(edgeColor, 4f))
                g.DrawEllipse(edge, disc);

            // The dark part of the moon stays faintly visible, so the round shape is clear in every phase.
            using (var dark = new SolidBrush(light ? Color.FromArgb(255, 94, 96, 106) : Color.FromArgb(255, 64, 68, 78)))
                g.FillEllipse(dark, disc);

            using (var lit = LitPath(cx, cy, r, phase, latitude < 0))
            {
                if (lit == null)
                    return;
                if (detailed)
                {
                    using (var brush = new PathGradientBrush(lit))
                    {
                        brush.CenterPoint = new PointF(cx, cy);
                        // A light brown moon. A deeper brown on a light background, so the moon stands out.
                        brush.CenterColor = light ? Color.FromArgb(255, 226, 200, 162) : Color.FromArgb(255, 240, 222, 194);
                        brush.SurroundColors = new[] { light ? Color.FromArgb(255, 180, 146, 104) : Color.FromArgb(255, 200, 172, 134) };
                        g.FillPath(brush, lit);
                    }
                    // A few craters on the lit part.
                    var state = g.Save();
                    g.SetClip(lit);
                    using (var crater = new SolidBrush(Color.FromArgb(70, 150, 126, 96)))
                    {
                        g.FillEllipse(crater, cx - r * 0.45f, cy - r * 0.35f, r * 0.32f, r * 0.32f);
                        g.FillEllipse(crater, cx + r * 0.15f, cy + r * 0.10f, r * 0.42f, r * 0.42f);
                        g.FillEllipse(crater, cx - r * 0.20f, cy + r * 0.45f, r * 0.22f, r * 0.22f);
                        g.FillEllipse(crater, cx + r * 0.30f, cy - r * 0.55f, r * 0.20f, r * 0.20f);
                    }
                    g.Restore(state);
                }
                else
                {
                    using (var brush = new SolidBrush(light ? Color.FromArgb(255, 184, 150, 108) : Color.FromArgb(255, 226, 202, 166)))
                        g.FillPath(brush, lit);
                }
            }
        }

        // Fraction of the lunar cycle, from a new moon on 6 January 2000 at 18:14 UTC.
        // Accurate to about half a day, which is enough for a picture.
        internal static double MoonPhase(DateTime utc)
        {
            const double SynodicMonth = 29.530588853;
            var days = (utc - new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc)).TotalDays;
            var phase = (days / SynodicMonth) % 1.0;
            return phase < 0 ? phase + 1.0 : phase;
        }

        internal static string MoonPhaseName(double phase)
        {
            if (phase < 0.03 || phase > 0.97) return "New moon";
            if (phase < 0.22) return "Waxing crescent";
            if (phase < 0.28) return "First quarter";
            if (phase < 0.47) return "Waxing gibbous";
            if (phase < 0.53) return "Full moon";
            if (phase < 0.72) return "Waning gibbous";
            if (phase < 0.78) return "Last quarter";
            return "Waning crescent";
        }

        // The lit part of a moon disc. Null at new moon.
        private static GraphicsPath LitPath(float cx, float cy, float r, double phase, bool southern)
        {
            var lit = (1 - Math.Cos(2 * Math.PI * phase)) / 2;   // lit fraction of the disc
            if (lit < 0.02)
                return null;

            var path = new GraphicsPath();
            var disc = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
            if (lit > 0.98)
            {
                path.AddEllipse(disc);
                return path;
            }

            // The terminator is half an ellipse with this horizontal radius.
            var rx = (float)(r * Math.Abs(Math.Cos(2 * Math.PI * phase)));
            var term = new RectangleF(cx - Math.Max(rx, 0.01f), cy - r, 2 * Math.Max(rx, 0.01f), 2 * r);
            var waxing = phase < 0.5;
            var litRight = waxing != southern;
            var gibbous = lit > 0.5;

            // Angles in GDI+: 0 = right, 90 = bottom, 180 = left, 270 = top.
            if (litRight)
            {
                path.AddArc(disc, 270, 180);                       // top, right side, bottom
                path.AddArc(term, 90, gibbous ? 180 : -180);       // back to the top through the left or right
            }
            else
            {
                path.AddArc(disc, 90, 180);                        // bottom, left side, top
                path.AddArc(term, 270, gibbous ? 180 : -180);      // back to the bottom through the right or left
            }
            path.CloseFigure();
            return path;
        }

        private static void DrawStar(Graphics g, float cx, float cy, float r, bool light)
        {
            var color = light ? Color.FromArgb(255, 120, 130, 160) : Color.FromArgb(255, 255, 250, 220);
            var points = new PointF[8];
            for (var i = 0; i < 8; i++)
            {
                var a = i * Math.PI / 4 - Math.PI / 2;
                var d = i % 2 == 0 ? r : r * 0.35f;
                points[i] = new PointF(cx + (float)Math.Cos(a) * d, cy + (float)Math.Sin(a) * d);
            }
            using (var brush = new SolidBrush(color))
                g.FillPolygon(brush, points);
        }

        // Four horizontal bands of fog.
        private static void DrawFog(Graphics g, bool light)
        {
            var color = light ? Color.FromArgb(255, 120, 130, 145) : Color.FromArgb(255, 214, 220, 228);
            var bands = new[]
            {
                new[] { 16f, 24f, 84f }, new[] { 10f, 42f, 74f }, new[] { 26f, 60f, 90f }, new[] { 12f, 78f, 80f }
            };
            using (var edge = new Pen(Edge(light, Color.FromArgb(255, 90, 100, 115)), 13f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var pen = new Pen(color, 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                foreach (var b in bands)
                {
                    if (!light)
                        g.DrawLine(edge, b[0], b[1], b[2], b[1]);
                    g.DrawLine(pen, b[0], b[1], b[2], b[1]);
                }
            }
        }

        // A red warning triangle with an exclamation mark, for severe storms.
        private static void DrawWarning(Graphics g, bool light, bool detailed)
        {
            var points = new[] { new PointF(73f, 46f), new PointF(98f, 94f), new PointF(48f, 94f) };
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(points);
                using (var edge = new Pen(light ? Color.FromArgb(255, 120, 20, 20) : Color.FromArgb(200, 0, 0, 0), 6f) { LineJoin = LineJoin.Round })
                    g.DrawPath(edge, path);
                using (var brush = new SolidBrush(Color.FromArgb(255, 229, 57, 53)))
                    g.FillPath(brush, path);
            }
            using (var mark = new Pen(Color.White, 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(mark, 73f, 62f, 73f, 77f);
            using (var dot = new SolidBrush(Color.White))
                g.FillEllipse(dot, 69.5f, 82f, 7f, 7f);
        }

        // The background of the hover panel: a soft sky color for the weather, light or dark.
        internal static void PaintPanelBackground(Graphics g, Rectangle area, WeatherKind kind, bool light)
        {
            Color top, bottom;
            switch (kind)
            {
                case WeatherKind.Sunny:
                    top = light ? Color.FromArgb(214, 236, 255) : Color.FromArgb(28, 58, 96);
                    bottom = light ? Color.FromArgb(255, 246, 222) : Color.FromArgb(46, 42, 36);
                    break;
                case WeatherKind.PartlyCloudy:
                    top = light ? Color.FromArgb(218, 234, 250) : Color.FromArgb(34, 52, 74);
                    bottom = light ? Color.FromArgb(244, 246, 249) : Color.FromArgb(43, 43, 43);
                    break;
                case WeatherKind.Cloudy:
                    top = light ? Color.FromArgb(226, 230, 236) : Color.FromArgb(52, 56, 62);
                    bottom = light ? Color.FromArgb(247, 248, 250) : Color.FromArgb(40, 40, 42);
                    break;
                case WeatherKind.LightRain:
                case WeatherKind.Rain:
                case WeatherKind.HeavyRain:
                    top = light ? Color.FromArgb(212, 222, 234) : Color.FromArgb(30, 42, 58);
                    bottom = light ? Color.FromArgb(240, 244, 248) : Color.FromArgb(36, 38, 42);
                    break;
                case WeatherKind.Thunderstorm:
                    top = light ? Color.FromArgb(222, 216, 238) : Color.FromArgb(40, 32, 64);
                    bottom = light ? Color.FromArgb(242, 240, 247) : Color.FromArgb(34, 34, 38);
                    break;
                case WeatherKind.LightSnow:
                case WeatherKind.HeavySnow:
                case WeatherKind.Ice:
                    top = light ? Color.FromArgb(226, 240, 255) : Color.FromArgb(34, 54, 78);
                    bottom = light ? Color.FromArgb(252, 253, 255) : Color.FromArgb(43, 45, 50);
                    break;
                case WeatherKind.Fog:
                    top = light ? Color.FromArgb(232, 234, 238) : Color.FromArgb(58, 60, 64);
                    bottom = light ? Color.FromArgb(248, 248, 249) : Color.FromArgb(42, 42, 44);
                    break;
                case WeatherKind.Severe:
                    top = light ? Color.FromArgb(244, 222, 222) : Color.FromArgb(70, 28, 32);
                    bottom = light ? Color.FromArgb(248, 244, 244) : Color.FromArgb(36, 32, 34);
                    break;
                case WeatherKind.ClearNight:
                case WeatherKind.PartlyCloudyNight:
                case WeatherKind.CloudyNight:
                    top = light ? Color.FromArgb(220, 224, 244) : Color.FromArgb(16, 24, 52);
                    bottom = light ? Color.FromArgb(244, 245, 252) : Color.FromArgb(34, 36, 48);
                    break;
                default:
                    top = bottom = light ? Color.FromArgb(249, 249, 249) : Color.FromArgb(43, 43, 43);
                    break;
            }
            using (var brush = new LinearGradientBrush(area, top, bottom, LinearGradientMode.Vertical))
                g.FillRectangle(brush, area);
        }
    }

    // A small panel shown above the tray icons while the mouse is over them. It replaces the
    // standard tooltip, which .NET Framework limits to 63 characters.
    internal sealed class HoverPanel : Form
    {
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x8;

        private string _title = string.Empty;
        private List<KeyValuePair<string, string>> _rows = new List<KeyValuePair<string, string>>();
        private string _footer = string.Empty;
        private WeatherKind _kind = WeatherKind.None;
        private Bitmap _art;
        private bool _light;

        // Dragging: hold the left mouse button on the panel and move it. After the drop the panel
        // opens at that place, until "Hover panel next to the icons" in the tray menu is chosen.
        private bool _dragging;
        private Point _dragStartCursor;
        private Point _dragStartLocation;

        /// <summary>The saved top-left corner in screen pixels, or null for "next to the icons".</summary>
        public Point? CustomLocation;

        /// <summary>Raised after a drag ends, with the new top-left corner.</summary>
        public event Action<Point> Moved;

        public bool IsDragging { get { return _dragging; } }

        public HoverPanel()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            Cursor = Cursors.SizeAll;   // shows that the panel can be dragged
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            _dragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartLocation = Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging)
                return;
            var p = Cursor.Position;
            Location = new Point(_dragStartLocation.X + p.X - _dragStartCursor.X, _dragStartLocation.Y + p.Y - _dragStartCursor.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_dragging || e.Button != MouseButtons.Left)
                return;
            _dragging = false;
            Capture = false;
            // A click without a real move does not save a place.
            var p = Cursor.Position;
            var drag = SystemInformation.DragSize;
            if (Math.Abs(p.X - _dragStartCursor.X) < drag.Width && Math.Abs(p.Y - _dragStartCursor.Y) < drag.Height)
            {
                Location = _dragStartLocation;
                return;
            }
            Location = KeepOnScreen(Location);
            CustomLocation = Location;
            var handler = Moved;
            if (handler != null)
                handler(Location);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            _dragging = false;   // for example when another window takes the mouse
        }

        // Moves the panel fully into the working area of the screen it is mostly on. This also
        // brings back a saved place after a monitor was removed or the resolution changed.
        private Point KeepOnScreen(Point topLeft)
        {
            var center = new Point(topLeft.X + Width / 2, topLeft.Y + Height / 2);
            var area = Screen.FromPoint(center).WorkingArea;
            var x = Math.Max(area.Left, Math.Min(topLeft.X, area.Right - Width));
            var y = Math.Max(area.Top, Math.Min(topLeft.Y, area.Bottom - Height));
            return new Point(x, y);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; } // never take focus from the window you are working in
        }

        private const int CS_DROPSHADOW = 0x00020000;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        // The panel colors: a gradient from very dark blue at the top to dark blue at the bottom.
        // White text has a contrast of about 18:1 at the top and 12:1 at the bottom; labels about 7:1.
        // It is painted over the full height, so it stretches with the number of details shown.
        private static readonly Color SkyTop = Color.FromArgb(255, 4, 12, 32);
        private static readonly Color SkyBottom = Color.FromArgb(255, 22, 52, 110);
        private static readonly Color TextColor = Color.FromArgb(255, 248, 250, 255);
        private static readonly Color LabelColor = Color.FromArgb(255, 176, 200, 236);
        private static readonly Color BorderColor = Color.FromArgb(255, 48, 80, 140);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
                cp.ClassStyle |= CS_DROPSHADOW;   // a soft shadow under the panel
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                // Rounded corners like other Windows 11 popups. Windows 10 ignores this.
                var round = DWMWCP_ROUND;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
            }
            catch (Exception)
            {
                // dwmapi.dll is part of every supported Windows. The corners are cosmetic.
            }
        }

        public void SetContent(string title, List<KeyValuePair<string, string>> rows, string footer, WeatherKind kind, bool lightTheme, double latitude)
        {
            _title = title ?? string.Empty;
            _rows = rows ?? new List<KeyValuePair<string, string>>();
            _footer = footer ?? string.Empty;
            if (_art != null)
            {
                _art.Dispose();
                _art = null;
            }
            _kind = kind;
            _light = lightTheme;
            if (_kind != WeatherKind.None)
            {
                // The panel is always dark blue, so the picture uses its colors for a dark background.
                try { _art = WeatherArt.Render(_kind, PictureSize, false, latitude); }
                catch (Exception) { _art = null; }
            }
            BackColor = SkyTop;
            ResizeToContent();
            Invalidate();
        }

        private Font TitleFont { get { return new Font("Segoe UI", 10f, FontStyle.Bold); } }
        private Font BodyFont { get { return new Font("Segoe UI", 9f); } }

        private int Scale(int value)
        {
            using (var g = CreateGraphics())
                return (int)Math.Round(value * g.DpiX / 96f);
        }

        // The weather picture at the right side of the panel.
        private int PictureSize { get { return Scale(72); } }

        private int PictureColumn { get { return _art == null ? 0 : PictureSize + Scale(12); } }

        // Text is measured and drawn without the extra space Windows adds around each piece of text,
        // so the panel fits its content closely.
        private const TextFormatFlags TextFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

        // Layout values from ResizeToContent, used by OnPaint.
        private int _labelWidth;
        private int _lineHeight;
        private int _titleHeight;
        private int _footerHeight;

        private static int TextWidth(Graphics g, string text, Font font)
        {
            return string.IsNullOrEmpty(text) ? 0 : TextRenderer.MeasureText(g, text, font, Size.Empty, TextFlags).Width;
        }

        // Height of the text wrapped at word boundaries to the given width.
        private static int WrappedHeight(Graphics g, string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            return TextRenderer.MeasureText(g, text, font, new Size(Math.Max(1, width), int.MaxValue),
                TextFlags | TextFormatFlags.WordBreak).Height;
        }

        // The panel has one fixed width. A long location name, a long detail and the bottom line wrap
        // onto more lines. Only the height changes with the content.
        private int PanelWidth { get { return Scale(340); } }

        // Height of each detail line from ResizeToContent, used by OnPaint.
        private readonly List<int> _rowHeights = new List<int>();

        private void ResizeToContent()
        {
            using (var g = CreateGraphics())
            using (var title = TitleFont)
            using (var body = BodyFont)
            {
                var pad = Scale(11);
                var gap = Scale(14);
                var picture = PictureColumn;
                var width = PanelWidth;
                _lineHeight = TextRenderer.MeasureText(g, "Ag", body, Size.Empty, TextFlags).Height + Scale(3);

                _labelWidth = 0;
                foreach (var row in _rows)
                    _labelWidth = Math.Max(_labelWidth, TextWidth(g, row.Key, body));
                // Keep at least half of the width for the values.
                _labelWidth = Math.Min(_labelWidth, (width - 2 * pad) / 2);

                _titleHeight = WrappedHeight(g, _title, title, width - pad - picture - pad);
                var y = pad + _titleHeight + Scale(7);

                _rowHeights.Clear();
                foreach (var row in _rows)
                {
                    var right = width - pad - (BesidePicture(y) ? picture : 0);
                    var valueWidth = right - (pad + _labelWidth + gap);
                    var h = Math.Max(_lineHeight, WrappedHeight(g, row.Value, body, valueWidth) + Scale(3));
                    _rowHeights.Add(h);
                    y += h;
                }

                _footerHeight = 0;
                if (_footer.Length > 0)
                {
                    var footerTop = y + Scale(6);
                    _footerHeight = WrappedHeight(g, _footer, body, width - pad - (BesidePicture(footerTop) ? picture : 0) - pad);
                    y = footerTop + _footerHeight;
                }
                var height = Math.Max(y, _art == null ? 0 : pad + PictureSize) + pad;
                Size = new Size(width, height);
            }
        }

        // True when a line that starts at this height sits beside the weather picture.
        private bool BesidePicture(int lineTop)
        {
            return _art != null && lineTop < Scale(11) + PictureSize + Scale(3);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The whole panel is painted in OnPaint.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var fore = TextColor;
            var dim = LabelColor;

            // The sky: very dark blue at the top to dark blue at the bottom, over the full panel height.
            var area = new Rectangle(0, 0, Width, Height);
            using (var sky = new LinearGradientBrush(area, SkyTop, SkyBottom, LinearGradientMode.Vertical))
                g.FillRectangle(sky, area);

            // A faint highlight along the top edge, like light on glass.
            using (var shine = new LinearGradientBrush(new Rectangle(0, 0, Width, Scale(28) + 1),
                       Color.FromArgb(34, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
                g.FillRectangle(shine, 0, 0, Width, Scale(28));

            using (var pen = new Pen(BorderColor))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            var pad = Scale(11);
            var textRight = Width - pad - PictureColumn;
            if (_art != null)
            {
                var picture = new Rectangle(Width - pad - PictureSize, pad, PictureSize, PictureSize);

                // A soft glow behind the weather picture.
                var glow = Rectangle.Inflate(picture, Scale(8), Scale(8));
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(glow);
                    using (var brush = new PathGradientBrush(path))
                    {
                        brush.CenterColor = Color.FromArgb(70, 140, 190, 255);
                        brush.SurroundColors = new[] { Color.FromArgb(0, 140, 190, 255) };
                        g.FillEllipse(brush, glow);
                    }
                }

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_art, picture);
            }

            using (var title = TitleFont)
            using (var body = BodyFont)
            {
                var y = pad;
                var gap = Scale(14);
                TextRenderer.DrawText(g, _title, title, new Rectangle(pad, y, textRight - pad, _titleHeight), fore,
                    TextFlags | TextFormatFlags.WordBreak);
                y += _titleHeight + Scale(7);

                for (var i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    var h = i < _rowHeights.Count ? _rowHeights[i] : _lineHeight;
                    var right = BesidePicture(y) ? textRight : Width - pad;
                    TextRenderer.DrawText(g, row.Key, body, new Rectangle(pad, y, _labelWidth + Scale(2), _lineHeight), dim,
                        TextFlags | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(g, row.Value, body, new Rectangle(pad + _labelWidth + gap, y, right - pad - _labelWidth - gap, h),
                        fore, TextFlags | TextFormatFlags.WordBreak);
                    y += h;
                }
                if (_footer.Length > 0)
                {
                    var footerTop = y + Scale(6);
                    var right = BesidePicture(footerTop) ? textRight : Width - pad;
                    TextRenderer.DrawText(g, _footer, body, new Rectangle(pad, footerTop, right - pad, _footerHeight), dim,
                        TextFlags | TextFormatFlags.WordBreak);
                }
            }
        }

        // Places the panel next to the notification area, on whichever side the taskbar is.
        public void ShowNear(Point cursor)
        {
            if (CustomLocation.HasValue)
            {
                Location = KeepOnScreen(CustomLocation.Value);
                if (!Visible) Show();
                return;
            }

            var area = Screen.FromPoint(cursor).WorkingArea;
            var gap = Scale(8);
            int x = cursor.X - Width / 2;
            int y;
            if (cursor.Y >= area.Bottom) y = area.Bottom - Height - gap;        // taskbar at the bottom
            else if (cursor.Y < area.Top) y = area.Top + gap;                    // taskbar at the top
            else y = cursor.Y - Height - Scale(24);                              // icons in the overflow flyout
            if (cursor.X >= area.Right) x = area.Right - Width - gap;           // taskbar on the right
            else if (cursor.X < area.Left) x = area.Left + gap;                  // taskbar on the left
            x = Math.Max(area.Left + gap, Math.Min(x, area.Right - Width - gap));
            y = Math.Max(area.Top + gap, Math.Min(y, area.Bottom - Height - gap));
            Location = new Point(x, y);
            if (!Visible) Show();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _art != null) _art.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class TrayContext : ApplicationContext
    {
        private static readonly string Protocol = TrayHost.Protocol;
        private static readonly string AppTitle = TrayHost.AppTitle;
        private const string Degree = "\u00B0";
        private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(3);

        private readonly string _workDir;
        private readonly NotifyIcon _pictureIcon;
        private readonly NotifyIcon _numberIcon;
        private readonly ContextMenuStrip _menu;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly System.Windows.Forms.Timer _changeDelay;
        private readonly Control _uiInvoker;
        private FileSystemWatcher _watcher;
        private string _watchedFolder;

        private Icon _pictureImage;
        private Icon _numberImage;
        private string _lastRenderKey = string.Empty;
        private bool _showFeelsLike;
        private List<string> _hoverDetails = new List<string>(DetailCatalog.Defaults);
        private readonly HoverPanel _hover = new HoverPanel();
        private readonly System.Windows.Forms.Timer _hoverShow = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _hoverWatch = new System.Windows.Forms.Timer();
        private Point _hoverPoint;
        private DateTime _hoverLeftUtc = DateTime.MinValue;   // when the pointer left the icons and the panel
        private static readonly TimeSpan HoverGrace = TimeSpan.FromMilliseconds(900);
        private static readonly TimeSpan HoverOpenDelay = TimeSpan.FromMilliseconds(1200);
        private DateTime _hoverStartUtc = DateTime.MinValue;                 // when the pointer came onto the icons
        private Point? _hoverLocation;                        // saved panel place, null = next to the icons
        private DateTime _keepMenuOpenUntil = DateTime.MinValue;
        private string _trayCity = AppCity;   // "app" = same city as the app, otherwise a saved placeId
        private const string AppCity = "app";

        // 5-minute updates while the app window is closed or minimized.
        private static readonly TimeSpan QuickRefreshInterval = TimeSpan.FromMinutes(5);
        private readonly System.Windows.Forms.Timer _quickRefreshTimer = new System.Windows.Forms.Timer();
        private bool _quickRefresh = true;
        private bool _quickRefreshRunning;
        private DateTime _lastQuickRefreshUtc = DateTime.MinValue;

        // Left-click toggle. A click on the notification area makes the taskbar the active window,
        // so the active window is checked a few times a second and remembered before the click.
        private readonly System.Windows.Forms.Timer _foregroundWatch = new System.Windows.Forms.Timer();
        private IntPtr _lastForeground = IntPtr.Zero;
        private bool _appWasActive;

        public TrayContext()
        {
            _workDir = TrayHost.WorkDir;
            LoadSettings();

            _menu = new ContextMenuStrip();
            _menu.Opening += OnMenuOpening;

            // Created in this order so the temperature usually sits to the left of the weather picture.
            // Windows places the icons of one program in the order they are created.
            // You can drag the icons on the taskbar to change their order.
            _numberIcon = CreateIcon();
            _pictureIcon = CreateIcon();

            InitHover();
            BuildMenu();
            Refresh(true);

            // A hidden control so file change events arrive on this (the UI) thread.
            _uiInvoker = new Control();
            _uiInvoker.CreateControl();

            // When the app writes a new reading, wait a quarter second for the write to finish, then redraw.
            _changeDelay = new System.Windows.Forms.Timer();
            _changeDelay.Interval = 250;
            _changeDelay.Tick += delegate { _changeDelay.Stop(); Refresh(false); };

            // Fallback check every 5 seconds, in case a change event is missed.
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 5000;
            _timer.Tick += delegate
            {
#if PACKAGED
                // The user turned off "Tray icons" in the app.
                if (!TrayHost.IsEnabled())
                {
                    ExitThread();
                    return;
                }
#endif
                EnsureWatcher();
                Refresh(false);
            };
            _timer.Start();
            EnsureWatcher();

            _foregroundWatch.Interval = 250;
            _foregroundWatch.Tick += delegate { WatchForeground(); };
            _foregroundWatch.Start();

            if (AppBridge.Available)
            {
                _quickRefreshTimer.Interval = 60 * 1000;   // check once a minute
                _quickRefreshTimer.Tick += delegate { MaybeQuickRefresh(); };
                _quickRefreshTimer.Start();
            }
        }

        // Asks the app for new weather data when the window is closed or minimized and the data
        // is at least 5 minutes old. With the window open, the app's own "Auto refresh" applies.
        private async void MaybeQuickRefresh()
        {
            if (!_quickRefresh || _quickRefreshRunning)
                return;
            if (DateTime.UtcNow - _lastQuickRefreshUtc < QuickRefreshInterval)
                return;

            var window = AppWindow.Find();
            if (window != IntPtr.Zero && !AppWindow.IsMinimized(window))
                return;

            var current = ReadWeather();
            if (current != null && DateTime.UtcNow - current.WrittenUtc < QuickRefreshInterval - TimeSpan.FromSeconds(30))
                return;   // fresh enough, for example after the app's background task

            var pfn = GetPackageFamilyName();
            if (pfn == null)
                return;

            _quickRefreshRunning = true;
            _lastQuickRefreshUtc = DateTime.UtcNow;
            try
            {
                await AppBridge.RequestRefreshAsync(pfn);
            }
            catch (Exception)
            {
                // Tried again in 5 minutes. The 15-minute background task still runs.
            }
            finally
            {
                _quickRefreshRunning = false;
            }
        }

        private void WatchForeground()
        {
            try
            {
                var foreground = AppWindow.GetForeground();
                if (foreground == _lastForeground)
                    return;
                _lastForeground = foreground;

                // The taskbar, its hidden icons area and this program's own menu do not count.
                // They become active when the tray icons are clicked.
                if (foreground == IntPtr.Zero || AppWindow.IsTaskbarOrOwn(foreground))
                    return;

                _appWasActive = AppWindow.IsAppFrame(foreground);
            }
            catch (Exception)
            {
                // Checked again at the next tick.
            }
        }

        private NotifyIcon CreateIcon()
        {
            var icon = new NotifyIcon();
            icon.Text = string.Empty;   // the hover panel replaces the 63-character tooltip
            icon.Icon = SystemIcons.Application;
            icon.ContextMenuStrip = _menu;
            icon.MouseClick += OnIconClick;
            icon.MouseMove += OnIconMouseMove;
            icon.Visible = true;
            return icon;
        }

        // ------------------------------------------------------------------
        // Display
        // ------------------------------------------------------------------

        // The reading for the city the tray shows: the app's city, or a saved city chosen in the tray menu.
        private WeatherReading GetDisplayReading(out string waitingFor)
        {
            waitingFor = null;
            if (_trayCity == AppCity)
                return ReadWeather();

            WeatherReading r;
            if (ReadAllCities().TryGetValue(_trayCity, out r))
                return r;

            var saved = ReadSavedLocations().Find(x => x.PlaceId == _trayCity);
            if (saved == null)
            {
                // The city was removed from the saved list. Go back to the app's city.
                _trayCity = AppCity;
                SaveSettings();
                return ReadWeather();
            }
            waitingFor = saved.Name;
            return null;
        }

        private void Refresh(bool force)
        {
            string waitingFor;
            var reading = GetDisplayReading(out waitingFor);
            var size = NativeMethods.GetSmallIconSize();
            var light = TaskbarIsLight();

            var moonStep = (int)(WeatherArt.MoonPhase(DateTime.UtcNow) * 50);   // redraw the moon as the phase changes
            var key = _trayCity + "|" + moonStep + "|" + string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                reading == null ? "none" : reading.WrittenUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                reading != null && IsStale(reading), _showFeelsLike, size, light,
                reading == null ? 0 : reading.Temp, reading == null ? 0 : reading.Feels,
                reading == null ? -1 : reading.IconCode);
            if (!force && key == _lastRenderKey)
                return;
            _lastRenderKey = key;

            Icon newPicture;
            Icon newNumber;
            string tip;

            if (reading == null || IsStale(reading))
            {
                newPicture = LoadAppIcon(size);
                newNumber = RenderNumber("--", size, light);
                tip = waitingFor != null
                    ? waitingFor + ": waiting for the next data refresh"
                    : reading == null
                        ? "FluentWeather: no data yet. Open the app once."
                        : "FluentWeather: no update since " + reading.WrittenUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
            }
            else
            {
                newPicture = LoadWeatherIcon(reading.IconCode, reading.Phrase, reading.IsNight, reading.Latitude, size, light) ?? LoadAppIcon(size);
                var value = _showFeelsLike ? reading.Feels : reading.Temp;
                newNumber = RenderNumber(value.ToString(CultureInfo.InvariantCulture), size, light);
                tip = BuildTooltip(reading);
            }

            SetIcon(_pictureIcon, ref _pictureImage, newPicture);
            SetIcon(_numberIcon, ref _numberImage, newNumber);
            _lastTip = tip;
            _lastReading = reading;
            _lastWaitingFor = waitingFor;
            if (_hover.Visible)
                UpdateHoverContent();
        }

        private string _lastTip = string.Empty;
        private WeatherReading _lastReading;
        private string _lastWaitingFor;

        // ------------------------------------------------------------------
        // Hover panel
        // ------------------------------------------------------------------

        private void InitHover()
        {
            // The panel opens only after the pointer has stayed on the icons for the whole 1.2 seconds.
            // The pointer is checked 10 times a second. If it leaves the icons earlier, nothing opens,
            // and the next visit starts a new 1.2 seconds.
            _hoverShow.Interval = 100;
            _hoverShow.Tick += delegate
            {
                var p = Cursor.Position;
                var limit = NativeMethods.GetSmallIconSize();
                if (_menu.Visible || Math.Abs(p.X - _hoverPoint.X) > limit || Math.Abs(p.Y - _hoverPoint.Y) > limit)
                {
                    _hoverShow.Stop();   // left the icons before the time was up
                    return;
                }
                if (DateTime.UtcNow - _hoverStartUtc < HoverOpenDelay)
                    return;
                _hoverShow.Stop();
                UpdateHoverContent();
                _hover.ShowNear(_hoverPoint);
                _hoverWatch.Start();
            };
            _hoverWatch.Interval = 150;
            _hoverWatch.Tick += delegate
            {
                // The panel stays open while the pointer is on the icons or on the panel, and while
                // it is dragged. After the pointer leaves both, it stays 0.9 seconds more, so the
                // pointer can move from the icons to a panel at a saved place to drag it again.
                if (_menu.Visible)
                {
                    HideHover();
                    return;
                }
                var p = Cursor.Position;
                var limit = NativeMethods.GetSmallIconSize() * 2;
                var onIcons = Math.Abs(p.X - _hoverPoint.X) <= limit && Math.Abs(p.Y - _hoverPoint.Y) <= limit;
                if (onIcons || _hover.IsDragging || _hover.Bounds.Contains(p))
                {
                    _hoverLeftUtc = DateTime.MinValue;
                    return;
                }
                if (_hoverLeftUtc == DateTime.MinValue)
                    _hoverLeftUtc = DateTime.UtcNow;
                else if (DateTime.UtcNow - _hoverLeftUtc > HoverGrace)
                    HideHover();
            };
            _hover.CustomLocation = _hoverLocation;
            _hover.Moved += delegate (Point location)
            {
                _hoverLocation = location;
                SaveSettings();
            };
            _menu.Opening += delegate { HideHover(); };
            // Keep the menu open while ticking hover details, so several can be changed at once.
            _menu.Closing += (sender, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && DateTime.UtcNow < _keepMenuOpenUntil)
                    e.Cancel = true;
            };
        }

        private void OnIconMouseMove(object sender, MouseEventArgs e)
        {
            _hoverPoint = Cursor.Position;
            if (!_hover.Visible && !_hoverShow.Enabled)
            {
                _hoverStartUtc = DateTime.UtcNow;
                _hoverShow.Start();
            }
        }

        private void HideHover()
        {
            _hoverShow.Stop();
            _hoverWatch.Stop();
            _hoverLeftUtc = DateTime.MinValue;
            if (_hover.Visible) _hover.Hide();
        }

        private void UpdateHoverContent()
        {
            var light = TaskbarIsLight();
            var r = _lastReading;
            if (r == null || IsStale(r))
            {
                var rows0 = new List<KeyValuePair<string, string>>();
                _hover.SetContent(_lastWaitingFor ?? "FluentWeather", rows0, _lastTip, WeatherKind.None, light, 45);
                return;
            }

            var rows = new List<KeyValuePair<string, string>>();
            foreach (var entry in DetailCatalog.All)
            {
                if (!_hoverDetails.Contains(entry[0]))
                    continue;
                string value;
                if (entry[0] == "Moon")
                {
                    var phase = WeatherArt.MoonPhase(DateTime.UtcNow);
                    var lit = (int)Math.Round(100 * (1 - Math.Cos(2 * Math.PI * phase)) / 2);
                    rows.Add(new KeyValuePair<string, string>(entry[1], WeatherArt.MoonPhaseName(phase) + ", " + lit + "% lit"));
                }
                else if (r.Details.TryGetValue(entry[0], out value))
                {
                    rows.Add(new KeyValuePair<string, string>(entry[1], value));
                }
            }
            if (rows.Count == 0)
                rows.Add(new KeyValuePair<string, string>("Temperature", r.Temp + Degree + r.Unit + ", feels like " + r.Feels + Degree + r.Unit));

            var title = string.IsNullOrEmpty(r.FullPlace) ? r.Place : r.FullPlace;
            var footer = (_trayCity == AppCity ? "Same city as the app. " : "Chosen in the tray. ") +
                         "Updated " + r.WrittenUtc.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
            var kind = WeatherArt.FromReading(r.IconCode, r.Phrase, r.IsNight);
            _hover.SetContent(title, rows, footer, kind, light, r.Latitude);
        }

        private string BuildTooltip(WeatherReading r)
        {
            // Tooltips in .NET Framework are limited to 63 characters.
            var unit = Degree + r.Unit;
            var line1 = r.Place + ": " + r.Temp + unit + ", feels like " + r.Feels + unit;
            var line2 = r.Phrase + ", " + r.ObservedLocal.ToString("t", CultureInfo.CurrentCulture);
            return line1 + "\n" + line2;
        }

        private static string Fit(string text)
        {
            return text.Length <= 63 ? text : text.Substring(0, 62) + "\u2026";
        }

        private static bool IsStale(WeatherReading r)
        {
            return DateTime.UtcNow - r.WrittenUtc > StaleAfter;
        }

        private static void SetIcon(NotifyIcon target, ref Icon current, Icon replacement)
        {
            var old = current;
            target.Icon = replacement;
            current = replacement;
            if (old != null && !ReferenceEquals(old, SystemIcons.Application))
                old.Dispose();
        }

        // Draws the number as large as the icon allows. Light taskbar: dark text. Dark taskbar: white text.
        private static Icon RenderNumber(string value, int size, bool lightTaskbar)
        {
            // Add the degree sign when it fits. Three-character numbers (-12, 104) are drawn without it.
            var text = value.Length <= 2 && value != "--" ? value + Degree : value;

            using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bitmap))
            using (var path = new GraphicsPath())
            using (var family = new FontFamily("Segoe UI"))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                path.AddString(text, family, (int)FontStyle.Bold, 100f, PointF.Empty, StringFormat.GenericTypographic);
                var bounds = path.GetBounds();
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    var margin = Math.Max(1f, size / 16f);
                    // Fit the width, and cap the height at 72% of the icon so short numbers ("-5")
                    // are not drawn much taller than longer ones ("41" plus the degree sign).
                    var scale = Math.Min((size - 2 * margin) / bounds.Width, (size * 0.72f) / bounds.Height);
                    using (var m = new Matrix())
                    {
                        m.Translate(size / 2f, size / 2f);
                        m.Scale(scale, scale);
                        m.Translate(-(bounds.X + bounds.Width / 2f), -(bounds.Y + bounds.Height / 2f));
                        path.Transform(m);
                    }

                    var fill = lightTaskbar ? Color.FromArgb(255, 20, 20, 20) : Color.White;
                    var edge = lightTaskbar ? Color.FromArgb(150, 255, 255, 255) : Color.FromArgb(170, 0, 0, 0);
                    using (var pen = new Pen(edge, Math.Max(1f, size / 12f)))
                    using (var brush = new SolidBrush(fill))
                    {
                        pen.LineJoin = LineJoin.Round;
                        g.DrawPath(pen, path);
                        g.FillPath(brush, path);
                    }
                }
                return ToIcon(bitmap);
            }
        }

        // One of the eight weather pictures, drawn for the taskbar color. Null for an unknown code.
        private static Icon LoadWeatherIcon(int iconCode, string phrase, bool night, double latitude, int size, bool lightTaskbar)
        {
            var kind = WeatherArt.FromReading(iconCode, phrase, night);
            if (kind == WeatherKind.None)
                return null;
            try
            {
                using (var bitmap = WeatherArt.Render(kind, size, lightTaskbar, latitude))
                    return ToIcon(bitmap);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Icon LoadAppIcon(int size)
        {
            return LoadScaledPng(Path.Combine(_workDir, "tray-icon.png"), size) ?? SystemIcons.Application;
        }

        private static Icon LoadScaledPng(string path, int size)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                using (var source = new Bitmap(path))
                using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    g.DrawImage(source, new Rectangle(0, 0, size, size));
                    return ToIcon(bitmap);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Icon ToIcon(Bitmap bitmap)
        {
            // Icon.FromHandle does not own the handle. Clone into a managed icon, then free the handle.
            var handle = bitmap.GetHicon();
            try
            {
                using (var temp = Icon.FromHandle(handle))
                {
                    return (Icon)temp.Clone();
                }
            }
            finally
            {
                NativeMethods.DestroyIcon(handle);
            }
        }

        private static bool TaskbarIsLight()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key == null ? null : key.GetValue("SystemUsesLightTheme");
                    return value is int && (int)value == 1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Data files
        // ------------------------------------------------------------------

        private string GetPackageFamilyName()
        {
#if PACKAGED
            return TrayHost.PackageFamilyName;
#else
            try
            {
                // The update script writes the package family name here after each install.
                var pfnFile = Path.Combine(_workDir, "package-family-name.txt");
                if (!File.Exists(pfnFile))
                    return null;
                var pfn = File.ReadAllText(pfnFile).Trim();
                if (pfn.Length == 0 || pfn.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return null;
                return pfn;
            }
            catch (Exception)
            {
                return null;
            }
#endif
        }

        private string GetLocalStatePath(string fileName)
        {
#if PACKAGED
            return Path.Combine(TrayHost.WorkDir, fileName);
#else
            var pfn = GetPackageFamilyName();
            if (pfn == null)
                return null;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", pfn, "LocalState", fileName);
#endif
        }

        // Watches the app's LocalState folder, so a new reading (for example after a city change)
        // shows in the tray right away instead of at the next timed check.
        private void EnsureWatcher()
        {
            var file = GetLocalStatePath("tray-weather.txt");
            var folder = file == null ? null : Path.GetDirectoryName(file);
            if (folder == null || !Directory.Exists(folder))
                return;
            if (_watcher != null && string.Equals(folder, _watchedFolder, StringComparison.OrdinalIgnoreCase))
                return;

            if (_watcher != null)
                _watcher.Dispose();
            try
            {
                _watcher = new FileSystemWatcher(folder, "tray-weather*");
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
                _watcher.SynchronizingObject = _uiInvoker;
                FileSystemEventHandler changed = delegate { _changeDelay.Stop(); _changeDelay.Start(); };
                _watcher.Changed += changed;
                _watcher.Created += changed;
                _watcher.Renamed += delegate { _changeDelay.Stop(); _changeDelay.Start(); };
                _watcher.EnableRaisingEvents = true;
                _watchedFolder = folder;
            }
            catch (Exception)
            {
                _watcher = null; // the 5 second check still works
            }
        }

        // Reads all lines with full sharing, so a read never stops the app from replacing the file.
        private static List<string> ReadSharedLines(string path)
        {
            var lines = new List<string>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string l;
                while ((l = reader.ReadLine()) != null)
                    lines.Add(l);
            }
            return lines;
        }

        private static WeatherReading ParseReading(IEnumerable<string> lines)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var eq = line.IndexOf('=');
                if (eq > 0)
                    values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }

            var r = new WeatherReading();
            if (!TryInt(values, "temp", out r.Temp) || !TryInt(values, "feels", out r.Feels))
                return null;
            int icon;
            r.IconCode = TryInt(values, "icon", out icon) ? icon : -1;
            r.IsNight = string.Equals(Get(values, "daynight"), "N", StringComparison.OrdinalIgnoreCase);
            double latitude;
            if (double.TryParse(Get(values, "lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude))
                r.Latitude = latitude;
            r.Place = Get(values, "place");
            r.FullPlace = Get(values, "fullplace");
            r.Unit = Get(values, "unit");
            r.Phrase = Get(values, "phrase");
            foreach (var kv in values)
            {
                if (kv.Key.StartsWith("d.", StringComparison.OrdinalIgnoreCase) && kv.Key.Length > 2)
                    r.Details[kv.Key.Substring(2)] = kv.Value;
            }

            DateTimeOffset written, observed;
            if (!DateTimeOffset.TryParse(Get(values, "written"), CultureInfo.InvariantCulture, DateTimeStyles.None, out written))
                return null;
            r.WrittenUtc = written.UtcDateTime;
            r.ObservedLocal = DateTimeOffset.TryParse(Get(values, "observed"), CultureInfo.InvariantCulture, DateTimeStyles.None, out observed)
                ? observed.LocalDateTime
                : written.LocalDateTime;
            return r;
        }

        // The city shown in the app (tray-weather.txt).
        private WeatherReading ReadWeather()
        {
            try
            {
                var path = GetLocalStatePath("tray-weather.txt");
                if (path == null || !File.Exists(path))
                    return null;
                return ParseReading(ReadSharedLines(path));
            }
            catch (Exception)
            {
                return null; // file being replaced at this moment; the next check will read it
            }
        }

        // Every saved city (tray-weather-all.txt). Each block starts with a "[placeId]" line.
        private Dictionary<string, WeatherReading> ReadAllCities()
        {
            var result = new Dictionary<string, WeatherReading>(StringComparer.Ordinal);
            try
            {
                var path = GetLocalStatePath("tray-weather-all.txt");
                if (path == null || !File.Exists(path))
                    return result;

                string id = null;
                var block = new List<string>();
                foreach (var line in ReadSharedLines(path))
                {
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (id != null) AddReading(result, id, block);
                        id = line.Substring(1, line.Length - 2);
                        block = new List<string>();
                    }
                    else if (id != null)
                    {
                        block.Add(line);
                    }
                }
                if (id != null) AddReading(result, id, block);
            }
            catch (Exception)
            {
                // Partial or missing file: show what could be read.
            }
            return result;
        }

        private static void AddReading(Dictionary<string, WeatherReading> target, string id, List<string> block)
        {
            var r = ParseReading(block);
            if (r != null && id.Length > 0)
                target[id] = r;
        }

        private static string Get(Dictionary<string, string> values, string key)
        {
            string v;
            return values.TryGetValue(key, out v) ? v : string.Empty;
        }

        private static bool TryInt(Dictionary<string, string> values, string key, out int result)
        {
            return int.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private string SettingsPath
        {
            get { return Path.Combine(_workDir, "tray-settings.txt"); }
        }

        // tray-settings.txt: "show=temp" or "show=feels", and "city=app" or "city=<placeId>".
        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;
                var version = 1;
                var hadDetails = false;
                foreach (var line in File.ReadAllLines(SettingsPath))
                {
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var k = line.Substring(0, eq).Trim();
                    var v = line.Substring(eq + 1).Trim();
                    if (k.Equals("show", StringComparison.OrdinalIgnoreCase))
                        _showFeelsLike = v.Equals("feels", StringComparison.OrdinalIgnoreCase);
                    else if (k.Equals("details", StringComparison.OrdinalIgnoreCase))
                    {
                        _hoverDetails = new List<string>(v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        hadDetails = true;
                    }
                    else if (k.Equals("detailsversion", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out version);
                    else if (k.Equals("city", StringComparison.OrdinalIgnoreCase) && v.Length > 0)
                        _trayCity = v;
                    else if (k.Equals("quickrefresh", StringComparison.OrdinalIgnoreCase))
                        _quickRefresh = !v.Equals("off", StringComparison.OrdinalIgnoreCase);
                    else if (k.Equals("hoverpos", StringComparison.OrdinalIgnoreCase))
                    {
                        // "hoverpos=x,y" in screen pixels, or "hoverpos=auto" for next to the icons.
                        var parts = v.Split(',');
                        int px, py;
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out px) &&
                            int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out py))
                            _hoverLocation = new Point(px, py);
                    }
                }
                // Version 2 added "Moon phase" to the defaults. Before, the moon line showed by itself at night.
                if (hadDetails && version < 2 && !_hoverDetails.Contains("Moon"))
                    _hoverDetails.Add("Moon");
            }
            catch (Exception)
            {
                // Defaults: temperature, same city as the app.
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(_workDir);
                File.WriteAllText(SettingsPath,
                    "show=" + (_showFeelsLike ? "feels" : "temp") + "\r\n" +
                    "city=" + _trayCity + "\r\n" +
                    "details=" + string.Join(",", _hoverDetails) + "\r\n" +
                    "detailsversion=" + DetailCatalog.Version.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                    "quickrefresh=" + (_quickRefresh ? "on" : "off") + "\r\n" +
                    "hoverpos=" + (_hoverLocation.HasValue
                        ? _hoverLocation.Value.X.ToString(CultureInfo.InvariantCulture) + "," + _hoverLocation.Value.Y.ToString(CultureInfo.InvariantCulture)
                        : "auto") + "\r\n");
            }
            catch (Exception)
            {
                // The choice still applies until the tray helper restarts.
            }
        }

        private void SaveShowFeelsLike(bool feels)
        {
            _showFeelsLike = feels;
            SaveSettings();
            Refresh(true);
        }

        // Changes the city shown in the tray. Does not open or change the app.
        private void SetTrayCity(string city)
        {
            _trayCity = city;
            SaveSettings();
            Refresh(true);
        }

        // Opens the app. If the tray shows a different saved city, the app opens at that city.
        private void OpenApp()
        {
            if (_trayCity == AppCity)
                Launch(Protocol);
            else
                Launch(Protocol + "switch?placeId=" + Uri.EscapeDataString(_trayCity));
        }

        // ------------------------------------------------------------------
        // Menu
        // ------------------------------------------------------------------

        private void OnMenuOpening(object sender, CancelEventArgs e)
        {
            BuildMenu(); // re-read the saved list each time, so new locations show up
        }

        private void BuildMenu()
        {
            _menu.Items.Clear();

            var open = new ToolStripMenuItem("Open " + AppTitle);
            open.Font = new Font(open.Font, FontStyle.Bold);
            open.Click += delegate { OpenApp(); };
            _menu.Items.Add(open);
            _menu.Items.Add(new ToolStripSeparator());

            var showTemp = new ToolStripMenuItem("Number shows temperature");
            showTemp.Checked = !_showFeelsLike;
            showTemp.Click += delegate { SaveShowFeelsLike(false); };
            _menu.Items.Add(showTemp);

            var showFeels = new ToolStripMenuItem("Number shows feels-like temperature");
            showFeels.Checked = _showFeelsLike;
            showFeels.Click += delegate { SaveShowFeelsLike(true); };
            _menu.Items.Add(showFeels);

            // Hover details: tick the lines to show. The submenu stays open while you tick several.
            var details = new ToolStripMenuItem("Hover details");
            foreach (var entry in DetailCatalog.All)
            {
                var key = entry[0];
                var item = new ToolStripMenuItem(entry[1]);
                item.Checked = _hoverDetails.Contains(key);
                item.Click += delegate
                {
                    _keepMenuOpenUntil = DateTime.UtcNow.AddMilliseconds(500);
                    if (_hoverDetails.Contains(key)) _hoverDetails.Remove(key); else _hoverDetails.Add(key);
                    item.Checked = _hoverDetails.Contains(key);
                    SaveSettings();
                };
                details.DropDownItems.Add(item);
            }
            details.DropDownItems.Add(new ToolStripSeparator());
            var showAll = new ToolStripMenuItem("Show all");
            showAll.Click += delegate
            {
                _hoverDetails = new List<string>();
                foreach (var entry in DetailCatalog.All) _hoverDetails.Add(entry[0]);
                SaveSettings();
            };
            details.DropDownItems.Add(showAll);
            var defaults = new ToolStripMenuItem("Reset to defaults");
            defaults.Click += delegate { _hoverDetails = new List<string>(DetailCatalog.Defaults); SaveSettings(); };
            details.DropDownItems.Add(defaults);
            details.DropDown.Closing += (sender, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                    e.Cancel = true;
            };
            _menu.Items.Add(details);

            var nextToIcons = new ToolStripMenuItem("Hover panel next to the icons");
            nextToIcons.Checked = !_hoverLocation.HasValue;
            nextToIcons.ToolTipText = "Drag the hover panel to move it. It then opens at that place until you choose this item.";
            nextToIcons.Click += delegate
            {
                _hoverLocation = null;
                _hover.CustomLocation = null;
                SaveSettings();
            };
            _menu.Items.Add(nextToIcons);
            _menu.Items.Add(new ToolStripSeparator());

            var locations = ReadSavedLocations();
            if (locations.Count == 0)
            {
                var none = new ToolStripMenuItem("No saved locations. Use the star in the app.");
                none.Enabled = false;
                _menu.Items.Add(none);
            }
            else
            {
                var header = new ToolStripMenuItem("Tray shows (the app window stays closed)");
                header.Enabled = false;
                _menu.Items.Add(header);

                var current = ReadWeather();
                var sameAsApp = new ToolStripMenuItem("    Same city as the app" +
                    (current != null ? "  (" + EscapeMenuText(current.Place) + ")" : string.Empty));
                sameAsApp.Checked = _trayCity == AppCity;
                sameAsApp.Click += delegate { SetTrayCity(AppCity); };
                _menu.Items.Add(sameAsApp);

                var all = ReadAllCities();
                foreach (var location in locations)
                {
                    var placeId = location.PlaceId;
                    WeatherReading r;
                    var text = "    " + EscapeMenuText(location.Name);
                    if (all.TryGetValue(placeId, out r) && !IsStale(r))
                        text += "    " + (_showFeelsLike ? r.Feels : r.Temp) + Degree + r.Unit + "  " + EscapeMenuText(r.Phrase);
                    var item = new ToolStripMenuItem(text);
                    item.ToolTipText = location.FullName;
                    item.Checked = _trayCity == placeId;
                    item.Click += delegate { SetTrayCity(placeId); };
                    _menu.Items.Add(item);
                }
            }

            _menu.Items.Add(new ToolStripSeparator());

            if (AppBridge.Available)
            {
                var quick = new ToolStripMenuItem("Update weather every 5 minutes while the window is closed");
                quick.Checked = _quickRefresh;
                quick.Click += delegate
                {
                    _quickRefresh = !_quickRefresh;
                    SaveSettings();
                    if (_quickRefresh)
                    {
                        _lastQuickRefreshUtc = DateTime.MinValue;
                        MaybeQuickRefresh();
                    }
                };
                _menu.Items.Add(quick);
                _menu.Items.Add(new ToolStripSeparator());
            }

            // The Store updates the packaged build, and the app starts its tray icons itself.
            // A direct install (FluentWeather-Setup.exe) has no update script, so it has no update item.
            if (!TrayHost.Packaged)
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update FluentWeather.cmd")))
                {
                    var update = new ToolStripMenuItem("Check for updates now");
                    update.Click += delegate { RunUpdate(); };
                    _menu.Items.Add(update);

                    _menu.Items.Add(new ToolStripSeparator());
                }

                var restart = new ToolStripMenuItem("Restart FluentWeather (app and tray icons)");
                restart.Click += delegate { RestartAll(); };
                _menu.Items.Add(restart);
            }

            var appRunning = AppControl.FindAppProcesses().Count > 0;
            var closeApp = new ToolStripMenuItem("Close FluentWeather window");
            closeApp.Enabled = appRunning;
            closeApp.Click += delegate { AppControl.CloseApp(); };
            _menu.Items.Add(closeApp);

            var exit = new ToolStripMenuItem("Exit tray icons");
            exit.ToolTipText = TrayHost.Packaged
                ? "They start again with the app and at the next sign-in. The setting \"Tray icons\" in the app turns them off."
                : "Start them again from the Start menu: FluentWeather tray icons";
            exit.Click += delegate { ExitThread(); };
            _menu.Items.Add(exit);

            var exitBoth = new ToolStripMenuItem("Exit tray icons and close FluentWeather");
            exitBoth.Enabled = appRunning;
            exitBoth.Click += delegate { AppControl.CloseApp(); ExitThread(); };
            _menu.Items.Add(exitBoth);
        }

        private static string EscapeMenuText(string text)
        {
            // "&" marks a keyboard shortcut in menus. Double it to show a literal "&".
            return (text ?? string.Empty).Replace("&", "&&");
        }

        // Left-click: if the app window was the active window, close it like its X button, so only
        // the tray icons stay. Otherwise open the app, or bring its window to the front.
        private void OnIconClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            HideHover();

            var window = AppWindow.Find();
            if (window != IntPtr.Zero && _appWasActive && !AppWindow.IsMinimized(window))
            {
                _appWasActive = false;
                AppWindow.Close(window);
                return;
            }

            OpenApp();
        }

        // ------------------------------------------------------------------
        // Saved locations
        // ------------------------------------------------------------------

        private sealed class SavedLocation
        {
            public string PlaceId;
            public string Name;
            public string FullName;
        }

        private List<SavedLocation> ReadSavedLocations()
        {
            var result = new List<SavedLocation>();
            try
            {
                // The app keeps its list in its own LocalState folder.
                var json = GetLocalStatePath("savedLocations.json");
                if (json == null || !File.Exists(json))
                    return result;

                var serializer = new JavaScriptSerializer();
                var items = serializer.DeserializeObject(File.ReadAllText(json)) as object[];
                if (items == null)
                    return result;

                foreach (var raw in items)
                {
                    var dict = raw as IDictionary;
                    if (dict == null)
                        continue;

                    var placeId = dict["placeId"] as string;
                    if (string.IsNullOrEmpty(placeId))
                        continue;

                    var name = dict["name"] as string;
                    var fullName = dict["fullName"] as string;

                    var location = new SavedLocation();
                    location.PlaceId = placeId;
                    location.Name = string.IsNullOrEmpty(name) ? (fullName ?? placeId) : name;
                    location.FullName = string.IsNullOrEmpty(fullName) ? location.Name : fullName;
                    result.Add(location);
                }
            }
            catch (Exception)
            {
                // A missing or half-written file only means the menu shows no locations this time.
            }
            return result;
        }

        // ------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------

        private void Launch(string uri)
        {
            try
            {
                var info = new ProcessStartInfo(uri);
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Win32Exception)
            {
                _numberIcon.ShowBalloonTip(5000, AppTitle,
                    TrayHost.Packaged
                        ? "Could not open the app."
                        : "The app is not installed yet. Choose \"Check for updates now\" to build and install it.",
                    ToolTipIcon.Warning);
            }
        }

        // Starts a new copy with /restart, which closes the app and this copy, then starts both again.
        private void RestartAll()
        {
            try
            {
                var info = new ProcessStartInfo(Application.ExecutablePath, "/restart");
                info.UseShellExecute = false;
                info.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(info);
                ExitThread();
            }
            catch (Exception)
            {
                _numberIcon.ShowBalloonTip(5000, AppTitle, "Could not restart. Use the Start menu item \"Restart FluentWeather\".", ToolTipIcon.Error);
            }
        }

        private void RunUpdate()
        {
            var cmd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update FluentWeather.cmd");
            if (!File.Exists(cmd))
            {
                _numberIcon.ShowBalloonTip(5000, AppTitle, "Update FluentWeather.cmd was not found next to the tray helper.", ToolTipIcon.Error);
                return;
            }
            var info = new ProcessStartInfo(cmd);
            info.UseShellExecute = true;
            info.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start(info);
        }

        protected override void ExitThreadCore()
        {
            _quickRefreshTimer.Stop();
            _quickRefreshTimer.Dispose();
            _foregroundWatch.Stop();
            _foregroundWatch.Dispose();
            _timer.Stop();
            _timer.Dispose();
            _changeDelay.Stop();
            _changeDelay.Dispose();
            if (_watcher != null)
                _watcher.Dispose();
            _uiInvoker.Dispose();
            HideHover();
            _hoverShow.Dispose();
            _hoverWatch.Dispose();
            _hover.Dispose();
            foreach (var icon in new[] { _pictureIcon, _numberIcon })
            {
                icon.Visible = false;
                icon.Dispose();
            }
            _menu.Dispose();
            if (_pictureImage != null && !ReferenceEquals(_pictureImage, SystemIcons.Application))
                _pictureImage.Dispose();
            if (_numberImage != null)
                _numberImage.Dispose();
            base.ExitThreadCore();
        }
    }

    internal static class AppControl
    {
        internal static readonly string Protocol = TrayHost.Protocol;

        // The app runs as FluentWeather.exe from its package folder, for example
        // C:\Program Files\WindowsApps\FluentWeather.LocalBuild_2.1.0.1_x64__qqrp5dnjcw9xy\FluentWeather.exe.
        // Only processes from the local build's folder are matched, never the Store version.
        internal static List<Process> FindAppProcesses()
        {
            var result = new List<Process>();
            foreach (var p in Process.GetProcessesByName("FluentWeather"))
            {
                try
                {
                    var path = p.MainModule.FileName;
#if PACKAGED
                    // In the packaged build, only the app from this package's folder.
                    if (path.StartsWith(TrayHost.AppFolder, StringComparison.OrdinalIgnoreCase))
#else
                    if (path.IndexOf(@"\WindowsApps\FluentWeather.LocalBuild_", StringComparison.OrdinalIgnoreCase) >= 0)
#endif
                        result.Add(p);
                    else
                        p.Dispose();
                }
                catch (Exception)
                {
                    p.Dispose(); // path not readable: leave that process alone
                }
            }
            return result;
        }

        // Closes the app window the same way its X button does, so Windows also saves the window
        // size and position. If the app is still running after 4 seconds (for example only its
        // background task runs), it is ended the way Task Manager's "End task" does.
        // Saved locations and settings are written when they change, so nothing is lost.
        internal static void CloseApp()
        {
            var processes = FindAppProcesses();
            if (processes.Count == 0)
                return;

            var window = AppWindow.Find();
            if (window != IntPtr.Zero && AppWindow.Close(window))
            {
                var waitUntil = DateTime.UtcNow.AddSeconds(4);
                while (DateTime.UtcNow < waitUntil && processes.Exists(x => { try { return !x.HasExited; } catch (Exception) { return false; } }))
                    Thread.Sleep(200);
            }

            foreach (var p in processes)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                }
                catch (Exception)
                {
                    // Already closed, or access denied. Nothing else to do.
                }
                finally
                {
                    p.Dispose();
                }
            }
        }

        // Ends other running copies of this tray program (used by /restart).
        internal static void CloseOtherTrayInstances()
        {
            var me = Process.GetCurrentProcess();
            foreach (var p in Process.GetProcessesByName(me.ProcessName))
            {
                try
                {
                    if (p.Id != me.Id)
                    {
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                }
                catch (Exception) { }
                finally { p.Dispose(); }
            }
        }

        // Opens FluentWeather (Local) through its link type.
        internal static bool LaunchApp()
        {
            try
            {
                var info = new ProcessStartInfo(Protocol);
                info.UseShellExecute = true;
                Process.Start(info);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // Asks the app to refresh the weather files through its app service. Windows starts the app in
    // the background for this, without a window. Only compiled with the Windows Runtime (WINRT).
    internal static class AppBridge
    {
        internal const string ServiceName = "FluentWeatherTrayRefresh";

#if WINRT
        internal static bool Available { get { return true; } }

        internal static async Task<bool> RequestRefreshAsync(string packageFamilyName)
        {
            using (var connection = new AppServiceConnection())
            {
                connection.AppServiceName = ServiceName;
                connection.PackageFamilyName = packageFamilyName;
                var status = await connection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                    return false;

                var message = new ValueSet();
                message.Add("action", "refresh");
                var response = await connection.SendMessageAsync(message);
                return response.Status == AppServiceResponseStatus.Success;
            }
        }
#else
        internal static bool Available { get { return false; } }

        internal static Task<bool> RequestRefreshAsync(string packageFamilyName)
        {
            var done = new TaskCompletionSource<bool>();
            done.SetResult(false);
            return done.Task;
        }
#endif
    }

    // Finds and closes the FluentWeather (Local) window.
    // Store-type apps run inside a frame window of class "ApplicationFrameWindow". Its title is the
    // app's display name. Closing that frame window works like the window's X button, so Windows
    // saves the window size and position as usual.
    internal static class AppWindow
    {
        private const string FrameClass = "ApplicationFrameWindow";
        private const uint WM_CLOSE = 0x0010;

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int maxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        // Window classes of the taskbar and of the hidden icons area (Windows 10 and 11).
        private static readonly string[] TaskbarClasses =
        {
            "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "NotifyIconOverflowWindow",
            "TopLevelWindowForOverflowXamlIsland", "XamlExplorerHostIslandWindow"
        };

        private static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;

        internal static IntPtr GetForeground()
        {
            return GetForegroundWindow();
        }

        internal static bool IsMinimized(IntPtr window)
        {
            return IsIconic(window);
        }

        private static string ClassOf(IntPtr hwnd)
        {
            var name = new StringBuilder(256);
            GetClassName(hwnd, name, name.Capacity);
            return name.ToString();
        }

        internal static bool IsAppFrame(IntPtr hwnd)
        {
            if (ClassOf(hwnd) != FrameClass)
                return false;
            var title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            return TrayHost.IsAppTitle(title.ToString());
        }

        internal static bool IsTaskbarOrOwn(IntPtr hwnd)
        {
            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            if (processId == OwnProcessId)
                return true;
            return Array.IndexOf(TaskbarClasses, ClassOf(hwnd)) >= 0;
        }

        internal static IntPtr Find()
        {
            var found = IntPtr.Zero;
            var name = new StringBuilder(256);
            EnumWindows(delegate (IntPtr hwnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hwnd))
                    return true;
                name.Length = 0;
                GetClassName(hwnd, name, name.Capacity);
                if (name.ToString() != FrameClass)
                    return true;
                name.Length = 0;
                GetWindowText(hwnd, name, name.Capacity);
                if (!TrayHost.IsAppTitle(name.ToString()))
                    return true;
                found = hwnd;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        internal static bool Close(IntPtr window)
        {
            return PostMessage(window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    internal static class NativeMethods
    {
        private const int SM_CXSMICON = 49;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool SetProcessDPIAware();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        // Notification area icon size in pixels at the current display scale (16 at 100%, 24 at 150%, 32 at 200%).
        internal static int GetSmallIconSize()
        {
            var size = GetSystemMetrics(SM_CXSMICON);
            return size >= 16 && size <= 64 ? size : 16;
        }
    }
}
