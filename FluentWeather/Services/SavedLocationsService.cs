using FluentWeather.Helpers;
using FluentWeather.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace FluentWeather.Services
{
    /// <summary>
    /// Stores the list of pinned locations in LocalFolder\savedLocations.json.
    /// A file is used instead of LocalSettings because LocalSettings values are limited to 8 KB.
    /// The active location is still stored in the "lastPlaceId" setting, so the live tile
    /// background task and the first-run dialog keep working unchanged.
    /// </summary>
    public static class SavedLocationsService
    {
        public const int MaxLocations = 25;

        private const string FileName = "savedLocations";

        private static readonly SemaphoreSlim Lock = new(1, 1);

        private static List<SavedLocation>? _cache;

        public static async Task<IReadOnlyList<SavedLocation>> GetAllAsync()
        {
            await Lock.WaitAsync();
            try
            {
                return Snapshot(await LoadUnlockedAsync());
            }
            finally
            {
                Lock.Release();
            }
        }

        public static async Task<bool> ContainsAsync(string placeId)
        {
            var all = await GetAllAsync();
            return all.Any(x => x.PlaceId == placeId);
        }

        /// <summary>Adds a location. Returns false if the list is full. Adding an existing location updates its names.</summary>
        public static async Task<bool> AddAsync(string placeId, string name, string fullName)
        {
            if (string.IsNullOrWhiteSpace(placeId))
                return false;

            await Lock.WaitAsync();
            try
            {
                var list = await LoadUnlockedAsync();
                var existing = list.FirstOrDefault(x => x.PlaceId == placeId);

                if (existing != null)
                {
                    existing.Name = name;
                    existing.FullName = fullName;
                }
                else
                {
                    if (list.Count >= MaxLocations)
                        return false;

                    list.Add(new SavedLocation { PlaceId = placeId, Name = name, FullName = fullName });
                }

                await SaveUnlockedAsync(list);
                return true;
            }
            finally
            {
                Lock.Release();
            }
        }

        public static async Task RemoveAsync(string placeId)
        {
            await Lock.WaitAsync();
            try
            {
                var list = await LoadUnlockedAsync();
                if (list.RemoveAll(x => x.PlaceId == placeId) > 0)
                    await SaveUnlockedAsync(list);
            }
            finally
            {
                Lock.Release();
            }
        }

        /// <summary>Moves a location by <paramref name="offset"/> positions. Negative values move it left.</summary>
        public static async Task MoveAsync(string placeId, int offset)
        {
            await Lock.WaitAsync();
            try
            {
                var list = await LoadUnlockedAsync();
                var index = list.FindIndex(x => x.PlaceId == placeId);
                if (index < 0)
                    return;

                var newIndex = Math.Clamp(index + offset, 0, list.Count - 1);
                if (newIndex == index)
                    return;

                var item = list[index];
                list.RemoveAt(index);
                list.Insert(newIndex, item);
                await SaveUnlockedAsync(list);
            }
            finally
            {
                Lock.Release();
            }
        }

        private static async Task<List<SavedLocation>> LoadUnlockedAsync()
        {
            if (_cache != null)
                return _cache;

            try
            {
                _cache = await ApplicationData.Current.LocalFolder.ReadAsync<List<SavedLocation>>(FileName) ?? [];
            }
            catch (Exception)
            {
                // A missing or corrupt file must not stop the app from starting.
                _cache = [];
            }

            // Drop invalid or duplicate entries that may come from a hand-edited file.
            _cache = _cache
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.PlaceId))
                .GroupBy(x => x.PlaceId)
                .Select(g => g.First())
                .Take(MaxLocations)
                .ToList();

            return _cache;
        }

        private static async Task SaveUnlockedAsync(List<SavedLocation> list)
        {
            _cache = list;
            await ApplicationData.Current.LocalFolder.SaveAsync(FileName, list);
        }

        private static IReadOnlyList<SavedLocation> Snapshot(List<SavedLocation> list)
        {
            // Return copies so the UI can set Label and IsActive without touching the stored objects.
            return list.Select(x => new SavedLocation
            {
                PlaceId = x.PlaceId,
                Name = x.Name,
                FullName = x.FullName
            }).ToList();
        }
    }
}
