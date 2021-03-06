FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source/server
COPY ./server /source/server
COPY ./protos /source/protos
RUN dotnet publish -r linux-musl-x64 --self-contained true -c Release -o /deploy

FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine-amd64 AS runtime
WORKDIR /app
COPY --from=build /deploy/appsettings.json .
COPY --from=build /deploy/server .
ENTRYPOINT ["/app/server"]
