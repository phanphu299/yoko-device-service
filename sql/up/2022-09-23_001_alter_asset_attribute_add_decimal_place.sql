alter table asset_attributes add column decimal_place int;
alter table asset_attributes add column thousand_separator bool;

alter table asset_attribute_templates add column decimal_place int;
alter table asset_attribute_templates add column thousand_separator bool;