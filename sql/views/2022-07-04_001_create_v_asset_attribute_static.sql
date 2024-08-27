create or replace view v_asset_attribute_static
as
select
    aasm.asset_id,
    aasm.id as attribute_id,
    now() as _ts,
    coalesce(cast(aasm.value as varchar(1024)), '') as value
from asset_attribute_static_mapping aasm
inner join asset_attribute_templates aat on aasm.asset_attribute_template_id = aat.id

union all
select 
    aa.asset_id,
    aa.id as attribute_id, 
    now() as _ts,
    coalesce(cast(aa.value as varchar(1024)), '') as value
from asset_attributes aa
where attribute_type = 'static';
-- trigger for update