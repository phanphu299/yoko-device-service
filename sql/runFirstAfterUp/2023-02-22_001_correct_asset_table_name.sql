do $$
declare
    r record;
    query text;
begin
    for r in select id, name, asset_id from asset_tables
    loop
        -- Maxlength of table name is 63 chars
        query := format('alter table if exists %I rename to %I', concat('asset_', r.asset_id, '_', substring(r.name, 1, 20)), concat('asset_', r.id));
        execute query;
    end loop;
end; $$