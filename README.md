# Extreal.Integration.Chat.Vivox

## How to test

1. Create ScriptableObject `VivoxAppConfig` and fill out the fields with vivox credential information.
1. Open `Main` scene and put ScriptableObject above into the field `VivoxAppConfig` of the component `VivoxAppConfigProvider` in the GameObject `VivoxAppConfigProvider`.
1. Open `Build Settings` and put `Main` scene into `Scenes In Build`.
1. Run `Extreal.Integration.Chat.Vivox.Test.dll`.
1. Make a sound when stopping at `AudioEnergyChanged` in `VivoxClientTest`.
1. Disable the Network Connection when stopping at `LoginWithoutInternetConnection` in `VivoxClientTest`, then enable it.
1. Disable the Network Connection when stopping at `UnexpectedDisconnect` in `VivoxClientTest`, then enable it after 30 seconds or more.
1. All tests are completed.

## How to play the sample

### Installation

- Install packages that the sample depends on from Package Manager.
  - Vivox
  - UniTask
  - UniRx
  - VContainer
  - Unity.Collections
  - Unity.InputSystem
  - TextMeshPro
  - Extreal.Core.Logging
  - Extreal.Core.StageNavigation
- Install this sample from Package Manager.

### How to play

1. Create ScriptableObject `VivoxAppConfig` and fill out the fields with vivox credential information.
1. Open `ChatControl` scene and put ScriptableObject above into the field `VivoxAppConfig` of the component `ChatControlScope` in the GameObject `Scope`.
1. Open `Build Settings` and put the scenes below into `Scenes In Build`.
    - `MVS/App/App`
    - `MVS/CameraControl/CameraControl`
    - `MVS/ChatControl/ChatControl`
    - `MVS/InputSystemControl/InputSystemControl`
    - `MVS/TextChatScreen/TextChatScreen`
    - `MVS/VoiceChatScreen/VoiceChatScreen`
1. Play `App` scene.
