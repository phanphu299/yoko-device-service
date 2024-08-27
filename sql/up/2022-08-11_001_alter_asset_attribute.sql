alter table asset_attributes add data_type varchar(255);
alter table asset_attribute_templates add data_type varchar(255);
alter table template_details add data_type varchar(255);
alter table template_bindings add data_type varchar(255);
ALTER TABLE template_details ALTER COLUMN data_type_id DROP NOT NULL;
alter table template_bindings ALTER COLUMN data_type_id DROP NOT NULL;

-- update data_type
update asset_attributes
set data_type = dt.name
from asset_attributes aa
inner join data_types dt on aa.data_type_id = dt.id;

-- update data_type for asset_attribute_templates
update asset_attribute_templates
set data_type = dt.name
from asset_attribute_templates aa
inner join data_types dt on aa.data_type_id = dt.id;

-- update data_type for template_details
update template_details
set data_type = dt.name
from template_details aa
inner join data_types dt on aa.data_type_id = dt.id;


-- update data_type for asset_attribute_templates
update template_bindings
set data_type = dt.name
from template_bindings aa
inner join data_types dt on aa.data_type_id = dt.id;
