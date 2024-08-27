create table if not exists device_bindings(
	id int GENERATED ALWAYS AS IDENTITY primary key,
    device_id varchar(50) not null,
	template_bindings_id int not null,
	key varchar(255) not null,
	value varchar(255) null,
	CONSTRAINT fk_devices_template_binding_id       FOREIGN KEY(template_bindings_id)  	  REFERENCES template_bindings(id) ON DELETE CASCADE,
    CONSTRAINT fk_devices_id      FOREIGN KEY(device_id)  	  REFERENCES devices(id) ON DELETE CASCADE
);