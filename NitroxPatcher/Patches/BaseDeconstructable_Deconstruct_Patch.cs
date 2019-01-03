using System;
using System.Reflection;
using Harmony;
using NitroxModel.Logger;
using NitroxClient.GameLogic.Helper;
using UnityEngine;
using NitroxClient.GameLogic;
using NitroxModel.Core;

namespace NitroxPatcher.Patches
{
    class BaseDeconstructable_Deconstruct_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(BaseDeconstructable);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("Deconstruct");

        // The assumption is that the state is shared between all clients and that the BaseDeconstructable.Deconstruct() follows the same logic on all clients.
        // NOTE: We may want to check whether other players are in this base. Not necessarily in here.
        public static void Prefix(BaseDeconstructable __instance)
        {
            //NitroxServiceLocator.LocateService<Building>().DeconstructionBegin(__instance.gameObject, typeof(BaseDeconstructable));
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPrefix(harmony, TARGET_METHOD);
        }
    }
}
