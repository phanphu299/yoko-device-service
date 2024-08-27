-- drop function if exists fnc_sel_asset_snapshots;
-- create or replace FUNCTION  fnc_sel_asset_snapshots(snapshotDateTime timestamp, VARIADIC assetIds uuid[] )
-- returns TABLE (
--   asset_id uuid,
--   asset_name varchar(255),
--   attribute_id uuid,
--   attribute_name varchar(255),
--   data_type varchar(255),
--   value varchar(1024),
--  -- unix_timestamp bigint,
--  _ts timestamp,
--   attribute_type varchar(255)
-- ) 
-- LANGUAGE plpgsql
-- AS $function$ 
-- BEGIN    
--     -- find the min and max value of data within the timerange
--     return query 
--     with alias as (
--         select aa.asset_id, a.name as asset_name, aa.id as attribute_id, aa.name as attribute_name, aa.data_type, aa.value::varchar(1024) as value, aa.updated_utc as _ts, aa.attribute_type as attribute_type, 1 as row_num--, targetAlias.attribute_id as target_alias_attribute
--         , (select attribute_id from find_root_alias_asset_attribute(aaa.alias_attribute_id) order by alias_level desc limit 1) as target_alias_attribute_id 
--         from asset_attributes aa
--         join assets a on aa.asset_id = a.id
--         join asset_attribute_alias aaa on aaa.asset_attribute_id  = aa.id
--         where aa.asset_id  = any (assetIds)
--     )
--      with q as (
--         -- from asset template
--         select am.asset_id, a.name as asset_name, am.id as attribute_id, aat.name as attribute_name, aat.data_type, dms.value::varchar(1024) as value, dms._ts as _ts, 'dynamic'::varchar(255) as attribute_type, row_number() over (
--         PARTITION by am.asset_id , am.asset_attribute_template_id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attribute_dynamic_mapping am
--         join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id
--         join device_metric_series dms on am.device_id  = dms.device_id  and am.metric_key  = dms.metric_key
--         join assets a on am.asset_id = a.id
--         where am.asset_id  = any (assetIds) and dms._ts <= snapshotDateTime 
        
--         union all 
--         select am.asset_id, a.name as asset_name, am.id as attribute_id, aat.name as attribute_name, aat.data_type, dms.value as value, dms."_ts" as _ts, 'dynamic'::varchar(255) as attribute_type, row_number() over (
--         PARTITION by am.asset_id , am.asset_attribute_template_id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attribute_dynamic_mapping am
--         join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id
--         join device_metric_series_text dms on am.device_id  = dms.device_id  and am.metric_key  = dms.metric_key
--         join assets a on am.asset_id = a.id
--         where am.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
--         union all 
--         select am.asset_id, a.name as asset_name, am.id as attribute_id, aat.name as attribute_name, aat.data_type , dms.value::varchar(1024) as value, dms._ts as _ts, 'runtime'::varchar(255) as attribute_type , row_number() over (
--         PARTITION by am.asset_id , am.asset_attribute_template_id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attribute_runtime_mapping  am
--         join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id
--         join asset_attribute_runtime_series dms on am.id  = dms.asset_attribute_id and am.asset_id  = dms.asset_id
--         join assets a on am.asset_id = a.id
--         where am.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
--         union all 
--         select am.asset_id, a.name as asset_name, am.id as attribute_id, aat.name as attribute_name, aat.data_type , dms.value as value, dms."_ts" as _ts, 'runtime'::varchar(255) as attribute_type , row_number() over (
--         PARTITION by am.asset_id , am.asset_attribute_template_id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attribute_runtime_mapping  am
--         join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id
--         join asset_attribute_runtime_series_text dms on am.id  = dms.asset_attribute_id and am.asset_id  = dms.asset_id
--         join assets a on am.asset_id = a.id
--         where am.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
--         union all 
--         select am.asset_id, a.name as asset_name, am.id as attribute_id, aat.name as attribute_name, aat.data_type ,am.value::varchar(1024) as value, am.updated_utc as _ts, aat.attribute_type as attribute_type ,1 as row_num
--         from asset_attribute_static_mapping am
--         inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id 
--         join assets a on am.asset_id = a.id
--         where am.asset_id  = any (assetIds) and am.updated_utc <= snapshotDateTime 
        
--         -- from standalone asset
        
--         union all 
--         select aa.asset_id, a.name as asset_name, aa.id as attribute_id, aa.name as attribute_name, aa.data_type, dms.value::varchar(1024) as value, dms._ts as _ts, aa.attribute_type as attribute_type , row_number() over (
--        PARTITION by aa.asset_id , aa.id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attribute_dynamic aad
--         inner join asset_attributes aa on aad.asset_attribute_id = aa.id and aa.attribute_type  = 'dynamic'
--         join device_metric_series dms on aad.device_id  = dms.device_id  and aad.metric_key  = dms.metric_key 
--         join assets a on aa.asset_id = a.id
--         where aa.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
--         union all 
--         select aa.asset_id, a.name as asset_name, aa.id as attribute_id, aa.name as attribute_name, aa.data_type, dms.value as value, dms."_ts" _ts, aa.attribute_type as attribute_type , row_number() over (
--         PARTITION by aa.asset_id , aa.id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attribute_dynamic aad
--         inner join asset_attributes aa on aad.asset_attribute_id = aa.id and aa.attribute_type  = 'dynamic'
--         join device_metric_series_text dms on aad.device_id  = dms.device_id  and aad.metric_key  = dms.metric_key 
--         join assets a on aa.asset_id = a.id
--         where aa.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
        
--         union all 
--         select aa.asset_id, a.name as asset_name, aa.id as attribute_id, aa.name as attribute_name, aa.data_type,dms.value::varchar(1024) as value, dms._ts as _ts, aa.attribute_type as attribute_type , row_number() over (
--         PARTITION by aa.asset_id , aa.id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attributes aa
--         join asset_attribute_runtime_series dms on aa.id  = dms.asset_attribute_id and aa.asset_id  = dms.asset_id
--         join assets a on aa.asset_id = a.id
--         where aa.attribute_type  = 'runtime' and aa.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
--         union all 
--         select aa.asset_id, a.name as asset_name, aa.id as attribute_id, aa.name as attribute_name, aa.data_type, dms.value as value, dms."_ts" as _ts, aa.attribute_type as attribute_type , row_number() over (
--         PARTITION by aa.asset_id , aa.id 
--         ORDER BY dms._ts
--         ) as row_num
--         from asset_attributes aa
--         join asset_attribute_runtime_series_text dms on aa.id  = dms.asset_attribute_id and aa.asset_id  = dms.asset_id
--         join assets a on aa.asset_id = a.id
--         where aa.attribute_type  = 'runtime' and aa.asset_id  = any (assetIds)  and dms._ts <= snapshotDateTime 
        
        
--         union all 
--         select aa.asset_id, a.name as asset_name, aa.id as attribute_id, aa.name as attribute_name, aa.data_type, aa.value::varchar(1024) as value, aa.updated_utc as _ts, aa.attribute_type as attribute_type, 1 as row_num
--         from asset_attributes aa
--         join assets a on aa.asset_id = a.id
--         where aa.attribute_type   = 'static' and aa.asset_id  = any (assetIds)    and aa.updated_utc <= snapshotDateTime 

--         -- ALIAS
       
-- 	)
-- 	select q.asset_id, q.asset_name, q.attribute_id, q.attribute_name, q.data_type, q.value, q._ts , q.attribute_type from q where q.row_num = 1
-- 	;
-- END $function$
-- ;