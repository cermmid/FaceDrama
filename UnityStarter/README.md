# UnityStarter — skrypty do skopiowania do `Assets/Scripts/`

Kopiuj pliki **dopiero na etapie, który ich wymaga** — wcześniej nie skompilują
się, bo brakuje pakietów (uLipSync, Photon, MediaPipe).

| Plik | Etap | Wymagane pakiety |
|---|---|---|
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
