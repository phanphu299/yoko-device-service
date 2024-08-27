create table if not exists device_health_check_methods (
    id smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name varchar(255) not null,
    deleted boolean not null default false
);

insert into device_health_check_methods (name) values 
('Server Monitoring');
