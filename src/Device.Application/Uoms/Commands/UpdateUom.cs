using System;
using System.Linq.Expressions;
using Device.Application.Constants;
using Device.Application.Uom.Command.Model;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Uom.Command
{
    public class UpdateUom : UpsertTagCommand, IRequest<UpdateUomsDto>
    {
        public int Id { get; set; }
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public double? RefFactor { get; set; }
        public double? RefOffset { get; set; }
        //public double? CanonicalFactor { get; set; }
        // public double? CanonicalOffset { get; set; }
        public string LookupCode { get; set; }
        public string Description { get; set; }
        [DynamicValidation(RemoteValidationKeys.abbreviation)]
        public string Abbreviation { get; set; }
        public int? RefId { get; set; }
        static Func<UpdateUom, Domain.Entity.Uom> Converter = Projection.Compile();
        private static Expression<Func<UpdateUom, Domain.Entity.Uom>> Projection
        {
            get
            {
                return element => new Domain.Entity.Uom
                {
                    Id = element.Id,
                    Name = element.Name,
                    RefFactor = element.RefFactor,
                    RefOffset = element.RefOffset,
                    //CanonicalFactor = element.CanonicalFactor,
                    //CanonicalOffset = element.CanonicalOffset,
                    LookupCode = element.LookupCode,
                    Description = element.Description,
                    Abbreviation = element.Abbreviation,
                    RefId = element.RefId
                };
            }
        }

        public static Domain.Entity.Uom Create(UpdateUom model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
