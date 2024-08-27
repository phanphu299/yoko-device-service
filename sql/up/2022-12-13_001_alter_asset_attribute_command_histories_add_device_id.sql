alter table asset_attribute_command_histories add column device_id varchar(255) NOT NULL;
alter table asset_attribute_command_histories add column metric_key varchar(255) NOT NULL;
alter table asset_attribute_command_histories drop column created_utc;
alter table asset_attribute_command_histories drop column _ts;