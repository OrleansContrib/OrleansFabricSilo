namespace Orleans.Fabric.Silo
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;

    using Orleans.Fabric.Common;
    using Orleans.Runtime;
    using Orleans.Runtime.Configuration;
    using Orleans.Runtime.Host;

    /// <summary>
    ///     Wrapper class for an Orleans silo running in the current host process.
    /// </summary>
    public class OrleansFabricSilo
    {
        /// <summary>
        ///     The Azure table service connection string
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        ///     The deployment id.
        /// </summary>
        private readonly string deploymentId;

        /// <summary>
        ///     The name of the silo.
        /// </summary>
        private readonly string siloName;

        /// <summary>
        ///     The task which completes when the silo stops running.
        /// </summary>
        private readonly TaskCompletionSource<int> stopped;

        /// <summary>
        ///     The host.
        /// </summary>
        private SiloHost host;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrleansFabricSilo"/> class.
        /// </summary>
        /// <param name="serviceName">
        /// The service name.
        /// </param>
        /// <param name="instanceId">
        /// The instance id.
        /// </param>
        /// <param name="siloEndpoint">
        /// The silo endpoint.
        /// </param>
        /// <param name="proxyEndpoint">
        /// The proxy endpoint.
        /// </param>
        /// <param name="connectionString">
        /// The Azure table service connection string.
        /// </param>
        public OrleansFabricSilo(
            Uri serviceName, 
            long instanceId, 
            IPEndPoint siloEndpoint, 
            IPEndPoint proxyEndpoint, 
            string connectionString)
        {
            this.stopped = new TaskCompletionSource<int>();
            this.SiloEndpoint = siloEndpoint;
            this.ProxyEndpoint = proxyEndpoint;
            this.connectionString = connectionString;
            this.deploymentId = OrleansFabricUtility.GetDeploymentId(serviceName);
            this.siloName = this.deploymentId + "_" + instanceId.ToString("X");
        }

        /// <summary>
        ///     Gets the silo endpoint.
        /// </summary>
        public IPEndPoint SiloEndpoint { get; private set; }

        /// <summary>
        ///     Gets the proxy endpoint.
        /// </summary>
        public IPEndPoint ProxyEndpoint { get; private set; }

        /// <summary>
        ///     Gets the task which completes when the silo stops running.
        /// </summary>
        public Task Stopped
        {
            get
            {
                return this.stopped.Task;
            }
        }

        /// <summary>
        /// Starts the silo.
        /// </summary>
        /// <param name="config">
        /// The config.
        /// </param>
        /// <returns>
        /// Whether or not initialization was successful.
        /// </returns>
        /// <exception cref="OrleansException">
        /// An exception occurred starting the silo.
        /// </exception>
        public bool Start(ClusterConfiguration config)
        {
            try
            {
                Trace.TraceInformation(
                    "Starting silo. Name: {0}, DeploymentId: {1}, Primary Endpoint: {2}", 
                    this.siloName, 
                    this.deploymentId, 
                    this.SiloEndpoint);

                // Configure this Orleans silo instance
                if (config == null)
                {
                    Trace.TraceInformation("Loading configuration from default locations.");
                    this.host = new SiloHost(this.siloName);
                    this.host.LoadOrleansConfig();
                }
                else
                {
                    Trace.TraceInformation("Using provided configuration.");
                    this.host = new SiloHost(this.siloName, config);
                }

                // Configure the silo for the current environment.
                var generation = SiloAddress.AllocateNewGeneration();
                this.host.SetSiloType(Silo.SiloType.Secondary);
                this.host.SetSiloLivenessType(GlobalConfiguration.LivenessProviderType.AzureTable);
                this.host.SetReminderServiceType(GlobalConfiguration.ReminderServiceProviderType.AzureTable);
                this.host.SetDeploymentId(this.deploymentId, this.connectionString);
                this.host.SetSiloEndpoint(this.SiloEndpoint, generation);
                this.host.SetProxyEndpoint(this.ProxyEndpoint);

                this.host.InitializeOrleansSilo();
                Trace.TraceInformation(
                    "Successfully initialized Orleans silo '{0}' as a {1} node.", 
                    this.host.Name, 
                    this.host.Type);
                Trace.TraceInformation("Starting Orleans silo '{0}' as a {1} node.", this.host.Name, this.host.Type);

                var ok = this.host.StartOrleansSilo();
                if (ok)
                {
                    Trace.TraceInformation(
                        "Successfully started Orleans silo '{0}' as a {1} node.", 
                        this.host.Name, 
                        this.host.Type);
                }
                else
                {
                    Trace.TraceInformation(
                        "Failed to start Orleans silo '{0}' as a {1} node.", 
                        this.host.Name, 
                        this.host.Type);
                }

                this.MonitorSilo();
                return ok;
            }
            catch (Exception e)
            {
                this.stopped.TrySetException(e);
                this.Abort();

                throw;
            }
        }

        /// <summary>
        ///     Stop this Orleans silo executing.
        /// </summary>
        public void Stop()
        {
            Trace.TraceInformation("Stopping {0}", this.GetType().FullName);
            var silo = this.host;
            if (silo != null)
            {
                try
                {
                    if (this.host.IsStarted)
                    {
                        this.host.StopOrleansSilo();
                    }

                    this.host.UnInitializeOrleansSilo();
                }
                catch (Exception exception)
                {
                    Trace.TraceWarning("Exception stopping Orleans silo: {0}", exception);
                }

                this.stopped.TrySetResult(0);
            }

            Trace.TraceInformation("Orleans silo '{0}' shutdown.", silo != null ? silo.Name : "null");
        }

        /// <summary>
        ///     The abort.
        /// </summary>
        public void Abort()
        {
            var silo = this.host;
            if (silo != null)
            {
                silo.UnInitializeOrleansSilo();
                silo.Dispose();
                this.host = null;
            }
        }

        /// <summary>
        ///     Monitors the silo.
        /// </summary>
        private void MonitorSilo()
        {
            Task.Run(
                () =>
                    {
                        var silo = this.host;
                        try
                        {
                            if (silo != null && silo.IsStarted)
                            {
                                Trace.TraceInformation("Monitoring silo '{0}' for shutdown event.", silo.Name);
                                this.host.WaitForOrleansSiloShutdown();
                                this.stopped.TrySetResult(0);
                            }
                            else
                            {
                                throw new ApplicationException(
                                    string.Format(
                                        "Silo '{0}' failed to start correctly - aborting", 
                                        silo != null ? silo.Name : "null"));
                            }
                        }
                        catch (Exception e)
                        {
                            this.stopped.TrySetException(e);
                        }
                    });
        }
    }
}