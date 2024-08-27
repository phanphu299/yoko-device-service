namespace AHI.Device.Function.Model.ImportModel
{
    public class Uom : FileParser.Model.ImportModel
    {
        #region Properties

        public int? Id { get; set; }

        public string Name { get; set; }

        public string Lookup { get; set; }

        public string Abbreviation { get; set; }

        public double? CanonicalFactor { get; set; }

        public double? CanonicalOffset { get; set; }

        public double? RefFactor { get; set; }

        public double? RefOffset { get; set; }

        public int? RefId { get; set; }

        public string RefName { get; set; }
        public string Tags { get; set; }


        #endregion
    }
}
