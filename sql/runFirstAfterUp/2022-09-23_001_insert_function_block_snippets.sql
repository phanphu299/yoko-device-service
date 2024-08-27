delete from function_block_snippets;

insert into function_block_snippets(name, template_code) values(N'System Running Datetime', N'// var runningDateTime = AHI.GetDateTime("system_trigger_datetime").Value;');
insert into function_block_snippets(name, template_code) values(N'System Snapshot Datetime', N'// var snapshotDateTime = AHI.GetDateTime("system_snapshot_datetime").Value;');
insert into function_block_snippets(name, template_code) values(N'Set primitive value', N'// AHI.Set("output_binding", value_here);');
insert into function_block_snippets(name, template_code) values(N'Set aggregated value', N'// AHI.Set("output_binding", datetime_here, value_here);');
insert into function_block_snippets(name, template_code) values(N'Set array of aggregated value', N'// AHI.Set("output_binding", 
//      (datetime_here, value_here),
//      (datetime_here, value_here),
//      (datetime_here, value_here)
// );');

insert into function_block_snippets(name, template_code) values(N'Get Double Value', N'// var v = AHI.GetDouble("input_binding_double");');
insert into function_block_snippets(name, template_code) values(N'Get Int Value', N'// var v = AHI.GetInt("input_binding_int");');
insert into function_block_snippets(name, template_code) values(N'Get Boolean Value', N'// var v = AHI.GetBoolean("input_binding_boolean");');
insert into function_block_snippets(name, template_code) values(N'Get Text Value', N'// var v = AHI.GetString("input_binding_text");');
insert into function_block_snippets(name, template_code) values(N'Get Static Value', N'// var v = AHI.GetAttribute("input_binding_asset_attribute").Value(); // Get current asset attribute static value (0/1 in case boolean)');
insert into function_block_snippets(name, template_code) values(N'Get Static Value String', N'// var v = AHI.GetAttribute("input_binding_asset_attribute").ValueString(); // Get current asset attribute static text value');
insert into function_block_snippets(name, template_code) values(N'LastValueAsync', N'// var (d, v) = await AHI.GetAttribute("input_binding_asset_attribute").LastValueAsync(); // Get current asset attribute snapshot value (0/1 in case boolean)');
insert into function_block_snippets(name, template_code) values(N'LastValueStringAsync', N'// var (d, v) = await AHI.GetAttribute("input_binding_asset_attribute").LastValueStringAsync(); // Get current asset attribute snapshot text value');

insert into function_block_snippets(name, template_code) values(N'SearchNearestAsync', N'// var filterDateTime = "1692858454000".UnixTimeStampToDateTime();
// var (leftPointDateTime, leftPointValue) = await AHI.GetAttribute("input_binding_asset_attribute").SearchNearestAsync(filterDateTime, "left"); // Position can be "left" or "right"');

insert into function_block_snippets(name, template_code) values(N'SearchNearestStringAsync', N'// var filterDateTime = "1692858454000".UnixTimeStampToDateTime();
// var (leftPointDateTime, leftPointValue) = await AHI.GetAttribute("input_binding_asset_attribute").SearchNearestStringAsync(filterDateTime, "left"); // Position can be "left" or "right"');

insert into function_block_snippets(name, template_code) values(N'LastTimeDiffAsync', N'// var lastTimeDiffDuration = await AHI.GetAttribute("input_binding_asset_attribute").LastTimeDiffAsync("sec"); // sec, min, hour, day (default: sec)');
insert into function_block_snippets(name, template_code) values(N'LastValueDiffAsync', N'// var lastValueDiff = await AHI.GetAttribute("input_binding_asset_attribute").LastValueDiffAsync("sec"); // sec, min, hour, day (default: sec)');

insert into function_block_snippets(name, template_code) values(N'TimeDiffAsync', N'// var start = "1690180054000".UnixTimeStampToDateTime();
// var end = "1692858454000".UnixTimeStampToDateTime();
// var lastTimeDiffDuration = await AHI.GetAttribute("input_binding_asset_attribute").TimeDiffAsync(start, end, "sec"); // sec, min, hour, day (default: sec)');

insert into function_block_snippets(name, template_code) values(N'ValueDiffAsync', N'// var start = "1690180054000".UnixTimeStampToDateTime();
// var end = "1692858454000".UnixTimeStampToDateTime();
// var lastValueDiffDuration = await AHI.GetAttribute("input_binding_asset_attribute").ValueDiffAsync(start, end);');

insert into function_block_snippets(name, template_code) values(N'AggregateAsync', N'// var start = "1690180054000".UnixTimeStampToDateTime();
// var end = "1692858454000".UnixTimeStampToDateTime();
// var sum = await AHI.GetAttribute("input_binding_asset_attribute").AggregateAsync("sum", start, end, ">", 100); // Sum all values which > 100 in range start/end');

insert into function_block_snippets(name, template_code) values(N'DurationInAsync', N'// var start = "1690180054000".UnixTimeStampToDateTime();
// var end = "1692858454000".UnixTimeStampToDateTime();
// var duration = await AHI.GetAttribute("input_binding_asset_attribute").DurationInAsync(start, end, ">", 100, "sec"); // Calculate duration of state by condition value > 100 in range start/end');

insert into function_block_snippets(name, template_code) values(N'CountInAsync', N'// var start = "1690180054000".UnixTimeStampToDateTime();
// var end = "1692858454000".UnixTimeStampToDateTime();
// var count = await AHI.GetAttribute("input_binding_asset_attribute").CountInAsync(start, end, "=", "running"); // Time count value = running in range start/end');

insert into function_block_snippets(name, template_code) values(N'Terminate the flow', N'// AHI.Set("system_exit_code", true); // The code will terminate (exit) at this line');

insert into function_block_snippets(name, template_code) values(N'Query from Asset Table', N'
// Follow document for more information (Document > Orientation > Function Block Tutorial > Support Functions > Query from Asset Table)
// var query = new
// {
//     PageIndex = 0,
//     PageSize = int.MaxValue,
//     Sorts = "column1=asc,column2=desc",
//     Filter = new
//     {
//         QueryKey = "query_key",
//         QueryType = "query_type",
//         Operation = "operation",
//         QueryValue = "query_value"
//     }
// };
// var outputs = await AHI.GetTable("input_binding_asset_table").QueryAsync(query);');

insert into function_block_snippets(name, template_code) values(N'Aggregation from Asset Table', N'// var v = await AHI.GetTable("input_binding_asset_table").Column("column_name").AggregateAsync(aggregate_here, filter_column_name_here, filter_operator_here, filter_value_here);');

insert into function_block_snippets(name, template_code) values(N'Insert into Asset Table', N'// await AHI.GetTable("input_binding_asset_table").WriteAsync(data_here);');

insert into function_block_snippets(name, template_code) values(N'Delete from Asset Table', N'// await AHI.GetTable("input_binding_asset_table").DeleteAsync(ids_here);');

insert into function_block_snippets(name, template_code) values(N'HTTP Get Request', N'
// var response = await AHI.WithHttp("http://sample_url")
//                                .AddHeader(key_here, value_here)
//                                .AddHeader(key_here, value_here)
//                                .GetAsync<Newtonsoft.Json.Linq.JObject>();');

insert into function_block_snippets(name, template_code) values(N'HTTP Post Request', N'
// var request = new
// {
//     Payload = new
//     {
//         Value = lastValue
//     }
// };
// var response = await AHI.WithHttp("http://sample_url")
//                                .AddHeader(key_here, value_here)
//                                .AddHeader(key_here, value_here)
//                                .PostAsync<Newtonsoft.Json.Linq.JObject>(request);');