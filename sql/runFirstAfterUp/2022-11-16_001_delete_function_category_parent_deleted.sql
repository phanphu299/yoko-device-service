delete  from function_block_categories where id in (
select  fbc .id  from  function_block_categories fbc  
where fbc .parent_category_id is not null and (select 1 from function_block_categories fbc2 where fbc.parent_category_id = fbc2.id) is null )