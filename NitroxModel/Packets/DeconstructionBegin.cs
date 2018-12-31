using System;
using UnityEngine;

namespace NitroxModel.Packets
{
    [Serializable]
    public class DeconstructionBegin : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }

        public DeconstructionBegin(string guid, string parentGuid)
        {
            Guid = guid;
            ParentGuid = parentGuid;
        }

        public override string ToString()
        {
            return string.Format("[DeconstructionBegin Guid={0} ParentGUID={1}]", Guid, ParentGuid);
        }
    }
}
