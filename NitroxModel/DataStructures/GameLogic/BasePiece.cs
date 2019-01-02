using NitroxModel.DataStructures.GameLogic.Buildings;
using NitroxModel.DataStructures.Util;
using ProtoBuf;
using System;
using UnityEngine;

namespace NitroxModel.DataStructures.GameLogic
{
    [Serializable]
    [ProtoContract]
    public class BasePiece
    {
        [ProtoMember(1)]
        public string Guid { get; set; }

        [ProtoMember(2)]
        public Vector3 ItemPosition { get; set; }

        [ProtoMember(3)]
        public Quaternion Rotation { get; set; }

        [ProtoMember(4)]
        public TechType TechType { get; set; }

        [ProtoMember(5)]
        // Guid of the parent GameObject
        // NOTE: we need the parent for most interactions AFTER construction has been completed 
        //       due to the way Unity works with component nesting.
        public string ParentGuid { get; set; }

        [ProtoMember(6)]
        public Vector3 CameraPosition { get; set; }

        [ProtoMember(7)]
        public Quaternion CameraRotation { get; set; }

        [ProtoMember(8)]
        public float ConstructionAmount { get; set; }

        [ProtoMember(9)]
        public bool ConstructionCompleted { get; set; }

        [ProtoMember(10)]
        public bool IsFurniture { get; set; }

        [ProtoMember(11)]
        // Guid of the base GameObject
        public string BaseGuid { get; set; }

        [ProtoMember(12, DynamicType = true)]
        public RotationMetadata SerializableRotationMetadata
        {
            get { return (RotationMetadata.IsPresent()) ? RotationMetadata.Get() : null; }
            set { RotationMetadata = Optional<RotationMetadata>.OfNullable(value); }
        }

        [ProtoIgnore]
        public Type TypeOfConstructable
        {
            get
            {
                return Type.GetType(_TypeOfConstructable);
            }
            set
            {
                _TypeOfConstructable = value.AssemblyQualifiedName;
            }
        }

        [ProtoMember(12)]
        private string _TypeOfConstructable;

        [ProtoIgnore]
        public Optional<RotationMetadata> RotationMetadata {get; set; }

        public BasePiece()
        {
            RotationMetadata = Optional<RotationMetadata>.Empty();
        }

        public BasePiece(string guid, Type typeOfConstructable, Vector3 itemPosition, Quaternion rotation, Vector3 cameraPosition, Quaternion cameraRotation, TechType techType, string parentGuid, string baseGuid, bool isFurniture, Optional<RotationMetadata> rotationMetadata)
        {
            Guid = guid;
            TypeOfConstructable = typeOfConstructable;
            ItemPosition = itemPosition;
            Rotation = rotation;
            TechType = techType;
            CameraPosition = cameraPosition;
            CameraRotation = cameraRotation;
            ParentGuid = parentGuid;
            BaseGuid = baseGuid;
            IsFurniture = isFurniture;
            ConstructionAmount = 0.0f;
            ConstructionCompleted = true;
            RotationMetadata = rotationMetadata;
        }

        public override string ToString()
        {
            return "[BasePiece - ItemPosition: " + ItemPosition + " Guid: " + Guid + " Rotation: " + Rotation + " CameraPosition: " + CameraPosition + "CameraRotation: " + CameraRotation + " TechType: " + TechType + " ParentGuid: " + ParentGuid + " BaseGuid: " + BaseGuid + " ConstructionAmount: " + ConstructionAmount + " IsFurniture: " + IsFurniture  + " RotationMetadata: " + RotationMetadata + "]";
        }
    }
}
