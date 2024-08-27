alter table asset_attribute_templates add column sequential_number INT;
alter table asset_attributes add column sequential_number INT;
alter table asset_attribute_dynamic_mapping add column sequential_number INT;
alter table asset_attribute_runtime_mapping add column sequential_number INT;
alter table asset_attribute_static_mapping add column sequential_number INT;
alter table asset_attribute_integration_mapping add column sequential_number INT;