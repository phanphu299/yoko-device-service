drop view if exists v_asset_attribute_snapshots cascade;
drop view if exists v_asset_attribute_static cascade;
drop view if exists v_asset_attribute_series cascade;
drop view if exists v_asset_attribute_series_text cascade;
drop view if exists v_asset_attributes cascade;
drop view if exists v_device_metrics cascade;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_monthly;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_yearly;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_minute;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_five_minutes;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_ten_minutes;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_fifteen_minutes;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_thirty_minutes;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_hourly;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_daily;
DROP MATERIALIZED VIEW IF EXISTS v_time_series_weekly;

alter table asset_attribute_runtime_series alter column value type double precision;
alter table device_metric_series alter column value type double precision;
alter table device_metric_series_text alter column value type text;
alter table asset_attribute_runtime_series_text alter column value type text;