drop view if exists v_asset_warning;
create or replace view v_asset_warning
as
with baseData as (
	-- template mapping
	select distinct asset.Asset_Id as asset_id, case when (asset.Dynamic_Device_Id is null and asset.Attribute_Type = 'dynamic') 
														   or (asset.Integration_Device_Id is null and asset.Attribute_Type = 'integration') 
														   or (asset.Integration_Id is null and asset.Attribute_Type = 'integration') 
														   or (Attribute_Type = 'alias' 
														   		and (asset.Mapping_Alias_Id is null 
																	or asset.Mapping_Alias_Attribute_Id not in 
																		(select id from asset_attributes aa where aa.asset_id = asset.Mapping_Alias_Id
																		union
																		select id from asset_attribute_dynamic_mapping aa where aa.asset_id = asset.asset_id
																		union
																		select id from asset_attribute_static_mapping aa where aa.asset_id = asset.asset_id
																		union
																		select id from asset_attribute_runtime_mapping aa where aa.asset_id = asset.asset_id
																		union
																		select id from asset_attribute_alias_mapping aa where aa.asset_id = asset.asset_id
																		union
																		select id from asset_attribute_integration_mapping aa where aa.asset_id = asset.asset_id)))
														   or ((asset.Command_Device_Id is null or asset.Command_Metric_Key is null or asset.Command_Metric_Key = '') 
																and asset.Attribute_Type = 'command')
														   or (Attribute_Type = 'runtime' 
														   		and Runtime_Enabled_Expression = true 
																and asset.Runtime_Trigger_Attribute not in 
																	(select id from asset_attribute_dynamic_mapping aa where aa.asset_id = asset.asset_id
																	union
																	select id from asset_attribute_static_mapping aa where aa.asset_id = asset.asset_id
																	union
																	select id from asset_attribute_runtime_mapping aa where aa.asset_id = asset.asset_id
																	union
																	select id from asset_attribute_alias_mapping aa where aa.asset_id = asset.asset_id
																	union
																	select id from asset_attribute_integration_mapping aa where aa.asset_id = asset.asset_id))
													then true
													else false
												end as has_warning
	from (
		select t.id as Asset_Id, dnm.device_id as Dynamic_Device_Id, igt.device_id as Integration_Device_Id, igt.integration_id as Integration_Id, 
							atr.attribute_type as Attribute_Type, amp.alias_asset_id  as Mapping_Alias_Id, amp.alias_attribute_id as Mapping_Alias_Attribute_Id,
							cmd.device_id as Command_Device_Id, cmd.metric_key as Command_Metric_Key,
							rnt.enabled_expression as Runtime_Enabled_Expression, rnt_trg.trigger_attribute_id as Runtime_Trigger_Attribute
		from (
		  select a.id, a.asset_template_id
		  from assets as a
		) as t
		left join asset_attribute_dynamic_mapping as dnm on t.id = dnm.asset_id
		left join asset_attribute_integration_mapping as igt on t.id = igt.asset_id
		left join asset_attribute_command_mapping as cmd on t.id = cmd.asset_id
		left join asset_attribute_runtime_mapping as rnt on t.id = rnt.asset_id
		left join asset_attribute_runtime_triggers as rnt_trg on t.id = rnt_trg.asset_id
		left join asset_attribute_templates as atr on atr.Id = dnm.asset_attribute_template_id
													or atr.Id = cmd.asset_attribute_template_id 					
													or atr.Id = igt.asset_attribute_template_id 
													or atr.Id = rnt.asset_attribute_template_id 
													or (atr.attribute_type = 'alias' and atr.asset_template_id = t.asset_template_id)
		left join asset_attribute_alias_mapping as amp on (amp.asset_attribute_template_id  = atr.id and t.id = amp.asset_id)
	) as asset
	union
	-- standalone asset
	select distinct a.asset_id
					, case when 
								(a.attribute_type = 'alias' and (alias.alias_attribute_id is null or alias.alias_attribute_id not in (select id from asset_attributes aa where aa.asset_id = alias.alias_asset_id
																																	  union
																																	  select id from asset_attribute_static_mapping aa where aa.asset_id = alias.alias_asset_id
																																	  union
																																	  select id from asset_attribute_dynamic_mapping aa where aa.asset_id = alias.alias_asset_id
																																	  union
																																	  select id from asset_attribute_runtime_mapping aa where aa.asset_id = alias.alias_asset_id
																																	  union
																																	  select id from asset_attribute_alias_mapping aa where aa.asset_id = alias.alias_asset_id
																																	  union
																																	  select id from asset_attribute_integration_mapping aa where aa.asset_id = alias.alias_asset_id)))
								or (a.attribute_type = 'runtime' and aar.enabled_expression = true and rrt.trigger_attribute_id not in (select id from asset_attributes aa where aa.asset_id = a.asset_id
																																		union
																																		select id from asset_attribute_static_mapping aa where aa.asset_id = a.asset_id
																																		union
																																		select id from asset_attribute_dynamic_mapping aa where aa.asset_id = a.asset_id
																																		union
																																		select id from asset_attribute_runtime_mapping aa where aa.asset_id = a.asset_id
																																		union
																																		select id from asset_attribute_alias_mapping aa where aa.asset_id = a.asset_id
																																		union
																																		select id from asset_attribute_integration_mapping aa where aa.asset_id = a.asset_id))
								or (a.attribute_type = 'command' and (aac.device_id is null or aac.metric_key is null))
								or (a.attribute_type = 'dynamic' and (aad.device_id is null or aad.metric_key is null))
								or (a.attribute_type = 'integration' and (aai.device_id is null or aai.integration_id is null or aai.integration_id = '00000000-0000-0000-0000-000000000000'))
							then true
							else false
							end as has_warning
	from asset_attributes a
	left join asset_attribute_alias as alias on (alias.asset_attribute_id  = a.id )
	left join asset_attribute_runtime_triggers as rrt on (rrt.attribute_id  = a.id and rrt.is_selected = true)
	left join asset_attribute_runtimes aar on aar.asset_attribute_id  = a.id
	left join asset_attribute_commands aac on aac.asset_attribute_id = a.id
	left join asset_attribute_dynamic aad on aad.asset_attribute_id = a.id
	left join asset_attribute_integration aai on aai.asset_attribute_id = a.id
)
select s.asset_id, s.has_warning 
from baseData s
join (
		select t.asset_id , count(1) as counts 
		from baseData t
		group by t.asset_id
	) t on s.asset_id  = t.asset_id
where t.counts = 1 or (t.counts = 2  and s.has_warning is true)