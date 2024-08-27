
insert into assets (id, name, asset_template_id, retention_days, parent_asset_id, created_by, resource_path)
values ('7f5e7a78-97c3-4fc8-8949-e385830e3a9c', 'Child 2 template only', 'eb716893-807a-4eb1-81fc-2490d20aca8a', 90, 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69','thanh.tran@yokogawa.com', 'objects/e1627e07-7ae1-45fd-85e9-0cbf7948ca69/children/7f5e7a78-97c3-4fc8-8949-e385830e3a9c');

-- attribute mapping XYZAcceleration
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('5196134b-9f29-4b63-87a8-7d4431ee4efd','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '62f394e4-13ae-4c4b-a434-05574b108211', '00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- attribute mapping ZVelocity
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb5','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '6bbb6993-6baf-4f6b-a3a7-cedfb9aec416', '00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- attribute mapping XYZTemperature
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb0','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '841a8f0a-c6de-4ac9-ad4e-52be332116b9', '00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- attribute mapping XVelocity
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb1','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '6420a1c5-e88e-4254-b935-f15f44d4887e', '00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- attribute mapping DiagStatusDetailWord
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb2','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '2721d03c-a8b5-4a2c-8d86-0773cf20d7b2', '00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- attribute mapping tmst
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb3','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '750a9c4c-81c1-4408-8b60-df6a5429ca41', '00-00-64-ff-fe-a3-8f-e0','tmst');

-- attribute CalculatedXYZTemperature
insert into asset_attribute_dynamic_mapping(id, asset_id, asset_attribute_template_id, device_id, metric_key)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cd5','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '07cbada0-779d-4500-a030-1c091171c174', '00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- attribute runtime mapping attribute
insert into asset_attribute_runtime_mapping(id,asset_id, asset_attribute_template_id,trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb6','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', 'a9334f3b-bb86-4327-b383-fb28a3c4b34f', '7f5e7a78-97c3-4fc8-8949-e385830e3a9c','5196134b-9f29-4b63-87a8-7d4431ee4efd', true, '${5196134b-9f29-4b63-87a8-7d4431ee4efd}$ + ${f9892b29-8f5c-41d4-8c89-11a4f4289cb5}$ * 2;', 'return (double)request["3a0c23c4-c954-4851-917a-05817bf1d166"] + (double)request["3c412678-9011-4a01-88ff-005a9be78b62"] * 2;', true);

-- attribute static mapping attribute
insert into asset_attribute_static_mapping(id,asset_id, asset_attribute_template_id, "value", is_overridden)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb7','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '67a73d1b-0237-43ec-88d0-22c5ced1288f', 'from template text1', false );

-- attribute static mapping attribute
insert into asset_attribute_static_mapping(id, asset_id, asset_attribute_template_id, "value", is_overridden)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb8','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', '78c91c58-0f03-46e9-866c-c7c1ad9dcb56', '100', false );

-- runtime attribute with output value 
insert into asset_attribute_runtime_mapping(id,asset_id, asset_attribute_template_id, enabled_expression, is_trigger_visibility)
values ('f9892b29-8f5c-41d4-8c89-11a4f4289cb9','7f5e7a78-97c3-4fc8-8949-e385830e3a9c', 'a3e4ef31-fabb-4cd0-aca5-21053cd1bf49', false, false);