namespace Orleans.Fabric.Silo
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Runtime.Configuration;

    /// <summary>
    ///     The service implementation.
    /// </summary>
    public class Service : IStatelessServiceInstance
    {
        /// <summary>
        ///     The service type name.
        /// </summary>
        public const string ServiceTypeName = "OrleansFabricSiloType";

        /// <summary>
        ///     The task which completes when this service does.
        /// </summary>
        private readonly TaskCompletionSource<int> stopped = new TaskCompletionSource<int>();

        /// <summary>
        ///     The cancellation token source.
        /// </summary>
        private CancellationTokenSource cancellation;

        /// <summary>
        ///     The silo.
        /// </summary>
        private OrleansFabricSilo fabricSilo;

        /// <summary>
        ///     The service initialization parameters.
        /// </summary>
        private StatelessServiceInitializationParameters parameters;

        /// <summary>
        ///     Gets the task which completes when this service shuts down.
        /// </summary>
        public Task Stopped
        {
            get
            {
                return this.stopped.Task;
            }
        }

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="initializationParameters">
        /// The initialization parameters.
        /// </param>
        public void Initialize(StatelessServiceInitializationParameters initializationParameters)
        {
            this.parameters = initializationParameters;
            this.cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// The open async.
        /// </summary>
        /// <param name="partition">
        /// The partition.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public async Task<string> OpenAsync(IStatelessServicePartition partition, CancellationToken cancellationToken)
        {
            var serviceName = this.parameters.ServiceName;
            var instanceId = this.parameters.InstanceId;
            var activation = this.parameters.CodePackageActivationContext;
            var node = await FabricRuntime.GetNodeContextAsync(TimeSpan.FromMinutes(1), cancellationToken);
            var nodeAddress = await GetNodeAddress(node.IPAddressOrFQDN);
            
            var context = await FabricRuntime.GetActivationContextAsync(TimeSpan.FromMinutes(5), CancellationToken.None);
            var serviceConfig = context.GetConfigurationPackageObject("Config").Settings;
            
            var siloEndpoint = new IPEndPoint(nodeAddress, activation.GetEndpoint("OrleansSiloEndpoint").Port);
            var proxyEndpoint = new IPEndPoint(nodeAddress, activation.GetEndpoint("OrleansProxyEndpoint").Port);

            var config = GetConfiguration();

            /*
            if (string.Equals(config.Globals.DataConnectionString, AzureStorageEmulator.UseDevelopmentStorage, StringComparison.Ordinal))
            {
                AzureStorageEmulator.Start();
            }
            */

            // Here you can read configuration from Fabric and modify the SiloConfiguration before starting the silo.
            /* 
            var storageConfig = new StorageConfiguration(serviceConfig);
            config.Globals.RegisterBootstrapProvider<SiloBootstrap>(SiloBootstrap.ProviderName);

            // Configure actor state storage.
            config.Globals.RegisterStorageProvider<AzureTableStorage>(
                "Default", 
                new Dictionary<string, string>
                {
                    { "DataConnectionString", storageConfig.ActorStateStoreConnectionString }, 
                    { "UseJsonFormat", true.ToString(CultureInfo.InvariantCulture) }
                });

            // Configure clustering.
            config.Globals.LivenessType = GlobalConfiguration.LivenessProviderType.AzureTable;
            config.Globals.DataConnectionString = storageConfig.ActorSystemStoreConnectionString;
            
            // Configure the event journal.
            // Note: Grain type and provider name are currently ignored.
            JournalProviderManager.Manager.GetProviderDelegate =
                (type, providerName) =>
                new AzureTableJournalProvider(storageConfig.JournalConnectionString, SerializationSettings.JsonConfig);
            */

            Trace.TraceInformation("Silo Configuration:\n{0}", config);
            var connectionString = config.Globals.DataConnectionString;
            try
            {
                this.fabricSilo = new OrleansFabricSilo(serviceName, instanceId, siloEndpoint, proxyEndpoint, connectionString);

                if (this.fabricSilo.Start(config))
                {
                    this.MonitorSilo(partition);
                    return this.fabricSilo.SiloEndpoint.ToString();
                }
            }
            catch (Exception exception)
            {
                this.stopped.TrySetException(exception);
                this.fabricSilo.Abort();
                throw;
            }

            partition.ReportFault(FaultType.Transient);
            var failure = new InvalidOperationException("Failed to start silo.");
            this.stopped.TrySetException(failure);
            throw failure;
        }

        /// <summary>
        /// Closes this service.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.cancellation.Cancel();
            this.fabricSilo.Stop();
            return this.fabricSilo.Stopped;
        }

        /// <summary>
        ///     Aborts this service.
        /// </summary>
        public void Abort()
        {
            this.cancellation.Cancel();
            this.fabricSilo.Stop();
        }

        /// <summary>
        ///     Returns the path of the executing assembly.
        /// </summary>
        /// <returns>
        ///     The path of the executing assembly.
        /// </returns>
        private static string GetAssemblyPath()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }

        /// <summary>
        /// Returns the host's network address.
        /// </summary>
        /// <param name="host">
        /// The host.
        /// </param>
        /// <returns>
        /// The host's network address.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Unable to determine the host's network address.
        /// </exception>
        private static async Task<IPAddress> GetNodeAddress(string host)
        {
            var nodeAddresses = await Dns.GetHostAddressesAsync(host);

            var nodeAddressV4 =
                nodeAddresses.FirstOrDefault(_ => _.AddressFamily == AddressFamily.InterNetwork && !IsLinkLocal(_));
            var nodeAddressV6 =
                nodeAddresses.FirstOrDefault(_ => _.AddressFamily == AddressFamily.InterNetworkV6 && !IsLinkLocal(_));
            var nodeAddress = nodeAddressV4 ?? nodeAddressV6;
            if (nodeAddress == null)
            {
                throw new InvalidOperationException("Could not determine own network address.");
            }

            return nodeAddress;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <paramref name="address"/> is a local-only address.
        /// </summary>
        /// <param name="address">
        /// The address.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the provided <paramref name="address"/> is a local-only address.
        /// </returns>
        private static bool IsLinkLocal(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return address.IsIPv6LinkLocal;
            }

            // 169.254.0.0/16
            var addrBytes = address.GetAddressBytes();
            return addrBytes[0] == 0xA9 && addrBytes[1] == 0xFE;
        }

        /// <summary>
        ///     Returns the silo configuration.
        /// </summary>
        /// <returns>
        ///     The silo configuration.
        /// </returns>
        private static ClusterConfiguration GetConfiguration()
        {
            var config = new ClusterConfiguration();
            config.LoadFromFile(Path.Combine(GetAssemblyPath(), "SiloConfiguration.xml"));
            return config;
        }

        /// <summary>
        /// Monitors the current silo, reporting a fault to the specified <paramref name="partition"/> if it faults.
        /// </summary>
        /// <param name="partition">
        /// The partition.
        /// </param>
        private void MonitorSilo(IServicePartition partition)
        {
            this.fabricSilo.Stopped.ContinueWith(
                _ =>
                    {
                        if (_.IsFaulted)
                        {
                            partition.ReportFault(FaultType.Transient);
                            this.stopped.TrySetException(
                                _.Exception ?? new Exception(typeof(OrleansFabricSilo).Name + " faulted."));
                        }
                        else if (_.IsCanceled)
                        {
                            this.stopped.TrySetCanceled();
                        }
                        else if (_.IsCompleted)
                        {
                            this.stopped.TrySetResult(0);
                        }
                    }, 
                TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}