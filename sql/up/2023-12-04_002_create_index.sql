CREATE INDEX IF NOT EXISTS idx_asset_attribute_dynamic_mapping_device_id
    ON public.asset_attribute_dynamic_mapping USING btree
    (device_id)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_dynamic_mapping_asset_attribute_template_id
    ON public.asset_attribute_dynamic_mapping USING btree
    (asset_attribute_template_id)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_dynamic_asset_attribute_id
    ON public.asset_attribute_dynamic USING btree
    (asset_attribute_id)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_dynamic_device_id
    ON public.asset_attribute_dynamic USING btree
    (device_id)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_runtime_mapping_asset_attribute_template_id
    ON public.asset_attribute_runtime_mapping USING btree
    (asset_attribute_template_id)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_runtime_mapping_id_asset_attribute_template_id
ON public.asset_attribute_runtime_mapping USING btree
(id, asset_attribute_template_id)
TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_runtime_mapping_asset_id
    ON public.asset_attribute_runtime_mapping USING btree
    (asset_id)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_runtime_triggers_attribute_id
    ON public.asset_attribute_runtime_triggers USING btree
    (attribute_id ASC NULLS LAST)
    TABLESPACE pg_default;
CREATE INDEX IF NOT EXISTS idx_asset_attribute_runtime_triggers_is_selected
    ON public.asset_attribute_runtime_triggers USING btree
    (is_selected ASC NULLS LAST)
    TABLESPACE pg_default;	
CREATE INDEX IF NOT EXISTS idx_asset_attribute_alias_asset_attribute_id
    ON public.asset_attribute_alias USING btree
    (asset_attribute_id)
    TABLESPACE pg_default;
	
