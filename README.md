# Orleans Service Fabric Silo
This is a simple library which allows [Orleans](github.com/dotnet/orleans/) to be hosted on Service Fabric.

## Instructions
1. Create a stateless service to host your actors.
2. Alter the `CreateServiceInstanceListeners` method to construct an `OrleansCommunicationListener`, like so:
```C#
protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
{
    var silo =
        new ServiceInstanceListener(
            parameters =>
            new OrleansCommunicationListener(parameters, this.GetClusterConfiguration(), this.ServicePartition));
    return new[] { silo };
}
```

Then consume it in the client, initializing Orleans like so:
```C#
OrleansFabricClient.Initialize(new Uri("fabric:/CalculatorApp/CalculatorService"), this.GetConfiguration());
```
Replace `fabric:/CalculatorApp/CalculatorService` with the Service Fabric URI of the service hosts Orleans


## Sample Project Instructions
1. Start the Azure Storage Emulator - it is currently needed for Orleans to discover other nodes.
2. Debug `CalculatorApp` from Visual Studio.
3. Run `TestClient.exe get` from TestClient's output directory.
4. Run `TestClient.exe add 3.14` from TestClient's output directory.

Hope this helps :) Hit me up on [@ReubenBond](https://twitter.com/reubenbond) if you have questions.

You can ask Orleans-specific questions on Gitter: [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
