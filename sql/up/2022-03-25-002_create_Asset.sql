CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;
--asset template

CREATE TABLE IF NOT EXISTS asset_templates (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    name varchar(255) NULL, 
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    current_user_upn varchar(255) DEFAULT NULL,
    "current_timestamp" TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_user_upn varchar(255) DEFAULT NULL,
    request_lock_timestamp TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_timeout TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    CONSTRAINT uq_asset_templates UNIQUE (name)
);

CREATE TABLE IF NOT EXISTS asset_attribute_templates (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_template_id uuid NOT NULL,
    name varchar(255) NOT NULL,
    expression varchar(255) NULL,
    expression_compile text null,
    attribute_type varchar(255) NULL,
    data_type_id int NULL,
    uom_id int NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_asset_attribute_templates UNIQUE (asset_template_id, name),
    CONSTRAINT fk_asset_attribute_templates_asset_template_id FOREIGN KEY(asset_template_id) REFERENCES asset_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_templates_asset_data_type FOREIGN KEY(data_type_id) REFERENCES data_types (id) ON DELETE SET NULL,
    CONSTRAINT fk_asset_attribute_templates_uom_id FOREIGN KEY(uom_id) REFERENCES uoms (id) ON DELETE SET NULL
);


--asset 
CREATE TABLE IF NOT EXISTS assets (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    name varchar(255) NULL,
    parent_asset_id uuid NULL,
    asset_template_id uuid NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    is_root boolean DEFAULT true,
    current_user_upn varchar(255) DEFAULT NULL,
    "current_timestamp" TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_user_upn varchar(255) DEFAULT NULL,
    request_lock_timestamp TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_timeout TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    --type varchar(50) default 'standalone', --template/standalone/mix
    CONSTRAINT uq_assets UNIQUE (name, parent_asset_id),
    CONSTRAINT fk_asset_parent_asset_id FOREIGN KEY (parent_asset_id) REFERENCES assets(id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_parent_asset_template_id FOREIGN KEY (asset_template_id) REFERENCES asset_templates(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS  asset_attributes (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    name varchar(255) NOT NULL,
    expression varchar(255) NULL,
    expression_compile text NUll,
    value varchar(255) NULL,
    attribute_type varchar(255) NULL,
    data_type_id int NULL,
    uom_id int NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_asset_attributes UNIQUE (asset_id, name),
    CONSTRAINT fk_asset_attributes_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attributes_data_type_id FOREIGN KEY(data_type_id) REFERENCES data_types (id) ON DELETE SET NULL,
    CONSTRAINT fk_asset_attributes_uom_id FOREIGN KEY(uom_id) REFERENCES uoms (id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS  asset_attribute_dynamic (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_id uuid NOT NULL,
    device_id varchar(255) NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_dynamic_asset_id FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_dynamic_device_id FOREIGN KEY(device_id) REFERENCES devices (id) ON DELETE CASCADE
    --CONSTRAINT fk_asset_attribute_dynamic_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS  asset_attribute_integration (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_id uuid NOT NULL,
    --device_external_id int NULL,
    integration_id uuid NULL,
    device_id varchar(255) NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_integration_asset_attribute FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE
    --CONSTRAINT fk_asset_attribute_integration_integration_id FOREIGN KEY(integration_id) REFERENCES integrations (id) ON DELETE CASCADE
    --CONSTRAINT fk_asset_attribute_integration_device_external_id FOREIGN KEY(device_external_id) REFERENCES device_externals (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS  asset_attribute_alias (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_id uuid NOT NULL,
    alias_asset_id uuid NOT NULL,
    alias_attribute_id uuid NULL, 
    alias_attribute_template_id uuid NULL, 
    parent_id uuid NULL, 
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_alias_asset_attribute_id FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_alias_alias_asset_id FOREIGN KEY(alias_asset_id) REFERENCES assets (id) ON DELETE CASCADE,
	CONSTRAINT fk_asset_attribute_alias_alias_attribute_id FOREIGN KEY(alias_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
	CONSTRAINT fk_asset_attribute_alias_alias_attr_template FOREIGN KEY(alias_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_alias_device_id FOREIGN KEY(parent_id) REFERENCES asset_attribute_alias (id) ON DELETE SET NULL
);

--asset template

CREATE TABLE IF NOT EXISTS asset_attribute_template_integrations (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_template_id uuid NOT NULL,
    integration_markup_name varchar(255) NOT NULL,
    integration_id uuid NULL,
    device_markup_name varchar(255) NOT NULL,
    device_id varchar(255) NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
    --CONSTRAINT fk_asset_attribute_template_integrations_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asset_attribute_template_dynamics (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_template_id uuid NOT NULL,
    template_id int NOT NULL,
    markup_name varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_template_dynamics_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_template_dynamics_template_id FOREIGN KEY(template_id) REFERENCES templates (id) ON DELETE CASCADE
    --CONSTRAINT fk_asset_attribute_template_dynamics_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
);

-- mapping
CREATE TABLE IF NOT EXISTS asset_attribute_static_mapping (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    asset_attribute_template_id uuid NOT NULL,
    value text NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_static_mapping_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_static_mapping_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asset_attribute_runtime_mapping (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    asset_attribute_template_id uuid NOT NULL,
    expression text NULL,
    expression_compile text NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_runtime_mapping_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_runtime_mapping_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asset_attribute_dynamic_mapping (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    asset_attribute_template_id uuid NOT NULL,
    device_id varchar(255) NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_dynamic_mapping_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_dynamic_mapping_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE
    --CONSTRAINT fk_asset_attribute_dynamic_mapping_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS asset_attribute_integration_mapping (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    asset_attribute_template_id uuid NOT NULL,
    integration_id uuid NULL,
    device_id varchar(255) NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_integration_mapping_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_integration_mapping_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE
    --CONSTRAINT fk_asset_attribute_integration_mapping_asset_integration_id FOREIGN KEY(integration_id) REFERENCES integrations(id) ON DELETE CASCADE
);

-- media and table
CREATE TABLE IF NOT EXISTS media (
    id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name varchar(255),
    asset_id uuid NULL,
    blob_path varchar(2048) NOT NULL,
    content_type varchar(255) NOT NULL,
    file_size int NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT fk_media_assets_id FOREIGN KEY (asset_id) REFERENCES assets(id) ON DELETE SET NULL
);

create table if not exists tables
(
	id int not null generated always as identity,
	name varchar(255) not null,
	asset_id uuid null,
	created_utc timestamp without time zone not null default now(),
    updated_utc timestamp without time zone not null default now(),
     current_user_upn varchar(255) DEFAULT NULL,
    "current_timestamp" TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_user_upn varchar(255) DEFAULT NULL,
    request_lock_timestamp TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_timeout TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
	deleted boolean not null default false,
    script varchar null,
    old_name varchar(255) null,
	constraint pk_tables primary key(id),
	constraint fk_tables_assets_id foreign key(asset_id) references assets(id) ON DELETE SET NULL
);

create table if not exists columns
(
	id int not null generated always as identity,
	name varchar(255) not null,
	is_primary boolean not null,
	type_code varchar(50) not null,
	default_value varchar(255),
	table_id int not null,
    allow_null boolean not null default false,
    "type_name" varchar(50) null,
	constraint pk_columns primary key(id),
	constraint fk_columns_table_id foreign key(table_id) references tables(id)
);

CREATE TABLE IF NOT EXISTS public.table_list
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    table_name character varying(255) COLLATE pg_catalog."default" NOT NULL,
    asset_path text COLLATE pg_catalog."default" NOT NULL,
    enabled boolean NOT NULL DEFAULT FALSE,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    deleted boolean not null default false
)