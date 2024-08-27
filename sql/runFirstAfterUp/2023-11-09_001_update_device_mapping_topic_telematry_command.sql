UPDATE devices SET has_command = null;

UPDATE devices SET command_topic = CONCAT(device_content::json ->> 'projectId', '/devices/', id, '/commands'), telemetry_topic = '$ahi/telemetry', has_command = true 
WHERE device_content IS NOT NULL AND (device_content LIKE '%BROKER_EMQX_COAP%' OR device_content LIKE '%BROKER_EMQX_MQTT%');