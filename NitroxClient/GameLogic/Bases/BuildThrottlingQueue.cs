using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using System;
using System.Collections.Generic;

namespace NitroxClient.GameLogic.Bases
{
    /**
     * Build events normally can not happen within the same frame as they can cause
     * changes to the surrounding environment.  This class encapsulates logic to 
     * hold build events for future processing.  Incoming packets can be converted
     * to more generic BuildEvent classes that can be re-used. Examples: we want
     * to re-use logic for ConstructionCompleted packet and InitialPlayerSync build
     * packets - this class helps faciliate that.
     */
    public class BuildThrottlingQueue : Queue<BuildEvent>
    {
        public bool NextEventRequiresFreshFrame()
        {
            if(Count > 0)
            {
                BuildEvent nextEvent = Peek();
                return nextEvent.RequiresFreshFrame();
            }

            return false;
        }

        public void EnqueueBasePiecePlaced(BasePiece basePiece)
        {
            Log.Info("Enqueuing base piece to be placed GUID={0} BaseGUID={1}", basePiece.Guid, basePiece.BaseGuid );
            Enqueue(new BasePiecePlacedEvent(basePiece));
        }

        public void EnqueueAmountChanged(string guid, string baseGuid, Type goType, float amount)
        {
            Log.Info("Enqueuing item to have construction amount changed GUID={0} BaseGUID={1}", guid, baseGuid);
            Enqueue(new ConstructionAmountChangedEvent(guid, baseGuid, goType, amount));
        }

        public void EnqueueDeconstructionBegin(string guid, string baseGuid, Type goType)
        {
            Log.Info("Enqueuing item to have deconstruction beginning GUID={0} BaseGUID={1}", guid, baseGuid);
            Enqueue(new DeconstructionBeginEvent(guid, baseGuid, goType));
        }

        public void EnqueueDeconstructionCompleted(string guid, string baseGuid, Type goType)
        {
            Log.Info("Enqueuing item to have deconstruction completed GUID={0} BaseGUID={1}", guid, baseGuid);
            Enqueue(new DeconstructionCompletedEvent(guid, baseGuid, goType));
        }

        public void EnqueueSetState(string guid, string baseGuid, Type goType, bool value, bool setAmount)
        {
            Log.Info("Enqueuing item to have state changes GUID={0} BaseGUID={1} Type={2} Value={3} SetAmount={4}",
                guid, baseGuid, goType.ToString(), value, setAmount);
            Enqueue(new SetStateEvent(guid, baseGuid, goType, value, setAmount));
        }
    }

    public class BasePiecePlacedEvent : BuildEvent
    {
        public BasePiece BasePiece { get; }

        public BasePiecePlacedEvent(BasePiece basePiece)
        {
            BasePiece = basePiece;
        }

        public bool RequiresFreshFrame()
        {
            // Since furniture can not be built upon, we only require
            // a fresh frame for actual base pieces.
            return !BasePiece.IsFurniture;
        }
    }

    public class ConstructionAmountChangedEvent : BuildEvent
    {
        public string Guid { get; }
        public string BaseGuid { get; }
        public Type GameObjectType { get; }
        public float Amount { get; }

        public ConstructionAmountChangedEvent(string guid, string baseGuid, Type goType, float amount)
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
            Amount = amount;
        }

        public bool RequiresFreshFrame()
        {
            // Change events only affect the materials used and
            // translusence of an item.  We can process multiple
            // of these per a frame.
            return false;
        }
    }

    public class DeconstructionBeginEvent : BuildEvent
    {
        public string Guid { get; }
        public string BaseGuid { get; }
        public Type GameObjectType { get; }

        public DeconstructionBeginEvent(string guid, string baseGuid, Type goType)
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
        }

        public bool RequiresFreshFrame()
        {
            // Starting a deconstruction event makes it so you can
            // no longer attach items and may change the surrounding 
            // environment.  Thus, we want to only process one per frame.
            return true;
        }
    }

    public class DeconstructionCompletedEvent : BuildEvent
    {
        public string Guid { get; }
        public string BaseGuid { get; }
        public Type GameObjectType { get; }

        public DeconstructionCompletedEvent(string guid, string baseGuid, Type goType)
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
        }

        public bool RequiresFreshFrame()
        {
            // Completing a deconstruction will change the surrounding 
            // environment.  Thus, we want to only process one per frame.
            return true;
        }
    }

    public class SetStateEvent : BuildEvent
    {
        public string Guid { get; }
        public string BaseGuid { get; }
        public Type GameObjectType { get; }
        public bool Value { get; }
        public bool SetAmount { get; }

        public SetStateEvent(string guid, string baseGuid, Type goType, bool value, bool setAmount)
        {
            Guid = guid;
            BaseGuid = baseGuid;
            GameObjectType = goType;
            Value = value;
            SetAmount = setAmount;
        }

        public bool RequiresFreshFrame()
        {
            // Changing the state of a piece changes the environment.
            return true;
        }
    }

    public interface BuildEvent
    {
        // Some build events should be processed exclusively in a single
        // frame.  This is usually the case when processing multiple events
        // would cause undeterminitic side-effects.  An example is creating
        // a new base piece will change the environment which needs at least
        // one frame to successfully process.
        bool RequiresFreshFrame();
    }
}
