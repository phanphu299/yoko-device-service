
-- '0c506a60-8101-41fd-87df-099a1dc7168c',	N'Functions' 		 --id cannot be changed
-- '85994092-a07f-4afd-a606-ba406eecc4a7',	N'Input Connectors'  --id cannot be changed
-- 'fc0c1827-5973-4785-91fb-244294523b2b',	N'Output Connectors' --id cannot be changed
--  public const string TYPE_BLOCK = "block";
--  public const string TYPE_INPUT_CONNECTOR = "in_connector";
--  public const string TYPE_OUTPUT_CONNECTOR = "out_connector";

insert into function_blocks(id, name, type, category_id, is_active, system)
values
('61097fc0-1135-4fa2-b959-ac3b3c4a60a3', 'Text Input',  'in_connector'    ,'85994092-a07f-4afd-a606-ba406eecc4a7', true, true),
('7dbb78a0-1c6e-4be4-87eb-e693999fada1', 'Integer Input',  'in_connector' ,'85994092-a07f-4afd-a606-ba406eecc4a7', true, true),
('a4930f87-44ff-4cf8-ac69-7cf957c0fc93', 'Double Input',  'in_connector'  ,'85994092-a07f-4afd-a606-ba406eecc4a7', true, true),
('9c035aa7-3d8b-439c-a310-bcc9f98d357d', 'Boolean Input',  'in_connector' ,'85994092-a07f-4afd-a606-ba406eecc4a7', true, true),
('87a92bf6-c485-43f3-bada-f6999986e861', 'DateTime Input',  'in_connector' ,'85994092-a07f-4afd-a606-ba406eecc4a7', true, true),
('1245a0c6-2942-437d-9bf1-f7283ddc0618', 'Asset Attribute Input', 'in_connector','85994092-a07f-4afd-a606-ba406eecc4a7', true, true),
('47120324-0567-4054-81c5-7810a5c5ab30', 'Asset Table Input', 'in_connector','85994092-a07f-4afd-a606-ba406eecc4a7', true, true) on conflict (id) do update set system = EXCLUDED.system;

--
insert into function_block_bindings(id, key, data_type, function_block_id, is_input, system)
values
('1de9d119-e82c-4ef8-9ab4-c4167d63bc6a', 'input-text',  'text', '61097fc0-1135-4fa2-b959-ac3b3c4a60a3', false, true),
('46947b17-73f6-415f-8e06-6898053c99e6', 'input-int', 'int', '7dbb78a0-1c6e-4be4-87eb-e693999fada1', false, true),
('45b42ca6-3959-471d-a589-dc5279a770ae', 'input-double', 'double', 'a4930f87-44ff-4cf8-ac69-7cf957c0fc93', false, true),
('bdafdb89-eefe-4087-a269-cc8604ba55a8', 'input-bool',  'bool', '9c035aa7-3d8b-439c-a310-bcc9f98d357d', false, true),
('1efa8c37-f167-44e4-85ce-6f66ad9e774d', 'input-datetime',  'datetime', '87a92bf6-c485-43f3-bada-f6999986e861', false, true),
('8aef843d-f586-4271-be2c-8b9eb77f4f89', 'input-asset-attribute', 'asset_attribute', '1245a0c6-2942-437d-9bf1-f7283ddc0618', false, true),
('6a572720-a668-4e65-af8c-b1c78fc33e40', 'input-asset-table', 'asset_table', '47120324-0567-4054-81c5-7810a5c5ab30', false, true) on conflict (id) do update set system = EXCLUDED.system;
