#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM microsoft/aspnetcore:2.0-stretch AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-stretch AS build
WORKDIR /src
COPY ["CustomersAPIServices/CustomersAPIServices.csproj", "CustomersAPIServices/"]
RUN dotnet restore "CustomersAPIServices/CustomersAPIServices.csproj"
COPY . .
WORKDIR "/src/CustomersAPIServices"
RUN dotnet build "CustomersAPIServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomersAPIServices.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CustomersAPIServices.dll"]