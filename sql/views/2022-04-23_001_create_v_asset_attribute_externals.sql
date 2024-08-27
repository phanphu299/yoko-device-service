create or replace view v_asset_attribute_externals
as
select aaim.asset_id , aaim.integration_id , aaim.device_id  
from asset_attribute_integration_mapping aaim 

union all 

select aa.asset_id, aai.integration_id , aai.device_id  
from asset_attribute_integration aai 
inner join asset_attributes aa on aai.asset_attribute_id  = aa.id 
