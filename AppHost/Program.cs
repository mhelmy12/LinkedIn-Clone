using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sqlserver = builder.AddSqlServer("sqlserver", sqlPassword)
    .WithImageTag("2022-latest")
    .WithEndpoint(port: 14330, targetPort: 1433, name: "tcp", isProxied: false)
    .WithDataVolume("sqlserver_data")
    .WithExternalHttpEndpoints()
    .WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithLifetime(ContainerLifetime.Persistent);


sqlserver.AddDatabase("keycloak-db");

var kcDb = builder.AddParameter("KC-DB", secret: true);
var kcDbUrl = builder.AddParameter("KC-DB-URL", secret: true);
var kcDbUsername = builder.AddParameter("KC-DB-USERNAME", secret: true);
var kcDbPassword = builder.AddParameter("KC-DB-PASSWORD", secret: true);
var keycloak = builder.AddKeycloak("keycloak", 8082)
    .WithImageTag("latest")
    .WithDataVolume()
    .WithContainerNetworkAlias("linkedIn-network")
    .WithOtlpExporter()
    .WithExternalHttpEndpoints()
    .WithEnvironment("KC_DB", kcDb)
    .WithEnvironment("KC_DB_URL", kcDbUrl)
    .WithEnvironment("KC_DB_USERNAME", kcDbUsername)
    .WithEnvironment("KC_DB_PASSWORD", kcDbPassword)
    .WithReference(sqlserver)
    .WithRealmImport("./Realms")
    .WithArgs("--features=docker,admin-fine-grained-authz,token-exchange,quick-theme")
    .WithLifetime(ContainerLifetime.Persistent)

    .WithUrls(context =>
        {
            foreach (var u in context.Urls)
            {
                u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
            }

            context.Urls.Add(
                new ResourceUrlAnnotation()
                {
                    Url = "/",
                    DisplayText = "Admin Dashboard",
                    Endpoint = context.GetEndpoint("http"),
                }
            );
        });

var userService = builder.AddProject<UserService>("user-service");

builder.AddProject<APIGateway>("APIGateway").WithReference(userService).WithReference(keycloak, "keycloak").WithExternalHttpEndpoints();
builder.Build().Run();
