-- Add column system (bit) to distingust between user data and predefined data

ALTER TABLE function_blocks ADD COLUMN "system" BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE function_block_bindings ADD COLUMN "system" BOOLEAN NOT NULL DEFAULT false;
