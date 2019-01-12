using NitroxClient.Communication.Abstract;
using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic;
using NitroxClient.GameLogic.Helper;
using NitroxClient.Unity.Helper;
using NitroxModel.Helper;
using NitroxModel.Logger;
using NitroxModel.Packets;
using NitroxModel.DataStructures.Util;
using UnityEngine;

namespace NitroxClient.Communication.Packets.Processors
{
    public class VehicleUndockingProcessor : ClientPacketProcessor<VehicleUndocking>
    {
        private readonly IPacketSender packetSender;
        private readonly Vehicles vehicles;
        private readonly PlayerManager remotePlayerManager;

        public VehicleUndockingProcessor(IPacketSender packetSender, Vehicles vehicles, PlayerManager remotePlayerManager)
        {
            this.packetSender = packetSender;
            this.vehicles = vehicles;
            this.remotePlayerManager = remotePlayerManager;
        }

        public override void Process(VehicleUndocking packet)
        {
            GameObject vehicleGo = GuidHelper.RequireObjectFrom(packet.VehicleGuid);
            GameObject vehicleDockingBayGo = GuidHelper.RequireObjectFrom(packet.DockGuid);

            Vehicle vehicle = vehicleGo.RequireComponent<Vehicle>();
            SubRoot subRoot = vehicleGo.GetComponent<SubRoot>();
            VehicleDockingBay vehicleDockingBay = vehicleDockingBayGo.RequireComponent<VehicleDockingBay>();

            Base vehicleDockBase = vehicleDockingBay.GetComponentInParent<Base>();
            BaseCell vehicleDockBaseCell = vehicleDockingBay.GetComponentInParent<BaseCell>();

            Int3 cellPosition = vehicleDockBase.WorldToGrid(vehicleDockBaseCell.transform.position);

            vehicleDockingBay.SetVehicleUndocked();

            vehicleDockBase.ClearGeometry();
            vehicleDockBase.ReflectionCall("BuildGeometry", false, false);
            vehicleDockBase.ReflectionCall("BuildPillars", false, false);

            //Transform targetTransform = vehicleDockBase.GetCellObject(cellPosition);
            
            vehicleDockBase.ReflectionCall("RecomputeOccupiedCells");
            vehicleDockBase.ReflectionCall("RecomputeOccupiedBounds");

            //SubRoot sr = vehicleDockingBay.GetSubRoot();
            //sr.

            //using (packetSender.Suppress<VehicleUndocking>())
            //{
            //    Optional<RemotePlayer> remotePilot = remotePlayerManager.Find(packet.PlayerId);
            //    Log.Info("Pilot: " + remotePilot.ToString() + " Vehicle: " + vehicle.ToString() + " Bay: " + vehicleDockingBay.ToString());
            //    if (remotePilot.IsPresent())
            //    {
            //        Log.Trace("VehicleUndockingProcessor: Setting remote player");
            //        RemotePlayer remotePlayer = remotePilot.Get();
            //        remotePlayer.SetVehicle(vehicle);
            //        remotePlayer.SetPilotingChair((subRoot != null) ? subRoot.GetComponentInChildren<PilotingChair>() : null);
            //        remotePlayer.SetSubRoot(subRoot);
            //        remotePlayer.AnimationController.UpdatePlayerAnimations = true;

            //        //vehicleDockingBay.OnUndockingStart();
            //        Log.Trace("VehicleUndockingProcessor: Setting Player inside");
            //        vehicle.SetPlayerInside(true);

            //        Log.Trace("VehicleUndockingProcessor: SetOnPilotMode");
            //        vehicles.SetOnPilotMode(packet.VehicleGuid, packet.PlayerId, true);

            //        // OnPlayerCinematicModeStart
            //        Log.Trace("VehicleUndockingProcessor: Broadcast Bay");
            //        vehicleDockingBay.subRoot.BroadcastMessage("OnLaunchBayOpening", SendMessageOptions.DontRequireReceiver);

            //        // SetVehicleUndocked
            //        Log.Trace("VehicleUndockingProcessor: Dock Set Undocked");
            //        vehicleDockingBay.SetVehicleUndocked();

            //        // OnUndockingComplete (Bay)
            //        Log.Trace("VehicleUndockingProcessor: Dock update");
            //        SkyEnvironmentChanged.Broadcast(vehicleGo, (GameObject)null);
            //        vehicleDockingBay.ReflectionSet("_dockedVehicle", null);

            //        vehicleDockingBay.ReflectionSet("vehicle_docked_param", false);

            //        // OnUndockingComplete (Vehicle)
            //        Log.Trace("VehicleUndockingProcessor: Vehicle update");
            //        vehicle.docked = false;
            //        vehicle.useRigidbody.AddForce(Vector3.down * 5f, ForceMode.VelocityChange);

            //        Log.Trace("VehicleUndockingProcessor: Done");
            //    }


            //}
        }
    }
}
