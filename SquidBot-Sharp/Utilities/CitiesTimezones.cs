using System;
using System.Collections.Generic;

namespace SquidBot_Sharp.Utilities
{
    public class CitiesTimezones
    {
        public CitiesTimezones()
        {
            entries = new List<CitiesTimezonesEntry>();
            entries.Add(BuildEntry("Abu Dhabi", "Arabian Standard Time"));
            entries.Add(BuildEntry("Adelaide", "Cen. Australia Standard Time"));
            entries.Add(BuildEntry("Akron", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Almaty", "N. Central Asia Standard Time"));
            entries.Add(BuildEntry("Amsterdam", "W. Europe Standard Time"));
            entries.Add(BuildEntry("Anchorage", "Alaskan Standard Time"));
            entries.Add(BuildEntry("Athens", "GTB Standard Time"));
            entries.Add(BuildEntry("Auckland", "New Zealand Standard Time"));
            entries.Add(BuildEntry("Baghdad", "Arabic Standard Time"));
            entries.Add(BuildEntry("Baku", "Azerbaijan Standard Time"));
            entries.Add(BuildEntry("Bangkok", "SE Asia Standard Time"));
            entries.Add(BuildEntry("Beijing", "China Standard Time"));
            entries.Add(BuildEntry("Beirut", "Middle East Standard Time"));
            entries.Add(BuildEntry("Belgrade", "Central European Standard Time"));
            entries.Add(BuildEntry("Berlin", "W. Europe Standard Time"));
            entries.Add(BuildEntry("Bern", "W. Europe Standard Time"));
            entries.Add(BuildEntry("Boston", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Brasilia", "E. South America Standard Time"));
            entries.Add(BuildEntry("Bratislava", "Central European Standard Time"));
            entries.Add(BuildEntry("Brisbane", "E. Australia Standard Time"));
            entries.Add(BuildEntry("Brussels", "Romance Standard Time"));
            entries.Add(BuildEntry("Bucharest", "GTB Standard Time"));
            entries.Add(BuildEntry("Budapest", "Central European Standard Time"));
            entries.Add(BuildEntry("Buenos Aires", "Argentina Standard Time"));
            entries.Add(BuildEntry("Cairo", "Egypt Standard Time"));
            entries.Add(BuildEntry("Canberra", "AUS Eastern Standard Time"));
            entries.Add(BuildEntry("Chicago", "Central Standard Time"));
            entries.Add(BuildEntry("Chongqing", "China Standard Time"));
            entries.Add(BuildEntry("Cleveland", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Copenhagen", "Romance Standard Time"));
            entries.Add(BuildEntry("Dallas", "Central Standard Time"));
            entries.Add(BuildEntry("Darwin", "AUS Central Standard Time"));
            entries.Add(BuildEntry("Delhi", "India Standard Time"));
            entries.Add(BuildEntry("Dusseldorf", "Central European Standard Time"));
            entries.Add(BuildEntry("Fucking", "W. Europe Standard Time"));
            entries.Add(BuildEntry("Hanoi", "SE Asia Standard Time"));
            entries.Add(BuildEntry("Harare", "South Africa Standard Time"));
            entries.Add(BuildEntry("Helsinki", "FLE Standard Time"));
            entries.Add(BuildEntry("Hobart", "Tasmania Standard Time"));
            entries.Add(BuildEntry("Hong Kong", "China Standard Time"));
            entries.Add(BuildEntry("Honolulu", "Hawaiian Standard Time"));
            entries.Add(BuildEntry("Houston", "Central Standard Time"));
            entries.Add(BuildEntry("Hyderabad", "India Standard Time"));
            entries.Add(BuildEntry("Islamabad", "Pakistan Standard Time"));
            entries.Add(BuildEntry("Istanbul", "GTB Standard Time"));
            entries.Add(BuildEntry("Jakarta", "SE Asia Standard Time"));
            entries.Add(BuildEntry("Jerusalem", "Israel Standard Time"));
            entries.Add(BuildEntry("Johannesburg", "South Africa Standard Time"));
            entries.Add(BuildEntry("Karachi", "Pakistan Standard Time"));
            entries.Add(BuildEntry("Kathmandu", "Nepal Standard Time"));
            entries.Add(BuildEntry("Kuala Lumpur", "Singapore Standard Time"));
            entries.Add(BuildEntry("Kuwait", "Arab Standard Time"));
            entries.Add(BuildEntry("Kyiv", "FLE Standard Time"));
            entries.Add(BuildEntry("Lagos", "W. Central Africa Standard Time"));
            entries.Add(BuildEntry("Lima", "SA Pacific Standard Time"));
            entries.Add(BuildEntry("Ljubljana", "Central European Standard Time"));
            entries.Add(BuildEntry("London", "GMT Standard Time"));
            entries.Add(BuildEntry("Los Angeles", "Pacific Standard Time"));
            entries.Add(BuildEntry("Mandrid", "Central European Standard Time"));
            entries.Add(BuildEntry("Manila", "Singapore Standard Time"));
            entries.Add(BuildEntry("Melbourne", "AUS Eastern Standard Time"));
            entries.Add(BuildEntry("Mexico City", "Central Standard Time (Mexico)"));
            entries.Add(BuildEntry("Miami", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Minsk", "E. Europe Standard Time"));
            entries.Add(BuildEntry("Montevideo", "Montevideo Standard Time"));
            entries.Add(BuildEntry("Moscow", "Russian Standard Time"));
            entries.Add(BuildEntry("Mumbai", "India Standard Time"));
            entries.Add(BuildEntry("Muscat", "Arabian Standard Time"));
            entries.Add(BuildEntry("Nairobi", "E. Africa Standard Time"));
            entries.Add(BuildEntry("NYC", "US Eastern Standard Time"));
            entries.Add(BuildEntry("New York", "US Eastern Standard Time"));
            entries.Add(BuildEntry("New York City", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Novosibirsk", "N. Central Asia Standard Time"));
            entries.Add(BuildEntry("Oklahoma City", "Central Standard Time"));
            entries.Add(BuildEntry("Onitsha", "W. Central Africa Standard Time"));
            entries.Add(BuildEntry("Orlando", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Osaka", "Tokyo Standard Time"));
            entries.Add(BuildEntry("Paris", "Central European Standard Time"));
            entries.Add(BuildEntry("Perth", "W. Australia Standard Time"));
            entries.Add(BuildEntry("Phoenix", "US Mountain Standard Time"));
            entries.Add(BuildEntry("Prague", "Central European Standard Time"));
            entries.Add(BuildEntry("Pretoria", "South Africa Standard Time"));
            entries.Add(BuildEntry("Riga", "FLE Standard Time"));
            entries.Add(BuildEntry("Rio de Janeiro", "E. South America Standard Time"));
            entries.Add(BuildEntry("Rome", "W. Europe Standard Time"));
            entries.Add(BuildEntry("Riyadh", "Arab Standard Time"));
            entries.Add(BuildEntry("San Francisco", "Pacific Standard Time"));
            entries.Add(BuildEntry("Santiago", "Pacific SA Standard Time"));
            entries.Add(BuildEntry("Sao Paulo", "E. South America Standard Time"));
            entries.Add(BuildEntry("Sarajevo", "Central European Standard Time"));
            entries.Add(BuildEntry("Seattle", "Pacific Standard Time"));
            entries.Add(BuildEntry("Seoul", "Korea Standard Time"));
            entries.Add(BuildEntry("Shanghai", "China Standard Time"));
            entries.Add(BuildEntry("Skopje", "Central European Standard Time"));
            entries.Add(BuildEntry("Sofia", "FLE Standard Time"));
            entries.Add(BuildEntry("Stockholm", "W. Europe Standard Time"));
            entries.Add(BuildEntry("St. Petersburg", "Russian Standard Time"));
            entries.Add(BuildEntry("Sydney", "AUS Eastern Standard Time"));
            entries.Add(BuildEntry("Taipei", "Taipei Standard Time"));
            entries.Add(BuildEntry("Tallinn", "FLE Standard Time"));
            entries.Add(BuildEntry("Tbilisi", "Georgian Standard Time"));
            entries.Add(BuildEntry("Tehran", "Iran Standard Time"));
            entries.Add(BuildEntry("Tokyo", "Tokyo Standard Time"));
            entries.Add(BuildEntry("Toronto", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Tulsa", "Central Standard Time"));
            entries.Add(BuildEntry("Ulaan Bataar", "North Asia East Standard Time"));
            entries.Add(BuildEntry("Vancouver", "Pacific Standard Time"));
            entries.Add(BuildEntry("Vienna", "W. Europe Standard Time"));
            entries.Add(BuildEntry("Vilnius", "FLE Standard Time"));
            entries.Add(BuildEntry("Vladivostok", "Vladivostok Standard Time"));
            entries.Add(BuildEntry("Volgograd", "Russian Standard Time"));
            entries.Add(BuildEntry("Warsaw", "Central European Standard Time"));
            entries.Add(BuildEntry("Washington DC", "US Eastern Standard Time"));
            entries.Add(BuildEntry("Wellington", "New Zealand Standard Time"));
            entries.Add(BuildEntry("Windhoek", "Namibia Standard Time"));
            entries.Add(BuildEntry("Zagreb", "Central European Standard Time"));
        }

        private CitiesTimezonesEntry BuildEntry(string CityName, string TimeZoneString)
        {
            var TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneString);
            return new CitiesTimezonesEntry { CityName = CityName, TimeZone = TimeZone };
        }

        public List<CitiesTimezonesEntry> entries { get; set; }
    }


    public class CitiesTimezonesEntry
    {
        public string CityName { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
    }

}
