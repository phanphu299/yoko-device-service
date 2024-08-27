-- Asset Template
alter table asset_templates add column resource_path varchar(1024);
alter table asset_templates add column created_by varchar(50);
-- Asset
alter table assets add column resource_path varchar(1024);
alter table assets add column created_by varchar(50);
-- Device Template
alter table device_templates add column resource_path varchar(1024);
alter table device_templates add column created_by varchar(50);
-- Device
alter table devices add column resource_path varchar(1024);
alter table devices add column created_by varchar(50);

--Function Block Template
alter table function_block_templates add column resource_path varchar(1024);
alter table function_block_templates add column created_by varchar(50);


--Function Block Execution
alter table function_block_executions add column resource_path varchar(1024);
alter table function_block_executions add column created_by varchar(50);