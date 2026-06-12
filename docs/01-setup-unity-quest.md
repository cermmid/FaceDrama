# Etap 0 — Unity 6 + Meta Quest: środowisko i pierwszy build

Cel etapu: pusty "gabinet" (pokój 3D) uruchamia się na Quest w stabilnych 72 FPS.
Czas: ok. 0,5–1 dzień, głównie instalacje.

## 1. Instalacja Unity

1. Zainstaluj **Unity Hub**: <https://unity.com/download>
2. W Hub → Installs → Install Editor → wybierz **Unity 6 LTS** (6000.0.x).
3. Przy instalacji zaznacz moduły:
   - **Android Build Support** (wraz z *OpenJDK* i *Android SDK & NDK Tools*) — Quest to Android,
   - **Windows Build Support (IL2CPP)** — build konsoli operatora.
4. Licencja **Unity Personal** jest darmowa (przychód < 200 tys. USD/rok).

## 2. Nowy projekt

1. Hub → New project → szablon **Universal 3D** (URP) → nazwa `UnityProject`,
   lokalizacja: katalog tego repo (`FaceDrama/UnityProject`).
2. Po otwarciu: `Edit → Project Settings → Player`:
   - Company/Product name — dowolne,
   - w zakładce Android: **Minimum API Level = 32**, **Scripting Backend = IL2CPP**,
     **Target Architectures = ARM64** (tylko).

## 3. Meta XR SDK + OpenXR

1. `Window → Package Manager → + → Add package by name…` i dodaj:
   - `com.meta.xr.sdk.all` (**Meta XR All-in-One SDK**).
   Alternatywnie pobierz z Asset Store: "Meta XR All-in-One SDK".
2. Po instalacji uruchom **Meta → Tools → Project Setup Tool** i kliknij
   **Fix All** / **Apply All** w obu zakładkach (Android i Windows) — narzędzie
   samo ustawi OpenXR, kolory liniowe, Vulkan/GLES, itd.
3. `Edit → Project Settings → XR Plug-in Management`:
   - zakładka Android: zaznacz **OpenXR** (lub Oculus — Project Setup Tool podpowie),
   - w OpenXR → Interaction Profiles dodaj **Oculus Touch Controller Profile**.

## 4. Scena "Gabinet"

1. Nowa scena `Assets/Scenes/TherapyRoom.unity`.
2. Usuń zwykłą `Main Camera`. Dodaj prefab **OVRCameraRig** (z Meta XR SDK)
   albo `XR Origin` — to jest "głowa" pacjenta.
3. Zbuduj prosty pokój: podłoga (Plane), 4 ściany, ciepłe światło
   (1 × Directional + lightmapy później). Na start wystarczą szare boxy —
   wystrój dopracujemy po etapie 3.
4. Dodaj krzesło/fotel naprzeciwko pozycji pacjenta — tu usiądzie avatar.

## 5. Quest w trybie deweloperskim

1. Załóż konto deweloperskie: <https://developers.meta.com> i utwórz "organizację".
2. W aplikacji **Meta Horizon** na telefonie: Urządzenia → Twój Quest →
   Ustawienia → **Tryb dewelopera → ON**.
3. Podłącz Quest kablem USB-C do komputera, w goglach zaakceptuj
   "Zezwól na debugowanie USB" (zaznacz "zawsze").
4. Sprawdź w terminalu: `adb devices` — urządzenie ma być na liście
   (adb jest w `<Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/`).

## 6. Pierwszy build

1. `File → Build Profiles` → platforma **Android** → Switch Platform.
2. Dodaj scenę `TherapyRoom` do listy scen.
3. **Build and Run** — APK zainstaluje się i uruchomi na Quest.
4. Załóż gogle: powinieneś stać/siedzieć w swoim pokoju 3D.

### Szybsza pętla developerska

- Większość pracy (lip sync, sieć, avatar) testuj w **edytorze na PC** —
  z goglami podpiętymi przez **Quest Link** możesz nawet testować VR bez builda
  (Play mode + Link). Build na Quest rób raz na kilka godzin pracy.

## Checklist na koniec etapu

- [ ] Projekt Unity 6 URP w `UnityProject/`, commit do repo (`.gitignore` już jest).
- [ ] `adb devices` widzi Quest.
- [ ] Build chodzi na Quest, rozglądanie się działa, brak czarnych migotań.
- [ ] W goglach: Ustawienia → System → wskaźnik FPS (lub OVR Metrics Tool) pokazuje ~72 FPS.

Następny krok: [02 — avatar ze zdjęcia](02-avatar-ze-zdjecia.md).
