# UnityStarter — skrypty do skopiowania do `Assets/Scripts/`

## Szybki start: kreator (Etap 1 jednym kliknięciem)

Zamiast ręcznie klikać konfigurację z docs/02 i docs/03:

1. Zainstaluj w Package Manager: `com.unity.cloud.gltfast` oraz uLipSync
   (`https://github.com/hecomi/uLipSync.git#upm`).
2. Skopiuj `ArkitBlendshapeMap.cs`, `MicAudioSource.cs`, `BlinkAndIdle.cs`
   do `Assets/Scripts/`, a `Editor/FaceDramaSetupWizard.cs` do
   `Assets/Scripts/Editor/` (nazwa folderu **Editor** jest obowiązkowa).
3. Menu **FaceDrama → Kreator konfiguracji** → wskaż plik `.glb` z Avaturn →
   **Zbuduj wszystko**.
4. Kreator: importuje avatar, sprawdza blend shape'y ARKit (raport czego
   brakuje), podpina mikrofon + uLipSync z gotowym mapowaniem, dodaje
   mruganie i prosty gabinet. Wciskasz Play, mówisz — avatar rusza ustami.

## Pozostałe skrypty (kopiuj na etapie, który ich wymaga)

Wcześniej nie skompilują się, bo brakuje pakietów (uLipSync, Photon, MediaPipe).

| Plik | Etap | Wymagane pakiety |
|---|---|---|
| `Editor/FaceDramaSetupWizard.cs` | 1 | glTFast + uLipSync |
| `ArkitBlendshapeMap.cs` | 1+ | — (czysty Unity) |
| `MicAudioSource.cs` | 1 | — (czysty Unity) |
| `BlinkAndIdle.cs` | 1+ | — (czysty Unity) |
| `SessionConnector.cs` | 2+ | Photon Fusion 2 |
| `OperatorConsoleUI.cs` | 2+ | Photon Fusion 2 + Voice 2 |
| `FaceCaptureSender.cs` | 3 | Photon Fusion 2 |
| `FaceCaptureReceiver.cs` | 3 | Photon Fusion 2 |
| `LipSyncFallbackBlender.cs` | 3 | uLipSync + Photon Fusion 2 |

Każdy plik ma nagłówek z instrukcją podpięcia w edytorze. Miejsca wymagające
decyzji/konfiguracji oznaczone `TODO:`.

Konwencja: cała praca na blend shape'ach odbywa się **po nazwach ARKit**
(z dopasowaniem po sufiksie), nigdy po indeksach — dzięki temu prefiksy
Avaturn (`blendShape1.jawOpen` itp.) i ewentualna zmiana avatara na
Avatar SDK nie psują niczego.
