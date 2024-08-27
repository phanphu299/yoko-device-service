 --drop view if exists v_asset_attributes;
 create or replace view v_asset_attributes
 as
 select am.asset_id, am.id as attribute_id, aat.data_type, aat.attribute_type as attribute_type, aat.name as attribute_name, null as enabled_expression, null as expression_compile, aat.uom_id as uom_id, null as trigger_asset_id, null as trigger_attribute_id, aat.created_utc as created_utc, aat.sequential_number as sequential_number, aat.decimal_place, aat.thousand_separator
 from asset_attribute_dynamic_mapping am
 inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 

 union all 
 select am.asset_id,am.id as attribute_id, aat.data_type , aat.attribute_type as attribute_type, aat.name as attribute_name, am.enabled_expression as enabled_expression, am.expression_compile as expression_compile, aat.uom_id as uom_id, am.trigger_asset_id as trigger_asset_id, am.trigger_attribute_id as trigger_attribute_id, aat.created_utc as created_utc, aat.sequential_number as sequential_number, aat.decimal_place, aat.thousand_separator
 from asset_attribute_runtime_mapping  am
 inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 

 union all 
 select am.asset_id,am.id as attribute_id, aat.data_type , aat.attribute_type as attribute_type, aat.name as attribute_name, null as enabled_expression, null as expression_compile, aat.uom_id as uom_id, null as trigger_asset_id, null as trigger_attribute_id, aat.created_utc as created_utc, aat.sequential_number as sequential_number, aat.decimal_place, aat.thousand_separator
 from asset_attribute_integration_mapping am
 inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 

 union all 
 select am.asset_id,am.id as attribute_id, aat.data_type , aat.attribute_type as attribute_type , aat.name as attribute_name, null as enabled_expression, null as expression_compile, aat.uom_id as uom_id, null as trigger_asset_id, null as trigger_attribute_id, aat.created_utc as created_utc, aat.sequential_number as sequential_number, aat.decimal_place, aat.thousand_separator
 from asset_attribute_static_mapping am
 inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 

 union all 
 select aa.asset_id, aa.id as attribute_id, aa.data_type, aa.attribute_type , aa.name as attribute_name, null as enabled_expression, aa.expression_compile as expression_compile, aa.uom_id as uom_id, null as trigger_asset_id, null as trigger_attribute_id, aa.created_utc as created_utc, aa.sequential_number as sequential_number, aa.decimal_place, aa.thousand_separator
 from asset_attributes aa
 where aa.attribute_type != 'runtime'

 union all 
 select aa.asset_id, aa.id as attribute_id, aa.data_type, aa.attribute_type , aa.name as attribute_name, aar.enabled_expression as enabled_expression, aar.expression_compile as expression_compile, aa.uom_id as uom_id, aar.trigger_asset_id as trigger_asset_id, aar.trigger_attribute_id as trigger_attribute_id, aa.created_utc as created_utc, aa.sequential_number as sequential_number, aa.decimal_place, aa.thousand_separator
 from asset_attributes aa
 inner join asset_attribute_runtimes aar on aa.id = aar.asset_attribute_id
 where aa.attribute_type = 'runtime'

 union all 
 select am.asset_id,am.id as attribute_id, aat.data_type , aat.attribute_type as attribute_type , aat.name as attribute_name, null as enabled_expression, null as expression_compile, aat.uom_id as uom_id, null as trigger_asset_id, null as trigger_attribute_id, am.created_utc as created_utc, 1 as sequential_number, aat.decimal_place, aat.thousand_separator
 from asset_attribute_alias_mapping am
 inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id
