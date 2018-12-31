using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Helper;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Buildings;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using NitroxModel.Packets;
using UnityEngine;
using System;
using static NitroxClient.GameLogic.Helper.TransientLocalObjectManager;

namespace NitroxClient.GameLogic
{
    public class Building
    {
        private const float CONSTRUCTION_CHANGE_EVENT_COOLDOWN_PERIOD_SECONDS = 0.10f;

        private readonly IPacketSender packetSender;
        private float timeSinceLastConstructionChangeEvent;

        public Building(IPacketSender packetSender)
        {
            this.packetSender = packetSender;
        }

        public void PlaceBasePiece(BaseGhost baseGhost, ConstructableBase constructableBase, Base targetBase, TechType techType, Quaternion quaternion)
        {
            if (!Builder.isPlacing) //prevent possible echoing
            {
                return;
            }

            string guid = GuidHelper.GetGuid(constructableBase.gameObject);

            // All parents come out of the box with the same GUID. 
            // We need to make sure they're unique because we rely on the GUID for retrieval.
            string parentGuid = Guid.NewGuid().ToString("D"); 
            GuidHelper.SetNewGuid(constructableBase.transform.parent.gameObject, parentGuid);

            string baseGuid = GuidHelper.GetGuid(constructableBase.transform.root.gameObject);

            // Possibly we don't need the targetbase?
            //string parentBaseGuid = (targetBase == null) ? null : GuidHelper.GetGuid(targetBase.gameObject);

            Vector3 placedPosition = constructableBase.gameObject.transform.position;
            Transform camera = Camera.main.transform;
            Optional<RotationMetadata> rotationMetadata = RotationMetadata.From(baseGhost);

            BasePiece basePiece = new BasePiece(guid, placedPosition, quaternion, camera.position, camera.rotation, techType, parentGuid, baseGuid, false, rotationMetadata);
            PlaceBasePiece placedBasePiece = new PlaceBasePiece(basePiece);
            packetSender.Send(placedBasePiece);
        }

        public void PlaceFurniture(GameObject gameObject, TechType techType, Vector3 itemPosition, Quaternion quaternion)
        {
            if (!Builder.isPlacing) //prevent possible echoing
            {
                return;
            }

            string guid = GuidHelper.GetGuid(gameObject);
            string parentGuid = GuidHelper.GetGuid(gameObject.transform.parent.gameObject);

            string subGuid = "";
            SubRoot sub = Player.main.currentSub;
            if (sub != null)
            {
                subGuid = GuidHelper.GetGuid(sub.gameObject);
            }

            Transform camera = Camera.main.transform;
            Optional<RotationMetadata> rotationMetadata = Optional<RotationMetadata>.Empty();

            BasePiece basePiece = new BasePiece(guid, itemPosition, quaternion, camera.position, camera.rotation, techType, parentGuid, subGuid, true, rotationMetadata);
            PlaceBasePiece placedBasePiece = new PlaceBasePiece(basePiece);
            packetSender.Send(placedBasePiece);
        }

        public void ChangeConstructionAmount(GameObject gameObject, float amount, bool constructing)
        {
            timeSinceLastConstructionChangeEvent += Time.deltaTime;

            if (timeSinceLastConstructionChangeEvent < CONSTRUCTION_CHANGE_EVENT_COOLDOWN_PERIOD_SECONDS)
            {
                return;
            }

            timeSinceLastConstructionChangeEvent = 0.0f;
            
            string guid = GuidHelper.GetGuid(gameObject);
            string parentGuid = GuidHelper.GetGuid(gameObject.transform.parent.gameObject);

            if (amount < 0.95f) // Construction complete event handled by function below
            {
                ConstructionAmountChanged amountChanged = new ConstructionAmountChanged(guid, parentGuid, amount, constructing);
                packetSender.Send(amountChanged);
            }
        }

        public void ConstructionComplete(GameObject gameObject)
        {
            string guid = GuidHelper.GetGuid(gameObject);
            string parentGuid = GuidHelper.GetGuid(gameObject.transform.parent.gameObject);

            ConstructionCompleted constructionCompleted = new ConstructionCompleted(guid, parentGuid);
            packetSender.Send(constructionCompleted);
        }

        public void DeconstructionBegin(GameObject gameObject)
        {
            string guid = GuidHelper.GetGuid(gameObject);
            string parentGuid = GuidHelper.GetGuid(gameObject.transform.parent.gameObject);

            DeconstructionBegin deconstructionBegin = new DeconstructionBegin(guid, parentGuid);
            packetSender.Send(deconstructionBegin);
        }

        public void DeconstructionComplete(GameObject gameObject)
        {
            string guid = GuidHelper.GetGuid(gameObject);
            string parentGuid = GuidHelper.GetGuid(gameObject.transform.parent.gameObject);

            DeconstructionCompleted deconstructionCompleted = new DeconstructionCompleted(guid, parentGuid);
            packetSender.Send(deconstructionCompleted);
        }
    }
}
