
-- '0c506a60-8101-41fd-87df-099a1dc7168c',	N'Functions' 		 --id cannot be changed
-- '85994092-a07f-4afd-a606-ba406eecc4a7',	N'Input Connectors'  --id cannot be changed
-- 'fc0c1827-5973-4785-91fb-244294523b2b',	N'Output Connectors' --id cannot be changed
--  public const string TYPE_BLOCK = "block";
--  public const string TYPE_INPUT_CONNECTOR = "in_connector";
--  public const string TYPE_OUTPUT_CONNECTOR = "out_connector";
insert into function_blocks(id, name, type, category_id, is_active, block_content, system)
values
('91769d9f-7a79-4174-ba3e-c9e8eb8cc2e0', 'Double To Asset Attribute',  'block'    ,'0c506a60-8101-41fd-87df-099a1dc7168c', true, '
var snapshotDateTime = AHI.GetDateTime("system_snapshot_datetime");
// fallback to system trigger datetime in case the snapshot dateime is not found
if(snapshotDateTime == null)
{
  snapshotDateTime = AHI.GetDateTime("system_trigger_datetime");
}
 var val = AHI.GetDouble("double_input");
 AHI.Set("aa_output", snapshotDateTime.Value, val);
', true) on conflict (id) do update set system = EXCLUDED.system;
--
insert into function_block_bindings(id, key, data_type, function_block_id, is_input, system)
values
('922faf77-42a9-436a-8330-970aada34a0b', 'double_input', 'double'      , '91769d9f-7a79-4174-ba3e-c9e8eb8cc2e0', true, true),
('ee76b263-00cb-4508-bb01-f7ed8e2125e2', 'aa_output', 'asset_attribute', '91769d9f-7a79-4174-ba3e-c9e8eb8cc2e0', false, true) on conflict (id) do update set system = EXCLUDED.system;
