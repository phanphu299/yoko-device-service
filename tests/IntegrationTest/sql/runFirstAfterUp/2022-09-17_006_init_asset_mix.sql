
insert into assets (id, name, asset_template_id, retention_days, created_by, resource_path)
values ('e8b2720a-9202-4742-a9d4-c440c4a5b463', 'Mix Asset from Template e8b2720a-9202-4742-a9d4-c440c4a5b463', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 90,'thanh.tran@yokogawa.com', 'objects/e8b2720a-9202-4742-a9d4-c440c4a5b463');

-- attribute mapping XYZAcceleration
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('5de84433-55d8-4bba-af8d-141d41e88e12','e8b2720a-9202-4742-a9d4-c440c4a5b463', '62f394e4-13ae-4c4b-a434-05574b108211', '00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- attribute mapping ZVelocity
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f329a','e8b2720a-9202-4742-a9d4-c440c4a5b463', '6bbb6993-6baf-4f6b-a3a7-cedfb9aec416', '00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- attribute mapping XYZTemperature
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3290','e8b2720a-9202-4742-a9d4-c440c4a5b463', '841a8f0a-c6de-4ac9-ad4e-52be332116b9', '00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- attribute mapping XVelocity
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3291','e8b2720a-9202-4742-a9d4-c440c4a5b463', '6420a1c5-e88e-4254-b935-f15f44d4887e', '00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- attribute mapping DiagStatusDetailWord
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3292','e8b2720a-9202-4742-a9d4-c440c4a5b463', '2721d03c-a8b5-4a2c-8d86-0773cf20d7b2', '00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- attribute mapping tmst
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3293','e8b2720a-9202-4742-a9d4-c440c4a5b463', '750a9c4c-81c1-4408-8b60-df6a5429ca41', '00-00-64-ff-fe-a3-8f-e0','tmst');

-- attribute CalculatedXYZTemperature
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3294','e8b2720a-9202-4742-a9d4-c440c4a5b463', '07cbada0-779d-4500-a030-1c091171c174', '00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- attribute runtime mapping attribute
insert into asset_attribute_runtime_mapping(id, asset_id, asset_attribute_template_id, trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3295','e8b2720a-9202-4742-a9d4-c440c4a5b463', 'a9334f3b-bb86-4327-b383-fb28a3c4b34f','e8b2720a-9202-4742-a9d4-c440c4a5b463' , '5de84433-55d8-4bba-af8d-141d41e88e12',true, '${5de84433-55d8-4bba-af8d-141d41e88e12}$ + ${a3ddc3e1-347c-41c4-bcb9-506cc25f329a}$ * 2;', 'return (double)request["5de84433-55d8-4bba-af8d-141d41e88e12"] + (double)request["a3ddc3e1-347c-41c4-bcb9-506cc25f329a"] * 2;', true);

-- runtime attribute with output value 
insert into asset_attribute_runtime_mapping(id, asset_id, asset_attribute_template_id, enabled_expression, is_trigger_visibility)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3296','e8b2720a-9202-4742-a9d4-c440c4a5b463', 'a3e4ef31-fabb-4cd0-aca5-21053cd1bf49', false, false);

-- attribute static mapping attribute
insert into asset_attribute_static_mapping(id, asset_id, asset_attribute_template_id, "value", is_overridden)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3297','e8b2720a-9202-4742-a9d4-c440c4a5b463', '67a73d1b-0237-43ec-88d0-22c5ced1288f', 'from template text1', false );

-- attribute static mapping attribute
insert into asset_attribute_static_mapping(id,  asset_id, asset_attribute_template_id, "value", is_overridden)
values ('a3ddc3e1-347c-41c4-bcb9-506cc25f3298', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', '78c91c58-0f03-46e9-866c-c7c1ad9dcb56', '100', false );

---- ASSET ATTRIBUTE
-- dynamic attribute XYZAcceleration
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'Dynamic XYZAcceleration Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210001','00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- dynamic attribute ZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'ZVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220001','00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- dynamic attribute XYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'XYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230001','00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- dynamic attribute XVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'XVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240001','00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- dynamic attribute DiagStatusDetailWord
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'DiagStatusDetailWord Attribute', 'dynamic', 'text');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250001','00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- dynamic attribute tmst
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'tmst Attribute', 'dynamic', 'int');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260001','00-00-64-ff-fe-a3-8f-e0','tmst');

-- dynamic attribute CalculatedXYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270001', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'CalculatedXYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270001','00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- runtime attribute double CalculatedZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('13eab098-6031-4f94-91d9-00968587a71b','e8b2720a-9202-4742-a9d4-c440c4a5b463', 'CalculatedZVelocity Attribute', 'runtime', 'double');
insert into asset_attribute_runtimes(asset_attribute_id,trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('13eab098-6031-4f94-91d9-00968587a71b','e8b2720a-9202-4742-a9d4-c440c4a5b463', 'a9028757-f79a-44f2-8a3b-9c7cf5220001',true,'${a9028757-f79a-44f2-8a3b-9c7cf5220001}$ * 2;', 'return (double)request["a9028757-f79a-44f2-8a3b-9c7cf5220001"] * 2;', true);

-- runtime attribute double CalculatedZVelocityOutput
insert into asset_attributes (id, asset_id, name, attribute_type, data_type, enabled_expression)
values ('300567db-e002-4e27-8da4-e9dff5cebc7b', 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'CalculatedZVelocityAttributeOutput', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('300567db-e002-4e27-8da4-e9dff5cebc7b', false, false);

-- static text attribute
insert into asset_attributes ( asset_id, name, attribute_type, data_type, "value")
values ( 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'Static Text Attribute', 'static', 'text', 'text1');

-- static int attribute
insert into asset_attributes ( asset_id, name, attribute_type, data_type, "value")
values ( 'e8b2720a-9202-4742-a9d4-c440c4a5b463', 'Static Int Attribute', 'static', 'int', '10');