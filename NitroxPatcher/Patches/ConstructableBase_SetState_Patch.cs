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

        public static bool Prefix(ConstructableBase __instance, ref bool value, ref bool setAmount)
        {
            Log.Debug("ConstructableBase_SetState_Patch-Prefix");
            NitroxServiceLocator.LocateService<Building>().SetState(__instance.gameObject, typeof(ConstructableBase), value, setAmount);

            return true;
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPrefix(harmony, TARGET_METHOD);
        }
    }
}
