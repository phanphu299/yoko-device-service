using System;
using System.Collections.Generic;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class BlockVariable : IBlockVariable
    {
        private IDictionary<string, IBlockContext> _dictionary;
        private IBlockEngine _engine;
        public BlockVariable(IBlockEngine engine)
        {
            _engine = engine;
            _dictionary = new Dictionary<string, IBlockContext>();
        }
        public object Get(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return context.Value;
            }
            return null;
        }
        public string GetString(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return context?.Value?.ToString();
            }
            return null;
        }
        public bool GetBoolean(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return Convert.ToBoolean(context.Value ?? "false");
            }
            return false;
        }
        public double GetDouble(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return Convert.ToDouble(context.Value ?? "0.0");
            }
            return 0;
        }
        public int GetInt(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return Convert.ToInt32(context.Value ?? "0");
            }
            return 0;
        }
        public DateTime? GetDateTime(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return context.Value as DateTime?;
            }
            return null;
        }
        public IBlockContext GetAttribute(string key)
        {
            if (_dictionary.ContainsKey(key))
            {
                IBlockContext context = _dictionary[key];
                return context;
            }
            return null;
        }

        public IBlockContext GetTable(string key)
        {
            IBlockContext context = null;
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            context = _dictionary[key];
            return context;
        }

        public void Set(string key, int value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(value);

        }
        public void Set(string key, DateTime value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(value);

        }
        public void SafetySetIfNotExists(string key, string dataType, object value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
                SafetySet(key, dataType, value);
            }
        }

        public void Set(string key, bool value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(value);

        }


        public void Set(string key, double value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(value);

        }


        public void Set(string key, string value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(value);

        }


        public void Set(string key, DateTime dateTime, double value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(dateTime, value);

        }

        public void Set(string key, DateTime dateTime, int value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(dateTime, value);

        }

        public void Set(string key, DateTime dateTime, bool value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(dateTime, value);

        }

        public void Set(string key, DateTime dateTime, string value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(dateTime, value);

        }

        public void Set(string key, (DateTime, double) value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            var (dateTime, v) = value;
            context.SetValue(dateTime, v);

        }
        public void Set(string key, params (DateTime, double)[] values)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(values);
        }

        public void Set(string key, (DateTime, int) value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            var (dateTime, v) = value;
            context.SetValue(dateTime, v);
        }
        public void Set(string key, params (DateTime, int)[] values)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            //var (dateTime, v) = value;
            context.SetValue(values);
        }

        public void Set(string key, (DateTime, bool) value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            var (dateTime, v) = value;
            context.SetValue(dateTime, v);

        }
        public void Set(string key, params (DateTime, bool)[] values)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            //var (dateTime, v) = value;
            context.SetValue(values);

        }

        public void Set(string key, (DateTime, string) value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            var (dateTime, v) = value;
            context.SetValue(dateTime, v);

        }
        public void Set(string key, params (DateTime, string)[] values)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            context.SetValue(values);

        }

        public void Set(string key, (DateTime, object, string) value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            IBlockContext context = _dictionary[key];
            var (dateTime, v, dataType) = value;
            switch (dataType)
            {
                case Constant.DataTypeConstants.TYPE_TEXT:
                    Set(key, v.ToString());
                    break;
                case Constant.DataTypeConstants.TYPE_BOOLEAN:
                    Set(key, Convert.ToBoolean(v));
                    break;
                case Constant.DataTypeConstants.TYPE_INTEGER:
                    Set(key, Convert.ToInt32(v));
                    break;
                case Constant.DataTypeConstants.TYPE_DOUBLE:
                    Set(key, Convert.ToDouble(v));
                    break;
            }
        }
        public void Set(string key, IBlockContext context)
        {
            _dictionary[key] = context;
        }

        public object SafetyGet(string key, string dataType)
        {
            if (_dictionary.ContainsKey(key))
            {
                switch (dataType)
                {
                    case Constant.BindingDataTypeIdConstants.TYPE_TEXT:
                        return GetString(key);
                    case Constant.BindingDataTypeIdConstants.TYPE_BOOLEAN:
                        return GetBoolean(key);
                    case Constant.BindingDataTypeIdConstants.TYPE_INTEGER:
                        return GetInt(key);
                    case Constant.BindingDataTypeIdConstants.TYPE_DOUBLE:
                        return GetDouble(key);
                    case Constant.BindingDataTypeIdConstants.TYPE_DATETIME:
                        return GetDateTime(key);
                    case Constant.BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE:
                    case Constant.BindingDataTypeIdConstants.TYPE_ASSET_TABLE:
                        return _dictionary[key];
                }
            }
            return null;
        }

        public void SafetySet(string key, string dataType, object value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new BlockContext(_engine);
            }
            var context = _dictionary[key];
            switch (dataType)
            {
                case Constant.BindingDataTypeIdConstants.TYPE_TEXT:
                    context.SetValue(value?.ToString() ?? "");
                    break;
                case Constant.BindingDataTypeIdConstants.TYPE_BOOLEAN:
                    var boolDefaultValue = !string.IsNullOrEmpty(value?.ToString()) ? value : false;
                    context.SetValue(Convert.ToBoolean(boolDefaultValue));
                    break;
                case Constant.BindingDataTypeIdConstants.TYPE_INTEGER:
                    var intDefaultValue = !string.IsNullOrEmpty(value?.ToString()) ? value : 0;
                    context.SetValue(Convert.ToInt32(intDefaultValue));
                    break;
                case Constant.BindingDataTypeIdConstants.TYPE_DOUBLE:
                    var doubleDefaultValue = !string.IsNullOrEmpty(value?.ToString()) ? value : 0;
                    context.SetValue(Convert.ToDouble(doubleDefaultValue));
                    break;
                case Constant.BindingDataTypeIdConstants.TYPE_DATETIME:
                    var dateTimeDefaultValue = value as DateTime?;
                    context.SetValue(dateTimeDefaultValue.HasValue ? dateTimeDefaultValue.Value : default(DateTime));
                    break;
                case Constant.BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE:
                case Constant.BindingDataTypeIdConstants.TYPE_ASSET_TABLE:
                    // for asset attribute -> try to cast into target type
                    var ctx = value as IBlockContext;
                    context.CopyFrom(ctx);
                    break;
            }
        }

        public IBlockHttpContext WithHttp(string endpoint)
        {
            return new BlockHttpContext(_engine, endpoint);
        }
    }
}