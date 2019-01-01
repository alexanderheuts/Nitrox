using System;

namespace NitroxModel.Packets
{
    [Serializable]
    public class SetState : Packet
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
        public bool Value { get; }
        public bool SetAmount { get; }
        private string _GameObjectType;

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
