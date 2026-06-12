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
        string _glbPath = "";
        bool _buildRoom = true;
        bool _setupLipSync = true;
        readonly List<string> _report = new List<string>();

        // Fonem -> (kanał ARKit, waga 0..1). Spójne z docs/03-lipsync.md.
        static readonly (string phoneme, string channel, float weight)[] PhonemeMap =
        {
            ("A", "jawOpen",           0.7f),
            ("I", "mouthStretchLeft",  0.5f),
            ("I", "mouthStretchRight", 0.5f),
            ("I", "jawOpen",           0.15f),
            ("U", "mouthPucker",       0.7f),
            ("U", "mouthFunnel",       0.3f),
            ("E", "jawOpen",           0.4f),
            ("E", "mouthStretchLeft",  0.3f),
            ("E", "mouthStretchRight", 0.3f),
            ("O", "jawOpen",           0.5f),
            ("O", "mouthFunnel",       0.6f),
            ("N", "mouthClose",        0.15f),
        };

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
            _buildRoom = EditorGUILayout.ToggleLeft(
                "Zbuduj prosty gabinet (podłoga, ściany, światło, fotel)", _buildRoom);
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
            if (_buildRoom) BuildRoom(avatar);

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

            var bs = voice.AddComponent<uLipSync.uLipSyncBlendShape>();
            bs.skinnedMeshRenderer = faceMesh;

            var meshMap = ArkitBlendshapeMap.ResolveMeshIndices(faceMesh);
            int mapped = 0;
            foreach (var (phoneme, channel, weight) in PhonemeMap)
            {
                int meshIdx = meshMap[ArkitBlendshapeMap.ChannelIndex(channel)];
                if (meshIdx < 0) continue;
                bs.blendShapes.Add(new uLipSync.uLipSyncBlendShape.BlendShapeInfo
                {
                    phoneme = phoneme,
                    index = meshIdx,
                    maxWeight = weight,
                });
                mapped++;
            }

            // spięcie zdarzeniem (to samo, co ręczne przeciągnięcie w Inspectorze)
            UnityEventTools.AddPersistentListener(ls.onLipSyncUpdate, bs.OnLipSyncUpdate);

            _report.Add($"OK: lip sync skonfigurowany ({mapped} mapowań fonem→blend shape). " +
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

        void BuildRoom(GameObject avatar)
        {
            var room = new GameObject("Gabinet");

            CreateBox(room, "Podloga", new Vector3(0, -0.05f, 0), new Vector3(6, 0.1f, 6));
            CreateBox(room, "Sciana_N", new Vector3(0, 1.5f, 3), new Vector3(6, 3, 0.1f));
            CreateBox(room, "Sciana_S", new Vector3(0, 1.5f, -3), new Vector3(6, 3, 0.1f));
            CreateBox(room, "Sciana_E", new Vector3(3, 1.5f, 0), new Vector3(0.1f, 3, 6));
            CreateBox(room, "Sciana_W", new Vector3(-3, 1.5f, 0), new Vector3(0.1f, 3, 6));
            CreateBox(room, "Fotel", new Vector3(0, 0.25f, 1.2f), new Vector3(0.6f, 0.5f, 0.6f));

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
