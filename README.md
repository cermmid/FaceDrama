# FaceDrama

Aplikacja VR do psychodramy terapeutycznej: pacjent w goglach Meta Quest rozmawia z 3D
avatarem bliskiej osoby (np. matki), którego twarz wygenerowano **ze zdjęcia**. Terapeuta
(operator) siedzi w tym samym gabinecie przy laptopie — mówi do mikrofonu i jest śledzony
kamerą, a avatar w VR mówi jego głosem i odtwarza jego mimikę w czasie rzeczywistym.

## Architektura

Jeden projekt Unity, dwa build targety:

```
[Laptop operatora — build Windows]                [Quest pacjenta — build Android]
 mikrofon ──────────── Photon Voice 2 ──────────► głos z ust avatara (audio 3D)
 webcam → MediaPipe →  52 wartości ARKit          ├─► blend shape'y twarzy avatara
          blendshapes  + pozycja głowy ── Fusion ─┘    (mimika 1:1 z operatora)
 panel sterowania   →  emocje/gesty (opcjonalnie)      uLipSync jako fallback ust,
                                                        gdy capture słabej jakości
```

Przesył mimiki to ~60 floatów na klatkę (~200 B przy 30–60 Hz) — trywialne obciążenie
dla sieci lokalnej, opóźnienie końcowe < 100 ms.

## Stack technologiczny

| Warstwa | Wybór | Dlaczego |
|---|---|---|
| Silnik | [Unity 6 LTS](https://unity.com/releases/lts) (URP) + [Meta XR SDK](https://developers.meta.com/horizon/downloads/package/meta-xr-sdk-all-in-one-upm/) | Najlepsze wsparcie Quest standalone, niski próg wejścia |
| Avatar ze zdjęcia | [Avaturn](https://avaturn.me) (GLB z blend shape'ami ARKit); plan B: [Avatar SDK](https://avatarsdk.com) | Aktywnie utrzymywany; Ready Player Me wygaszony 31.01.2026 |
| Lip sync z audio | [uLipSync](https://github.com/hecomi/uLipSync) (open source) | Mapuje na **dowolne** blend shape'y, w tym ARKit; działa na Quest |
| Face capture | Webcam + [MediaPipe Face Landmarker](https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker) ([MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin)) | Darmowy, bez dodatkowego sprzętu, zwraca dokładnie 52 współczynniki ARKit — 1:1 zgodne z avatarem Avaturn |
| Sieć (głos + mimika) | [Photon Fusion 2](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro) + [Photon Voice 2](https://doc.photonengine.com/voice/current/getting-started/voice-intro) | Standard w VR, darmowy tier 20 CCU wystarcza dla pary pacjent–operator |

### Dlaczego nie…

- **Ready Player Me** — usługa wygaszona 31.01.2026 (przejęcie przez Netflix). Nie używać.
- **Unreal + MetaHuman** — fotorealizm wymaga PCVR (nie działa sensownie na Quest
  standalone), a MetaHuman jest licencyjnie zamknięty w Unrealu. Wyższy próg wejścia.
- **Oculus OVRLipSync wprost na avatarze Avaturn** — OVRLipSync oczekuje blend shape'ów
  wizemowych (`viseme_PP`, `viseme_aa`…), a Avaturn eksportuje konwencję ARKit
  (`jawOpen`, `mouthFunnel`…). To dlatego "blend shape'y nie działały z mową" —
  rozwiązaniem jest uLipSync z ręcznym mapowaniem (patrz `docs/03-lipsync.md`).
- **Live Link Face (iPhone)** — od iOS 26 aplikacja jest niestabilna (crashe po
  10–15 min). Webcam + MediaPipe jest pewniejszy i nie wymaga iPhone'a.
- **NVIDIA Audio2Face / ACE** — inference w chmurze, dodatkowe opóźnienia i koszty;
  zbędny przy face capture operatora.

## Dokumentacja — kolejność czytania

1. [`docs/01-setup-unity-quest.md`](docs/01-setup-unity-quest.md) — Unity 6, Meta XR SDK, pierwszy build na Quest
2. [`docs/02-avatar-ze-zdjecia.md`](docs/02-avatar-ze-zdjecia.md) — zdjęcie → Avaturn → GLB → Unity
3. [`docs/03-lipsync.md`](docs/03-lipsync.md) — uLipSync + mapowanie na blend shape'y ARKit
4. [`docs/04-siec-glos.md`](docs/04-siec-glos.md) — Photon Fusion + Voice, głos operatora w VR
5. [`docs/05-face-capture.md`](docs/05-face-capture.md) — MediaPipe, streaming mimiki na avatar
6. [`docs/06-plan-etapow.md`](docs/06-plan-etapow.md) — roadmapa etapów 0–4

## Skrypty startowe

Katalog [`UnityStarter/`](UnityStarter/) zawiera skrypty C# do skopiowania do
`Assets/Scripts/` w projekcie Unity. Wymagają zainstalowanych pakietów (uLipSync,
Photon Fusion 2 + Voice 2, MediaPipeUnityPlugin) — szczegóły w poszczególnych
dokumentach. Każdy plik ma nagłówek z opisem, do którego etapu należy.

## Uwagi prawne / etyczne (przed użyciem klinicznym)

- Zdjęcia osób przesyłane są do chmury Avaturn w celu generacji avatara — kwestia
  RODO/zgody osoby ze zdjęcia do rozstrzygnięcia z prawnikiem.
- Konfrontacja z avatarem osoby związanej z traumą to silna interwencja — aplikacja
  ma wbudowany "przycisk bezpieczeństwa" (natychmiastowe wyciemnienie sceny, etap 4),
  ale protokół terapeutyczny musi nadzorować wykwalifikowany terapeuta.
