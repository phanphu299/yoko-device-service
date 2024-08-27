UPDATE asset_attribute_templates
SET decimal_place = 2
WHERE attribute_type IN ('static', 'dynamic', 'integration', 'runtime')
AND data_type = 'double'
AND decimal_place IS NULL;

UPDATE asset_attribute_templates
SET thousand_separator = true
WHERE attribute_type IN ('static', 'dynamic', 'integration', 'runtime')
AND data_type IN ('integer', 'double')
AND thousand_separator IS NULL;