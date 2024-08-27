--series
drop view if exists v_asset_attribute_series_text CASCADE;
alter table device_metric_series_text 
alter column value TYPE VARCHAR(1024);

drop view if exists  v_asset_attribute_series_text CASCADE;
alter table asset_attribute_runtime_series_text 
alter column value TYPE VARCHAR(1024);