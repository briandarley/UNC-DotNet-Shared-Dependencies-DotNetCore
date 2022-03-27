using Serilog;
using UNC.Services;

namespace UNC.Cryptography
{
    public class PublicPrivateKeyFactoryService:ServiceBase
    {
        private readonly ILogger _logger;

        public PublicPrivateKeyFactoryService(ILogger logger) : base(logger)
        {
            _logger = logger;
        }

        public PublicPrivateKeyService GetPublicPrivateKeyService(int dwKeySize)
        {
            var service = new PublicPrivateKeyService(_logger, dwKeySize);

            return service;
        }

    }
}
