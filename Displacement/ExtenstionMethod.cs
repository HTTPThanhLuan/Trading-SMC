using System;
using System.Collections.Generic;
using System.Text;

namespace Displacement
{
    public static class ExtenstionMethod
    {
        public static DateTime ConvertLocalToEstTime(this DateTime localDateTime)
        {
            try
            {
                // Get EST timezone (Eastern Standard Time)
                var estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                // Convert from local time to EST
                return TimeZoneInfo.ConvertTime(localDateTime, TimeZoneInfo.Local, estZone);
            }
            catch
            {
                // If timezone conversion fails, return the original time
                return localDateTime;
            }
        }

        public static DateTime ConvertEstToLocalTime(this DateTime estDateTime)
        {
            try
            {
                // Get EST timezone (Eastern Standard Time)
                var estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

                // Convert from EST to local time
                return TimeZoneInfo.ConvertTime(estDateTime, estZone, TimeZoneInfo.Local);
            }
            catch
            {
                // If timezone conversion fails, return the original time
                return estDateTime;
            }
        }
    }
}
