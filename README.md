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