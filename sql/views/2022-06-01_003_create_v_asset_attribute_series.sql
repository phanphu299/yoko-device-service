create or replace view v_asset_attribute_series
as
select 
aa.asset_id as asset_id
, aad.asset_attribute_id as attribute_id
, dms._ts  as _ts
, dms.value as value
from device_metric_series dms
inner join asset_attribute_dynamic aad on dms.device_id  = aad.device_id  and dms.metric_key  = aad.metric_key
inner join asset_attributes aa on aad.asset_attribute_id  = aa.id 

union all

select 
aadm.asset_id as asset_id
, aadm.id as attribute_id
, dms._ts  as _ts
, dms.value as value
from device_metric_series dms
inner join asset_attribute_dynamic_mapping aadm  on  dms.device_id = aadm.device_id and dms.metric_key  = aadm.metric_key 


union all 
select 
aars.asset_id  as asset_id
, aars.asset_attribute_id as attribute_id
, aars._ts  as _ts
, aars.value as value
from asset_attribute_runtime_series aars;
-- trigger for update