alter table device_metric_series add column _lts TIMESTAMP WITHOUT TIME zone;
alter table device_metric_series add column last_good_value double precision;
alter table device_metric_series_text add column _lts TIMESTAMP WITHOUT TIME zone;
alter table device_metric_series_text add column last_good_value text;