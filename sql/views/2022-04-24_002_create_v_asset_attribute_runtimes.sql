drop view if exists v_asset_attribute_runtimes;
create or replace view v_asset_attribute_runtimes
as
select aarm.asset_id  as asset_id, aarm.id as attribute_id, aarm.expression_compile as expression_compile, aat.data_type as data_type, aatr.trigger_asset_id, aatr.trigger_attribute_id, aarm.enabled_expression
from asset_attribute_runtime_mapping aarm 
inner join asset_attribute_templates aat on aarm.asset_attribute_template_id  = aat.id 
left join asset_attribute_runtime_triggers aatr on aarm.id = aatr.attribute_id and aatr.is_selected = true

union all 
select aa.asset_id, aa.id as attribute_id, aar.expression_compile as expression_compile, aa.data_type  as data_type, aatr.trigger_asset_id, aatr.trigger_attribute_id, aar.enabled_expression
from asset_attributes aa  
inner join asset_attribute_runtimes aar on aa.id = aar.asset_attribute_id
left join asset_attribute_runtime_triggers aatr on aar.asset_attribute_id = aatr.attribute_id  and aatr.is_selected = true