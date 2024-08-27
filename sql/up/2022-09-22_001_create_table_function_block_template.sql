-- function_block_categories
create table if not exists function_block_categories
(
	id uuid DEFAULT uuid_generate_v4() primary key,
	name varchar(255) not null,
	parent_category_id uuid null,
	deleted boolean not null default false,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);

-- function_blocks
create table if not exists function_blocks
(
	id uuid DEFAULT uuid_generate_v4() primary key,
	name varchar(255) not null,
    block_content text,
	type varchar(50),
    category_id uuid null,
    is_active boolean not null default true,
	deleted boolean not null default false,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_categories       FOREIGN KEY(category_id)  	  REFERENCES function_block_categories(id) ON DELETE CASCADE
);

-- function_block_bindings
create table if not exists function_block_bindings
(
	id uuid DEFAULT uuid_generate_v4() primary key,
	key varchar(255) not null,
	data_type varchar(15) not null,
	default_value varchar(255) null,
	--binding_type varchar(15) not null, -- input,
	description varchar(255) null,
	-- asset_template_id uuid null,
	-- asset_attribute_id uuid null,
	function_block_id uuid not null,
	deleted boolean not null default false,
	is_input boolean not null default true,
    CONSTRAINT fk_function_blocks       FOREIGN KEY(function_block_id)  	  REFERENCES function_blocks(id) ON DELETE CASCADE
);

-- function_block_templates
create table if not exists function_block_templates
(
	id uuid DEFAULT uuid_generate_v4() primary key,
	name varchar(255) not null,
	design_content text,
	content text,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
	deleted boolean not null default false
);

-- function_block_template_nodes
create table if not exists function_block_template_nodes
(
	id uuid DEFAULT uuid_generate_v4() primary key,
	template_id uuid not null,
	block_id uuid not null,
	block_type varchar(15) not null,
	sequential_number int,
	CONSTRAINT fk_function_block_template_nodes_template_id		FOREIGN KEY(template_id) 	REFERENCES function_block_templates(id) ON DELETE CASCADE,
	CONSTRAINT fk_function_block_template_nodes_block_id 		FOREIGN KEY(block_id) 		REFERENCES function_blocks(id) ON DELETE CASCADE
);