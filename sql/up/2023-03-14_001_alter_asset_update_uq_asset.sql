ALTER TABLE assets DROP CONSTRAINT uq_assets;
ALTER TABLE assets ADD CONSTRAINT uq_assets UNIQUE (name, created_by, parent_asset_id);