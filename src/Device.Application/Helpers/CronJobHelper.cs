namespace Device.Application.Helper
{
    public static class CronJobHelper
    {
        public static bool IsValidCronExpression(string expression)
        {
            return Quartz.CronExpression.IsValidExpression(expression);
        }
    }
}