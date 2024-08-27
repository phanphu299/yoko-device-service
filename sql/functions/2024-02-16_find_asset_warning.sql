create or replace
function public.find_asset_warning(assetIds uuid[], assetName varchar(255), startsWithResourcePath varchar(1024))
	returns table(asset_id uuid)
as
$func$
-- standalone
  ---- alias
select
  distinct aa.asset_id
from
  asset_attributes aa
  inner join assets a on aa.asset_id = a.id
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  left join asset_attribute_alias aaa on aa.id = aaa.asset_attribute_id
  and aaa.alias_attribute_id is not null
  left join asset_attributes t_aa on aaa.alias_asset_id = t_aa.asset_id
  and aaa.alias_attribute_id = t_aa.id
  left join asset_attribute_static_mapping t_aasm on aaa.alias_asset_id = t_aasm.asset_id
  and aaa.alias_attribute_id = t_aasm.id
  left join asset_attribute_dynamic_mapping t_aadm on aaa.alias_asset_id = t_aadm.asset_id
  and aaa.alias_attribute_id = t_aadm.id
  left join asset_attribute_runtime_mapping t_aarm on aaa.alias_asset_id = t_aarm.asset_id
  and aaa.alias_attribute_id = t_aarm.id
  left join asset_attribute_alias_mapping t_aaam on aaa.alias_asset_id = t_aaam.asset_id
  and aaa.alias_attribute_id = t_aaam.id
  left join asset_attribute_integration_mapping t_aaim on aaa.alias_asset_id = t_aaim.asset_id
  and aaa.alias_attribute_id = t_aaim.id
where
  aa.attribute_type = 'alias'
  and (
    assetIds is null
    or aa.asset_id = any(assetIds)
  )
  and (
    aaa.id is null
    or (
      t_aa.id is null
      and t_aasm.id is null
      and t_aadm.id is null
      and t_aarm.id is null
      and t_aaam.id is null
      and t_aaim.id is null
    )
  )
union
  ---- runtime
select
  distinct aa.asset_id
from
  asset_attributes aa
  inner join assets a on aa.asset_id = a.id
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  left join asset_attribute_runtimes aar on aa.id = aar.asset_attribute_id
  left join asset_attribute_runtime_triggers aart on aa.id = aart.attribute_id
  and aart.is_selected = true
  left join asset_attributes t_aa on aart.trigger_asset_id = t_aa.asset_id
  and aart.trigger_attribute_id = t_aa.id
  left join asset_attribute_static_mapping t_aasm on aart.trigger_asset_id = t_aasm.asset_id
  and aart.trigger_attribute_id = t_aasm.id
  left join asset_attribute_dynamic_mapping t_aadm on aart.trigger_asset_id = t_aadm.asset_id
  and aart.trigger_attribute_id = t_aadm.id
  left join asset_attribute_runtime_mapping t_aarm on aart.trigger_asset_id = t_aarm.asset_id
  and aart.trigger_attribute_id = t_aarm.id
  left join asset_attribute_alias_mapping t_aaam on aart.trigger_asset_id = t_aaam.asset_id
  and aart.trigger_attribute_id = t_aaam.id
  left join asset_attribute_integration_mapping t_aaim on aart.trigger_asset_id = t_aaim.asset_id
  and aart.trigger_attribute_id = t_aaim.id
where
  aa.attribute_type = 'runtime'
  and (
    assetIds is null
    or aa.asset_id = any(assetIds)
  )
  and aar.enabled_expression = true
  and aart.trigger_attribute_id is not null
  and (
    t_aa.id is null
    and t_aasm.id is null
    and t_aadm.id is null
    and t_aarm.id is null
    and t_aaam.id is null
    and t_aaim.id is null
  )
union
  ---- dynamic, command
select
  distinct aa.asset_id
from
  asset_attributes aa
  inner join assets a on aa.asset_id = a.id
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  left join asset_attribute_dynamic aad on aa.id = aad.asset_attribute_id
  and aa.attribute_type = 'dynamic'
  and aad.device_id is not null
  and aad.metric_key is not null
  and aad.metric_key != ''
  left join asset_attribute_commands aac on aa.id = aac.asset_attribute_id
  and aa.attribute_type = 'command'
  and aac.device_id is not null
  and aac.metric_key is not null
  and aac.metric_key != ''
where
  (
    assetIds is null
    or aa.asset_id = any(assetIds)
  )
  and (
    (
      aa.attribute_type = 'dynamic'
      and aad.id is null
    )
    or (
      aa.attribute_type = 'command'
      and aac.id is null
    )
  )
union
  ---- integration
select
  distinct aa.asset_id
from
  asset_attributes aa
  inner join assets a on aa.asset_id = a.id
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  left join asset_attribute_integration aai on aa.id = aai.asset_attribute_id
  and aa.attribute_type = 'integration'
  and aai.device_id is not null
  and aai.integration_id is not null
  and aai.integration_id != '00000000-0000-0000-0000-000000000000'
where
  (
    assetIds is null
    or aa.asset_id = any(assetIds)
  )
  and aa.attribute_type = 'integration'
  and aai.id is null
union
-- template
  ---- alias
select
  distinct a.id as asset_id
from
  assets a
  left join asset_attribute_templates aat on a.asset_template_id = aat.asset_template_id
  and aat.attribute_type = 'alias'
  left join asset_attribute_alias_mapping aaam on aat.id = aaam.asset_attribute_template_id
  and a.id = aaam.asset_id
  and aaam.alias_attribute_id is not null
  left join asset_attributes t_aa on aaam.alias_asset_id = t_aa.asset_id
  and aaam.alias_attribute_id = t_aa.id
  left join asset_attribute_static_mapping t_aasm on aaam.alias_asset_id = t_aasm.asset_id
  and aaam.alias_attribute_id = t_aasm.id
  left join asset_attribute_dynamic_mapping t_aadm on aaam.alias_asset_id = t_aadm.asset_id
  and aaam.alias_attribute_id = t_aadm.id
  left join asset_attribute_runtime_mapping t_aarm on aaam.alias_asset_id = t_aarm.asset_id
  and aaam.alias_attribute_id = t_aarm.id
  left join asset_attribute_alias_mapping t_aaam on aaam.alias_asset_id = t_aaam.asset_id
  and aaam.alias_attribute_id = t_aaam.id
  left join asset_attribute_integration_mapping t_aaim on aaam.alias_asset_id = t_aaim.asset_id
  and aaam.alias_attribute_id = t_aaim.id
where
  (
    assetIds is null
    or a.id = any(assetIds)
  )
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  and aat.attribute_type = 'alias'
  and (
    aaam.id is null
    or (
      t_aa.id is null
      and t_aasm.id is null
      and t_aadm.id is null
      and t_aarm.id is null
      and t_aaam.id is null
      and t_aaim.id is null
    )
  )
union
  ---- runtime
select
  distinct a.id as asset_id
from
  assets a
  left join asset_attribute_templates aat on a.asset_template_id = aat.asset_template_id
  and aat.attribute_type = 'runtime'
  left join asset_attribute_runtime_mapping aarm on aat.id = aarm.asset_attribute_template_id
  and a.id = aarm.asset_id
  left join asset_attribute_runtime_triggers aart on aarm.id = aart.attribute_id
  and aart.is_selected = true
  left join asset_attributes t_aa on aart.trigger_asset_id = t_aa.asset_id
  and aart.trigger_attribute_id = t_aa.id
  left join asset_attribute_static_mapping t_aasm on aart.trigger_asset_id = t_aasm.asset_id
  and aart.trigger_attribute_id = t_aasm.id
  left join asset_attribute_dynamic_mapping t_aadm on aart.trigger_asset_id = t_aadm.asset_id
  and aart.trigger_attribute_id = t_aadm.id
  left join asset_attribute_runtime_mapping t_aarm on aart.trigger_asset_id = t_aarm.asset_id
  and aart.trigger_attribute_id = t_aarm.id
  left join asset_attribute_alias_mapping t_aaam on aart.trigger_asset_id = t_aaam.asset_id
  and aart.trigger_attribute_id = t_aaam.id
  left join asset_attribute_integration_mapping t_aaim on aart.trigger_asset_id = t_aaim.asset_id
  and aart.trigger_attribute_id = t_aaim.id
where
  (
    assetIds is null
    or a.id = any(assetIds)
  )
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  and aat.attribute_type = 'runtime'
  and aarm.enabled_expression = true
  and aart.trigger_attribute_id is not null
  and (
    t_aa.id is null
    and t_aasm.id is null
    and t_aadm.id is null
    and t_aarm.id is null
    and t_aaam.id is null
    and t_aaim.id is null
  )
union
  ---- dynamic, command
select
  distinct a.id as asset_id
from
  assets a
  left join asset_attribute_templates aat on a.asset_template_id = aat.asset_template_id
  left join asset_attribute_dynamic_mapping aadm on aat.id = aadm.asset_attribute_template_id
  and a.id = aadm.asset_id
  and aat.attribute_type = 'dynamic'
  and aadm.device_id is not null
  and aadm.metric_key is not null
  and aadm.metric_key != ''
  left join asset_attribute_command_mapping aacm on aat.id = aacm.asset_attribute_template_id
  and a.id = aacm.asset_id
  and aat.attribute_type = 'command'
  and aacm.device_id is not null
  and aacm.metric_key is not null
  and aacm.metric_key != ''
where
  (
    assetIds is null
    or a.id = any(assetIds)
  )
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  and (
    (
      aat.attribute_type = 'dynamic'
      and aadm.id is null
    )
    or (
      aat.attribute_type = 'command'
      and aacm.id is null
    )
  )
union
  -- integration
select
  distinct a.id as asset_id
from
  assets a
  left join asset_attribute_templates aat on a.asset_template_id = aat.asset_template_id
  left join asset_attribute_integration_mapping aaim on aat.id = aaim.asset_attribute_template_id
  and a.id = aaim.asset_id
  and aat.attribute_type = 'integration'
  and aaim.device_id is not null
  and aaim.integration_id is not null
  and aaim.integration_id != '00000000-0000-0000-0000-000000000000'
where
  (
    assetIds is null
    or a.id = any(assetIds)
  )
  and (assetName is null or a.name ilike concat('%', assetName , '%'))
  and (startsWithResourcePath is null or a.resource_path like concat(startsWithResourcePath, '%'))
  and aat.attribute_type = 'integration'
  and aaim.id is null;
$func$
language sql;
