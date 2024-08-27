CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='function_blocks' and column_name='version')
  THEN
     ALTER TABLE function_blocks ADD COLUMN version uuid DEFAULT uuid_generate_v4();
  END IF;
END $$