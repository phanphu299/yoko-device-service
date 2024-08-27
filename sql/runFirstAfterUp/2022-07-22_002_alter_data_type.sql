update data_types 
set name = 'double' where name = 'decimal';

update 
   asset_attributes
set 
   expression_compile = REPLACE (
      expression_compile ,
    '(decimal)',
    '(double)'
   );
   
update 
   asset_attribute_templates
set 
   expression_compile = REPLACE (
      expression_compile ,
    '(decimal)',
    '(double)'
   );
   
 update
 	template_details
 set 
   expression_compile = REPLACE (
      expression_compile ,
    '(decimal)',
    '(double)'
   );