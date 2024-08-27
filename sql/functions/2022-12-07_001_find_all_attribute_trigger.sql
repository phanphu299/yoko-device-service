CREATE OR REPLACE FUNCTION public.find_all_attribute_trigger_relation(attributeIds uuid[])
 RETURNS TABLE(asset_id uuid, attribute_id uuid, trigger_asset_id uuid, trigger_attribute_id uuid)
 LANGUAGE plpgsql
AS $function$
declare
-- variable declaration
begin
-- stored procedure body
	return query 
	  with RECURSIVE cte_assets AS  (
			select distinct
				rrt.asset_id as AssetId ,
				rrt.attribute_id  as AttributeId,
				rrt.trigger_asset_id as TriggerAssetId,
				rrt.trigger_attribute_id as TriggerAttributeId
			from v_asset_attribute_runtimes rrt
			where rrt.trigger_attribute_id = ANY(attributeIds) 
			
			union 
			
			select distinct
				rrt.asset_id as AssetId ,
				rrt.attribute_id  as AttributeId,
				rrt.trigger_asset_id as TriggerAssetId,
				rrt.trigger_attribute_id as TriggerAttributeId
			from v_asset_attribute_runtimes rrt
			join cte_assets cte on cte.AssetId = rrt.trigger_asset_id and cte.AttributeId = rrt.trigger_attribute_id
			)
			select cte.AssetId, cte.AttributeId, cte.TriggerAssetId, cte.TriggerAttributeId from cte_assets cte;
	
end  $function$
;