DO
$do$
declare
   old_type_code varchar(10) = 'DECIMAL';
   new_type_code varchar(10) = 'DOUBLE';
   new_type_name varchar(20) = 'double precision';
  
   col record;
  
   table_id uuid;
   table_name varchar(255);
   asset_id uuid;
   column_name varchar(255);
   default_value varchar(255);
  
   table_desc varchar(350);
   query_alter_type varchar(1000);
   query_alter_default_value varchar(1000);
   default_value_update varchar(255);
BEGIN
   for col in 
   	select 
   		at2.id table_id,
   		at2."name" table_name,
   		at2.asset_id asset_id,
   		atc."name" column_name,
   		atc.default_value 
   	from 
   		asset_tables at2  
   		join asset_table_columns atc on at2.id = atc.asset_table_id 
   	where at2.deleted != true 
   	and atc.type_code = 'DECIMAL'
    and at2.asset_id is not null
   loop 
   	table_id = col.table_id;
    table_name = col.table_name;
   	asset_id = col.asset_id;
    column_name = col.column_name;
    default_value = col.default_value;
   
    if(default_value is null) then
     default_value_update = '';
   	else
   	 default_value_update = ' set default ''' || default_value || '''::' || new_type_name;
   	end if;
   	RAISE NOTICE 'default value update: (%)', default_value_update;
   
    table_desc = concat('asset_', asset_id, '_', table_name);
    query_alter_type = concat('alter table "', table_desc, '" alter column ',  column_name, ' type ', new_type_name);
    query_alter_default_value = concat('alter table "', table_desc, '" alter column ', column_name, default_value_update);
   
    -- alter table asset
    EXECUTE query_alter_type;
   
    if(default_value is not null) then
     execute query_alter_default_value;
    end if;
   	RAISE NOTICE 'Alter successfully, Table: (%), Column: (%)', table_desc, column_name;
   end loop;
    -- update asset_table_columns information
  	update asset_table_columns set type_code = new_type_code, type_name = new_type_name 
    where deleted != true 
   		and type_code = 'DECIMAL';
end;
$do$