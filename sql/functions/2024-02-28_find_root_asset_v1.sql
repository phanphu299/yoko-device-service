CREATE OR REPLACE FUNCTION public.find_root_asset_v1(assetids uuid[])
	RETURNS TABLE(id_output uuid, asset_path_id_output text, asset_path_name_output text)
 LANGUAGE plpgsql
AS $function$
declare
	tmp_resource_path text;
	tmp_asset_path_id text;
	tmp_index int = 1;
	tmp_id varchar(50);
	tmp_assetid uuid;
begin
CREATE TEMP TABLE tmp_assets(
								assetId uuid,
								resource_path text);
							
CREATE TEMP TABLE tmp_asset_paths(
								currentAssetId uuid,
								asset_path_id text,
								ancestorAssetId uuid,
								name character varying,
								index int);

-- only scan/seek data on tmp_assets
insert into tmp_assets
select a.id, a.resource_path
from assets a
where a.id = any(assetids);

-- process data
FOREACH tmp_assetid IN ARRAY assetids
LOOP
	-- reset temp data
	select 1 into tmp_index;   
	   
	-- get asset ids
	select a.resource_path into tmp_resource_path
	  from tmp_assets a
	  where a.assetId = tmp_assetid limit 1;
	 
	select tmp_resource_path into tmp_asset_path_id;
	
	SELECT REPLACE (tmp_resource_path, 'objects/', '')  into tmp_resource_path;
	
	while strpos(tmp_resource_path, '/children/') > 0 loop 
		SELECT split_part(tmp_resource_path, '/children/', 1) into tmp_id;
		SELECT REPLACE (tmp_resource_path, concat(tmp_id, '/children/'), '')  into tmp_resource_path;
		
		insert into tmp_asset_paths(currentAssetId, ancestorAssetId, index) values (tmp_assetid, uuid(tmp_id), tmp_index);
		select tmp_index + 1 into tmp_index;
	end loop;
	
	insert into tmp_asset_paths(currentAssetId, ancestorAssetId, index) values (tmp_assetid, uuid(tmp_resource_path), tmp_index);
	
	-- set asset_path_id
	SELECT REPLACE (tmp_asset_path_id, 'objects/', '')  into tmp_asset_path_id;
	SELECT REPLACE (tmp_asset_path_id, '/children/', '/')  into tmp_asset_path_id;

	update tmp_asset_paths set asset_path_id=tmp_asset_path_id where currentAssetId=tmp_assetid;
END LOOP;
  
-- set asset_path_name
update tmp_asset_paths set name = assets.name
from assets
where assets.id = ancestorAssetId;

-- return table
return query
select currentAssetId, asset_path_id, string_agg(name,'/' order by index) from tmp_asset_paths GROUP BY currentAssetId, asset_path_id;

drop table tmp_assets;
drop table tmp_asset_paths;

-- test: select * from find_root_asset_v1(ARRAY[uuid('f7038206-9b5c-4b4e-ae32-dd47132ddd74'), uuid('43df1cc4-60dd-4c9c-ba8b-2d3b93e2d7c0')]);

END $function$
;
