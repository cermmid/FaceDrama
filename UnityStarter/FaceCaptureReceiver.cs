// FaceDrama — Etap 3 (wymaga: Photon Fusion 2)
//
// Strona PACJENTA (Quest): czyta mimikę z FaceCaptureSender (Networked
// Properties), wygładza i nakłada na blend shape'y avatara oraz na kość głowy.
//
// Podział odpowiedzialności za kanały UST:
//  - jeśli na obiekcie jest LipSyncFallbackBlender, kanały ust zostawiamy jemu
//    (on miksuje capture z audio lip sync),
//  - w przeciwnym razie nakładamy też usta (czyste capture).
//
// Podpięcie: na prefab avatara (NetworkObject), obok FaceCaptureSender.
// Wskaż SkinnedMeshRenderer głowy i (opcjonalnie) kość głowy/szyi.

using Fusion;
using UnityEngine;

namespace FaceDrama
{
    public class FaceCaptureReceiver : NetworkBehaviour
    {
        public SkinnedMeshRenderer faceMesh;

        [Tooltip("Kość głowy avatara (opcjonalnie) — dostaje obrót głowy operatora.")]
        public Transform headBone;
        [Tooltip("Maksymalny obrót głowy w stopniach (avatar siedzi, bez przesady).")]
        public float headRotationLimit = 20f;

        [Tooltip("Wygładzanie (stała czasowa, s). Mniejsze = szybciej, bardziej nerwowo.")]
        public float smoothingTime = 0.08f;

        [Tooltip("Mnożnik ekspresji — MediaPipe bywa zachowawczy. 1.0 = bez zmian.")]
        public float expressionGain = 1.2f;

        [Tooltip("Mruganie poniżej tego progu (0..1) zerujemy — redukcja szumu trackingu.")]
        public float blinkDeadzone = 0.1f;

        /// <summary>Wygładzone wagi 0..1 — czyta z nich też LipSyncFallbackBlender.</summary>
        public float[] SmoothedWeights { get; } = new float[ArkitBlendshapeMap.Count];

        /// <summary>Czy dane capture są świeże (sender nadał klatkę w ciągu ostatnich 0,5 s).</summary>
        public bool HasFreshData => Time.time - _lastFrameTime < 0.5f;

        FaceCaptureSender _sender;
        LipSyncFallbackBlender _mouthBlender;
        BlinkAndIdle _idle;
        int[] _map;
        int _chBlinkL, _chBlinkR;
        int _lastSeenFrameId = -1;
        float _lastFrameTime = -999f;
        float _weightScale;
        Quaternion _headBoneRestRotation;

        public override void Spawned()
        {
            _sender = GetComponent<FaceCaptureSender>();
            _mouthBlender = GetComponent<LipSyncFallbackBlender>();
            _idle = GetComponent<BlinkAndIdle>();

            if (faceMesh == null) faceMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            _map = ArkitBlendshapeMap.ResolveMeshIndices(faceMesh);
            _weightScale = ArkitBlendshapeMap.DetectWeightScale(faceMesh);
            ArkitBlendshapeMap.LogMissingChannels(faceMesh);

            _chBlinkL = ArkitBlendshapeMap.ChannelIndex("eyeBlinkLeft");
            _chBlinkR = ArkitBlendshapeMap.ChannelIndex("eyeBlinkRight");
            if (headBone != null) _headBoneRestRotation = headBone.localRotation;
        }

        // Render() w Fusion = raz na klatkę renderowania, po aktualizacji stanu sieci.
        public override void Render()
        {
            if (_sender == null) return;

            if (_sender.FrameId != _lastSeenFrameId)
            {
                _lastSeenFrameId = _sender.FrameId;
                _lastFrameTime = Time.time;
                _idle?.NotifyExternalData();
            }
            if (!HasFreshData) return; // BlinkAndIdle przejmie twarz

            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, smoothingTime));

            for (int ch = 0; ch < ArkitBlendshapeMap.Count; ch++)
            {
                float target = Mathf.Clamp01(_sender.Weights.Get(ch) * expressionGain);
                if ((ch == _chBlinkL || ch == _chBlinkR) && target < blinkDeadzone) target = 0f;

                SmoothedWeights[ch] = Mathf.Lerp(SmoothedWeights[ch], target, k);

                bool mouthHandledElsewhere =
                    _mouthBlender != null && ArkitBlendshapeMap.MouthChannels.Contains(ch);
                if (mouthHandledElsewhere) continue;

                int meshIdx = _map[ch];
                if (meshIdx >= 0)
                    faceMesh.SetBlendShapeWeight(meshIdx, SmoothedWeights[ch] * _weightScale);
            }

            if (headBone != null)
            {
                var target = ClampRotation(_sender.HeadRotation, headRotationLimit);
                headBone.localRotation = Quaternion.Slerp(
                    headBone.localRotation, _headBoneRestRotation * target, k);
            }
        }

        static Quaternion ClampRotation(Quaternion q, float maxDegrees)
        {
            q.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, -maxDegrees, maxDegrees);
            return Quaternion.AngleAxis(angle, axis);
        }
    }
}
