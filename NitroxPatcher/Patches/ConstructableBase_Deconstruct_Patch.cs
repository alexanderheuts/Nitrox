using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxModel.Core;

namespace NitroxPatcher.Patches
{
    public class ConstructableBase_Deconstruct_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(ConstructableBase);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Deconstruct");

        public static bool Prefix(ConstructableBase __instance)
        {
            if (!__instance._constructed && __instance.constructedAmount > 0)
            {
                NitroxServiceLocator.LocateService<Building>().ChangeConstructionAmount(__instance.gameObject, typeof(ConstructableBase), __instance.constructedAmount);
            }

            return true;
        }

        public static void Postfix(ConstructableBase __instance, bool __result)
        {
            if (__result && __instance.constructedAmount <= 0f)
            {
                NitroxServiceLocator.LocateService<Building>().SetState(__instance.gameObject, typeof(ConstructableBase), false, true);
                NitroxServiceLocator.LocateService<Building>().DeconstructionComplete(__instance.gameObject, typeof(ConstructableBase));
            }
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchMultiple(harmony, TARGET_METHOD, true, true, false);
        }
    }
}
