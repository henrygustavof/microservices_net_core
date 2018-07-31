FROM microsoft/aspnetcore-build:2.0.0 AS build

WORKDIR /code

COPY . .

RUN dotnet restore

RUN dotnet publish --output /out/ --configuration Release 

FROM microsoft/aspnetcore:2.0.0

COPY --from=build /out /app/

WORKDIR  /app

ENTRYPOINT ["dotnet","ProductCatalogApi.dll"]


 