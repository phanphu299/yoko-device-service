drop function if exists fnc_sel_asset_attribute_lead;
create or replace FUNCTION  fnc_sel_asset_attribute_lead(assetId uuid, attributeId uuid, startDate timestamp, endDate timestamp)
returns TABLE (
 value double precision
, duration double precision
) 
as
$func$
select 
  s.value
, extract(epoch from LEAD(s._ts) OVER(ORDER BY s._ts) - s._ts) duration
from (
	
 --start 
 select * from (
 	select startDate as _ts
	, dms.value 
	from asset_attribute_runtime_series  dms 
	where dms.asset_id  = assetId and dms.asset_attribute_id  = attributeId
	and dms._ts < startDate 
	order by dms._ts desc
	limit 1 ) st

 union all
	select 
		  dms._ts
	 	, dms.value 
		from asset_attribute_runtime_series  dms 
		where dms.asset_id  = assetId and dms.asset_attribute_id  = attributeId
		and dms._ts >= startDate and dms._ts < endDate

 union  all
  --end 
 select * from (
 	select endDate as _ts
		 , dms.value 
	from asset_attribute_runtime_series  dms 
	where dms.asset_id  = assetId and dms.asset_attribute_id  = attributeId
	and dms._ts >= startDate and dms._ts < endDate
	order by dms._ts desc
	limit 1 ) et
) s 
$func$ 
LANGUAGE sql;