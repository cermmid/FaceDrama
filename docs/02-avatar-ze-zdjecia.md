# Etap 1a — Avatar bliskiej osoby ze zdjęcia (Avaturn → Unity)

Cel: avatar wygenerowany ze zdjęcia siedzi w gabinecie, a w Unity widać jego
blend shape'y ARKit na komponencie `SkinnedMeshRenderer`.

## Ścieżka podstawowa: Avaturn

Na początek **nie potrzebujesz SDK ani integracji runtime** — avatary dla pacjentów
można generować ręcznie przez przeglądarkę i wrzucać do projektu jako pliki.
Integrację API (generowanie w aplikacji) zostaw na później, gdy będzie skala.

### 1. Generacja avatara

1. Wejdź na <https://hub.avaturn.me> (lub demo na avaturn.me), załóż konto.
2. Wgraj **zdjęcie twarzy** danej osoby (en face, dobre światło, neutralna mina).
3. Dobierz ciało/ubranie/fryzurę możliwie zbliżone do realnej osoby.
4. **Eksport**: format **GLB**, koniecznie z opcją **ARKit blend shapes / facial
   morphs** (w eksporcie Avaturn opcja nazywa się "Face animations / blendshapes").

> Uwaga RODO: zdjęcie jest przetwarzane na serwerach Avaturn. Przed użyciem
> klinicznym potrzebna zgoda i ocena prawna.

### 2. Import GLB do Unity

Unity nie czyta GLB natywnie — dodaj **glTFast** (oficjalny pakiet Unity):

1. Package Manager → `+` → Add package by name → `com.unity.cloud.gltfast`.
2. Przeciągnij plik `.glb` do `Assets/Avatars/` — zaimportuje się jak prefab.
3. Wstaw do sceny `TherapyRoom`, posadź na fotelu (na razie statycznie;
   animacje siedzenia/idle to etap 4 — można użyć darmowych animacji z Mixamo,
   rig Avaturn jest z nimi zgodny).

Alternatywnie: oficjalny **Avaturn Unity SDK/przykłady** —
<https://docs.avaturn.me> (sekcja Integrations → Unity). Daje loader runtime,
ale na MVP plik GLB w Assets jest prostszy i pewniejszy.

### 3. Weryfikacja blend shape'ów (kluczowy krok!)

To tutaj wcześniej "nic nie działało", więc sprawdź zanim pójdziesz dalej:

1. W hierarchii avatara znajdź obiekt z komponentem **SkinnedMeshRenderer**,
   który ma siatkę głowy (zwykle nazwa typu `avatar_mesh`, `Head`, `Body`).
2. W Inspectorze rozwiń **BlendShapes** — powinna być lista ~50 pozycji.
3. Nazwy będą w konwencji **ARKit**: `jawOpen`, `mouthFunnel`, `mouthPucker`,
   `eyeBlinkLeft`, `browInnerUp`, … Często z prefiksem, np. `blendShape1.jawOpen`
   albo `Face.jawOpen` — **to normalne**, nasze skrypty szukają po sufiksie
   (patrz `UnityStarter/ArkitBlendshapeMap.cs`).
4. Test ręczny: w Play mode przesuń suwak `jawOpen` na 100 — szczęka ma opaść.
   Jeśli tak — avatar jest dobry i cały dalszy pipeline zadziała.

> Jeśli lista BlendShapes jest pusta → eksport z Avaturn był bez opcji
> blendshapes. Wyeksportuj ponownie z włączonymi "Face animations".

## Plan B: Avatar SDK (itSeez3D)

Jeśli Avaturn nie zadowoli jakością podobieństwa albo cenowo:

- <https://avatarsdk.com> — generacja pełnej postaci z jednego zdjęcia,
  oficjalny plugin Unity, głowy w wariancie "Head 2.0" mają 51 blend shape'ów
  **plus 17 wizemów** (czyli działa też wprost z OVRLipSync),
  udokumentowana integracja z lip syncem.
- Migracja w naszym kodzie sprowadza się do podmiany prefabu avatara —
  reszta (uLipSync, sieć, face capture) operuje na nazwach blend shape'ów.

## Optymalizacja pod Quest

- Avatar Avaturn ma ~30–60 tys. trójkątów — Quest 2/3 to udźwignie, ale:
  - w ustawieniach importu tekstur zbij rozmiar do 1024 (skóra może zostać 2048),
  - włącz kompresję ASTC (Build Settings → Texture Compression),
  - jeden avatar + prosty pokój = bez problemu 72 FPS.

## Checklist na koniec etapu

- [ ] Plik GLB avatara w `Assets/Avatars/`, avatar widoczny w scenie na fotelu.
- [ ] `SkinnedMeshRenderer` pokazuje listę blend shape'ów ARKit.
- [ ] Suwak `jawOpen` w Inspectorze otwiera usta.
- [ ] Build na Quest nadal trzyma ~72 FPS z avatarem w kadrze.

Następny krok: [03 — lip sync](03-lipsync.md).
