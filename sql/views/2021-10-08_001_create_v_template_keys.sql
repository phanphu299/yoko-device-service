create or replace view v_device_key
as
SELECT distinct d.id device_id, td.key as device_key
FROM devices d 
inner join device_templates t ON  t.id  = d.device_template_id 
inner join template_payloads tp on tp.device_template_id  = t.id 
inner join template_details td  on td.template_payload_id  =tp.id
inner  join template_key_types tkt  on td.key_type_id  = tkt.id 
where tkt.name  = 'device_id' and td.enabled = true
-- trigger for update from change the device_id column