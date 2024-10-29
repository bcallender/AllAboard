using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Vehicles;
using Unity.Burst;
using Unity.Entities;

namespace AllAboard.System.Utility
{
    public class PublicTransportBoardingHelper
    {
        public enum TransportFamily
        {
            Train,
            Bus
        }

        //https://cs2.paradoxwikis.com/Commonly_units_in_the_game
        private const double SimulationFramesPerMinute = 16384U / 90.0;

        public static readonly SharedStatic<uint> TrainMaxAllowedMinutesLate =
            SharedStatic<uint>.GetOrCreate<PublicTransportBoardingHelper, TrainMaxAllowedMinutesLateKey>();

        public static readonly SharedStatic<uint> BusMaxAllowedMinutesLate =
            SharedStatic<uint>.GetOrCreate<PublicTransportBoardingHelper, BusMaxAllowedMinutesLateKey>();


        /*
         * https://docs.unity3d.com/Packages/com.unity.burst@1.7/manual/docs/AdvancedUsages.html#shared-static
         * don't forget your static initializers kids, or SharedStatic<bool> will just have undefined behavior
         */
        static PublicTransportBoardingHelper()
        {
            TrainMaxAllowedMinutesLate.Data = 0;
            BusMaxAllowedMinutesLate.Data = 0;
        }

        public static bool ArePassengersReady(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData,
            PublicTransport publicTransport,
            TransportFamily transportFamily,
            uint simulationFrameIndex)
        {
            var numberOfFramesLate = simulationFrameIndex - publicTransport.m_DepartureFrame;
            var approxMinutesLate = numberOfFramesLate / SimulationFramesPerMinute;
            return ArePassengersReady(passengers, currentVehicleData, transportFamily,
                approxMinutesLate);
        }

        private static bool ArePassengersReady(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData,
            TransportFamily transportFamily,
            double approxMinutesLate)
        {
            uint maxAllowedSecondsLate;
            switch (transportFamily)
            {
                case TransportFamily.Bus:
                    maxAllowedSecondsLate = BusMaxAllowedMinutesLate.Data;
                    break;
                case TransportFamily.Train:
                default:
                    maxAllowedSecondsLate = TrainMaxAllowedMinutesLate.Data;
                    break;
            }

            return approxMinutesLate > maxAllowedSecondsLate || AreAllPassengersBoarded(passengers, currentVehicleData);
        }


        private static bool EndBoardingWithCleanup(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleDataLookup,
            EntityCommandBuffer.ParallelWriter commandBuffer, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            int jobIndex)
        {
            for (var i = 0; i < passengers.Length; i++)
            {
                var passenger = passengers[i].m_Passenger;
                //credit for this logic goes to @WayzWare.
                // If we find the passenger in the vehicle data, and the passenger is not Ready, we can clean the passenger up.
                if (!currentVehicleDataLookup.TryGetComponent(passenger,
                        out var passengerVehicleData)
                    || (passengerVehicleData.m_Flags & CreatureVehicleFlags.Ready) == 0)
                    continue;
                passengerVehicleData.m_Flags |= CreatureVehicleFlags.Ready;
                passengerVehicleData.m_Flags &= ~CreatureVehicleFlags.Entering;
                commandBuffer.SetComponent(jobIndex, passenger, passengerVehicleData);
                commandBuffer.AddComponent(jobIndex, passenger, default(BatchesUpdated));
                searchTree.TryRemove(passenger);
            }

            return true;
        }

        private static bool AreAllPassengersBoarded(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData)
        {
            for (var index = 0; index < passengers.Length; ++index)
            {
                var passenger2 = passengers[index].m_Passenger;
                if (currentVehicleData.HasComponent(passenger2) &&
                    (currentVehicleData[passenger2].m_Flags & CreatureVehicleFlags.Ready) == 0)
                    return false;
            }

            return true;
        }

        //these private classes are absolute unity wizardry for having multiple SharedStatic in one class.
        private class TrainMaxAllowedMinutesLateKey
        {
        }

        private class BusMaxAllowedMinutesLateKey
        {
        }

        private class CleanUpPathfindingQueueKey
        {
        }
    }
}