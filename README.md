# Orleans Service Fabric Silo
This is a simple library which allows [Orleans](https://github.com/dotnet/orleans/) to be hosted on Service Fabric.

## Instructions
* Create a stateless service to host your actors.
* Install the Orleans Service Fabric Silo package:
```PS
PM> Install-Package Microsoft.Orleans.ServiceFabric.Silo -Pre
```
* Alter the `CreateServiceInstanceListeners` method to construct an `OrleansCommunicationListener`, like so:
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
* Install the Orleans Service Fabric Client package:
```PS
PM> Install-Package Microsoft.Orleans.ServiceFabric.Client -Pre
```
Then consume it in the client, initializing Orleans like so:
```C#
OrleansFabricClient.Initialize(new Uri("fabric:/CalculatorApp/CalculatorService"), this.GetConfiguration());
```
Replace `fabric:/CalculatorApp/CalculatorService` with the Service Fabric URI of the service created earlier.

## Sample Project Instructions
1. Start the Azure Storage Emulator - it is currently needed for Orleans to discover other nodes.
2. Debug `CalculatorApp` from Visual Studio.
3. Run `TestClient.exe get` from TestClient's output directory.
4. Run `TestClient.exe add 3.14` from TestClient's output directory.

Hope this helps :) Hit me up on [@ReubenBond](https://twitter.com/reubenbond) if you have questions.

Join us! Orleans is a warm, welcoming community & we're on Gitter: [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
