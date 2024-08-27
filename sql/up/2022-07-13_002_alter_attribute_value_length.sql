drop view if exists v_asset_attribute_snapshots cascade;
drop view if exists v_asset_attribute_static cascade;
drop view if exists v_asset_attribute_series cascade;

alter table asset_attributes alter column value type varchar(512);
alter table asset_attribute_runtime_series alter column value type numeric(309,20);
alter table asset_attribute_templates alter column value type varchar(512);