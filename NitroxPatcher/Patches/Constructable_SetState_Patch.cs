using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxModel.Core;
using NitroxModel.Logger;

namespace NitroxPatcher.Patches
{
    class Constructable_SetState_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(Constructable);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetState");

        public static void Postfix(Constructable __instance, ref bool value, ref bool setAmount)
        {
            if(__instance is ConstructableBase)
            {
                // Check to see that we don't trigger SetState twice due to inheritance.
                return;
            }

            Log.Debug("Constructable_SetState_Patch-Postfix");
            NitroxServiceLocator.LocateService<Building>().SetState(__instance.gameObject, typeof(Constructable), value, setAmount);
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPostfix(harmony, TARGET_METHOD);
        }
    }
}
