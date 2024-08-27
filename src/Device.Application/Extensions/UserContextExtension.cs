using AHI.Infrastructure.UserContext.Abstraction;

namespace Device.ApplicationExtension.Extension
{
    public static class UserContextExtension
    {
        public static void CopyFrom(this IUserContext des, IUserContext src)
        {
            des.SetId(src.Id);
            des.SetUpn(src.Upn);
            des.SetName(src.FirstName, src.MiddleName, src.LastName);
            des.SetAvatar(src.Avatar);
            des.SetDateTimeFormat(src.DateTimeFormat);
            des.SetTimezone(src.Timezone);
            des.SetRightShorts(src.RightShorts);
            des.SetObjectRightShorts(src.ObjectRightShorts);
            des.SetApplicationId(src.ApplicationId);
        }
    }
}
