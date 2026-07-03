// FaceDrama — Etap 1 (bez zależności zewnętrznych)
//
// Avatar z Avaturn przychodzi w T-pose i stoi jak manekin. Ten komponent:
//  1) na starcie (Play) układa ręce wzdłuż tułowia — naturalna, rozluźniona
//     postawa; liczone geometrycznie (FromToRotation na kierunkach kości),
//     więc działa niezależnie od lokalnych osi konkretnego riga,
//  2) animuje delikatny "idle" żywego człowieka: oddech klatką piersiową,
//     powolne przenoszenie ciężaru ciała, mikroruchy głowy.
//
// Bez Animatora i bez zewnętrznych animacji. Wyższa jakość później:
// animacje idle z Mixamo (rig Avaturn jest z nimi zgodny) — docs/06, Etap 4.
//
// Podpięcie: na korzeń avatara (kreator robi to sam). Kości znajdowane po
// nazwach Mixamo/Avaturn: Hips, Spine/Chest, Head, LeftArm, LeftForeArm...

using UnityEngine;

namespace FaceDrama
{
    public class IdleBodyMotion : MonoBehaviour
    {
        [Header("Postawa (stosowana raz, na starcie)")]
        [Tooltip("Opuszczenie barków/obojczyków (stopnie) — T-pose trzyma je uniesione.")]
        [Range(0f, 15f)] public float shoulderDrop = 6f;
        [Tooltip("Odchylenie rąk od tułowia w stopniach (0 = ręce pionowo w dół).")]
        [Range(0f, 40f)] public float armOutwardTilt = 10f;
        [Tooltip("Lekki dryf rąk do przodu (stopnie) — rozluźnione ręce nie wiszą idealnie w pionie.")]
        [Range(0f, 15f)] public float armForwardDrift = 5f;
        [Tooltip("Zgięcie łokci do przodu (stopnie).")]
        [Range(0f, 35f)] public float forearmBend = 18f;
        [Tooltip("Zwinięcie palców (stopnie na paliczek) — 0 = rozcapierzona 'deska'.")]
        [Range(0f, 30f)] public float fingerCurl = 14f;

        [Header("Idle")]
        [Tooltip("Długość cyklu oddechu (s).")]
        public float breathCycle = 4.5f;
        [Range(0f, 5f)] public float breathAmount = 1.6f;
        [Tooltip("Przenoszenie ciężaru — kołysanie bioder (stopnie).")]
        [Range(0f, 4f)] public float swayAmount = 1.2f;
        [Tooltip("Mikroruchy głowy (stopnie).")]
        [Range(0f, 6f)] public float headAmount = 2.5f;
        [Tooltip("Etap 3: FaceCaptureReceiver wyłącza to, gdy głową steruje operator.")]
        public bool headMotionEnabled = true;

        Transform _hips, _chest, _head;
        Quaternion _restHips, _restChest, _restHead;
        const float Seed = 13.7f; // deterministyczny szum — avatar zawsze "ten sam"

        void Start()
        {
            _hips = FindBone("Hips");
            _chest = FindBone("Spine2") ?? FindBone("Chest")
                  ?? FindBone("Spine1") ?? FindBone("Spine");
            _head = FindBone("Head");

            PoseShoulder("LeftShoulder", "LeftArm");
            PoseShoulder("RightShoulder", "RightArm");
            PoseArm("LeftArm", "LeftForeArm", "LeftHand");
            PoseArm("RightArm", "RightForeArm", "RightHand");

            if (_hips != null) _restHips = _hips.localRotation;
            if (_chest != null) _restChest = _chest.localRotation;
            if (_head != null) _restHead = _head.localRotation;

            if (_chest == null || _head == null)
                Debug.LogWarning("[FaceDrama] IdleBodyMotion: nie znalazłem części kości " +
                                 "(Spine/Head) — idle będzie częściowy. Sprawdź nazwy w rigu.");
        }

        Transform FindBone(string suffix)
        {
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            return null;
        }

        // Opuszcza obojczyk: kierunek "bark -> ramię" pochylamy w dół o shoulderDrop.
        // Bez tego barki zostają uniesione jak w T-pose (efekt "wzruszonych ramion").
        void PoseShoulder(string shoulderName, string upperName)
        {
            var shoulder = FindBone(shoulderName);
            var upper = FindBone(upperName);
            if (shoulder == null || upper == null) return;

            Vector3 dir = (upper.position - shoulder.position).normalized;
            Vector3 target = (dir + Vector3.down * Mathf.Tan(shoulderDrop * Mathf.Deg2Rad))
                .normalized;
            shoulder.rotation = Quaternion.FromToRotation(dir, target) * shoulder.rotation;
        }

        // Układa rękę z T-pose wzdłuż tułowia. Kierunek "ramię -> łokieć"
        // obracamy do celu: prawie pionowo w dół, z lekkim odchyleniem na
        // zewnątrz i minimalnie do przodu (rozluźniona ręka nie jest pionem).
        void PoseArm(string upperName, string forearmName, string handName)
        {
            var upper = FindBone(upperName);
            var forearm = FindBone(forearmName);
            if (upper == null || forearm == null) return;

            Vector3 dir = (forearm.position - upper.position).normalized;
            Vector3 outward = new Vector3(dir.x, 0f, dir.z).normalized;
            Vector3 target = (Vector3.down
                              + outward * Mathf.Tan(armOutwardTilt * Mathf.Deg2Rad)
                              + transform.forward * Mathf.Tan(armForwardDrift * Mathf.Deg2Rad))
                .normalized;
            upper.rotation = Quaternion.FromToRotation(dir, target) * upper.rotation;

            var hand = FindBone(handName);
            if (hand == null) return;
            Vector3 fdir = (hand.position - forearm.position).normalized;
            Vector3 ftarget = (fdir + transform.forward * Mathf.Tan(forearmBend * Mathf.Deg2Rad))
                .normalized;
            forearm.rotation = Quaternion.FromToRotation(fdir, ftarget) * forearm.rotation;

            CurlFingers(hand);
        }

        // Delikatnie zwija palce (bez kciuka). Każdy paliczek pochylamy w stronę
        // ciała (dłonie po pozowaniu wiszą przy udach, wnętrzem do ciała), więc
        // "w stronę bioder" ≈ kierunek zginania. Geometrycznie, bez założeń o osiach.
        void CurlFingers(Transform hand)
        {
            if (fingerCurl <= 0f || _hips == null) return;

            foreach (var bone in hand.GetComponentsInChildren<Transform>())
            {
                if (bone == hand) continue;
                string n = bone.name;
                bool finger = n.Contains("Index") || n.Contains("Middle")
                           || n.Contains("Ring") || n.Contains("Pinky");
                if (!finger || bone.childCount == 0) continue;

                Vector3 dir = (bone.GetChild(0).position - bone.position).normalized;
                Vector3 towardBody = _hips.position - bone.position;
                towardBody.y = 0f;
                if (towardBody.sqrMagnitude < 1e-6f) continue;
                towardBody.Normalize();

                Vector3 target = (dir + towardBody * Mathf.Tan(fingerCurl * Mathf.Deg2Rad))
                    .normalized;
                bone.rotation = Quaternion.FromToRotation(dir, target) * bone.rotation;
            }
        }

        void LateUpdate()
        {
            float t = Time.time;

            if (_chest != null)
            {
                float breath = Mathf.Sin(t * 2f * Mathf.PI / Mathf.Max(1f, breathCycle))
                               * breathAmount;
                _chest.localRotation = _restChest * Quaternion.Euler(breath, 0f, 0f);
            }

            if (_hips != null)
            {
                float sway = (Mathf.PerlinNoise(t * 0.15f, Seed) - 0.5f) * 2f * swayAmount;
                _hips.localRotation = _restHips * Quaternion.Euler(0f, 0f, sway);
            }

            if (_head != null && headMotionEnabled)
            {
                float yaw = (Mathf.PerlinNoise(t * 0.20f, Seed + 1f) - 0.5f) * 2f * headAmount;
                float pitch = (Mathf.PerlinNoise(t * 0.17f, Seed + 2f) - 0.5f) * 2f
                              * headAmount * 0.6f;
                _head.localRotation = _restHead * Quaternion.Euler(pitch, yaw, 0f);
            }
        }
    }
}
