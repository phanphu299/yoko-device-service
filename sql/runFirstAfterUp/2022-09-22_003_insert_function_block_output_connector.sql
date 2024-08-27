
-- '0c506a60-8101-41fd-87df-099a1dc7168c',	N'Functions' 		 --id cannot be changed
-- '85994092-a07f-4afd-a606-ba406eecc4a7',	N'Input Connectors'  --id cannot be changed
-- 'fc0c1827-5973-4785-91fb-244294523b2b',	N'Output Connectors' --id cannot be changed
--  public const string TYPE_BLOCK = "block";
--  public const string TYPE_INPUT_CONNECTOR = "in_connector";
--  public const string TYPE_OUTPUT_CONNECTOR = "out_connector";
insert into function_blocks(id, name, type, category_id, is_active, system)
values
('8833941a-81b6-40cf-b5a3-855559471f2c', 'Asset Attribute Output', 'out_connector', 'fc0c1827-5973-4785-91fb-244294523b2b', true, true),
('c495a22e-b057-4ad6-899e-94f6cbe8b40d', 'Asset Table Output', 'out_connector', 'fc0c1827-5973-4785-91fb-244294523b2b', true, true) on conflict (id) do update set system = EXCLUDED.system;

-- function_block_bindings
insert into function_block_bindings(id, key, data_type, function_block_id, is_input, system)
values
('034263a2-019b-4a5a-81d9-1d186c0aa6af', 'output-asset-attribute',  'asset_attribute', '8833941a-81b6-40cf-b5a3-855559471f2c', true, true),
('ef9cfb6c-574b-4fcf-b1c8-ddb2f4e09b59', 'output-asset-table',  'asset_table', 'c495a22e-b057-4ad6-899e-94f6cbe8b40d', true, true) on conflict (id) do update set system = EXCLUDED.system;
