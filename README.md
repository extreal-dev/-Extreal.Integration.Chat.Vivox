# Extreal.Integration.Chat.Vivox

## How to test

1. Make ScriptableObject `VivoxConnectionConfig` and fill out the fields with vivox credential information.
1. Open `Main` scene and put ScriptableObject above into the field `VivoxConnectionConfig` of the component `VivoxConnectionConfigProvider` in the GameObject `VivoxConnectionConfigProvider`.
1. Open `Build Settings` and put `Main` scene into `Scenes In Build`.
1. Run `Extreal.Integration.Chat.Vivox.Test.dll`.
1. Make a sound when stopping at `AudioEnergyChanged` in `VivoxClientTest`.
1. Disable the Network Connection when stopping at `LoginWithoutInternetConnection` in `VivoxClientTest`, then enable it.
1. All tests are completed.
