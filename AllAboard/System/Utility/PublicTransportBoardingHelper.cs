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
        //https://cs2.paradoxwikis.com/Commonly_units_in_the_game
        private static readonly double SimulationFramesPerMinute = 16384U / 90.0;

        public static readonly SharedStatic<uint> MaxAllowedMinutesLate =
            SharedStatic<uint>.GetOrCreate<PublicTransportBoardingHelper>();

        public static bool ArePassengersReady(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData,
            EntityCommandBuffer.ParallelWriter commandBuffer,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            PublicTransport publicTransport,
            int jobIndex, uint simulationFrameIndex)
        {
            var numberOfFramesLate = simulationFrameIndex - publicTransport.m_DepartureFrame;
            var approxMinutesLate = numberOfFramesLate / SimulationFramesPerMinute;
            return ArePassengersReady(passengers, currentVehicleData, commandBuffer, searchTree, approxMinutesLate,
                jobIndex);
        }

        private static bool ArePassengersReady(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData,
            EntityCommandBuffer.ParallelWriter commandBuffer,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            double approxMinutesLate, int jobIndex)
        {
            return approxMinutesLate > MaxAllowedMinutesLate.Data
                ? EndBoarding(passengers, currentVehicleData, commandBuffer, searchTree, jobIndex)
                : AreAllPassengersBoarded(passengers, currentVehicleData);
        }

        private static bool EndBoarding(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleDataLookup,
            EntityCommandBuffer.ParallelWriter commandBuffer, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            int jobIndex)
        {
            for (var i = 0; i < passengers.Length; i++)
            {
                var passenger = passengers[i].m_Passenger;
                //passenger is on train, or passenger is entering the train.
                if (currentVehicleDataLookup.HasComponent(passenger) ||
                    (currentVehicleDataLookup[passenger].m_Flags & CreatureVehicleFlags.Entering) == 0) continue;

                //credit for this logic goes to @WayzWare
                if (currentVehicleDataLookup.TryGetComponent(passenger, out var passengerVehicleData))
                {
                    passengerVehicleData.m_Flags |= CreatureVehicleFlags.Ready;
                    passengerVehicleData.m_Flags &= ~CreatureVehicleFlags.Entering;
                    commandBuffer.SetComponent(jobIndex, passenger, passengerVehicleData);
                    commandBuffer.AddComponent(jobIndex, passenger, default(BatchesUpdated));
                    searchTree.TryRemove(passenger);
                }
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
    }
}