-- Change broker_id column type to uuid and casting existing data to new type
ALTER TABLE devices ALTER COLUMN broker_id TYPE uuid USING broker_id::uuid;