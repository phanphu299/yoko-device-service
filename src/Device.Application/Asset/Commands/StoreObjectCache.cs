namespace Device.Application.Asset.Command
{
    public class StoreObjectCache
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public StoreObjectCache(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}