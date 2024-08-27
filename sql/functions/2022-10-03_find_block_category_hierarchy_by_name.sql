DROP FUNCTION IF EXISTS find_block_category_hierarchy_by_name(searchName varchar(255));
CREATE OR REPLACE FUNCTION find_block_category_hierarchy_by_name(searchName varchar(255))
  RETURNS TABLE(entity_id uuid, entity_name varchar, entity_is_category boolean, entity_type varchar, hierarchy_entity_id uuid, hierarchy_entity_name varchar, hierarchy_entity_is_category boolean, hierarchy_entity_type varchar, hierarchy_entity_parent_category_id uuid)
  LANGUAGE plpgsql
AS $function$
DECLARE
BEGIN
    RETURN query
            WITH RECURSIVE cte AS (
                    SELECT 
                        fbc1.id as entity_id,
                        fbc1.name as entity_name,
                        true as entity_is_category,
                        'category' as entity_type,
                        fbc1.id as hierarchy_entity_id,
                        fbc1.name as hierarchy_entity_name,
                        true as hierarchy_entity_is_category,
                        'category' as hierarchy_entity_type,
                        fbc1.parent_category_id as hierarchy_entity_parent_category_id,
						1 as entity_level
                        FROM function_block_categories fbc1
                    WHERE fbc1.deleted is FALSE and fbc1.name ilike concat('%', searchName , '%') 
                    and (fbc1.parent_category_id is null or (select 1 from function_block_categories where id = fbc1.parent_category_id) is not null)
                UNION ALL
					SELECT 
                        fb1.id as entity_id,
                        fb1.name as entity_name,
                        false as entity_is_category,
                        fb1.type as entity_type,
                        fb1.id as hierarchy_entity_id,
                        fb1.name as hierarchy_entity_name,
                        false as hierarchy_entity_is_category,
                        fb1.type as hierarchy_entity_type,
                        fb1.category_id as hierarchy_entity_parent_category_id,
                        1 as entity_level
                        FROM function_blocks fb1
                    WHERE fb1.deleted is FALSE and fb1.name ilike concat('%', searchName , '%') and (select 1 from function_block_categories where id = fb1.category_id) is not null
                UNION ALL
                    SELECT 
                        c.entity_id,
                        c.entity_name,
                        c.entity_is_category,
                        c.entity_type,
                        fb2.id as hierarchy_entity_id,
                        fb2.name as hierarchy_entity_name,
                        true as hierarchy_entity_is_category,
						'category' as hierarchy_entity_type,							
                        fb2.parent_category_id as hierarchy_entity_parent_category_id,
						entity_level + 1
                        FROM function_block_categories fb2
                            JOIN cte c ON c.hierarchy_entity_parent_category_id = fb2.id
                        WHERE fb2.parent_category_id is null OR (select 1 from function_block_categories where id = fb2.parent_category_id) is not null
                )
    SELECT  
        cte.entity_id,
        cte.entity_name,
		cte.entity_is_category,
        cte.entity_type,
        cte.hierarchy_entity_id,
        cte.hierarchy_entity_name,
		cte.hierarchy_entity_is_category,
        cte.hierarchy_entity_type,
        cte.hierarchy_entity_parent_category_id
    FROM cte
    ORDER BY cte.entity_level desc;
end  $function$