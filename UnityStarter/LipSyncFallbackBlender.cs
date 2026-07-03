// FaceDrama — Etap 3 (wymaga: uLipSync + Photon Fusion 2)
//
// Decyduje, co steruje USTAMI avatara:
//  - CaptureOnly — usta w 100% z face capture (kamera operatora),
//  - Blend (domyślny) — kanał po kanale max(capture, audio-lip-sync);
//    gdy kamera zgubi twarz, usta dalej ruszają się z głosu,
//  - AudioOnly — jak w Etapie 2 (czysty uLipSync).
//
// Zamiast komponentu uLipSyncBlendShape sami nakładamy wagi ust, bo musimy
// je zmiksować z capture w jednym miejscu (dwa komponenty piszące w te same
// blend shape'y nadpisywałyby się nawzajem).
//
// Podpięcie:
//  1) na obiekcie avatara obok FaceCaptureReceiver,
//  2) komponent uLipSync musi siedzieć na obiekcie z AudioSource głosu
//     operatora (Speaker z Photon Voice) — wskaż go w polu lipSync,
//  3) w komponencie uLipSync dodaj callback On Lip Sync Update -> ten skrypt,
//     metoda OnLipSyncUpdate (albo zostaw — podepniemy się w Start()).

using UnityEngine;
using uLipSync;

namespace FaceDrama
{
    public class LipSyncFallbackBlender : MonoBehaviour
    {
        public enum MouthMode { CaptureOnly, Blend, AudioOnly }

        public MouthMode mode = MouthMode.Blend;

        [Tooltip("Komponent uLipSync analizujący audio głosu operatora.")]
        public uLipSync.uLipSync lipSync;

        public SkinnedMeshRenderer faceMesh;

        [Tooltip("Wygładzanie ust z audio (s).")]
        public float audioSmoothingTime = 0.06f;

        [Tooltip("Minimalna głośność (0..1), poniżej której fonem ignorujemy.")]
        public float volumeThreshold = 0.01f;

        // Wagi docelowe ust wyliczone z fonemu uLipSync (indeks = kanał ARKit).
        readonly float[] _audioTarget = new float[ArkitBlendshapeMap.Count];
        readonly float[] _audioSmoothed = new float[ArkitBlendshapeMap.Count];

        FaceCaptureReceiver _capture;
        int[] _map;
        float _weightScale;

        // Mapowanie fonem -> (kanał ARKit, waga 0..1). Punkt wyjścia — patrz
        // docs/03-lipsync.md, dostrój do swojego avatara i profilu kalibracji.
        static readonly (string phoneme, string channel, float weight)[] PhonemeMap =
        {
            ("A", "jawOpen",          0.7f),
            ("I", "mouthStretchLeft", 0.5f),
            ("I", "mouthStretchRight",0.5f),
            ("I", "jawOpen",          0.15f),
            ("U", "mouthPucker",      0.7f),
            ("U", "mouthFunnel",      0.3f),
            ("E", "jawOpen",          0.4f),
            ("E", "mouthStretchLeft", 0.3f),
            ("E", "mouthStretchRight",0.3f),
            ("O", "jawOpen",          0.5f),
            ("O", "mouthFunnel",      0.6f),
            ("N", "mouthClose",       0.15f),
            ("S", "mouthClose",       0.1f),
        };

        void Start()
        {
            _capture = GetComponent<FaceCaptureReceiver>();
            if (faceMesh == null) faceMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            _map = ArkitBlendshapeMap.ResolveMeshIndices(faceMesh);
            _weightScale = ArkitBlendshapeMap.DetectWeightScale(faceMesh);

            if (lipSync != null)
                lipSync.onLipSyncUpdate.AddListener(OnLipSyncUpdate);
            else
                Debug.LogWarning("[FaceDrama] LipSyncFallbackBlender: brak referencji do uLipSync.");
        }

        /// <summary>Callback uLipSync — przelicza fonem na wagi kanałów ARKit.</summary>
        public void OnLipSyncUpdate(LipSyncInfo info)
        {
            for (int i = 0; i < _audioTarget.Length; i++) _audioTarget[i] = 0f;
            if (info.volume < volumeThreshold) return;

            foreach (var (phoneme, channel, weight) in PhonemeMap)
            {
                if (phoneme != info.phoneme) continue;
                int ch = ArkitBlendshapeMap.ChannelIndex(channel);
                if (ch >= 0)
                    _audioTarget[ch] = Mathf.Max(_audioTarget[ch],
                        weight * Mathf.Clamp01(info.volume));
            }
        }

        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, audioSmoothingTime));
            bool captureFresh = _capture != null && _capture.HasFreshData;

            foreach (int ch in ArkitBlendshapeMap.MouthChannels)
            {
                _audioSmoothed[ch] = Mathf.Lerp(_audioSmoothed[ch], _audioTarget[ch], k);

                float captureW = captureFresh ? _capture.SmoothedWeights[ch] : 0f;
                float final = mode switch
                {
                    MouthMode.CaptureOnly => captureW,
                    MouthMode.AudioOnly => _audioSmoothed[ch],
                    _ => captureFresh
                        ? Mathf.Max(captureW, _audioSmoothed[ch])
                        : _audioSmoothed[ch], // kamera zgubiła twarz -> audio
                };

                int meshIdx = _map[ch];
                if (meshIdx >= 0) faceMesh.SetBlendShapeWeight(meshIdx, final * _weightScale);
            }
        }
    }
}
