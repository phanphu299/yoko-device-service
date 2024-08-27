ALTER TABLE device_metric_series 
DROP CONSTRAINT IF EXISTS fk_device_metric_series_signal_quality_code;

ALTER TABLE device_metric_series_text 
DROP CONSTRAINT IF EXISTS fk_device_metric_series_text_signal_quality_code;

ALTER TABLE device_metric_series
DROP COLUMN IF EXISTS signal_quality_code;

ALTER TABLE device_metric_series_text
DROP COLUMN IF EXISTS signal_quality_code;

alter table device_metric_series add column signal_quality_code smallint default 192;
alter table device_metric_series_text add column signal_quality_code smallint default 192;
-- alter table device_metric_series add CONSTRAINT fk_device_metric_series_signal_quality_code FOREIGN KEY(signal_quality_code) REFERENCES device_signal_quality_codes(id) ON DELETE CASCADE;
-- alter table device_metric_series_text add CONSTRAINT fk_device_metric_series_text_signal_quality_code FOREIGN KEY(signal_quality_code) REFERENCES device_signal_quality_codes(id) ON DELETE CASCADE;