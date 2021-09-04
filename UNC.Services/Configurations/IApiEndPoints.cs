using System.Collections.Generic;

namespace UNC.Services.Configurations
{
    public interface IApiEndPoints
    {
        IEnumerable<EndPointAddress> EndPointAddresses { get; }
        EndPointAddress GetEndPointAddress(string connectionName);
    }
}
