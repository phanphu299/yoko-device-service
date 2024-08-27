CREATE OR REPLACE FUNCTION public.find_alias_asset_related_to_device_refactor(deviceid character varying)
 RETURNS TABLE(attribute_id uuid)
 LANGUAGE plpgsql
AS $function$
declare
-- variable declaration
begin
-- stored procedure body
	return query
			-- alias 
			with RECURSIVE cte_asset_alias AS  (
			select 
			1 as level
			, al.asset_id
            , al.attribute_id
            , al.alias_asset_id
            , al.alias_attribute_id
            , coalesce (dy.device_id, dy1.device_id)  as device_id
            , concat(al.attribute_id,'->',  al.alias_attribute_id) as path
			from v_asset_attribute_alias al
            left join v_asset_attribute_dynamics dy on dy.attribute_id = al.alias_attribute_id
            left join v_asset_attribute_dynamics dy1 on dy1.attribute_id = al.attribute_id
			where coalesce(dy.device_id, dy1.device_id) = deviceId
			
			union all 
			select 
            cte.level + 1 as level,
			cte.alias_asset_id as asset_id ,
			cte.alias_attribute_id  as attribute_id,
			rrt.alias_asset_id as alias_asset_id,
			rrt.alias_attribute_id as alias_attribute_id
            , coalesce (dy.device_id, dy1.device_id,cte.device_id)  as device_id
            , concat(cte.path, '->', rrt.alias_attribute_id) as path
			from cte_asset_alias cte
			join  v_asset_attribute_alias rrt  on cte.alias_attribute_id = rrt.attribute_id
            left join v_asset_attribute_dynamics dy on cte.alias_attribute_id = dy.attribute_id
            left join v_asset_attribute_dynamics dy1 on rrt.alias_attribute_id = dy1.attribute_id
			)
		select distinct unnest(string_to_array(path, '->'))::uuid as attribute_id
		from cte_asset_alias;
end  $function$
;
