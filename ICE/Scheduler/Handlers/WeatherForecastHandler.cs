using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace ICE.Scheduler.Handlers
{
    internal class WeatherForecastHandler
    {
        private const double Seconds = 1;
        private const double Minutes = 60 * Seconds;
        private const double WeatherPeriod = 23 * Minutes + 20 * Seconds;
        internal static bool AccurateTime = false;

        private static ExcelSheet<Weather>? WeatherSheet;
        internal static List<WeatherForecast> weathers = [];

        private static DateTime _lastProcessed = DateTime.MinValue;
        private static TimeSpan _delay = TimeSpan.FromSeconds(300);
        private static uint previousZoneForecast = 0;

        internal static unsafe void Tick()
        {
            WeatherSheet ??= Svc.Data.GetExcelSheet<Weather>();
            if (!IsInCosmicZone()) return;

            Weather currWeather = GetCurrentWeather();
            var CurrentWeatherInForecast = weathers
                .Select((item, index) => new { item, index })
                .FirstOrDefault(w => w.item.Name == currWeather.Name);

            //Only allow refresh if territory changed
            //Or weather is not in forecast (ex: Red Alert)
            //Or already past 5 minutes since the last refresh
            if (Svc.ClientState.TerritoryType == previousZoneForecast && CurrentWeatherInForecast != null && DateTime.Now - _lastProcessed < _delay) return;
            RefreshForecast();
        }

        internal static void RefreshForecast()
        {
            if (!IsInCosmicZone()) return;
            
            _lastProcessed = DateTime.Now;
            GetForecast();
        }

        private static unsafe Weather GetCurrentWeather()
        {
            WeatherManager* wm = WeatherManager.Instance();
            byte currWeatherId = wm->GetCurrentWeather();
            return WeatherSheet.GetRow(currWeatherId);
        }

        internal static unsafe uint GetCurrentWeatherId()
        {
            if (!IsInCosmicZone()) return default;

            Weather currWeather = GetCurrentWeather();
            return currWeather.RowId;
        }

        internal static unsafe (string, string, string) GetNextWeather()
        {
            if (!IsInCosmicZone()) return default;

            Weather currWeather = GetCurrentWeather();

            var currentWeather = weathers
                .Select((item, index) => new { item, index })
                .First(w => w.item.Name == currWeather.Name);
            var nextWeather = weathers
                .Skip(currentWeather.index + 1)
                .Select((item, index) => new { item, index })
                .First();

            return (currentWeather.item.Name, nextWeather.item.Name, FormatForecastTime(nextWeather.item.Time));
        }

        internal static unsafe void GetForecast()
        {
            previousZoneForecast = Svc.ClientState.TerritoryType;

            WeatherManager* wm = WeatherManager.Instance();
            Weather currentWeather = GetCurrentWeather();
            Weather lastWeather = currentWeather;

            weathers = [BuildResultObject(currentWeather, GetRootTime(0))];

            for (var i = 1; i <= 10; i++)
            {
                byte weatherId = wm->GetWeatherForDaytime(Svc.ClientState.TerritoryType, i);
                var weather = WeatherSheet.GetRow(weatherId)!;
                var time = GetRootTime(i * WeatherPeriod);

                if (lastWeather.RowId != weather.RowId)
                {
                    lastWeather = weather;
                    weathers.Add(BuildResultObject(weather, time));
                }
            }
            weathers = weathers.Take(3).ToList();
        }

        private static WeatherForecast BuildResultObject(Weather weather, DateTime time)
        {
            var name = weather.Name.ExtractText();
            var iconId = (uint)weather.Icon;

            return new(time, name, iconId);
        }
        private static DateTime GetRootTime(double initialOffset)
        {
            var now = DateTime.UtcNow;
            var rootTime = now.AddMilliseconds(-now.Millisecond).AddSeconds(initialOffset);
            var seconds = (long)(rootTime - DateTime.UnixEpoch).TotalSeconds % WeatherPeriod;

            rootTime = rootTime.AddSeconds(-seconds);

            return rootTime;
        }

        internal static string FormatForecastTime(DateTime forecastTime)
        {
            TimeSpan timeDifference = forecastTime - DateTime.UtcNow;
            if (!AccurateTime)
            {
                string format = C.ShowSeconds ? @"hh\:mm\:ss" : @"hh\:mm";
                return timeDifference < TimeSpan.Zero ? "-" + timeDifference.Duration().ToString(format) : timeDifference.ToString(format);
            }
            else
            {
                int totalSeconds = Math.Abs((int)timeDifference.TotalSeconds);
                int hours = totalSeconds / 10000;
                int minutes = (totalSeconds % 10000) / 100;
                string format = C.ShowSeconds ? $"{hours:D2}:{minutes:D2}:{totalSeconds % 100:D2}" : $"{hours:D2}:{minutes:D2}";
                return timeDifference < TimeSpan.Zero ? "-" + format : format;
            }
        }
    }

    internal class WeatherForecast(DateTime time, string name, uint iconId)
    {
        public DateTime Time = time;
        public string Name = name;
        public uint IconId = iconId;
    }
}
