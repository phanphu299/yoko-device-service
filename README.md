# Local development
az login -t d9f3dee8-148c-49ea-8e87-dd97cd0cd5de
az account set -s 7a9a0f8c-eb0d-4803-89f5-4e9e32a6333d
az acr login -n dxpprivate

$trackingEndpoint = 'https://ahs-test01-ppm-be-sea-wa.azurewebsites.net/fnc/mst/messaging/rabbitmq?code=xKvUgzJgdbwRcBRvPsee3gPbmBMXTpR8pkWhTWQky6RfvW5cxe5kqn94C9D'
Invoke-WebRequest $trackingEndpoint | Set-Content './rabbitmq/rabbitmq-definitions.json'

$env:CAKE_SETTINGS_SKIPPACKAGEVERSIONCHECK="true"
.\build.ps1 --target=Compose
.\build.ps1 --target=Up

newman run -k -e ./tests/IntegrationTest/AppData/Docker.postman_environment.json ./tests/IntegrationTest/AppData/Test.postman_collection.json


$postParams = Get-Content './rabbitmq/rabbitmq-definitions.json'
Invoke-WebRequest -Uri $trackingEndpoint -Method POST -Body $postParams

.\build.ps1 --target=Down

$env:ConnectionStrings__Default="User ID=postgres;Password=Pass1234!;Host=localhost;Port=5432;Database=device_34e5ee62429c4724b3d03891bd0a08c9;Pooling=true"
 .\build.ps1 --target=migrate --base64ConnectionString=false --databaseType=postgres
 .\build.ps1 --target=migrate --base64ConnectionString=false --databaseType=postgres --sql=sql/no-trans

# Integration Test Predefine Variable
#.\build.ps1 --target=IntegrationTest --testFolder=tests/IntegrationTest
$sessionId
$timestamp
$randomDateRecent
$guid
$randomFirstName
$randomEmail
$randomUrl

# Generate diagram
.\schemacrawler.bat --password=Pass1234! --user=postgres --database=device_34e5ee62429c4724b3d03891bd0a08c9 --host=127.0.0.1 --port=5432 --server=postgresql --command=schema --output-format=png --output-file=graph.png --info-level=detailed  --schemas=public


MessagePack info: https://msgpack.org/index.html