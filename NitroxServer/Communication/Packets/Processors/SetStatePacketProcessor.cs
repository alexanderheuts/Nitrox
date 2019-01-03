using NitroxModel.Packets;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;
using NitroxServer.GameLogic.Bases;

namespace NitroxServer.Communication.Packets.Processors
{
    class SetStatePacketProcessor : AuthenticatedPacketProcessor<SetState>
    {
        private readonly BaseData baseData;
        private readonly PlayerManager playerManager;

        public SetStatePacketProcessor(BaseData baseData, PlayerManager playerManager)
        {
            this.baseData = baseData;
            this.playerManager = playerManager;
        }

        public override void Process(SetState packet, Player player)
        {
            baseData.BasePieceSetState(packet.Guid, packet.BaseGuid, packet.GameObjectType, packet.Value, packet.SetAmount);
            playerManager.SendPacketToOtherPlayers(packet, player);
        }
    }
}
