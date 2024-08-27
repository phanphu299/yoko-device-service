using System;

namespace Device.Job.Model
{
    public class Attribute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Attribute(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}