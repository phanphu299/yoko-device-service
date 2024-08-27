using System;

namespace Device.Job.Extension
{
    public static class DateTimeExtension
    {
        public static DateTime AddTimezoneOffset(this DateTime datetime, string timeserieTimezoneOffset)
        {
            var hour = 0;
            var minutes = 0;
            var operation = "+";
            int multiple = 1;
            if (!string.IsNullOrEmpty(timeserieTimezoneOffset) && (timeserieTimezoneOffset.StartsWith("+") || timeserieTimezoneOffset.StartsWith("-")))
            {
                if (timeserieTimezoneOffset.StartsWith("-"))
                {
                    operation = "-";
                    multiple = -1;
                }
                timeserieTimezoneOffset = timeserieTimezoneOffset.Replace(operation, "");
                int.TryParse(timeserieTimezoneOffset.Split(':')[0], out hour);
                int.TryParse(timeserieTimezoneOffset.Split(':')[1], out minutes);
            }
            return datetime.AddHours(hour * multiple).AddMinutes(minutes * multiple);
        }
    }
}