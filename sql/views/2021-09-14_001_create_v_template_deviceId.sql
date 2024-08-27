drop view if exists v_template_deviceId;
create view v_template_deviceId
as
select distinct key from (select key from  template_details td
	 inner join template_key_types tkt  on td.key_type_id  = tkt.id 
	 inner join template_payloads tp  on td.template_payload_id  = tp.id 
	 inner join device_templates dt on tp.device_template_id  = dt.id 
	 where td.enabled = true and tkt.name = 'device_id' and key not in ('timestamp')
	 order by dt.updated_utc  desc
	 ) s ;
	 