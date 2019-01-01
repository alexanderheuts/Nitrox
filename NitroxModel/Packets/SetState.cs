using System;

namespace NitroxModel.Packets
{
    [Serializable]
    public class SetState : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }
        public Type GameObjectType { get; }
        public bool Value { get; }
        public bool SetAmount { get; }

        public SetState(string guid, string parentGuid, Type goType, bool value, bool setAmount)
        {
            Guid = guid;
            ParentGuid = parentGuid;
            GameObjectType = goType;
            Value = value;
            SetAmount = setAmount;
        }
    }
}
