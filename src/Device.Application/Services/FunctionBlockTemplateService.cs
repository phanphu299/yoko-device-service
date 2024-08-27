using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Block.Command.Model;
using Device.Application.BlockTemplate.Command;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.BlockTemplate.Query;
using Device.Application.Constant;
using Device.Application.Model;
using Device.Application.Repositories;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using Device.Domain.Entity;
using FluentValidation;
using Newtonsoft.Json;
using static Device.Application.BlockTemplate.Command.Model.ConnectorInfo;
using static Device.Application.BlockTemplate.Command.Model.FunctionBlockLinkDto;

namespace Device.Application.Service
{
    public class FunctionBlockTemplateService : BaseSearchService<Domain.Entity.FunctionBlockTemplate, Guid, GetFunctionBlockTemplateByCriteria, FunctionBlockTemplateSimpleDto>, IFunctionBlockTemplateService
    {
        private readonly IBlockFunctionUnitOfWork _blockFunctionUnitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly ITenantContext _tenantContext;
        private readonly ICache _cache;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly IUserContext _userContext;
        private readonly IValidator<ArchiveBlockTemplateDto> _validator;
        private readonly IReadFunctionBlockRepository _readFunctionBlockRepository;
        private readonly IReadFunctionBlockTemplateRepository _readFunctionBlockTemplateRepository;
        private readonly IReadFunctionBlockExecutionRepository _readFunctionBlockExecutionRepository;
        private readonly string[] _inBinding = { BindingTypeConstants.INPUT, BindingTypeConstants.INOUT };

        public FunctionBlockTemplateService(IServiceProvider serviceProvider
            , IAuditLogService auditLogService
            , ITenantContext tenantContext
            , ICache cache
            , IBlockFunctionUnitOfWork blockFunctionUnitOfWork
            , DeviceBackgroundService deviceBackgroundService
            , IValidator<ArchiveBlockTemplateDto> validator
            , IUserContext userContext
            , IReadFunctionBlockRepository readFunctionBlockRepository
            , IReadFunctionBlockTemplateRepository readFunctionBlockTemplateRepository
            , IReadFunctionBlockExecutionRepository readFunctionBlockExecutionRepository) : base(FunctionBlockTemplateSimpleDto.Create, serviceProvider)
        {
            _blockFunctionUnitOfWork = blockFunctionUnitOfWork;
            _auditLogService = auditLogService;
            _tenantContext = tenantContext;
            _cache = cache;
            _deviceBackgroundService = deviceBackgroundService;
            _userContext = userContext;
            _readFunctionBlockRepository = readFunctionBlockRepository;
            _readFunctionBlockTemplateRepository = readFunctionBlockTemplateRepository;
            _readFunctionBlockExecutionRepository = readFunctionBlockExecutionRepository;
            _validator = validator;
        }

        protected override Type GetDbType() { return typeof(IFunctionBlockTemplateRepository); }

        public async Task<IEnumerable<GetFunctionBlockDto>> GetFunctionBlocksByTemplateIdAsync(GetFunctionBlockByTemplateId command, CancellationToken cancellationToken)
        {
            var response = new List<GetFunctionBlockDto>();
            var templates = await _readFunctionBlockTemplateRepository.AsQueryable().AsNoTracking().Where(x => x.Id == command.Id).SelectMany(x => x.Nodes.Select(node => node.FunctionBlock)).ToListAsync();
            return templates.Select(GetFunctionBlockDto.Create);
        }

        public async Task<FunctionBlockTemplateDto> AddBlockTemplateAsync(AddFunctionBlockTemplate command, CancellationToken cancellationToken)
        {
            try
            {
                if (await IsDuplicateTemplateAsync(Guid.Empty, command.Name))
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(AddFunctionBlockTemplate.Name));
                var contentDetailDto = GetFunctionBlockTemplateContent(command.DesignContent);

                var template = AddFunctionBlockTemplate.Create(command);
                await AddEntityRelationAsync(template, contentDetailDto);
                template.Content = await ConstructBlockContentAsync(command.DesignContent);
                template.CreatedBy = _userContext.Upn;
                var entityResult = await _blockFunctionUnitOfWork.FunctionBlockTemplates.AddEntityAsync(template);
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_TEMPLATE, ActionType.Add, ActionStatus.Success, entityResult.Id, entityResult.Name, command);

                var hashKey = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_KEY.GetCacheKey(template.Id);
                var hashField = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_FIELD.GetCacheKey(_tenantContext.ProjectId);
                await _cache.DeleteHashByKeyAsync(hashKey, hashField);

                return FunctionBlockTemplateDto.Create(template);
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_TEMPLATE, ActionType.Add, ex, payload: command);
                throw;
            }
        }

        private async Task AddEntityRelationAsync(FunctionBlockTemplate template, FunctionBlockTemplateContentDto contentDetailDto)
        {
            var templateNodes = new List<FunctionBlockTemplateNode>();

            // input connector
            var inputNodes = contentDetailDto.Inputs.Select(input =>
                new FunctionBlockTemplateNode(input.FunctionBlockId, input.Name, input.AssetMarkupName, input.TargetName, BlockTypeConstants.TYPE_INPUT_CONNECTOR, input.PortId)
            );
            // output connector
            var outputNodes = contentDetailDto.Outputs.Select(output =>
                new FunctionBlockTemplateNode(output.FunctionBlockId, output.Name, output.AssetMarkupName, output.TargetName, BlockTypeConstants.TYPE_OUTPUT_CONNECTOR, output.PortId)
            );
            var functionBlockIds = await _readFunctionBlockRepository.AsFetchable().Where(x => x.Type == BlockTypeConstants.TYPE_BLOCK).Select(x => x.Id).ToListAsync();

            // block
            var functionBlocks = contentDetailDto.FunctionBlocks.Where(x => functionBlockIds.Contains(x.Id)).Select((content, idx) =>
                new FunctionBlockTemplateNode(content.Id, BlockTypeConstants.TYPE_BLOCK, idx)
            );

            templateNodes.AddRange(inputNodes);
            templateNodes.AddRange(outputNodes);
            templateNodes.AddRange(functionBlocks);

            template.Nodes = templateNodes;
        }

        private Task<bool> IsDuplicateTemplateAsync(Guid id, string name)
        {
            return _readFunctionBlockTemplateRepository.AsQueryable().AsNoTracking()
                        .Where(x => x.Name == name && x.Id != id).AnyAsync();
        }

        public async Task<BaseResponse> RemoveBlockTemplatesAsync(DeleteFunctionBlockTemplate command, CancellationToken cancellationToken)
        {
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            var deleteNames = new List<string>();
            try
            {
                var names = await _readFunctionBlockTemplateRepository.AsQueryable().AsNoTracking()
                                    .Where(x => command.Ids.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                deleteNames.AddRange(names);
                foreach (var id in command.Ids)
                {
                    await RemoveBlockTemplateAsync(id);
                }
                await _blockFunctionUnitOfWork.CommitAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_TEMPLATE, ActionType.Delete, ActionStatus.Success, command.Ids, deleteNames, payload: command);
                return BaseResponse.Success;
            }
            catch (System.Exception ex)
            {
                await _blockFunctionUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_TEMPLATE, ActionType.Delete, ex, command.Ids, deleteNames, payload: command);
                throw;
            }
        }

        private async Task<string> ConstructBlockContentAsync(string designContent)
        {
            var jsonPayload = JsonConvert.DeserializeObject<TemplateContent>(designContent);
            var templateFunctionBlockIds = jsonPayload.Layers.Where(x => !x.IsDiagramLink).SelectMany(x => x.Models.Values.Select(x => x.FunctionBlockId)).Distinct();

            var functionBlocks = await _readFunctionBlockRepository.AsQueryable().AsNoTracking().Where(x => templateFunctionBlockIds.Any(f => f == x.Id)).ToListAsync();
            var outputs = functionBlocks.Where(x => x.Type == BlockTypeConstants.TYPE_OUTPUT_CONNECTOR).Select(x => x.Id).Distinct().ToArray();

            var inputBindings = functionBlocks.Where(x => x.Type == BlockTypeConstants.TYPE_INPUT_CONNECTOR).SelectMany(x => x.Bindings).ToList();
            var functionBlockBindings = functionBlocks.Where(x => x.Type == BlockTypeConstants.TYPE_BLOCK).SelectMany(x => x.Bindings).ToList();

            var functions = functionBlocks.Where(x => x.Type == BlockTypeConstants.TYPE_BLOCK).Distinct().ToArray();
            var graph = BuildOutputFunctionGraph(outputs, jsonPayload.Layers);
            var links = jsonPayload.Layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models.Values);
            var diagramPorts = jsonPayload.Layers.Where(x => !x.IsDiagramLink).SelectMany(x => x.Models.Values).SelectMany(x => x.Ports);
            var contents = new List<FunctionBlockTemplateContent>();
            foreach (var graphDetail in graph) // Important
            {
                var (graphNodeId, graphNodes) = graphDetail;
                var ctn = BuildFunctionBlockContent(graphNodeId, graphNodes, functions, links, diagramPorts, functionBlockBindings, inputBindings);
                contents.Add(ctn);
            }
            return JsonConvert.SerializeObject(contents);
        }

        private FunctionBlockTemplateContent BuildFunctionBlockContent(
            Guid graphNodeId, IEnumerable<NodeGraph> graphNodes,
            Domain.Entity.FunctionBlock[] functions,
            IEnumerable<FunctionModel> links,
            IEnumerable<FunctionPort> diagramPorts,
            IEnumerable<FunctionBlockBinding> functionBlockBindings,
            IEnumerable<FunctionBlockBinding> inputBindings)
        {
            var builder = new StringBuilder();
            foreach (var graphFunction in graphNodes)
            {
                var function = functions.FirstOrDefault(x => x.Id == graphFunction.Current.FunctionBlockId);
                if (function == null)
                {
                    continue;
                }
                // default value
                foreach (var binding in function.Bindings.Where(x => _inBinding.Contains(x.BindingType) && x.DataType != BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE && x.DataType != BindingDataTypeIdConstants.TYPE_ASSET_TABLE))
                {
                    builder.AppendLine($"AHI.SafetySetIfNotExists(\"{binding.Key}\",\"{binding.DataType}\", \"{binding.DefaultValue}\" );");
                }
                // binding value: template input (source) -> function input (target) - you're here
                var templateBindings = (from targetPort in graphFunction.Current.Ports
                                        join targetLink in links on targetPort.Id equals targetLink.TargetPort
                                        join sourcePort in diagramPorts on targetLink.SourcePort equals sourcePort.Id

                                        // join with function input/output to get the information like key
                                        join targetBinding in functionBlockBindings on targetPort.BlockBinding.Id equals targetBinding.Id
                                        join sourceBinding in inputBindings on sourcePort.BlockBinding.Id equals sourceBinding.Id
                                        select new { Source = sourceBinding, Target = targetBinding });
                foreach (var binding in templateBindings)
                {
                    // copy from template to function input
                    builder.AppendLine($"AHI.SafetySet(\"{binding.Target.Key}\",\"{binding.Target.DataType}\" ,AHI.SafetyGet(\"{$"{binding.Target.FunctionBlockId.ToString("N")}_{binding.Target.Key}"}\", \"{binding.Target.DataType}\"));");
                }

                builder.AppendLine(@$"{{
                                    {function.BlockContent}
                                  }}");
                // if the exit code request -> terminate the flow
                builder.AppendLine("if (AHI.GetBoolean(\"system_exit_code\") == true) { return ;}");
                var bindings = (from source in graphFunction.Current.Ports
                                join sourceLink in links on source.Id equals sourceLink.SourcePort
                                join targetPort in diagramPorts on sourceLink.TargetPort equals targetPort.Id
                                // join with function input/output to get the information like key
                                join functionInputBinding in functionBlockBindings on source.BlockBinding.Id equals functionInputBinding.Id
                                join functionOutputBinding in functionBlockBindings on targetPort.BlockBinding.Id equals functionOutputBinding.Id
                                where sourceLink.Id == graphFunction.LinkId
                                select new { Source = functionInputBinding, SourcePort = source.Id, Target = functionOutputBinding });

                // mapping with another output
                foreach (var binding in bindings)
                {
                    builder.AppendLine($"AHI.SafetySet(\"{$"{binding.SourcePort.ToString("N")}_{binding.Source.Key}"}\",\"{binding.Source.DataType}\" ,AHI.SafetyGet(\"{binding.Source.Key}\", \"{binding.Source.DataType}\"));");
                    builder.AppendLine($"AHI.SafetySet(\"{binding.Target.Key}\",\"{binding.Target.DataType}\" ,AHI.SafetyGet(\"{$"{binding.SourcePort.ToString("N")}_{binding.Source.Key}"}\", \"{binding.Source.DataType}\"));");
                }
            }
            FunctionBlockTemplateContent templateContent = new FunctionBlockTemplateContent();
            templateContent.Content = builder.ToString();
            templateContent.NodeId = graphNodeId;
            return templateContent;
        }

        private async Task<string> RemoveBlockTemplateAsync(Guid id)
        {
            var entity = await _readFunctionBlockTemplateRepository.AsQueryable().AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            // check template using

            var executions = await _readFunctionBlockExecutionRepository.AsQueryable().Where(x => x.TemplateId == id).ToListAsync();
            foreach (var exe in executions)
            {
                //Bug: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/56222
                //If the block execution is running, the status is changed to Running (Obsolete)
                //If the block execution is Stoped/Stopped (Error), the status is not changed.
                if (exe.Status == BlockExecutionStatusConstants.RUNNING)
                {
                    exe.Status = BlockExecutionStatusConstants.RUNNING_OBSOLETE;
                }
                await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpdateAsync(exe.Id, exe);
            }

            await _blockFunctionUnitOfWork.FunctionBlockTemplates.RemoveEntityAsync(entity);

            var hashKey = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_KEY.GetCacheKey(id);
            await _cache.DeleteAsync(hashKey);

            return entity.Name;
        }

        public async Task<FunctionBlockTemplateDto> UpdateBlockTemplateAsync(UpdateFunctionBlockTemplate command, CancellationToken cancellationToken)
        {
            var newTemplate = UpdateFunctionBlockTemplate.Create(command);
            try
            {
                await _blockFunctionUnitOfWork.BeginTransactionAsync();
                var templateDB = await _blockFunctionUnitOfWork.FunctionBlockTemplates.FindAsync(command.Id);
                if (templateDB == null)
                    throw new EntityNotFoundException();
                // check duplicate
                if (await IsDuplicateTemplateAsync(command.Id, command.Name))
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(UpdateFunctionBlockTemplate.Name));

                var contentDto = GetFunctionBlockTemplateContent(command.DesignContent);

                await AddEntityRelationAsync(newTemplate, contentDto);
                newTemplate.Content = await ConstructBlockContentAsync(command.DesignContent);

                // check the trigger setting has changed
                var triggerChanged = CheckTriggerSettingChanged(templateDB, command);

                // check diagram has changed
                var diagramChanged = CheckDiagramChanged(contentDto, templateDB);

                if (triggerChanged || diagramChanged)
                {
                    // upgrade version
                    newTemplate.Version = Guid.NewGuid();
                    command.RequiredBlockExecutionRefreshing = true;
                    command.HasDiagramChanged = diagramChanged;
                }
                var entityResult = await _blockFunctionUnitOfWork.FunctionBlockTemplates.UpdateEntityAsync(newTemplate);

                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch (System.Exception ex)
            {
                await _blockFunctionUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_TEMPLATE, ActionType.Update, ex, command.Id, command.Name, payload: command);
                throw;
            }

            await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_TEMPLATE, ActionType.Update, ActionStatus.Success, command.Id, command.Name, command);

            var hashKey = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_KEY.GetCacheKey(command.Id);
            var hashField = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_FIELD.GetCacheKey(_tenantContext.ProjectId);
            await _cache.DeleteHashByKeyAsync(hashKey, hashField);

            await _deviceBackgroundService.QueueAsync(_tenantContext, command);
            return FunctionBlockTemplateDto.Create(newTemplate);
        }

        public async Task<IEnumerable<Guid>> UpdateBlockTemplateContentsAsync(Guid functionBlockId)
        {
            var blockTemplates = await _readFunctionBlockTemplateRepository.AsQueryable()
                                                                        .Where(x => x.Nodes.Any(y => y.FunctionBlockId == functionBlockId))
                                                                        .ToListAsync();
            if (blockTemplates.Any())
            {
                foreach (var template in blockTemplates)
                {
                    template.Content = await ConstructBlockContentAsync(template.DesignContent);
                }
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            return blockTemplates.Select(b => b.Id);
        }

        private bool CheckDiagramChanged(FunctionBlockTemplateContentDto commandContent, FunctionBlockTemplate templateDb)
        {
            var trackingContent = GetFunctionBlockTemplateContent(templateDb.DesignContent);
            return CheckDesignContentChanged(commandContent, trackingContent);
        }

        private bool CheckTriggerSettingChanged(FunctionBlockTemplate templateDb, UpdateFunctionBlockTemplate command)
        {
            var triggerChanged = false;
            var typeChanged = command.TriggerType != templateDb.TriggerType;
            if (typeChanged)
            {
                triggerChanged = true;
            }
            else if (!typeChanged && !string.IsNullOrEmpty(templateDb.TriggerContent) && !string.IsNullOrEmpty(command.TriggerContent))
            {
                triggerChanged = !templateDb.TriggerContent.Equals(command.TriggerContent);
            }
            return triggerChanged;
        }

        public async Task<bool> ValidationChangedTemplateAsync(ValidationBlockContent command, CancellationToken cancellationToken)
        {
            var templateDB = await _readFunctionBlockTemplateRepository.FindAsync(command.Id);
            if (templateDB == null)
                throw new EntityNotFoundException();

            var templateUsed = await _readFunctionBlockExecutionRepository.AsQueryable().AnyAsync(x => x.TemplateId == command.Id);
            if (!templateUsed)
            {
                return false;
            }

            var triggerChanged = !string.Equals(command.TriggerContent, templateDB.TriggerContent);
            var designChanged = false;
            if (!string.IsNullOrEmpty(command.DesignContent))
            {
                var commandContent = GetFunctionBlockTemplateContent(command.DesignContent);
                var contentTrack = GetFunctionBlockTemplateContent(templateDB.DesignContent);
                designChanged = CheckDesignContentChanged(commandContent, contentTrack);
            }

            return (triggerChanged || designChanged);
        }

        private bool CheckDesignContentChanged(FunctionBlockTemplateContentDto commandContent, FunctionBlockTemplateContentDto trackingContent)
        {
            var inputNotChange = trackingContent.Inputs.SequenceEqual(commandContent.Inputs, new ConnectorInfoComparer());
            if (!inputNotChange)
                return true;

            var outputNotChange = trackingContent.Outputs.SequenceEqual(commandContent.Outputs, new ConnectorInfoComparer());
            if (!outputNotChange)
                return true;

            var linkNotChange = trackingContent.Links.SequenceEqual(commandContent.Links, new FunctionBlockLinkDtoComparer());
            if (!linkNotChange)
                return true;

            return false;
        }

        public async Task<bool> ValidationBlockTemplateAsync(ValidationBlockTemplates command, CancellationToken cancellationToken)
        {
            var executions = await _readFunctionBlockExecutionRepository.AsQueryable().Where(x => x.TemplateId != null).ToListAsync();
            var templateUsed = executions.Any(x => command.Ids.Contains((Guid)x.TemplateId));
            return templateUsed;
        }

        public async Task<GetFunctionBlockTemplateDto> FindBlockTemplateByIdAsync(GetBlockTemplateById command, CancellationToken cancellationToken)
        {
            var hashKey = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_KEY.GetCacheKey(command.Id);
            var hashField = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_FIELD.GetCacheKey(_tenantContext.ProjectId);

            var dto = await _cache.GetHashByKeyAsync<GetFunctionBlockTemplateDto>(hashKey, hashField);

            if (dto == null)
            {
                var template = await _readFunctionBlockTemplateRepository
                                                            .AsQueryable()
                                                            .AsNoTracking()
                                                            .Include(x => x.Nodes).ThenInclude(x => x.FunctionBlock).ThenInclude(x => x.Bindings)
                                                            .FirstOrDefaultAsync(x => x.Id == command.Id);

                if (template == null)
                    throw new EntityNotFoundException();

                dto = GetFunctionBlockTemplateDto.Create(template);
                await _cache.SetHashByKeyAsync(hashKey, hashField, dto);
            }
            return dto;
        }

        public Task<bool> CheckUsedBlockTemplateAsync(CheckUsedBlockTemplate command, CancellationToken cancellationToken)
        {
            return _readFunctionBlockExecutionRepository.AsQueryable().AnyAsync(x => x.TemplateId != null && x.TemplateId == command.Id && (x.Status == BlockExecutionStatusConstants.RUNNING || x.Status == BlockExecutionStatusConstants.RUNNING_OBSOLETE));
        }

        public FunctionBlockTemplateContentDto GetFunctionBlockTemplateContent(string content)
        {
            // see the sample content at: AppData/FunctionBlockTemplateRaw.json and AppData/FunctionBlockTemplateContent.json
            var jsonPayload = JsonConvert.DeserializeObject<TemplateContent>(content);
            var nodes = jsonPayload.Layers.Where(x => !x.IsDiagramLink);
            var links = jsonPayload.Layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models);
            var contentDto = new FunctionBlockTemplateContentDto();
            var innerNodes = nodes.SelectMany(x => x.Models.Values).ToList();

            var inputs = innerNodes.Where(x => x.FunctionBlockType == BlockTypeConstants.TYPE_INPUT_CONNECTOR)
                                    .Select(x => IsNodeModelSupportingMarkup(x)
                                            ? new ConnectorInfo(x.FunctionBlockId, x.Name, x.Name.GetMarkupName(), x.Name.GetTargetName(), x.AssetId, x.Ports[0]?.Id)
                                            : new ConnectorInfo(x.FunctionBlockId, x.Name, null, null, null, x.Ports[0]?.Id));
            var outputs = innerNodes.Where(x => x.FunctionBlockType == BlockTypeConstants.TYPE_OUTPUT_CONNECTOR)
                                    .Select(x => IsNodeModelSupportingMarkup(x)
                                            ? new ConnectorInfo(x.FunctionBlockId, x.Name, x.Name.GetMarkupName(), x.Name.GetTargetName(), x.AssetId, x.Ports[0]?.Id)
                                            : new ConnectorInfo(x.FunctionBlockId, x.Name, null, null, null, x.Ports[0]?.Id));
            var functionBlockInfo = new List<(Guid PortId, Guid FunctionBlockId, Guid BindingId, string BindingType, Guid LinkId, int Index)>();
            int idx = 0;
            foreach (var port in innerNodes.SelectMany(x => x.Ports))
            {
                idx++;
                var bindingType = port.BlockBinding.BindingType;
                var functionBlockId = port.BlockBinding.FunctionBlockId;
                var bindingId = port.BlockBinding.Id;
                var portId = port.Id;
                if (port.Links != null)
                {
                    // has input binding
                    functionBlockInfo.AddRange(port.Links.Select(link => (portId, functionBlockId, bindingId, bindingType, link, idx)));
                }
            }
            var templateFunctionBlocks = functionBlockInfo.OrderBy(x => x.Index).GroupBy(x => x.FunctionBlockId).Select(x => new GetFunctionBlockDto()
            {
                Id = x.Key,
                Bindings = x.Select(input => new BlockBinding.Command.Model.GetFunctionBlockBindingDto
                {
                    Id = input.BindingId,
                    BindingType = input.BindingType
                })
            });

            var functionBlockLinks = functionBlockInfo.Where(x => x.LinkId != null).OrderBy(x => x.Index).GroupBy(x => x.LinkId).Select(group =>
            {
                var link = links.First(x => x.Key == group.Key.ToString()).Value;
                var output = group.Where(x => link.SourcePort == x.PortId).First(); //1 output and multiple input
                var inputs = group.Where(x => link.TargetPort == x.PortId); // multiple output

                var blockLinkOutput = new FunctionBlockLinkDetailDto()
                {
                    FunctionBlockId = output.FunctionBlockId,
                    BindingId = output.BindingId,
                    PortId = output.PortId
                };
                var blockLinkInputs = inputs.Select(input => new FunctionBlockLinkDetailDto()
                {
                    FunctionBlockId = input.FunctionBlockId,
                    BindingId = input.BindingId,
                    PortId = input.PortId
                });
                return new FunctionBlockLinkDto()
                {
                    Output = blockLinkOutput,
                    Inputs = blockLinkInputs
                };
            });
            contentDto.Inputs = inputs;
            contentDto.Outputs = outputs;
            contentDto.Links = functionBlockLinks;
            contentDto.FunctionBlocks = templateFunctionBlocks;
            return contentDto;
        }

        private bool IsNodeModelSupportingMarkup(FunctionModel node)
        {
            return node.Ports[0]?.BlockBinding.DataType == BindingDataTypeIdConstants.TYPE_ASSET_TABLE
                || node.Ports[0]?.BlockBinding.DataType == BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE;
        }

        private IEnumerable<(Guid, IEnumerable<NodeGraph>)> BuildOutputFunctionGraph(Guid[] outputs, IEnumerable<TemplateLayers> layers)
        {
            var graphResult = new List<(Guid, IEnumerable<NodeGraph>)>();
            var models = layers.Where(x => !x.IsDiagramLink).SelectMany(s => s.Models.Select(x => x.Value)).ToList();
            var links = layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models.Select(m => m.Value)).ToList();
            var ports = models.SelectMany(m => m.Ports);
            var outputNodes = models.Where(x => outputs.Contains(x.FunctionBlockId));
            foreach (var node in outputNodes)
            {
                var nodeGraph = FindPreviousNode(Guid.Empty, node, node.Ports.Where(x => x.In), models, links);
                var functionNodes = ExtractFunctionFromGraph(nodeGraph, 1);
                var stack = new Stack<NodeGraph>();
                foreach (var item in functionNodes.GroupBy(x => x.Index).SelectMany(x => x))
                {
                    var lastValue = stack.FirstOrDefault();
                    if (lastValue == null)
                    {
                        stack.Push(item);
                    }
                    else if (lastValue != null && lastValue.Current.Id != item.Current.Id)
                    {
                        stack.Push(item);
                    }
                }
                graphResult.Add((node.Id, stack));
            }
            return graphResult;
        }

        private List<NodeGraph> ExtractFunctionFromGraph(NodeLink node, int level)
        {
            var functionOrder = new List<NodeGraph>();
            if (node != null)
            {
                functionOrder.Add(new NodeGraph(node.Current, node.Id, level));
                if (node.Next != null)
                {
                    foreach (var nextNode in node.Next)
                    {
                        functionOrder.AddRange(ExtractFunctionFromGraph(nextNode, level + 1));
                    }
                }
            }
            return functionOrder;
        }

        private NodeLink FindPreviousNode(Guid linkId, FunctionModel current, IEnumerable<FunctionPort> currentInputPorts, IEnumerable<FunctionModel> models, IEnumerable<FunctionModel> links)
        {
            var inputPorts = current.Ports.Where(x => x.In == true).ToList(); // reserve
            var previous = links.Where(x => inputPorts.Any(port => port.Id == x.TargetPort));
            var node = new NodeLink() { Current = current, Id = linkId };
            if (previous.Any())
            {
                node.Next = previous.Select(link => new
                {
                    Function = models.FirstOrDefault(x => link.Source == x.Id),
                    LinkId = link.Id
                }).Where(x => x.Function != null).Select(previousNode => FindPreviousNode(previousNode.LinkId, previousNode.Function, inputPorts, models, links)).ToList();
            }
            return node;
        }

        private void UpdateScore(IDictionary<Guid, int> scoreResult, Guid scoreKey, IEnumerable<Guid> source, Guid functionBlockId, int score)
        {
            if (source.Contains(functionBlockId))
            {
                if (scoreResult.ContainsKey(scoreKey))
                {
                    scoreResult[scoreKey] = scoreResult[scoreKey] + score;
                }
                else
                {
                    scoreResult[scoreKey] = score;
                }
            }
        }

        public async Task<IEnumerable<ArchiveBlockTemplateDto>> ArchiveAsync(ArchiveBlockTemplate command, CancellationToken cancellationToken)
        {
            var templates = await _readFunctionBlockTemplateRepository.AsQueryable().AsNoTracking().Where(x => x.UpdatedUtc <= command.ArchiveTime).ToListAsync();
            return templates.Select(ArchiveBlockTemplateDto.Create);
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveBlockTemplate command, CancellationToken cancellationToken)
        {
            _userContext.SetUpn(command.Upn);
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveBlockTemplateDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!data.Any())
            {
                return BaseResponse.Success;
            }

            var templates = data.OrderBy(x => x.UpdatedUtc).Select(x => ArchiveBlockTemplateDto.Create(x, _userContext.Upn)).ToList();
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            try
            {
                await _blockFunctionUnitOfWork.FunctionBlockTemplates.RetrieveAsync(templates);
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch
            {
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyArchiveBlockTemplate command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveBlockTemplateDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            foreach (var category in data)
            {
                var validation = await _validator.ValidateAsync(category);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }
    }
}