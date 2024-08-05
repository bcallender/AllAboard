using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Vehicles;
using Unity.Burst;
using Unity.Entities;

namespace AllAboard.System.Utility
{
    public class PassengerBoardingChecks
    {
        //https://cs2.paradoxwikis.com/Commonly_units_in_the_game
        private static readonly double SimulationFramesPerMinute = 16384U / 90.0;

        public static readonly SharedStatic<uint> MaxAllowedMinutesLate =
            SharedStatic<uint>.GetOrCreate<PassengerBoardingChecks>();

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

        public static bool ArePassengersReady(DynamicBuffer<Passenger> passengers,
            ComponentLookup<CurrentVehicle> currentVehicleData,
            EntityCommandBuffer.ParallelWriter commandBuffer,
            NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree,
            double approxSecondsLate, int jobIndex)
        {
            return approxSecondsLate > MaxAllowedMinutesLate.Data
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