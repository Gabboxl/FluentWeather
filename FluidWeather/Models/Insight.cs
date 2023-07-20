using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidWeather.Models
{
    public enum InsightLevel { None, Low, Medium, High }

    public class Insight
    {
        public string Title { get; set; }
        public int Value { get; set; }
        public string Description { get; set; }
        public Dictionary<int, InsightLevel> Levels { get; set; }
        public string IconName { get; set; }
    }

    public static class InsightLevels
    {
        public static Dictionary<int, InsightLevel> RunningLevels = new()
        {
            {1, InsightLevel.Low},
            {2, InsightLevel.Low},
            {3, InsightLevel.Low},
            {4, InsightLevel.Low},
            {5, InsightLevel.Medium},
            {6, InsightLevel.Medium},
            {7, InsightLevel.High},
            {8, InsightLevel.High},
            {9, InsightLevel.High},
            {10, InsightLevel.High},
        };

        public static Dictionary<int, InsightLevel> DrivingLevels = new()
        {
            {0, InsightLevel.High},
            {1, InsightLevel.Medium},
            {2, InsightLevel.Medium},
            {3, InsightLevel.Medium},
            {4, InsightLevel.Medium},
            {5, InsightLevel.Low},
            {6, InsightLevel.Low},
        };

        public static Dictionary<int, InsightLevel> PollenLevels = new()
        {
            {0, InsightLevel.High},
            {1, InsightLevel.High},
            {2, InsightLevel.Medium},
            {3, InsightLevel.Low},
            {4, InsightLevel.Low},
        };

        public static Dictionary<int, InsightLevel> DrySkinLevels = new()
        {
            {0, InsightLevel.High},
            {1, InsightLevel.High},
            {2, InsightLevel.High},
            {3, InsightLevel.High},
            {4, InsightLevel.High},
            {5, InsightLevel.Medium},
            {6, InsightLevel.Medium},
            {7, InsightLevel.Low},
            {8, InsightLevel.Low},
            {9, InsightLevel.Low},
            {10, InsightLevel.Low},
        };

        public static Dictionary<int, InsightLevel> WateringLevels = new()
        {
            {0, InsightLevel.High},
            {1, InsightLevel.High},
            {2, InsightLevel.High},
            {3, InsightLevel.High},
            {4, InsightLevel.High},
            {5, InsightLevel.Medium},
            {6, InsightLevel.Medium},
            {7, InsightLevel.Low},
            {8, InsightLevel.Low},
            {9, InsightLevel.Low},
            {10, InsightLevel.Low},
        };
    }
}
