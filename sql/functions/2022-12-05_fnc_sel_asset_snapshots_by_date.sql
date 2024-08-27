drop function if exists fnc_sel_asset_snapshots_by_date;
create or replace FUNCTION  fnc_sel_asset_snapshots_by_date(startDate timestamp, endDate timestamp, VARIADIC AttributeIds uuid[] )
returns TABLE (
  asset_id uuid,
  attribute_id uuid,
  data_type varchar(255),
  _ts timestamp without time zone,
  value text,
  _lts timestamp without time zone,
  last_good_value text,
  signal_quality_code smallint,
  row_num bigint
) 
LANGUAGE plpgsql
AS $function$ 
BEGIN  return query 
            with sources as (
                select * from (
                            select 
                            am.asset_id as asset_id
                            , am.id as attribute_id
                            , aa.data_type as data_type
                            , dms._ts as _ts
                            , dms.value as value
                            , dms._lts as _lts
                            , dms.last_good_value as last_good_value
                            , dms.signal_quality_code as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by am.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_dynamic_mapping am
                            inner join asset_attribute_templates aa on am.asset_attribute_template_id = aa.id and aa.data_type = 'text'
                            inner join device_metric_series_text dms on am.device_id = dms.device_id and am.metric_key = dms.metric_key
                            where dms._ts >= startDate and dms._ts <= endDate and am.id = ANY(attributeIds)
                        ) s1
                    union
                        select * from (
                            select 
                            am.asset_id as asset_id
                            , am.id as attribute_id
                            , aa.data_type as data_type
                            , dms._ts as _ts
                            , dms.value::text as value
                            , dms._lts as _lts
                            , dms.last_good_value::text as last_good_value
                            , dms.signal_quality_code as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by am.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_dynamic_mapping am
                            inner join asset_attribute_templates aa on am.asset_attribute_template_id = aa.id and aa.data_type != 'text'
                            inner join device_metric_series dms on am.device_id = dms.device_id and am.metric_key = dms.metric_key
                            where dms._ts >= startDate and dms._ts <= endDate and am.id = ANY(attributeIds)
                        ) s2

                    union
                        select * from (
                            select 
                            am.asset_id as asset_id
                            , am.id as attribute_id
                            , aa.data_type as data_type
                            , dms._ts as _ts
                            , dms.value as value
                            , NULL::timestamp without time zone as _lts
                            , NULL::text as last_good_value
                            , 192::smallint as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by am.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_runtime_mapping am
                            inner join asset_attribute_templates aa on am.asset_attribute_template_id = aa.id and aa.data_type = 'text'
                            inner join asset_attribute_runtime_series_text dms on am.asset_id = dms.asset_id and am.id = dms.asset_attribute_id
                            where dms._ts >= startDate and dms._ts <= endDate and am.id = ANY(attributeIds)
                    ) s3
                    union
                        select * from (
                            select 
                            am.asset_id as asset_id
                            , am.id as attribute_id
                            , aa.data_type as data_type
                            , dms._ts as _ts
                            , dms.value::text as value
                            , NULL::timestamp without time zone as _lts
                            , NULL::text as last_good_value
                            ,192::smallint as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by am.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_runtime_mapping am
                            inner join asset_attribute_templates aa on am.asset_attribute_template_id = aa.id and aa.data_type != 'text'
                            inner join asset_attribute_runtime_series dms on am.asset_id = dms.asset_id and am.id = dms.asset_attribute_id
                            where dms._ts >= startDate and dms._ts <= endDate and am.id = ANY(attributeIds)
                    ) s4
                    union
                    -- asset normal attribute
                        select * from (
                            select 
                            aa.asset_id as asset_id
                            , am.asset_attribute_id as attribute_id
                            , aa.data_type::varchar(255) as data_type
                            , dms._ts as _ts
                            , dms.value as value
                            , dms._lts as _lts
                            , dms.last_good_value as last_good_value
                            , dms.signal_quality_code as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by aa.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_dynamic am
                            inner join asset_attributes aa on am.asset_attribute_id = aa.id
                            inner join device_metric_series_text dms on am.device_id = dms.device_id and am.metric_key = dms.metric_key
                            where dms._ts >= startDate and dms._ts <= endDate and am.asset_attribute_id = ANY(attributeIds)
                        ) s5
                        union
                        select * from (
                            select 
                            aa.asset_id as asset_id
                            , am.asset_attribute_id as attribute_id
                            , aa.data_type::varchar(255) as data_type
                            , dms._ts as _ts
                            , dms.value::text as value
                            , dms._lts as _lts
                            , dms.last_good_value::text as last_good_value
                            , dms.signal_quality_code as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by aa.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_dynamic am
                            inner join asset_attributes aa on am.asset_attribute_id = aa.id
                            inner join device_metric_series dms on am.device_id = dms.device_id and am.metric_key = dms.metric_key
                            where dms._ts >= startDate and dms._ts <= endDate and am.asset_attribute_id = ANY(attributeIds)
                        ) s6
                        union 
                         select * from (
                            select 
                            aa.asset_id as asset_id
                            , am.asset_attribute_id as attribute_id
                            , aa.data_type::varchar(255) as data_type
                            , dms._ts as _ts
                            , dms.value::text as value
                            , NULL::timestamp without time zone as _lts
                            , NULL::text as last_good_value
                            , 192::smallint as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by aa.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_runtimes am
                            inner join asset_attributes aa on am.asset_attribute_id = aa.id
                            inner join asset_attribute_runtime_series_text dms on aa.asset_id = dms.asset_id and am.asset_attribute_id = dms.asset_attribute_id
                            where dms._ts >= startDate and dms._ts <= endDate and am.asset_attribute_id = ANY(attributeIds)
                        ) s7
                        union 
                         select * from (
                            select 
                            aa.asset_id as asset_id
                            , am.asset_attribute_id as attribute_id
                            , aa.data_type::varchar(255) as data_type
                            , dms._ts as _ts
                            , dms.value::text as value
                            , NULL::timestamp without time zone as _lts
                            , NULL::text as last_good_value
                            , 192::smallint as signal_quality_code
                            , ROW_NUMBER () over ( PARTITION by aa.asset_id, am.id order by dms._ts desc) as row_num
                            from asset_attribute_runtimes am
                            inner join asset_attributes aa on am.asset_attribute_id = aa.id
                            inner join asset_attribute_runtime_series dms on aa.asset_id = dms.asset_id and am.asset_attribute_id = dms.asset_attribute_id
                            where dms._ts >= startDate and dms._ts <= endDate and am.asset_attribute_id = ANY(attributeIds)
                        ) s8
                    union 
                    -- fallback to snapshot
                        select 
                              msf.asset_id as asset_id
                            , msf.attribute_id as attribute_id
                            , msf.data_type as data_type
                            , msf._ts as ts
                            , msf.value::text as value
                            , msf._lts as _lts
                            , msf.last_good_value::text as last_good_value
                            , msf.signal_quality_code as signal_quality_cod
                            , -1::bigint as row_num
                        FROM v_asset_attribute_snapshots msf
                        WHERE msf._ts >= startDate and msf._ts <= endDate and msf.attribute_id = ANY(attributeIds)
                )
        select s.asset_id, s.attribute_id, s.data_type, s._ts, s.value, s._lts, s.last_good_value, s.signal_quality_code, s.row_num
         from sources s
         where s.row_num = 1 or s.row_num = -1;
END $function$