create or replace view v_function_block_template_overlay
as
select 
exe.id as function_block_execution_id,
coalesce(fbt."name", fb."name") as name
from function_block_executions exe 
left join function_block_templates fbt  on exe.template_id  = fbt.id  and fbt.deleted = false
left join function_blocks fb  on exe.function_block_id  = fb.id and fb.deleted = false