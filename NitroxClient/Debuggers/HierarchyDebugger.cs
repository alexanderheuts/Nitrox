using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NitroxClient.GameLogic.Helper;
using NitroxModel.Logger;
using UnityEngine;

namespace NitroxClient.Debuggers
{
    public class HierarchyDebugger
    {
        public static void PrintHierarchy(GameObject gameObject, bool startAtRoot = false, int parentsUpwards = 1)
        {
            GameObject startHierarchy = gameObject;
            if (startAtRoot)
            {
                GameObject rootObject = gameObject.transform.root.gameObject;
                if (rootObject != null)
                {
                    startHierarchy = rootObject;
                }
            }
            else
            {
                GameObject parentObject = gameObject;
                int i = 0;
                while (i < parentsUpwards)
                {
                    i++;
                    if (parentObject.transform.parent.gameObject != null)
                    {
                        parentObject = parentObject.transform.parent.gameObject;
                    }
                    else
                    {
                        i = parentsUpwards;
                    }
                }
            }

            TravelDown(startHierarchy);
            
        }

        private static void TravelDown(GameObject gameObject, string linePrefix = "")
        {
            Log.Debug("{0}+GameObject GUID={1} NAME={2}", linePrefix, GuidHelper.GetGuid(gameObject), gameObject.name);

            foreach (Transform child in gameObject.transform)
            {
                TravelDown(child.gameObject, linePrefix + "|  ");
            }
        }
    }
}
