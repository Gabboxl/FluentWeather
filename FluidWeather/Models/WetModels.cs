using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidWeather.Models
{
    public class Locale
        {
            public string locale1 { get; set; }
            public string locale2 { get; set; }
            public object locale3 { get; set; }
            public object locale4 { get; set; }
        }

        public class Location
        {
            public string address { get; set; }
            public Locale locale { get; set; }
            public string displayName { get; set; }
            public string ianaTimeZone { get; set; }
            public string adminDistrict { get; set; }
            public object adminDistrictCode { get; set; }
            public string city { get; set; }
            public double longitude { get; set; }
            public string postalCode { get; set; }
            public double latitude { get; set; }
            public string placeId { get; set; }
            public object neighborhood { get; set; }
            public string country { get; set; }
            public string postalKey { get; set; }
            public string countryCode { get; set; }
            public bool disputedArea { get; set; }
            public object disputedCountries { get; set; }
            public object disputedCountryCodes { get; set; }
            public object disputedCustomers { get; set; }
            public List<bool> disputedShowCountry { get; set; }
            public string iataCode { get; set; }
            public string icaoCode { get; set; }
            public string locId { get; set; }
            public object locationCategory { get; set; }
            public string pwsId { get; set; }
            public string type { get; set; }
        }

        public class LocationResponse
        {
            public List<Location> location { get; set; }
        }


}
