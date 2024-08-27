alter table function_block_templates add column trigger_type varchar(50) null;
alter table function_block_templates add column trigger_content text null;
alter table function_block_template_nodes add column name varchar(255);
alter table function_block_template_nodes add column asset_markup_name varchar(255);
alter table function_block_template_nodes add column target_name varchar(255);