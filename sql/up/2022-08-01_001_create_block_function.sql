CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;

CREATE TABLE IF NOT EXISTS block_functions (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    name varchar(255) NOT NULL, 
    content text NULL, 
    trigger_type varchar(50), -- should be lookup
    trigger_content text NULL, -- json content varchar(50) NULL, 
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    deleted boolean NOT NULL DEFAULT false,
    status varchar(2) NOT NULL default 'CR',
    executed_utc TIMESTAMP WITHOUT TIME ZONE NULL,
    CONSTRAINT uq_block_functions_name UNIQUE (name)
);