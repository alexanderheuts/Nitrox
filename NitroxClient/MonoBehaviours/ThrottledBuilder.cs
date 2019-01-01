using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Bases;
using NitroxClient.GameLogic.Helper;
using NitroxClient.MonoBehaviours.Overrides;
using NitroxClient.Unity.Helper;
using NitroxModel.Core;
using NitroxModel.DataStructures.GameLogic;
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
            else if (buildEvent is SetStateEvent)
            {
                SetState((SetStateEvent)buildEvent);
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
            GameObject deconstructing = GuidHelper.RequireObjectFrom(begin.ParentGuid);

            using (packetSender.Suppress<DeconstructionBegin>())
            {
                using (packetSender.Suppress<SetState>())
                {
                    if (begin.GameObjectType == typeof(Constructable))
                    {
                        // We're dealing with a Constructable. Which triggers deconstruction via SetState(false, false).
                        Constructable constructable = deconstructing.GetComponentInChildren<Constructable>();

                        constructable.SetState(false, false);
                    }
                    else
                    {
                        // We're dealing with either BaseDeconstructable or BaseExplicitFace. 
                        // Both resolve with the same method Deconstruct().
                        BaseDeconstructable baseDeconstructable = null;
                        if (begin.GameObjectType == typeof(BaseDeconstructable))
                        {
                            baseDeconstructable = deconstructing.GetComponentInChildren<BaseDeconstructable>();
                        }
                        else
                        {
                            BaseExplicitFace bef = deconstructing.GetComponentInChildren<BaseExplicitFace>();
                            baseDeconstructable = bef.parent;
                        }

                        baseDeconstructable.Deconstruct();
                    }
                }
            }
        }

        private void DeconstructionCompleted(DeconstructionCompletedEvent completed)
        {
            Log.Debug("Processing DeconstructionCompleted Guid={0} ParentGUID={1}", completed.Guid, completed.ParentGuid);
            // We destroy the parent of the constructable, not just the constructable
            GameObject deconstructing = GuidHelper.RequireObjectFrom(completed.ParentGuid);
            Log.Debug("Parent to be deconstructed is of type {0}", deconstructing.GetType().ToString());
            Constructable constructable = deconstructing.GetComponentInChildren<Constructable>();

            // In case we missed the DeconstructionBegin, make sure the game knows it's being deconstructed
            constructable.SetState(false, false);

            constructable.constructedAmount = 0f;
            constructable.Deconstruct();

            //UnityEngine.Object.Destroy(deconstructing);
        }

        private void SetState(SetStateEvent setState)
        {
            GameObject goSetStateParent = GuidHelper.RequireObjectFrom(setState.ParentGuid);

            using (packetSender.Suppress<SetState>())
            {
                if (setState.GameObjectType == typeof(Constructable))
                {
                    Constructable c = goSetStateParent.GetComponentInChildren<Constructable>();
                    c.SetState(setState.Value, setState.SetAmount);
                }
                else if (setState.GameObjectType == typeof(ConstructableBase))
                {
                    ConstructableBase cb = goSetStateParent.GetComponentInChildren<ConstructableBase>();
                    cb.SetState(setState.Value, setState.SetAmount);
                }
            }
        }
    }
}
