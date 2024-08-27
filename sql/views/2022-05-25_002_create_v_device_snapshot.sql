drop view if exists v_device_snapshot;
create or replace view v_device_snapshot
as
select d.id as device_id, 
dms."_ts" as _ts, 
acm.command_data_timestamps, 
case 
    when (d.device_content like '%BROKER_EMQX_COAP%' or d.device_content like '%BROKER_EMQX_MQTT%') and d.device_content like '%"password":"%' then 'RG' -- registered
    when d.device_content like '%iot.azure-devices.net%' then 'RG' -- registered
    when dms._ts is not null then 'AC'
    else 'CR' -- created
end as status 
from devices d
left join (select device_id, max(_ts) as _ts from device_metric_snapshots group by device_id) dms on dms.device_id = d.id
left join (select device_id, now() as command_data_timestamps from asset_attribute_command_histories group by device_id) acm on acm.device_id = d.id
-- trigger for update from change snapshot value column type
-- trigger for update from change the device_id column