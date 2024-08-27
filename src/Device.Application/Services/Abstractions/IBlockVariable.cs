using System;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockVariable
    {
        void Set(string key, int value);
        void SafetySetIfNotExists(string key, string dataType, object value);
        void Set(string key, bool value);
        void Set(string key, double value);
        void Set(string key, DateTime value);
        void Set(string key, string value);
        void Set(string key, DateTime dateTime, double value);
        void Set(string key, DateTime dateTime, int value);
        void Set(string key, DateTime dateTime, bool value);
        void Set(string key, DateTime dateTime, string value);
        void Set(string key, (DateTime, double) value);
        void Set(string key, (DateTime, int) value);
        void Set(string key, (DateTime, bool) value);
        void Set(string key, (DateTime, string) value);
        void Set(string key, (DateTime, object, string) value);
        void Set(string key, IBlockContext context);
        void Set(string key, params (DateTime, double)[] values);
        void Set(string key, params (DateTime, int)[] values);
        void Set(string key, params (DateTime, bool)[] values);
        void Set(string key, params (DateTime, string)[] values);
        object SafetyGet(string key, string dataType);
        void SafetySet(string key, string dataType, object value);
        IBlockContext GetAttribute(string key);
        IBlockContext GetTable(string key);
        IBlockHttpContext WithHttp(string endpoint);
        string GetString(string key);
        bool GetBoolean(string key);
        double GetDouble(string key);
        DateTime? GetDateTime(string key);
        int GetInt(string key);
    }
}