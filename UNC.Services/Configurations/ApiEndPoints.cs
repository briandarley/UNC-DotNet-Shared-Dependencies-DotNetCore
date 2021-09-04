using System.Collections.Generic;
using System.Linq;

namespace UNC.Services.Configurations
{
    /// <summary>
    /// Helper class to help retrieve the base address for the various WEB API addresses.
    /// EndPoint addresses will be retrieved from the configuration file upon application load/Beginning of the application life-cycle. 
    /// </summary>
    public class ApiEndPoints : IApiEndPoints
    {

        public IEnumerable<EndPointAddress> EndPointAddresses { get; }
        public ApiEndPoints(IEnumerable<EndPointAddress> endPointAddresses)
        {
            EndPointAddresses = endPointAddresses;
        }

        public EndPointAddress GetEndPointAddress(string connectionName)
        {
            if (EndPointAddresses.All(c => c.Name != connectionName)) return null;

            return EndPointAddresses.Single(c => c.Name == connectionName);
        }

    }
}
