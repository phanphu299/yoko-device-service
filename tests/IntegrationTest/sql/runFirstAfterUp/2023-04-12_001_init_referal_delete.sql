-- Clean old data
delete from asset_attribute_alias where alias_asset_id = 'fddb9fe7-034e-4651-b5f9-babf018d40ba';
delete from asset_attribute_dynamic where asset_attribute_id =  'bf6d9449-67c7-4b38-9e37-efdafa51e174';
delete from asset_attribute_runtimes where asset_attribute_id = 'c200dc88-2a8f-4238-858f-6ac71e347ea4';
delete from asset_attributes where asset_id in ('fddb9fe7-034e-4651-b5f9-babf018d40ba', 'be3395b5-af6c-4d07-85f4-de009a8da6d1');
delete from assets where id in ('fddb9fe7-034e-4651-b5f9-babf018d40ba', 'be3395b5-af6c-4d07-85f4-de009a8da6d1');

-- Initial for first Asset
insert into assets (id, name, asset_template_id, retention_days, created_by, resource_path)
values ('fddb9fe7-034e-4651-b5f9-babf018d40ba', 'Asset Root', null, 90,'thanh.tran@yokogawa.com', 'objects/fddb9fe7-034e-4651-b5f9-babf018d40ba');

-- runtime attributes
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( 'c200dc88-2a8f-4238-858f-6ac71e347ea4', 'fddb9fe7-034e-4651-b5f9-babf018d40ba', 'Root - Runtime', 'runtime', 'double', false);
insert into asset_attribute_runtimes(asset_attribute_id, enabled_expression, is_trigger_visibility)
values ('c200dc88-2a8f-4238-858f-6ac71e347ea4', false, false);

-- dynamic attributes
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( 'bf6d9449-67c7-4b38-9e37-efdafa51e174', 'fddb9fe7-034e-4651-b5f9-babf018d40ba', 'Root - Dynamic', 'dynamic', 'double', false);
insert into asset_attribute_dynamic(asset_attribute_id, device_id, metric_key)
values ('bf6d9449-67c7-4b38-9e37-efdafa51e174', '00-00-64-ff-fe-a3-8f-e0', 'ZVelocity');

-------------------------
-- Initial for next Asset
insert into assets (id, name, asset_template_id, retention_days, created_by, resource_path)
values ('be3395b5-af6c-4d07-85f4-de009a8da6d1', 'Asset Refer', null, 90,'thanh.tran@yokogawa.com', 'objects/be3395b5-af6c-4d07-85f4-de009a8da6d1');

-- Alias attributes
insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( 'cbf8343a-6ca0-458b-83d6-c60c832ec1de', 'be3395b5-af6c-4d07-85f4-de009a8da6d1', 'Root - Alias Runtime', 'alias', 'double', false);
insert into asset_attribute_alias(asset_attribute_id, alias_asset_id, alias_attribute_id)
values ('cbf8343a-6ca0-458b-83d6-c60c832ec1de', 'fddb9fe7-034e-4651-b5f9-babf018d40ba', 'c200dc88-2a8f-4238-858f-6ac71e347ea4');

insert into asset_attributes ( id, asset_id, name, attribute_type, data_type, enabled_expression)
values ( '34153dbd-6029-43e1-a38c-8a3c0993c74e', 'be3395b5-af6c-4d07-85f4-de009a8da6d1', 'Root - Alias Dynamic', 'alias', 'double', false);
insert into asset_attribute_alias(asset_attribute_id, alias_asset_id, alias_attribute_id)
values ('34153dbd-6029-43e1-a38c-8a3c0993c74e', 'fddb9fe7-034e-4651-b5f9-babf018d40ba', 'bf6d9449-67c7-4b38-9e37-efdafa51e174');
