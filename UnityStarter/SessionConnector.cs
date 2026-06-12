// FaceDrama — Etap 2 (wymaga: Photon Fusion 2)
//
// Minimalne połączenie do wspólnego pokoju Photon w trybie Shared.
// Obie aplikacje (Quest pacjenta i konsola operatora) używają tego samego
// komponentu; różni je tylko flaga isOperator (operator dostaje rolę
// nadawcy mimiki w Etapie 3).
//
// Podpięcie: pusty GameObject "Network" w scenie; App ID Fusion ustawia się
// w Fusion -> Realtime Settings (nie w tym pliku).

using Fusion;
using System.Threading.Tasks;
using UnityEngine;

namespace FaceDrama
{
    public class SessionConnector : MonoBehaviour
    {
        [Tooltip("Nazwa pokoju wspólna dla pary pacjent-operator.")]
        public string roomName = "facedrama-gabinet-1";

        [Tooltip("Zaznacz w buildzie konsoli operatora.")]
        public bool isOperator = false;

        [Tooltip("Połącz automatycznie po starcie (pacjent). Operator łączy się przyciskiem.")]
        public bool connectOnStart = true;

        [Tooltip("Prefab sieciowy avatara (NetworkObject) — spawnuje go operator.")]
        public NetworkObject avatarPrefab;

        public NetworkRunner Runner { get; private set; }
        public bool IsConnected => Runner != null && Runner.IsRunning;

        async void Start()
        {
            if (connectOnStart && !isOperator) await Connect();
        }

        public async Task Connect()
        {
            if (IsConnected) return;

            Runner = gameObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = false; // sterujemy stanem, nie inputem gracza

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                // TODO: dodaj NetworkSceneManagerDefault, jeśli będziesz
                // synchronizować sceny; na razie obie strony ładują swoje sceny same.
            });

            if (!result.Ok)
            {
                Debug.LogError($"[FaceDrama] Połączenie nieudane: {result.ShutdownReason}");
                return;
            }

            Debug.Log($"[FaceDrama] Połączono do pokoju '{roomName}' jako " +
                      (isOperator ? "OPERATOR" : "PACJENT"));

            // Avatar spawnuje operator — wtedy ma nad nim State Authority
            // i może pisać do Networked Properties (mimika, Etap 3).
            if (isOperator && avatarPrefab != null)
                Runner.Spawn(avatarPrefab);
        }

        public async Task Disconnect()
        {
            if (Runner != null) await Runner.Shutdown();
            Runner = null;
        }
    }
}
