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
            // var oidcOptions = OidcTokenOptions.FromTargetAudience(serverAddress);

            // // see: https://grpc.github.io/grpc/csharp/api/Grpc.Auth.GoogleGrpcCredentials.html

            GoogleCredential creds = GoogleCredential.GetApplicationDefault();
            var oidcToken = await creds.GetOidcTokenAsync(
                OidcTokenOptions.FromTargetAudience(serverAddress)
            );
            var bearerToken = await oidcToken.GetAccessTokenAsync();
            var headers = new Metadata();
            headers.Add("Authorization", $"Bearer {bearerToken}");

            var channel = GrpcChannel.ForAddress(serverAddress);
            var client = new Greeter.GreeterClient(channel);
            var reply = await client.SayHelloAsync(
                new HelloRequest {Name = "Jason"}, headers 
            );
            System.Console.WriteLine("Greeting " + reply.Message);           
        }
    }
}
