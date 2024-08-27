CREATE OR REPLACE VIEW v_template_timestamp AS
  SELECT td.key, d.id  as device_id
  FROM devices d
  join device_templates t on d.device_template_id = t.id 
  join template_payloads tp on t.id = tp.device_template_id 
  join template_details  td on tp.id = td.template_payload_id 
  join template_key_types tkt on tkt.id  = td.key_type_id 
  where tkt.name = 'timestamp' and td.enabled = true
  order by t.updated_utc  desc;
-- trigger for update from change the device_id column