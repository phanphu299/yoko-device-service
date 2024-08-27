using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public static class AlarmBlockExtension
    {
        public static IAlarmBlockContext Alarm(this IBlockContext context, string assetName)
        {
            var alarmContext = new AlarmBlockContext();
            alarmContext.SetAlarmName(assetName);
            return alarmContext;
        }
    }
}