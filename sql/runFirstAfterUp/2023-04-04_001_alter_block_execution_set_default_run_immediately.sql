alter table function_block_executions alter column run_immediately set default false;
update function_block_executions set run_immediately = false where run_immediately is null;