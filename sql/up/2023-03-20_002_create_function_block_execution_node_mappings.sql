CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;

CREATE TABLE IF NOT EXISTS function_block_execution_node_mappings (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    block_execution_id uuid NOT NULL,
    block_template_node_id uuid NOT NULL,
    asset_markup_name varchar(255)  NULL,
    asset_id uuid NULL,
    asset_name varchar(255)  NULL,
    target_name varchar(255)  NULL,
    "value" varchar(255) NULL,
    CONSTRAINT fk_function_block_execution_node_mappings_block_execution_id FOREIGN KEY(block_execution_id) REFERENCES function_block_executions(id) ON DELETE CASCADE,
    CONSTRAINT fk_function_block_execution_node_mappings_block_template_node_id FOREIGN KEY(block_template_node_id) REFERENCES function_block_template_nodes(id) ON DELETE CASCADE
);