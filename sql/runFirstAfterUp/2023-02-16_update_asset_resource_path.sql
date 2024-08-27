WITH RECURSIVE asset_cte AS (
	SELECT 
		child.id, 
		child.name,
		1 as asset_level,
		concat('objects/', child.id) as asset_path_id
	FROM assets child 
	WHERE child.parent_asset_id is null
	UNION ALL
	SELECT 
		child.id as child_id, 
		child.name AS child_name, 
		acte.asset_level + 1 AS asset_level,
		concat(acte.asset_path_id, '/children/', child.id) as asset_path_id
	FROM asset_cte acte
	INNER JOIN assets child ON child.parent_asset_id  = acte.id
)
update assets  
set resource_path  =  cte.asset_path_id
from asset_cte  cte
where assets.id  = cte.id;