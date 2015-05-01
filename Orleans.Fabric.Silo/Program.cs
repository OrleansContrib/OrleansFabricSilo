namespace Orleans.Fabric.Silo
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     The program entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The program entry point.
        /// </summary>
        /// <param name="args">
        /// Command-line arguments.
        /// </param>
        public static void Main(string[] args)
        {
            Run(CancellationToken.None).Wait();
        }

        /// <summary>
        /// Runs the services and waits for termination.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the work performed.
        /// </returns>
        private static async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ServiceFactory();
                Console.WriteLine("Initializing runtime.");
                var timeout = TimeSpan.FromMinutes(1);
                using (var fabric = await FabricRuntime.CreateAsync(timeout, cancellationToken))
                {
                    Console.WriteLine("Registering services.");
                    await
                        fabric.RegisterStatelessServiceFactoryAsync(
                            Service.ServiceTypeName,
                            factory,
                            timeout,
                            cancellationToken);

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(OrleansFabricSilo).Name);
                    Console.WriteLine("Services running.");
                    await Task.WhenAny(factory.Stopped, cancellationToken.WhenCancelled());
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e);
                throw;
            }
            finally
            {
                Console.WriteLine("Terminating.");
            }
        }

        /// <summary>
        /// Returns a <see cref="Task"/> which completes when the provided <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which completes when the provided <paramref name="cancellationToken"/> is cancelled.
        /// </returns>
        private static Task WhenCancelled(this CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<int>();
            cancellationToken.Register(_ => ((TaskCompletionSource<int>)_).TrySetResult(0), completion);
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetResult(0);
            }

            return completion.Task;
        }
    }
}