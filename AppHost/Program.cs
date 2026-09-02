using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var userService = builder.AddProject<UserService>("user-service");

builder.AddProject<APIGateway>("APIGateway").WithReference(userService).WithExternalHttpEndpoints();
builder.Build().Run();
