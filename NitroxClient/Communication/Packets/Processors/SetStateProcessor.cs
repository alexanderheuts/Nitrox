using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic.Bases;
using NitroxModel.Logger;
using NitroxModel.Packets;

namespace NitroxClient.Communication.Packets.Processors
{
    public class SetStateProcessor : ClientPacketProcessor<SetState>
    {
        private BuildThrottlingQueue buildEventQueue;

        public SetStateProcessor(BuildThrottlingQueue buildEventQueue)
        {
            this.buildEventQueue = buildEventQueue;
        }

        public override void Process(SetState setStatePacket)
        {
            buildEventQueue.EnqueueSetState(setStatePacket.Guid, setStatePacket.ParentGuid, setStatePacket.GameObjectType, setStatePacket.Value, setStatePacket.SetAmount);
        }
    }
}
