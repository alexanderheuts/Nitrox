using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Logger;
using ProtoBufNet;
using System.Collections.Generic;

namespace NitroxServer.GameLogic.Bases
{
    [ProtoContract]
    public class BaseData
    {
        [ProtoIgnore]
        private object changeLock = new object();

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
                Log.Debug("AddBasePiece GUID={0} ParentGUID={1} BaseGUID={2}", basePiece.Guid, basePiece.ParentGuid, basePiece.BaseGuid);
                DebugOutput();
                basePiecesByGuid.Add(basePiece.ParentGuid, basePiece);
                Log.Debug("AddBasePiece done.");
                DebugOutput();
            }
        }

        public void BasePieceConstructionAmountChanged(string guid, string parentGuid, float constructionAmount, bool constructing)
        {
            BasePiece basePiece;

            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    basePiece.ConstructionAmount = constructionAmount;
                }
            }
        }

        public void BasePieceConstructionCompleted(string guid, string parentGuid)
        {
            BasePiece basePiece;

            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    Log.Debug("BasePieceConstructionCompleted GUID={0} ParentGUID={1} BaseGUID={2}", basePiece.Guid, basePiece.ParentGuid, basePiece.BaseGuid);
                    DebugOutput();
                    basePiece.ConstructionAmount = 1.0f;
                    basePiece.ConstructionCompleted = true;                    

                    completedBasePieceHistory.Add(parentGuid, basePiece);
                    Log.Debug("BasePieceConstructionCompleted done.");
                    DebugOutput();
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
                    basePiece.ConstructionCompleted = false;
                }
            }
        }

        public void BasePieceDeconstructionCompleted(string guid, string parentGuid)
        {
            BasePiece basePiece;
            lock (changeLock)
            {
                if (basePiecesByGuid.TryGetValue(parentGuid, out basePiece))
                {
                    completedBasePieceHistory.Remove(parentGuid);

                    basePiecesByGuid.Remove(parentGuid);
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
