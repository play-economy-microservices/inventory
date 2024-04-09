# play.inventory
Play Economy Inventory microservice

## Add the GitHub package source
```powershell
$version="1.0.1"
$owner="play-economy-microservices"
$gh_pat="[PAT HERE]"

dotnet pack src/Play.Inventory.Contracts/ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/play.inventory -o ../packages

dotnet nuget push ../packages/Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

## Build the docker image
```powershell
$env:GH_OWNER="play-economy-microservices"
$env:GH_PAT="[PAT HERE]"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t play.inventory:$version .
```

## Run the docker image
```powershell
docker run -it --rm -p 5004:5004 --name inventory -e MongoDBSettings__Host=mongo -e RabbitMQSettings__Host=rabbitmq  --network playinfra_default play.inventory:$version
```