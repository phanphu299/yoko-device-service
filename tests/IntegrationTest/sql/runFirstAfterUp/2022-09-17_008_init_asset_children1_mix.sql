
insert into assets (id, name, asset_template_id, retention_days, parent_asset_id, created_by, resource_path)
values ('34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'Child 1', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 90, 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69','thanh.tran@yokogawa.com', 'objects/e1627e07-7ae1-45fd-85e9-0cbf7948ca69/children/34b9ee44-a25e-4127-ac56-dd4f29204cf2');

-- attribute mapping XYZAcceleration
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('b20a95e4-08d3-49fd-9951-e7e2fa93939f','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '62f394e4-13ae-4c4b-a434-05574b108211', '00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- attribute mapping ZVelocity
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef6d','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '6bbb6993-6baf-4f6b-a3a7-cedfb9aec416', '00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- attribute mapping XYZTemperature
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef60','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '841a8f0a-c6de-4ac9-ad4e-52be332116b9', '00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- attribute mapping XVelocity
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef61','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '6420a1c5-e88e-4254-b935-f15f44d4887e', '00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- attribute mapping DiagStatusDetailWord
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef62','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '2721d03c-a8b5-4a2c-8d86-0773cf20d7b2', '00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- attribute mapping tmst
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef63','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '750a9c4c-81c1-4408-8b60-df6a5429ca41', '00-00-64-ff-fe-a3-8f-e0','tmst');

-- attribute CalculatedXYZTemperature
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef64','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '07cbada0-779d-4500-a030-1c091171c174', '00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- attribute runtime mapping attribute
insert into asset_attribute_runtime_mapping(id, asset_id, asset_attribute_template_id, trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef65','34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'a9334f3b-bb86-4327-b383-fb28a3c4b34f', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'b20a95e4-08d3-49fd-9951-e7e2fa93939f', true, '${b20a95e4-08d3-49fd-9951-e7e2fa93939f}$ + ${9d9fe845-dfa9-4cfa-b935-aaa76539ef6d}$ * 2;', 'return (double)request["3a0c23c4-c954-4851-917a-05817bf1d166"] + (double)request["3c412678-9011-4a01-88ff-005a9be78b62"] * 2;', true);

-- runtime attribute with output value 
insert into asset_attribute_runtime_mapping(id, asset_id, asset_attribute_template_id, enabled_expression, is_trigger_visibility)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef66','34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'a3e4ef31-fabb-4cd0-aca5-21053cd1bf49', false, false);

-- attribute static mapping attribute
insert into asset_attribute_static_mapping(id, asset_id, asset_attribute_template_id, "value", is_overridden)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef67','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '67a73d1b-0237-43ec-88d0-22c5ced1288f', 'from template text1', false );

-- attribute static mapping attribute
insert into asset_attribute_static_mapping(id,  asset_id, asset_attribute_template_id, "value", is_overridden)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef68','34b9ee44-a25e-4127-ac56-dd4f29204cf2', '78c91c58-0f03-46e9-866c-c7c1ad9dcb56', '100', false );

---- ASSET ATTRIBUTE
-- dynamic attribute XYZAcceleration
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'XYZAcceleration', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210003','00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- dynamic attribute ZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'ZVelocity', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220003','00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- dynamic attribute XYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'XYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230003','00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- dynamic attribute XVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'XVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240003','00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- dynamic attribute DiagStatusDetailWord
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'DiagStatusDetailWord Attribute', 'dynamic', 'text');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250003','00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- dynamic attribute tmst
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'tmst Attribute', 'dynamic', 'int');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260003','00-00-64-ff-fe-a3-8f-e0','tmst');

-- dynamic attribute CalculatedXYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270003', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'CalculatedXYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270003','00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- runtime attribute double CalculatedZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef70', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'CalculatedZVelocity Attribute', 'runtime', 'double');
insert into asset_attribute_runtimes(asset_attribute_id,trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef70','34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'a9028757-f79a-44f2-8a3b-9c7cf5220001',true,'${a9028757-f79a-44f2-8a3b-9c7cf5220003}$ * 2;', 'return (double)request["a9028757-f79a-44f2-8a3b-9c7cf5220003"] * 2;', true);


-- runtime attribute double CalculatedZVelocityOutput
insert into asset_attributes (id,  asset_id, name, attribute_type, data_type)
values ( '9d9fe845-dfa9-4cfa-b935-aaa76539ef71','34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'CalculatedZVelocity Attribute Output', 'runtime', 'double');
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef71', false, false);

-- static text attribute
insert into asset_attributes (id,  asset_id, name, attribute_type, data_type, "value")
values ( '9d9fe845-dfa9-4cfa-b935-aaa76539ef72','34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'Static Text Attribute', 'static', 'text', 'text1');

-- static int attribute
insert into asset_attributes (id, asset_id, name, attribute_type, data_type, "value")
values ('9d9fe845-dfa9-4cfa-b935-aaa76539ef73', '34b9ee44-a25e-4127-ac56-dd4f29204cf2', 'Static Int Attribute', 'static', 'int', '10');