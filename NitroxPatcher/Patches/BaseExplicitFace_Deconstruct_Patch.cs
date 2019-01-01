using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxModel.Core;

namespace NitroxPatcher.Patches
{
    class BaseExplicitFace_Deconstruct_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(BaseExplicitFace);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Deconstruct");

        // TODO!
        // This needs to inform clients that a face is being deconstructed.
        public static void Prefix(BaseExplicitFace __instance)
        {
            NitroxServiceLocator.LocateService<Building>().DeconstructionBegin(__instance.gameObject, typeof(BaseExplicitFace));
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPrefix(harmony, TARGET_METHOD);
        }
    }
}
