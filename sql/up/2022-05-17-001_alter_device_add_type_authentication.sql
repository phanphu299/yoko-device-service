ALTER TABLE devices ADD COLUMN parent_device_id varchar(255) NULL;
ALTER TABLE devices ADD COLUMN iot_authentication_type varchar(255) NULL;
ALTER TABLE devices ADD COLUMN primary_thumbprint varchar(255) NULL;
ALTER TABLE devices ADD COLUMN secondary_thumbprint varchar(255) NULL;