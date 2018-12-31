using System;
using UnityEngine;

namespace NitroxModel.Packets
{
    [Serializable]
    public class ConstructionAmountChanged : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }
        public float ConstructionAmount { get; }
        public bool Constructing { get; }

        public ConstructionAmountChanged(string guid, string parentGuid, float constructionAmount, bool constructing)
        {
            Guid = guid;
            ParentGuid = parentGuid;
            ConstructionAmount = constructionAmount;
            Constructing = constructing;
        }

        public override string ToString()
        {
            return string.Format("[ConstructionAmountChanged Guid={0} ParentGUID={1} ConstructionAmount={2} Constructing={3}]", Guid, ParentGuid, ConstructionAmount, Constructing);
        }
    }
}
