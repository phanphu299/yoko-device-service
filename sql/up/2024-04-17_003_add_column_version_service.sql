DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='function_block_executions' and column_name='feature_flag')
  THEN
     alter table function_block_executions add feature_flag smallint;
  END IF;
END $$