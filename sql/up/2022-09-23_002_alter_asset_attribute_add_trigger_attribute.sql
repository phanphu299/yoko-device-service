--- add into asset template
alter table asset_attribute_runtime_mapping add column trigger_asset_id uuid;
alter table asset_attribute_runtime_mapping add column trigger_attribute_id uuid;
alter table asset_attribute_runtime_mapping add column enabled_expression bool default true;
ALTER TABLE asset_attribute_runtime_mapping  ADD CONSTRAINT fk_asset_attribute_runtime_mapping_asset FOREIGN KEY (trigger_asset_id) REFERENCES assets(id);

-- add constraint
alter table asset_attributes add column trigger_asset_id uuid;
alter table asset_attributes add column trigger_attribute_id uuid;
ALTER TABLE asset_attributes  ADD CONSTRAINT fk_asset_attributes_asset FOREIGN KEY (trigger_asset_id) REFERENCES assets(id);