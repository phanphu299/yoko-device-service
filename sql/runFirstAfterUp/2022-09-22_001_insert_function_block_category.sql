insert into function_block_categories(id, name, parent_category_id, system) values('0c506a60-8101-41fd-87df-099a1dc7168c',N'Functions', null, true) on conflict (id) 
    do update set system = true; --id cannot be changed
insert into function_block_categories(id, name, parent_category_id, system) values('85994092-a07f-4afd-a606-ba406eecc4a7',N'Input Connectors', null, true) on conflict (id) 
    do update set system = true; --id cannot be changed
insert into function_block_categories(id, name, parent_category_id, system) values('fc0c1827-5973-4785-91fb-244294523b2b',N'Output Connectors', null, true) on conflict (id) 
    do update set system = true; --id cannot be changed