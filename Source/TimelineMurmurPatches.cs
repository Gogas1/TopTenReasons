using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine.Playables;

namespace TopTenReasons {
    [HarmonyPatch(typeof(TimelineMurmur))]
    internal static class TimelineMurmurPatches {

        private static FieldInfo timelineFieldInfo = AccessTools.Field(typeof(TimelineMurmur), "timeline");
        private static FieldInfo isPlayingFieldInfo = AccessTools.Field(typeof(TimelineMurmur), "isPlaying");

        static readonly MethodInfo replacementInstance = AccessTools.Method(typeof(TimelineMurmurPatches), "PlayBubbleTimeline_Replacement", new[] { typeof(TimelineMurmur) });

        private static void PlayBubbleTimeline_Replacement(TimelineMurmur __instance) {
            var timeline = (PlayableDirector)timelineFieldInfo.GetValue(__instance);
            var isPlaying = (bool)isPlayingFieldInfo.GetValue(__instance);

            if (__instance.isPlayed == null || !__instance.isPlayed.FlagValue) {
                if (!isPlaying) {
                    isPlayingFieldInfo.SetValue(__instance, true);
                    timeline.Play();
                }

                return;
            }

            __instance.isPlayed.FlagValue = true;
        }

        [HarmonyPatch(nameof(TimelineMurmur.PlayBubbleTimeline))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler_PlayBubbleTimeline(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>();
            codes.Add(new CodeInstruction(OpCodes.Ldarg_0));

            codes.Add(new CodeInstruction(OpCodes.Callvirt, replacementInstance));
            codes.Add(new CodeInstruction(OpCodes.Ret));

            return codes;
        }
    }
}
