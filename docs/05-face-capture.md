# Etap 3 — Pełny face capture operatora (webcam + MediaPipe → avatar)

Cel: mimika operatora (brwi, uśmiech, mruganie, ruchy głowy) jest śledzona
kamerą laptopa i odtwarzana 1:1 na twarzy avatara w VR.

## Dlaczego MediaPipe (a nie iPhone/Live Link Face)

- **MediaPipe Face Landmarker** (Google) z webcam zwraca **dokładnie 52
  współczynniki blend shape'ów ARKit** (0–1) + macierz pozy głowy —
  czyli ten sam format, który ma avatar Avaturn. Mapowanie jest 1:1.
- Darmowy, działa na każdej kamerce, bez dodatkowego sprzętu.
- Live Link Face (iPhone) od iOS 26 jest niestabilny (crashe po kilkunastu
  minutach) i celuje w Unreala; odpada.

## 1. Instalacja MediaPipeUnityPlugin

Najdojrzalszy port MediaPipe do Unity: <https://github.com/homuler/MediaPipeUnityPlugin>

1. Pobierz najnowszy `.unitypackage` / paczkę UPM z Releases i zaimportuj
   **tylko w buildzie operatora** (Windows). Na Quest ten pakiet nie jest potrzebny.
2. Pobierz model **Face Landmarker (with blendshapes)**:
   `face_landmarker_v2_with_blendshapes.task` z
   <https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker>
   i umieść w `Assets/StreamingAssets/`.
3. Uruchom sample "Face Landmark Detection" z paczki, sprawdź że kamera działa
   i w wynikach są `faceBlendshapes` (52 kategorie z nazwami ARKit).

> Alternatywa, jeśli walka z pluginem pójdzie opornie: osobny mały program
> w Pythonie (`mediapipe` z pip) wysyłający blendshapes po UDP do Unity —
> ~50 linii kodu. Architektura skryptów w tym repo to przewiduje
> (`FaceCaptureSender` ma wariant odbioru z UDP).

## 2. Przepływ danych

```
webcam → MediaPipe FaceLandmarker (30 Hz)
       → float[52] ARKit + poza głowy
       → FaceCaptureSender (konsola operatora)      ── Photon Fusion ──►
       → FaceCaptureReceiver (Quest): wygładzanie → SkinnedMeshRenderer
       → LipSyncFallbackBlender: usta = max(capture, uLipSync) lub wybór źródła
```

- Pakiet danych: 52 × float + kwaternion głowy ≈ 224 B, wysyłka 20–30 Hz
  (Networked Property / unreliable RPC w Fusion) — pomijalne obciążenie.
- Po stronie Quest wartości są wygładzane (lerp / filtr One Euro), żeby
  niedoskonałości trackingu nie powodowały drgań twarzy.

## 3. Podpięcie skryptów (z `UnityStarter/`)

Na obiekcie sieciowym avatara (NetworkObject we Fusion):

1. **`FaceCaptureSender`** — tylko po stronie operatora (HasStateAuthority
   operatora): czyta wynik MediaPipe co klatkę i zapisuje do Networked Properties.
2. **`FaceCaptureReceiver`** — po stronie pacjenta: czyta Networked Properties,
   wygładza i nakłada na blend shape'y (`ArkitBlendshapeMap` znajduje indeksy
   po sufiksach nazw, więc prefiksy Avaturn nie przeszkadzają).
3. **`LipSyncFallbackBlender`** — decyduje o ustach:
   - tryb **CaptureOnly** — usta w pełni z kamery (najwierniejszy, wymaga
     dobrego światła na twarz operatora),
   - tryb **Blend** (domyślny) — `max(capture, uLipSync)` na kanałach ust;
     gdy kamera gubi twarz, usta dalej ruszają się z audio,
   - tryb **AudioOnly** — jak w etapie 2.
4. **`BlinkAndIdle`** — gdy strumień capture znika na > 0,5 s (operator
   odwrócił głowę, kamera zasłonięta): automatyczne mruganie i mikroruchy,
   żeby avatar nie "zamarzał".

Ruch głowy: kwaternion z MediaPipe nakładamy z ograniczeniem (np. ±20°)
na kość szyi/głowy avatara — avatar siedzi, więc tylko obroty, bez translacji.

## 4. Jakość trackingu — praktyka

- Światło na twarz operatora (lampka za laptopem) — największa dźwignia jakości.
- Kamera na wysokości oczu; patrzenie w monitor ≈ patrzenie "na pacjenta".
- `eyeBlink*` z MediaPipe bywa czuły — daj próg (np. < 0,1 → 0).
- Skala ekspresji: współczynniki MediaPipe są zachowawcze; w `FaceCaptureReceiver`
  jest mnożnik per-kanał (np. brwi ×1.3, uśmiech ×1.2) — dostrój na oko.

## Checklist na koniec etapu

- [ ] Sample MediaPipe pokazuje 52 blendshapes z kamery laptopa.
- [ ] Uśmiech/uniesienie brwi/mrugnięcie operatora widać na avatarze w VR < 0,2 s.
- [ ] Ruchy głowy operatora obracają głowę avatara (z limitem).
- [ ] Zasłonięcie kamery → avatar przechodzi na mruganie idle + usta z audio.

Następny krok: [06 — plan etapów i warstwa terapeutyczna](06-plan-etapow.md).
