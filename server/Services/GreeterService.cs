using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CloudRunGrpc
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly IConfiguration _config;

        public GreeterService(ILogger<GreeterService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = $"Hello {request.Name}, I know your secret: {_config["my-secret"]}"
            });
        }
    }
}
