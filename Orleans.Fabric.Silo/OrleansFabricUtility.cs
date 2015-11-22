namespace Microsoft.Orleans.ServiceFabric.Silo
{
    using System;
    using System.Fabric;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Utilities for working with silos hosted in Windows Fabric.
    /// </summary>
    internal static class OrleansFabricUtility
    {
        /// <summary>
        /// Returns the deployment id for the provided service and partition.
        /// </summary>
        /// <param name="serviceName">
        /// The service.
        /// </param>
        /// <param name="partition">
        /// The partition.
        /// </param>
        /// <returns>
        /// The deployment id for the provided service and partition.
        /// </returns>
        public static string GetDeploymentId(Uri serviceName, ServicePartitionInformation partition = null)
        {
            var partitionId = partition != null ? GetPartitionKey(partition) : string.Empty;
            var serviceId = Regex.Replace(serviceName.PathAndQuery.Trim('/'), "[^a-zA-Z0-9_]", "_");
            return string.IsNullOrWhiteSpace(partitionId) ? serviceId : $"{serviceId}@{partitionId}";
        }

        /// <summary>
        /// Returns the partition key for the provided <paramref name="partition"/>.
        /// </summary>
        /// <param name="partition">
        /// The partition.
        /// </param>
        /// <returns>
        /// The partition key for the provided <paramref name="partition"/>.
        /// </returns>
        private static string GetPartitionKey(ServicePartitionInformation partition)
        {
            switch (partition.Kind)
            {
                case ServicePartitionKind.Int64Range:
                    {
                        var intPartition = (Int64RangePartitionInformation)partition;
                        return $"{intPartition.LowKey:X}_{intPartition.HighKey:X}";
                    }

                case ServicePartitionKind.Named:
                    return ((NamedPartitionInformation)partition).Name;
                case ServicePartitionKind.Singleton:
                    return string.Empty;
                default:
                    throw new ArgumentOutOfRangeException(nameof(partition));
            }
        }
    }
}