# Roadmapa projektu FaceDrama

Etapy są ułożone tak, żeby każdy kończył się działającą, testowalną wersją.
Szacunki czasowe dla osoby początkującej w Unity, pracującej po godzinach.

## Etap 0 — środowisko (≈ 2–4 dni)
Doc: [01-setup-unity-quest.md](01-setup-unity-quest.md)

Projekt Unity 6 + Meta XR SDK, pusty gabinet renderuje się na Quest w 72 FPS.

## Etap 1 — avatar + lip sync lokalnie (≈ 1 tydzień) ← MVP "odczarowania problemu"
Docs: [02-avatar-ze-zdjecia.md](02-avatar-ze-zdjecia.md), [03-lipsync.md](03-lipsync.md)

Avatar ze zdjęcia siedzi w gabinecie; mówisz do mikrofonu — rusza ustami.
To jest moment, w którym problem "blend shape'y z mową nie działają" jest
rozwiązany. Warto pokazać ten efekt terapeucie i zebrać feedback, zanim
zainwestujesz w sieć.

## Etap 2 — sieć i głos operatora (≈ 1 tydzień)
Doc: [04-siec-glos.md](04-siec-glos.md)

Operator przy laptopie mówi → pacjent w VR słyszy głos z ust avatara,
usta animują się z odbieranego audio. Od tego etapu można robić pierwsze
"sesje" testowe w parze.

## Etap 3 — pełny face capture (≈ 1–2 tygodnie)
Doc: [05-face-capture.md](05-face-capture.md)

Webcam + MediaPipe: pełna mimika operatora na avatarze, audio lip sync
zostaje jako fallback. Najtrudniejszy technicznie etap (integracja MediaPipe).

## Etap 4 — warstwa terapeutyczna (otwarty)

Funkcje do dodania po walidacji z terapeutą, w kolejności wartości:

1. **Przycisk bezpieczeństwa pacjenta** — przycisk na kontrolerze natychmiast
   wyciemnia scenę i wycisza audio ("wyjście z sytuacji"). Również operator
   musi mieć taki przycisk na konsoli. *To powinno powstać przed pierwszą
   sesją z prawdziwym pacjentem.*
2. **Panel emocji operatora** — przyciski nakładające na mimikę bazową
   wyraz twarzy (smutek, ciepło, gniew) — przydatne, gdy operator chce
   zagrać emocję mocniej, niż przekazuje ją kamera.
3. **Pozy i gesty ciała** — animacje idle (Mixamo), "pochyl się", "wstań",
   sterowane z konsoli; proceduralne skierowanie wzroku avatara na pacjenta.
4. **Scenografia** — gabinet w wersji "dom rodzinny" itp.; światło i dźwięk
   tła jako regulatory intensywności doświadczenia.
5. **Nagrywanie sesji** (za zgodą) — zapis audio + parametrów mimiki do
   odtworzenia/omówienia po sesji.
6. **Pipeline wielu avatarów** — wybór avatara z menu konsoli operatora,
   ewentualnie integracja API Avaturn do generacji w aplikacji.

## Ryzyka i decyzje odroczone

| Ryzyko | Mitygacja |
|---|---|
| Jakość podobieństwa Avaturn rozczaruje | Plan B: Avatar SDK (itSeez3D); wymiana to podmiana prefabu |
| MediaPipeUnityPlugin oporny w integracji | Wariant: capture w Pythonie → UDP → Unity (przewidziane w `FaceCaptureSender`) |
| Photon Cloud w gabinecie bez internetu | Self-hosted Photon Server albo zapasowy LTE router |
| Uncanny valley nasila dyskomfort pacjenta | Celowo nie idziemy w fotorealizm; intensywność reguluje terapeuta (światło, dystans, czas) |
| RODO: zdjęcia i dane sesji | Zgody na piśmie; avatary trzymane lokalnie; konsultacja prawna przed użyciem klinicznym |
