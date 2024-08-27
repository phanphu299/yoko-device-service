update asset_attribute_runtimes 
set is_trigger_visibility = true
where enabled_expression = true;

update asset_attribute_runtime_mapping
set is_trigger_visibility = true
where enabled_expression = true;

insert into asset_attribute_runtime_triggers(asset_id, attribute_id, trigger_asset_id, trigger_attribute_id)
select aa.asset_id, aa.id, asset_id, aar.trigger_attribute_id
from  asset_attribute_runtimes aar
inner join asset_attributes  aa on aar.asset_attribute_id  = aa.id 
where aar.enabled_expression  = true and aar.trigger_attribute_id is not null;


insert into asset_attribute_runtime_triggers(asset_id, attribute_id, trigger_asset_id, trigger_attribute_id)
select asset_id, id, asset_id, trigger_attribute_id
from  asset_attribute_runtime_mapping
where enabled_expression = true and trigger_attribute_id is not null;