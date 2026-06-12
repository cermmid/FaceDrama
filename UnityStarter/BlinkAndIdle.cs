// FaceDrama — Etap 1+ (bez zależności zewnętrznych)
//
// "Oznaki życia" avatara: automatyczne mruganie i mikroruch brwi, gdy nie ma
// świeżych danych z face capture. W Etapie 1-2 działa ciągle; w Etapie 3
// FaceCaptureReceiver woła NotifyExternalData() przy każdej klatce capture
// i ten komponent się wycofuje, dopóki dane płyną.
//
// Podpięcie: na obiekt avatara; wskaż SkinnedMeshRenderer głowy.

using UnityEngine;

namespace FaceDrama
{
    public class BlinkAndIdle : MonoBehaviour
    {
        public SkinnedMeshRenderer faceMesh;

        [Tooltip("Po ilu sekundach bez danych capture przejmujemy kontrolę.")]
        public float takeoverAfterSeconds = 0.5f;

        [Header("Mruganie")]
        public Vector2 blinkIntervalRange = new Vector2(2.5f, 6f);
        public float blinkDuration = 0.12f;

        [Header("Mikroruchy brwi")]
        [Range(0f, 30f)] public float browNoiseAmount = 8f;

        int[] _map;
        int _chBlinkL, _chBlinkR, _chBrowInnerUp;
        float _lastExternalDataTime = -999f;
        float _nextBlinkTime;
        float _blinkStartTime = -999f;

        void Start()
        {
            if (faceMesh == null) faceMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            _map = ArkitBlendshapeMap.ResolveMeshIndices(faceMesh);
            _chBlinkL = ArkitBlendshapeMap.ChannelIndex("eyeBlinkLeft");
            _chBlinkR = ArkitBlendshapeMap.ChannelIndex("eyeBlinkRight");
            _chBrowInnerUp = ArkitBlendshapeMap.ChannelIndex("browInnerUp");
            ScheduleNextBlink();
        }

        /// <summary>Woła FaceCaptureReceiver, gdy przyszła świeża klatka mimiki.</summary>
        public void NotifyExternalData() => _lastExternalDataTime = Time.time;

        bool IdleActive => Time.time - _lastExternalDataTime > takeoverAfterSeconds;

        void LateUpdate()
        {
            if (!IdleActive) return;

            // mruganie: szybkie zamknięcie i otwarcie
            if (Time.time >= _nextBlinkTime)
            {
                _blinkStartTime = Time.time;
                ScheduleNextBlink();
            }
            float t = (Time.time - _blinkStartTime) / blinkDuration;
            float blink = t < 1f ? Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 100f : 0f;
            SetWeight(_chBlinkL, blink);
            SetWeight(_chBlinkR, blink);

            // delikatny "oddech" brwi, żeby twarz nie była martwa
            float brow = (Mathf.PerlinNoise(Time.time * 0.3f, 0.5f) - 0.5f) * 2f * browNoiseAmount;
            SetWeight(_chBrowInnerUp, Mathf.Max(0f, brow));
        }

        void ScheduleNextBlink() =>
            _nextBlinkTime = Time.time + Random.Range(blinkIntervalRange.x, blinkIntervalRange.y);

        void SetWeight(int channel, float weight)
        {
            if (channel < 0) return;
            int meshIdx = _map[channel];
            if (meshIdx >= 0) faceMesh.SetBlendShapeWeight(meshIdx, weight);
        }
    }
}
