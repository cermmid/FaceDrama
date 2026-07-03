# Etap 1b — Lip sync: uLipSync na blend shape'ach ARKit

Cel: mówisz do mikrofonu → avatar rusza ustami. To jest dokładnie miejsce,
w którym wcześniej Avaturn + OVRLipSync "nie działały" — wyjaśnienie i obejście niżej.

## Dlaczego wcześniej nie działało

- **OVRLipSync** (Meta) analizuje audio i wypluwa 15 **wizemów**
  (`sil, PP, FF, TH, DD, kk, CH, SS, nn, RR, aa, E, ih, oh, ou`). Komponent
  `OVRLipSyncContextMorphTarget` oczekuje, że mesh ma blend shape'y o tych nazwach.
- **Avaturn** eksportuje blend shape'y w konwencji **ARKit** (`jawOpen`,
  `mouthFunnel`, …) — wizemów nie ma, więc OVRLipSync nie miał czym ruszać.
- Rozwiązanie: **uLipSync**, który pozwala zmapować wykrywane fonemy na
  **dowolne** blend shape'y — w tym ARKit.

## 1. Instalacja uLipSync

Package Manager → `+` → **Add package from git URL**:

```
https://github.com/hecomi/uLipSync.git#upm
```

Wymagane zależności (`Burst`, `Mathematics`) dociągną się same; jeśli nie —
doinstaluj `com.unity.burst` i `com.unity.mathematics` ręcznie.

## 2. Podpięcie pod avatar

Na obiekcie avatara (lub osobnym obiekcie "LipSync"):

1. **AudioSource** — na MVP źródłem jest mikrofon lokalny (skrypt
   `UnityStarter/MicAudioSource.cs` podaje mikrofon do AudioSource).
   W etapie 2 podmienimy to na strumień z Photon Voice — uLipSync
   tego nie zauważy, bo czyta audio z tego samego AudioSource.
2. Komponent **uLipSync** — analizuje audio (MFCC) i rozpoznaje fonemy.
3. Komponent **uLipSyncBlendShape**:
   - `Skinned Mesh Renderer` → mesh głowy avatara,
   - w sekcji *Blend Shapes* dodaj wpisy fonem → blend shape (tabela niżej).

### Mapowanie fonemów na ARKit (punkt wyjścia)

| Fonem (uLipSync) | Blend shape ARKit | Waga maks. |
|---|---|---|
| A | `jawOpen` | 70–100 |
| I | `mouthStretchLeft` + `mouthStretchRight` (lub `mouthSmileLeft/Right` lekko) | 40–60 |
| U | `mouthPucker` (+ odrobina `mouthFunnel`) | 60–80 |
| E | `jawOpen` (40) + `mouthStretchLeft/Right` (30) | — |
| O | `jawOpen` (50) + `mouthFunnel` (60) | — |
| N/S (spółgłoski) | `mouthClose` lekko lub nic | 0–20 |
| — (cisza) | wszystkie do 0 | — |

uLipSyncBlendShape pozwala przypisać kilka blend shape'ów do jednego fonemu —
korzystaj z tego, pojedyncze `jawOpen` wygląda jak kukiełka. Dobre ustawienia
`Smoothness` ~0.05–0.1 s.

## 3. Kalibracja profilu

1. Komponent uLipSync ma **Profile** — użyj wbudowanego sample'a
   (Female/Male) na start.
2. Lepszy efekt: nagraj własny profil — w komponencie uLipSync otwórz
   zakładkę kalibracji, wymawiaj "aaa, iii, uuu, eee, ooo" trzymając
   przycisk Calib przy odpowiednim fonemie. 2 minuty pracy, duża różnica.
3. Docelowo profil kalibrujemy na **głos terapeuty** (to jego głos będzie
   analizowany).

## Pułapka: zakres wag blend shape'ów (GLB ≠ FBX)

Unity tradycyjnie traktuje wagi blend shape'ów jako **0–100**, ale avatar
importowany z **GLB przez glTFast** ma klatki kształtów zdefiniowane w skali
**0–1** (tak zapisuje je format glTF). Ustawienie wagi "100" na takim meshu
daje 100-krotne przesterowanie — twarz/głowa dosłownie "eksploduje" na czas
mrugnięcia czy sylaby, a brwi latają nienaturalnie wysoko.

Skrypty FaceDrama wykrywają skalę automatycznie
(`ArkitBlendshapeMap.DetectWeightScale`) — dlatego do nakładania ust używamy
własnego `LipSyncArkitApplier` zamiast komponentu `uLipSyncBlendShape`
(który zakłada 0–100). Jeśli konfigurujesz coś ręcznie i widzisz "wybuchy"
siatki, to prawie na pewno ten problem: sprawdź, w jakiej skali są wagi
(w Inspectorze przesuń suwak blend shape'a — pełny efekt przy 1 czy przy 100?).

## 4. Test

- Play mode w edytorze → mów do mikrofonu → okno uLipSync (wizualizacja)
  pokazuje rozpoznane fonemy, avatar rusza ustami.
- Jeśli usta drgają chaotycznie: zwiększ `Smoothness`, sprawdź czy AudioSource
  nie ma włączonego dodatkowego przetwarzania, skalibruj profil.
- Build na Quest: uLipSync używa Burst/Job System — działa na Quest bez zmian
  (na MVP możesz mówić do mikrofonu samych gogli, żeby przetestować standalone).

## Checklist na koniec etapu

- [ ] Avatar rusza ustami zgodnie z tym, co mówisz do mikrofonu (edytor).
- [ ] Mapowanie obejmuje co najmniej A/I/U/E/O + ciszę (usta się domykają).
- [ ] Ruch ust wygląda płynnie (Smoothness dobrane), bez "klapania".
- [ ] To samo działa w buildzie na Quest.

Następny krok: [04 — sieć i głos operatora](04-siec-glos.md).
