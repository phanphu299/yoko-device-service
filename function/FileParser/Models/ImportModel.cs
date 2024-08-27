using System;
using System.Collections.Generic;

namespace AHI.Device.Function.FileParser.Model
{
    public abstract class ImportModel : TrackModel
    {
    }

    public abstract class ComplexModel : ImportModel
    {
        public abstract Type ChildType { get; }
        public abstract IEnumerable<object> ChildProperty { set; }
    }
}