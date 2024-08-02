using Game;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace AllAboard
{
    /// <summary>
    ///     DOES NOT WORK!
    /// </summary>
    public partial class InstantBoardingSystem : GameSystemBase
    {
        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<CurrentVehicle>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Deleted>()
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<Pet>()
                }
            });
        }

        protected override void OnUpdate()
        {
            var passengers = _query.ToEntityArray(Allocator.Temp);

            foreach (var passenger in passengers)
            {
                var passengerData = EntityManager.GetComponentData<CurrentVehicle>(passenger);

                if ((passengerData.m_Flags & CreatureVehicleFlags.Entering) != 0)
                {
                    passengerData.m_Flags |= CreatureVehicleFlags.Ready;
                    passengerData.m_Flags &= ~CreatureVehicleFlags.Entering;
                    EntityManager.SetComponentData(passenger, passengerData);
                    EntityManager.AddComponentData(passenger, default(BatchesUpdated));
                }
            }
        }
    }
}