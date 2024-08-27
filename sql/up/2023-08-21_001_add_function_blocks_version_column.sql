CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;

ALTER TABLE function_blocks
ADD COLUMN version uuid DEFAULT uuid_generate_v4();