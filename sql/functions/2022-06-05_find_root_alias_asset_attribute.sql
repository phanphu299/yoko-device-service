CREATE OR REPLACE FUNCTION find_root_alias_asset_attribute(attributeId uuid)
 RETURNS TABLE(attribute_id uuid, alias_level integer)
 LANGUAGE plpgsql
AS $function$
declare
-- variable declaration
begin
-- stored procedure body
	return query 
	  WITH RECURSIVE alias_cte AS (
			SELECT 
				ea.attribute_id,1 as alias_level
			FROM v_asset_attributes ea
			where  ea.attribute_id = attributeId
			
			UNION ALL
			
			SELECT 
				a.alias_attribute_id   as attribute_id, e.alias_level + 1 as alias_level
			FROM alias_cte e
			inner join (
				select asset_attribute_id, alias_attribute_id from asset_attribute_alias
				union all 
				select id as asset_attribute_id, alias_attribute_id  from asset_attribute_alias_mapping
			) as a on a.asset_attribute_id  = e.attribute_id
		)
		SELECT distinct m.attribute_id, m.alias_level FROM alias_cte m;
	
end  $function$