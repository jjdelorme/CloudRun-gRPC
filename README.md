# Authenticated gRPC with C# on CloudRun 

This sample code demonstrates how to create an **authenticated** .NET Core 5 C# gRPC server and client.  Using TLS for an authenticated service is what makes this different from the [Getting started with .NET](https://cloud.google.com/dotnet/docs/getting-started) sample already available from Google and is based on the [Microsoft tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-5.0&tabs=visual-studio-code).

## Authentication
The server is deployed to Google CloudRun and uses *"Require authentication"* with TLS.  CloudRun handles TLS termination so it is important that the server implementation listens on port 80 **without TLS**.

### Prerequisites
1. Clone this repo locally
1. Create a project and enable the CloudRun API from [here](https://console.cloud.google.com/flows/enableapi?apiid=run.googleapis.com)
1. See the docs on [.NET development environment](https://cloud.google.com/dotnet/docs/setup) instructions to setup a service account, download a service account key and create the ```GOOGLE_APPLICATION_CREDENTIALS``` environment variable that will be used by the client to connect
1. Set the environment variable ```GOOGLE_PROJECT_ID``` to the project id you just created
1. Follow [these steps](https://cloud.google.com/container-registry/docs/quickstart) to enable the container registry and configure docker authentication so that you can push an image

### Deploy the server

(Unique to SecretManager Branch)
1. Run docker build from root dev directory as such to have ./grpc and ./SecretConfigurationProvider available.
    ```bash 
    cd ~\dev # Your development directory
    docker build --force-rm --no-cache -t gcr.io/$GOOGLE_PROJECT_ID/mygrpc:v1 -f ./grpc/Dockerfile .
    ```
1. Add the ```Secret Manager Secret Accessor``` role to the Service Account for the Cloud Run app.

(If you're not using the SecretManager branch)
1. Build the server container by executing
    ```bash 
    docker build --force-rm --no-cache -t gcr.io/$GOOGLE_PROJECT_ID/mygrpc:v1 -f Dockerfile .
    ```
1. Push the container to your registry
    ```bash 
    docker push gcr.io/$GOOGLE_PROJECT_ID/mygrpc:v1
    ```
1. Create the new CloudRun service
    ```bash
    gcloud run deploy mygrpc --region us-central1 --platform managed \
        --image gcr.io/$GOOGLE_PROJECT_ID/mygrpc:v1 --port 80 --no-allow-unauthenticated
    
    ...

    Service URL: https://mygrpc-xxxxxxxxxx.a.run.app
    ```
1. Take note of the **Service URL** that was created above

    * Alternatively you can deploy using the console and choose **Require authentication**

        ![Require authentication](cloudrun-auth.png)
1. Ensure that your service account has the ```roles/run.invoker``` role for your CloudRun service
    ```bash
    gcloud run services add-iam-policy-binding mygrpc \
        --member="[YOUR_GOOGLE_SERVICE_ACCOUNT]" \
        --role="roles/run.invoker"
    ```

### Run the client locally

The client requires 2 parameters:
* Service URL (from above)
* The reply name you want the server to return with
```bash
# Change into the client directory
cd ./client

# Execute the client app
dotnet run -- https://mygrpc-xxxxxxxxxx.a.run.app Jason

# You should see this reply:
Greeting Hello Jason
```

## How the client authenticates to CloudRun
The purpose for creating yet another example of gRPC with C# here is to help document how to use TLS and allow CloudRun to authenticate your client request.  

The magic of this is captured in this ```Program.cs``` snippet where a bearer token is obtained from the default google credentials and appended to the metadata (headers) of each request.

```c#
        private static async Task<GrpcChannel> CreateChannelAsync(string address)
        {
            // only create authenticated channel for https
            if (address.StartsWith("http:"))
                return GrpcChannel.ForAddress(address);

            var token = await GetTokenAsync(address);

            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                if (!string.IsNullOrEmpty(token))
                {
                    metadata.Add("Authorization", $"Bearer {token}");
                }
                return Task.CompletedTask;
            });

            // SslCredentials is used here because this channel is using TLS.
            // CallCredentials can't be used with ChannelCredentials.Insecure on non-TLS channels.
            var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
            return channel;
        } 
```
## .NET Core Optimizations

Because CloudRun supports scale to 0, startup time for a container can be important.  .NET Core offers some great advantages that allow you to minimize container size, which can benefit startup time significantly.  A good blog post on this is [here](https://www.pragmacoders.net/how-to-get-a-net-5-docker-image-below-40-mb/).  For this project, the container is **~50.7mb**.  The application is deployed as a self-contained executable on Alpine linux.

This is achieved by setting these properties in the ```server.csproj```:

~~~xml
  <PropertyGroup>  
    ...
    <PublishSingleFile>True</PublishSingleFile>
    <PublishTrimmed>True</PublishTrimmed>
    ...
  </PropertyGroup>
~~~

And when building in the docker file:
~~~dockerfile
# Setting the RID to Alpine and self-contained
RUN dotnet publish -r linux-musl-x64 --self-contained true -c Release -o /deploy
...
# Base the runtime container on Alpine:
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine-amd64 AS runtime
~~~
