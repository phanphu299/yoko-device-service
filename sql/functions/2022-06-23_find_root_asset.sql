CREATE OR REPLACE FUNCTION find_root_asset(assetId uuid)
	RETURNS TABLE(id UUID, name VARCHAR, parent_asset_id UUID, parent_asset_name VARCHAR, asset_level INT, asset_path_id TEXT, asset_path_name TEXT)
 	LANGUAGE plpgsql
AS $FUNCTION$
DECLARE
BEGIN
	RETURN query
	WITH RECURSIVE asset_cte AS (
		SELECT 
			a.id, 
            a.name,
            a.parent_asset_id, 
            pa.name AS parent_asset_name, 
            1 as asset_level,
            concat(a.parent_asset_id, coalesce(CASE WHEN a.parent_asset_id IS NOT NULL THEN '/' ELSE '' END), a.id) as asset_path_id,
            concat(pa.name, coalesce(CASE WHEN pa.name IS NOT NULL THEN '/' ELSE '' END), a.name) as asset_path_name
		FROM assets a
        LEFT JOIN assets pa ON pa.id = a.parent_asset_id
		WHERE a.id = assetId
		UNION ALL
		SELECT 
			a.id,
            a.name, 
            a.parent_asset_id AS parent_asset_id,
            pa.name AS parent_asset_name,
            acte.asset_level + 1 AS asset_level,
            concat(a.parent_asset_id, coalesce(CASE WHEN a.parent_asset_id IS NOT NULL THEN '/' ELSE '' END), acte.asset_path_id) as asset_path_id,
            concat(pa.name, coalesce(CASE WHEN pa.name IS NOT NULL THEN '/' ELSE '' END), acte.asset_path_name) as asset_path_name
		FROM asset_cte acte
		INNER JOIN assets a ON a.id = acte.parent_asset_id
        LEFT JOIN assets pa ON pa.id = a.parent_asset_id
	)
    SELECT asset_cte.id, asset_cte.name, asset_cte.parent_asset_id, asset_cte.parent_asset_name, asset_cte.asset_level, asset_cte.asset_path_id, asset_cte.asset_path_name FROM asset_cte
    ORDER BY asset_cte.asset_level DESC;
END $FUNCTION$;