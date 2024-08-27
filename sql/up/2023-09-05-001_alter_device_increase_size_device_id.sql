DROP VIEW IF EXISTS v_asset_attribute_snapshots;
DROP VIEW IF EXISTS v_template_timestamp;
DROP VIEW IF EXISTS v_device_key;
DROP VIEW IF EXISTS v_device_snapshot;
DROP VIEW IF EXISTS v_device_metrics;
DROP VIEW IF EXISTS v_device_metrics_enable;

ALTER TABLE devices
ALTER COLUMN id TYPE varchar(100);

ALTER TABLE device_bindings
ALTER COLUMN device_id TYPE varchar(100);