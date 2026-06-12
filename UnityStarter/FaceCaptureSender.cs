// FaceDrama — Etap 3 (wymaga: Photon Fusion 2)
//
// Strona OPERATORA: przyjmuje klatki mimiki (52 współczynniki ARKit 0..1
// + obrót głowy) i publikuje je przez Fusion jako Networked Properties.
// Odbiorem i nałożeniem na avatar zajmuje się FaceCaptureReceiver.
//
// Źródła danych (jedno z dwóch):
//  A) MediaPipeUnityPlugin w tej samej aplikacji — z callbacku wyników wołaj
//     FeedFrame(weights01, headRotation). Nazwy kategorii MediaPipe tłumacz
//     na indeksy przez ArkitBlendshapeMap.ChannelIndex(category.categoryName).
//  B) Zewnętrzny proces (np. Python + mediapipe) przez UDP — włącz
//     useUdpSource. Format pakietu: 56 x float32 little-endian
//     (52 wagi + kwaternion x,y,z,w), razem 224 bajty.
//
//     Minimalny nadajnik w Pythonie:
//       import socket, struct
//       sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
//       sock.sendto(struct.pack('<56f', *weights, qx, qy, qz, qw),
//                   ('127.0.0.1', 9466))
//
// Podpięcie: na prefab avatara (NetworkObject), obok FaceCaptureReceiver.
// Pisze tylko strona ze State Authority (operator — on spawnuje avatar).

using Fusion;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace FaceDrama
{
    public class FaceCaptureSender : NetworkBehaviour
    {
        [Header("Źródło B: UDP (np. capture w Pythonie)")]
        public bool useUdpSource = false;
        public int udpPort = 9466;

        // ---- Stan sieciowy ----
        [Networked, Capacity(ArkitBlendshapeMap.Count)]
        public NetworkArray<float> Weights => default;

        [Networked] public Quaternion HeadRotation { get; set; }

        /// <summary>Rośnie z każdą klatką capture — odbiorca poznaje po tym świeżość danych.</summary>
        [Networked] public int FrameId { get; set; }

        // ---- Bufor ostatniej klatki z dowolnego źródła ----
        readonly float[] _pending = new float[ArkitBlendshapeMap.Count];
        Quaternion _pendingHead = Quaternion.identity;
        volatile bool _hasPending;
        readonly object _lock = new object();

        UdpClient _udp;
        Thread _udpThread;

        /// <summary>Źródło A: wołaj z callbacku MediaPipe (worker thread jest OK).</summary>
        public void FeedFrame(float[] weights01, Quaternion headRotation)
        {
            if (weights01 == null || weights01.Length < ArkitBlendshapeMap.Count) return;
            lock (_lock)
            {
                Array.Copy(weights01, _pending, ArkitBlendshapeMap.Count);
                _pendingHead = headRotation;
                _hasPending = true;
            }
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority && useUdpSource) StartUdpListener();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || !_hasPending) return;

            lock (_lock)
            {
                for (int i = 0; i < ArkitBlendshapeMap.Count; i++)
                    Weights.Set(i, _pending[i]);
                HeadRotation = _pendingHead;
                _hasPending = false;
            }
            FrameId++;
        }

        // ---- UDP ----
        void StartUdpListener()
        {
            _udp = new UdpClient(udpPort);
            _udpThread = new Thread(UdpLoop) { IsBackground = true };
            _udpThread.Start();
            Debug.Log($"[FaceDrama] FaceCaptureSender: nasłuch UDP na porcie {udpPort}");
        }

        void UdpLoop()
        {
            var any = new IPEndPoint(IPAddress.Any, 0);
            var weights = new float[ArkitBlendshapeMap.Count];
            const int expectedBytes = (ArkitBlendshapeMap.Count + 4) * sizeof(float);

            while (true)
            {
                byte[] data;
                try { data = _udp.Receive(ref any); }
                catch (SocketException) { return; } // socket zamknięty przy wyjściu

                if (data.Length != expectedBytes) continue;

                for (int i = 0; i < ArkitBlendshapeMap.Count; i++)
                    weights[i] = BitConverter.ToSingle(data, i * sizeof(float));

                int q = ArkitBlendshapeMap.Count * sizeof(float);
                var head = new Quaternion(
                    BitConverter.ToSingle(data, q),
                    BitConverter.ToSingle(data, q + 4),
                    BitConverter.ToSingle(data, q + 8),
                    BitConverter.ToSingle(data, q + 12));

                FeedFrame(weights, head);
            }
        }

        void OnDestroy()
        {
            _udp?.Close();
        }
    }
}
