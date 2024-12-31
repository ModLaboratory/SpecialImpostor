using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpecialImpostor
{
    [BepInPlugin("com.modlaboratory.specialimpostor", "SpecialImpostor", "1.0.0")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        static Harmony Harmony { get; set; }
        public static ManualLogSource Logger { get; set; }
        public override void Load()
        {
            Harmony = new("com.modlaboratory.specialimpostor");
            Harmony.PatchAll();
            Logger = Log;
        }
    }
}
