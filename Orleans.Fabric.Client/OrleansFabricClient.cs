namespace Microsoft.Orleans.ServiceFabric.Client
{
    using System;
    using System.Fabric;

    using global::Orleans;
    using global::Orleans.Runtime.Configuration;

    /// <summary>
    ///     Orleans client for silos hosted in Windows Fabric.
    /// </summary>
    public class OrleansFabricClient
    {
        /// <summary>
        /// Generate a Orleans ServiceFabric Deployment id.
        /// </summary>
        /// <param name="serviceName">
        /// The Windows Fabric service name.
        /// </param>
        /// <param name="partition">
        /// The partition, or <see langword="null"/> for a singleton partition.
        /// </param>
        /// <returns>A string representing the Orleans Deployment id</returns>
        public static string CreateDeploymentId(Uri serviceName, ServicePartitionInformation partition = null) =>
            OrleansFabricUtility.GetDeploymentId(serviceName, partition);

        /// <summary>
        /// Initializes the silo client.
        /// </summary>
        /// <param name="serviceName">
        /// The Windows Fabric service name.
        /// </param>
        /// <param name="config">
        /// The configuration, or <see langword="null"/> to load from the current directory.
        /// </param>
        public static void Initialize(ClientConfiguration config = null)
        {
            if (config == null)
                config = ClientConfiguration.StandardLoad();

            GrainClient.Initialize(config);
        }
    }
}