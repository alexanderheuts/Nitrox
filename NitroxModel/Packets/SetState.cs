using System;

namespace NitroxModel.Packets
{
    [Serializable]
    public class SetState : Packet
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
        public bool Value { get; }
        public bool SetAmount { get; }
        public string NewGuid { get; }
        private string _GameObjectType;

        public SetState(string guid, string baseGuid, Type goType, bool value, bool setAmount, string newGuid = "")
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
            Value = value;
            SetAmount = setAmount;
            NewGuid = newGuid;
        }
    }
}
