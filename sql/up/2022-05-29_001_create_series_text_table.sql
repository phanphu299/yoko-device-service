--series
CREATE TABLE IF NOT EXISTS device_metric_series_text (
    device_id varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
     value NUMERIC(100, 20) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    retention_days int null default 90
);
CREATE TABLE IF NOT EXISTS asset_attribute_runtime_series_text (
    asset_id uuid NOT NULL,
    asset_attribute_id uuid NOT NULL,
    value NUMERIC(100, 20) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    retention_days int null default 90
);
