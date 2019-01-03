using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic.Bases;
using NitroxModel.Packets;

namespace NitroxClient.Communication.Packets.Processors
{
    public class DeconstructionBeginProcessor : ClientPacketProcessor<DeconstructionBegin>
    {
        private BuildThrottlingQueue buildEventQueue;

        public DeconstructionBeginProcessor(BuildThrottlingQueue buildEventQueue)
        {
            this.buildEventQueue = buildEventQueue;
        }

        public override void Process(DeconstructionBegin packet)
        {
            buildEventQueue.EnqueueDeconstructionBegin(packet.Guid, packet.BaseGuid, packet.GameObjectType);
        }
    }
}
