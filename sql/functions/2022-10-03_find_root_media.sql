CREATE OR REPLACE FUNCTION public.find_root_media(mediaid int)
 RETURNS TABLE(id int, name character varying, parent_media_id int, parent_media_name character varying, media_level integer, media_path_id text, media_path_name text)
 LANGUAGE plpgsql
AS $function$
DECLARE
BEGIN
	RETURN query
	WITH RECURSIVE media_cte AS (
		SELECT 
			m.id, 
            m.name,
            m.parent_media_id , 
            pm.name AS parent_media_name, 
            1 as media_level,
            concat(m.parent_media_id , coalesce(CASE WHEN m.parent_media_id IS NOT NULL THEN '/' ELSE '' END), m.id) as media_path_id,
            concat(pm.name, coalesce(CASE WHEN pm.name IS NOT NULL THEN '/' ELSE '' END), m.name) as media_path_name
		FROM media m
        LEFT JOIN media pm ON pm.id = m.parent_media_id 
        where m.id = mediaid
        UNION all
        SELECT 
			m.id, 
            m.name,
            m.parent_media_id , 
            pm.name AS parent_media_name, 
            mcte.media_level + 1 AS media_level,
            concat(m.parent_media_id, coalesce(CASE WHEN m.parent_media_id IS NOT NULL THEN '/' ELSE '' END), mcte.media_path_id) as media_path_id,
            concat(pm.name, coalesce(CASE WHEN pm.name IS NOT NULL THEN '/' ELSE '' END), mcte.media_path_name) as media_path_name
		FROM media_cte mcte
		INNER JOIN media m ON m.id = mcte.parent_media_id
        LEFT JOIN media pm ON pm.id = m.parent_media_id
	)
    SELECT media_cte.id, media_cte.name, media_cte.parent_media_id, media_cte.parent_media_name, media_cte.media_level, media_cte.media_path_id, media_cte.media_path_name 
    FROM media_cte
    ORDER BY media_cte.media_level DESC;
END $function$
;
