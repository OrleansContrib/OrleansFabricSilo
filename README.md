# Orleans Service Fabric Silo
This is a sample project demonstrating [Orleans](github.com/dotnet/orleans/) running on Service Fabric with a simple calculator test.

## Instructions
1. Restore the packages. Ensure that NuGet doesn't wipe the `ClientConfiguration.xml` or `OrleansConfiguration.xml` files.
2. Start the Azure Storage Emulator - it is currently needed for Orleans to discover other nodes.
3. Debug `OrleansFabricSiloApplication` from Visual Studio.
4. Run `TestClient.exe get` from TestClient's output directory.
5. Run `TestClient.exe add 3.14` from TestClient's output directory.

Hope this helps :) Hit me up on Twitter [@ReubenBond](https://twitter.com/reubenbond) if you have questions.

You can ask Orleans-specific questions on Gitter: [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
