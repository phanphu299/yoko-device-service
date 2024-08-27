-- function_block_snippets
create table if not exists function_block_snippets
(
	id uuid DEFAULT uuid_generate_v4() primary key,
	name varchar(255) not null,
	template_code text,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);
