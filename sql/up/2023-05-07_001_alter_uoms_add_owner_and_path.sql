alter table uoms add column resource_path varchar(1024);
alter table uoms add column created_by varchar(255) constraint default_uoms_created_by default 'thanh.tran@yokogawa.com';
CREATE INDEX IF NOT EXISTS idx_uoms_resourcepath_createdby ON uoms(resource_path, created_by);
