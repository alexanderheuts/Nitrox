using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Helper;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Buildings;
using NitroxModel.DataStructures.Util;
using NitroxModel.Packets;
using NitroxModel.Logger;
using UnityEngine;
using System;

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
            Log.Debug("Placing ConstructableBase gameObjectGuid={0} ", guid);
            GameObject model = constructableBase.model;
            Log.Debug("Placing ConstructableBase modelGuid={0}",GuidHelper.GetGuid(model));
            GameObject ghost = baseGhost.gameObject;
            Log.Debug("Placing ConstructableBase ghostGuid={0}", GuidHelper.GetGuid(ghost));

            // For a new base piece the Base is hiding in a child of the parent gameobject of the ConstructableBase.
            // Note: when deconstructing it is in a component in the parent of the ConstructableBase
            Base b = constructableBase.transform.parent.gameObject.GetComponentInChildren<Base>();
            string baseGuid = GuidHelper.GetGuid(b.gameObject);

            // If the targetBase doesn't exist, we're creating a new base.
            string targetBaseGuid = (targetBase == null) ? null : GuidHelper.GetGuid(targetBase.gameObject);

            Vector3 placedPosition = constructableBase.gameObject.transform.position;
            Transform camera = Camera.main.transform;
            Optional<RotationMetadata> rotationMetadata = RotationMetadata.From(baseGhost);

            BasePiece basePiece = new BasePiece(guid, typeof(ConstructableBase), placedPosition, quaternion, camera.position, camera.rotation, techType, baseGuid, Optional<string>.OfNullable(targetBaseGuid), false, rotationMetadata);
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

            string subGuid = "";
            SubRoot sub = Player.main.currentSub;
            if (sub != null)
            {
                subGuid = GuidHelper.GetGuid(sub.gameObject);
            }

            Transform camera = Camera.main.transform;
            Optional<RotationMetadata> rotationMetadata = Optional<RotationMetadata>.Empty();

            BasePiece basePiece = new BasePiece(guid, typeof(Constructable), itemPosition, quaternion, camera.position, camera.rotation, techType, subGuid, null, true, rotationMetadata);
            PlaceBasePiece placedBasePiece = new PlaceBasePiece(basePiece);
            packetSender.Send(placedBasePiece);
        }

        public void ChangeConstructionAmount(GameObject gameObject, Type goType, float amount)
        {
            timeSinceLastConstructionChangeEvent += Time.deltaTime;

            if (timeSinceLastConstructionChangeEvent < CONSTRUCTION_CHANGE_EVENT_COOLDOWN_PERIOD_SECONDS)
            {
                return;
            }

            timeSinceLastConstructionChangeEvent = 0.0f;
            
            string guid = GuidHelper.GetGuid(gameObject);
            string baseGuid = GuidHelper.GetGuid(GetBase(gameObject).gameObject);

            if (amount < 0.95f) // Deconstruction / Construction complete event handled by function below
            {
                ConstructionAmountChanged amountChanged = new ConstructionAmountChanged(guid, baseGuid, goType, amount);
                packetSender.Send(amountChanged);
            }
        }

        public void DeconstructionBegin(GameObject gameObject, Type goType)
        {
            // Only called for base pieces. Furniture is deconstructed via SetState.
            string guid = GuidHelper.GetGuid(gameObject);

            Base b = GetBase(gameObject);
            string baseGuid = GuidHelper.GetGuid(b.gameObject);

            DeconstructionBegin deconstructionBegin = new DeconstructionBegin(guid, baseGuid, goType);
            packetSender.Send(deconstructionBegin);
            Log.Debug("Sent DeconstructionBegin Guid={0} BaseGuid={1} Type={2}", guid, baseGuid, goType);
        }

        public void DeconstructionComplete(GameObject gameObject, Type goType)
        {
            string guid = GuidHelper.GetGuid(gameObject);
            string baseGuid = GuidHelper.GetGuid(GetBase(gameObject).gameObject);

            DeconstructionCompleted deconstructionCompleted = new DeconstructionCompleted(guid, baseGuid, goType);
            packetSender.Send(deconstructionCompleted);
        }

        public void SetState(GameObject gameObject, Type goType, bool value, bool setAmount, string newGuid = "")
        {
            // false, false -> deconstruct-start
            // false, true -> construction-start (as ConstructableBase starts with _constructed = true 
            // true, true -> construction-complete
            string guid = GuidHelper.GetGuid(gameObject);

            Base b = GetBase(gameObject);
            if (b.gameObject == null)
            {
                // If there's no Base, what are we destroying? Must be client only for deconstruction routine. Other clients already lost interest.
                return;
            }
            string baseGuid = GuidHelper.GetGuid(b.gameObject);

            SetState setState = new SetState(guid, baseGuid, goType, value, setAmount, newGuid);
            packetSender.Send(setState);
            Log.Debug("Client sent setState for Guid={0} BaseGuid={1} goType={2} value={3} setAmount={4} newGuid={5}", guid, baseGuid, goType, value, setAmount, newGuid);
        }

        private Base GetBase(GameObject gameObject)
        {
            Base baseOut = null;

            try
            {
                // For furniture & completed construction it's a ComponentInParent
                baseOut = gameObject.GetComponentInParent<Base>();

                if(baseOut == null)
                {
               
                        // We must still be constructing the base
                        baseOut = gameObject.transform.parent.gameObject.GetComponentInChildren<Base>();

                }
            }
            catch (Exception e)
            {
                Log.Debug("Building::GetBase() Base doesn't exist.");
            }

            return baseOut;
        }
    }
}
