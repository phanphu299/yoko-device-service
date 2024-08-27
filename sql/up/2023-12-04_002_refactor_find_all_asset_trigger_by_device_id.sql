-- FUNCTION: public.find_all_asset_trigger_by_device_id(character varying)

-- DROP FUNCTION IF EXISTS public.find_all_asset_trigger_by_device_id(character varying);

CREATE OR REPLACE FUNCTION public.find_all_asset_trigger_by_device_id_refactor(
	deviceid character varying)
    RETURNS TABLE(asset_id uuid, attribute_id uuid, trigger_asset_id uuid, trigger_attribute_id uuid, trigger_level integer, metric_key character varying) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
declare
-- variable declaration
begin
-- stored procedure body
	DROP TABLE IF EXISTS attribute_runtimes_tbl;
	DROP TABLE IF EXISTS attribute_dynamics_tbl;
	
	CREATE TEMP TABLE attribute_dynamics_tbl
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
	 
	CREATE TEMP TABLE attribute_runtimes_tbl
	 AS
	 SELECT aarm.asset_id,
		aarm.id AS attribute_id,
		aarm.expression_compile,
		aat.data_type,
		aatr.trigger_asset_id,
		aatr.trigger_attribute_id,
		aarm.enabled_expression
	   FROM asset_attribute_runtime_mapping aarm
	     JOIN attribute_dynamics_tbl adt ON adt.asset_id = aarm.asset_id
		 JOIN asset_attribute_templates aat ON aarm.asset_attribute_template_id = aat.id
		 LEFT JOIN asset_attribute_runtime_triggers aatr ON aarm.id = aatr.attribute_id AND aatr.is_selected = true
	UNION ALL
	 SELECT aa.asset_id,
		aa.id AS attribute_id,
		aar.expression_compile,
		aa.data_type,
		aatr.trigger_asset_id,
		aatr.trigger_attribute_id,
		aar.enabled_expression
	   FROM asset_attributes aa
	     JOIN attribute_dynamics_tbl adt ON adt.asset_id = aa.asset_id
		 JOIN asset_attribute_runtimes aar ON aa.id = aar.asset_attribute_id
		 LEFT JOIN asset_attribute_runtime_triggers aatr ON aar.asset_attribute_id = aatr.attribute_id AND aatr.is_selected = true
		 ;
	
	return query 
	
	  with RECURSIVE cte_assets AS  (
			select 
			1 as trigger_level,
			rt.asset_id as asset_id ,
			rt.attribute_id  as attribute_id,
			rt.trigger_asset_id as trigger_asset_id,
			rt.trigger_attribute_id as trigger_attribute_id,
			dy.metric_key as metric_key
			from attribute_dynamics_tbl dy
			inner join attribute_runtimes_tbl rt on dy.asset_id  = rt.trigger_asset_id  and dy.attribute_id  = rt.trigger_attribute_id
			where dy.device_id  = deviceId

			union all 
			select cte.trigger_level + 1 as trigger_level,
			rrt.asset_id as asset_id ,
			rrt.attribute_id  as attribute_id,
			rrt.trigger_asset_id as trigger_asset_id,
			rrt.trigger_attribute_id as trigger_attribute_id,
			cte.metric_key as metric_key
			from attribute_runtimes_tbl rrt 
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
			from find_alias_asset_related_to_device_refactor(deviceId) s
			inner join attribute_runtimes_tbl rt on s.attribute_id  = rt.trigger_attribute_id
			;
	
end  
$BODY$;

