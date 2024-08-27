CREATE TABLE IF NOT EXISTS  asset_attribute_alias_mapping (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid NOT NULL,
    alias_asset_id uuid NULL,
    alias_attribute_id uuid NULL, 
    asset_attribute_template_id uuid NULL, 
    created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_asset_attribute_alias_mapping_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE,
	CONSTRAINT fk_asset_attribute_alias_mapping_alias_attr_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE
);