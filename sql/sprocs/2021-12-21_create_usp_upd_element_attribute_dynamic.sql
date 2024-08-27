-- create or replace procedure usp_upd_element_attribute_dynamic(assetId uuid)
-- language plpgsql
-- as $$
-- declare
-- -- variable declaration

-- begin
-- -- stored procedure body
-- 	--- float
-- 	update asset_attributes 
-- 	set value = s.value, updated_utc  = s._ts
-- 	from (
-- 	select msf.value 
-- 	,e.asset_id
-- 	, e.attribute_id
-- 	, msf._ts
-- 	from v_element_device_metrics e
-- 	inner join v_template_metric  m on e.metric_id  = m.metric_id and e.device_id= m.device_id and m.data_type  in ('double','int')
-- 	inner join device_metric_snapshots_float msf  on m.metric_key  = msf.metric_key  and m.device_id  = msf .device_id 
-- 	where e.asset_id  = assetId
-- 	) s
-- 	where s.asset_id = asset_attributes.asset_id and s.attribute_id = asset_attributes.id;

-- 	---- text
-- 	update asset_attributes 
-- 	set value = s.value, updated_utc  = s._ts
-- 	from (
-- 	select msf.value 
-- 	,e.asset_id
-- 	, e.attribute_id
-- 	, msf._ts
-- 	from v_element_device_metrics e
-- 	inner join v_template_metric  m on e.metric_id  = m.metric_id and e.device_id= m.device_id  and m.data_type  = 'text'
-- 	inner join device_metric_snapshots_text msf  on m.metric_key  = msf.metric_key  and m.device_id  = msf .device_id 
-- 	where e.asset_id  = assetId
-- 	) s
-- 	where s.asset_id = asset_attributes.asset_id and s.attribute_id = asset_attributes.id;

-- 	---- boolean
-- 	update asset_attributes 
-- 	set value = s.value, updated_utc  = s._ts
-- 	from (
-- 	select msf.value 
-- 	,e.asset_id
-- 	, e.attribute_id
-- 	, msf._ts
-- 	from v_element_device_metrics e
-- 	inner join v_template_metric  m on e.metric_id  = m.metric_id and e.device_id= m.device_id and m.data_type  = 'bool'
-- 	inner join device_metric_snapshots_boolean msf  on m.metric_key  = msf.metric_key  and m.device_id  = msf .device_id 
-- 	where e.asset_id  = assetId
-- 	) s
-- 	where s.asset_id = asset_attributes.asset_id and s.attribute_id = asset_attributes.id;
-- end; $$



