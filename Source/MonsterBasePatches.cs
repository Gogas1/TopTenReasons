using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TopTenReasons {

    [HarmonyPatch(typeof(MonsterBase))]
    internal static class MonsterBasePatches {

        internal static event Action<MonsterBase>? OnCheckInitPostfix;


        [HarmonyPatch("CheckInit")]
        [HarmonyPostfix]
        private static void CheckInitPostfix(MonsterBase __instance) {
            try {
                OnCheckInitPostfix?.Invoke(__instance);
            } catch (Exception ex) {
                Log.Error($"Failed in MonsterBase.CheckInit postfix: {ex}. \n {ex.StackTrace}");
            }
        }
    }
}
