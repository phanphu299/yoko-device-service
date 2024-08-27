using System;

namespace Device.Application.Asset.Command.Model
{
    public class BaseDependency : IEquatable<BaseDependency>
    {
        public string Type { get; }
        public Guid Id { get; }
        public string Name { get; }

        public BaseDependency(string type, Guid id, string name)
        {
            Type = type;
            Id = id;
            Name = name;
        }

        public bool Equals(BaseDependency other)
        {
            if (Id == other.Id && Name == other.Name && Type == other.Type)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            int hashId = Id == Guid.Empty ? 0 : Id.GetHashCode();
            int hashName = Name == null ? 0 : Name.GetHashCode();
            int hashType = Type == null ? 0 : Type.GetHashCode();

            return hashId ^ hashName ^ hashType;
        }
    }
}
