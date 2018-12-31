﻿using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Bases;
using NitroxClient.GameLogic.Helper;
using NitroxClient.MonoBehaviours.Overrides;
using NitroxClient.Unity.Helper;
using NitroxModel.Core;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using NitroxModel.Logger;
using NitroxModel.Packets;
using System;
using System.Reflection;
using UnityEngine;

namespace NitroxClient.MonoBehaviours
{
    /**
     * Build events normally can not happen within the same frame as they can cause
     * changes to the surrounding environment.  This class encapsulates logic to 
     * execute build events at a throttled rate of once per frame.  All build logic
     * is contained within this class (it used to be in the individual packet processors)
     * because we want the build logic to be re-useable.
     */
    public class ThrottledBuilder : MonoBehaviour
    {
        public static ThrottledBuilder main;

        public event EventHandler QueueDrained;
        private BuildThrottlingQueue buildEvents;
        private IPacketSender packetSender;

        public void Start()
        {
            main = this;
            buildEvents = NitroxServiceLocator.LocateService<BuildThrottlingQueue>();
            packetSender = NitroxServiceLocator.LocateService<IPacketSender>();
        }

        public void Update()
        {
            if(LargeWorldStreamer.main == null || !LargeWorldStreamer.main.IsReady() || !LargeWorldStreamer.main.IsWorldSettled())
            {
                return;
            }

            bool queueHadItems = (buildEvents.Count > 0);

            ProcessBuildEventsUntilFrameBlocked();

            if(queueHadItems && buildEvents.Count == 0 && QueueDrained != null)
            {
                QueueDrained(this, new EventArgs());
            }
        }

        private void ProcessBuildEventsUntilFrameBlocked()
        {
            bool processedFrameBlockingEvent = false;
            bool isNextEventFrameBlocked = false;

            while (buildEvents.Count > 0 && !isNextEventFrameBlocked)
            {
                BuildEvent nextEvent = buildEvents.Dequeue();

                try
                {
                    ActionBuildEvent(nextEvent);
                }
                catch (Exception ex)
                {
                    Log.Error("Error processing buildEvent in ThrottledBuilder" + ex);
                }

                if (nextEvent.RequiresFreshFrame())
                {
                    processedFrameBlockingEvent = true;
                }

                isNextEventFrameBlocked = (processedFrameBlockingEvent && buildEvents.NextEventRequiresFreshFrame());
            }
        }

        private void ActionBuildEvent(BuildEvent buildEvent)
        {
            if (buildEvent is BasePiecePlacedEvent)
            {
                BuildBasePiece((BasePiecePlacedEvent)buildEvent);
            }
            else if (buildEvent is ConstructionCompletedEvent)
            {
                ConstructionCompleted((ConstructionCompletedEvent)buildEvent);
            }
            else if (buildEvent is ConstructionAmountChangedEvent)
            {
                ConstructionAmountChanged((ConstructionAmountChangedEvent)buildEvent);
            }
            else if (buildEvent is DeconstructionBeginEvent)
            {
                DeconstructionBegin((DeconstructionBeginEvent)buildEvent);
            }
            else if (buildEvent is DeconstructionCompletedEvent)
            {
                DeconstructionCompleted((DeconstructionCompletedEvent)buildEvent);
            }
        }

        private void BuildBasePiece(BasePiecePlacedEvent basePiecePlacedBuildEvent)
        {
            BasePiece basePiece = basePiecePlacedBuildEvent.BasePiece;
            GameObject buildPrefab = CraftData.GetBuildPrefab(basePiece.TechType);
            MultiplayerBuilder.overridePosition = basePiece.ItemPosition;
            MultiplayerBuilder.overrideQuaternion = basePiece.Rotation;
            MultiplayerBuilder.overrideTransform = new GameObject().transform;
            MultiplayerBuilder.overrideTransform.position = basePiece.CameraPosition;
            MultiplayerBuilder.overrideTransform.rotation = basePiece.CameraRotation;
            MultiplayerBuilder.placePosition = basePiece.ItemPosition;
            MultiplayerBuilder.placeRotation = basePiece.Rotation;
            MultiplayerBuilder.rotationMetadata = basePiece.RotationMetadata;
            MultiplayerBuilder.Begin(buildPrefab);

            bool setBaseGuid = false;
            GameObject goBase = null;

            try
            {
                goBase = GuidHelper.RequireObjectFrom(basePiece.BaseGuid);
            }
            catch(Exception e)
            {
                // This should only happen during initial player sync with the placement of the first base piece.
                setBaseGuid = true;
            }

            Constructable constructable;
            GameObject gameObject;

            if (basePiece.IsFurniture)
            {
                SubRoot subRoot = (goBase != null) ? goBase.RequireComponent<SubRoot>() : null;
                                
                gameObject = MultiplayerBuilder.TryPlaceFurniture(subRoot);
                constructable = gameObject.RequireComponentInParent<Constructable>();
            }
            else
            {
                constructable = MultiplayerBuilder.TryPlaceBase(goBase);
                gameObject = constructable.gameObject;

                // The parent object of the constructable is also newly created. We need to keep our GUID references correct.
                GuidHelper.SetNewGuid(gameObject.transform.parent.gameObject, basePiece.ParentGuid);

                if(setBaseGuid)
                {
                    GuidHelper.SetNewGuid(gameObject.transform.root.gameObject, basePiece.BaseGuid);
                }
            }
            
            GuidHelper.SetNewGuid(gameObject, basePiece.Guid);
            
            /**
             * Manually call start to initialize the object as we may need to interact with it within the same frame.
             */
            MethodInfo startCrafting = typeof(Constructable).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            Validate.NotNull(startCrafting);
            startCrafting.Invoke(constructable, new object[] { });
        }

        private void ConstructionCompleted(ConstructionCompletedEvent constructionCompleted)
        {
            GameObject constructing = GuidHelper.RequireObjectFrom(constructionCompleted.ParentGuid);
            Constructable constructable = constructing.GetComponentInChildren<Constructable>();
            constructable.constructedAmount = 1f;
            constructable.SetState(true, true);

            // Confirm the parent GameObject has the correct GUID
            GameObject localParent = constructable.transform.parent.gameObject;
            string localParentGuid = GuidHelper.GetGuid(localParent);

            if(localParentGuid != constructionCompleted.ParentGuid)
            {
                GuidHelper.SetNewGuid(localParent, constructionCompleted.ParentGuid);
                Log.Debug("ConstructionCompleted::Setting new ParentGUID={0} to replace existing ParentGUID={1}", constructionCompleted.ParentGuid, localParentGuid);
            }

        }

        private void ConstructionAmountChanged(ConstructionAmountChangedEvent amountChanged)
        {
            Log.Debug("Processing ConstructionAmountChanged Guid={0} ParentGUID={1} AmountChanged={2}", amountChanged.Guid, amountChanged.ParentGuid, amountChanged.Amount);

            GameObject constructing = GuidHelper.RequireObjectFrom(amountChanged.ParentGuid);
            Constructable constructable = constructing.GetComponentInChildren<Constructable>();
            constructable.constructedAmount = amountChanged.Amount;

            using (packetSender.Suppress<ConstructionAmountChanged>())
            {
                constructable.Construct();
            }
        }

        private void DeconstructionBegin(DeconstructionBeginEvent begin)
        {
            // We need to find the correct constructable based on the parent gameobject.
            GameObject deconstructing = GuidHelper.RequireObjectFrom(begin.ParentGuid);
            Constructable constructable = deconstructing.RequireComponent<Constructable>();

            constructable.SetState(false, false);
        }

        private void DeconstructionCompleted(DeconstructionCompletedEvent completed)
        {
            // We destroy the parent of the constructable, not just the constructable
            GameObject deconstructing = GuidHelper.RequireObjectFrom(completed.ParentGuid);
            UnityEngine.Object.Destroy(deconstructing);
        }
    }
}
