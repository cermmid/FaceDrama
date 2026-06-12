// FaceDrama — Etap 1 (bez zależności zewnętrznych)
//
// Podaje sygnał z mikrofonu do AudioSource w pętli — do lokalnych testów
// uLipSync (mówisz do mikrofonu, avatar rusza ustami), zanim wejdzie sieć.
//
// Podpięcie: na obiekt z AudioSource (ten sam, na którym siedzi uLipSync).
// W Etapie 2 ten komponent wyłączamy — źródłem audio staje się Photon Voice
// (Speaker), a uLipSync czyta z tego samego AudioSource bez zmian.

using System.Collections;
using UnityEngine;

namespace FaceDrama
{
    [RequireComponent(typeof(AudioSource))]
    public class MicAudioSource : MonoBehaviour
    {
        [Tooltip("Pusta = domyślny mikrofon systemowy.")]
        public string deviceName = "";

        [Tooltip("Wycisz odsłuch (uLipSync nadal analizuje audio).")]
        public bool muteOutput = false;

        const int SampleRate = 16000; // wystarcza dla mowy, mniejszy koszt na Quest

        IEnumerator Start()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[FaceDrama] Brak mikrofonu w systemie.");
                yield break;
            }

            var source = GetComponent<AudioSource>();
            source.clip = Microphone.Start(deviceName, true, 1, SampleRate);
            source.loop = true;

            // czekaj aż mikrofon faktycznie ruszy, inaczej będzie cisza/echo
            while (Microphone.GetPosition(deviceName) <= 0) yield return null;

            source.Play();
            // mute przez volume=0 zamiast source.mute — przy mute Unity potrafi
            // pominąć OnAudioFilterRead, a z niego czyta uLipSync
            source.volume = muteOutput ? 0f : 1f;
        }

        void OnDestroy()
        {
            if (Microphone.IsRecording(deviceName)) Microphone.End(deviceName);
        }
    }
}
