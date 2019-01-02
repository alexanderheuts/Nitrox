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
            Log.Debug("BaseDeconstructable");
            GameObject parent = __instance.transform.parent.gameObject;
            Log.Debug("Parent Guid={0} type={1}", GuidHelper.GetGuid(parent), parent.GetType());
            Base b = __instance.GetComponentInParent<Base>();
            Log.Debug("Base Guid={0}", GuidHelper.GetGuid(b.gameObject));
            Log.Debug("Trying to find the ConstructableBase in the Children");
            ConstructableBase cb = __instance.GetComponentInChildren<ConstructableBase>();
            if(cb==null)
            {
                Log.Debug("Trying to find ConstructableBase on the same level.");
                cb = __instance.GetComponent<ConstructableBase>();
            }
            if(cb==null)
            {
                Log.Debug("Trying to find the ConstructableBase in the Parents");
                cb = __instance.GetComponentInParent<ConstructableBase>();
            }
            if(cb==null)
            {
                Log.Debug("Try the children of the root.");
                cb = __instance.transform.root.gameObject.GetComponentInChildren<ConstructableBase>();
            }
            if(cb==null)
            {
                Log.Debug("Last hope, is it part of the root object?");
                cb = __instance.transform.root.gameObject.GetComponent<ConstructableBase>();
            }
            if(cb != null)
            {
                Log.Debug("Parent has ConstructableBase");
            }
            //NitroxServiceLocator.LocateService<Building>().DeconstructionBegin(__instance.gameObject, typeof(BaseDeconstructable));
        }


        public override void Patch(HarmonyInstance harmony)
        {
            PatchPrefix(harmony, TARGET_METHOD);
        }
    }
}
