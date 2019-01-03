using System;

namespace NitroxModel.Packets
{
    [Serializable]
    public class ConstructionAmountChanged : Packet
    {
        public string Guid { get; }
        public string BaseGuid { get; }
        public Type GameObjectType {
            get
            {
                return Type.GetType(_GameObjectType);
            }
            set
            {
                _GameObjectType = value.AssemblyQualifiedName;
            }
        }
        public float ConstructionAmount { get; }
        private string _GameObjectType;

        public ConstructionAmountChanged(string guid, string baseGuid, Type goType, float constructionAmount)
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
            ConstructionAmount = constructionAmount;
        }

        public override string ToString()
        {
            return string.Format("[ConstructionAmountChanged Guid={0} BaseGUID={1} ConstructionAmount={2} GameObjecType={3}]", Guid, BaseGuid, ConstructionAmount, GameObjectType);
        }
    }
}
