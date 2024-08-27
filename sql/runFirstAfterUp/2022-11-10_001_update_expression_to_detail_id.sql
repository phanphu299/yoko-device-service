CREATE table IF NOT EXISTS complex_t (
    key text,
	detail_id   text,
  device_template_id uuid
);

do
$do$
DECLARE
  vart complex_t%ROWTYPE;
 
  cliente_cursor CURSOR FOR SELECT * FROM (
    select td.key  as "key"  ,td.detail_id::text , te.device_template_id
    from template_details td 
    inner join template_payloads tp2 on tp2.id = td.template_payload_id 
    inner join (
    select 	array_to_string(REGEXP_MATCHES(td.expression, '\{(.*?)\}', 'g'),';') as key, tp.device_template_id, td.id
    from template_details td 
    inner join template_payloads tp on tp.id = td.template_payload_id
    where td.expression is not null and td.expression <> ''
    ) te on tp2.device_template_id = te.device_template_id and td."key" = te.key) tbb; 
BEGIN
  FOR vart IN cliente_cursor loop
    update template_details td set "expression" = replace("expression", vart.key, vart.detail_id),
                                "expression_compile" = replace("expression_compile", vart.key, vart.detail_id)
    from template_payloads tp 
    where tp.id = td.template_payload_id and position(vart.key in "expression") <> 0 and tp.device_template_id = vart.device_template_id;
    
  END loop;
end
$do$;

DROP TABLE IF EXISTS complex_t;