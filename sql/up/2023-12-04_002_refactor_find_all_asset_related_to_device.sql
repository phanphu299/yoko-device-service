-- FUNCTION: public.find_all_asset_related_to_device(character varying)

-- DROP FUNCTION IF EXISTS public.find_all_asset_related_to_device(character varying);

CREATE OR REPLACE FUNCTION public.find_all_asset_related_to_device_refactor(
	deviceid character varying)
    RETURNS TABLE(asset_id uuid) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
declare
-- variable declaration
begin
   DROP TABLE IF EXISTS attribute_dynamics_related_to_device_tbl;
	
	CREATE TEMP TABLE attribute_dynamics_related_to_device_tbl
	AS
	 SELECT aadm.device_id,
		aadm.metric_key,
		aat.data_type,
		aadm.asset_id,
		aadm.id AS attribute_id
	   FROM asset_attribute_dynamic_mapping aadm
		 JOIN asset_attribute_templates aat ON aadm.asset_attribute_template_id = aat.id
	   WHERE aadm.device_id=deviceid
	UNION ALL
	 SELECT aad.device_id,
		aad.metric_key,
		aa.data_type,
		aa.asset_id,
		aa.id AS attribute_id
	   FROM asset_attribute_dynamic aad
		 JOIN asset_attributes aa ON aad.asset_attribute_id = aa.id
	   WHERE aad.device_id=deviceid;
	   
-- stored procedure body
	return query 
			select cte.asset_id from find_all_asset_trigger_by_device_id_refactor(deviceId) cte
			union 
			select dy.asset_id from attribute_dynamics_related_to_device_tbl dy
			union 
			select distinct aa.asset_id from find_alias_asset_related_to_device_refactor(deviceId) s
			inner join v_asset_attributes aa on s.attribute_id = aa.attribute_id
			;
end  
$BODY$;

