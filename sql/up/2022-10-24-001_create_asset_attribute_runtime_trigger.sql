CREATE TABLE asset_attribute_runtime_triggers (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    asset_id uuid not null, 
    attribute_id uuid not null,
    trigger_asset_id uuid not null, 
    trigger_attribute_id uuid not null,
    CONSTRAINT fk_asset_attribute_runtime_triggers_asset_id FOREIGN KEY(asset_id) REFERENCES assets(id) ON DELETE CASCADE,
    CONSTRAINT fk_asset_attribute_runtime_triggers_trigger_asset_id FOREIGN KEY(trigger_asset_id) REFERENCES assets (id) ON DELETE CASCADE
);