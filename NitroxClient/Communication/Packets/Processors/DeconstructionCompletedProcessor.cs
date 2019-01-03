using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic.Bases;
using NitroxModel.Packets;

namespace NitroxClient.Communication.Packets.Processors
{
    public class DeconstructionCompletedProcessor : ClientPacketProcessor<DeconstructionCompleted>
    {
        private BuildThrottlingQueue buildEventQueue;

        public DeconstructionCompletedProcessor(BuildThrottlingQueue buildEventQueue)
        {
            this.buildEventQueue = buildEventQueue;
        }

        public override void Process(DeconstructionCompleted packet)
        {
            buildEventQueue.EnqueueDeconstructionCompleted(packet.Guid, packet.BaseGuid, packet.GameObjectType);
        }
    }
}
