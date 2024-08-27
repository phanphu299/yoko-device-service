drop view if exists v_template_valid;
create view v_template_valid
AS

SELECT DISTINCT 
    t0.id AS id, 
    t0.name AS name,
    t0.deleted,
    t0.created_utc,
    t0.updated_utc
from device_templates t0
INNER JOIN (
	SELECT template_id_d AS template_id FROM
	(
   		(
			SELECT 
				t.id AS template_id_d
			FROM 
				template_details td 
			INNER JOIN template_key_types tkt ON td.key_type_id  = tkt.id
			INNER JOIN template_payloads tp  ON tp.id  = td.template_payload_id 
			INNER JOIN device_templates t ON t.id = tp.device_template_id 
			WHERE tkt.name = 'device_id'
	    ) AS x
	    JOIN
	    (
			SELECT 
				t2.id AS template_id_t
			FROM 
				template_details td2 
			INNER JOIN template_key_types tkt2 ON td2.key_type_id  = tkt2.id
			INNER JOIN template_payloads tp2  ON tp2.id  = td2.template_payload_id 
			INNER JOIN device_templates t2 ON t2.id = tp2.device_template_id
			WHERE tkt2.name = 'timestamp'
		) AS y ON x.template_id_d = y.template_id_t
  
	)
) AS a ON t0.id = a.template_id;