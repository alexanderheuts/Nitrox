﻿using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using ProtoBufNet;
using System;
using System.Collections.Generic;

namespace NitroxServer.GameLogic.Bases
{
    [ProtoContract]
    public class BaseData
    {
        [ProtoIgnore]
        private readonly object changeLock = new object();

        [ProtoMember(1)]
        public Dictionary<string, BasePiece> SerializableBasePiecesByGuid
        {
            get
            {
                lock (changeLock)
                {
                    return new Dictionary<string, BasePiece>(basePiecesByGuid);
                }
            }
            set { basePiecesByGuid = value; }
        }

        [ProtoMember(2)]
        public Dictionary<string, BasePiece> SerializableCompletedBasePieceHistory
        {
            get
            {
                lock (changeLock)
                {
                    return new Dictionary<string, BasePiece>(completedBasePieceHistory);
                }
            }
            set { completedBasePieceHistory = value; }
        }

        [ProtoIgnore]
        private Dictionary<string, BasePiece> basePiecesByGuid = new Dictionary<string, BasePiece>();

        [ProtoIgnore]
        private Dictionary<string, BasePiece> completedBasePieceHistory = new Dictionary<string, BasePiece>();

        public void AddBasePiece(BasePiece basePiece)
        {
            lock(changeLock)
            {
                basePiecesByGuid.Add(basePiece.ParentGuid, basePiece);
            }
        }

        public void BasePieceConstructionAmountChanged(string guid, string parentGuid, Type goType, float constructionAmount)
        {
            Log.Debug("Trying to change ConstructionAmount for Guid={0} ParentGuid={1} Type={2} Amount={3}", guid, parentGuid, goType, constructionAmount);
            BasePiece basePiece;

            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    basePiece.ConstructionAmount = constructionAmount;
                }
            }
        }

        public void BasePieceDeconstructionBegin(string guid, string parentGuid)
        {
            BasePiece basePiece;

            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    basePiece.ConstructionAmount = 0.95f;

                    completedBasePieceHistory.Remove(parentGuid);
                }
            }
        }

        public void BasePieceDeconstructionCompleted(string guid, string parentGuid)
        {
            Log.Debug("DeconstructionCompleted for Guid={0} ParentGuid={1}", guid, parentGuid);
            BasePiece basePiece;
            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    basePiecesByGuid.Remove(parentGuid);
                }
                if(completedBasePieceHistory.TryGetValue(parentGuid, out basePiece))
                {
                    completedBasePieceHistory.Remove(parentGuid);
                }
                DebugOutput();
            }
        }

        public void BasePieceSetState(string guid, string parentGuid, Type goType, bool value, bool setAmount)
        {
            Log.Debug("Trying to setState for Guid={0} ParentGuid={1} Type={2} Value={3} SetAmount={4}", guid, parentGuid, goType, value, setAmount);
            BasePiece basePiece;
            lock (changeLock)
            {
                if(basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    if(basePiece.ConstructionCompleted == value)
                    {
                        // We're not changing the state, as it's already set.
                        Log.Debug("State already set, abort");
                        return;
                    }

                    basePiece.ConstructionCompleted = value;
                    if(setAmount)
                    {
                        Log.Debug("Updating amount");
                        basePiece.ConstructionAmount = (!basePiece.ConstructionCompleted) ? 0f : 1f;
                    }

                    if(basePiece.ConstructionCompleted)
                    {
                        Log.Debug("Construction Complete");
                        completedBasePieceHistory.Add(parentGuid, basePiece);
                    }
                }
            }
        }

        public List<BasePiece> GetBasePiecesForNewlyConnectedPlayer()
        {
            List<BasePiece> basePieces = new List<BasePiece>();
            
            lock(changeLock)
            {
                // Play back all completed base pieces first (other pieces have a dependency on these being done)
                foreach (KeyValuePair<string, BasePiece> kvp in completedBasePieceHistory)
                {
                    basePieces.Add(kvp.Value);
                }

                // Play back pieces that may not be completed yet.
                foreach (KeyValuePair<string, BasePiece> kvp in basePiecesByGuid)
                {
                    // If the basePiece is not in the completedHistory we should assume it can be added
                    if (!completedBasePieceHistory.ContainsKey(kvp.Key))
                    {
                        basePieces.Add(kvp.Value);
                    }
                }
            }

            return basePieces;
        }

        private void DebugOutput()
        {
            Log.Debug("BaseData Debugger");
            Log.Debug("BaseData history count={0} total piece count={1}", completedBasePieceHistory.Count, basePiecesByGuid.Count);

            Log.Debug("BaseData History");
            foreach(KeyValuePair<string, BasePiece> kvp in completedBasePieceHistory)
            {
                Log.Debug("BasePiece with KEY={0} GUID={1} ParentGUID={2} BaseGUID={3} Type={4}", kvp.Key, kvp.Value.Guid, kvp.Value.ParentGuid, kvp.Value.BaseGuid, kvp.Value.TechType);
            }

            Log.Debug("BaseData All Pieces");
            foreach (KeyValuePair<string, BasePiece> kvp in basePiecesByGuid)
            {
                Log.Debug("BasePiece with KEY={0} GUID={1} ParentGUID={2} BaseGUID={3} Type={4}", kvp.Key, kvp.Value.Guid, kvp.Value.ParentGuid, kvp.Value.BaseGuid, kvp.Value.TechType);
            }
        }
    }
}
