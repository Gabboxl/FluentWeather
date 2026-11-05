using System.Text.Json.Serialization;
using WinRT;

namespace FluentWeather.Models
{
    /// <summary>
    /// A location the user has pinned for quick switching.
    /// Only PlaceId, Name and FullName are persisted.
    /// </summary>
    [GeneratedBindableCustomPropertyAttribute]
    public partial class SavedLocation
    {
        public string PlaceId { get; set; } = string.Empty;

        /// <summary>Short name, for example "Tokyo".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Full name, for example "Tokyo, Tokyo Prefecture, Japan".</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Text shown on the chip. Set at render time.</summary>
        [JsonIgnore]
        public string Label { get; set; } = string.Empty;

        /// <summary>Tooltip with the full name and the keyboard shortcut. Set at render time.</summary>
        [JsonIgnore]
        public string ToolTipText { get; set; } = string.Empty;

        /// <summary>True for the location that is currently selected. Set at render time.</summary>
        [JsonIgnore]
        public bool IsActive { get; set; }
    }
}
