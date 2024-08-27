update function_block_templates set design_content = replace(design_content, '"isInput":false','"bindingType":"output"') where true;
update function_block_templates set design_content = replace(design_content, '"isInput":true','"bindingType":"input"') where true ;
update function_block_executions set diagram_content = replace(diagram_content, '"isInput":false','"bindingType":"output"') where true ;
update function_block_executions set diagram_content = replace(diagram_content, '"isInput":true','"bindingType":"input"') where true ;
