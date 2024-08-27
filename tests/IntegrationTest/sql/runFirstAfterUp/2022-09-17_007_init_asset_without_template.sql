
insert into assets (id, name, retention_days, created_by, resource_path)
values ('d880c314-d133-4f10-940f-1fcddc669337', 'Asset without template',  90,'thanh.tran@yokogawa.com', 'objects/d880c314-d133-4f10-940f-1fcddc669337');

---- ASSET ATTRIBUTE
-- dynamic attribute XYZAcceleration
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210002', 'd880c314-d133-4f10-940f-1fcddc669337', 'XYZAcceleration Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210002','00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- dynamic attribute ZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220002', 'd880c314-d133-4f10-940f-1fcddc669337', 'ZVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220002','00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- dynamic attribute XYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230002', 'd880c314-d133-4f10-940f-1fcddc669337', 'XYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230002','00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- dynamic attribute XVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240002', 'd880c314-d133-4f10-940f-1fcddc669337', 'XVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240002','00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- dynamic attribute DiagStatusDetailWord
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250002', 'd880c314-d133-4f10-940f-1fcddc669337', 'DiagStatusDetailWord Attribute', 'dynamic', 'text');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250002','00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- dynamic attribute tmst
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260002', 'd880c314-d133-4f10-940f-1fcddc669337', 'tmst Attribute', 'dynamic', 'int');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260002','00-00-64-ff-fe-a3-8f-e0','tmst');

-- dynamic attribute CalculatedXYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270002', 'd880c314-d133-4f10-940f-1fcddc669337', 'CalculatedXYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270002','00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- runtime attribute double CalculatedZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type, expression, expression_compile, enabled_expression)
values ('d92f2606-e22a-414f-8692-e9c8f6517a9d', 'd880c314-d133-4f10-940f-1fcddc669337', 'CalculatedZVelocity Attribute', 'runtime', 'double', '${a9028757-f79a-44f2-8a3b-9c7cf5220002}$ * 2;', 'return (double)request["a9028757-f79a-44f2-8a3b-9c7cf5220002"] * 2;', true);
insert into asset_attribute_runtimes(asset_attribute_id,trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('d92f2606-e22a-414f-8692-e9c8f6517a9d','e8b2720a-9202-4742-a9d4-c440c4a5b463', 'a9028757-f79a-44f2-8a3b-9c7cf5220001',true,'${a9028757-f79a-44f2-8a3b-9c7cf5220001}$ * 2;', 'return (double)request["a9028757-f79a-44f2-8a3b-9c7cf5220001"] * 2;', true);

-- runtime attribute double CalculatedZVelocityOutput
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '7542dc90-45f4-4ad8-b059-f81bd53c9343','d880c314-d133-4f10-940f-1fcddc669337', 'CalculatedZVelocity Attribute Output', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('7542dc90-45f4-4ad8-b059-f81bd53c9343', false, false);

-- static text attribute
insert into asset_attributes ( asset_id, name, attribute_type, data_type, "value")
values ( 'd880c314-d133-4f10-940f-1fcddc669337', 'Static Text Attribute', 'static', 'text', 'text1');

-- static int attribute
insert into asset_attributes ( asset_id, name, attribute_type, data_type, "value")
values ( 'd880c314-d133-4f10-940f-1fcddc669337', 'Static Int Attribute', 'static', 'int', '10');