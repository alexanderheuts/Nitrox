using NitroxModel.Logger;
using NitroxModel.Packets;
using NitroxModel.Packets.Processors.Abstract;
using Lidgren.Network;
using System;

namespace NitroxServer.Communication
{
    public class Connection : IProcessorContext
    {
        private readonly NetServer server;
        private readonly NetConnection connection;

        public Connection(NetServer server, NetConnection connection)
        {
            this.server = server;
            this.connection = connection;
        }
        
        public void SendPacket(Packet packet)
        {
            if (connection.Status == NetConnectionStatus.Connected)
            {
                try
                {
                    byte[] packetData = packet.Serialize();
                    NetOutgoingMessage om = server.CreateMessage();
                    om.Write(packetData);

                    connection.SendMessage(om, packet.DeliveryMethod, (int)packet.UdpChannel);
                }
                catch(Exception e)
                {
                    Log.Error("SendPacket failure: ", e.ToString());
                }
            }
            else
            {
                Log.Info("Cannot send packet to a closed connection.");
            }
        }
    }
}
