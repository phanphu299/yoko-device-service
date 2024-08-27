create or replace view v_asset_attribute_snapshots
as
select am.asset_id, am.id as attribute_id, sn.data_type, coalesce (sn.value, '') as value, sn._ts as _ts, aat.attribute_type as attribute_type, sn.device_id as device_id, sn.metric_key as metric_key, cast(null as uuid) as integration_id , sn.signal_quality_code as signal_quality_code, sn.last_good_value as last_good_value , sn._lts as _lts
from asset_attribute_dynamic_mapping am
inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 
inner join v_device_metrics sn on  am.device_id  = sn.device_id and am.metric_key  = sn.metric_key

union all
select am.asset_id, am.id as attribute_id, aat.data_type, coalesce (am.value, '') as value, am._ts as _ts, aat.attribute_type as attribute_type, am.device_id as device_id, am.metric_key as metric_key, cast(null as uuid) as integration_id , null as signal_quality_code , null as last_good_value, null as _lts
from asset_attribute_command_mapping am
inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 

union all 
select am.asset_id,am.id as attribute_id, aat.data_type , coalesce (sn.value, '') as value, sn._ts as _ts, aat.attribute_type as attribute_type, null as device_id, null as metric_key, cast(null as uuid) as integration_id , null as signal_quality_code , null as last_good_value, null as _lts
from asset_attribute_runtime_mapping  am
inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 
left join asset_attribute_runtime_snapshots sn on am.id  = sn.asset_attribute_id and am.asset_id  = sn.asset_id

union all 
select am.asset_id,am.id as attribute_id, aat.data_type , coalesce (sn.value, '') as value, sn._ts as _ts, aat.attribute_type as attribute_type, coalesce (am.device_id, sn.device_id ) as device_id, coalesce ( am.metric_key, sn.metric_key ) as metric_key, coalesce(am.integration_id, sn.integration_id) as integration_id  , null as signal_quality_code , null as last_good_value, null as _lts
from asset_attribute_integration_mapping am
inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 
left join device_metric_external_snapshots sn  on am.device_id  = sn.device_id  and am.integration_id  = sn.integration_id  and am.metric_key  = sn.metric_key 

union all 
select am.asset_id,am.id as attribute_id, aat.data_type , coalesce (cast (am.value as varchar(1024)), '') as value, null as _ts, aat.attribute_type as attribute_type , null as device_id, null as metric_key, cast(null as uuid) as integration_id , null as signal_quality_code , null as last_good_value, null as _lts
from asset_attribute_static_mapping am
inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 

union all 
select aa.asset_id, aad.asset_attribute_id as attribute_id, sn.data_type, coalesce (sn.value, '') as value, sn._ts as _ts, aa.attribute_type as attribute_type, sn.device_id as device_id, sn.metric_key as metric_key, cast(null as uuid) as integration_id , sn.signal_quality_code as signal_quality_code , sn.last_good_value as last_good_value, sn._lts as _lts
from asset_attribute_dynamic aad
inner join asset_attributes aa on aad.asset_attribute_id = aa.id 
inner join v_device_metrics sn on aad.device_id = sn.device_id and aad.metric_key = sn.metric_key
where aa.attribute_type  = 'dynamic'

union all 
select aa.asset_id, aad.asset_attribute_id as attribute_id, aa.data_type, coalesce (aad.value, '') as value, aad._ts as _ts, aa.attribute_type as attribute_type, aad.device_id as device_id, aad.metric_key as metric_key, cast(null as uuid) as integration_id , null as signal_quality_code , null as last_good_value, null as _lts
from asset_attribute_commands aad
inner join asset_attributes aa on aad.asset_attribute_id = aa.id
where aa.attribute_type  = 'command'

union all 
select aad.asset_id, aad.id as attribute_id, aad.data_type, coalesce (sn.value, '') as value, sn._ts as _ts, aad.attribute_type as attribute_type , null as device_id, null as metric_key, cast(null as uuid) as integration_id, null as signal_quality_code , null as last_good_value, null as _lts
from asset_attributes aad
left join asset_attribute_runtime_snapshots sn on aad.id  = sn.asset_attribute_id 
where aad.attribute_type  = 'runtime'

-- union all 
-- select aa.asset_id, am.asset_attribute_id as attribute_id, aa.data_type, coalesce (sn.value, '') as value, sn._ts as _ts, aa.attribute_type as attribute_type , coalesce(am.device_id, sn.device_id) as device_id, coalesce (am.metric_key, sn.metric_key) as metric_key, coalesce(am.integration_id, sn.integration_id)  as integration_id, null as signal_quality_code 
-- from asset_attribute_integration am
-- inner join asset_attributes aa on am.asset_attribute_id = aa.id 
-- left join device_metric_external_snapshots sn  on am.device_id  = sn.device_id  and am.integration_id  = sn.integration_id  and am.metric_key  = sn.metric_key 
-- where aa.attribute_type  = 'integration'

union all 
select aad.asset_id, aad.id as attribute_id, aad.data_type, coalesce (cast (aad.value as varchar(1024)), '') as value, null as _ts, aad.attribute_type as attribute_type, null as device_id, null as metric_key, cast(null as uuid) as integration_id, null as signal_quality_code , null as last_good_value, null as _lts
from asset_attributes aad
where attribute_type   = 'static'
-- trigger for update from change snapshot value column type
-- trigger for update from change the device_id column
-- trigger v_asset_attribute_snapshots after run 2023-09-05-001_alter_device_increase_size_device_id