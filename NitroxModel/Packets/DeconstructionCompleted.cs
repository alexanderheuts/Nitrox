using System;
using UnityEngine;

namespace NitroxModel.Packets
{
    [Serializable]
    public class DeconstructionCompleted : Packet
    {
        public string Guid { get; }
        public string BaseGuid { get; }
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

        public DeconstructionCompleted(string guid, string baseGuid, Type goType)
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
        }

        public override string ToString()
        {
            return string.Format("[DeconstructionCompleted Guid={0} BaseGUID={1}]", Guid, BaseGuid);
        }
    }
}
