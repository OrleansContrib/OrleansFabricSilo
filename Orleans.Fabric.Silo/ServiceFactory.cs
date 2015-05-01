namespace Orleans.Fabric.Silo
{
    using System;
    using System.Fabric;
    using System.Threading.Tasks;

    /// <summary>
    ///     The service factory.
    /// </summary>
    public class ServiceFactory : IStatelessServiceFactory
    {
        /// <summary>
        ///     The completion task.
        /// </summary>
        private readonly TaskCompletionSource<int> stopped = new TaskCompletionSource<int>();

        /// <summary>
        ///     Gets the task which completes when any of the created services shut down.
        /// </summary>
        public Task Stopped
        {
            get
            {
                return this.stopped.Task;
            }
        }

        /// <summary>
        /// Returns a new instance of the <see cref="Service"/>.
        /// </summary>
        /// <param name="serviceTypeName">
        /// The service type name.
        /// </param>
        /// <param name="serviceName">
        /// The service name.
        /// </param>
        /// <param name="initializationData">
        /// The initialization data.
        /// </param>
        /// <param name="partitionId">
        /// The partition id.
        /// </param>
        /// <param name="instanceId">
        /// The instance id.
        /// </param>
        /// <returns>
        /// The <see cref="IStatelessServiceInstance"/>.
        /// </returns>
        public IStatelessServiceInstance CreateInstance(
            string serviceTypeName, 
            Uri serviceName, 
            byte[] initializationData, 
            Guid partitionId, 
            long instanceId)
        {
            var service = new Service();
            service.Stopped.ContinueWith(
                _ =>
                    {
                        if (_.IsCompleted)
                        {
                            this.stopped.TrySetResult(0);
                        }
                        else if (_.IsCanceled)
                        {
                            this.stopped.TrySetCanceled();
                        }
                        else if (_.IsFaulted && _.Exception != null)
                        {
                            this.stopped.TrySetException(_.Exception);
                        }
                    }, 
                TaskContinuationOptions.ExecuteSynchronously);
            return service;
        }
    }
}