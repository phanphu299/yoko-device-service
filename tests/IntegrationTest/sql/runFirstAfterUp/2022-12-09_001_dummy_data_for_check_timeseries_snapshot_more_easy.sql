
-- 2022-11-25 00:00:00.000  =   1669334400000    
-- 2022-11-25 00:01:00.000  =   1669334460000

-- 2022-11-26 00:00:00.000  =   1669420800000   //3.333
-- 2022-11-26 00:01:00.000  =   1669420860000   //4.4444

-- 2100-01-01 00:00:00.000  =   4102444800000   //5.55555
-- 2100-01-01 00:01:00.000  =   4102444860000   //6.66666


-- Data mẫu có các bản ghi:
-- Value ở quá khứ (11/2022)
-- 1.1
-- 2.22
-- 3.333
-- 4.4444

-- Value ở tương lai (năm 2100)
-- 5.55555
-- 6.666666


-- UseCustomTimeRange=True
-- 	Runtime, Dynamic
-- 		Query start->end trong khoảng 3.333->4.4444 Actual: 4.4444  => ok
-- 		Query start->end trong khoảng 3.333->5.55555 Actual: 5.55555  => ok
-- 		Query start->end trong khoảng không có data Actual: []  => ok
-- 		Alias link đến type này như trên => ok
-- 	Static => luôn lấy 6.666666 -> ok
	

-- UseCustomTimeRange=False
-- 	Runtime, Dynamic 
-- 		Query start->end trong khoảng 3.333->4.4444 Actual: 6.666666
-- 		Query start->end trong khoảng 3.333->5.55555 Actual: 5.55555
-- 		Query start->end trong khoảng không có data Actual: 6.666666
-- 		Alias link đến type này cũng như trên



-- ###########  Runtime -- 71323c27-db2a-4ceb-9541-e4ad6fb24d19
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '71323c27-db2a-4ceb-9541-e4ad6fb24d19','e1627e07-7ae1-45fd-85e9-0cbf7948ca69', 'last_value_diff_output', 'runtime', 'double', false);

insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('71323c27-db2a-4ceb-9541-e4ad6fb24d19', false, false);

-- series
insert into asset_attribute_runtime_series values 
('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19',1.1 , '2022-11-25 00:00:00.000', 90),
('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19',2.22, '2022-11-25 00:01:00.000', 90),
('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19',3.333, '2022-11-26 00:00:00.000', 90),
('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19',4.4444, '2022-11-26 00:01:00.000', 90),
('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19',5.55555, '2100-01-01 00:00:00.000', 90),
('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19',6.666666, '2100-01-01 00:01:00.000', 90);

--snapshot 
insert into asset_attribute_runtime_snapshots values ('e1627e07-7ae1-45fd-85e9-0cbf7948ca69','71323c27-db2a-4ceb-9541-e4ad6fb24d19' , 6.666666, '2100-01-01 00:01:00.000');

-- Alias for Runtime -- 3745348c-6a06-4410-9cb7-cb108101cee4
insert into asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number, value)
values ('3745348c-6a06-4410-9cb7-cb108101cee4', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69',  'Alias Runtime Attribute ut', 'alias', 'double', 1, '');

insert into asset_attribute_Alias(asset_attribute_id, Alias_asset_id, Alias_attribute_id, created_utc, updated_utc)
values ('3745348c-6a06-4410-9cb7-cb108101cee4', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69', '71323c27-db2a-4ceb-9541-e4ad6fb24d19', current_timestamp ,current_timestamp);

-- ########### End Runtime



-- ###########  Dynamic -- fb035f6d-c31b-4196-8206-ca7fe49a4226
insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'RotateDegree','RotateDegree', 2, 'double'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';


insert into asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number)
values ('fb035f6d-c31b-4196-8206-ca7fe49a4226', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69', 'RotateDegree Attribute ut', 'dynamic', 'double', 1);
insert into asset_attribute_dynamic ( asset_attribute_id, device_id, metric_key)
values ('fb035f6d-c31b-4196-8206-ca7fe49a4226','00-00-64-ff-fe-a3-8f-e0','RotateDegree');

-- series
INSERT INTO public.device_metric_series (device_id,metric_key,value,"_ts",retention_days) VALUES
('00-00-64-ff-fe-a3-8f-e0','RotateDegree',1.1 , '2022-11-25 00:00:00.000', 90),
('00-00-64-ff-fe-a3-8f-e0','RotateDegree',2.22, '2022-11-25 00:01:00.000', 90),
('00-00-64-ff-fe-a3-8f-e0','RotateDegree',3.333, '2022-11-26 00:00:00.000', 90),
('00-00-64-ff-fe-a3-8f-e0','RotateDegree',4.4444, '2022-11-26 00:01:00.000', 90),
('00-00-64-ff-fe-a3-8f-e0','RotateDegree',5.55555, '2100-01-01 00:00:00.000', 90),
('00-00-64-ff-fe-a3-8f-e0','RotateDegree',6.666666, '2100-01-01 00:01:00.000', 90);

--snapshot
INSERT INTO public.device_metric_snapshots (device_id,metric_key,value, last_good_value, "_ts", "_lts") VALUES ('00-00-64-ff-fe-a3-8f-e0','RotateDegree',6.666666, 6.666666,'2100-01-01 00:01:00.000', '2100-01-01 00:01:00.000');

-- Alias for Dynamic serries -- 63f839bb-993c-487c-8487-396ec404e605
insert into asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number, value)
values ('63f839bb-993c-487c-8487-396ec404e605', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69',  'Alias Dynamic Attribute ut', 'alias', 'double', 1, '');

insert into asset_attribute_Alias(asset_attribute_id, Alias_asset_id, Alias_attribute_id, created_utc, updated_utc)
values ('63f839bb-993c-487c-8487-396ec404e605', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69', 'fb035f6d-c31b-4196-8206-ca7fe49a4226', current_timestamp ,current_timestamp);

-- ########### End Dynamic



-- ###########  Static -- bc125a38-45c6-41b8-a9da-247ae2bfc7da

insert into asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number, value)
values ('bc125a38-45c6-41b8-a9da-247ae2bfc7da', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69', 'Static Attribute new ut', 'static', 'double', 1, 6.666666);

-- Alias for Static serries -- dc7a2184-da31-416c-99f5-b0b99d1932e9
insert into asset_attributes (id, asset_id, name, attribute_type, data_type, sequential_number, value)
values ('dc7a2184-da31-416c-99f5-b0b99d1932e9', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69',  'Alias Static Attribute new ut', 'alias', 'double', 1, '');

insert into asset_attribute_Alias(asset_attribute_id, Alias_asset_id, Alias_attribute_id, created_utc, updated_utc)
values ('dc7a2184-da31-416c-99f5-b0b99d1932e9', 'e1627e07-7ae1-45fd-85e9-0cbf7948ca69', 'bc125a38-45c6-41b8-a9da-247ae2bfc7da', current_timestamp ,current_timestamp);

-- ########### End Static
