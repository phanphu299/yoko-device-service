drop view if exists v_asset_attribute_alias;
create or replace view v_asset_attribute_alias
as
-- select aadm.device_id , aadm.metric_key , aat.data_type, aadm.asset_id, aadm.id as attribute_id
-- from asset_attribute_dy_alias aadm 
-- inner join asset_attribute_templates aat on aadm.asset_attribute_template_id  = aat.id 

--union all
select aa.asset_id
, aa.id as attribute_id
, aal.alias_asset_id
, aal.alias_attribute_id
from asset_attribute_alias aal 
inner join asset_attributes aa on aal.asset_attribute_id  = aa.id

union all

select am.asset_id
, am.id as attribute_id
, am.alias_asset_id
, am.alias_attribute_id
from asset_attribute_alias_mapping am;
