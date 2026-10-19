using System;
using System.Globalization;

namespace Shooter.Game
{
    public static class RussianDate
    {
        private const string TimeFormat = "HH:mm";

        private static readonly string[] MonthNames =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        public static string FormatDate(DateTime moment)
        {
            return moment.Day + " " + MonthNames[moment.Month - 1] + " " + moment.Year;
        }

        public static string FormatDateAndTime(DateTime moment)
        {
            return FormatDate(moment) + ", " + moment.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }
    }
}
