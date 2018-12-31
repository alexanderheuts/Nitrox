using System.Collections.Generic;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Packets;
using NitroxModel.Logger;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;
using NitroxServer.Serialization.World;

namespace NitroxServer.Communication.Packets.Processors
{
    public class PlayerJoiningMultiplayerSessionProcessor : UnauthenticatedPacketProcessor<PlayerJoiningMultiplayerSession>
    {
        private readonly TimeKeeper timeKeeper;
        private readonly EscapePodManager escapePodManager;
        private readonly PlayerManager playerManager;
        private readonly World world;

        public PlayerJoiningMultiplayerSessionProcessor(TimeKeeper timeKeeper, EscapePodManager escapePodManager,
            PlayerManager playerManager, World world)
        {
            this.timeKeeper = timeKeeper;
            this.escapePodManager = escapePodManager;
            this.playerManager = playerManager;
            this.world = world;
        }

        public override void Process(PlayerJoiningMultiplayerSession packet, Connection connection)
        {
            Player player = playerManager.CreatePlayer(connection, packet.ReservationKey);
            player.SendPacket(new TimeChange(timeKeeper.GetCurrentTime()));


            escapePodManager.AssignPlayerToEscapePod(player.Id);

            BroadcastEscapePods broadcastEscapePods = new BroadcastEscapePods(escapePodManager.GetEscapePods());
            playerManager.SendPacketToAllPlayers(broadcastEscapePods);

            PlayerJoinedMultiplayerSession playerJoinedPacket = new PlayerJoinedMultiplayerSession(player.PlayerContext);
            playerManager.SendPacketToOtherPlayers(playerJoinedPacket, player);

            Log.Debug("Preparing InitialPlayerSync-packet");
            InitialPlayerSync initialPlayerSync = new InitialPlayerSync(player.Id.ToString(),
                                                                       world.PlayerData.GetEquippedItemsForInitialSync(player.Name),
                                                                       world.BaseData.GetBasePiecesForNewlyConnectedPlayer(),
                                                                       world.VehicleData.GetVehiclesForInitialSync(),
                                                                       world.InventoryData.GetAllItemsForInitialSync(),
                                                                       world.GameData.PDAState.GetInitialPdaData(),
                                                                       world.PlayerData.PlayerSpawn(player.Name),
                                                                       world.PlayerData.GetSubRootGuid(player.Name),
                                                                       world.PlayerData.Stats(player.Name),
                                                                       getRemotePlayerData(player));

            Log.Debug("Ready to send InitialPlayerSync-packet");
            player.SendPacket(initialPlayerSync);
            Log.Debug("Sent InitialPlayerSync-packet");
        }

        private List<InitialRemotePlayerData> getRemotePlayerData(Player player)
        {
            List<InitialRemotePlayerData> playerData = new List<InitialRemotePlayerData>();

            foreach (Player otherPlayer in playerManager.GetPlayers())
            {
                if (!player.Equals(otherPlayer))
                {
                    InitialRemotePlayerData remotePlayer = new InitialRemotePlayerData(otherPlayer.PlayerContext, otherPlayer.Position, otherPlayer.SubRootGuid);
                    playerData.Add(remotePlayer);
                }
            }

            return playerData;
        }
    }
}
