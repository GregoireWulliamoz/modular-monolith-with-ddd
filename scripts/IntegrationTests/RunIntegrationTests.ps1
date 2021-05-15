$srcPath = [IO.Path]::Combine($PSScriptRoot, "..", "..", "src")
$dockerComposeFile = [IO.Path]::Combine($PSScriptRoot, "docker-compose.integration.yml")
$envFilePath = [IO.Path]::Combine($PSScriptRoot, ".env")

$environmentVariables = get-content $envFilePath | ConvertFrom-StringData

docker-compose -f $dockerComposeFile build
docker-compose -f $dockerComposeFile up -d --remove-orphans

$solution = [IO.Path]::Combine($srcPath, "CompanyName.MyMeetings.sln")

$Env:ASPNETCORE_MyMeetings_IntegrationTests_ConnectionString="Server=localhost,$($environmentVariables.MSSQL_PORT);Database=$($environmentVariables.MSSQL_DATABASE);User Id=$($environmentVariables.MSSQL_USER);Password=$($environmentVariables.SA_PASSWORD);"

dotnet restore $solution
dotnet build $solution --configuration Release --no-restore

$administrationIntegrationTests = [IO.Path]::Combine($srcPath, "Modules/Administration/Tests/IntegrationTests/CompanyName.MyMeetings.Modules.Administration.IntegrationTests.csproj")
dotnet test --configuration Release --no-build --verbosity normal $administrationIntegrationTests
$paymentsIntegrationTests = [IO.Path]::Combine($srcPath, "Modules/Payments/Tests/IntegrationTests/CompanyName.MyMeetings.Modules.Payments.IntegrationTests.csproj")
dotnet test --configuration Release --no-build --verbosity normal $paymentsIntegrationTests
$userAccessIntegrationTests = [IO.Path]::Combine($srcPath, "Modules/UserAccess/Tests/IntegrationTests/CompanyNames.MyMeetings.Modules.UserAccess.IntegrationTests.csproj")
dotnet test --configuration Release --no-build --verbosity normal $userAccessIntegrationTests
$meetingsIntegrationTests = [IO.Path]::Combine($srcPath, "Modules/Meetings/Tests/IntegrationTests/CompanyName.MyMeetings.Modules.Meetings.IntegrationTests.csproj")
dotnet test --configuration Release --no-build --verbosity normal $meetingsIntegrationTests
$globalIntegrationTests = [IO.Path]::Combine($srcPath, "Tests/IntegrationTests/CompanyName.MyMeetings.IntegrationTests.csproj")
dotnet test --configuration Release --no-build --verbosity normal $globalIntegrationTests

docker-compose -f $dockerComposeFile kill