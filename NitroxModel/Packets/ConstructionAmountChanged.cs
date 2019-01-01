using System;

namespace NitroxModel.Packets
{
    [Serializable]
    public class ConstructionAmountChanged : Packet
    {
        public string Guid { get; }
        public string ParentGuid { get; }
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

        public ConstructionAmountChanged(string guid, string parentGuid, Type goType, float constructionAmount)
        {
            Guid = guid;
            ParentGuid = parentGuid;
            GameObjectType = goType;
            ConstructionAmount = constructionAmount;
        }

        public override string ToString()
        {
            return string.Format("[ConstructionAmountChanged Guid={0} ParentGUID={1} ConstructionAmount={2} GameObjecType={3}]", Guid, ParentGuid, ConstructionAmount, GameObjectType);
        }
    }
}
