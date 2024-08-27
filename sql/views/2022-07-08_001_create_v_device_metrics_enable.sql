CREATE OR REPLACE VIEW v_device_metrics_enable
AS 
SELECT DISTINCT d.id AS device_id,
    td.key AS metric_key,
    td.data_type AS data_type
   FROM template_details td
     JOIN template_payloads tp ON td.template_payload_id = tp.id
     JOIN device_templates dt ON tp.device_template_id = dt.id
     JOIN devices d ON dt.id = d.device_template_id
  WHERE td.enabled = true;
  -- trigger for update from change the device_id column