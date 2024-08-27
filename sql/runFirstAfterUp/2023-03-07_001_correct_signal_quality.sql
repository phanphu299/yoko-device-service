update devices
set signal_quality_code = null
where devices.enable_health_check = false;