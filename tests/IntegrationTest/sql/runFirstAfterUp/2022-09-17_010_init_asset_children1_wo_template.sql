
insert into assets (id, name, asset_template_id, retention_days, parent_asset_id, created_by, resource_path)
values ('cd872742-088c-4b31-ab24-7426b3cfc5b3', 'Child 3 without template', null, 90, 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69','thanh.tran@yokogawa.com', 'objects/e1627e07-7ae1-45fd-85e9-0cbf7948ca69/children/cd872742-088c-4b31-ab24-7426b3cfc5b3');

---- ASSET ATTRIBUTE
-- dynamic attribute XYZAcceleration
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'XYZAcceleration Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5210004','00-00-64-ff-fe-a3-8f-e0','XYZAcceleration');

-- dynamic attribute ZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'ZVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5220004','00-00-64-ff-fe-a3-8f-e0','ZVelocity');

-- dynamic attribute XYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'XYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5230004','00-00-64-ff-fe-a3-8f-e0','XYZTemperature');

-- dynamic attribute XVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'XVelocity Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5240004','00-00-64-ff-fe-a3-8f-e0','XVelocity');

-- dynamic attribute DiagStatusDetailWord
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'DiagStatusDetailWord Attribute', 'dynamic', 'text');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5250004','00-00-64-ff-fe-a3-8f-e0','DiagStatusDetailWord');

-- dynamic attribute tmst
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'tmst Attribute', 'dynamic', 'int');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5260004','00-00-64-ff-fe-a3-8f-e0','tmst');

-- dynamic attribute CalculatedXYZTemperature
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'CalculatedXYZTemperature Attribute', 'dynamic', 'double');
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('a9028757-f79a-44f2-8a3b-9c7cf5270004','00-00-64-ff-fe-a3-8f-e0','CalculatedXYZTemperature');

-- runtime attribute double CalculatedZVelocity
insert into asset_attributes (id, asset_id, name, attribute_type, data_type)
values ('a9028757-f79a-44f2-8a3b-9c7ce5270004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'CalculatedZVelocity Attribute', 'runtime', 'double');
insert into asset_attribute_runtimes(asset_attribute_id,trigger_asset_id, trigger_attribute_id, enabled_expression, expression, expression_compile, is_trigger_visibility)
values ('a9028757-f79a-44f2-8a3b-9c7ce5270004','cd872742-088c-4b31-ab24-7426b3cfc5b3', 'a9028757-f79a-44f2-8a3b-9c7cf5210004',true,'${a9028757-f79a-44f2-8a3b-9c7cf5210004}$ * 2;', 'return (double)request["a9028757-f79a-44f2-8a3b-9c7cf5210004"] * 2;', true);

-- runtime attribute double CalculatedZVelocityOutput
insert into asset_attributes (id,  asset_id, name, attribute_type, data_type, enabled_expression)
values ( 'a9028757-f79a-44f2-8a3b-9c7de5270004','cd872742-088c-4b31-ab24-7426b3cfc5b3', 'CalculatedZVelocity Attribute Output', 'runtime', 'double', false);

-- static text attribute
insert into asset_attributes (id,  asset_id, name, attribute_type, data_type, "value")
values ('a9028757-f79a-44f2-8a3b-9c7df5270004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'Static Text Attribute', 'static', 'text', 'text1');

-- static int attribute
insert into asset_attributes (id,  asset_id, name, attribute_type, data_type, "value")
values ('a9028757-f79a-44f2-8a3b-9c7da5270004', 'cd872742-088c-4b31-ab24-7426b3cfc5b3', 'Static Int Attribute', 'static', 'int', '10');