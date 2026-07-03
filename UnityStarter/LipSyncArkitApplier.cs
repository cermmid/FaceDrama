// FaceDrama — Etap 1 (wymaga: uLipSync)
//
// Nakłada wynik analizy uLipSync na blend shape'y ARKit avatara.
// Zamiennik komponentu uLipSyncBlendShape z dwóch powodów:
//  1) sam wykrywa zakres wag mesha (GLB/glTFast: 0..1, FBX: 0..100) —
//     uLipSyncBlendShape zakłada 0..100 i na avatarze z GLB "wybucha" siatka,
//  2) jeden fonem może sterować kilkoma kanałami ARKit naraz (tabela niżej).
//
// Podpięcie (robi to kreator FaceDrama -> Kreator konfiguracji):
//  - komponent na obiekcie z uLipSync (tam, gdzie AudioSource głosu),
//  - pole faceMesh -> SkinnedMeshRenderer twarzy avatara,
//  - w komponencie uLipSync zdarzenie On Lip Sync Update -> OnLipSyncUpdate.

using UnityEngine;
using uLipSync;

namespace FaceDrama
{
    public class LipSyncArkitApplier : MonoBehaviour
    {
        public SkinnedMeshRenderer faceMesh;

        [Tooltip("Wygładzanie ust (stała czasowa, s).")]
        public float smoothingTime = 0.06f;

        // uLipSync podaje SUROWĄ głośność RMS (zwykle 0.001..0.1) — normalizujemy
        // ją logarytmicznie do 0..1, jak robi to oryginalny uLipSyncBlendShape.
        [Header("Czułość na głośność (skala log10)")]
        [Tooltip("Głośność (log10), przy której usta zaczynają się otwierać. Za słaba reakcja -> obniż (np. -3.5).")]
        [Range(-5f, 0f)] public float minVolume = -2.5f;
        [Tooltip("Głośność (log10), przy której usta są w pełni otwarte. Za słaba reakcja -> obniż (np. -2).")]
        [Range(-5f, 0f)] public float maxVolume = -1.5f;

        // Fonem -> (kanał ARKit, waga 0..1). Punkt wyjścia — dostrój wg
        // docs/03-lipsync.md. Ta sama tabela co w LipSyncFallbackBlender (Etap 3).
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

        readonly float[] _target = new float[ArkitBlendshapeMap.Count];
        readonly float[] _smoothed = new float[ArkitBlendshapeMap.Count];
        int[] _map;
        float _weightScale;

        void Start()
        {
            // komponent zwykle siedzi na dziecku "Voice" — twarzy szukaj od korzenia
            if (faceMesh == null)
                faceMesh = transform.root.GetComponentInChildren<SkinnedMeshRenderer>();

            _map = ArkitBlendshapeMap.ResolveMeshIndices(faceMesh);
            _weightScale = ArkitBlendshapeMap.DetectWeightScale(faceMesh);
        }

        float NormalizedVolume(float rawVolume)
        {
            if (rawVolume < 1e-6f) return 0f;
            float log = Mathf.Log10(rawVolume);
            return Mathf.Clamp01((log - minVolume) / Mathf.Max(0.01f, maxVolume - minVolume));
        }

        /// <summary>Callback zdarzenia On Lip Sync Update komponentu uLipSync.</summary>
        public void OnLipSyncUpdate(LipSyncInfo info)
        {
            for (int i = 0; i < _target.Length; i++) _target[i] = 0f;

            float vol = NormalizedVolume(info.volume);
            if (vol <= 0f) return;

            foreach (var (phoneme, channel, weight) in PhonemeMap)
            {
                if (phoneme != info.phoneme) continue;
                int ch = ArkitBlendshapeMap.ChannelIndex(channel);
                if (ch >= 0)
                    _target[ch] = Mathf.Max(_target[ch], weight * vol);
            }
        }

        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, smoothingTime));

            foreach (int ch in ArkitBlendshapeMap.MouthChannels)
            {
                _smoothed[ch] = Mathf.Lerp(_smoothed[ch], _target[ch], k);
                int meshIdx = _map[ch];
                if (meshIdx >= 0)
                    faceMesh.SetBlendShapeWeight(meshIdx, _smoothed[ch] * _weightScale);
            }
        }
    }
}
