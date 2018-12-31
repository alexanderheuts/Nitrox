﻿using System;
using UnityEngine;

namespace NitroxModel.Packets
{
    [Serializable]
    public class DeconstructionCompleted : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }

        public DeconstructionCompleted(string guid, string parentGuid)
        {
            Guid = guid;
            ParentGuid = parentGuid;
        }

        public override string ToString()
        {
            return string.Format("[DeconstructionCompleted Guid={0} ParentGUID={1}]", Guid, ParentGuid);
        }
    }
}
