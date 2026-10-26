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


## 4. Deploy your .NET 10 application with the sidecar
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



# Project structure
- `src/Spim.Domain`: domain entities and business rules.
- `src/Spim.Application`: application services, DTOs, and persistence abstractions.
- `src/Spim.Persistence`: Dapr state-store implementations.
- `src/Spim.Infrastructure`: dependency registration and external-service wiring.
- `src/Spim.Web`: ASP.NET Core API, authentication, health checks, OpenAPI, and Scalar.

# Running locally
Build the .NET 10 solution:
`dotnet build Spim.slnx`

Run the Web API:
`dotnet run --project src/Spim.Web/Spim.Web.csproj --launch-profile Spim.Web`

Open Scalar at `http://localhost:5157/scalar/v1`. The OpenAPI document is at `http://localhost:5157/openapi/v1.json`; readiness is at `http://localhost:5157/health/ready`.

The generated forecast route works without Dapr: `GET http://localhost:5157/WeatherForecast`.

To test Dapr state-store routes, initialize Dapr once with `dapr init`, then start the API with the local components:
`dapr run --app-id spim --app-port 5157 --dapr-http-port 3500 --resources-path ./components -- dotnet run --project src/Spim.Web/Spim.Web.csproj --launch-profile Spim.Web`

The local `statestore` component expects Redis at `localhost:6379`. Dapr's local initialization normally provides it. The authenticated token endpoint additionally requires a reachable Keycloak instance.


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