# Build with:
# dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishReadyToRun=true -p:PublishTrimmed=true -p:CrossGenDuringPublish=false -c Release -o ./output
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0
WORKDIR /app
COPY ./output/appsettings.json .
COPY ./output/cloudrun .
ENTRYPOINT ["/app/cloudrun"]
