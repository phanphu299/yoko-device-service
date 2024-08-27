CREATE OR REPLACE FUNCTION fn_GetFullAssetPath(asset_id_input uuid)
  RETURNS TABLE(AssetId uuid, AssetName varchar, TreeLvl1Id uuid, HierarchyName varchar, HierarchyId varchar)
  LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS
$func$
with RECURSIVE  cte as (
 	select a.id as asset_id, a.parent_asset_id, a.name as asset_name, 1 as order
	from assets a 
	where a.id = asset_id_input
    UNION ALL
    SELECT  ca.id as asset_id, ca.parent_asset_id , ca.name  as asset_name, (t.order + 1) as order
   	from assets ca
    INNER JOIN cte t ON  ca.id = t.parent_asset_id
), cte2 as (
 	select * from cte c order by c.order desc
)
select 	(select asset_id from cte2 order by cte2.order limit 1) as AssetId,
		(select asset_name from cte2 order by cte2.order limit 1) as AssetName, 
		(select asset_id from cte2 order by cte2.order desc limit 1) TreeLvl1Id, 
		string_agg(c2.asset_name, '/')  as  HierarchyName,
		string_agg(c2.asset_id::text, '/') as HierarchyId
from cte2 c2
$func$;

