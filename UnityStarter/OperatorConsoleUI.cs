// FaceDrama — Etap 2+ (wymaga: Photon Fusion 2 + Photon Voice 2)
//
// Szkielet konsoli operatora (build Windows): połączenie z pokojem,
// wybór mikrofonu, mute, status. Przyciski emocji (Etap 4) — zaślepki.
//
// Podpięcie: Canvas (uGUI) ze wskazanymi kontrolkami + referencje do
// SessionConnector i Recorder (Photon Voice) w scenie konsoli.

using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace FaceDrama
{
    public class OperatorConsoleUI : MonoBehaviour
    {
        [Header("Sieć")]
        public SessionConnector connector;
        public Recorder voiceRecorder;

        [Header("UI")]
        public Button connectButton;
        public Toggle muteToggle;
        public Dropdown microphoneDropdown;
        public Text statusText;

        void Start()
        {
            // lista mikrofonów systemowych
            microphoneDropdown.ClearOptions();
            foreach (var device in Microphone.devices)
                microphoneDropdown.options.Add(new Dropdown.OptionData(device));
            microphoneDropdown.RefreshShownValue();
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);

            connectButton.onClick.AddListener(OnConnectClicked);
            muteToggle.onValueChanged.AddListener(OnMuteChanged);

            if (Microphone.devices.Length > 0) OnMicrophoneChanged(0);
        }

        async void OnConnectClicked()
        {
            connectButton.interactable = false;
            if (connector.IsConnected)
            {
                await connector.Disconnect();
            }
            else
            {
                await connector.Connect();
            }
            connectButton.interactable = true;
        }

        void OnMicrophoneChanged(int index)
        {
            if (index < 0 || index >= Microphone.devices.Length) return;
            // Photon Voice: zmiana urządzenia restartuje nagrywanie
            voiceRecorder.MicrophoneDevice =
                new Photon.Voice.DeviceInfo(Microphone.devices[index]);
        }

        void OnMuteChanged(bool muted)
        {
            voiceRecorder.TransmitEnabled = !muted;
        }

        void Update()
        {
            if (statusText == null) return;
            statusText.text = connector.IsConnected
                ? $"Połączono: {connector.roomName}" +
                  (voiceRecorder.TransmitEnabled ? "  |  mikrofon: ON" : "  |  WYCISZONY")
                : "Rozłączono";
            connectButton.GetComponentInChildren<Text>().text =
                connector.IsConnected ? "Rozłącz" : "Połącz";
        }

        // ---- Etap 4: zaślepki pod panel terapeutyczny ----

        /// <summary>Nakładka emocji na mimikę (smutek/ciepło/gniew) — do zrobienia w Etapie 4.</summary>
        public void TriggerEmotion(string emotionName)
        {
            // TODO(Etap 4): RPC do avatara nakładające zestaw blend shape'ów emocji
            Debug.Log($"[FaceDrama] Emocja '{emotionName}' — jeszcze niezaimplementowana.");
        }

        /// <summary>Przycisk bezpieczeństwa: natychmiastowe wyciemnienie sceny pacjenta.</summary>
        public void TriggerSafetyFade()
        {
            // TODO(Etap 4): RPC do aplikacji pacjenta — fade-to-black + mute audio.
            // To MUSI powstać przed pierwszą sesją z prawdziwym pacjentem.
            Debug.Log("[FaceDrama] Safety fade — jeszcze niezaimplementowany.");
        }
    }
}
