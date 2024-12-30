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
    public class Plugin : BasePlugin
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

    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    internal static class ModStampPatch
    {
        static void Postfix(ModManager __instance) => __instance.ShowModStamp();
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    internal static class StartGamePatch
    {
        static void Postfix()
        {
            var allVents = ShipStatus.Instance.AllVents.ToList();
            var tempVent = GameObject.Instantiate(allVents[0]);
            tempVent.gameObject.SetActive(false);

            foreach (var pc in PlayerControl.AllPlayerControls.ToArray().Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId))
            {
                var vent = pc.gameObject.AddComponent<Vent>();
                vent.Id = (pc.PlayerId + 1) * 10000;
                vent.myAnim = tempVent.myAnim;
                vent.Buttons = new(Array.Empty<ButtonBehavior>());
                vent.myRend = pc.GetComponent<SpriteRenderer>(); // This may be useless, but keeping it is not a bad idea

                Plugin.Logger.LogInfo(pc.PlayerId);
                allVents.Add(vent);
            }

            ShipStatus.Instance.AllVents = new(allVents.ToArray());
        }
    }

    [HarmonyPatch(typeof(Vent))]
    internal static class VentPatch
    {
        [HarmonyPatch(nameof(Vent.SetOutline))]
        [HarmonyPrefix]
        static bool OutlinePatch(Vent __instance) => !__instance.IsModded();
    }

    [HarmonyPatch(typeof(PlayerPhysics._CoEnterVent_d__55), nameof(PlayerPhysics._CoEnterVent_d__55.MoveNext))]
    internal static class EnterVentAnimationPatch
    {
        //public static int Entered { get; set; } = -1;
        static void Postfix(PlayerPhysics._CoEnterVent_d__55 __instance, bool __result)
        {
            if (!__result)
            {
                if (__instance.__4__this.myPlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    if (__instance._vent_5__2.IsModded())
                    {
                        PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(__instance._vent_5__2.Id);
                        //Entered = __instance._vent_5__2.Id;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(DebugLogHandler), nameof(DebugLogHandler.LogException))]
    internal static class DisableSpamPatch
    {
        static bool Prefix() => false; // Better solution known, apply it days later...
    }

    [HarmonyPatch(typeof(PlayerPhysics._CoExitVent_d__56), nameof(PlayerPhysics._CoExitVent_d__56.MoveNext))]
    internal static class ExitVentAnimationPatch
    {
        static void Postfix(PlayerPhysics._CoExitVent_d__56 __instance, bool __result)
        {
            if (!__result)
            {
                var id = __instance.id/*EnterVentAnimationPatch.Entered*/;
                if (id == -1) return;
                var list = ShipStatus.Instance.AllVents.ToList();
                var vent = list.FirstOrDefault(v => v.Id == id);
                if (!vent) return;
                if (!vent.IsModded()) return;

                list.Remove(vent);
                ShipStatus.Instance.AllVents = list.ToArray();
                GameObject.Destroy(vent);

                var playerId = id / 10000 - 1;
                if (__instance.__4__this.myPlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    var target = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(p => p.PlayerId == playerId);
                    if (target && target.Object)
                        PlayerControl.LocalPlayer.CmdCheckMurder(target.Object);

                    //Dictionary<Collider2D, IUsable[]> dict = new();
                    //foreach (var pair in PlayerControl.LocalPlayer.cache)
                    //{
                    //    var newValue = pair.Value.ToList();
                    //    newValue.RemoveAll(i => i == null);
                    //    dict[pair.Key] = newValue.ToArray();
                    //}

                    //foreach(var (col, arr) in dict)
                    //    PlayerControl.LocalPlayer.cache[col] = new(arr);
                }

                //EnterVentAnimationPatch.Entered = -1;
            }
        }
    }

    public static class ModHelper
    {
        public static bool IsModded(this Vent vent) => vent.Id >= 10000;
    }
}
