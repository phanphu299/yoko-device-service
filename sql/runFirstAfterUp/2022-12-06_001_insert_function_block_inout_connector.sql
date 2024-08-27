-- update old data set binding type from is_input
update function_block_bindings set binding_type = 'input' where is_input = true;
update function_block_bindings set binding_type = 'output' where is_input = false;
