ALTER TABLE assets
ADD COLUMN is_document boolean default false; -- if true -> asset document -> will sync to asset viewer