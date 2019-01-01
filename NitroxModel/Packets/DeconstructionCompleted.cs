using System;
using UnityEngine;

namespace NitroxModel.Packets
{
    [Serializable]
    public class DeconstructionCompleted : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }
        public Type GameObjectType
        {
            get
            {
                return Type.GetType(_GameObjectType);
            }
            set
            {
                _GameObjectType = value.AssemblyQualifiedName;
            }
        }
        private string _GameObjectType;

        public DeconstructionCompleted(string guid, string parentGuid, Type goType)
        {
            Guid = guid;
            ParentGuid = parentGuid;
            GameObjectType = goType;
        }

        public override string ToString()
        {
            return string.Format("[DeconstructionCompleted Guid={0} ParentGUID={1}]", Guid, ParentGuid);
        }
    }
}
