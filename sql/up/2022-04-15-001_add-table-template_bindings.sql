create table if not exists template_bindings(
	id int GENERATED ALWAYS AS IDENTITY  primary key,
	template_id int not null,
	key varchar(255) not null,
	data_type_id int not null,
	default_value varchar(255) not null,
	CONSTRAINT fk_templates       FOREIGN KEY(template_id)  	  REFERENCES templates(id) ON DELETE CASCADE,
	CONSTRAINT fk_template_details_data_type       FOREIGN KEY(data_type_id)  	  REFERENCES data_types(id)
);