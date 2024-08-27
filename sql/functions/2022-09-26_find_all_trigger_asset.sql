drop function if exists find_all_asset_trigger_by_device_id;
CREATE OR REPLACE FUNCTION find_all_asset_trigger_by_device_id(deviceId varchar(255))
 RETURNS TABLE(asset_id uuid, attribute_id uuid, trigger_asset_id uuid, trigger_attribute_id uuid, trigger_level integer, metric_key varchar(255))
 LANGUAGE plpgsql
AS $function$
declare
-- variable declaration
begin
-- stored procedure body
	return query 
	  with RECURSIVE cte_assets AS  (
			select 
			1 as trigger_level,
			rt.asset_id as asset_id ,
			rt.attribute_id  as attribute_id,
			rt.trigger_asset_id as trigger_asset_id,
			rt.trigger_attribute_id as trigger_attribute_id,
			dy.metric_key as metric_key
			from v_asset_attribute_dynamics dy
			inner join v_asset_attribute_runtimes rt on dy.asset_id  = rt.trigger_asset_id  and dy.attribute_id  = rt.trigger_attribute_id
			where dy.device_id  = deviceId

			union all 
			select cte.trigger_level + 1 as trigger_level,
			rrt.asset_id as asset_id ,
			rrt.attribute_id  as attribute_id,
			rrt.trigger_asset_id as trigger_asset_id,
			rrt.trigger_attribute_id as trigger_attribute_id,
			cte.metric_key as metric_key
			from v_asset_attribute_runtimes rrt 
			join cte_assets cte on cte.asset_id  = rrt.trigger_asset_id  and cte.attribute_id  = rrt.trigger_attribute_id
			)
			select cte.asset_id, cte.attribute_id, cte.trigger_asset_id, cte.trigger_attribute_id, cte.trigger_level, cte.metric_key from cte_assets cte
			union 
			select 
			rt.asset_id as asset_id ,
			rt.attribute_id  as attribute_id,
			rt.trigger_asset_id as trigger_asset_id,
			rt.trigger_attribute_id as trigger_attribute_id,
			1 as trigger_level,
			null as metric_key -- alias, the key should be null
			from find_alias_asset_related_to_device(deviceId) s
			inner join v_asset_attribute_runtimes rt on s.attribute_id  = rt.trigger_attribute_id
			;
	
end  $function$