using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Application.Uom.Command;
using Device.Application.Uom.Command.Model;
using Device.ApplicationExtension.Extension;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Device.Application.Helper;
using AHI.Infrastructure.Service.Tag.Extension;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;

namespace Device.Application.Service
{
    public class UomService : BaseSearchService<Domain.Entity.Uom, int, GetUomByCriteria, GetUomDto>, IUomService
    {
        private readonly IUomUnitOfWork _unitOfWork;
        private readonly IReadUomRepository _readUomRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IUserContext _userContext;
        private readonly IFileEventService _fileEventService;
        private readonly IAuditLogService _auditLogService;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly ITagService _tagService;
        private readonly IValidator<ArchiveUomDto> _validator;

        public UomService(
            IServiceProvider serviceProvider,
            IUomUnitOfWork repository,
            IReadUomRepository readUomRepository,
            ITenantContext tenantContext,
            IUserContext userContext,
            IFileEventService fileEventService,
            IAuditLogService auditLogService,
            DeviceBackgroundService deviceBackgroundService,
            ITagService tagService,
            IValidator<ArchiveUomDto> validator)
            : base(GetUomDto.Create, serviceProvider)
        {
            _unitOfWork = repository;
            _readUomRepository = readUomRepository;
            _tenantContext = tenantContext;
            _userContext = userContext;
            _fileEventService = fileEventService;
            _auditLogService = auditLogService;
            _deviceBackgroundService = deviceBackgroundService;
            _validator = validator;
            _tagService = tagService;
        }

        public async Task<AddUomsDto> AddUomAsync(AddUom command, CancellationToken token)
        {
            // Uom is duplicated Name and abb.
            var isCheckDuplicate = await FindDuplicateUomsNameAbbAsync(command.Name, command.Abbreviation, token);

            if (isCheckDuplicate.Any())
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Uom.Name));

            // Uom is duplicated.
            var isCheckDuplicateName = await FindDuplicateUomsAsync(command.Name, token);

            if (isCheckDuplicateName.Any())
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Uom.Name));

            // Uom is duplicated.
            var isCheckDuplicateAbb = await ValidateAbbreviationAsync(command.RefId, command.Abbreviation);

            if (isCheckDuplicateAbb)
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Uom.Abbreviation));

            // Select Ref UoM. Value list: query list UOM Abbreviation has the same Category (except the UoM that is being added/edited).
            //add logic refUom , If  Ref UoM is blank : Value of them: Factor = Canonical Factor = 1, Offset = Canonical Factor = 0 => that is deafault
            if (command.RefId != null)
            {
                // Uom ref is not found.
                var hasValidUomReference = await _readUomRepository.AsQueryable()
                    .Where(x => x.LookupCode.ToLower() == command.LookupCode.ToLower() &&
                                x.Id == command.RefId.Value)
                    .AnyAsync(token);

                if (!hasValidUomReference)
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.Uom.RefId));
            }

            var entity = AddUom.Create(command);
            entity.CreatedBy = _userContext.Upn;
            if (entity.RefId != null)
            {
                var refUom = await _readUomRepository.AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.RefId);
                entity.CanonicalFactor = DoubleExtension.Multiply(entity.RefFactor ?? 1, refUom.CanonicalFactor ?? 1);
                entity.CanonicalOffset = entity.RefOffset + refUom.CanonicalOffset;
            }
            else
            {
                entity.RefFactor = 1;
                entity.RefOffset = 0;
            }

            Domain.Entity.Uom entityResult = null;
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                entityResult = await _unitOfWork.Uoms.AddAsync(entity);
                entityResult.ResourcePath = string.Format(ObjectBaseConstants.RESOURCE_PATH, entityResult.Id);

                var tagIds = Array.Empty<long>();
                command.Upn = _userContext.Upn;
                command.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);
                if (command.Tags != null && command.Tags.Any())
                {
                    tagIds = await _tagService.UpsertTagsAsync(command);
                }
                var entityId = EntityTagHelper.GetEntityId(entityResult.Id);
                entityResult.EntityTags = EntityTagHelper.GetEntityTags(EntityTypeConstants.UOM, tagIds, entityId);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            return AddUomsDto.Create(entityResult);
        }

        public async Task<GetUomDto> FindUomByIdAsync(GetUomById command, CancellationToken token)
        {
            var uom = await _readUomRepository.AsQueryable()
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(x => x.Id == command.Id);
            if (uom == null || uom.Deleted)
                throw new EntityNotFoundException();

            return await _tagService.FetchTagsAsync(GetUomDto.Create(uom));
        }

        public async Task<UpdateUomsDto> UpdateUomAsync(UpdateUom command, CancellationToken token)
        {
            // Uom is not found.
            var uom = await _readUomRepository.AsQueryable()
                                              .FirstOrDefaultAsync(x => x.Id == command.Id);
            if (uom == null || uom.Deleted)
                throw new EntityNotFoundException();

            // Uom is duplicated Name and abb.
            var isCheckDuplicate = await FindDuplicateUomsNameAbbAsync(command.Name, command.Abbreviation, token);

            if (isCheckDuplicate.Any())
            {
                var isExist = isCheckDuplicate.FirstOrDefault(x => x.Id == command.Id);
                if (isExist == null)
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Uom.Name));
            }

            // Uom is duplicated.
            var isCheckDuplicateName = await FindDuplicateUomsAsync(command.Name, token);

            if (isCheckDuplicateName.Any())
            {
                var isExist = isCheckDuplicateName.FirstOrDefault(x => x.Id == command.Id);
                if (isExist == null)
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Uom.Name));
            }

            // Uom is duplicated.
            var isCheckDuplicateAbb = await ValidateAbbreviationAsync(command.Id, command.Abbreviation);

            if (isCheckDuplicateAbb)
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.Uom.Abbreviation));

            // Select Ref UoM. Value list: query list UOM Abbreviation has the same Category (except the UoM that is being added/edited).
            if (command.RefId != null)
            {
                // Uom ref is not found.
                var hasValidUomReference = await _readUomRepository.AsQueryable()
                    .Where(x => x.LookupCode.ToLower() == command.LookupCode.ToLower() &&
                                x.Id == command.RefId.Value)
                    .AnyAsync(token);

                if (!hasValidUomReference)
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.Uom.RefId));

                //check loop refUom
                await ValidRefUomDeadlyAsync(command.RefId, command.Id);
            }

            var entity = UpdateUom.Create(command);
            if (entity.RefId != null)
            {
                var refUom = await _readUomRepository.AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.RefId);
                entity.CanonicalFactor = DoubleExtension.Multiply(entity.RefFactor ?? 1, refUom.CanonicalFactor ?? 1);
                entity.CanonicalOffset = entity.RefOffset + refUom.CanonicalOffset;
            }
            else
            {
                entity.RefFactor = 1;
                entity.RefOffset = 0;
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                command.Upn = _userContext.Upn;
                command.ApplicationId = Guid.Parse(_userContext.ApplicationId ?? ApplicationInformation.APPLICATION_ID);

                var isSameTag = command.IsSameTags(uom.EntityTags);
                if (!isSameTag)
                {
                    await _unitOfWork.EntityTags.RemoveByEntityIdAsync(EntityTypeConstants.UOM, uom.Id, true);

                    var tagIds = await _tagService.UpsertTagsAsync(command);
                    if (tagIds.Any())
                    {
                        var entityId = EntityTagHelper.GetEntityId(command.Id);
                        var entitiesTags = EntityTagHelper.GetEntityTags(EntityTypeConstants.UOM, tagIds, entityId).ToArray();
                        await _unitOfWork.EntityTags.AddRangeWithSaveChangeAsync(entitiesTags);
                    }
                }
                await _unitOfWork.Uoms.UpdateAsync(entity);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache());
            return UpdateUomsDto.Create(entity);
        }

        public async Task<BaseResponse> RemoveUomAsync(DeleteUom command, CancellationToken token)
        {
            if (command?.Ids == null || command.Ids.Length < 1)
                return BaseResponse.Success;

            var ids = command.Ids.Distinct().ToArray();
            var names = new List<string>();
            bool result = false;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                //check deleting id in uoms like {id::number}
                var uoms = await _readUomRepository.AsQueryable()
                                                    .Where(x => ids.Contains(x.Id) && !x.Deleted)
                                                    .ToArrayAsync(cancellationToken: token);
                names.AddRange(uoms.Select(x => x.Name));
                if (ids.Length > uoms.Length)
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }

                var toBeDeletedUoms = uoms.Select(x => new Domain.Entity.Uom() { Id = x.Id }).ToList();
                result = await _unitOfWork.Uoms.RemoveListEntityWithRelationAsync(toBeDeletedUoms);

                await _unitOfWork.EntityTags.RemoveByEntityIdsAsync(EntityTypeConstants.UOM, ids, true);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Delete, ActionStatus.Fail, command.Ids, names, payload: command);
                throw;
            }

            await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanAssetCache());
            await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Delete, ActionStatus.Success, command.Ids, names, payload: command);

            return new BaseResponse(result, "");
        }

        public async Task<ActivityResponse> ExportAsync(ExportUom request, CancellationToken cancellationToken)
        {
            try
            {
                var ids = request.Ids.Select(x => Convert.ToInt32(x));
                var existingEntityCount = await _readUomRepository.AsQueryable().AsNoTracking().CountAsync(x => ids.Contains(x.Id));
                if (existingEntityCount < ids.Count())
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }
                await _fileEventService.SendExportEventAsync(request.ActivityId, request.ObjectType, request.Ids);
                return new ActivityResponse(request.ActivityId);
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.UOM, ActionType.Export, ActionStatus.Fail, request.Ids, payload: request);
                throw;
            }
        }

        public override async Task<BaseSearchResponse<GetUomDto>> SearchAsync(GetUomByCriteria criteria)
        {
            criteria.MappingSearchTags();
            var response = await base.SearchAsync(criteria);
            return await _tagService.FetchTagsAsync(response);
        }

        public async Task<CalculationRefUomDto> CalculationRefUomAsync(CalculationRefUom request, CancellationToken cancellationToken)
        {
            //Canonical_Factor = Factor * Ref_Canonical_Factor
            //Canonical_Offset =  Offset + Ref_Canonical_Offset
            var refUom = await _readUomRepository.AsQueryable().AsNoTracking().Where(x => x.Id == request.RefId).FirstOrDefaultAsync();
            if (refUom == null)
                throw ValidationExceptionHelper.GenerateRequiredValidation(nameof(CalculationRefUom.RefId));

            //check dead loop
            //await ValidRefUomDeadlyAsync(refUom.Id);

            var canonicalFactor = DoubleExtension.Multiply(request.Factor, (refUom.CanonicalFactor ?? 1));
            // if (double.IsInfinity(canonicalFactor))
            //     throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(CalculationRefUom.Factor));

            var canonicalOffset = request.Offset + refUom.CanonicalOffset ?? 0;
            // if (double.IsInfinity(canonicalOffset))
            //     throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(CalculationRefUom.Offset));

            var result = new CalculationRefUomDto
            {
                RefUom = GetUomDto.Create(refUom),
                Factor = StringExtension.ToLongString(request.Factor),
                Offset = StringExtension.ToLongString(request.Offset),
                CanonicalFactor = StringExtension.ToLongString(canonicalFactor),
                CanonicalOffset = StringExtension.ToLongString(canonicalOffset)
            };

            return result;
        }

        protected override Type GetDbType() { return typeof(IUomRepository); }

        /// <summary>
        /// Find uom by search for its name.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<Domain.Entity.Uom[]> FindDuplicateUomsAsync(string name, CancellationToken cancellationToken = default)
        {
            var uoms = _readUomRepository.AsQueryable().AsNoTracking().Where(x => x.Name.ToLower() == name.ToLower());
            return await uoms.ToArrayAsync(cancellationToken);
        }

        private Task<bool> ValidateAbbreviationAsync(int? id, string abbr)
        {
            var result = _readUomRepository.AsQueryable().AsNoTracking().Where(x => x.Abbreviation.ToLower() == abbr.ToLower());
            if (id != null)
            {
                result = result.Where(x => x.Id != id);
            }
            return result.AnyAsync();

        }
        private async Task ValidRefUomDeadlyAsync(int? refId, int id = 0)
        {
            if (!refId.HasValue)
                return;

            var refs = new List<int>();
            await GetRefUomRootAsync(refs, refId);
            // a(root) - b.ref(a) - c.ref(b) - d.ref(c): update a: a -> a.ref(d): list ref(d,c,b,a).Contains(a) => add nerver got that
            if (refs.Contains(id))
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.Uom.RefUom), MessageConstants.UOM_CIRCULAR_REFERENCE);
        }
        private async Task GetRefUomRootAsync(List<int> refs, int? refId)
        {
            var entity = await _readUomRepository.AsQueryable().AsNoTracking().Include(x => x.RefUom).Where(x => x.Id == refId).FirstOrDefaultAsync();
            if (entity == null)
                return;

            refs.Add(entity.Id);
            if (entity.RefUom == null)
                return;

            await GetRefUomRootAsync(refs, entity.RefId);
            return;
        }

        protected virtual async Task<Domain.Entity.Uom[]> FindDuplicateUomsNameAbbAsync(string name, string abb, CancellationToken cancellationToken = default)
        {
            return await _readUomRepository.AsQueryable().AsNoTracking().Where(x => x.Name.ToLower() == name.ToLower() && x.Abbreviation.ToLower() == abb.ToLower()).ToArrayAsync(cancellationToken);
            //return uoms;//await uoms.ToArrayAsync(cancellationToken);
        }

        public Task<BaseResponse> CheckExistUomsAsync(CheckExistUom command, CancellationToken cancellationToken)
        {
            return ValidateExistUomsAsync(command, cancellationToken);
        }

        private async Task<BaseResponse> ValidateExistUomsAsync(CheckExistUom command, CancellationToken cancellationToken)
        {
            var requestIds = new HashSet<int>(command.Ids.Distinct());
            var uoms = new HashSet<int>(await _readUomRepository.AsQueryable().AsNoTracking().Where(x => requestIds.Any(i => i == x.Id)).Select(x => x.Id).ToListAsync());
            if (!requestIds.SetEquals(uoms))
                throw new EntityNotFoundException();
            return BaseResponse.Success;
        }

        public async Task<IEnumerable<ArchiveUomDto>> ArchiveAsync(ArchiveUom command, CancellationToken cancellationToken)
        {
            var uoms = await _readUomRepository.AsQueryable().AsNoTracking()
                                            .Where(x => x.System == false)
                                            .Where(x => x.UpdatedUtc <= command.ArchiveTime)
                                            .Select(x => ArchiveUomDto.Create(x)).ToListAsync();
            return uoms;
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveUom command, CancellationToken cancellationToken)
        {
            _userContext.SetUpn(command.Upn);
            var data = JsonConvert.DeserializeObject<ArchiveUomDataDto>(command.Data);
            if (!data.Uoms.Any())
            {
                return BaseResponse.Success;
            }
            var uoms = data.Uoms.OrderBy(x => x.UpdatedUtc).Select(x => ArchiveUomDto.CreateEntity(x));
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Uoms.RetrieveAsync(uoms);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyUom command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<ArchiveUomDataDto>(command.Data);
            foreach (var uom in data.Uoms)
            {
                var validation = await _validator.ValidateAsync(uom);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }
    }
}
