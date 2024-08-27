alter table devices add column healthz_interval smallint;
alter table devices add column signal_quality_code smallint;
alter table devices add column enable_health_check boolean NOT NULL DEFAULT false;
alter table devices add column health_check_method_id smallint NULL;
alter table devices add column last_heartbeat_timestamp timestamp WITHOUT TIME ZONE;
alter table devices add CONSTRAINT fk_devices_signal_quality_code FOREIGN KEY(signal_quality_code) REFERENCES device_signal_quality_codes(id) ON DELETE CASCADE;
alter table devices add CONSTRAINT fk_devices_health_check_method FOREIGN KEY(health_check_method_id) REFERENCES device_health_check_methods(id) ON DELETE CASCADE;