CREATE OR REPLACE FUNCTION find_root_block_category(categoryId uuid)
 RETURNS TABLE(id uuid, name character varying, parent_category_id uuid, parent_name character varying, category_level integer, category_path_id text, category_path_name text)
 LANGUAGE plpgsql
AS $function$
DECLARE
BEGIN
	RETURN query
	WITH RECURSIVE category_cte AS (
		SELECT 
			a.id, 
            a.name,
            a.parent_category_id, 
            pa.name AS parent_name, 
            1 as category_level,
            concat(a.parent_category_id, coalesce(CASE WHEN a.parent_category_id IS NOT NULL THEN '/' ELSE '' END), a.id) as category_path_id,
            concat(pa.name, coalesce(CASE WHEN pa.name IS NOT NULL THEN '/' ELSE '' END), a.name) as category_path_name
		FROM function_block_categories a
        LEFT JOIN function_block_categories pa ON pa.id = a.parent_category_id
		WHERE a.id = categoryId
		UNION ALL
		SELECT 
			a.id,
            a.name, 
            a.parent_category_id AS parent_category_id,
            pa.name AS parent_name,
            acte.category_level + 1 AS category_level,
            concat(a.parent_category_id, coalesce(CASE WHEN a.parent_category_id IS NOT NULL THEN '/' ELSE '' END), acte.category_path_id) as category_path_id,
            concat(pa.name, coalesce(CASE WHEN pa.name IS NOT NULL THEN '/' ELSE '' END), acte.category_path_name) as category_path_name
		FROM category_cte acte
		INNER JOIN function_block_categories a ON a.id = acte.parent_category_id
        LEFT JOIN function_block_categories pa ON pa.id = a.parent_category_id
	)
    SELECT category_cte.id, category_cte.name, category_cte.parent_category_id, category_cte.parent_name, category_cte.category_level, category_cte.category_path_id, category_cte.category_path_name FROM category_cte
    ORDER BY category_cte.category_level DESC;
END $function$
;

