using NitroxModel.DataStructures.GameLogic;
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
            Log.Debug("Adding basePiece.");
            lock(changeLock)
            {
                basePiecesByGuid.Add(basePiece.Guid, basePiece);
            }
            DebugOutput();
        }

        public void BasePieceConstructionAmountChanged(string guid, string baseGuid, Type goType, float constructionAmount)
        {
            Log.Debug("Trying to change ConstructionAmount for Guid={0} BaseGuid={1} Type={2} Amount={3}", guid, baseGuid, goType, constructionAmount);
            BasePiece basePiece;

            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(guid, out basePiece))
                {
                    basePiece.ConstructionAmount = constructionAmount;

                    Log.Debug("Updating BaseGuid");
                    Log.Debug("Old GUID={0} New GUID={1}", basePiece.BaseGuid, baseGuid);
                    basePiece.BaseGuid = baseGuid;
                }
            }
        }

        public void BasePieceDeconstructionBegin(string guid, string baseGuid)
        {
            BasePiece basePiece;
            // Due to the way Deconstruction works for Bases we're going to remove a Base item immediately.
            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(guid, out basePiece))
                {
                    // If we can find it, it's a piece of Furniture being removed.
                    basePiece.ConstructionAmount = 0.95f;

                    completedBasePieceHistory.Remove(guid);
                }
                else
                {
                    bool remove = false;
                    string key = "";
                    // If we can't find it, we need to do something smart to find the base piece
                    foreach (KeyValuePair<string, BasePiece> kvp in completedBasePieceHistory)
                    {
                        if (kvp.Value.BaseGuid == baseGuid && kvp.Value.TypeOfConstructable == typeof(ConstructableBase))
                        {
                            Log.Debug("Found basePiece with GUID={0}. Removing it.", kvp.Key);
                            key = kvp.Key;
                            remove = true;
                        }
                    }
                    if (remove)
                    {
                        basePiecesByGuid.Remove(key);
                        completedBasePieceHistory.Remove(key);
                    }
                }
            }
        }

        public void BasePieceDeconstructionCompleted(string guid, string baseGuid)
        {
            Log.Debug("DeconstructionCompleted for Guid={0} BaseGuid={1}", guid, baseGuid);
            BasePiece basePiece;
            lock (changeLock)
            {
                DebugOutput();
                if (basePiecesByGuid.TryGetValue(guid, out basePiece))
                {
                    basePiecesByGuid.Remove(guid);
                }
                else if(completedBasePieceHistory.TryGetValue(guid, out basePiece))
                {
                    completedBasePieceHistory.Remove(guid);
                }
                else
                {
                    bool remove = false;
                    string key = "";
                    // If we can't find it, we need to do something smart to find the base piece
                    foreach (KeyValuePair<string, BasePiece> kvp in completedBasePieceHistory)
                    {
                        if (kvp.Value.BaseGuid == baseGuid && kvp.Value.TypeOfConstructable == typeof(ConstructableBase))
                        {
                            Log.Debug("Found basePiece with GUID={0}. Removing it.", kvp.Key);
                            key = kvp.Key;
                            remove = true;
                        }
                    }
                    if(remove)
                    {
                        basePiecesByGuid.Remove(key);
                        completedBasePieceHistory.Remove(key);
                    }
                }
                DebugOutput();
            }
        }

        public void BasePieceSetState(string guid, string baseGuid, Type goType, bool value, bool setAmount)
        {
            Log.Debug("Trying to setState for Guid={0} BaseGuid={1} Type={2} Value={3} SetAmount={4}", guid, baseGuid, goType, value, setAmount);
            BasePiece basePiece;
            lock (changeLock)
            {
                DebugOutput();
                if (basePiecesByGuid.TryGetValue(guid, out basePiece))
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

                    if(!basePiece.ConstructionCompleted && setAmount)
                    {
                        Log.Debug("Updating BaseGuid");
                        Log.Debug("Old GUID={0} New GUID={1}", basePiece.BaseGuid, baseGuid);
                        basePiece.BaseGuid = baseGuid;
                    }
                    
                    if(basePiece.ConstructionCompleted)
                    {
                        Log.Debug("Construction Complete");
                        completedBasePieceHistory.Add(guid, basePiece);
                    }
                }
                DebugOutput();
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
            Log.Debug("=================");
            Log.Debug("BaseData Debugger");
            Log.Debug("BaseData history count={0} total piece count={1}", completedBasePieceHistory.Count, basePiecesByGuid.Count);

            Log.Debug("=== BaseData History");
            foreach(KeyValuePair<string, BasePiece> kvp in completedBasePieceHistory)
            {
                Log.Debug("BasePiece with GUID={0} BaseGUID={1} Type={2}", kvp.Value.Guid, kvp.Value.BaseGuid, kvp.Value.TechType);
            }

            Log.Debug("=== BaseData All Pieces");
            foreach (KeyValuePair<string, BasePiece> kvp in basePiecesByGuid)
            {
                Log.Debug("BasePiece with GUID={0} BaseGUID={1} Type={2}", kvp.Value.Guid, kvp.Value.BaseGuid, kvp.Value.TechType);
            }
            Log.Debug("=================");
        }
    }
}
