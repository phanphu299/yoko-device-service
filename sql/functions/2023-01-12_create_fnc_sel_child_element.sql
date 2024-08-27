create or replace function fnc_sel_child_element (assetId uuid)
returns TABLE (
	id uuid,
	child_level int
) 
language plpgsql
as $$
begin
		return query 
		with recursive cte_element AS (
			select e.id , 0 as child_level
			from assets e
			where e.id = assetId
			
			union
			select e.id, cte.child_level + 1 as child_level
			from assets e
			inner join cte_element cte on cte.id = e.parent_asset_id  
		)
		
		SELECT
		 e.id
		 , e.child_level
		from
		cte_element e;
end;$$