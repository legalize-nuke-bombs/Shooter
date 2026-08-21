using System;
using System.Globalization;

namespace Shooter.Client.Interface
{
    public static class RussianDate
    {
        private const string TimeFormat = "HH:mm";

        private static readonly string[] Months =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        public static string Day(DateTime moment)
        {
            return moment.Day + " " + Months[moment.Month - 1] + " " + moment.Year;
        }

        public static string Moment(DateTime moment)
        {
            return Day(moment) + ", " + moment.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }
    }
}
