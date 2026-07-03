// FaceDrama — Kreator konfiguracji: Etap 1 jednym kliknięciem.
//
// Okno w edytorze Unity (menu: FaceDrama -> Kreator konfiguracji), które
// z podanego pliku GLB (avatar z Avaturn) buduje całą scenę Etapu 1:
//   1. kopiuje GLB do Assets/Avatars i importuje (przez glTFast),
//   2. wstawia avatar do sceny i waliduje blend shape'y ARKit,
//   3. dodaje mikrofon (MicAudioSource) + uLipSync + mapowanie fonemów
//      na blend shape'y ARKit (to, co wcześniej trzeba było klikać ręcznie),
//   4. dodaje BlinkAndIdle (mruganie) i opcjonalnie prosty gabinet,
//   5. wypisuje raport: czego brakuje i co zrobić dalej.
//
// Wymagania PRZED uruchomieniem (Package Manager):
//   - com.unity.cloud.gltfast            (import GLB)
//   - https://github.com/hecomi/uLipSync.git#upm  (lip sync)
// Plik skopiuj do: Assets/Scripts/Editor/  (folder MUSI nazywać się "Editor").
// Pozostałe skrypty z UnityStarter/ skopiuj do Assets/Scripts/.
//
// Pisane pod uLipSync v3.x — jeśli autor zmieni API (pola BlendShapeInfo),
// skonfiguruj mapowanie ręcznie wg docs/03-lipsync.md.
//
// Etapy 2-3 (sieć Photon, face capture) celowo zostają poza kreatorem —
// wymagają kont/App ID i drugiego urządzenia, patrz docs/04 i docs/05.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace FaceDrama.EditorTools
{
    public class FaceDramaSetupWizard : EditorWindow
    {
        public enum Scenery { PolanaZJeziorem, Gabinet, Brak }

        string _glbPath = "";
        Scenery _scenery = Scenery.PolanaZJeziorem;
        bool _setupLipSync = true;
        readonly List<string> _report = new List<string>();

        // Mapowanie fonem -> kanały ARKit żyje w LipSyncArkitApplier.PhonemeMap.

        [MenuItem("FaceDrama/Kreator konfiguracji")]
        static void Open()
        {
            var window = GetWindow<FaceDramaSetupWizard>("FaceDrama");
            window.minSize = new Vector2(420, 240);
        }

        void OnGUI()
        {
            GUILayout.Label("Avatar ze zdjęcia (plik .glb z Avaturn)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _glbPath = EditorGUILayout.TextField(_glbPath);
                if (GUILayout.Button("Wybierz…", GUILayout.Width(80)))
                {
                    string picked = EditorUtility.OpenFilePanel(
                        "Wybierz avatar (GLB)", "", "glb");
                    if (!string.IsNullOrEmpty(picked)) _glbPath = picked;
                }
            }

            EditorGUILayout.Space();
            _scenery = (Scenery)EditorGUILayout.EnumPopup("Sceneria", _scenery);
            _setupLipSync = EditorGUILayout.ToggleLeft(
                "Skonfiguruj lip sync (mikrofon + uLipSync + mapowanie ARKit)", _setupLipSync);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Kreator zbuduje scenę Etapu 1: avatar + mówienie do mikrofonu " +
                "rusza ustami. Sieć (Etap 2) i face capture (Etap 3) — wg docs/.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_glbPath)))
            {
                if (GUILayout.Button("Zbuduj wszystko", GUILayout.Height(36)))
                    RunSetup();
            }
        }

        void RunSetup()
        {
            _report.Clear();

            GameObject avatar = ImportAvatar();
            if (avatar == null) { ShowReport(false); return; }

            var faceMesh = FindFaceMesh(avatar);
            if (faceMesh == null)
            {
                _report.Add("BŁĄD: avatar nie ma SkinnedMeshRenderera z blend shape'ami. " +
                            "Wyeksportuj z Avaturn ponownie z włączonymi 'Face animations / blendshapes'.");
                ShowReport(false);
                return;
            }
            ValidateBlendshapes(faceMesh);

            avatar.AddComponent<BlinkAndIdle>().faceMesh = faceMesh;
            _report.Add("OK: BlinkAndIdle (automatyczne mruganie) dodane.");

            if (_setupLipSync) SetupLipSync(avatar, faceMesh);
            if (_scenery == Scenery.PolanaZJeziorem) BuildMeadow(avatar);
            else if (_scenery == Scenery.Gabinet) BuildRoom(avatar);

            EditorSceneManagerMarkDirty();
            ShowReport(true);
        }

        GameObject ImportAvatar()
        {
            if (!File.Exists(_glbPath))
            {
                _report.Add($"BŁĄD: plik nie istnieje: {_glbPath}");
                return null;
            }

            const string dir = "Assets/Avatars";
            Directory.CreateDirectory(dir);
            string assetPath = $"{dir}/{Path.GetFileName(_glbPath)}";

            // kopiuj tylko, jeśli plik nie jest już w projekcie
            if (Path.GetFullPath(assetPath) != Path.GetFullPath(_glbPath))
                File.Copy(_glbPath, assetPath, overwrite: true);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                _report.Add("BŁĄD: Unity nie zaimportowało GLB. Zainstaluj pakiet glTFast " +
                            "(com.unity.cloud.gltfast) i spróbuj ponownie.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Avatar";
            instance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "FaceDrama Avatar");
            _report.Add($"OK: avatar zaimportowany ({assetPath}) i wstawiony do sceny.");
            return instance;
        }

        static SkinnedMeshRenderer FindFaceMesh(GameObject avatar)
        {
            SkinnedMeshRenderer best = null;
            foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null || smr.sharedMesh.blendShapeCount == 0) continue;
                if (best == null || smr.sharedMesh.blendShapeCount > best.sharedMesh.blendShapeCount)
                    best = smr; // mesh z największą liczbą blend shape'ów = twarz
            }
            return best;
        }

        void ValidateBlendshapes(SkinnedMeshRenderer faceMesh)
        {
            var map = ArkitBlendshapeMap.ResolveMeshIndices(faceMesh);
            int found = 0;
            foreach (int idx in map) if (idx >= 0) found++;

            _report.Add($"OK: mesh twarzy '{faceMesh.name}' — znaleziono {found}/52 kanałów ARKit.");
            if (map[ArkitBlendshapeMap.ChannelIndex("jawOpen")] < 0)
                _report.Add("UWAGA: brak kanału 'jawOpen' — lip sync nie ruszy szczęką. " +
                            "Sprawdź eksport z Avaturn.");
            ArkitBlendshapeMap.LogMissingChannels(faceMesh); // szczegóły w konsoli
        }

        void SetupLipSync(GameObject avatar, SkinnedMeshRenderer faceMesh)
        {
            // Obiekt "Voice" przy głowie: audio + analiza + mapowanie
            var voice = new GameObject("Voice");
            voice.transform.SetParent(avatar.transform, false);
            var head = FindHead(avatar);
            voice.transform.position = head != null
                ? head.position
                : faceMesh.bounds.center;

            var audio = voice.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            voice.AddComponent<MicAudioSource>();

            var ls = voice.AddComponent<uLipSync.uLipSync>();
            ls.profile = FindLipSyncProfile();
            if (ls.profile == null)
                _report.Add("UWAGA: nie znaleziono profilu uLipSync — przypisz Sample profile " +
                            "ręcznie albo skalibruj własny (docs/03, sekcja 3).");

            // Własny applier zamiast uLipSyncBlendShape: wykrywa zakres wag mesha
            // (GLB: 0..1, FBX: 0..100) i mapuje fonemy na kanały ARKit.
            var applier = voice.AddComponent<LipSyncArkitApplier>();
            applier.faceMesh = faceMesh;

            // spięcie zdarzeniem (to samo, co ręczne przeciągnięcie w Inspectorze)
            UnityEventTools.AddPersistentListener(ls.onLipSyncUpdate, applier.OnLipSyncUpdate);

            float scale = ArkitBlendshapeMap.DetectWeightScale(faceMesh);
            _report.Add($"OK: lip sync skonfigurowany (zakres wag mesha: 0..{scale:0.##}). " +
                        "Wciśnij Play i mów do mikrofonu.");
        }

        static Transform FindHead(GameObject avatar)
        {
            foreach (var t in avatar.GetComponentsInChildren<Transform>())
                if (t.name.ToLowerInvariant().Contains("head")) return t;
            return null;
        }

        static uLipSync.Profile FindLipSyncProfile()
        {
            // preferuj sample z paczki uLipSync; bierz pierwszy z brzegu jako fallback
            foreach (string guid in AssetDatabase.FindAssets("t:uLipSync.Profile"))
            {
                var p = AssetDatabase.LoadAssetAtPath<uLipSync.Profile>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (p != null) return p;
            }
            return null;
        }

        // ---- Sceneria: sielska polana z jeziorem ----
        // Wszystko z prymitywów i kolorów (bez assetów): trawa, jezioro,
        // niskie słońce, mgiełka, drzewa, kwiaty, pieniek obok avatara.

        void BuildMeadow(GameObject avatar)
        {
            var meadow = new GameObject("Polana");
            var rng = new System.Random(20260613); // deterministycznie — zawsze ta sama polana

            // trawa
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Trawa";
            ground.transform.SetParent(meadow.transform);
            ground.transform.localScale = new Vector3(8, 1, 8); // 80 x 80 m
            ground.GetComponent<Renderer>().sharedMaterial =
                CreateMat(new Color(0.36f, 0.55f, 0.26f), smoothness: 0.05f);

            // jezioro — spłaszczony walec z boku polany
            var lake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lake.name = "Jezioro";
            lake.transform.SetParent(meadow.transform);
            lake.transform.localPosition = new Vector3(9f, 0.02f, 12f);
            lake.transform.localScale = new Vector3(16f, 0.01f, 12f);
            lake.GetComponent<Renderer>().sharedMaterial =
                CreateMat(new Color(0.25f, 0.5f, 0.65f), smoothness: 0.95f);

            // pieniek przy avatarze (zamiast szarego sześcianu w kolanach)
            var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = "Pieniek";
            stump.transform.SetParent(meadow.transform);
            stump.transform.localPosition = new Vector3(0.9f, 0.2f, 1.6f);
            stump.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
            stump.GetComponent<Renderer>().sharedMaterial =
                CreateMat(new Color(0.45f, 0.33f, 0.22f), smoothness: 0.1f);

            // drzewa dookoła (pierścień 14-22 m od środka, z dala od jeziora)
            var trunkMat = CreateMat(new Color(0.4f, 0.28f, 0.18f), 0.1f);
            var leafMat = CreateMat(new Color(0.22f, 0.42f, 0.2f), 0.05f);
            for (int i = 0; i < 24; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                float dist = Mathf.Lerp(14f, 22f, (float)rng.NextDouble());
                var pos = new Vector3(Mathf.Sin(angle) * dist, 0, Mathf.Cos(angle) * dist);
                if (Vector3.Distance(pos, lake.transform.localPosition) < 10f) continue;
                CreateTree(meadow, pos, Mathf.Lerp(0.8f, 1.4f, (float)rng.NextDouble()),
                    rng.NextDouble() < 0.4, trunkMat, leafMat);
            }

            // kwiaty w zasięgu wzroku pacjenta
            Color[] petals = { new Color(0.95f, 0.85f, 0.3f), Color.white,
                               new Color(0.85f, 0.4f, 0.45f) };
            for (int i = 0; i < 45; i++)
            {
                var pos = new Vector3(
                    Mathf.Lerp(-8f, 8f, (float)rng.NextDouble()), 0.04f,
                    Mathf.Lerp(-4f, 10f, (float)rng.NextDouble()));
                if (Vector3.Distance(pos, lake.transform.localPosition) < 9f) continue;
                var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "Kwiat";
                flower.transform.SetParent(meadow.transform);
                flower.transform.localPosition = pos;
                flower.transform.localScale = Vector3.one * 0.07f;
                flower.GetComponent<Renderer>().sharedMaterial =
                    CreateMat(petals[i % petals.Length], 0.2f);
            }

            // niskie, ciepłe słońce + niebo + mgiełka
            var sunGo = new GameObject("Slonce");
            sunGo.transform.SetParent(meadow.transform);
            sunGo.transform.rotation = Quaternion.Euler(28f, -140f, 0);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.75f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;

            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.05f);
            sky.SetFloat("_AtmosphereThickness", 0.9f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.7f, 0.85f));
            sky.SetColor("_GroundColor", new Color(0.45f, 0.5f, 0.4f));
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = new Color(0.75f, 0.82f, 0.85f);

            // avatar naprzeciw miejsca pacjenta (pacjent na -Z, patrzy na +Z)
            avatar.transform.SetPositionAndRotation(
                new Vector3(0, 0, 1.5f), Quaternion.Euler(0, 180, 0));

            Undo.RegisterCreatedObjectUndo(meadow, "FaceDrama Polana");
            _report.Add("OK: polana z jeziorem zbudowana (trawa, drzewa, kwiaty, " +
                        "niskie słońce, mgiełka). OVRCameraRig postaw w (0, 0, -1) — docs/01.");
        }

        static void CreateTree(GameObject parent, Vector3 pos, float scale,
            bool conifer, Material trunkMat, Material leafMat)
        {
            var tree = new GameObject("Drzewo");
            tree.transform.SetParent(parent.transform);
            tree.transform.localPosition = pos;
            tree.transform.localScale = Vector3.one * scale;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale = new Vector3(0.4f, 1.5f, 0.4f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

            // iglak = stożek z rozciągniętej kapsuły; liściaste = kula
            var crown = GameObject.CreatePrimitive(
                conifer ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            crown.transform.SetParent(tree.transform, false);
            crown.transform.localPosition = new Vector3(0, conifer ? 4.2f : 4.0f, 0);
            crown.transform.localScale = conifer
                ? new Vector3(1.8f, 2.4f, 1.8f)
                : new Vector3(2.6f, 2.2f, 2.6f);
            crown.GetComponent<Renderer>().sharedMaterial = leafMat;
        }

        static Material CreateMat(Color color, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard"); // projekt bez URP
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color; // Standard shader / fallback
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        // ---- Sceneria: prosty gabinet (wariant zapasowy) ----

        void BuildRoom(GameObject avatar)
        {
            var room = new GameObject("Gabinet");

            CreateBox(room, "Podloga", new Vector3(0, -0.05f, 0), new Vector3(6, 0.1f, 6));
            CreateBox(room, "Sciana_N", new Vector3(0, 1.5f, 3), new Vector3(6, 3, 0.1f));
            CreateBox(room, "Sciana_S", new Vector3(0, 1.5f, -3), new Vector3(6, 3, 0.1f));
            CreateBox(room, "Sciana_E", new Vector3(3, 1.5f, 0), new Vector3(0.1f, 3, 6));
            CreateBox(room, "Sciana_W", new Vector3(-3, 1.5f, 0), new Vector3(0.1f, 3, 6));
            // fotel ZA avatarem (avatar stoi w 0,0,1.2 — wcześniej nachodziły na siebie)
            CreateBox(room, "Fotel", new Vector3(0, 0.25f, 1.8f), new Vector3(0.6f, 0.5f, 0.6f));

            var lightGo = new GameObject("Swiatlo");
            lightGo.transform.SetParent(room.transform);
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f); // ciepłe światło
            light.intensity = 1.1f;

            // avatar naprzeciw miejsca pacjenta (pacjent siedzi na -Z, patrzy na +Z)
            avatar.transform.SetPositionAndRotation(
                new Vector3(0, 0, 1.2f), Quaternion.Euler(0, 180, 0));

            Undo.RegisterCreatedObjectUndo(room, "FaceDrama Gabinet");
            _report.Add("OK: prosty gabinet zbudowany. Pamiętaj o OVRCameraRig " +
                        "z Meta XR SDK w pozycji (0, 0, -1) — patrz docs/01, sekcja 4.");
        }

        static void CreateBox(GameObject parent, string name, Vector3 pos, Vector3 scale)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform);
            box.transform.localPosition = pos;
            box.transform.localScale = scale;
        }

        static void EditorSceneManagerMarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        void ShowReport(bool success)
        {
            string text = string.Join("\n", _report);
            Debug.Log($"[FaceDrama] Raport kreatora:\n{text}");
            EditorUtility.DisplayDialog(
                success ? "FaceDrama — gotowe" : "FaceDrama — błąd",
                text, "OK");
        }
    }
}
