create or replace procedure usp_del_time_series_cleanup(job_id int, config jsonb)
language plpgsql
as $$
declare
-- variable declaration

begin
-- stored procedure body
delete from device_metric_series where _ts + (retention_days * interval '1 day') < current_timestamp ;
delete from device_metric_series_text where _ts + (retention_days * interval '1 day') < current_timestamp ;
delete from asset_attribute_runtime_series where  _ts + (retention_days * interval '1 day') < current_timestamp;
delete from asset_attribute_runtime_series_text where  _ts + (retention_days * interval '1 day') < current_timestamp;
end; $$