using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc;
using Grpc.Net.Client;
using Grpc.Core;
using Grpc.Auth;
using Google.Apis.Auth.OAuth2;
using cloudrun;

namespace client
{
    class Program
    {
        // const string serverAddress = "http://localhost:5000";
        // const string serverAddress = "http://34.122.133.56";
        const string serverAddress = "https://mygrpc-62b5pp6dlq-uc.a.run.app";

        static async Task Main(string[] args)
        {
            // // see: https://grpc.github.io/grpc/csharp/api/Grpc.Auth.GoogleGrpcCredentials.html

            var token = await GetTokenAsync();
            var channel = CreateAuthenticatedChannel(serverAddress, token);

            var client = new Greeter.GreeterClient(channel);
            var reply = await client.SayHelloAsync(
                new HelloRequest {Name = "Jason"}
            );
            System.Console.WriteLine("Greeting " + reply.Message);           
        }

        private static async Task<string> GetTokenAsync()
        {
            GoogleCredential creds = GoogleCredential.GetApplicationDefault();
            var oidcToken = await creds.GetOidcTokenAsync(
                OidcTokenOptions.FromTargetAudience(serverAddress)
            );
            var token = await oidcToken.GetAccessTokenAsync();
            return token;
        }

        private static GrpcChannel CreateAuthenticatedChannel(string address, string token)
        {
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
    }
}
