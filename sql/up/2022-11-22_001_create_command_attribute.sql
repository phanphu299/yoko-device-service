CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;

CREATE TABLE IF NOT EXISTS asset_attribute_commands (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_id uuid NOT NULL,
    device_id varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
    value text NULL,
    row_version uuid NOT NULL,
    sequential_number int not null,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    deleted boolean not null default false,
    CONSTRAINT fk_asset_attribute_commands_attribute_id FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asset_attribute_command_histories (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_id uuid NOT NULL,
    value text NULL,
    row_version uuid NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    _ts timestamp NOT NULL,
    CONSTRAINT fk_asset_attribute_command_histories_attribute_id FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asset_attribute_template_commands (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_template_id uuid NOT NULL,
    device_template_id uuid NOT NULL,
    markup_name varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    deleted boolean not null default false,
    CONSTRAINT fk_asset_attribute_template_commands_asset_attribute_template_id FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates(id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_template_commands_device_template_id FOREIGN KEY(device_template_id) REFERENCES device_templates(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS asset_attribute_command_mapping (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    asset_attribute_template_id uuid NOT NULL,
    device_id varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
    value text NOT NULL,
    created_utc timestamp,
    updated_utc timestamp,
    row_version uuid NOT NULL,
    sequential_number int not null,
    CONSTRAINT fk_asset_attribute_command_mapping_asset_id FOREIGN KEY(asset_id) REFERENCES assets(id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_command_mapping_asset_attribute_template_id FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates(id) ON DELETE CASCADE
);