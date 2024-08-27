-- --SELECT create_hypertable('metric_series_float', '_ts');
-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_minute;
-- CREATE MATERIALIZED VIEW v_time_series_minute
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '1 minute', msf._ts) AS time_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id,msf.metric_key, time_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_five_minutes;
-- CREATE MATERIALIZED VIEW v_time_series_five_minutes
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '5 minutes', msf._ts) AS time_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id, msf.metric_key,time_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_ten_minutes;
-- CREATE MATERIALIZED VIEW v_time_series_ten_minutes
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '10 minutes', msf._ts) AS time_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id, msf.metric_key,time_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_fifteen_minutes;
-- CREATE MATERIALIZED VIEW v_time_series_fifteen_minutes
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '15 minutes', msf._ts) AS time_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id,msf.metric_key, time_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_thirty_minutes;
-- CREATE MATERIALIZED VIEW v_time_series_thirty_minutes
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '30 minutes', msf._ts) AS time_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id, msf.metric_key,time_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_hourly;
-- CREATE MATERIALIZED VIEW v_time_series_hourly
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '1 hour', msf._ts) AS time_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id, msf.metric_key,time_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_weekly;
-- CREATE MATERIALIZED VIEW v_time_series_weekly
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '1 week', msf._ts) AS week_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id,msf.metric_key, week_bucket;


-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_yearly;
-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_monthly;
-- DROP MATERIALIZED VIEW IF EXISTS v_time_series_daily;

-- CREATE MATERIALIZED VIEW v_time_series_daily
-- WITH (timescaledb.continuous) AS
-- SELECT
-- 	msf.device_id,
-- 	msf.metric_key,
-- 	time_bucket(INTERVAL '1 day', msf._ts) AS day_bucket,
-- 	AVG(msf.value),
-- 	MAX(msf.value),
-- 	MIN(msf.value),
-- 	SUM(msf.value),
-- 	COALESCE(STDDEV(msf.value), 0) AS std
-- FROM device_metric_series msf
-- GROUP BY msf.device_id,msf.metric_key, day_bucket;


-- CREATE MATERIALIZED VIEW v_time_series_monthly AS
-- SELECT
-- 	daily.device_id,
-- 	daily.metric_key,
-- 	date_trunc('month', daily.day_bucket) AS month_bucket,
-- 	AVG(daily.avg),
-- 	MAX(daily.max),
-- 	MIN(daily.min),
-- 	SUM(daily.sum),
-- 	STDDEV(daily.std) AS std
-- FROM v_time_series_daily daily
-- GROUP BY daily.device_id,daily.metric_key, month_bucket;


-- CREATE  MATERIALIZED VIEW v_time_series_yearly AS
-- SELECT
-- 	daily.device_id,
-- 	daily.metric_key,
-- 	date_trunc('year', daily.day_bucket) AS year_bucket,
-- 	AVG(daily.avg),
-- 	MAX(daily.max),
-- 	MIN(daily.min),
-- 	SUM(daily.sum),
-- 	STDDEV(daily.std) AS std
-- FROM v_time_series_daily daily
-- GROUP BY daily.device_id,daily.metric_key, year_bucket;