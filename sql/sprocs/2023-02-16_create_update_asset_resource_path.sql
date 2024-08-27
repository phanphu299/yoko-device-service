create or replace procedure fnc_udp_update_asset_resource_path(assetId uuid)
language plpgsql
as $$
begin
  with updated_asset as (
    select a.id, a.resource_path, case
      when a.parent_asset_id is null then concat('objects/', a.id)
      else concat(pa.resource_path, '/children/', a.id)
    end as new_resource_path
    from assets a
    left join assets pa on a.parent_asset_id = pa.id
    where a.id = assetId 
    and (
        a.resource_path is null
        or (a.parent_asset_id is null and a.resource_path != concat('objects/', a.id))
        or (a.parent_asset_id is not null and a.resource_path != concat(pa.resource_path, '/children/', a.id))
    )
  )
  update assets a
  set resource_path = case 
    when a.resource_path is not null and ua.resource_path is not null 
      then replace(a.resource_path, ua.resource_path, ua.new_resource_path)
    else ua.new_resource_path
  end
  from updated_asset ua
  where a.resource_path like concat(ua.resource_path, '/%')
  or a.id = ua.id;
return;
end; $$
-- trigger for update