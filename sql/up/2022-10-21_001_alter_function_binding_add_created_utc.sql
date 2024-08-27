alter table function_block_bindings add column created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW();
alter table function_block_bindings add column sequential_number INT;