create table entity_locks
(
    id uuid not null,
    locked_by_upn varchar(255),
    request_unlock_upn varchar(255),
    request_unlock_timeout timestamp,
    created_utc    timestamp default now()              not null,
    updated_utc    timestamp default now()              not null
);