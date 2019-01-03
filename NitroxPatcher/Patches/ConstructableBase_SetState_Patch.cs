using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxModel.Core;
using NitroxModel.Logger;

namespace NitroxPatcher.Patches
{
    class ConstructableBase_SetState_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(ConstructableBase);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetState");

        public static void Postfix(ConstructableBase __instance, ref bool value, ref bool setAmount)
        {
            Log.Debug("ConstructableBase_SetState_Patch-Postfix");
            if (__instance.gameObject != null)
            {
                NitroxServiceLocator.LocateService<Building>().SetState(__instance.gameObject, typeof(ConstructableBase), value, setAmount);
            }
            else
            {
                Log.Debug("No __instance!");
            }
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPostfix(harmony, TARGET_METHOD);
        }
    }
}
