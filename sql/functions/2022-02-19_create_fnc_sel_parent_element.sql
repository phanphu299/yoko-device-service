create or replace FUNCTION  fnc_sel_parent_element(assetId uuid)
returns TABLE (
id uuid,
parent_id uuid,
parent_level int
) 
as
$func$
    with RECURSIVE cte_element AS (
    select e.id , e.parent_asset_id as parent_id, 1 as parent_level
    from assets e
    where e.id = assetId
    
    union
    select e.id, e.parent_asset_id as parent_id, cte.parent_level + 1 as parent_level
    from assets e
    inner join cte_element cte on cte.parent_id = e.id 
	)
	
	SELECT
	 e.id
	 , e.parent_id
	 , e.parent_level
	from
	cte_element e;
$func$ 
LANGUAGE sql;
