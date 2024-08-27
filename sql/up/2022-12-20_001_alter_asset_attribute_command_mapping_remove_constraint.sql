ALTER TABLE asset_attribute_command_mapping ALTER COLUMN value DROP NOT NULL,
                                            ALTER COLUMN device_id DROP NOT NULL,
                                            ALTER COLUMN metric_key DROP NOT NULL;

ALTER TABLE asset_attribute_commands ALTER COLUMN device_id DROP NOT NULL,
                                     ALTER COLUMN metric_key DROP NOT NULL;
