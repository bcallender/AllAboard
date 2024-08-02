using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Vehicles;
using Unity.Entities;

namespace AllAboard.System.Utility
{
    public static class PassengerBoardingChecks
    {
        private static readonly uint MaxAllowedSecondsLate = 30U;
        private static readonly uint SimulationFramesPerSecond = 30U;

        public static uint CalculateDwellDelay(uint simulationFrameIndex, CargoTransport cargoTransport,
            PublicTransport publicTransport)
        {
            var intendedDepartureFrame = simulationFrameIndex > publicTransport.m_DepartureFrame
                ? publicTransport.m_DepartureFrame
                : cargoTransport.m_DepartureFrame;
            var numberOfFramesLate = simulationFrameIndex - intendedDepartureFrame;
            var approxSecondsLate = numberOfFramesLate / SimulationFramesPerSecond;
            return approxSecondsLate;
        }

        public static bool ArePassengersReady(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData,
            EntityCommandBuffer.ParallelWriter commandBuffer,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            uint approxSecondsLate, int jobIndex)
        {
            return approxSecondsLate > MaxAllowedSecondsLate
                ? BruteForceBoarding(passengers, currentVehicleData, commandBuffer, searchTree, jobIndex)
                : AreAllPassengersBoarded(passengers, currentVehicleData);
        }

        private static bool BruteForceBoarding(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleDataLookup,
            EntityCommandBuffer.ParallelWriter commandBuffer, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            int jobIndex)
        {
            for (var i = 0; i < passengers.Length; i++)
            {
                var passenger = passengers[i].m_Passenger;
                if (currentVehicleDataLookup.HasComponent(passenger)) continue;


                if ((currentVehicleDataLookup[passenger].m_Flags & CreatureVehicleFlags.Entering) != 0)
                {
                    //credit for this logic goes to @WayzWare
                    if (currentVehicleDataLookup.TryGetComponent(passenger, out var currentVehicleData))
                    {
                        currentVehicleData.m_Flags |= CreatureVehicleFlags.Ready;
                        currentVehicleData.m_Flags &= ~CreatureVehicleFlags.Entering;
                        commandBuffer.SetComponent(jobIndex, passenger, currentVehicleData);
                        commandBuffer.AddComponent(jobIndex, passenger, default(BatchesUpdated));
                        searchTree.TryRemove(passenger);
                    }
                    else
                    {
                        return false;
                    }
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
                    (currentVehicleData[passenger2].m_Flags & CreatureVehicleFlags.Ready) ==
                    0)
                    return false;
            }

            return true;
        }
    }
}