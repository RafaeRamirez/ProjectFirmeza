FROM mcr.microsoft.com/dotnet/sdk:8.0 AS tester
WORKDIR /src

# copy solution and project files to leverage layer caching
COPY ["Firmeza.sln", "."]
COPY ["Firmeza.Api/Firmeza.Api.csproj", "Firmeza.Api/"]
COPY ["Firmeza.Web/Firmeza.Web.csproj", "Firmeza.Web/"]
COPY ["Firmeza.Tests/Firmeza.Tests.csproj", "Firmeza.Tests/"]

# restore dependencies for the whole solution
RUN dotnet restore

# copy the rest of the source code
COPY . .

# run the xUnit suite when this container executes
ENTRYPOINT ["dotnet", "test", "Firmeza.Tests/Firmeza.Tests.csproj", "-c", "Release", "--no-restore"]
