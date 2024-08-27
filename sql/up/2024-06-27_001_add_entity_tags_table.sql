CREATE TABLE IF NOT EXISTS entity_tags (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tag_id bigint NOT NULL,
    entity_id_varchar varchar(100) NULL,
    entity_id_int int NULL,
    entity_id_long bigint NULL,
    entity_id_uuid uuid NULL,
    entity_type varchar(100) NOT NULL
);