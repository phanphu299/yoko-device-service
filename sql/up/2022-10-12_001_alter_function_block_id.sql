alter table function_block_executions alter COLUMN template_id drop not null;
alter table function_block_executions add column function_block_id uuid;
ALTER TABLE function_block_executions ADD CONSTRAINT fk_function_block_executions_function_block_id FOREIGN KEY (function_block_id) REFERENCES function_blocks(id);