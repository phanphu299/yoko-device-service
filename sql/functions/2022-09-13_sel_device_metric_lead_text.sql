drop function if EXISTS fnc_sel_device_metric_lead_text;
create or replace FUNCTION  fnc_sel_device_metric_lead_text(deviceId varchar(255), metricKey varchar(255), startDate timestamp, endDate timestamp)
returns TABLE (
 value text
, duration double precision
, signal_quality_code smallint
) 
as
$func$
select 
  s.value
, extract(epoch from LEAD(s._ts) OVER(ORDER BY s._ts) - s._ts) duration
, s.signal_quality_code
from (
	
 --start 
 select * from (
 	select startDate as _ts
	 , dms.device_id
	 , dms.metric_key
	 , dms.value
	 , dms.signal_quality_code
	from device_metric_series_text dms 
	where dms.device_id  = deviceId and dms.metric_key  = metricKey
	and dms._ts < startDate 
	order by dms._ts desc
	limit 1 ) st
    
 union all
	select 
		  dms._ts
		, dms.device_id
	 	, dms.metric_key
	 	, dms.value
		, dms.signal_quality_code
		from device_metric_series_text dms 
		where dms.device_id  = deviceId and dms.metric_key  = metricKey
		and dms._ts >= startDate and dms._ts < endDate
		
 union  all

  --end 
 select * from (
 	select endDate as _ts
		 , dms.device_id
	 	 , dms.metric_key
		 , dms.value 
		 , dms.signal_quality_code
	from device_metric_series_text dms 
	where dms.device_id  = deviceId and dms.metric_key  = metricKey
	and dms._ts >= startDate and dms._ts < endDate
	order by dms._ts desc
	limit 1 ) et
) s 
$func$ 
LANGUAGE sql;

