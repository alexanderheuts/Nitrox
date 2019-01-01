using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxModel.Core;

namespace NitroxPatcher.Patches
{
    public class Constructable_Construct_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(Constructable);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Construct");

        public static bool Prefix(Constructable __instance)
        {
            if(__instance is ConstructableBase)
            {
                // Check to make sure that we don't call ChangeConstructionAmount twice due to inheritance.
                return true;
            }

            if (!__instance._constructed && __instance.constructedAmount < 1.0f && __instance.constructedAmount > 0f)
            {
                NitroxServiceLocator.LocateService<Building>().ChangeConstructionAmount(__instance.gameObject, typeof(Constructable), __instance.constructedAmount);
            }

            return true;
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPrefix(harmony, TARGET_METHOD);
        }
    }
}
