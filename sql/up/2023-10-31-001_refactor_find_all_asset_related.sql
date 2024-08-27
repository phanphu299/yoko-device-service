CREATE OR REPLACE FUNCTION public.find_all_asset_related_to_device_refactor(deviceid character varying)
 RETURNS TABLE(asset_id uuid)
 LANGUAGE plpgsql
AS $function$
declare
-- variable declaration
begin
-- stored procedure body
	return query 
			select cte.asset_id from find_all_asset_trigger_by_device_id(deviceId) cte
			union 
			select dy.asset_id from v_asset_attribute_dynamics dy where dy.device_id  = deviceId 
			union 
			select distinct aa.asset_id from find_alias_asset_related_to_device_refactor(deviceId) s
			inner join v_asset_attributes aa on s.attribute_id = aa.attribute_id
			;
end  $function$
;
