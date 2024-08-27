CREATE TABLE IF NOT EXISTS asset_attribute_template_runtimes (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_template_id uuid NOT NULL,
    markup_name varchar(255) NULL,
    trigger_asset_template_id uuid NULL,
    trigger_attribute_id uuid NULL,
    enabled_expression bool not null default true,
    expression text NULL,
    expression_compile text NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_template_runtimes_attribute_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS  asset_attribute_runtimes (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_attribute_id uuid NOT NULL,
    trigger_asset_id uuid NULL,
    trigger_attribute_id uuid NULL,
    enabled_expression bool not null default true,
    expression text NULL,
    expression_compile text NULL,
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_runtimes_asset_id FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_runtimes_device_id FOREIGN KEY(trigger_asset_id) REFERENCES assets (id) ON DELETE CASCADE
);
