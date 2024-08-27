create table if not exists asset_tables
(
	id uuid DEFAULT uuid_generate_v4(),
	name varchar(255) not null,
	asset_id uuid null,
	created_utc timestamp without time zone not null default now(),
    updated_utc timestamp without time zone not null default now(),
    	deleted boolean not null default false,
    script varchar null,
    old_name varchar(255) null,
	constraint pk_asset_tables primary key(id),
	constraint fk_asset_tables_assets_id foreign key(asset_id) references assets(id) ON DELETE SET NULL
);

create table if not exists asset_table_columns
(
	id int GENERATED ALWAYS AS IDENTITY,
	name varchar(255) not null,
	is_primary boolean not null,
	type_code varchar(50) not null,
	default_value varchar(255),
	asset_table_id uuid not null,
    allow_null boolean not null default false,
    "type_name" varchar(50) null,
    deleted boolean not null default false,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
	constraint pk_asset_table_columns primary key(id),
	constraint fk_asset_table_columns_asset_table_id foreign key(asset_table_id) references asset_tables(id)
);

CREATE TABLE IF NOT EXISTS asset_table_list
(
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    table_name character varying(255) COLLATE pg_catalog."default" NOT NULL,
    asset_path text COLLATE pg_catalog."default" NOT NULL,
    enabled boolean NOT NULL DEFAULT FALSE,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    deleted boolean not null default false
)