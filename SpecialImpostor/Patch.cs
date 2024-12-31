using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace SpecialImpostor
{
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
            tempVent.enabled = false;

            foreach (var pc in PlayerControl.AllPlayerControls.ToArray().Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId))
            {
                var vent = pc.gameObject.AddComponent<Vent>();
                vent.Id = int.MinValue + pc.PlayerId;
                vent.myAnim = tempVent.myAnim;
                vent.Buttons = new(Array.Empty<ButtonBehavior>());
                vent.myRend = pc.GetComponent<SpriteRenderer>(); // This may be useless, but keeping it is not a bad idea

                Main.Logger.LogInfo(pc.PlayerId);
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

        [HarmonyPatch(nameof(Vent.CanUse))]
        [HarmonyPrefix]
        static bool CanUsePatch(Vent __instance, ref bool canUse, ref bool couldUse, ref float __result)
        {
            if (__instance.IsModded() && !__instance.enabled)
            {
                canUse = couldUse = false;
                __result = float.MinValue;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics._CoEnterVent_d__55), nameof(PlayerPhysics._CoEnterVent_d__55.MoveNext))]
    internal static class EnterVentAnimationPatch
    {
        static void Postfix(PlayerPhysics._CoEnterVent_d__55 __instance, bool __result)
        {
            if (!__result)
            {
                if (__instance.__4__this.myPlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    if (__instance._vent_5__2.IsModded())
                    {
                        PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(__instance._vent_5__2.Id);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics._CoExitVent_d__56), nameof(PlayerPhysics._CoExitVent_d__56.MoveNext))]
    internal static class ExitVentAnimationPatch
    {
        static void Postfix(PlayerPhysics._CoExitVent_d__56 __instance, bool __result)
        {
            if (!__result)
            {
                var id = __instance.id;
                var list = ShipStatus.Instance.AllVents.ToList();
                var vent = list.FirstOrDefault(v => v.Id == id);
                if (!vent) return;
                if (!vent.IsModded()) return;

                list.Remove(vent);
                ShipStatus.Instance.AllVents = list.ToArray();
                vent.enabled = false;

                if (__instance.__4__this.myPlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    var target = vent.GetModdedInfo();
                    if (target)
                        PlayerControl.LocalPlayer.CmdCheckMurder(target);

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
            }
        }
    }
}