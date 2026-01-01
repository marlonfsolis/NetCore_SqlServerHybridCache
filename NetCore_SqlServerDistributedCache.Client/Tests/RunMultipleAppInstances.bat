start cmd /k "dotnet run --urls http://localhost:5001 --project ../" timeout /t 2
start cmd /k "dotnet run --urls http://localhost:5002 --project ../" timeout /t 2
start cmd /k "dotnet run --urls http://localhost:5003 --project ../" timeout /t 2
start cmd /k "dotnet run --urls http://localhost:9000 --project ../../NetCore_SqlServerDistributedCache.ReverseProxy/" timeout /t 2