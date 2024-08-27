DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='function_block_executions' and column_name='running_trigger_asset_id')
  THEN
     alter table function_block_executions add running_trigger_asset_id uuid;
  END IF;
END $$