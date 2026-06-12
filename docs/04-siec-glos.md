# Etap 2 — Sieć: głos operatora wychodzi z ust avatara (Photon Fusion 2 + Voice 2)

Cel: operator mówi do mikrofonu laptopa → pacjent w Quest słyszy głos
przestrzennie "z głowy avatara", a usta avatara ruszają się z tego głosu.

## Architektura

- **Photon Fusion 2** — synchronizacja stanu (później też mimika, etap 3).
- **Photon Voice 2** (integracja z Fusion) — streaming audio mikrofonu.
- Ruch idzie przez **chmurę Photon** (darmowy tier: 20 CCU — nasza para
  pacjent+operator to 2 CCU). W gabinecie wystarczy zwykły internet;
  opóźnienie do najbliższego regionu (EU) to ~20–40 ms.
  Praca 100% offline (self-hosted Photon Server) jest możliwa, ale zostaw to
  na później — komplikuje setup, a zysk minimalny.

## 1. Konta i pakiety

1. Załóż konto na <https://dashboard.photonengine.com>, utwórz **dwie aplikacje**:
   typu *Fusion* i typu *Voice* — zapisz oba App ID.
2. Asset Store / Package Manager: zaimportuj **Photon Fusion 2** oraz
   **Photon Voice 2** (paczka "Voice 2" zawiera integrację `Fusion.Addons.Voice`
   — w razie czego doc: *Voice For Fusion*).
3. `Fusion → Realtime Settings` (i odpowiednio Voice) — wklej App ID.
   Jeśli repo ma być publiczne, nie commituj App ID (patrz `.gitignore`).

## 2. Topologia sesji

- Tryb **Shared Mode** (najprostszy): obie aplikacje (Quest i konsola operatora)
  wchodzą do pokoju o znanej nazwie, np. `facedrama-gabinet-1`.
- Pacjent: dołącza automatycznie po starcie aplikacji.
- Operator: dołącza z konsoli (build Windows) — ma przycisk Connect.
- Skrypt startowy połączenia: `UnityStarter/SessionConnector.cs`.

## 3. Voice: mikrofon operatora → AudioSource na avatarze

1. Na obiekcie sieciowym operatora: **Recorder** (Photon Voice) —
   `Source = Microphone`, wybierz mikrofon laptopa. **Transmit Enabled = true**.
2. Po stronie pacjenta głos odbierany jest przez **Speaker** + AudioSource.
   Kluczowe: prefab "głosu operatora" ma być **zaparentowany do głowy avatara**,
   z ustawieniami AudioSource: `Spatial Blend = 1.0` (pełne 3D),
   `Min Distance ≈ 0.5`, `Max Distance ≈ 8` — wtedy głos dochodzi z miejsca,
   gdzie siedzi avatar.
3. **Lip sync z sieci**: komponent `uLipSync` przenosimy/odtwarzamy na tym samym
   obiekcie, na którym jest AudioSource Speakera — uLipSync analizuje audio
   przez `OnAudioFilterRead`, więc działa tak samo dla audio z sieci jak
   z lokalnego mikrofonu. Nic więcej nie trzeba zmieniać.

> Wzorzec "audio frame → viseme po stronie odbiorcy" to udokumentowane
> podejście Photona (przykład Fusion + Meta Avatars). My robimy to samo,
> tylko z uLipSync + ARKit.

## 4. Konsola operatora (build Windows)

Minimalny interfejs na ten etap (`UnityStarter/OperatorConsoleUI.cs`):

- status połączenia + nazwa pokoju,
- wybór mikrofonu, przycisk **Mute**,
- suwak głośności monitoringu (operator słyszy siebie? — domyślnie nie).

Build: `File → Build Profiles → Windows` (osobny build target, te same sceny —
scena `OperatorConsole.unity` zamiast `TherapyRoom.unity`).

## 5. Test końcowy etapu

1. Konsola operatora na laptopie + build na Quest (lub drugi PC w edytorze).
2. Operator mówi → w VR słychać głos z pozycji avatara, usta się ruszają.
3. Zmierz subiektywne opóźnienie (klaśnięcie + obserwacja ust): cel < 0,2 s.

## Checklist na koniec etapu

- [ ] Dwa buildy łączą się do wspólnego pokoju Photon.
- [ ] Głos operatora słychać w VR przestrzennie z głowy avatara.
- [ ] Usta avatara ruszają się z głosu sieciowego (uLipSync na Speakerze).
- [ ] Mute działa; rozłączenie/ponowne połączenie nie wywala aplikacji.

Następny krok: [05 — face capture operatora](05-face-capture.md).
