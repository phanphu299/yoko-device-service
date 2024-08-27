--Function Block Execution
alter table function_blocks add column resource_path varchar(1024);
alter table function_blocks add column created_by varchar(50) default 'thanh.tran@yokogawa.com';
CREATE INDEX IF NOT EXISTS idx_function_blocks_resourcepath_createdby ON function_blocks(resource_path, created_by);