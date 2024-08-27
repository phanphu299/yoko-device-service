DO $$BEGIN
  	INSERT INTO device_templates (id, name, created_by)
	VALUES ('f6683b15-0662-4f2d-99b2-44ac928bb66d', 'device_template_dummy', 'email@test.local');
	
	INSERT INTO template_payloads (device_template_id, json_payload)
	SELECT id, '{
		"device-id": "device-id",
	    "timestamp": 1628273393856,
	    "valueInt": 10,
	    "valueDouble" : 10.5
	}'
	FROM device_templates t
	WHERE name = 'device_template_dummy';
	
	INSERT INTO template_details (template_payload_id, KEY, "name", key_type_id, data_type, enabled)
	SELECT tp.id, 'device-id', 'device-id', 1, 'text' , TRUE
	FROM template_payloads tp
	INNER JOIN device_templates t ON
	tp.device_template_id = t.id
	WHERE t.name = 'device_template_dummy';

	INSERT INTO template_details (template_payload_id, KEY, "name", key_type_id, data_type, enabled)
	SELECT tp.id, 'timestamp', 'timestamp', 3, 'timestamp' , TRUE
	FROM template_payloads tp
	INNER JOIN device_templates t ON
	tp.device_template_id = t.id
	WHERE t.name = 'device_template_dummy';
	
	INSERT INTO template_details (template_payload_id, KEY, "name", key_type_id, data_type, enabled)
	SELECT tp.id, 'valueInt', 'valueInt', 2, 'int' , TRUE
	FROM template_payloads tp
	INNER JOIN device_templates t ON
	tp.device_template_id = t.id
	WHERE t.name = 'device_template_dummy';
END $$;


DO $$BEGIN
	INSERT INTO template_details (template_payload_id, KEY, "name", key_type_id, data_type, enabled)
	SELECT tp.id, 'valueDouble', 'valueDouble', 2, 'double' , TRUE
	FROM template_payloads tp
	INNER JOIN device_templates t ON
	tp.device_template_id = t.id
	WHERE t.name = 'device_template_dummy';
	
	INSERT INTO devices (id, name, status, device_template_id, updated_utc, created_by)
	SELECT 'device-id-for-dummy-data', 'device-id-for-dummy-data', 'AC', id,  (current_timestamp + INTERVAL '30 seconds'), 'email@test.local'
	FROM device_templates
	WHERE name = 'device_template_dummy';
	
	INSERT INTO assets (id, name, asset_template_id, retention_days, parent_asset_id, created_by, resource_path)
	VALUES ('baa77abc-56a8-404d-abee-43a4bbb160e2', 'AssetForDummyData', NULL, 90, NULL, 'email@test.local', 'objects/baa77abc-56a8-404d-abee-43a4bbb160e2');
	
	INSERT INTO asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number)
	VALUES ('ce08f8f2-2f2c-4959-ae61-c0f8b143d08f', 'baa77abc-56a8-404d-abee-43a4bbb160e2', 'AttibuteForInt', 'dynamic', 'int', 1);
	INSERT INTO asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
	VALUES ('ce08f8f2-2f2c-4959-ae61-c0f8b143d08f', 'device-id-for-dummy-data', 'valueInt');
	
	
	INSERT INTO asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number)
	VALUES ('c3eeef87-510b-4f16-9c83-5a87253578a8', 'baa77abc-56a8-404d-abee-43a4bbb160e2', 'AttibuteForDouble', 'dynamic', 'double', 2);
	INSERT INTO asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
	VALUES ('c3eeef87-510b-4f16-9c83-5a87253578a8', 'device-id-for-dummy-data', 'valueDouble');
END $$;


DO $$
DECLARE last_int_val int = 50;
		last_double_val decimal(18,2) = 20;
		count_for_change_value int = 0;
		last_value int = 0;
		next_value int = 0;
		final_value_change int = 0;
		start_ts timestamp := current_timestamp + INTERVAL '-6 days 5 hours';  -- -135 days
BEGIN
   	FOR i IN 1..10000 LOOP  --233270
	   	next_value = floor(random() * 11 - 5);
	    IF (abs(count_for_change_value) > 6 AND (count_for_change_value * next_value) < 0) THEN
			count_for_change_value = 4 * CASE WHEN next_value < 0 THEN -1 ELSE 1 END;
		ELSE
		  	count_for_change_value = count_for_change_value + CASE WHEN count_for_change_value < 0 THEN -1 ELSE 1 END;
		END IF;
		final_value_change = (abs(next_value) * CASE WHEN count_for_change_value < 0 THEN -1 ELSE 1 END);
		last_int_val = last_int_val + final_value_change;
		last_double_val = last_double_val + (final_value_change * floor(random() * 20 + 5)/ 10);
		last_double_val = floor(last_double_val * 100)/100;
		INSERT INTO public.device_metric_series (device_id,metric_key,value,"_ts",retention_days) VALUES 
		('device-id-for-dummy-data','valueInt', last_int_val, start_ts + INTERVAL '50 seconds' * i, 90),
		('device-id-for-dummy-data','valueDouble', last_double_val, start_ts + INTERVAL '50 seconds' * i, 90);
	END LOOP;
END $$;

DO $$BEGIN
	INSERT INTO public.device_metric_snapshots (device_id,metric_key,value, "_ts")
	SELECT device_id,metric_key,value, "_ts" FROM device_metric_series WHERE device_id = 'device-id-for-dummy-data' ORDER BY _ts DESC LIMIT 2;
END $$;