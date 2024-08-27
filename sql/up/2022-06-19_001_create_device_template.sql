-- device template
create table if not exists device_templates(
	id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
	name varchar(255) not null,
	total_metric int default 0,
	deleted boolean not null default false,
	created_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);

-- template payload
alter table template_payloads add device_template_id uuid null;
alter table template_payloads add CONSTRAINT fk_template_payloads_device_template       FOREIGN KEY(device_template_id)  	  REFERENCES device_templates(id) ON DELETE CASCADE;
alter table template_payloads drop CONSTRAINT fk_template_payloads_template;
alter table template_payloads alter column template_id DROP NOT NULL;

-- device template
alter table devices add device_template_id uuid null;
alter table devices add CONSTRAINT fk_devices_device_template_id       FOREIGN KEY(device_template_id)  	  REFERENCES device_templates(id) ON DELETE CASCADE;
alter table devices drop CONSTRAINT fk_devices_template_id;
alter table devices alter column template_id DROP NOT NULL;

-- template_bindings
alter table template_bindings add device_template_id uuid null;
alter table template_bindings add CONSTRAINT fk_templates_device_template_id       FOREIGN KEY(device_template_id)  	  REFERENCES device_templates(id) ON DELETE CASCADE;
alter table template_bindings drop CONSTRAINT fk_templates;
alter table template_bindings alter column template_id DROP NOT NULL;

-- asset_attribute_template_dynamics
alter table asset_attribute_template_dynamics add device_template_id uuid null;
alter table asset_attribute_template_dynamics add CONSTRAINT fk_asset_attribute_template_dynamics_device_template_id       FOREIGN KEY(device_template_id)  	  REFERENCES device_templates(id) ON DELETE CASCADE;
alter table asset_attribute_template_dynamics drop CONSTRAINT fk_asset_attribute_template_dynamics_template_id;
alter table asset_attribute_template_dynamics alter column template_id DROP NOT NULL;