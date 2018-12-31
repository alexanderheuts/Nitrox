using System;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxModel.Packets
{
    [Serializable]
    public class ConstructionCompleted : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }

        public ConstructionCompleted(string guid, string parentGuid)
        {
            Guid = guid;
            ParentGuid = parentGuid;
        }

        public override string ToString()
        {
            return string.Format("[ConstructionCompleted Guid={0} ParentGUID={1}]", Guid, ParentGuid);
        }
    }
}
