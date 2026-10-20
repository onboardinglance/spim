# Spim – Clean Architecture (.NET 10)

| Layer | Project | Depends on | Responsibility |
|---|---|---|---|
| Domain | `src/Spim.Domain` | nothing | Entities and business rules (pure C#) |
| Application | `src/Spim.Application` | Domain | CQRS commands/queries (MediatR), validators, AutoMapper profiles, abstractions (`IWeatherForecastRepository`, `IUnitOfWork`, `IEmailService`, `ICurrentUserService`) |
| Infrastructure | `src/Spim.Infrastructure` | Application | Internal/external services (e.g. SMTP email) |
| Persistence | `src/Spim.Persistence` | Application | EF Core `DbContext`, repositories, configurations, interceptors, migrations |
| Web | `src/Spim.Web` | all | Composition root, controllers (HTTP → MediatR), Keycloak JWT auth, Scalar, Serilog |

DI uses Autofac (`InfrastructureModule`, `PersistenceModule`, `WebModule`) plus `Add*Services` extensions for framework registrations.

## Keycloak setup (realm `demo`)
1. Create a **public** client `spim-scalar` with *Standard flow* on, PKCE `S256`, Valid redirect URIs `http://localhost:5157/scalar/*` (and `http://localhost:8080/scalar/*` for k8s), Web origins `+`.
2. Add an *Audience* mapper to the client (or a client scope) with included custom audience `spim-api` so access tokens carry `aud: spim-api`.
3. Open `/scalar`, click *Authorize*, sign in, then call the API.

## Running locally
Requires PostgreSQL (`ConnectionStrings:DefaultConnection` in `src/Spim.Web/appsettings.Development.json`) and Keycloak on `http://localhost:8081`.

`dotnet run --project src/Spim.Web`

In Development the schema is created/migrated and seeded on startup.

## EF Core migrations
`dotnet ef migrations add InitialCreate --project src/Spim.Persistence --startup-project src/Spim.Persistence --output-dir Migrations`

## Licensing
AutoMapper (v15+) and MediatR (v13+) are commercially licensed by Lucky Penny Software; set `LuckyPenny:LicenseKey` (e.g. env var `LuckyPenny__LicenseKey`).

# Deploying to kubernetes
## 0. run dapr init -k to init the dapr environment (just need to run 1 time)
`dapr init -k`

## 1. Deploy to docker as image
`docker build -t dapr-demo:latest .`

## 2 Deploy Keycloak for OIDC
`kubectl apply -f keycloak.yaml`

## 3. Apply the state store configuration (Redis)
Create the secret for Redis password:
`kubectl create secret generic redis-secret --from-literal=redis-password="password"`

`kubectl apply -f redis-statestore.yaml`
create the secret so that it is not stored in plain
`kubectl create secret generic redis-secret --from-literal=redis-password=password`

`kubectl apply -f postgresql-statestore.yaml`
create the secret so that it is stored in kubernetes and not in plain
`kubectl create secret generic postgres-secret --from-literal=connectionString="host=postgres-postgresql.infra.svc.cluster.local port=5432 user=dapr password=StrongPassword123 dbname=daprdb sslmode=disable"`


## 4. Create the application database secret and deploy your .NET 10 application
`kubectl create secret generic spim-db-secret --from-literal=connectionString="Host=postgres-postgresql.infra.svc.cluster.local;Port=5432;Database=spim;Username=dapr;Password=<password>"`

`kubectl apply -f deployment.yaml`

## 5. Expose your kubernetes pods by port forwarding it 
`kubectl port-forward svc/dapr-demo-service 8080:80`

## 6. Expose your keycloak kubernetes pods by port forwarding it, connect using 8081 
`kubectl port-forward svc/keycloak-service 8081:8080`

# Install via helm chart
`helm install dapr dapr-helm-chart.tgz \
  --namespace dapr-system \
  --set global.registry=my-local-registry.com/dapr \
  --set global.tag=latest`



# Running with the Dapr sidecar
`dapr run --app-id dapr-demo --app-port 5157 --dapr-http-port 3500 --resources-path ./components -- dotnet run --project src/Spim.Web` 


## kubectl commands ##

## Gets pods from namespace of infra
`kubectl get pods -n infra`

## rollout restart 
`kubectl rollout restart deployment dapr-demo-deployment -n default`

## Describe the deployment 
`kubectl describe deployment dapr-demo-deployment -n default`

## Get the yaml file from the component called statestore 
`kubectl get component statestore -o yaml`

## Gets the log files for app dapr-demo and check for statestore
`kubectl logs -l app=dapr-demo -c daprd | grep statestore`

## Gets into the pod redis-master-0 and use the cli, with the password as argument
`kubectl exec -it redis-master-0 -- redis-cli -a password`

## Gets into the pod of postgres in namespace of infra and run psql command
`kubectl exec -it postgres-postgresql-0 -n infra -- psql -U dapr -d daprdb`