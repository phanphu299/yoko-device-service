UPDATE asset_attribute_runtime_mapping
SET is_trigger_visibility = false
WHERE is_trigger_visibility IS NULL;

UPDATE asset_attribute_runtimes
SET is_trigger_visibility = false
WHERE is_trigger_visibility IS NULL;