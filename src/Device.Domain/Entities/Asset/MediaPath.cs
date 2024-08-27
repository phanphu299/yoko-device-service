namespace Device.Domain.Entity
{
    public class MediaPath
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentMediaId { get; set; }
        public string ParentMediaName { get; set; }
        public int MediaLevel { get; set; }
        public string MediaPathId { get; set; }
        public string MediaPathName { get; set; }
    }
}