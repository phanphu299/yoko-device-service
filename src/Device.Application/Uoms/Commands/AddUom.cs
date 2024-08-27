using System;
using System.Linq.Expressions;
using Device.Application.Constants;
using Device.Application.Uom.Command.Model;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Uom.Command
{
    public class AddUom : UpsertTagCommand, IRequest<AddUomsDto>
    {
        #region Properties
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public double? RefFactor { get; set; }
        public double? RefOffset { get; set; }
        public string LookupCode { get; set; }
        public string Description { get; set; }
        [DynamicValidation(RemoteValidationKeys.abbreviation)]
        public string Abbreviation { get; set; }
        public int? RefId { get; set; }

        #endregion

        #region Methods
        static Func<AddUom, Domain.Entity.Uom> Converter = Projection.Compile();
        private static Expression<Func<AddUom, Domain.Entity.Uom>> Projection
        {
            get
            {
                return element => new Domain.Entity.Uom
                {
                    Name = element.Name,
                    RefFactor = element.RefFactor,
                    RefOffset = element.RefOffset,
                    LookupCode = element.LookupCode,
                    Description = element.Description,
                    Abbreviation = element.Abbreviation,
                    RefId = element.RefId
                };
            }
        }

        public static Domain.Entity.Uom Create(AddUom model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }

        #endregion
    }
}
