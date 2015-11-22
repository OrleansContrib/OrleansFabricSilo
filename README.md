# Orleans Service Fabric Silo
This is a sample project demonstrating [Orleans](github.com/dotnet/orleans/) running on Service Fabric with a simple calculator test.

## Instructions
1. Start the Azure Storage Emulator - it is currently needed for Orleans to discover other nodes.
2. Debug `CalculatorApp` from Visual Studio.
3. Run `TestClient.exe get` from TestClient's output directory.
4. Run `TestClient.exe add 3.14` from TestClient's output directory.

Hope this helps :) Hit me up on [@ReubenBond](https://twitter.com/reubenbond) if you have questions.

You can ask Orleans-specific questions on Gitter: [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
