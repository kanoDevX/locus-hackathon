FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UstazAI.slnx .
COPY UstazAI.Domain/UstazAI.Domain.csproj UstazAI.Domain/
COPY UstazAI.Application/UstazAI.Application.csproj UstazAI.Application/
COPY UstazAI.Infrastructure/UstazAI.Infrastructure.csproj UstazAI.Infrastructure/
COPY UstazAI/UstazAI.csproj UstazAI/
COPY UstazAI.Tests/UstazAI.Tests.csproj UstazAI.Tests/
RUN dotnet restore UstazAI/UstazAI.csproj

COPY . .
RUN dotnet publish UstazAI/UstazAI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UstazAI.dll"]
