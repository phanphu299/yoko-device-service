-- this function will update device signal quality to good when system receive data from device
drop procedure if exists fnc_udp_update_device_signal_quality;
drop function if exists fnc_udp_update_device_signal_quality;    
create or replace function fnc_udp_update_device_signal_quality(in deviceId varchar(255))
    returns table(device_id varchar)
language plpgsql
as $$
declare
    -- variable declaration
    signal_quality_good smallint = 192;
    check_timestamp timestamp WITHOUT TIME ZONE = current_timestamp;
begin
    update devices 
    set 
        last_heartbeat_timestamp = check_timestamp, 
        signal_quality_code = case when enable_health_check = true then signal_quality_good else null end, 
        status = case when enable_health_check = true then 'AC' else status end
    where id = deviceId;

    return query
        select id as device_id
        from devices where id = deviceId and status = 'AC';
end; $$