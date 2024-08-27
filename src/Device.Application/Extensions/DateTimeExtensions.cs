using System;

namespace Device.ApplicationExtension.Extension
{
    public static class DateTimeExtensions
    {

        public static DateTime CutOffNanoseconds(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, dateTime.Kind);
        }
    }
}