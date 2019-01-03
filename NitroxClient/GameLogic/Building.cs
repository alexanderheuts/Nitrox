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
            string baseGuid = ""; // Base Component doesn't exist until the object is instantiated, which is done AFTER this piece of code has run, by the ConstructableBase.SetState(false, true)

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
            string baseGuid = GuidHelper.GetGuid(gameObject.GetComponentInParent<Base>().gameObject);

            if (0.05f < amount && amount < 0.95f) // Deconstruction / Construction complete event handled by function below
            {
                ConstructionAmountChanged amountChanged = new ConstructionAmountChanged(guid, baseGuid, goType, amount);
                packetSender.Send(amountChanged);
            }
        }

        public void DeconstructionBegin(GameObject gameObject, Type goType)
        {
            Log.Debug("DeconstructionBegin");
            Base compInParent = gameObject.GetComponentInParent<Base>();
            if (compInParent != null)
            {
                Log.Debug("There's a Base component ");
            }
            string guid = GuidHelper.GetGuid(gameObject);
            string baseGuid = GuidHelper.GetGuid(gameObject.GetComponentInParent<Base>().gameObject);
            Log.Debug("Sending DeconstructionBegin Guid={0} BaseGuid={1} Type={2}", guid, baseGuid, goType);
            DeconstructionBegin deconstructionBegin = new DeconstructionBegin(guid, baseGuid, goType);
            packetSender.Send(deconstructionBegin);
        }

        public void DeconstructionComplete(GameObject gameObject, Type goType)
        {
            string guid = GuidHelper.GetGuid(gameObject);
            string baseGuid = GuidHelper.GetGuid(gameObject.GetComponentInParent<Base>().gameObject);

            DeconstructionCompleted deconstructionCompleted = new DeconstructionCompleted(guid, baseGuid, goType);
            packetSender.Send(deconstructionCompleted);
        }

        public void SetState(GameObject gameObject, Type goType, bool value, bool setAmount)
        {
            string guid = GuidHelper.GetGuid(gameObject);
           
            string baseGuid = "";
            try
            {
                 baseGuid = GuidHelper.GetGuid(gameObject.GetComponentInParent<Base>().gameObject);
            }
            catch(Exception)
            {
                // Apparently a state is set on an orphaned object. We don't care for this in multiplayer.
                // Deconstruction is a nasty piece of work. This is most likely the cause.
                return;
            }

            SetState setState = new SetState(guid, baseGuid, goType, value, setAmount);
            packetSender.Send(setState);
            Log.Debug("Client sent setState for Guid={0} BaseGuid={1} goType={2} value={3} setAmount={4}", guid, baseGuid, goType, value, setAmount);
        }
    }
}
