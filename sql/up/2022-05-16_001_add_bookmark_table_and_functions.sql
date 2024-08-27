create table bookmarks(
   id UUID primary key default uuid_generate_v1(),
   upn varchar(100) not null,
   asset_id UUID not null,
   created_utc timestamp not null,
   updated_utc timestamp not null,
   deleted bit not null default '0',
   CONSTRAINT fk_assets_asset_id FOREIGN KEY (asset_id) REFERENCES assets(id) ON DELETE CASCADE
);

create index ix_upn on bookmarks (upn, asset_id);