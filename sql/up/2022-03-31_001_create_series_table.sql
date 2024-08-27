-- snapshot  
CREATE TABLE IF NOT EXISTS device_metric_external_snapshots (
    integration_id uuid NOT NULL,
    device_id varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
    value varchar(1024) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    CONSTRAINT IDX_device_metric_external_snapshots PRIMARY KEY (integration_id,device_id,metric_key)
    --CONSTRAINT fk_device_metric_external_snapshots_integration_id FOREIGN KEY(integration_id) REFERENCES integrations (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS device_metric_snapshots (
    device_id varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
    value varchar(1024) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    CONSTRAINT IDX_device_metric_snapshots PRIMARY KEY (device_id,metric_key),
    CONSTRAINT fk_device_metric_snapshots_device_id FOREIGN KEY(device_id) REFERENCES devices (id) ON DELETE CASCADE
    --CONSTRAINT fk_device_metric_snapshots_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS asset_attribute_runtime_snapshots (
    asset_id uuid NOT NULL,
    asset_attribute_id uuid NOT NULL,
    value varchar(1024) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    CONSTRAINT IDX_asset_attribute_runtime_snapshots PRIMARY KEY (asset_id,asset_attribute_id),
    CONSTRAINT fk_asset_attribute_runtime_snapshots_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE
);

-- CREATE TABLE IF NOT EXISTS device_metric_snapshots_boolean (
--     device_id varchar(255) NOT NULL,
--     metric_key varchar(255) NOT NULL,
--     value boolean NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT IDX_device_metric_snapshots_boolean PRIMARY KEY (device_id,metric_key),
--     CONSTRAINT fk_device_metric_snapshots_boolean_device_id FOREIGN KEY(device_id) REFERENCES devices (id) ON DELETE CASCADE,
--     CONSTRAINT fk_device_metric_snapshots_boolean_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
-- );

-- CREATE TABLE IF NOT EXISTS device_metric_snapshots_float (
--     device_id varchar(255) NOT NULL,
--     metric_key varchar(255) NOT NULL,
--     value double precision NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT IDX_device_metric_snapshots_float PRIMARY KEY (device_id,metric_key),
--     CONSTRAINT fk_device_metric_snapshots_float_device_id FOREIGN KEY(device_id) REFERENCES devices (id) ON DELETE CASCADE,
--     CONSTRAINT fk_device_metric_snapshots_float_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
-- );

-- CREATE TABLE IF NOT EXISTS device_metric_snapshots_text (
--     device_id varchar(255) NOT NULL,
--     metric_key varchar(255) NOT NULL,
--     value varchar(255) NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT IDX_device_metric_snapshots_text PRIMARY KEY (device_id,metric_key),
--     CONSTRAINT fk_device_metric_snapshots_text_device_id FOREIGN KEY(device_id) REFERENCES devices (id) ON DELETE CASCADE,
--     CONSTRAINT fk_device_metric_snapshots_text_metric_key FOREIGN KEY(metric_key) REFERENCES metrics (name) ON DELETE CASCADE
-- );

-- CREATE TABLE IF NOT EXISTS asset_attribute_runtime_snapshots_boolean (
--     asset_id uuid NOT NULL,
--     asset_attribute_id uuid NOT NULL,
--     value boolean NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT IDX_asset_attribute_runtime_snapshots_boolean PRIMARY KEY (asset_id,asset_attribute_id),
--     CONSTRAINT fk_asset_attribute_runtime_snapshots_boolean_attribute FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
--     CONSTRAINT fk_asset_attribute_runtime_snapshots_boolean_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE
-- );

-- CREATE TABLE IF NOT EXISTS asset_attribute_runtime_snapshots_float (
--     asset_id uuid NOT NULL,
--     asset_attribute_id uuid NOT NULL,
--     value double precision NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT IDX_asset_attribute_runtime_snapshots_float PRIMARY KEY (asset_id,asset_attribute_id),
--     CONSTRAINT fk_asset_attribute_runtime_snapshots_float_attribute FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
--     CONSTRAINT fk_asset_attribute_runtime_snapshots_float_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE
-- );

-- CREATE TABLE IF NOT EXISTS asset_attribute_runtime_snapshots_text (
--     asset_id uuid NOT NULL,
--     asset_attribute_id uuid NOT NULL,
--     value varchar(255) NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT IDX_asset_attribute_runtime_snapshots_text PRIMARY KEY (asset_id,asset_attribute_id),
--     CONSTRAINT fk_asset_attribute_runtime_snapshots_text_attribute FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
--     CONSTRAINT fk_asset_attribute_runtime_snapshots_text_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE
-- );


-- CREATE TABLE IF NOT EXISTS asset_attribute_static_snapshots (
--     asset_id uuid NOT NULL,
--     asset_attribute_id uuid NULL,
--     asset_attribute_template_id uuid NULL,
--     value text NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     CONSTRAINT fk_asset_attribute_static_snapshots_attribute FOREIGN KEY(asset_attribute_id) REFERENCES asset_attributes (id) ON DELETE CASCADE,
--     CONSTRAINT fk_asset_attribute_static_snapshots_att_template FOREIGN KEY(asset_attribute_template_id) REFERENCES asset_attribute_templates (id) ON DELETE CASCADE,
--     CONSTRAINT fk_asset_attribute_static_snapshots_asset_id FOREIGN KEY(asset_id) REFERENCES assets (id) ON DELETE CASCADE
-- );

--series
CREATE TABLE IF NOT EXISTS device_metric_series (
    device_id varchar(255) NOT NULL,
    metric_key varchar(255) NOT NULL,
     value NUMERIC(100, 20) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    retention_days int null default 90
);
-- CREATE TABLE IF NOT EXISTS device_metric_series_boolean (
--     device_id varchar(255) NOT NULL,
--     metric_key varchar(255) NOT NULL,
--     value boolean NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     retention_days int null default 0
-- );

-- CREATE TABLE IF NOT EXISTS device_metric_series_float (
--     device_id varchar(255) NOT NULL,
--     metric_key varchar(255) NOT NULL,
--     value double precision NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     retention_days int null default 0
-- );

-- CREATE TABLE IF NOT EXISTS device_metric_series_text (
--     device_id varchar(255) NOT NULL,
--     metric_key varchar(255) NOT NULL,
--     value varchar(255) NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     retention_days int null default 0
-- );

-- CREATE TABLE IF NOT EXISTS asset_attribute_runtime_series_boolean (
--     asset_id uuid NOT NULL,
--     asset_attribute_id uuid NOT NULL,
--     value boolean NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     retention_days int null default 0
-- );

-- CREATE TABLE IF NOT EXISTS asset_attribute_runtime_series_float (
--     asset_id uuid NOT NULL,
--     asset_attribute_id uuid NOT NULL,
--     value double precision NOT NULL,
--     _ts TIMESTAMP WITHOUT TIME zone,
--     retention_days int null default 0
-- );

CREATE TABLE IF NOT EXISTS asset_attribute_runtime_series (
    asset_id uuid NOT NULL,
    asset_attribute_id uuid NOT NULL,
    value NUMERIC(100, 20) NOT NULL,
    _ts TIMESTAMP WITHOUT TIME zone,
    retention_days int null default 90
);
