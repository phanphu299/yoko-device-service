-- Add column system (bit) to distingust between user data and predefined data

ALTER TABLE uoms ADD COLUMN "system" BOOLEAN NOT NULL DEFAULT false;