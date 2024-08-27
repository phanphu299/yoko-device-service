-- asset_attribute_runtime_series
CREATE INDEX idx_asset_attribute_runtime_series_asset_attribute_id_ts ON asset_attribute_runtime_series USING btree (asset_attribute_id, _ts);

CREATE INDEX idx_asset_attribute_runtime_series_assetid_asset_attributeid ON asset_attribute_runtime_series USING btree (asset_id, asset_attribute_id);

CREATE INDEX idx_asset_attribute_runtime_series_assetid_asset_attributeid_ts ON asset_attribute_runtime_series USING btree (asset_id, asset_attribute_id, _ts);

-- asset_attribute_runtime_series_text
CREATE INDEX idx_asset_attribute_runtime_seriestext_assetattributeid_ts ON asset_attribute_runtime_series_text USING btree (asset_attribute_id, _ts);

CREATE INDEX idx_asset_attribute_runtime_seriestext_assetid_assetattributeid ON asset_attribute_runtime_series_text USING btree (asset_id, asset_attribute_id);

-- asset_attribute_runtime_triggers
CREATE INDEX idx_asset_attribute_runtime_triggers_attribute_id_is_selected ON asset_attribute_runtime_triggers USING btree (attribute_id, is_selected);

-- asset_attribute_templates
CREATE INDEX idx_asset_attribute_templates_name_asset_template_id ON asset_attribute_templates USING btree ("name", asset_template_id);

-- device_metric_series
CREATE INDEX idx_device_metric_series_device_id_ts ON device_metric_series USING btree (device_id, _ts);

CREATE INDEX idx_device_metric_series_ts_device_id_metric_key ON device_metric_series USING btree (_ts, device_id, metric_key);

CREATE INDEX idx_device_metricseries_signalqualitycode_deviceid_metrickey ON device_metric_series USING btree (signal_quality_code, device_id, metric_key);

CREATE INDEX idx_device_metricseries_signalqualitycode_deviceid_metrickey_ts ON device_metric_series USING btree (signal_quality_code, device_id, metric_key, _ts);

-- device_metric_series_text
CREATE INDEX idx_device_metric_series_text_device_id__ts ON device_metric_series_text USING btree (device_id, _ts);

CREATE INDEX idx_device_metric_series_text__ts_device_id_metric_key ON device_metric_series_text USING btree (_ts, device_id, metric_key);

CREATE INDEX idx_devicemetricseriestext_signalqualitycode_deviceid_metrickey ON device_metric_series_text USING btree (signal_quality_code, device_id, metric_key);

-- device_templates
CREATE INDEX idx_device_templates_name ON device_templates USING btree ("name");

-- devices
CREATE INDEX idx_devices_deleted ON devices USING btree (deleted);

-- template_bindings
CREATE INDEX idx_template_bindings_device_template_id ON template_bindings USING btree (device_template_id);

-- template_details
CREATE INDEX idx_template_details_key_type_id_key ON template_details USING btree (key_type_id, "key");

CREATE INDEX idx_template_details_enabled_key_type_id ON template_details USING btree ("enabled", key_type_id);

CREATE INDEX idx_template_details_enabled_key ON template_details USING btree ("enabled", "key");

-- template_key_types
CREATE INDEX idx_template_key_types ON template_key_types USING btree (name);

-- template_payloads
CREATE INDEX idx_device_template_id ON template_payloads USING btree (device_template_id);