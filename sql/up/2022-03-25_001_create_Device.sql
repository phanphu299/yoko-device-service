CREATE EXTENSION IF NOT EXISTS "uuid-ossp" ;
create table if not exists data_types(
	id INT GENERATED ALWAYS AS IDENTITY primary key,
	name varchar(255) not null,
	abbreviation varchar(255),
	description varchar(255)
);
--template
create table if not exists templates(
	id int GENERATED ALWAYS AS IDENTITY  primary key,
	name varchar(255) not null,
	total_metric int default 0,
	deleted boolean not null default false,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    current_user_upn varchar(255) DEFAULT NULL,
    "current_timestamp" TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_user_upn varchar(255) DEFAULT NULL,
    request_lock_timestamp TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL,
    request_lock_timeout TIMESTAMP WITHOUT TIME ZONE DEFAULT NULL
);

create table if not exists template_key_types(
	id int GENERATED ALWAYS AS IDENTITY  primary key,
	name varchar(255) not null,
	deleted boolean not null default false
);

create table if not exists template_payloads(
	id int GENERATED ALWAYS AS IDENTITY  primary key,
	template_id int not null,
	json_payload text not null,
	CONSTRAINT fk_template_payloads_template       FOREIGN KEY(template_id)  	  REFERENCES templates(id) ON DELETE CASCADE
);

create table if not exists template_details(
	id int GENERATED ALWAYS AS IDENTITY  primary key,
	template_payload_id int not null,
	key varchar(255) not null,
	name varchar(255) not null,
	key_type_id int not null,
	data_type_id int not null,
	expression varchar null,
	enabled boolean not null default false,
	enable_deadband_compression boolean not null default false,
	enable_swingdoor_compression boolean not null default false,
	idle_timeout int null,
	exception_deviation_plus double precision null,
	exception_deviation_minus double precision null, 
	compression_deviation_plus double precision null, 
	compression_deviation_minus double precision null, 
	CONSTRAINT fk_template_details_template_payload       FOREIGN KEY(template_payload_id)  	  REFERENCES template_payloads(id) ON DELETE CASCADE,
	CONSTRAINT fk_template_details_template_key_type       FOREIGN KEY(key_type_id)  	  REFERENCES template_key_types(id) ON DELETE CASCADE,
	CONSTRAINT fk_template_details_data_type       FOREIGN KEY(data_type_id)  	  REFERENCES data_types(id) ON DELETE CASCADE
);

--device
create table if not exists devices (
	id varchar(50) unique not null DEFAULT uuid_generate_v4(),
	name varchar(255) null, 
	template_id int not null,
	status varchar(2) not null default 'IN',
	_ts TIMESTAMP WITHOUT TIME zone,
	description text,
	retention_days int not null default 90,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
	CONSTRAINT fk_devices_template_id     FOREIGN KEY(template_id)    REFERENCES templates(id) ON DELETE CASCADE
);

create table IF NOT EXISTS device_info(
	id int GENERATED ALWAYS AS IDENTITY primary key,
	device_id varchar(50) not null, 
	type varchar(255),
	_ts TIMESTAMP WITHOUT TIME zone,
	value text,
	CONSTRAINT fk_device_info_device       FOREIGN KEY(device_id)  	  REFERENCES devices(id) ON DELETE CASCADE,
	CONSTRAINT uq_device_info_device_type  UNIQUE (device_id, type)
);

create table if not exists uoms (
	id int GENERATED ALWAYS AS IDENTITY primary key,
	name varchar(255) not null,
	abbreviation varchar(255),
	description varchar(255),
	ref_factor double precision not null default 1,
	ref_offset double precision not null default 0,
	canonical_factor double precision not null default 1,
	canonical_offset double precision not null default 0,
	ref_id int null,
	lookup_code varchar(255),
	deleted boolean not null default false,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
	CONSTRAINT fk_uoms_ref_id    FOREIGN KEY(ref_id)    REFERENCES uoms(id) ON DELETE set null
);

-- create table if not exists metrics (
-- 	id int GENERATED ALWAYS AS IDENTITY  primary key,
-- 	name varchar(255) not null, 
-- 	description varchar(255),
-- 	data_type_id int not null,
-- 	expression varchar(255) null,
-- 	CONSTRAINT fk_metric_data_type       FOREIGN KEY(data_type_id)  	  REFERENCES data_types(id) ON DELETE CASCADE,
-- 	CONSTRAINT uq_metric_name unique (name)
-- );

-- create table if not exists device_metrics(
-- 	id bigint GENERATED ALWAYS AS IDENTITY  primary key, 
-- 	device_id varchar(50),
-- 	metric_id int,
-- 	enable boolean,
-- 	total_rule int not null default 0,
-- 	CONSTRAINT fk_device_metric_device       FOREIGN KEY(device_id)  	  REFERENCES devices(id) ON DELETE CASCADE,
-- 	CONSTRAINT fk_device_metric_metric       FOREIGN KEY(metric_id)  	  REFERENCES metrics(id) ON DELETE CASCADE
-- );

-- CREATE TABLE IF NOT EXISTS integrations (
--     id uuid DEFAULT uuid_generate_v4(),
--     name varchar(255) NOT NULL,
--     PRIMARY KEY (id)
-- );

-- create table IF NOT EXISTS device_externals(
--     id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
--     integration_id uuid NOT NULL,
--     device_id varchar(255) NOT NULL,
--     metric_id varchar(255) NOT NULL,
-- 	CONSTRAINT uq_device_externals UNIQUE(integration_id,device_id,metric_id),
-- 	CONSTRAINT fk_device_externals_integrations   FOREIGN KEY(integration_id)   REFERENCES integrations(id) ON DELETE CASCADE
-- );