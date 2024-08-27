update asset_attribute_templates set data_type = 'double' where data_type = 'decimal';
update asset_attribute_templates set expression_compile = replace(expression_compile, '(decimal)', '(double)') where expression_compile is not null;

update asset_attributes set data_type = 'double' where data_type = 'decimal';
update asset_attributes set expression_compile = replace(expression_compile, '(decimal)', '(double)') where expression_compile is not null;

update template_details set data_type = 'double' where data_type = 'decimal';
update template_details set expression_compile = replace(expression_compile, '(decimal)', '(double)') where expression_compile is not null;
