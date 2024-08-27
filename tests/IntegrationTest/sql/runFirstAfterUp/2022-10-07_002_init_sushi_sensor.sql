insert into assets (id, name, asset_template_id, retention_days, created_by, resource_path)
values ('6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'Sushi Output',  null, 90,'thanh.tran@yokogawa.com', 'objects/6ff1e0d2-be85-49a0-9f52-da8c7312c628');

-- runtime attributes
-- runtime attribute double CalculatedZVelocityOutput
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( 'b112bc22-d50a-4954-8466-f7d6917e0d15','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'snapshot_value_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('b112bc22-d50a-4954-8466-f7d6917e0d15', false, false);
--
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '03dcf108-e140-4c2c-8895-ff7e4f3c8a1f','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'last_time_diff_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('03dcf108-e140-4c2c-8895-ff7e4f3c8a1f', false, false);
--
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( 'bd4511f0-ee4f-4128-82e8-f20625dd1b9b','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'nearest_value_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('bd4511f0-ee4f-4128-82e8-f20625dd1b9b', false, false);
--
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '83f718f3-b5be-4bf8-85d3-58863e9859c4','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'last_value_diff_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('83f718f3-b5be-4bf8-85d3-58863e9859c4', false, false);
--
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '2e163ee5-d18e-4a0b-90c2-a1ff9835a23d','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'sum_aggregration_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('2e163ee5-d18e-4a0b-90c2-a1ff9835a23d', false, false);
--
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '8e509dd6-1593-4274-9c1f-c2707bba9eb5','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'duration_in_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('8e509dd6-1593-4274-9c1f-c2707bba9eb5', false, false);
--
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '32aff557-aed8-41bd-9cbd-3cc516813884','6ff1e0d2-be85-49a0-9f52-da8c7312c628', 'count_in_output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('32aff557-aed8-41bd-9cbd-3cc516813884', false, false);