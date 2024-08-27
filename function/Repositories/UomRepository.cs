using System;
using System.Net.Http;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Model.SearchModel;
using AHI.Infrastructure.Import.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using System.Text.RegularExpressions;
using AHI.Device.Function.FileParser.Abstraction;
using System.Linq;
using Function.Extension;
using DisplayPropertyName = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.Uom;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;

namespace AHI.Infrastructure.Repository
{
    public class UomRepository : IImportRepository<Uom>
    {
        private readonly ErrorType _errorType = ErrorType.DATABASE;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly IUserContext _userContext;
        private readonly IImportTrackingService _errorService;
        private readonly ITagService _tagService;
        private readonly NameValidator _abbrValidator;
        private readonly TagValidator _tagValidator;

        public UomRepository(
            IConfiguration configuration,
            IHttpClientFactory factory,
            ITenantContext tenantContext,
            IUserContext userContext,
            ITagService tagService,
            IDictionary<string, IImportTrackingService> errorHandlers)
        {
            _configuration = configuration;
            _httpClientFactory = factory;
            _tenantContext = tenantContext;
            _userContext = userContext;
            _tagService = tagService;
            _errorService = errorHandlers[MimeType.EXCEL];

            _abbrValidator = new NameValidator("uoms", "abbreviation", new FilterCondition("deleted", "false"));
            _abbrValidator.Seperator = ' ';
            _tagValidator = new TagValidator(_errorService);
        }

        public async Task CommitAsync(IEnumerable<Uom> source)
        {
            // if any error detected when parsing data in any sheet, discard all file
            if (_errorService.HasError)
                return;

            if (!await ValidateUomLookupAsync(source))
                return;

            //sort source for order blank field go first
            var UomsWithNoRef = source.Where(x => string.IsNullOrEmpty(x.RefName));
            var UomsWithRef = source.Except(UomsWithNoRef);
            IEnumerable<Uom> sortedSource = UomsWithNoRef.Concat(UomsWithRef);

            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);

            bool success = true;
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    foreach (var uom in sortedSource)
                    {
                        try
                        {
                            if (!await ValidateUomAsync(uom, connection, transaction))
                            {
                                success = false;
                                continue;
                            }

                            var insertQuery = $@"INSERT INTO uoms(name, lookup_code, ref_id, ref_factor, ref_offset, canonical_factor, canonical_offset, abbreviation, created_by)
                                                 VALUES (@Name, @LookupCode, @RefId, @RefFactor, @RefOffset, @CanonicalFactor, @CanonicalOffset, @Abbreviation, @CreatedBy) RETURNING uoms.id";
                            var updateQuery = $@"UPDATE uoms SET
                                                     name = @Name,
                                                     lookup_code = @LookupCode,
                                                     ref_id = @RefId,
                                                     ref_factor = @RefFactor,
                                                     ref_offset = @RefOffset,
                                                     canonical_factor = @CanonicalFactor,
                                                     canonical_offset = @CanonicalOffset,
                                                     abbreviation = @Abbreviation,
                                                     deleted = @Deleted,
                                                     updated_utc = @UpdatedUtc
                                                 WHERE id = @Id";
                            var newUomId = await connection.ExecuteScalarAsync<int>(uom.Id is null ? insertQuery : updateQuery, new
                            {
                                Name = uom.Name,
                                LookupCode = uom.Lookup,
                                RefId = uom.RefId,
                                RefFactor = uom.RefFactor,
                                RefOffset = uom.RefOffset,
                                CanonicalFactor = uom.CanonicalFactor,
                                CanonicalOffset = uom.CanonicalOffset,
                                Abbreviation = uom.Abbreviation,
                                Deleted = false,
                                Id = uom.Id,
                                UpdatedUtc = System.DateTime.UtcNow,
                                CreatedBy = _userContext.Upn
                            },
                            transaction, commandTimeout: 600);

                            if (uom.Id is null)
                            {
                                var updateResourcePathQuery = $@"UPDATE uoms SET resource_path = @ResourcePath WHERE id = @Id";
                                await connection.ExecuteAsync(updateResourcePathQuery, new { ResourcePath = string.Format(ObjectBaseConstants.RESOURCE_PATH, newUomId), Id = newUomId });
                            }

                            if (!string.IsNullOrWhiteSpace(uom.Tags))
                            {

                                if (!_tagValidator.ValidateTags(uom.Tags, uom))
                                {
                                    success = false;
                                    continue;
                                }

                                var upsertTag = new UpsertTagCommand
                                {
                                    Upn = _userContext.Upn,
                                    ApplicationId = Guid.Parse(ApplicationInformation.APPLICATION_ID),
                                    IgnoreNotFound = true, // NOTE: DO NOT ADD NEW TAG IF NOT EXIST IN PROJECT
                                    Tags = uom.Tags.Split(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR)
                                                                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                                                                .Select(tag => new UpsertTag
                                                                {
                                                                    Key = tag.Split(":")[0].Trim(),
                                                                    Value = tag.Split(":")[1].Trim()
                                                                })
                                };
                                var tagIds = await _tagService.UpsertTagsAsync(upsertTag);

                                if (tagIds.Any())
                                {
                                    var uomId = uom.Id is null ? newUomId : uom.Id;

                                    var updateTagBindingQuery = $@"
                                                           INSERT INTO entity_tags (entity_id_int, entity_type, tag_id)
                                                           VALUES (@Id, @EntityType, @TagId)";
                                    await connection.ExecuteAsync(updateTagBindingQuery, tagIds.Distinct().Select(tagId => new { Id = uomId, EntityType = nameof(Uom), TagId = tagId }), transaction, commandTimeout: 600);
                                }
                            }
                        }
                        catch (DbException e)
                        {
                            _errorService.RegisterError(e.Message, uom, null, _errorType);
                            success = false;
                        }
                    }

                    await (success ? transaction.CommitAsync() : transaction.RollbackAsync());
                }
                await connection.CloseAsync();
            }
        }

        public Task CommitAsync(IEnumerable<Uom> source, Guid correlationId)
        {
            throw new NotImplementedException();
        }

        private async Task<bool> ValidateUomAsync(Uom uom, IDbConnection connection, IDbTransaction transaction)
        {
            if (!await ValidateRefUomAsync(uom, connection, transaction))
                return false;

            await ValidateDuplicateAsync(uom, connection, transaction);
            return true;
        }

        private async Task<bool> ValidateUomLookupAsync(IEnumerable<Uom> uoms)
        {
            var names = uoms.Select(uom => uom.Lookup).Distinct();
            var query = new FilteredSearchQuery(
                FilteredSearchQuery.LogicalOp.Or,
                filterObjects: names.Select(name => new SearchFilter("name", $"{name}")).ToArray()
            );
            IDictionary<string, string> lookupInfos;
            try
            {
                var client = _httpClientFactory.CreateClient(ClientNameConstant.CONFIGURATION_SERVICE, _tenantContext);
                var response = await client.SearchAsync<LookupInfo>("cnm/lookups/search", query);

                lookupInfos = response.Data.Where(info => info.Active && info.LookupType.Id == "UOM").ToDictionary(info => info.Name, info => info.Id);
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("Failed to get categories");
            }

            var notFoundLookup = new HashSet<string>();
            foreach (var uom in uoms)
            {
                if (lookupInfos.TryGetValue(uom.Lookup, out var id))
                    uom.Lookup = id;
                else
                    notFoundLookup.Add(uom.Lookup);
            }
            if (notFoundLookup.Any())
            {
                foreach (var lookup in notFoundLookup)
                    foreach (var uom in uoms.Where(uom => uom.Lookup == lookup))
                        _errorService.RegisterError(ValidationMessage.NOT_EXIST, uom, nameof(Uom.Lookup), _errorType, new Dictionary<string, object>
                        {
                            { "propertyName", DisplayPropertyName.LOOKUP },
                            { "propertyValue", uom.Lookup }
                        });

                return false;
            }
            return true;
        }

        private async Task<bool> ValidateRefUomAsync(Uom uom, IDbConnection connection, IDbTransaction transaction)
        {
            if (uom.RefName is null || uom.RefName.Equals(uom.Abbreviation, StringComparison.InvariantCultureIgnoreCase))
            {
                if (uom.RefFactor == null || uom.CanonicalFactor == null)
                    uom.RefFactor = uom.CanonicalFactor = 1;
                if (uom.RefOffset == null || uom.CanonicalOffset == null)
                    uom.RefOffset = uom.CanonicalOffset = 0;

                return true;
            }

            var query = $@"SELECT id, canonical_factor as CanonicalFactor, canonical_offset as CanonicalOffset FROM uoms WHERE abbreviation ILIKE @Abbreviation ESCAPE '\' AND lookup_code = @LookupCode";
            Uom refUom;
            try
            {
                refUom = await connection.QueryFirstAsync<Uom>(query, new
                {
                    Abbreviation = NameValidator.EscapePattern(uom.RefName),
                    LookupCode = uom.Lookup
                },
                transaction, commandTimeout: 600);
            }
            catch (InvalidOperationException)
            {
                _errorService.RegisterError(ValidationMessage.NOT_EXIST, uom, nameof(Uom.RefName), _errorType, new Dictionary<string, object>
                {
                    { "propertyName", DisplayPropertyName.REF_NAME },
                    { "propertyValue", uom.RefName }
                });
                return false;
            }

            // Loop: a(root) - b.ref(a) - c.ref(b) - d.ref(c): update a: a -> a.ref(d): list ref(d,c,b,a).Contains(a) => add never got that

            uom.RefId = refUom.Id;
            //calc Canonical
            uom.CanonicalFactor = DoubleValueExtensions.Multiply(uom.RefFactor.Value, refUom.CanonicalFactor.Value);
            uom.CanonicalOffset = uom.RefOffset + refUom.CanonicalOffset;
            return true;
        }

        private async Task ValidateDuplicateAsync(Uom uom, IDbConnection connection, IDbTransaction transaction)
        {
            uom.Abbreviation = await ValidateDuplicateAbbreviationAsync(uom, connection);
            uom.Name = await ValidateDuplicateNameAsync(uom, connection, transaction);
        }

        private async Task<string> ValidateDuplicateNameAsync(Uom uom, IDbConnection connection, IDbTransaction transaction)
        {
            var trailingSuffixRegex = new Regex($"( copy)+$", RegexOptions.IgnoreCase);
            var nonSuffix = trailingSuffixRegex.Replace(uom.Name, string.Empty);

            // Check duplicate name, exclude the deleted name if exists (which will be overwrite)
            var deleted = await ValidateDeletedUomAsync(uom, connection, transaction);
            var query = $@"SELECT name FROM uoms WHERE (name ILIKE @Name OR name ILIKE @Pattern ESCAPE '\')";
            if (deleted != null)
            {
                uom.Id = deleted.Id;
                query = string.Concat(query, $" AND id != @Id");
            }
            var duplicateCandidates = await connection.QueryAsync<string>(query, new
            {
                Name = NameValidator.EscapePattern(nonSuffix),
                Pattern = NameValidator.GetCopyPattern(nonSuffix, " "),
                Id = uom.Id
            },
            transaction, commandTimeout: 600);

            var regex = new Regex($"^{NameValidator.EscapeRegex(nonSuffix)}( copy)*$", RegexOptions.IgnoreCase);
            var duplicate = new List<int>(
                duplicateCandidates.Where(name => regex.IsMatch(name))
                                   .Select(name => Regex.Matches(trailingSuffixRegex.Match(name).Value, "copy", RegexOptions.IgnoreCase).Count));

            var offset = Regex.Matches(trailingSuffixRegex.Match(uom.Name).Value, "copy", RegexOptions.IgnoreCase).Count;
            return NameValidator.AppendCopy(uom.Name, " ", duplicate, offset);
        }

        private Task<string> ValidateDuplicateAbbreviationAsync(Uom uom, IDbConnection connection)
        {
            return _abbrValidator.ValidateDuplicateNameAsync(uom.Abbreviation, connection);
        }

        private Task<DeletedInfo> ValidateDeletedUomAsync(Uom uom, IDbConnection connection, IDbTransaction transaction)
        {
            var query = $@"SELECT id, name FROM uoms WHERE deleted = 'true' AND abbreviation ILIKE @Abbreviation ESCAPE '\'";
            return connection.QueryFirstOrDefaultAsync<DeletedInfo>(query, new { Abbreviation = NameValidator.EscapePattern(uom.Abbreviation) }, transaction, commandTimeout: 600);
        }
    }

    class LookupInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public LookupTypeInfo LookupType { get; set; }
    }

    class LookupTypeInfo
    {
        public string Id { get; set; }
    }

    class DeletedInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}