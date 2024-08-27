create table device_template_tmp (
template_id int,
device_template_id uuid
);

insert into device_template_tmp(template_id, device_template_id)
select id, uuid_generate_v4()
from templates ;

insert into device_templates (id, name, total_metric)
select tmp.device_template_id, t.name, t.total_metric
from templates t
inner join device_template_tmp tmp on t.id = tmp.template_id;


update template_payloads 
set device_template_id = tmp.device_template_id
from device_template_tmp tmp
where template_payloads.template_id = tmp.template_id;


update devices 
set device_template_id = tmp.device_template_id
from device_template_tmp tmp
where devices.template_id = tmp.template_id;

update template_bindings 
set device_template_id = tmp.device_template_id
from device_template_tmp tmp
where template_bindings.template_id = tmp.template_id;

update asset_attribute_template_dynamics 
set device_template_id = tmp.device_template_id
from device_template_tmp tmp
where asset_attribute_template_dynamics.template_id = tmp.template_id;

drop table device_template_tmp;


