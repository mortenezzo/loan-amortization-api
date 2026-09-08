FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/LoanAmortization.Api/LoanAmortization.Api.csproj", "src/LoanAmortization.Api/"]
COPY ["src/LoanAmortization.Application/LoanAmortization.Application.csproj", "src/LoanAmortization.Application/"]
COPY ["src/LoanAmortization.Domain/LoanAmortization.Domain.csproj", "src/LoanAmortization.Domain/"]
COPY ["src/LoanAmortization.Infrastructure/LoanAmortization.Infrastructure.csproj", "src/LoanAmortization.Infrastructure/"]
RUN dotnet restore "src/LoanAmortization.Api/LoanAmortization.Api.csproj"

COPY . .
RUN dotnet publish "src/LoanAmortization.Api/LoanAmortization.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoanAmortization.Api.dll"]
