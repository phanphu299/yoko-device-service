alter table function_block_executions add column run_immediately boolean;
alter table function_block_executions add column trigger_asset_markup varchar(255);
alter table function_block_executions add column trigger_asset_id uuid;
alter table function_block_executions add column trigger_attribute_id uuid;