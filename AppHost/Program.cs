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
    .WithEnvironment("MSSQL_AGENT_ENABLED", "True")
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

keycloak.WaitFor(sqlserver);


var kafka = builder.AddKafka("kafka")
    .WithEndpoint(port: 9094, targetPort: 9094, name: "external", isProxied: false)
    .WithEndpoint(targetPort: 9092, name: "internal", isProxied: false)
    .WithEndpoint(port: 9095, targetPort: 9095, name: "external-docker", isProxied: false)
    .WithImage("confluentinc/cp-kafka", "latest")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:9093")
    .WithEnvironment("KAFKA_LISTENERS", "INTERNAL://0.0.0.0:9092,EXTERNAL_DOCKER://0.0.0.0:9095,EXTERNAL://0.0.0.0:9094,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "INTERNAL://kafka:9092,EXTERNAL_DOCKER://host.docker.internal:9095,EXTERNAL://localhost:9094,CONTROLLER://kafka:9093")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "INTERNAL:PLAINTEXT,EXTERNAL_DOCKER:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
    .WithEnvironment("CLUSTER_ID", "MkU3OEVBNTcwNTJENDM2Qk")
    .WithVolume("kafka_data", "/var/lib/kafka/data");




var kafkaUi = builder.AddContainer("kafka-ui", "kafbat/kafka-ui", "latest")
.WithHttpEndpoint(port: 8088, targetPort: 8080, name: "http", isProxied: false)
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka:9092")
    .WithReference(kafka)
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
                    DisplayText = "Kafka UI Dashboard",
                    Endpoint = context.GetEndpoint("http"),
                }
            );
        });



var debezium = builder.AddContainer("debezium", "quay.io/debezium/connect", "latest")
    .WithEnvironment("BOOTSTRAP_SERVERS", "kafka:9092")
    .WithEnvironment("GROUP_ID", "banking-connect")
    .WithEnvironment("CONFIG_STORAGE_TOPIC", "connect_configs")
    .WithEnvironment("OFFSET_STORAGE_TOPIC", "connect_offsets")
    .WithEnvironment("STATUS_STORAGE_TOPIC", "connect_status")
    .WithEnvironment("CONFIG_STORAGE_REPLICATION_FACTOR", "1")
    .WithEnvironment("OFFSET_STORAGE_REPLICATION_FACTOR", "1")
    .WithEnvironment("STATUS_STORAGE_REPLICATION_FACTOR", "1")
    .WithReference(kafka)
    .WithReference(sqlserver)
    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "http", isProxied: false)
    .WithExternalHttpEndpoints();

var debeziumUi = builder.AddContainer("debezium-ui", "debezium/debezium-ui", "latest")
    .WithEnvironment("KAFKA_CONNECT_URIS", debezium.GetEndpoint("http"))
    .WithHttpEndpoint(port: 8085, targetPort: 8080, name: "http", isProxied: false)

    .WithExternalHttpEndpoints()
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
                    DisplayText = "Debezium Dashboard",
                    Endpoint = context.GetEndpoint("http"),
                }
            );
        });
debezium.WaitFor(kafka).WaitFor(sqlserver);

//  elasticsearch:
//     image: elasticsearch:8.11.0
//     container_name: elasticsearch
//     environment:
//       - discovery.type=single-node
//       - xpack.security.enabled=false
//       - ES_JAVA_OPTS=-Xms512m -Xmx512m
//     ports:
//       - "9200:9200"
//     volumes:
//       - elasticsearch_data:/usr/share/elasticsearch/data


var elasticsearch = builder.AddElasticsearch("elasticsearch")
.WithImageTag("8.11.0")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http", isProxied: false)
    .WithDataVolume("elasticsearch_data");

var userService = builder.AddProject<UserService>("user-service").WithReference(kafka, "kafka").WithReference(sqlserver, "sqlserver").WithReference(keycloak, "keycloak").WithExternalHttpEndpoints();
var searchService = builder.AddProject<SearchService>("search-service").WithReference(kafka, "kafka").WithReference(elasticsearch, "elasticsearch").WithExternalHttpEndpoints();

builder.AddProject<APIGateway>("APIGateway")
.WithReference(userService)
.WithReference(keycloak, "keycloak")
.WithReference(searchService)
.WithExternalHttpEndpoints();
builder.Build().Run();
