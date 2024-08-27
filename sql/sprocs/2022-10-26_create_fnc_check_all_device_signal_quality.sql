    -- this function will check all devices signal quality in the database
    -- it will cause performance issue if the device are huge, please be aware of this
    drop procedure if exists fnc_udp_device_signal_quality;
    drop procedure if exists fnc_health_check_all_device_signal_quality;
    drop function if exists fnc_health_check_all_device_signal_quality;
    create or replace function fnc_health_check_all_device_signal_quality()
    RETURNS TABLE( deviceId varchar)
    language plpgsql
    as $$
    declare
    -- variable declaration
        signal_quality_bad smallint = 0;
        affected_row_count int ;
        check_timestamp timestamp WITHOUT TIME ZONE = current_timestamp;
    begin
        --reset has_data_updated
        update devices d set has_data_updated = false where d.has_data_updated = true;
        -- update the device signal
        update devices d
        set 
         has_data_updated = true
        , signal_quality_code = signal_quality_bad
        , status = 'IN'
        from v_device_snapshot ds
        where ds.device_id = d.id and d.enable_health_check = true and (d.status = 'AC' or d.signal_quality_code is null)  -- only check the healthy device
        and  (COALESCE(ds._ts, '1970-01-01'::timestamp) + coalesce (d.healthz_interval,60) * INTERVAL '1' second) < check_timestamp;

        GET DIAGNOSTICS affected_row_count = ROW_COUNT;
        if(affected_row_count > 0) 
        then
            -- the device signal quality has changed.
            -- update the timeseries data
            insert into device_metric_series(device_id, metric_key, value, _ts, retention_days, signal_quality_code, last_good_value, _lts)
            select dm.device_id
            , dm.metric_key
            , null -- value should be null
            , check_timestamp
            , d.retention_days
            , signal_quality_bad
            , case when dm.data_type = 'bool' then cast(dm.value::boolean::integer as double precision) else cast(dm.value as double precision) end
            , dm."_ts" 
            from v_device_metrics dm
            join devices d on dm.device_id = d.id
            where dm.data_type in ('double','int','bool')
            and d.has_data_updated = true;

            -- update the timeseries text data
            insert into device_metric_series_text(device_id, metric_key, value, _ts, retention_days, signal_quality_code, last_good_value, _lts)
            select 
                d.id
                , dm.metric_key
                , null -- value should be null
                , check_timestamp
                , d.retention_days
                , signal_quality_bad
                , dm.value 
                , dm."_ts" 
            from v_device_metrics dm
            join devices d on dm.device_id = d.id
            where dm.data_type in ('text')
            and d.has_data_updated = true;

            -- need to update the snapshot as well as the timeseries.
            update device_metric_snapshots
            set 
            _ts = check_timestamp
            , value = null
            , last_good_value = device_metric_snapshots.value 
            , "_lts"  = device_metric_snapshots."_ts" 
            from devices d
            where device_metric_snapshots.device_id = d.id 
            and d.has_data_updated = true;

            return query
       		    select d.id as deviceId from devices d where d.has_data_updated = true;
        end if;
    end; $$
-- trigger for update
