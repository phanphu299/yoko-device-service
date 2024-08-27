drop view if exists v_asset_attribute_snapshots;
drop view if exists v_device_snapshot;
drop view if exists v_device_metrics;
alter table device_metric_external_snapshots alter  column value type text;
alter table device_metric_snapshots alter column value type text;
alter table asset_attribute_runtime_snapshots alter column value type text;
