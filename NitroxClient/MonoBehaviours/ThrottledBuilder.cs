using NitroxClient.Communication.Abstract;
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

            Constructable constructable;
            GameObject gameObject;
            GameObject targetBase = null;

            if (basePiece.TargetBase.IsPresent())
            {
                targetBase = GuidHelper.RequireObjectFrom(basePiece.TargetBase.Get());
            }

            if (basePiece.IsFurniture)
            {
                SubRoot subRoot = (targetBase != null) ? targetBase.RequireComponent<SubRoot>() : null;
                                
                gameObject = MultiplayerBuilder.TryPlaceFurniture(subRoot);
                constructable = gameObject.RequireComponentInParent<Constructable>();
            }
            else
            {
                constructable = MultiplayerBuilder.TryPlaceBase(targetBase);
                gameObject = constructable.gameObject;

                GuidHelper.SetNewGuid(gameObject.transform.parent.GetComponentInChildren<Base>().gameObject, basePiece.BaseGuid);
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
            GameObject constructing = GuidHelper.RequireObjectFrom(amountChanged.Guid);

            using (packetSender.Suppress<ConstructionAmountChanged>())
            {
                if (amountChanged.GameObjectType == typeof(Constructable))
                {
                    Constructable constructable = constructing.GetComponent<Constructable>();
                    constructable.constructedAmount = amountChanged.Amount;
                    constructable.Construct();
                }
                else if (amountChanged.GameObjectType == typeof(ConstructableBase))
                {
                    ConstructableBase constructable = constructing.GetComponent<ConstructableBase>();
                    constructable.constructedAmount = amountChanged.Amount;
                    constructable.Construct();
                }
            }
            
        }

        private void DeconstructionBegin(DeconstructionBeginEvent begin)
        {
            GameObject deconstructing = null;

            using (packetSender.Suppress<DeconstructionBegin>())
            {
                using (packetSender.Suppress<SetState>())
                {
                    if (begin.GameObjectType == typeof(Constructable))
                    {
                        deconstructing = GuidHelper.RequireObjectFrom(begin.Guid);

                        // We're dealing with a Constructable. Which triggers deconstruction via SetState(false, false).
                        Constructable constructable = deconstructing.GetComponent<Constructable>();

                        constructable.SetState(false, false);
                    }
                    else
                    {
                        // We're dealing with either BaseDeconstructable or BaseExplicitFace. 
                        // Both resolve with the same method Deconstruct().

                        // TODO: fix instantiating new object in deconstruct sequence.
                        deconstructing = GuidHelper.RequireObjectFrom(begin.BaseGuid);

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
            if(completed.GameObjectType == typeof(Constructable))
            {
                GameObject deconstructing = GuidHelper.RequireObjectFrom(completed.Guid);

                Constructable constructable = deconstructing.GetComponent<Constructable>();
                constructable.Deconstruct();

                Destroy(deconstructing);
            }
            else if(completed.GameObjectType == typeof(ConstructableBase))
            {
                Log.Debug("DeconstructionCompleted guid={0} baseGuid={1}", completed.Guid, completed.BaseGuid);
                // TODO: fix for changing GUID's in deconstruct due to copy instantiation
                GameObject deconstructing = GuidHelper.RequireObjectFrom(completed.BaseGuid);

                Base b = deconstructing.GetComponent<Base>();
                BaseDeconstructable bd = b.GetComponentInChildren<BaseDeconstructable>();

                if (bd.face != null)
                {
                    b.ClearFace(bd.face.Value, bd.faceType);
                }
                else
                {
                    b.ClearCell(bd.bounds.mins);
                }
                if (b.IsEmpty(null))
                {
                    b.OnPreDestroy();
                    Destroy(b.gameObject);
                }
                else
                {
                    b.FixRoomFloors();
                    b.FixCorridorLinks();
                    b.RebuildGeometry();
                }
            }
        }

        private void SetState(SetStateEvent setState)
        {
            GameObject goSetState = GuidHelper.RequireObjectFrom(setState.Guid);

            using (packetSender.Suppress<SetState>())
            {
                if (setState.GameObjectType == typeof(Constructable))
                {
                    Constructable c = goSetState.GetComponent<Constructable>();
                    c.SetState(setState.Value, setState.SetAmount);
                }
                else if (setState.GameObjectType == typeof(ConstructableBase))
                {
                    // TODO: fix deconstruction
                    ConstructableBase cb = goSetState.GetComponent<ConstructableBase>();

                    cb.SetState(setState.Value, setState.SetAmount);

                    if(setState.Value && setState.SetAmount)
                    {
                        Optional<object> newInstance = TransientLocalObjectManager.Get(TransientLocalObjectManager.TransientObjectType.BASE_NEWLY_SPAWNED_MODULE_GAMEOBJECT);
                        if(newInstance.IsPresent() && setState.NewGuid != "")
                        {
                            GameObject newBaseModule = (GameObject)newInstance.Get();
                            GuidHelper.SetNewGuid(newBaseModule, setState.NewGuid);
                            TransientLocalObjectManager.localObjectsById.Remove(TransientLocalObjectManager.TransientObjectType.BASE_NEWLY_SPAWNED_MODULE_GAMEOBJECT);
                        }
                        else
                        {
                            Log.Error("Could not set new GUID during SetState");
                        }
                    }
                }
            }
        }
    }
}
