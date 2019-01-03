using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxClient.GameLogic.Helper;
using NitroxModel.Logger;
using NitroxModel.Core;

namespace NitroxPatcher.Patches
{
    public class Constructable_Construct_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(Constructable);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Construct");

        public static bool Prefix(Constructable __instance)
        {
            Log.Debug("Constructable_ConstructPatch::Prefix");
            Log.Debug("Constructable GUID={0}", GuidHelper.GetGuid(__instance.gameObject));
            Log.Debug("Constructable ParentGuid={0} Type={1}", GuidHelper.GetGuid(__instance.transform.parent.gameObject), __instance.transform.parent.gameObject.GetType());

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
