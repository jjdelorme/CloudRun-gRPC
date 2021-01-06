FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source/server
COPY ./grpc/server /source/server
COPY ./SecretConfigurationProvider/ /SecretConfigurationProvider
COPY ./grpc/protos /source/protos
RUN dotnet publish -r linux-musl-x64 --self-contained true -c Release -o /deploy

FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine-amd64 AS runtime
# gRPC issue: https://github.com/grpc/grpc/issues/21446
RUN echo 'http://dl-cdn.alpinelinux.org/alpine/v3.8/main' >> /etc/apk/repositories && apk update --no-cache && apk add --no-cache bash libc6-compat=1.1.19-r11
WORKDIR /app
COPY --from=build /deploy/appsettings.json .
COPY --from=build /deploy/ .
ENTRYPOINT ["/app/server"]
