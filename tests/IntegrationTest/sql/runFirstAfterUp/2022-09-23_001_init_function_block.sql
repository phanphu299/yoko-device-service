
-- '0c506a60-8101-41fd-87df-099a1dc7168c',	N'Functions' 		 --id cannot be changed
-- '85994092-a07f-4afd-a606-ba406eecc4a7',	N'Input Connectors'  --id cannot be changed
-- 'fc0c1827-5973-4785-91fb-244294523b2b',	N'Output Connectors' --id cannot be changed
--  public const string TYPE_BLOCK = "block";
--  public const string TYPE_INPUT_CONNECTOR = "in_connector";
--  public const string TYPE_OUTPUT_CONNECTOR = "out_connector";
insert into function_blocks(id, name, block_content, type, category_id, system)
values
('92ca8c73-6dbe-416f-88e6-955de297573d', 'FB Aggreration Function', '
 var (d,v) = await AHI.GetAttribute("aa_in_1").LastValueAsync(); // get current asset attribute snapshot value
 var filterDateTime = "2022-09-10 13:28:19".UnixTimeStampToDateTime();
 var (leftPointDateTime, leftPointValue) = await AHI.GetAttribute("aa_in_1").SearchNearestAsync(filterDateTime, "left"); // position can be "left" or "right"
 var lastTimeDiffDuration = await AHI.GetAttribute("aa_in_1").LastTimeDiffAsync("sec"); // sec, min, hour, day (default: sec)
 var lastValueDiff = await AHI.GetAttribute("aa_in_1").LastValueDiffAsync();
 var date1 = "2022-08-25 12:40:50".UnixTimeStampToDateTime();
 var date2 = "2022-08-26 12:41:20".UnixTimeStampToDateTime();
 var lastValueDiffDuration = await AHI.GetAttribute("aa_in_1").TimeDiffAsync(date1, date2, "sec"); // sec, min, hour, day (default: sec)
 var lastValueDiffDuration1 = await AHI.GetAttribute("aa_in_1").ValueDiffAsync(date1, date2);
 var sum = await AHI.GetAttribute("aa_in_1").AggregateAsync("sum", date1, date2); // sum all values in date1 < date < date2
 var sumValue = await AHI.GetAttribute("aa_in_1").AggregateAsync("sum", date1, date2, ">", 100); // sum all values which > 100 in date1 < date < date2
 var duration2 = await AHI.GetAttribute("aa_in_1").DurationInAsync(date1, date2, ">", 100); // calculate duration of state by condition value > 100 in date1< date < date2
 var countIn = await AHI.GetAttribute("aa_in_1").CountInAsync(date1, date2, ">", 10); // time count of state by condition value > 100 in date1< date < date2
 AHI.Set("snapshot_value", lastValueDiff);
 AHI.Set("last_time_diff", lastTimeDiffDuration);
 AHI.Set("nearest_value", leftPointValue);
 AHI.Set("last_value_diff", lastValueDiffDuration);
 AHI.Set("sum_aggregration", sum);
 AHI.Set("duration_in", duration2);
 AHI.Set("count_in", countIn);
', 'block', '0c506a60-8101-41fd-87df-099a1dc7168c', true);

-- add the block bindings.
insert into function_block_bindings(id, key, data_type, function_block_id, is_input, system)
values
('29db6881-8b9b-4966-aa54-1ee5e7863fed','aa_in_1',  'asset_attribute',                                 '92ca8c73-6dbe-416f-88e6-955de297573d', true, true);

insert into function_block_bindings(id, key, data_type, function_block_id, is_input, system)
values
('fba36456-9fb9-448e-90a5-b6ce1168da0e','snapshot_value',  'double',                         '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('1654c4a2-3c85-4b54-bf5c-10710c21d810','last_time_diff',  'double',                         '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('be204d0b-94cc-4949-b387-b1c213ed5712','nearest_value',  'double',                          '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('bdd2ec91-7576-45ce-92fc-57663079549d','last_value_diff',  'double',                        '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('307242fa-5743-4eab-b44b-ae5ee0562161','last_value_diff',  'double',                        '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('76412f85-24b4-4451-b67b-4dace2b7aba5','sum_aggregration',  'double',                       '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('c7627116-d690-4dd1-931c-231e63595415','duration_in',  'double',                            '92ca8c73-6dbe-416f-88e6-955de297573d', false, true),
('7597f65a-35a4-46c7-8942-af7a58c9b185','count_in',  'int',                               '92ca8c73-6dbe-416f-88e6-955de297573d', false, true);
