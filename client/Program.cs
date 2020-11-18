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
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                ShowUsage();
                return;
            }

            string serverAddress = args[0];
            string name = args[1];

            var token = await GetTokenAsync(serverAddress);
            var channel = CreateAuthenticatedChannel(serverAddress, token);

            var client = new Greeter.GreeterClient(channel);
            var reply = await client.SayHelloAsync(
                new HelloRequest {Name = name}
            );
            System.Console.WriteLine("Greeting " + reply.Message);           
        }

        private static async Task<string> GetTokenAsync(string serverAddress)
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

        private static void ShowUsage()
        {
            System.Console.WriteLine("client {server} {reply-name}");
            System.Console.WriteLine("  eg. client https://localhost:5001 Jason");
        }
    }
}
