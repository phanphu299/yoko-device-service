using System.Text.Json.Serialization;

namespace AHI.Device.Function.FileParser.Model
{
    public abstract class TrackModel
    {
        private static int seed = 0;
        [JsonIgnore]
        public int TrackId { get; set; }

        // Use when validating to notify which property has error, use for error tracking purpose.
        // Override to custom different mapping between the property where error was found and the property to be shown or tracked
        // (When error data spread between different property and only track one of them)
        public virtual string ErrorProperty(string validationPropertyName)
        {
            return validationPropertyName;
        }

        public TrackModel()
        {
            if (seed == int.MaxValue)
                seed = 0;
            TrackId = seed++;
        }
    }
}