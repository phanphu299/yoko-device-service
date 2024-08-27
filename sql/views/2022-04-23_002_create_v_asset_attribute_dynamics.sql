create or replace view v_asset_attribute_dynamics
as
select aadm.device_id , aadm.metric_key , aat.data_type, aadm.asset_id, aadm.id as attribute_id
from asset_attribute_dynamic_mapping aadm 
inner join asset_attribute_templates aat on aadm.asset_attribute_template_id  = aat.id 

union all
select aad.device_id , aad.metric_key, aa.data_type, aa.asset_id, aa.id as attribute_id
from asset_attribute_dynamic aad 
inner join asset_attributes aa on aad.asset_attribute_id  = aa.id 

-- union all 
-- select vaas.asset_id, cast( vaas.device_id as varchar(255))
-- from v_asset_attribute_snapshots vaas 
-- inner join asset_attribute_alias aaa on vaas.asset_id  = aaa.alias_asset_id;