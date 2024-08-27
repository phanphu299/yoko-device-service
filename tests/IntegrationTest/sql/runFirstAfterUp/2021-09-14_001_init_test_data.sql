insert into device_templates (id,name, created_by) values ('92889f2a-a069-4bb1-b89d-4af1f662d474','Thanh Test','thanh.tran@yokogawa.com');
insert into template_payloads (device_template_id, json_payload)
select id, '{
    "eui": "00-00-64-ff-fe-a3-8f-e0",
    "ack": "false",
    "appeui": "00-08-00-ff-fe-4a-85-0e",
    "chan": 4,
    "deveui": "00-00-64-ff-fe-a3-8f-e0",
    "freq": 922.2,
    "lsnr": 1,
    "rssi": -89,
    "seqn": 81,
    "size": 9,
    "timestamp": 1628273393856,
    "tmst": 2388162772,
    "XYZAcceleration": 0.65771484767578125,
    "ZVelocity": 0.154296875,
    "XYZTemperature": 29.125,
    "XVelocity": 29.125,
    "resource": "00-00-64-ff-fe-a3-8f-e0",
    "DiagStatusDetailWord":"Sensor in abnormal operation"
}'
from device_templates t  where name = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id, data_type, enabled) -- device_id
select tp.id,'eui','eui', 1, 'text' , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';


insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'timestamp','timestamp', 3, 'timestamp'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';


insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'XYZAcceleration','XYZAcceleration', 2,'double'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'ZYZVelocity','ZYZVelocity', 2, 'double'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'ZVelocity','ZVelocity', 2, 'double'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'XVelocity','XVelocity', 2, 'double'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'tmst','tmst', 2, 'int'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'XYZTemperature','XYZTemperature', 2, 'double'  , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, expression, expression_compile, enabled)
select tp.id,'CalculatedXYZTemperature','CalculatedXYZTemperature', 4, 'double' , '${XYZTemperature}$ * ${ZVelocity}$ / 30', 'return (double)request["XYZTemperature"] * (double)request["ZVelocity"] / 30;', true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'size','size', 2, 'int' , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';
insert into template_details (template_payload_id, key, "name", key_type_id,  data_type, enabled)
select tp.id,'DiagStatusDetailWord','DiagStatusDetailWord', 2, 'text' , true
from template_payloads tp
inner join device_templates  t on tp.device_template_id  = t.id 
where t.name  = 'Thanh Test';

-- insert into metrics (name, data_type_id)
-- select key, data_type_id
-- from template_details ;

insert into devices (id, name, status, device_template_id, updated_utc, created_by)

--insert into device_templates (device_id, template_id, status_id, _ts)
select '00-00-64-ff-fe-a3-8f-e0','unit test device 1', 'AC', id, current_timestamp, 'thanh.tran@yokogawa.com'
from device_templates where name = 'Thanh Test';

