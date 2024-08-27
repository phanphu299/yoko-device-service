create or replace view v_device_metrics
as
SELECT  d.id device_id, td.key as metric_key,  td.expression_compile, td.data_type as data_type, tkt.name as metric_type, dms.value, dms._ts, td.detail_id as detail_id, d.signal_quality_code  as signal_quality_code, dms.last_good_value as last_good_value, dms._lts as _lts
FROM devices d 
inner join device_templates t ON  t.id  = d.device_template_id 
inner join template_payloads tp on tp.device_template_id  = t.id 
inner join template_details td  on td.template_payload_id  =tp.id
inner  join template_key_types tkt  on td.key_type_id  = tkt.id 
left join device_metric_snapshots dms on d.id  = dms.device_id and td.key = dms.metric_key 
where td.enabled = true and tkt.name in ('metric', 'aggregation', 'timestamp')
order by td.id asc
-- trigger for update from change snapshot value column type
-- trigger for update from change the device_id column