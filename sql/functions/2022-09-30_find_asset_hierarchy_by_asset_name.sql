DROP FUNCTION IF EXISTS find_asset_hierarchy_by_asset_name(assetName varchar(255));
DROP FUNCTION IF EXISTS find_asset_hierarchy_by_asset_name(assetName varchar(255), tagIds bigint[]);
CREATE OR REPLACE FUNCTION find_asset_hierarchy_by_asset_name(assetName varchar(255), tagIds bigint[])
  RETURNS TABLE(
    id uuid, name varchar, created_utc timestamp, has_warning boolean, asset_template_id uuid, retention_days int,
  parent_asset_id uuid, resource_path varchar(1024), created_by varchar(100), found_id uuid, is_found_result boolean)
  LANGUAGE plpgsql
AS $function$
DECLARE
BEGIN
    RETURN query
    with found_assets as (
      select 
        a.id as found_id,
        unnest(string_to_array(a.resource_path, '/')) as id
      from assets a
      left join entity_tags et on (et.entity_id_uuid = a.id and et.entity_type = 'Asset')
      where a.name ilike concat('%', assetName , '%')
      and (array_length(tagIds, 1) is NULL OR et.tag_id in (select unnest(tagIds)))
    )
    select 
      a.id,
      a.name,
      a.created_utc,
      aw.asset_id is not null as has_warning,
      a.asset_template_id,
      a.retention_days,
      a.parent_asset_id,
      a.resource_path,
      a.created_by,
      fa.found_id,
      fa.id::uuid = fa.found_id as is_found_result
    from assets a
    inner join found_assets fa on fa.id != 'objects' and fa.id != 'children' and a.id = fa.id::uuid
    left join find_asset_warning(null, assetName, null) aw on a.id = aw.asset_id
    order by fa.found_id, fa.id::uuid = fa.found_id desc;
END $function$
;
