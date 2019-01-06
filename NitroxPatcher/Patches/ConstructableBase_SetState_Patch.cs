using System;
using System.Reflection;
using Harmony;
using NitroxClient.GameLogic;
using NitroxClient.GameLogic.Helper;
using NitroxModel.Core;
using NitroxModel.Logger;

namespace NitroxPatcher.Patches
{
    class ConstructableBase_SetState_Patch : NitroxPatch
    {
        public static readonly Type TARGET_CLASS = typeof(ConstructableBase);
        public static readonly MethodInfo TARGET_METHOD = TARGET_CLASS.GetMethod("SetState");

        public static void Prefix(ConstructableBase __instance, ref bool value, ref bool setAmount)
        {
            Log.Debug("ConstructableBase_SetState_Patch-Prefix");
            // We trigger on construction start or deconstruction start, or we get null-refs after the original method.

            try
            {
                SubRoot sr = __instance.GetComponent<SubRoot>();
                SubRoot srInP = __instance.GetComponentInParent<SubRoot>();
               
                if( sr != null)
                {
                    Log.Debug("SubRoot in sr={0}", GuidHelper.GetGuid(sr.gameObject));
             
                }
                if(srInP != null)
                {
                    Log.Debug("SubRoot in srInP={0}", GuidHelper.GetGuid(srInP.gameObject));
                }
                if (__instance.gameObject != null && !value)
                {
                    NitroxServiceLocator.LocateService<Building>().SetState(__instance.gameObject, typeof(ConstructableBase), value, setAmount);
                }
            }
            catch (Exception e)
            {
                Log.Debug("Oh noes, something went wrong. " + e.Message.ToString());
                Log.Debug(e.StackTrace.ToString());
            }

        }

        public static void Postfix(ConstructableBase __instance, ref bool value, ref bool setAmount)
        {
            Log.Debug("ConstructableBase_SetState_Patch-Postfix");
            // We only trigger when we complete construction (true, true) to make sure we get the new BaseGuid
            if (__instance.gameObject != null && value && setAmount)
            {
                string newInstanceGuid = "";
                if (Base_SpawnModule_Patch.LastSpawnedRoomModule != null)
                {
                    newInstanceGuid = GuidHelper.GetGuid(Base_SpawnModule_Patch.LastSpawnedRoomModule);
                }
                NitroxServiceLocator.LocateService<Building>().SetState(__instance.gameObject, typeof(ConstructableBase), value, setAmount, newInstanceGuid);
            }
            else
            {
                Log.Debug("No __instance!");
            }
        }

        public override void Patch(HarmonyInstance harmony)
        {
            PatchMultiple(harmony, TARGET_METHOD, true, true, false);
        }
    }
}
