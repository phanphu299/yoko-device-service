--: Turn into hypertable
SELECT create_hypertable('device_metric_series','_ts', 'device_id' ,4, chunk_time_interval => INTERVAL '1 day');
--: Turn into hypertable
SELECT create_hypertable('asset_attribute_runtime_series','_ts', 'asset_id' , 4, chunk_time_interval => INTERVAL '1 day');