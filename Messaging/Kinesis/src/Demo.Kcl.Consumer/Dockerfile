FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS build
WORKDIR /build

# copy csproj and restore as distinct layers
COPY *.sln ./

# Copy the main source project files
COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done

# Restore to cache the layers
RUN dotnet restore

# Copy all the source code and build
COPY ./src ./src

RUN dotnet build -c Release --no-restore

FROM build AS publish
RUN dotnet publish ./src/Demo.Kcl.Consumer/Demo.Kcl.Consumer.csproj -c Release -o /app/consumer --no-restore --no-build
RUN dotnet publish ./src/Bootstrap/Bootstrap.csproj -c Release -o /app/bootstrap --no-restore --no-build

# application image
FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine
RUN apk --no-cache add openjdk11 --repository=http://dl-cdn.alpinelinux.org/alpine/edge/community
WORKDIR /app
COPY --from=publish /app ./
WORKDIR /app/bootstrap
RUN dotnet Bootstrap.dll --properties /app/consumer/kcl.properties

ENTRYPOINT ["dotnet", "Bootstrap.dll", "--properties", "/app/consumer/kcl.properties", "--execute"]