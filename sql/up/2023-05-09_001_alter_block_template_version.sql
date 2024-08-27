CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;

ALTER TABLE function_block_templates DROP COLUMN IF EXISTS version;
ALTER TABLE function_block_executions DROP COLUMN IF EXISTS version;

ALTER TABLE function_block_templates ADD column version uuid DEFAULT uuid_generate_v4();
ALTER TABLE function_block_executions ADD column version uuid DEFAULT uuid_generate_v4();