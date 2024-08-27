update asset_attribute_templates set sequential_number = 1 where sequential_number is null;
update asset_attributes set sequential_number = 1 where sequential_number is null;
update asset_attribute_dynamic_mapping set sequential_number = 1 where sequential_number is null;
update asset_attribute_runtime_mapping set sequential_number = 1 where sequential_number is null;
update asset_attribute_static_mapping set sequential_number = 1 where sequential_number is null;
update asset_attribute_integration_mapping set sequential_number = 1 where sequential_number is null;

alter table asset_attribute_templates alter column sequential_number set not null;
alter table asset_attributes alter column sequential_number set not null;
alter table asset_attribute_dynamic_mapping alter column sequential_number set not null;
alter table asset_attribute_runtime_mapping alter column sequential_number set not null;
alter table asset_attribute_static_mapping alter column sequential_number set not null;
alter table asset_attribute_integration_mapping alter column sequential_number set not null;

alter table asset_attribute_templates alter column sequential_number set default 1;
alter table asset_attributes alter column sequential_number set default 1;
alter table asset_attribute_dynamic_mapping alter column sequential_number set default 1;
alter table asset_attribute_runtime_mapping alter column sequential_number set default 1;
alter table asset_attribute_static_mapping alter column sequential_number set default 1;
alter table asset_attribute_integration_mapping alter column sequential_number set default 1;