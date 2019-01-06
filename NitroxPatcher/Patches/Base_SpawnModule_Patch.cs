using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using NitroxClient.GameLogic.Helper;
using NitroxModel.Logger;

namespace NitroxPatcher.Patches
{
    public class Base_SpawnModule_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(Base);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SpawnModule");

        // Used by ConstructableBase_SetState_Patch::Postfix to keep track of newly created instances
        public static GameObject LastSpawnedRoomModule;

        public static void Postfix(GameObject __result)
        {
            Log.Debug("Base_SpawnModule_Patch::Postfix");
            if (__result == null)
            {
                Log.Debug("Base_SpawnModule_Patch:: result is NULL");
            }
            else
            {
                Log.Debug("Base_SpawnModule_Patch:: result has GUID={0}", GuidHelper.GetGuid(__result));
            }
            LastSpawnedRoomModule = __result;
            TransientLocalObjectManager.Add(TransientLocalObjectManager.TransientObjectType.BASE_NEWLY_SPAWNED_MODULE_GAMEOBJECT, __result);
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchPostfix(harmony, TARGET_METHOD);
        }
    }
}
