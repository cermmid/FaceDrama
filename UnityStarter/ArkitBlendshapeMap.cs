// FaceDrama — Etap 1+ (bez zależności zewnętrznych)
//
// Kanoniczna lista 52 blend shape'ów ARKit + narzędzie do odpornego
// odnajdywania ich indeksów na SkinnedMeshRendererze avatara.
//
// Avatary Avaturn (i inne) często prefiksują nazwy (np. "blendShape1.jawOpen",
// "Face.jawOpen"), a MediaPipe zwraca czyste nazwy ("jawOpen") — dlatego
// dopasowujemy po sufiksie, bez rozróżniania wielkości liter.
//
// MediaPipe Face Landmarker zwraca 52 kategorie: "_neutral" + 51 nazw ARKit
// (bez "tongueOut"). Zawsze mapuj po NAZWIE kategorii, nigdy po indeksie.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FaceDrama
{
    public static class ArkitBlendshapeMap
    {
        public static readonly string[] Names =
        {
            "eyeBlinkLeft", "eyeLookDownLeft", "eyeLookInLeft", "eyeLookOutLeft",
            "eyeLookUpLeft", "eyeSquintLeft", "eyeWideLeft",
            "eyeBlinkRight", "eyeLookDownRight", "eyeLookInRight", "eyeLookOutRight",
            "eyeLookUpRight", "eyeSquintRight", "eyeWideRight",
            "jawForward", "jawLeft", "jawRight", "jawOpen",
            "mouthClose", "mouthFunnel", "mouthPucker", "mouthLeft", "mouthRight",
            "mouthSmileLeft", "mouthSmileRight", "mouthFrownLeft", "mouthFrownRight",
            "mouthDimpleLeft", "mouthDimpleRight", "mouthStretchLeft", "mouthStretchRight",
            "mouthRollLower", "mouthRollUpper", "mouthShrugLower", "mouthShrugUpper",
            "mouthPressLeft", "mouthPressRight",
            "mouthLowerDownLeft", "mouthLowerDownRight",
            "mouthUpperUpLeft", "mouthUpperUpRight",
            "browDownLeft", "browDownRight", "browInnerUp",
            "browOuterUpLeft", "browOuterUpRight",
            "cheekPuff", "cheekSquintLeft", "cheekSquintRight",
            "noseSneerLeft", "noseSneerRight",
            "tongueOut",
        };

        public const int Count = 52; // Names.Length

        /// <summary>Kanały ust — te kanały kontroluje LipSyncFallbackBlender.</summary>
        public static readonly HashSet<int> MouthChannels = BuildMouthChannels();

        static HashSet<int> BuildMouthChannels()
        {
            var set = new HashSet<int>();
            for (int i = 0; i < Names.Length; i++)
            {
                string n = Names[i];
                if (n.StartsWith("mouth", StringComparison.Ordinal) ||
                    n.StartsWith("jaw", StringComparison.Ordinal) ||
                    n == "tongueOut")
                {
                    set.Add(i);
                }
            }
            return set;
        }

        /// <summary>Indeks kanonicznego kanału ARKit po nazwie (np. z MediaPipe), -1 gdy brak.</summary>
        public static int ChannelIndex(string arkitName)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                if (string.Equals(Names[i], arkitName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Dla danego mesha zwraca tablicę: indeks kanału ARKit → indeks blend shape'a
        /// na meshu (-1, gdy mesh nie ma danego kanału). Dopasowanie po sufiksie nazwy.
        /// </summary>
        public static int[] ResolveMeshIndices(SkinnedMeshRenderer smr)
        {
            var result = new int[Count];
            for (int i = 0; i < Count; i++) result[i] = -1;

            var mesh = smr.sharedMesh;
            for (int meshIdx = 0; meshIdx < mesh.blendShapeCount; meshIdx++)
            {
                string meshName = mesh.GetBlendShapeName(meshIdx);
                for (int ch = 0; ch < Count; ch++)
                {
                    if (result[ch] >= 0) continue;
                    if (meshName.EndsWith(Names[ch], StringComparison.OrdinalIgnoreCase))
                    {
                        result[ch] = meshIdx;
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>Diagnostyka: wypisuje w konsoli, których kanałów ARKit brakuje na meshu.</summary>
        public static void LogMissingChannels(SkinnedMeshRenderer smr)
        {
            var map = ResolveMeshIndices(smr);
            var missing = new List<string>();
            for (int i = 0; i < Count; i++)
                if (map[i] < 0) missing.Add(Names[i]);

            Debug.Log(missing.Count == 0
                ? $"[FaceDrama] {smr.name}: wszystkie 52 kanały ARKit znalezione."
                : $"[FaceDrama] {smr.name}: brak {missing.Count} kanałów: {string.Join(", ", missing)}");
        }
    }
}
