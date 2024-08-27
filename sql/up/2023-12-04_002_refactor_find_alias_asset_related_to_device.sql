-- FUNCTION: public.find_alias_asset_related_to_device(character varying)

-- DROP FUNCTION IF EXISTS public.find_alias_asset_related_to_device(character varying);

CREATE OR REPLACE FUNCTION public.find_alias_asset_related_to_device_refactor(
	deviceid character varying)
    RETURNS TABLE(attribute_id uuid) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
declare
-- variable declaration
begin
-- stored procedure body
    DROP TABLE IF EXISTS alias_tbl;
	DROP TABLE IF EXISTS attribute_dynamics_alias_tbl;
	CREATE TEMP TABLE attribute_dynamics_alias_tbl
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
	   
	CREATE TEMP TABLE alias_tbl AS
	SELECT
	al.asset_id
	, al.attribute_id
	, al.alias_asset_id
	, al.alias_attribute_id
	, coalesce (dy.device_id, dy1.device_id)  as device_id
	, concat(al.attribute_id,'->',  al.alias_attribute_id) as path
	FROM v_asset_attribute_alias al
	LEFT JOIN attribute_dynamics_alias_tbl dy on dy.attribute_id = al.alias_attribute_id
	LEFT JOIN attribute_dynamics_alias_tbl dy1 on dy1.attribute_id = al.attribute_id
	where coalesce(dy.device_id, dy1.device_id) = deviceId;

	return query
			-- alias 
			with RECURSIVE cte_asset_alias AS  (
			select 
			1 as level
			, al.asset_id
            , al.attribute_id
            , al.alias_asset_id
            , al.alias_attribute_id
            , al.device_id
            , concat(al.attribute_id,'->',  al.alias_attribute_id) as path
			from alias_tbl al

			union all 
			select 
            cte.level + 1 as level,
			cte.alias_asset_id as asset_id ,
			cte.alias_attribute_id  as attribute_id,
			rrt.alias_asset_id as alias_asset_id,
			rrt.alias_attribute_id as alias_attribute_id
            , coalesce (rrt.device_id,cte.device_id)  as device_id
            , concat(cte.path, '->', rrt.alias_attribute_id) as path
			from cte_asset_alias cte
			join  alias_tbl rrt  on cte.alias_attribute_id = rrt.attribute_id
			)
		select s.attribute_id_string::uuid as attribute_id from (select distinct unnest(string_to_array(path,'->')) attribute_id_string from  cte_asset_alias cte where  device_id = deviceId) s
			
			;
end  
$BODY$;

