update  asset_attribute_runtimes 
set expression_compile = 
REPLACE(
REPLACE( 
REPLACE( 
REPLACE(expression_compile, '"]', '"])'), '(bool)request', 'Convert.ToBoolean(request'), '(double)request', 'Convert.ToDouble(request'), '(int)request', 'Convert.ToInt(request')
where expression_compile is not null;

update  asset_attribute_runtime_mapping 
set expression_compile = 
REPLACE(
REPLACE( 
REPLACE( 
REPLACE(expression_compile, '"]', '"])'), '(bool)request', 'Convert.ToBoolean(request'), '(double)request', 'Convert.ToDouble(request'), '(int)request', 'Convert.ToInt(request')
where expression_compile is not null;