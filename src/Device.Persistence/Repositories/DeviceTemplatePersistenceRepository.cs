using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Exception;
using Dapper;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;

namespace Device.Persistence.Repository
{
    public class DeviceTemplatePersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<DeviceTemplate, Guid>, IDeviceTemplateRepository, IReadDeviceTemplateRepository
    {
        private readonly DeviceDbContext _context;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DeviceTemplatePersistenceRepository(DeviceDbContext context, IDbConnectionFactory dbConnectionFactory) : base(context)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _context = context;
        }

        public override IQueryable<DeviceTemplate> AsFetchable()
        {
            return _context.DeviceTemplates.Include(x => x.Payloads).ThenInclude(x => x.Details).Include(x => x.EntityTags).AsNoTracking()
                                                .Where(x => !x.EntityTags.Any() || x.EntityTags.Any(a => a.EntityType == Privileges.DeviceTemplate.ENTITY_NAME))
                                                .Select(x => new Domain.Entity.DeviceTemplate
                                                {
                                                    Id = x.Id,
                                                    Name = x.Name,
                                                    Payloads = x.Payloads.Select(p => new TemplatePayload
                                                    {
                                                        Id = p.Id,
                                                        Details = p.Details.Select(d => new TemplateDetail { Id = d.Id, Key = d.Key, Name = d.Name }).ToList()
                                                    }).ToList(),
                                                    EntityTags = x.EntityTags
                                                });
        }

        public override Task<DeviceTemplate> FindAsync(Guid id)
        {
            return _context.DeviceTemplates.Include(x => x.Payloads).ThenInclude(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
        }

        protected override void Update(DeviceTemplate requestObject, DeviceTemplate targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.Deleted = requestObject.Deleted;
            targetObject.UpdatedUtc = DateTime.UtcNow;
        }

        public async Task<DeviceTemplate> AddEntityWithRelationAsync(DeviceTemplate e)
        {
            //if you add A and B and A depend on B, then B will be inserted before A => loop add for get correct order detail in input data
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    /*
                    * when template_detail had field enable = true will add metrics to metric table
                    */
                    DeviceTemplate entity = new DeviceTemplate
                    {
                        Deleted = false,
                        Name = e.Name,
                        TotalMetric = await GetTotalMetricTemplateAsync(e),
                        CreatedBy = e.CreatedBy
                    };
                    // cant return id with id was auto gen
                    await _context.Set<DeviceTemplate>().AddAsync(entity);
                    await Context.SaveChangesAsync();

                    if (e.Payloads == null || !e.Payloads.Any())
                        return entity;
                    foreach (TemplatePayload payloads in e.Payloads)
                    {
                        await ProcessAddOrUpdatePayloadWithOrderDetailAsync(payloads, entity.Id, true);
                    }

                    if (e.Bindings != null)
                        foreach (TemplateBinding binding in e.Bindings)
                        {
                            binding.TemplateId = entity.Id;
                            await _context.Set<TemplateBinding>().AddAsync(binding);
                            await _context.SaveChangesAsync();
                        }

                    await Context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return entity;
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
                }
            }
        }

        public virtual async Task<DeviceTemplate> UpdateEntityWithRelationAsync(Guid key, DeviceTemplate updateTemplate)
        {
            var trackingEntity = await AsQueryable()
                .Include(x => x.Payloads).ThenInclude(x => x.Details)
                .Include(x => x.Bindings)
                .Where(a => a.Id == key).FirstOrDefaultAsync();
            if (trackingEntity == null)
                return updateTemplate;
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    /*
                     * * Should upload the full of payload list / detail list
                     * => payload null = add new
                     * => payload was exist but not upload = delete that payload
                     * => payload was exist = update
                     * ==> like new add all
                     *
                     * * Metric will update to when template_detail had field enable = true/false => will not deleted metric when not enabled cause relation to anther table
                     * => enable = false :
                     * --- when metric_device was exist => dont delete metric + update metric_device.enable = false
                     * => enable = true :
                     * --- check exist => if not exist add metric and metric_device
                     */
                    Update(updateTemplate, trackingEntity);
                    //update total metric
                    trackingEntity.TotalMetric = await GetTotalMetricTemplateAsync(updateTemplate);
                    _context.Set<DeviceTemplate>().Update(trackingEntity);

                    await ProcessUpdateOrRemoveTrackingPayloadsAsync(trackingEntity, updateTemplate);

                    //add payload which has payloadID = null => add new payload
                    foreach (TemplatePayload payload in updateTemplate.Payloads)
                    {
                        if (payload.Id != 0)
                            continue;
                        await ProcessAddOrUpdatePayloadWithOrderDetailAsync(payload, trackingEntity.Id, false);
                    }

                    await ProcessUpdateOrRemoveTrackingBindingsAsync(trackingEntity, updateTemplate);

                    foreach (TemplateBinding binding in updateTemplate.Bindings.Where(binding => binding.Id == 0))
                    {
                        binding.TemplateId = trackingEntity.Id;
                        await _context.Set<TemplateBinding>().AddAsync(binding);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
                }
            }
            return updateTemplate;
        }

        private async Task ProcessUpdateOrRemoveTrackingPayloadsAsync(DeviceTemplate trackingEntity, DeviceTemplate updateTemplate)
        {
            foreach (TemplatePayload payload in trackingEntity.Payloads)
            {
                //check update data exist in DB
                TemplatePayload payloadCheck = updateTemplate.Payloads.Where(x => x.Id == payload.Id).FirstOrDefault();
                if (payloadCheck == null)
                {
                    _context.Set<TemplateDetail>().RemoveRange(payload.Details);
                    _context.Set<TemplatePayload>().Remove(payload);

                }
                else
                {
                    payload.JsonPayload = payloadCheck.JsonPayload;
                    _context.Set<TemplatePayload>().Update(payload);

                    //payloadDB in update data => update
                    await ProcessUpdateTrackingPayloadDetailsAsync(payload, payloadCheck);
                }
            }
        }

        private async Task ProcessUpdateTrackingPayloadDetailsAsync(TemplatePayload payload, TemplatePayload payloadCheck)
        {
            foreach (TemplateDetail detail in payload.Details)
            {
                //check update data exist in DB
                TemplateDetail detailCheck = payloadCheck.Details.Where(x => x.Id == detail.Id).FirstOrDefault();
                if (detailCheck == null)
                {
                    //detail not in update data => delete
                    _context.Set<TemplateDetail>().Remove(detail);
                }
                else
                {
                    //detail in update data => update
                    UpdateTemplateDetail(detail, detailCheck);
                }
            }

            //add detail which has detailID = null => add new detail
            foreach (TemplateDetail detail in payloadCheck.Details)
            {
                if (detail.Id != 0)
                    continue;
                _ = await AddTemplateDetailAsync(detail, payload.Id);
            }
        }

        private async Task ProcessUpdateOrRemoveTrackingBindingsAsync(DeviceTemplate trackingEntity, DeviceTemplate updateTemplate)
        {
            foreach (TemplateBinding binding in trackingEntity.Bindings)
            {
                TemplateBinding bindingCheck = updateTemplate.Bindings.Where(x => x.Id == binding.Id).FirstOrDefault();
                if (bindingCheck == null)
                {
                    _context.Set<TemplateBinding>().Remove(binding);
                }
                else
                {
                    if (binding.DataType != bindingCheck.DataType || binding.Key != bindingCheck.Key)
                    {
                        await UpdateRelatedAssetsAndAssetTemplates(trackingEntity.Id, binding, bindingCheck);
                    }

                    binding.DataType = bindingCheck.DataType;
                    binding.Key = bindingCheck.Key;
                    binding.DefaultValue = bindingCheck.DefaultValue;
                    _context.Set<TemplateBinding>().Update(binding);
                }
            }
        }

        private async Task UpdateRelatedAssetsAndAssetTemplates(Guid deviceTemplateId, TemplateBinding binding, TemplateBinding bindingCheck)
        {
            var relatedDeviceIds = await _context.Devices.AsNoTracking().Where(x => x.TemplateId == deviceTemplateId).Select(x => x.Id).ToListAsync();
            //update asset template command attributes
            var relatedTemplateAttributes = await _context.AssetAttributeTemplates.Include(x => x.AssetAttributeCommand)
                .Where(x => x.AssetAttributeCommand.DeviceTemplateId == deviceTemplateId && x.AssetAttributeCommand.MetricKey == binding.Key).ToListAsync();
            foreach (var attribute in relatedTemplateAttributes)
            {
                attribute.AssetAttributeCommand.MetricKey = bindingCheck.Key;
                attribute.DataType = bindingCheck.DataType;
                _context.AssetAttributeTemplates.Update(attribute);
            }
            //update asset command attributes
            var relatedAssetAttributes = await _context.AssetAttributes.Include(x => x.AssetAttributeCommand)
                .Where(x => relatedDeviceIds.Contains(x.AssetAttributeCommand.DeviceId) && x.AssetAttributeCommand.MetricKey == binding.Key).ToListAsync();
            foreach (var attribute in relatedAssetAttributes)
            {
                attribute.AssetAttributeCommand.MetricKey = bindingCheck.Key;
                attribute.DataType = bindingCheck.DataType;
                _context.AssetAttributes.Update(attribute);
            }
            //update asset command mappings
            var relatedTemplateAttributeIds = relatedTemplateAttributes.Select(x => x.Id).ToList();
            var relatedAttributeMappings = await _context.AssetAttributeCommandMappings
                                        .Where(x => relatedTemplateAttributeIds.Contains(x.AssetAttributeTemplateId)).ToListAsync();
            foreach (var mapping in relatedAttributeMappings)
            {
                mapping.MetricKey = bindingCheck.Key;
                _context.AssetAttributeCommandMappings.Update(mapping);
            }
        }

        public virtual async Task<DeviceTemplate> FindEntityWithRelationAsync(Guid id)
        {
            DeviceTemplate entity = await AsQueryable()
                .Include(x => x.Devices)
                .Include(x => x.Bindings)
                .Include(x => x.Payloads).ThenInclude(x => x.Details).ThenInclude(x => x.TemplateKeyType)
                .Include(x => x.Payloads).ThenInclude(x => x.Details)
                .Include(x => x.Bindings)
                .Include(x => x.EntityTags)
                .Where(a => a.Id == id && (!a.EntityTags.Any() || a.EntityTags.Any(e => e.EntityType == Privileges.DeviceTemplate.ENTITY_NAME))).FirstOrDefaultAsync();
            return entity;
        }

        public async Task<bool> ValidationAttributeUsingMetricsAsync(DeviceTemplate template, string key)
        {
            var result = false;
            var templatePayloads = template.Payloads;
            if (templatePayloads != null && templatePayloads.Any())
            {
                foreach (var item in templatePayloads)
                {
                    var templateDetailKeys = item.Details.Where(x => x.Key == key && x.Enabled && x.KeyTypeId == (int)MetricId.metric).Select(x => x.Key);
                    var attrDynamic = await _context.AssetAttributeDynamic.FirstOrDefaultAsync(x => templateDetailKeys.Contains(x.MetricKey));
                    if (attrDynamic != null)
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        private async Task ProcessAddOrUpdatePayloadWithOrderDetailAsync(TemplatePayload payload, Guid templateId, bool isAdd)
        {
            payload.TemplateId = templateId;
            TemplatePayload entityPayload = new TemplatePayload
            {
                JsonPayload = payload.JsonPayload,
                TemplateId = templateId
            };
            await _context.Set<TemplatePayload>().AddAsync(entityPayload);
            await Context.SaveChangesAsync();

            if (payload.Details == null || !payload.Details.Any())
                return;
            foreach (TemplateDetail details in payload.Details)
            {
                _ = await AddTemplateDetailAsync(details, entityPayload.Id);
            }
        }

        private async Task<TemplateDetail> AddTemplateDetailAsync(TemplateDetail detail, int payloadId)
        {
            TemplateDetail entityDetails = detail;
            detail.TemplatePayloadId = payloadId;
            await _context.Set<TemplateDetail>().AddAsync(entityDetails);
            await Context.SaveChangesAsync();
            return entityDetails;

        }

        private void UpdateTemplateDetail(TemplateDetail detail, TemplateDetail detailCheck)
        {
            detail.Key = detailCheck.Key;
            detail.Name = detailCheck.Name;
            detail.KeyTypeId = detailCheck.KeyTypeId;
            detail.DataType = detailCheck.DataType;
            detail.Expression = detailCheck.Expression;
            detail.Enabled = detailCheck.Enabled;
            detail.ExpressionCompile = detailCheck.ExpressionCompile;
            detail.DetailId = detailCheck.DetailId;
            _context.Set<TemplateDetail>().Update(detail);

        }

        private async Task<int> GetTotalMetricTemplateAsync(DeviceTemplate template)
        {
            int countTotalMetric = 0;
            ICollection<TemplateKeyType> keyTypes = await _context.Set<TemplateKeyType>().AsQueryable()
                                                               .Where(x => x.Name == TemplateKeyTypeConstants.AGGREGATION || x.Name == TemplateKeyTypeConstants.METRIC)
                                                               .ToListAsync();
            if (template.Payloads == null || !template.Payloads.Any())
                return 0;
            foreach (TemplatePayload payload in template.Payloads)
            {
                if (payload.Details == null || !payload.Details.Any())
                    continue;
                foreach (TemplateDetail detail in payload.Details)
                {
                    if (!detail.Enabled || !keyTypes.Any(x => x.Id == detail.KeyTypeId))
                        continue;
                    countTotalMetric++;
                }
            }

            return countTotalMetric;
        }

        public async Task<bool> HasBindingAsync(Guid id)
        {
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryFirstOrDefaultAsync<bool>("select 1 from template_bindings where device_template_id = @TemplateId"
                , new { TemplateId = id });
                connection.Close();
                return result;
            }
        }

        public async Task RetrieveAsync(IEnumerable<DeviceTemplate> input)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            var tableSeeds = CalculateIdentitySeeds(input);
            var builder = new System.Text.StringBuilder();
            foreach (var table in tableSeeds.Keys)
            {
                builder.Append($"ALTER TABLE {table} ALTER COLUMN id DROP IDENTITY;");
            }
            var triggerSql = builder.ToString();
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
            await _context.DeviceTemplates.AddRangeAsync(input);
            await _context.SaveChangesAsync();

            builder.Clear();
            foreach (var item in tableSeeds)
            {
                builder.Append($"ALTER TABLE {item.Key} ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (START WITH {item.Value});");
            }
            triggerSql = builder.ToString();
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
        }

        private IDictionary<string, int> CalculateIdentitySeeds(IEnumerable<DeviceTemplate> input)
        {
            var bindings = input.SelectMany(x => x.Bindings);
            var payloads = input.SelectMany(x => x.Payloads);
            var details = payloads.SelectMany(x => x.Details);
            var maxBindingId = bindings.Any() ? bindings.Max(x => x.Id) : 0;
            var maxPayloadId = payloads.Any() ? payloads.Max(x => x.Id) : 0;
            var maxDetailId = details.Any() ? details.Max(x => x.Id) : 0;

            return new Dictionary<string, int>
            {
                {"template_bindings", maxBindingId + 1},
                {"template_payloads", maxPayloadId + 1},
                {"template_details", maxDetailId + 1}
            };
        }

        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();
    }
}
