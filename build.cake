//#tool "nuget:https://www.nuget.org/api/v2?package=JetBrains.ReSharper.CommandLineTools&version=2018.1.0"
//#tool "nuget:https://www.nuget.org/api/v2?package=coveralls.io&version=1.4.2"
#addin nuget:?package=Cake.Docker&version=1.0.0
//#addin nuget:?package=Cake.Newman&version=0.3.1
#tool "nuget:?package=roundhouse"
///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Run");
var environment = Argument<string>("environment", "DEV01");
var tenantId = Argument<string>("tenantId", "0779433E-F36B-1410-8650-00F91313348C");
var subscriptionId = Argument<string>("subscriptionId", "0E79433E-F36B-1410-8650-00F91313348C");
var projectId = Argument<string>("projectId", "34E5EE62-429C-4724-B3D0-3891BD0A08C9");
var projectFiles = Argument<string>("projects", "./**/*.csproj").Split(',');
var connectionString = EnvironmentVariable("ConnectionStrings__Default");
var sqlFolder = Argument<string>("sql", "sql");
var output = Argument<string>("output", "output");
var waitTime =  Argument<int>("wait", 0);
var useTransaction = Argument<bool>("useTransaction", true);
var databaseType = Argument<string>("databaseType","sqlserver");
var doNotCreateDatabase = Argument<bool>("doNotCreateDatabase", true);
var base64ConnectionString = Argument<bool>("base64ConnectionString", true);
var outputPath = Argument<string>("outputPath", "C:/roundhouse/");
///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
//RoundhouseMigrate(settings);

var projects = projectFiles.SelectMany( x=> GetFiles(x));

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Information("Running tasks...");
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Task("Compose")
    .Description("Builds all the different parts of the project.")
    //.IsDependentOn("Clean")
    //.IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DockerComposeBuildSettings 
     {
        Files = new string[]{"docker/db.yml", "docker/service.yml", "docker/development.yml"}
     };

     DockerComposeBuild(settings);
});
Task("Up")
    .Description("Builds all the different parts of the project.")
    .Does( async () =>
{
    var settings = new DockerComposeUpSettings  
     {
        Files = new string[]{ "docker/service.yml",  "docker/storage.yml", "docker/db.yml"},
        Build = false,
        DetachedMode = true
     };

     DockerComposeUp(settings);
     Information($"Waiting for database to be boosted up in {waitTime}");
    await System.Threading.Tasks.Task.Delay(waitTime); //wait 20s for db to be booted up
});
Task("Down")
    .Description("Builds all the different parts of the project.")
    .Does(() =>
{
    var settings = new DockerComposeDownSettings  
     {
        Files = new string[]{"docker/db.yml", "docker/storage.yml", "docker/service.yml"}
     };

     DockerComposeDown(settings);
});
Task("DestroyDB")
    .Description("Destroy the current db instances.")
    .Does(() =>
{
    var settings = new DockerComposeDownSettings  
     {
        Files = new string[]{"docker/db.yml"}
     };

     DockerComposeDown(settings);
});

///////////////////////////////////////////////////////////////////////////////
// Migrate DB
///////////////////////////////////////////////////////////////////////////////
Task("Migrate")
    .Description("Migrate the current schema to target database")
    .Does(() =>
{
    if(base64ConnectionString){
        var base64EncodedBytes = System.Convert.FromBase64String(connectionString);
        connectionString =  System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }
    // Create the migration database
    var settings = new RoundhouseSettings {
        ConnectionString = connectionString.Replace("{{projectId}}", projectId.Replace("-","").ToLower()).Replace("{{subscriptionId}}", subscriptionId.Replace("-","").ToLower()).Replace("{{tenantId}}", tenantId.Replace("-","").ToLower()),
        DoNotCreateDatabase=doNotCreateDatabase,
        Silent=true,
        Drop=false,
        Debug=false,
        WithTransaction=useTransaction,
        SqlFilesDirectory = sqlFolder,
        DatabaseType = databaseType,
        Environment = environment,
        CommandTimeout = 3600,
        OutputPath = outputPath
    };
    RoundhouseMigrate(settings);
});

///////////////////////////////////////////////////////////////////////////////
// Unit Tests
///////////////////////////////////////////////////////////////////////////////

Task("IntegrationTest")
    .Description("Run all unit tests within the project.")
    .DoesForEach(projects, (p) => 
{
    if(p.GetFilename().ToString().ToLower().Contains("test"))
    {
        Information($"Test project: {p.ToString()}");
        // Calculate code coverage
        var settings = new DotNetCoreTestSettings
        {
            Logger = "trx",
            ArgumentCustomization = args => args.Append("/p:CollectCoverage=true")
                                                .Append("/p:CoverletOutputFormat=cobertura")
                                                .Append($"/p:CoverletOutput={output}/tests/")
                                                .Append("/p:Exclude=\"[Dapper.*]*\"")
                                                .Append("/p:Exclude=\"[Pipelines.Sockets.Unofficial.*]*\"")
                                                .Append("/p:Exclude=\"[StackExchange.Redis.*]*\"")
                                                .Append("/p:Exclude=\"[System.Interactive.Async]*\"")
                                                .Append("/p:ThresholdType=line")
                                                // .Append($"/p:Threshold={coverageThreshold}")
        };
        DotNetCoreTest(p.ToString(), settings);
    }
});
///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);