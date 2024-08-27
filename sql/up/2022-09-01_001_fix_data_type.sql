-- update data_type
update asset_attributes
set data_type = dt.name
from data_types dt 
where asset_attributes.data_type_id = dt.id;

-- update data_type for asset_attribute_templates
update asset_attribute_templates
set data_type = dt.name
from data_types dt where asset_attribute_templates.data_type_id = dt.id;

-- update data_type for template_details
update template_details
set data_type = dt.name
from data_types dt where template_details.data_type_id = dt.id;


-- update data_type for asset_attribute_templates
update template_bindings
set data_type = dt.name
from data_types dt where template_bindings.data_type_id = dt.id;