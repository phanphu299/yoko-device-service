UPDATE device_templates
SET name = CONCAT(name, id)
WHERE name IN
(SELECT name FROM device_templates GROUP BY (name) HAVING count(id) > 1);