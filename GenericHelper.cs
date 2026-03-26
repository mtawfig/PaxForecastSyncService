using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XovisPaxForecastFeedWinSvc
{
    public static class GenericHelper
    {
        public static long ToUnixTimestamp(DateTime dateTime)
        {
            DateTime utcDateTime = dateTime.ToUniversalTime();
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((utcDateTime - epoch).TotalSeconds);
        }

        public static DateTime UnixTimeToDateTime(long unixTimestamp)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimestamp).ToLocalTime();
        }

        public static int ConvertSecondsToRoundedMinutes(int seconds)
        {
            double minutes = seconds / 60.0;

            // Rule 2
            if (minutes < 1)
                return 1;

            // Rule 3
            return (int)Math.Ceiling(minutes);
        }
    }
}
