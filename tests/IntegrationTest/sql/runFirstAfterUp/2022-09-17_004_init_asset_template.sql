insert into asset_templates(id, name, created_by)
values ('eb716893-807a-4eb1-81fc-2490d20aca8a','Sushi Sensor Template','thanh.tran@yokogawa.com');

-- dynamic attribute XYZAcceleration
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('62f394e4-13ae-4c4b-a434-05574b108211', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'XYZAcceleration','dynamic', 'double');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ( '62f394e4-13ae-4c4b-a434-05574b108211', 'Place XYZAcceleration here', 'XYZAcceleration', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- dynamic attribute ZVelocity
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('6bbb6993-6baf-4f6b-a3a7-cedfb9aec416', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'ZVelocity','dynamic', 'double');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ('6bbb6993-6baf-4f6b-a3a7-cedfb9aec416', 'Place ZVelocity here', 'ZVelocity', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- dynamic attribute XYZTemperature
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('841a8f0a-c6de-4ac9-ad4e-52be332116b9', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'XYZTemperature','dynamic', 'double');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ('841a8f0a-c6de-4ac9-ad4e-52be332116b9', 'Place XYZTemperature here', 'XYZTemperature', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- dynamic attribute XVelocity
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('6420a1c5-e88e-4254-b935-f15f44d4887e', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'XVelocity','dynamic', 'double');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ('6420a1c5-e88e-4254-b935-f15f44d4887e', 'Place XVelocity here', 'XVelocity', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- dynamic attribute DiagStatusDetailWord
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('2721d03c-a8b5-4a2c-8d86-0773cf20d7b2', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'DiagStatusDetailWord','dynamic', 'text');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ('2721d03c-a8b5-4a2c-8d86-0773cf20d7b2', 'Place DiagStatusDetailWord here', 'DiagStatusDetailWord', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- dynamic attribute tmst
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('750a9c4c-81c1-4408-8b60-df6a5429ca41', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'tmst','dynamic', 'int');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ('750a9c4c-81c1-4408-8b60-df6a5429ca41', 'Place tmst here', 'tmst', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- runtime attribute CalculatedXYZTemperature
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('07cbada0-779d-4500-a030-1c091171c174', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'CalculatedXYZTemperature Template','dynamic', 'double');
insert into asset_attribute_template_dynamics ( asset_attribute_template_id, markup_name, metric_key, device_template_id)
values ('07cbada0-779d-4500-a030-1c091171c174', 'Place CalculatedXYZTemperature here', 'CalculatedXYZTemperature', '92889f2a-a069-4bb1-b89d-4af1f662d474');

-- runtime attribute 
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('a9334f3b-bb86-4327-b383-fb28a3c4b34f', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'RuntimeCalculatedXYZTemperature','runtime', 'double');

-- trigger by: XYZAcceleration
insert into asset_attribute_template_runtimes (asset_attribute_template_id, markup_name,trigger_asset_template_id, trigger_attribute_id,enabled_expression, expression, expression_compile )
values ('a9334f3b-bb86-4327-b383-fb28a3c4b34f', 'test template 1', '62f394e4-13ae-4c4b-a434-05574b108211','6420a1c5-e88e-4254-b935-f15f44d4887e', true, '${62f394e4-13ae-4c4b-a434-05574b108211}$ + ${6bbb6993-6baf-4f6b-a3a7-cedfb9aec416}$ * 2', 'return (double)request["62f394e4-13ae-4c4b-a434-05574b108211"] + (double)request["6bbb6993-6baf-4f6b-a3a7-cedfb9aec416"] * 2' );

-- runtime attribute with output value 
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type)
values ('a3e4ef31-fabb-4cd0-aca5-21053cd1bf49', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'XYZAccelerationOutput','runtime', 'double');
insert into asset_attribute_template_runtimes (asset_attribute_template_id,enabled_expression )
values ('a3e4ef31-fabb-4cd0-aca5-21053cd1bf49', false);

-- static text
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type, value)
values ('67a73d1b-0237-43ec-88d0-22c5ced1288f', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'Static Text1','static', 'text', 'template text1');

-- static int
insert into asset_attribute_templates (id, asset_template_id, name, attribute_type, data_type, value)
values ('78c91c58-0f03-46e9-866c-c7c1ad9dcb56', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 'Static Int','static', 'int', '1');

-- update