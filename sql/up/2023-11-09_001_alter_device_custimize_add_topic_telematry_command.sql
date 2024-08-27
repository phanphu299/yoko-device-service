ALTER TABLE devices ADD COLUMN IF NOT EXISTS telemetry_topic text;
ALTER TABLE devices ADD COLUMN IF NOT EXISTS command_topic text;
ALTER TABLE devices ADD COLUMN IF NOT EXISTS has_command boolean default true;
