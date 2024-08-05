using System.Runtime.CompilerServices;
using AllAboard.System.Utility;
using Colossal.Collections;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;
using CargoTransport = Game.Vehicles.CargoTransport;
using CarLaneFlags = Game.Vehicles.CarLaneFlags;
using Color = Game.Routes.Color;
using PublicTransport = Game.Vehicles.PublicTransport;
using SearchSystem = Game.Objects.SearchSystem;
using StorageCompany = Game.Companies.StorageCompany;
using SubLane = Game.Net.SubLane;
using TransportStation = Game.Buildings.TransportStation;

namespace AllAboard.System.Patched
{
// Decompiled with JetBrains decompiler
// Type: Game.Simulation.TransportCarAISystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B32C7D7E-7CA9-498A-8CE0-EA5E934FB7A7
// Assembly location: F:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll


    [CompilerGenerated]
    public partial class PatchedTransportCarAISystem : GameSystemBase
    {
        private TypeHandle __TypeHandle;
        private TransportBoardingHelpers.BoardingLookupData m_BoardingLookupData;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityArchetype m_EvacuationRequestArchetype;
        private EntityArchetype m_HandleRequestArchetype;
        private SearchSystem m_ObjectSearchSystem;
        private PathfindSetupSystem m_PathfindSetupSystem;
        private EntityArchetype m_PrisonerTransportRequestArchetype;
        private SimulationSystem m_SimulationSystem;
        private EntityArchetype m_TransportVehicleRequestArchetype;
        private EntityQuery m_VehicleQuery;

        [Preserve]
        public PatchedTransportCarAISystem()
        {
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 16;
        }

        public override int GetUpdateOffset(SystemUpdatePhase phase)
        {
            return 1;
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_PathfindSetupSystem = World.GetOrCreateSystemManaged<PathfindSetupSystem>();

            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();

            m_BoardingLookupData = new TransportBoardingHelpers.BoardingLookupData(this);

            m_VehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[5]
                {
                    ComponentType.ReadWrite<CarCurrentLane>(),
                    ComponentType.ReadOnly<Owner>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadWrite<PathOwner>(),
                    ComponentType.ReadWrite<Target>()
                },
                Any = new ComponentType[2]
                {
                    ComponentType.ReadWrite<CargoTransport>(),
                    ComponentType.ReadWrite<PublicTransport>()
                },
                None = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<TripSource>(),
                    ComponentType.ReadOnly<OutOfControl>()
                }
            });

            m_TransportVehicleRequestArchetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<TransportVehicleRequest>(),
                ComponentType.ReadWrite<RequestGroup>());

            m_EvacuationRequestArchetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<EvacuationRequest>(),
                ComponentType.ReadWrite<RequestGroup>());

            m_PrisonerTransportRequestArchetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PrisonerTransportRequest>(),
                ComponentType.ReadWrite<RequestGroup>());

            m_HandleRequestArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(),
                ComponentType.ReadWrite<Event>());

            RequireForUpdate(m_VehicleQuery);
            m_ObjectSearchSystem = World.GetOrCreateSystemManaged<SearchSystem>();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            var boardingData =
                new TransportBoardingHelpers.BoardingData(Allocator.TempJob);

            m_BoardingLookupData.Update(this);


            __TypeHandle.__Game_Vehicles_LoadingResources_RW_BufferLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Net_SubLane_RO_BufferLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Net_Lane_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Buildings_TransportStation_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_Color_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_BoardingVehicle_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup.Update(
                ref CheckedStateRef);


            __TypeHandle.__Game_Simulation_EvacuationRequest_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup.Update(
                ref CheckedStateRef);


            __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup.Update(
                ref CheckedStateRef);


            __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup.Update(
                ref CheckedStateRef);


            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_CargoTransport_RW_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle.Update(ref CheckedStateRef);


            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);


            // ISSUE: object of a compiler-generated type is created

            var jobHandle = new TransportCarTickJob
            {
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_OwnerType = __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle,
                m_UnspawnedType = __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle,
                m_PathInformationType = __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle,
                m_PrefabRefType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                m_CurrentRouteType = __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentTypeHandle,
                m_PassengerType = __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle,
                m_CargoTransportType = __TypeHandle.__Game_Vehicles_CargoTransport_RW_ComponentTypeHandle,
                m_PublicTransportType = __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentTypeHandle,
                m_CarType = __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle,
                m_CurrentLaneType = __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle,
                m_TargetType = __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle,
                m_PathOwnerType = __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle,
                m_OdometerType = __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle,
                m_CarNavigationLaneType = __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle,
                m_ServiceDispatchType = __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle,
                m_TransformData = __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                m_OwnerData = __TypeHandle.__Game_Common_Owner_RO_ComponentLookup,
                m_PathInformationData = __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup,
                m_PrefabCarData = __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup,
                m_PrefabRefData = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_PublicTransportVehicleData =
                    __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup,
                m_CargoTransportVehicleData =
                    __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup,
                m_ServiceRequestData = __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup,
                m_TransportVehicleRequestData =
                    __TypeHandle.__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup,
                m_EvacuationRequestData = __TypeHandle.__Game_Simulation_EvacuationRequest_RO_ComponentLookup,
                m_PrisonerTransportRequestData =
                    __TypeHandle.__Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup,
                m_WaypointData = __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup,
                m_ConnectedData = __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup,
                m_BoardingVehicleData = __TypeHandle.__Game_Routes_BoardingVehicle_RO_ComponentLookup,
                m_RouteLaneData = __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup,
                m_RouteColorData = __TypeHandle.__Game_Routes_Color_RO_ComponentLookup,
                m_StorageCompanyData = __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup,
                m_TransportStationData = __TypeHandle.__Game_Buildings_TransportStation_RO_ComponentLookup,
                m_LaneData = __TypeHandle.__Game_Net_Lane_RO_ComponentLookup,
                m_SlaveLaneData = __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup,
                m_CurveData = __TypeHandle.__Game_Net_Curve_RO_ComponentLookup,
                m_CurrentVehicleData = __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup,
                m_RouteWaypoints = __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup,
                m_SubLanes = __TypeHandle.__Game_Net_SubLane_RO_BufferLookup,
                m_PathElements = __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup,
                m_LoadingResources = __TypeHandle.__Game_Vehicles_LoadingResources_RW_BufferLookup,
                m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
                m_TransportVehicleRequestArchetype = m_TransportVehicleRequestArchetype,
                m_EvacuationRequestArchetype = m_EvacuationRequestArchetype,
                m_PrisonerTransportRequestArchetype = m_PrisonerTransportRequestArchetype,
                m_HandleRequestArchetype = m_HandleRequestArchetype,
                m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
                m_BoardingData = boardingData.ToConcurrent(),
                m_SearchTree = m_ObjectSearchSystem.GetMovingSearchTree(false, out var dependencies)
            }.ScheduleParallel(m_VehicleQuery, Dependency);


            var inputDeps = boardingData.ScheduleBoarding(this, m_CityStatisticsSystem,
                m_BoardingLookupData, m_SimulationSystem.frameIndex, jobHandle);
            boardingData.Dispose(inputDeps);


            m_PathfindSetupSystem.AddQueueWriter(jobHandle);

            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();

            __AssignQueries(ref CheckedStateRef);


            __TypeHandle.__AssignHandles(ref CheckedStateRef);
        }

        [BurstCompile]
        private struct TransportCarTickJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<Owner> m_OwnerType;
            [ReadOnly] public ComponentTypeHandle<Unspawned> m_UnspawnedType;
            [ReadOnly] public ComponentTypeHandle<PathInformation> m_PathInformationType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            [ReadOnly] public ComponentTypeHandle<CurrentRoute> m_CurrentRouteType;
            [ReadOnly] public BufferTypeHandle<Passenger> m_PassengerType;
            public ComponentTypeHandle<CargoTransport> m_CargoTransportType;
            public ComponentTypeHandle<PublicTransport> m_PublicTransportType;
            public ComponentTypeHandle<Car> m_CarType;
            public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;
            public ComponentTypeHandle<Target> m_TargetType;
            public ComponentTypeHandle<PathOwner> m_PathOwnerType;
            public ComponentTypeHandle<Odometer> m_OdometerType;
            public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;
            public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;
            [ReadOnly] public ComponentLookup<Transform> m_TransformData;
            [ReadOnly] public ComponentLookup<Owner> m_OwnerData;
            [ReadOnly] public ComponentLookup<PathInformation> m_PathInformationData;
            [ReadOnly] public ComponentLookup<CarData> m_PrefabCarData;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly] public ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleData;
            [ReadOnly] public ComponentLookup<CargoTransportVehicleData> m_CargoTransportVehicleData;
            [ReadOnly] public ComponentLookup<ServiceRequest> m_ServiceRequestData;
            [ReadOnly] public ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;
            [ReadOnly] public ComponentLookup<EvacuationRequest> m_EvacuationRequestData;
            [ReadOnly] public ComponentLookup<PrisonerTransportRequest> m_PrisonerTransportRequestData;
            [ReadOnly] public ComponentLookup<Waypoint> m_WaypointData;
            [ReadOnly] public ComponentLookup<Connected> m_ConnectedData;
            [ReadOnly] public ComponentLookup<BoardingVehicle> m_BoardingVehicleData;
            [ReadOnly] public ComponentLookup<RouteLane> m_RouteLaneData;
            [ReadOnly] public ComponentLookup<Color> m_RouteColorData;
            [ReadOnly] public ComponentLookup<StorageCompany> m_StorageCompanyData;
            [ReadOnly] public ComponentLookup<TransportStation> m_TransportStationData;
            [ReadOnly] public ComponentLookup<Lane> m_LaneData;
            [ReadOnly] public ComponentLookup<SlaveLane> m_SlaveLaneData;
            [ReadOnly] public ComponentLookup<Curve> m_CurveData;
            [ReadOnly] public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;
            [ReadOnly] public BufferLookup<RouteWaypoint> m_RouteWaypoints;
            [ReadOnly] public BufferLookup<SubLane> m_SubLanes;
            [NativeDisableParallelForRestriction] public BufferLookup<PathElement> m_PathElements;
            [NativeDisableParallelForRestriction] public BufferLookup<LoadingResources> m_LoadingResources;
            [ReadOnly] public uint m_SimulationFrameIndex;
            [ReadOnly] public EntityArchetype m_TransportVehicleRequestArchetype;
            [ReadOnly] public EntityArchetype m_EvacuationRequestArchetype;
            [ReadOnly] public EntityArchetype m_PrisonerTransportRequestArchetype;
            [ReadOnly] public EntityArchetype m_HandleRequestArchetype;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public TransportBoardingHelpers.BoardingData.Concurrent m_BoardingData;

            //search tree??
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var nativeArray1 = chunk.GetNativeArray(m_EntityType);

                var nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);

                var nativeArray3 =
                    chunk.GetNativeArray(ref m_PathInformationType);

                var nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);

                var nativeArray5 =
                    chunk.GetNativeArray(ref m_CurrentRouteType);

                var nativeArray6 =
                    chunk.GetNativeArray(ref m_CurrentLaneType);

                var nativeArray7 =
                    chunk.GetNativeArray(ref m_CargoTransportType);

                var nativeArray8 =
                    chunk.GetNativeArray(ref m_PublicTransportType);

                var nativeArray9 = chunk.GetNativeArray(ref m_CarType);

                var nativeArray10 = chunk.GetNativeArray(ref m_TargetType);

                var nativeArray11 = chunk.GetNativeArray(ref m_PathOwnerType);

                var nativeArray12 = chunk.GetNativeArray(ref m_OdometerType);

                var bufferAccessor1 =
                    chunk.GetBufferAccessor(ref m_CarNavigationLaneType);

                var bufferAccessor2 =
                    chunk.GetBufferAccessor(ref m_PassengerType);

                var bufferAccessor3 =
                    chunk.GetBufferAccessor(ref m_ServiceDispatchType);

                var isUnspawned = chunk.Has(ref m_UnspawnedType);
                for (var index = 0; index < nativeArray1.Length; ++index)
                {
                    var entity = nativeArray1[index];
                    var owner = nativeArray2[index];
                    var prefabRef = nativeArray4[index];
                    var pathInformation = nativeArray3[index];
                    var car = nativeArray9[index];
                    var currentLane = nativeArray6[index];
                    var pathOwner = nativeArray11[index];
                    var target = nativeArray10[index];
                    var odometer = nativeArray12[index];
                    var navigationLanes = bufferAccessor1[index];
                    var serviceDispatches = bufferAccessor3[index];
                    var currentRoute = new CurrentRoute();
                    if (nativeArray5.Length != 0)
                        currentRoute = nativeArray5[index];
                    var cargoTransport = new CargoTransport();
                    if (nativeArray7.Length != 0)
                        cargoTransport = nativeArray7[index];
                    var publicTransport = new PublicTransport();
                    var passengers = new DynamicBuffer<Passenger>();
                    if (nativeArray8.Length != 0)
                    {
                        publicTransport = nativeArray8[index];
                        passengers = bufferAccessor2[index];
                    }


                    VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned,
                        m_CommandBuffer);

                    Tick(unfilteredChunkIndex, entity, owner, pathInformation, prefabRef, currentRoute,
                        navigationLanes, passengers, serviceDispatches, ref cargoTransport, ref publicTransport,
                        ref car, ref currentLane, ref pathOwner, ref target, ref odometer);
                    nativeArray9[index] = car;
                    nativeArray6[index] = currentLane;
                    nativeArray11[index] = pathOwner;
                    nativeArray10[index] = target;
                    nativeArray12[index] = odometer;
                    if (nativeArray7.Length != 0)
                        nativeArray7[index] = cargoTransport;
                    if (nativeArray8.Length != 0)
                        nativeArray8[index] = publicTransport;
                }
            }

            private void Tick(
                int jobIndex,
                Entity vehicleEntity,
                Owner owner,
                PathInformation pathInformation,
                PrefabRef prefabRef,
                CurrentRoute currentRoute,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<Passenger> passengers,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Car car,
                ref CarCurrentLane currentLane,
                ref PathOwner pathOwner,
                ref Target target,
                ref Odometer odometer)
            {
                PublicTransportVehicleData componentData1;

                var isPublicTransport =
                    m_PublicTransportVehicleData.TryGetComponent(prefabRef.m_Prefab, out componentData1);
                CargoTransportVehicleData componentData2;

                var isCargoVehicle =
                    m_CargoTransportVehicleData.TryGetComponent(prefabRef.m_Prefab, out componentData2);
                if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
                {
                    ResetPath(jobIndex, vehicleEntity, pathInformation, serviceDispatches, ref cargoTransport,
                        ref publicTransport, ref car, ref currentLane, ref pathOwner, isPublicTransport);
                    DynamicBuffer<LoadingResources> bufferData;

                    if (((publicTransport.m_State & PublicTransportFlags.DummyTraffic) != 0 ||
                         (cargoTransport.m_State & CargoTransportFlags.DummyTraffic) != 0) &&
                        m_LoadingResources.TryGetBuffer(vehicleEntity, out bufferData))
                        CheckDummyResources(jobIndex, vehicleEntity, prefabRef, bufferData);
                }

                var flag1 = (cargoTransport.m_State & CargoTransportFlags.EnRoute) == 0 &&
                            (publicTransport.m_State & PublicTransportFlags.EnRoute) == 0;
                var flag2 = false;
                if (isPublicTransport)
                {
                    if ((publicTransport.m_State &
                         (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) !=
                        0)
                    {
                        if (!passengers.IsCreated || passengers.Length >= componentData1.m_PassengerCapacity)
                        {
                            publicTransport.m_State |= PublicTransportFlags.Full;
                            flag1 = false;
                        }
                        else
                        {
                            publicTransport.m_State &= ~PublicTransportFlags.Full;
                        }

                        flag2 = true;
                    }

                    if (odometer.m_Distance >= (double)componentData1.m_MaintenanceRange &&
                        componentData1.m_MaintenanceRange > 0.10000000149011612 &&
                        (publicTransport.m_State & PublicTransportFlags.Refueling) == 0)
                        publicTransport.m_State |= PublicTransportFlags.RequiresMaintenance;
                }

                if (isCargoVehicle && odometer.m_Distance >= (double)componentData2.m_MaintenanceRange &&
                    componentData2.m_MaintenanceRange > 0.10000000149011612 &&
                    (cargoTransport.m_State & CargoTransportFlags.Refueling) == 0)
                    cargoTransport.m_State |= CargoTransportFlags.RequiresMaintenance;
                if (flag1)
                {
                    CheckServiceDispatches(vehicleEntity, serviceDispatches, flag2, ref cargoTransport,
                        ref publicTransport, ref pathOwner);
                    if (serviceDispatches.Length <= math.select(0, 1, flag2) &&
                        (cargoTransport.m_State & (CargoTransportFlags.RequiresMaintenance |
                                                   CargoTransportFlags.DummyTraffic | CargoTransportFlags.Disabled)) ==
                        0 &&
                        (publicTransport.m_State & (PublicTransportFlags.RequiresMaintenance |
                                                    PublicTransportFlags.DummyTraffic |
                                                    PublicTransportFlags.Disabled)) == 0)
                        RequestTargetIfNeeded(jobIndex, vehicleEntity, ref publicTransport, ref cargoTransport);
                }
                else
                {
                    serviceDispatches.Clear();
                    cargoTransport.m_RequestCount = 0;
                    publicTransport.m_RequestCount = 0;
                }

                var flag3 = false;

                if (!m_PrefabRefData.HasComponent(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
                {
                    if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != 0 ||
                        (publicTransport.m_State & PublicTransportFlags.Boarding) != 0)
                    {
                        flag3 = true;

                        StopBoarding(vehicleEntity, currentRoute, passengers, ref cargoTransport,
                            ref publicTransport, ref target, ref odometer, true, isCargoVehicle, jobIndex);
                    }

                    if (VehicleUtils.IsStuck(pathOwner) ||
                        (cargoTransport.m_State & (CargoTransportFlags.Returning | CargoTransportFlags.DummyTraffic)) !=
                        0 ||
                        (publicTransport.m_State &
                         (PublicTransportFlags.Returning | PublicTransportFlags.DummyTraffic)) !=
                        0)
                    {
                        m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, new Deleted());
                        return;
                    }


                    ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                        ref cargoTransport, ref publicTransport, ref car, ref pathOwner, ref target);
                }
                else if (VehicleUtils.PathEndReached(currentLane))
                {
                    if ((cargoTransport.m_State & (CargoTransportFlags.Returning | CargoTransportFlags.DummyTraffic)) !=
                        0 ||
                        (publicTransport.m_State &
                         (PublicTransportFlags.Returning | PublicTransportFlags.DummyTraffic)) !=
                        0)
                    {
                        if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != 0 ||
                            (publicTransport.m_State & PublicTransportFlags.Boarding) != 0)
                        {
                            if (StopBoarding(vehicleEntity, currentRoute, passengers, ref cargoTransport,
                                    ref publicTransport, ref target, ref odometer, false, isCargoVehicle, jobIndex))
                            {
                                flag3 = true;

                                if (!SelectNextDispatch(jobIndex, vehicleEntity, currentRoute, navigationLanes,
                                        serviceDispatches, ref cargoTransport, ref publicTransport, ref car,
                                        ref currentLane, ref pathOwner, ref target, isPublicTransport))
                                {
                                    m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, new Deleted());
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if ((!passengers.IsCreated || passengers.Length <= 0 || !StartBoarding(jobIndex,
                                    vehicleEntity, currentRoute, prefabRef, ref cargoTransport, ref publicTransport,
                                    ref target, isCargoVehicle)) && !SelectNextDispatch(jobIndex, vehicleEntity,
                                    currentRoute, navigationLanes, serviceDispatches, ref cargoTransport,
                                    ref publicTransport, ref car, ref currentLane, ref pathOwner, ref target,
                                    isPublicTransport))
                            {
                                m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, new Deleted());
                                return;
                            }
                        }
                    }
                    else if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != 0 ||
                             (publicTransport.m_State & PublicTransportFlags.Boarding) != 0)
                    {
                        if (StopBoarding(vehicleEntity, currentRoute, passengers, ref cargoTransport,
                                ref publicTransport, ref target, ref odometer, false, isCargoVehicle, jobIndex))
                        {
                            flag3 = true;
                            if ((publicTransport.m_State &
                                 (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) !=
                                0)
                            {
                                if (!SelectNextDispatch(jobIndex, vehicleEntity, currentRoute, navigationLanes,
                                        serviceDispatches, ref cargoTransport, ref publicTransport, ref car,
                                        ref currentLane, ref pathOwner, ref target, isPublicTransport))
                                    ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                        ref cargoTransport, ref publicTransport, ref car, ref pathOwner, ref target);
                            }
                            else if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) == 0 &&
                                     (publicTransport.m_State & PublicTransportFlags.EnRoute) ==
                                     0)
                            {
                                ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                    ref cargoTransport, ref publicTransport, ref car, ref pathOwner, ref target);
                            }
                            else
                            {
                                SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                            }
                        }
                    }
                    else
                    {
                        if ((publicTransport.m_State &
                             (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) ==
                            0 && (!m_RouteWaypoints.HasBuffer(currentRoute.m_Route) ||
                                  !m_WaypointData.HasComponent(target.m_Target)))
                        {
                            ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                ref cargoTransport, ref publicTransport, ref car, ref pathOwner, ref target);
                        }
                        else
                        {
                            if (!StartBoarding(jobIndex, vehicleEntity, currentRoute, prefabRef,
                                    ref cargoTransport, ref publicTransport, ref target, isCargoVehicle))
                            {
                                if ((publicTransport.m_State & (PublicTransportFlags.Evacuating |
                                                                PublicTransportFlags.PrisonerTransport)) !=
                                    0)
                                {
                                    if (!SelectNextDispatch(jobIndex, vehicleEntity, currentRoute, navigationLanes,
                                            serviceDispatches, ref cargoTransport, ref publicTransport, ref car,
                                            ref currentLane, ref pathOwner, ref target, isPublicTransport))
                                        ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner,
                                            serviceDispatches, ref cargoTransport, ref publicTransport, ref car,
                                            ref pathOwner, ref target);
                                }
                                else if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) ==
                                         0 &&
                                         (publicTransport.m_State & PublicTransportFlags.EnRoute) ==
                                         0)
                                {
                                    ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                        ref cargoTransport, ref publicTransport, ref car, ref pathOwner, ref target);
                                }
                                else
                                {
                                    SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                                }
                            }
                        }
                    }
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != 0 ||
                         (publicTransport.m_State & PublicTransportFlags.Boarding) != 0)
                {
                    flag3 = true;

                    StopBoarding(vehicleEntity, currentRoute, passengers, ref cargoTransport, ref publicTransport,
                        ref target, ref odometer, true, isCargoVehicle, jobIndex);
                }

                publicTransport.m_State &= ~(PublicTransportFlags.StopLeft | PublicTransportFlags.StopRight);
                var skipWaypoint = Entity.Null;
                if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != 0 ||
                    (publicTransport.m_State & PublicTransportFlags.Boarding) != 0)
                {
                    if (!flag3) UpdateStop(navigationLanes, ref currentLane, ref publicTransport, ref target);
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.Returning) != 0 ||
                         (publicTransport.m_State & PublicTransportFlags.Returning) != 0)
                {
                    if (!passengers.IsCreated || passengers.Length == 0)
                        SelectNextDispatch(jobIndex, vehicleEntity, currentRoute, navigationLanes,
                            serviceDispatches, ref cargoTransport, ref publicTransport, ref car, ref currentLane,
                            ref pathOwner, ref target, isPublicTransport);
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.Arriving) != 0 ||
                         (publicTransport.m_State & PublicTransportFlags.Arriving) != 0)
                {
                    UpdateStop(navigationLanes, ref currentLane, ref publicTransport, ref target);
                }
                else
                {
                    CheckNavigationLanes(vehicleEntity, currentRoute, navigationLanes, ref cargoTransport,
                        ref publicTransport, ref currentLane, ref pathOwner, ref target, out skipWaypoint);
                }

                cargoTransport.m_State &= ~CargoTransportFlags.Testing;
                publicTransport.m_State &= ~PublicTransportFlags.Testing;

                FindPathIfNeeded(vehicleEntity, prefabRef, skipWaypoint, ref currentLane, ref cargoTransport,
                    ref publicTransport, ref pathOwner, ref target);
            }

            private void UpdateStop(
                DynamicBuffer<CarNavigationLane> navigationLanes,
                ref CarCurrentLane currentLane,
                ref PublicTransport publicTransport,
                ref Target target)
            {
                Connected componentData1;
                Transform componentData2;


                if (!m_ConnectedData.TryGetComponent(target.m_Target, out componentData1) ||
                    !m_TransformData.TryGetComponent(componentData1.m_Connected, out componentData2))
                    return;
                var lane = Entity.Null;
                float2 float2 = 0.0f;
                for (var index = navigationLanes.Length - 1; index >= 0; --index)
                {
                    var navigationLane = navigationLanes[index];
                    if (navigationLane.m_CurvePosition.y - (double)navigationLane.m_CurvePosition.x != 0.0)
                    {
                        lane = navigationLane.m_Lane;
                        float2 = navigationLane.m_CurvePosition;
                        break;
                    }
                }

                if (float2.x == (double)float2.y)
                {
                    lane = currentLane.m_Lane;
                    float2 = currentLane.m_CurvePosition.xz;
                }

                Curve componentData3;

                if (float2.x == (double)float2.y || !m_CurveData.TryGetComponent(lane, out componentData3))
                    return;
                var float3 = MathUtils.Position(componentData3.m_Bezier, float2.y);
                var xz1 = float3.xz;
                float3 = MathUtils.Tangent(componentData3.m_Bezier, float2.y);
                var xz2 = float3.xz;
                var y = componentData2.m_Position.xz - xz1;
                if (math.dot(MathUtils.Left(math.select(xz2, -xz2, float2.y < (double)float2.x)), y) >
                    0.0)
                {
                    publicTransport.m_State |= PublicTransportFlags.StopLeft;
                    currentLane.m_LaneFlags |= CarLaneFlags.TurnLeft;
                }
                else
                {
                    publicTransport.m_State |= PublicTransportFlags.StopRight;
                    currentLane.m_LaneFlags |= CarLaneFlags.TurnRight;
                }
            }

            private void FindPathIfNeeded(
                Entity vehicleEntity,
                PrefabRef prefabRef,
                Entity skipWaypoint,
                ref CarCurrentLane currentLane,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref PathOwner pathOwner,
                ref Target target)
            {
                if (!VehicleUtils.RequireNewPath(pathOwner))
                    return;

                var carData = m_PrefabCarData[prefabRef.m_Prefab];
                var parameters = new PathfindParameters
                {
                    m_MaxSpeed = carData.m_MaxSpeed,
                    m_WalkSpeed = 5.555556f,
                    m_Methods = PathMethod.Road,
                    m_IgnoredRules = RuleFlags.ForbidPrivateTraffic | VehicleUtils.GetIgnoredPathfindRules(carData)
                };
                var setupQueueTarget = new SetupQueueTarget();
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Road;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                var origin = setupQueueTarget;
                setupQueueTarget = new SetupQueueTarget();
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Road;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                setupQueueTarget.m_Entity = target.m_Target;
                var destination = setupQueueTarget;
                if ((publicTransport.m_State & (PublicTransportFlags.Returning | PublicTransportFlags.Evacuating)) ==
                    PublicTransportFlags.Evacuating)
                {
                    parameters.m_Weights = new PathfindWeights(1f, 0.2f, 0.0f, 0.1f);
                    parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic |
                                                 RuleFlags.ForbidHeavyTraffic;
                }
                else
                {
                    parameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
                }

                if (skipWaypoint != Entity.Null)
                {
                    origin.m_Entity = skipWaypoint;
                    pathOwner.m_State |= PathFlags.Append;
                }
                else
                {
                    pathOwner.m_State &= ~PathFlags.Append;
                }

                if ((cargoTransport.m_State & (CargoTransportFlags.EnRoute | CargoTransportFlags.RouteSource)) ==
                    (CargoTransportFlags.EnRoute | CargoTransportFlags.RouteSource) ||
                    (publicTransport.m_State & (PublicTransportFlags.EnRoute | PublicTransportFlags.RouteSource)) ==
                    (PublicTransportFlags.EnRoute | PublicTransportFlags.RouteSource))
                {
                    parameters.m_PathfindFlags = PathfindFlags.Stable | PathfindFlags.IgnoreFlow;
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) == 0 &&
                         (publicTransport.m_State & PublicTransportFlags.EnRoute) == 0)
                {
                    cargoTransport.m_State &= ~CargoTransportFlags.RouteSource;
                    publicTransport.m_State &= ~PublicTransportFlags.RouteSource;
                }

                var setupQueueItem = new SetupQueueItem(vehicleEntity, parameters, origin, destination);

                VehicleUtils.SetupPathfind(ref currentLane, ref pathOwner, m_PathfindQueue, setupQueueItem);
            }

            private void CheckNavigationLanes(
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref CarCurrentLane currentLane,
                ref PathOwner pathOwner,
                ref Target target,
                out Entity skipWaypoint)
            {
                skipWaypoint = Entity.Null;
                if (navigationLanes.Length >= 8)
                    return;
                var carNavigationLane = new CarNavigationLane();
                if (navigationLanes.Length != 0)
                {
                    carNavigationLane = navigationLanes[navigationLanes.Length - 1];
                    if ((carNavigationLane.m_Flags & CarLaneFlags.EndOfPath) ==
                        0)
                        return;
                }
                else if ((currentLane.m_LaneFlags & CarLaneFlags.EndOfPath) ==
                         0)
                {
                    return;
                }


                if (m_WaypointData.HasComponent(target.m_Target) &&
                    m_RouteWaypoints.HasBuffer(currentRoute.m_Route) &&
                    (!m_ConnectedData.HasComponent(target.m_Target) ||
                     !m_BoardingVehicleData.HasComponent(m_ConnectedData[target.m_Target].m_Connected)))
                {
                    if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete)) !=
                        0)
                        return;
                    skipWaypoint = target.m_Target;

                    SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                    if (navigationLanes.Length != 0)
                    {
                        if ((carNavigationLane.m_Flags & CarLaneFlags.GroupTarget) !=
                            0)
                        {
                            navigationLanes.RemoveAt(navigationLanes.Length - 1);
                        }
                        else
                        {
                            carNavigationLane.m_Flags &= ~CarLaneFlags.EndOfPath;
                            navigationLanes[navigationLanes.Length - 1] = carNavigationLane;
                        }
                    }
                    else
                    {
                        currentLane.m_LaneFlags &= ~CarLaneFlags.EndOfPath;
                    }

                    cargoTransport.m_State |= CargoTransportFlags.RouteSource;
                    publicTransport.m_State |= PublicTransportFlags.RouteSource;
                }
                else
                {
                    if (m_WaypointData.HasComponent(target.m_Target) &&
                        m_RouteWaypoints.HasBuffer(currentRoute.m_Route))
                    {
                        var connected = m_ConnectedData[target.m_Target];

                        if (GetTransportStationFromStop(connected.m_Connected) == Entity.Null &&
                            (cargoTransport.m_State & (CargoTransportFlags.RequiresMaintenance |
                                                       CargoTransportFlags.AbandonRoute)) == 0 &&
                            (publicTransport.m_State & (PublicTransportFlags.RequiresMaintenance |
                                                        PublicTransportFlags.AbandonRoute)) == 0)
                        {
                            if (m_BoardingVehicleData[connected.m_Connected].m_Testing == vehicleEntity)
                            {
                                m_BoardingData.EndTesting(vehicleEntity, currentRoute.m_Route,
                                    connected.m_Connected, target.m_Target);
                                if ((cargoTransport.m_State & CargoTransportFlags.RequireStop) ==
                                    0 &&
                                    (publicTransport.m_State & PublicTransportFlags.RequireStop) ==
                                    0)
                                {
                                    if ((pathOwner.m_State &
                                         (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete)) != 0)
                                        return;
                                    skipWaypoint = target.m_Target;

                                    SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                                    if (navigationLanes.Length != 0)
                                    {
                                        if ((carNavigationLane.m_Flags & CarLaneFlags.GroupTarget) !=
                                            0)
                                        {
                                            navigationLanes.RemoveAt(navigationLanes.Length - 1);
                                        }
                                        else
                                        {
                                            carNavigationLane.m_Flags &= ~CarLaneFlags.EndOfPath;
                                            navigationLanes[navigationLanes.Length - 1] = carNavigationLane;
                                        }
                                    }
                                    else
                                    {
                                        currentLane.m_LaneFlags &= ~CarLaneFlags.EndOfPath;
                                    }

                                    cargoTransport.m_State |= CargoTransportFlags.RouteSource;
                                    publicTransport.m_State |= PublicTransportFlags.RouteSource;
                                    return;
                                }
                            }
                            else
                            {
                                if (navigationLanes.Length != 0 &&
                                    (carNavigationLane.m_Flags & CarLaneFlags.Reserved) ==
                                    0)
                                {
                                    if (navigationLanes.Length < 2)
                                        return;
                                    var navigationLane = navigationLanes[navigationLanes.Length - 2];
                                    Owner componentData1;
                                    Owner componentData2;


                                    if ((navigationLane.m_Flags & CarLaneFlags.Reserved) ==
                                        0 ||
                                        !m_OwnerData.TryGetComponent(carNavigationLane.m_Lane,
                                            out componentData1) ||
                                        !m_OwnerData.TryGetComponent(navigationLane.m_Lane, out componentData2) ||
                                        componentData1.m_Owner != componentData2.m_Owner)
                                        return;
                                }


                                m_BoardingData.BeginTesting(vehicleEntity, currentRoute.m_Route,
                                    connected.m_Connected, target.m_Target);
                                return;
                            }
                        }
                    }

                    cargoTransport.m_State |= CargoTransportFlags.Arriving;
                    publicTransport.m_State |= PublicTransportFlags.Arriving;

                    if (!m_RouteLaneData.HasComponent(target.m_Target))
                        return;

                    var routeLane = m_RouteLaneData[target.m_Target];
                    if (routeLane.m_StartLane != routeLane.m_EndLane)
                    {
                        var elem = new CarNavigationLane();
                        if (navigationLanes.Length != 0)
                        {
                            carNavigationLane.m_CurvePosition.y = 1f;
                            elem.m_Lane = carNavigationLane.m_Lane;
                        }
                        else
                        {
                            currentLane.m_CurvePosition.z = 1f;
                            elem.m_Lane = currentLane.m_Lane;
                        }


                        if (NetUtils.FindNextLane(ref elem.m_Lane, ref m_OwnerData, ref m_LaneData,
                                ref m_SubLanes))
                        {
                            if (navigationLanes.Length != 0)
                            {
                                carNavigationLane.m_Flags &= ~CarLaneFlags.EndOfPath;
                                navigationLanes[navigationLanes.Length - 1] = carNavigationLane;
                            }
                            else
                            {
                                currentLane.m_LaneFlags &= ~CarLaneFlags.EndOfPath;
                            }

                            elem.m_Flags |= CarLaneFlags.EndOfPath | CarLaneFlags.FixedLane;
                            elem.m_CurvePosition = new float2(0.0f, routeLane.m_EndCurvePos);
                            navigationLanes.Add(elem);
                        }
                        else
                        {
                            if (navigationLanes.Length == 0)
                                return;
                            navigationLanes[navigationLanes.Length - 1] = carNavigationLane;
                        }
                    }
                    else if (navigationLanes.Length != 0)
                    {
                        carNavigationLane.m_CurvePosition.y = routeLane.m_EndCurvePos;
                        navigationLanes[navigationLanes.Length - 1] = carNavigationLane;
                    }
                    else
                    {
                        currentLane.m_CurvePosition.z = routeLane.m_EndCurvePos;
                    }
                }
            }

            private void ResetPath(
                int jobIndex,
                Entity vehicleEntity,
                PathInformation pathInformation,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Car car,
                ref CarCurrentLane currentLane,
                ref PathOwner pathOwnerData,
                bool isPublicTransport)
            {
                cargoTransport.m_State &= ~CargoTransportFlags.Arriving;
                publicTransport.m_State &= ~PublicTransportFlags.Arriving;

                var pathElement = m_PathElements[vehicleEntity];
                if ((pathOwnerData.m_State & PathFlags.Append) == 0)
                    PathUtils.ResetPath(ref currentLane, pathElement, m_SlaveLaneData, m_OwnerData,
                        m_SubLanes);

                if ((cargoTransport.m_State & (CargoTransportFlags.Returning | CargoTransportFlags.DummyTraffic)) !=
                    0 ||
                    (publicTransport.m_State & (PublicTransportFlags.Returning | PublicTransportFlags.DummyTraffic)) !=
                    0)
                {
                    car.m_Flags &= ~CarFlags.StayOnRoad;
                }
                else if (cargoTransport.m_RequestCount + publicTransport.m_RequestCount > 0 &&
                         serviceDispatches.Length > 0)
                {
                    var request = serviceDispatches[0].m_Request;

                    if (m_EvacuationRequestData.HasComponent(request))
                    {
                        car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad;
                    }
                    else
                    {
                        if (m_PrisonerTransportRequestData.HasComponent(request))
                        {
                            car.m_Flags &= ~(CarFlags.Emergency | CarFlags.StayOnRoad);
                        }
                        else
                        {
                            car.m_Flags &= ~CarFlags.Emergency;
                            car.m_Flags |= CarFlags.StayOnRoad;
                        }
                    }
                }
                else
                {
                    car.m_Flags &= ~CarFlags.Emergency;
                    car.m_Flags |= CarFlags.StayOnRoad;
                }

                if (isPublicTransport)
                    car.m_Flags |= CarFlags.UsePublicTransportLanes | CarFlags.PreferPublicTransportLanes |
                                   CarFlags.Interior;
                cargoTransport.m_PathElementTime = pathInformation.m_Duration / math.max(1, pathElement.Length);
                publicTransport.m_PathElementTime = cargoTransport.m_PathElementTime;
            }

            private void CheckDummyResources(
                int jobIndex,
                Entity vehicleEntity,
                PrefabRef prefabRef,
                DynamicBuffer<LoadingResources> loadingResources)
            {
                if (loadingResources.Length == 0)
                    return;

                if (m_CargoTransportVehicleData.HasComponent(prefabRef.m_Prefab))
                {
                    var transportVehicleData =
                        m_CargoTransportVehicleData[prefabRef.m_Prefab];

                    var dynamicBuffer =
                        m_CommandBuffer.SetBuffer<Resources>(jobIndex, vehicleEntity);
                    for (var index = 0;
                         index < loadingResources.Length &&
                         dynamicBuffer.Length < transportVehicleData.m_MaxResourceCount;
                         ++index)
                    {
                        var loadingResource = loadingResources[index];
                        var num = math.min(loadingResource.m_Amount, transportVehicleData.m_CargoCapacity);
                        loadingResource.m_Amount -= num;
                        transportVehicleData.m_CargoCapacity -= num;
                        if (num > 0)
                            dynamicBuffer.Add(new Resources
                            {
                                m_Resource = loadingResource.m_Resource,
                                m_Amount = num
                            });
                    }
                }

                loadingResources.Clear();
            }

            private void SetNextWaypointTarget(
                CurrentRoute currentRoute,
                ref PathOwner pathOwnerData,
                ref Target targetData)
            {
                var routeWaypoint = m_RouteWaypoints[currentRoute.m_Route];

                var a = m_WaypointData[targetData.m_Target].m_Index + 1;
                var index = math.select(a, 0, a >= routeWaypoint.Length);
                VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, routeWaypoint[index].m_Waypoint);
            }

            private void CheckServiceDispatches(
                Entity vehicleEntity,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                bool allowQueued,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref PathOwner pathOwner)
            {
                if (!allowQueued)
                {
                    if (serviceDispatches.Length > 1)
                        serviceDispatches.RemoveRange(1, serviceDispatches.Length - 1);
                    cargoTransport.m_RequestCount = math.min(1, cargoTransport.m_RequestCount);
                    publicTransport.m_RequestCount = math.min(1, publicTransport.m_RequestCount);
                }

                var index1 = math.max(cargoTransport.m_RequestCount, publicTransport.m_RequestCount);
                if (serviceDispatches.Length <= index1)
                    return;
                var num1 = -1f;
                var request1 = Entity.Null;
                var pathElement1 = new PathElement();
                var flag = false;
                var num2 = 0;
                if (index1 >= 1 && (cargoTransport.m_State & CargoTransportFlags.Returning) == 0 &&
                    (publicTransport.m_State & PublicTransportFlags.Returning) == 0)
                {
                    var pathElement2 = m_PathElements[vehicleEntity];
                    num2 = 1;
                    if (pathOwner.m_ElementIndex < pathElement2.Length)
                    {
                        pathElement1 = pathElement2[pathElement2.Length - 1];
                        flag = true;
                    }
                }

                for (var index2 = num2; index2 < index1; ++index2)
                {
                    DynamicBuffer<PathElement> bufferData;

                    if (m_PathElements.TryGetBuffer(serviceDispatches[index2].m_Request, out bufferData) &&
                        bufferData.Length != 0)
                    {
                        pathElement1 = bufferData[bufferData.Length - 1];
                        flag = true;
                    }
                }

                for (var index3 = index1; index3 < serviceDispatches.Length; ++index3)
                {
                    var request2 = serviceDispatches[index3].m_Request;

                    if (m_TransportVehicleRequestData.HasComponent(request2))
                    {
                        var transportVehicleRequest = m_TransportVehicleRequestData[request2];

                        if (m_PrefabRefData.HasComponent(transportVehicleRequest.m_Route) &&
                            transportVehicleRequest.m_Priority > (double)num1)
                        {
                            num1 = transportVehicleRequest.m_Priority;
                            request1 = request2;
                        }
                    }
                    else
                    {
                        if (m_EvacuationRequestData.HasComponent(request2))
                        {
                            var evacuationRequest = m_EvacuationRequestData[request2];
                            DynamicBuffer<PathElement> bufferData;

                            if (flag && m_PathElements.TryGetBuffer(request2, out bufferData) &&
                                bufferData.Length != 0)
                            {
                                var pathElement3 = bufferData[0];
                                if (pathElement3.m_Target != pathElement1.m_Target ||
                                    pathElement3.m_TargetDelta.x != (double)pathElement1.m_TargetDelta.y)
                                    continue;
                            }


                            if (m_PrefabRefData.HasComponent(evacuationRequest.m_Target) &&
                                evacuationRequest.m_Priority > (double)num1)
                            {
                                num1 = evacuationRequest.m_Priority;
                                request1 = request2;
                            }
                        }
                        else
                        {
                            if (m_PrisonerTransportRequestData.HasComponent(request2))
                            {
                                var transportRequest =
                                    m_PrisonerTransportRequestData[request2];
                                DynamicBuffer<PathElement> bufferData;

                                if (flag && m_PathElements.TryGetBuffer(request2, out bufferData) &&
                                    bufferData.Length != 0)
                                {
                                    var pathElement4 = bufferData[0];
                                    if (pathElement4.m_Target != pathElement1.m_Target ||
                                        pathElement4.m_TargetDelta.x != (double)pathElement1.m_TargetDelta.y)
                                        continue;
                                }


                                if (m_PrefabRefData.HasComponent(transportRequest.m_Target) &&
                                    transportRequest.m_Priority > (double)num1)
                                {
                                    num1 = transportRequest.m_Priority;
                                    request1 = request2;
                                }
                            }
                        }
                    }
                }

                if (request1 != Entity.Null)
                {
                    serviceDispatches[index1++] = new ServiceDispatch(request1);
                    ++publicTransport.m_RequestCount;
                    ++cargoTransport.m_RequestCount;
                }

                if (serviceDispatches.Length <= index1)
                    return;
                serviceDispatches.RemoveRange(index1, serviceDispatches.Length - index1);
            }

            private void RequestTargetIfNeeded(
                int jobIndex,
                Entity entity,
                ref PublicTransport publicTransport,
                ref CargoTransport cargoTransport)
            {
                if (m_ServiceRequestData.HasComponent(publicTransport.m_TargetRequest) ||
                    m_ServiceRequestData.HasComponent(cargoTransport.m_TargetRequest))
                    return;
                if ((publicTransport.m_State & PublicTransportFlags.Evacuating) != 0)
                {
                    if (((int)m_SimulationFrameIndex & ((int)math.max(64U, 16U) - 1)) != 1)
                        return;


                    var entity1 = m_CommandBuffer.CreateEntity(jobIndex, m_EvacuationRequestArchetype);

                    m_CommandBuffer.SetComponent(jobIndex, entity1, new ServiceRequest(true));

                    m_CommandBuffer.SetComponent(jobIndex, entity1,
                        new EvacuationRequest(entity, 1f));

                    m_CommandBuffer.SetComponent(jobIndex, entity1, new RequestGroup(4U));
                }
                else if ((publicTransport.m_State & PublicTransportFlags.PrisonerTransport) != 0)
                {
                    if (((int)m_SimulationFrameIndex & ((int)math.max(256U, 16U) - 1)) != 1)
                        return;


                    var entity2 =
                        m_CommandBuffer.CreateEntity(jobIndex, m_PrisonerTransportRequestArchetype);

                    m_CommandBuffer.SetComponent(jobIndex, entity2, new ServiceRequest(true));

                    m_CommandBuffer.SetComponent(jobIndex, entity2,
                        new PrisonerTransportRequest(entity, 1));

                    m_CommandBuffer.SetComponent(jobIndex, entity2, new RequestGroup(16U));
                }
                else
                {
                    if (((int)m_SimulationFrameIndex & ((int)math.max(256U, 16U) - 1)) != 1)
                        return;


                    var entity3 =
                        m_CommandBuffer.CreateEntity(jobIndex, m_TransportVehicleRequestArchetype);

                    m_CommandBuffer.SetComponent(jobIndex, entity3, new ServiceRequest(true));

                    m_CommandBuffer.SetComponent(jobIndex, entity3,
                        new TransportVehicleRequest(entity, 1f));

                    m_CommandBuffer.SetComponent(jobIndex, entity3, new RequestGroup(8U));
                }
            }

            private bool SelectNextDispatch(
                int jobIndex,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                DynamicBuffer<CarNavigationLane> navigationLanes,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Car car,
                ref CarCurrentLane currentLane,
                ref PathOwner pathOwner,
                ref Target target,
                bool isPublicTransport)
            {
                if ((cargoTransport.m_State & CargoTransportFlags.Returning) == 0 &&
                    (publicTransport.m_State & PublicTransportFlags.Returning) == 0 &&
                    cargoTransport.m_RequestCount + publicTransport.m_RequestCount > 0 && serviceDispatches.Length > 0)
                {
                    serviceDispatches.RemoveAt(0);
                    cargoTransport.m_RequestCount = math.max(0, cargoTransport.m_RequestCount - 1);
                    publicTransport.m_RequestCount = math.max(0, publicTransport.m_RequestCount - 1);
                }

                if ((cargoTransport.m_State &
                     (CargoTransportFlags.RequiresMaintenance | CargoTransportFlags.Disabled)) !=
                    0 ||
                    (publicTransport.m_State &
                     (PublicTransportFlags.RequiresMaintenance | PublicTransportFlags.Disabled)) !=
                    0)
                {
                    cargoTransport.m_RequestCount = 0;
                    publicTransport.m_RequestCount = 0;
                    serviceDispatches.Clear();
                    return false;
                }

                for (;
                     cargoTransport.m_RequestCount + publicTransport.m_RequestCount > 0 && serviceDispatches.Length > 0;
                     publicTransport.m_RequestCount = math.max(0, publicTransport.m_RequestCount - 1))
                {
                    var request = serviceDispatches[0].m_Request;
                    var route = Entity.Null;
                    var entity1 = Entity.Null;
                    var carFlags = car.m_Flags;
                    if (isPublicTransport)
                        carFlags |= CarFlags.UsePublicTransportLanes | CarFlags.PreferPublicTransportLanes;

                    if (m_TransportVehicleRequestData.HasComponent(request))
                    {
                        route = m_TransportVehicleRequestData[request].m_Route;

                        if (m_PathInformationData.HasComponent(request))
                            entity1 = m_PathInformationData[request].m_Destination;

                        carFlags = (carFlags & ~CarFlags.Emergency) | CarFlags.StayOnRoad;
                    }
                    else
                    {
                        if (m_EvacuationRequestData.HasComponent(request))
                        {
                            entity1 = m_EvacuationRequestData[request].m_Target;
                            carFlags |= CarFlags.Emergency | CarFlags.StayOnRoad;
                        }
                        else
                        {
                            if (m_PrisonerTransportRequestData.HasComponent(request))
                            {
                                entity1 = m_PrisonerTransportRequestData[request].m_Target;
                                carFlags &= ~(CarFlags.Emergency | CarFlags.StayOnRoad);
                            }
                        }
                    }


                    if (!m_PrefabRefData.HasComponent(entity1))
                    {
                        serviceDispatches.RemoveAt(0);
                        cargoTransport.m_RequestCount = math.max(0, cargoTransport.m_RequestCount - 1);
                    }
                    else
                    {
                        if (m_TransportVehicleRequestData.HasComponent(request))
                        {
                            serviceDispatches.Clear();
                            cargoTransport.m_RequestCount = 0;
                            publicTransport.m_RequestCount = 0;

                            if (m_PrefabRefData.HasComponent(route))
                            {
                                if (currentRoute.m_Route != route)
                                {
                                    m_CommandBuffer.AddComponent(jobIndex, vehicleEntity,
                                        new CurrentRoute(route));

                                    m_CommandBuffer.AppendToBuffer(jobIndex, route,
                                        new RouteVehicle(vehicleEntity));
                                    Color componentData;

                                    if (m_RouteColorData.TryGetComponent(route, out componentData))
                                    {
                                        m_CommandBuffer.AddComponent(jobIndex, vehicleEntity,
                                            componentData);

                                        m_CommandBuffer.AddComponent<BatchesUpdated>(jobIndex, vehicleEntity);
                                    }
                                }

                                cargoTransport.m_State |= CargoTransportFlags.EnRoute;
                                publicTransport.m_State |= PublicTransportFlags.EnRoute;
                            }
                            else
                            {
                                m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);
                            }


                            var entity2 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);

                            m_CommandBuffer.SetComponent(jobIndex, entity2,
                                new HandleRequest(request, vehicleEntity, true));
                        }
                        else
                        {
                            m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);


                            var entity3 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);

                            m_CommandBuffer.SetComponent(jobIndex, entity3,
                                new HandleRequest(request, vehicleEntity, false, true));
                        }

                        cargoTransport.m_State &= ~CargoTransportFlags.Returning;
                        publicTransport.m_State &= ~PublicTransportFlags.Returning;
                        car.m_Flags = carFlags;

                        if (m_ServiceRequestData.HasComponent(publicTransport.m_TargetRequest))
                        {
                            var entity4 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);

                            m_CommandBuffer.SetComponent(jobIndex, entity4,
                                new HandleRequest(publicTransport.m_TargetRequest, Entity.Null, true));
                        }


                        if (m_ServiceRequestData.HasComponent(cargoTransport.m_TargetRequest))
                        {
                            var entity5 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);

                            m_CommandBuffer.SetComponent(jobIndex, entity5,
                                new HandleRequest(cargoTransport.m_TargetRequest, Entity.Null, true));
                        }


                        if (m_PathElements.HasBuffer(request))
                        {
                            var pathElement1 = m_PathElements[request];
                            if (pathElement1.Length != 0)
                            {
                                var pathElement2 = m_PathElements[vehicleEntity];
                                PathUtils.TrimPath(pathElement2, ref pathOwner);

                                var num =
                                    math.max(cargoTransport.m_PathElementTime, publicTransport.m_PathElementTime) *
                                    pathElement2.Length + m_PathInformationData[request].m_Duration;


                                if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, pathElement2,
                                        pathElement1, m_SlaveLaneData, m_OwnerData, m_SubLanes))
                                {
                                    cargoTransport.m_PathElementTime = num / math.max(1, pathElement2.Length);
                                    publicTransport.m_PathElementTime = cargoTransport.m_PathElementTime;
                                    target.m_Target = entity1;
                                    VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
                                    cargoTransport.m_State &= ~CargoTransportFlags.Arriving;
                                    publicTransport.m_State &= ~PublicTransportFlags.Arriving;
                                    return true;
                                }
                            }
                        }

                        VehicleUtils.SetTarget(ref pathOwner, ref target, entity1);
                        return true;
                    }
                }

                return false;
            }

            private void ReturnToDepot(
                int jobIndex,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                Owner ownerData,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Car car,
                ref PathOwner pathOwner,
                ref Target target)
            {
                serviceDispatches.Clear();
                cargoTransport.m_RequestCount = 0;
                cargoTransport.m_State &= ~(CargoTransportFlags.EnRoute | CargoTransportFlags.Refueling |
                                            CargoTransportFlags.AbandonRoute);
                cargoTransport.m_State |= CargoTransportFlags.Returning;
                publicTransport.m_RequestCount = 0;
                publicTransport.m_State &= ~(PublicTransportFlags.EnRoute | PublicTransportFlags.Refueling |
                                             PublicTransportFlags.AbandonRoute);
                publicTransport.m_State |= PublicTransportFlags.Returning;

                m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);
                car.m_Flags &= ~CarFlags.Emergency;
                VehicleUtils.SetTarget(ref pathOwner, ref target, ownerData.m_Owner);
            }

            private bool StartBoarding(
                int jobIndex,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                PrefabRef prefabRef,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Target target,
                bool isCargoVehicle)
            {
                if ((publicTransport.m_State &
                     (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) !=
                    0)
                {
                    publicTransport.m_State |= PublicTransportFlags.Boarding;

                    publicTransport.m_DepartureFrame = m_SimulationFrameIndex + 4096U;
                    return true;
                }


                if (m_ConnectedData.HasComponent(target.m_Target))
                {
                    var connected = m_ConnectedData[target.m_Target];

                    if (m_BoardingVehicleData.HasComponent(connected.m_Connected))
                    {
                        var transportStationFromStop = GetTransportStationFromStop(connected.m_Connected);
                        var nextStorageCompany = Entity.Null;
                        var refuel = false;

                        if (m_TransportStationData.HasComponent(transportStationFromStop))
                        {
                            var carData = m_PrefabCarData[prefabRef.m_Prefab];

                            refuel = (m_TransportStationData[transportStationFromStop].m_CarRefuelTypes &
                                      carData.m_EnergyType) != 0;
                        }

                        if ((!refuel &&
                             ((cargoTransport.m_State & CargoTransportFlags.RequiresMaintenance) !=
                              0 ||
                              (publicTransport.m_State & PublicTransportFlags.RequiresMaintenance) !=
                              0)) ||
                            (cargoTransport.m_State & CargoTransportFlags.AbandonRoute) != 0 ||
                            (publicTransport.m_State & PublicTransportFlags.AbandonRoute) != 0)
                        {
                            cargoTransport.m_State &= ~(CargoTransportFlags.EnRoute | CargoTransportFlags.AbandonRoute);
                            publicTransport.m_State &=
                                ~(PublicTransportFlags.EnRoute | PublicTransportFlags.AbandonRoute);
                            if (currentRoute.m_Route != Entity.Null)
                                m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);
                        }
                        else
                        {
                            cargoTransport.m_State &= ~CargoTransportFlags.RequiresMaintenance;
                            publicTransport.m_State &= ~PublicTransportFlags.RequiresMaintenance;
                            cargoTransport.m_State |= CargoTransportFlags.EnRoute;
                            publicTransport.m_State |= PublicTransportFlags.EnRoute;
                            if (isCargoVehicle)
                                nextStorageCompany = GetNextStorageCompany(currentRoute.m_Route, target.m_Target);
                        }

                        cargoTransport.m_State |= CargoTransportFlags.RouteSource;
                        publicTransport.m_State |= PublicTransportFlags.RouteSource;
                        var storageCompanyFromStop = Entity.Null;
                        if (isCargoVehicle) storageCompanyFromStop = GetStorageCompanyFromStop(connected.m_Connected);


                        m_BoardingData.BeginBoarding(vehicleEntity, currentRoute.m_Route, connected.m_Connected,
                            target.m_Target, storageCompanyFromStop, nextStorageCompany, refuel);
                        return true;
                    }
                }


                if (m_WaypointData.HasComponent(target.m_Target))
                {
                    cargoTransport.m_State |= CargoTransportFlags.RouteSource;
                    publicTransport.m_State |= PublicTransportFlags.RouteSource;
                    return false;
                }

                cargoTransport.m_State &= ~(CargoTransportFlags.EnRoute | CargoTransportFlags.AbandonRoute);
                publicTransport.m_State &= ~(PublicTransportFlags.EnRoute | PublicTransportFlags.AbandonRoute);
                if (currentRoute.m_Route != Entity.Null)
                    m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);

                return false;
            }

            private bool StopBoarding(
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                DynamicBuffer<Passenger> passengers,
                ref CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Target target,
                ref Odometer odometer,
                bool forcedStop,
                bool isCargoVehicle,
                int jobIndex)
            {
                var flag = false;
                Connected componentData1;
                BoardingVehicle componentData2;


                if (m_ConnectedData.TryGetComponent(target.m_Target, out componentData1) &&
                    m_BoardingVehicleData.TryGetComponent(componentData1.m_Connected, out componentData2))
                    flag = componentData2.m_Vehicle == vehicleEntity;
                if (!forcedStop)
                {
                    if ((flag || (publicTransport.m_State &
                                  (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) !=
                            0) &&
                        (m_SimulationFrameIndex < cargoTransport.m_DepartureFrame ||
                         m_SimulationFrameIndex < publicTransport.m_DepartureFrame))
                        return false;
                    if (passengers.IsCreated)
                        return PublicTransportBoardingHelper.ArePassengersReady(passengers, m_CurrentVehicleData,
                            m_CommandBuffer, m_SearchTree, publicTransport, jobIndex, m_SimulationFrameIndex);
                }

                if ((cargoTransport.m_State & CargoTransportFlags.Refueling) != 0 ||
                    (publicTransport.m_State & PublicTransportFlags.Refueling) != 0)
                    odometer.m_Distance = 0.0f;
                if ((publicTransport.m_State &
                     (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) ==
                    0 && flag)
                {
                    var storageCompanyFromStop = Entity.Null;
                    var nextStorageCompany = Entity.Null;
                    if (!forcedStop && (cargoTransport.m_State & CargoTransportFlags.Boarding) !=
                        0)
                    {
                        storageCompanyFromStop = GetStorageCompanyFromStop(componentData1.m_Connected);
                        if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) != 0)
                            nextStorageCompany = GetNextStorageCompany(currentRoute.m_Route, target.m_Target);
                    }


                    m_BoardingData.EndBoarding(vehicleEntity, currentRoute.m_Route, componentData1.m_Connected,
                        target.m_Target, storageCompanyFromStop, nextStorageCompany);
                    return true;
                }

                cargoTransport.m_State &= ~(CargoTransportFlags.Boarding | CargoTransportFlags.Refueling);
                publicTransport.m_State &= ~(PublicTransportFlags.Boarding | PublicTransportFlags.Refueling);
                return true;
            }

            private Entity GetTransportStationFromStop(Entity stop)
            {
                for (; !m_TransportStationData.HasComponent(stop); stop = m_OwnerData[stop].m_Owner)
                    if (!m_OwnerData.HasComponent(stop))
                        return Entity.Null;


                if (m_OwnerData.HasComponent(stop))
                {
                    var owner = m_OwnerData[stop].m_Owner;

                    if (m_TransportStationData.HasComponent(owner))
                        return owner;
                }

                return stop;
            }

            private Entity GetStorageCompanyFromStop(Entity stop)
            {
                for (; !m_StorageCompanyData.HasComponent(stop); stop = m_OwnerData[stop].m_Owner)
                    if (!m_OwnerData.HasComponent(stop))
                        return Entity.Null;

                return stop;
            }

            private Entity GetNextStorageCompany(Entity route, Entity currentWaypoint)
            {
                var routeWaypoint = m_RouteWaypoints[route];

                var a = m_WaypointData[currentWaypoint].m_Index + 1;
                for (var index1 = 0; index1 < routeWaypoint.Length; ++index1)
                {
                    var index2 = math.select(a, 0, a >= routeWaypoint.Length);
                    var waypoint = routeWaypoint[index2].m_Waypoint;

                    if (m_ConnectedData.HasComponent(waypoint))
                    {
                        var storageCompanyFromStop =
                            GetStorageCompanyFromStop(m_ConnectedData[waypoint].m_Connected);
                        if (storageCompanyFromStop != Entity.Null)
                            return storageCompanyFromStop;
                    }

                    a = index2 + 1;
                }

                return Entity.Null;
            }

            void IJobChunk.Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly] public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly] public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

            [ReadOnly] public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentTypeHandle;
            [ReadOnly] public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

            public ComponentTypeHandle<CargoTransport>
                __Game_Vehicles_CargoTransport_RW_ComponentTypeHandle;

            public ComponentTypeHandle<PublicTransport>
                __Game_Vehicles_PublicTransport_RW_ComponentTypeHandle;

            public ComponentTypeHandle<Car> __Game_Vehicles_Car_RW_ComponentTypeHandle;
            public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;
            public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;
            public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;
            public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RW_ComponentTypeHandle;
            public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle;
            public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;
            [ReadOnly] public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<PublicTransportVehicleData>
                __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<CargoTransportVehicleData>
                __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<TransportVehicleRequest>
                __Game_Simulation_TransportVehicleRequest_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<EvacuationRequest> __Game_Simulation_EvacuationRequest_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<PrisonerTransportRequest>
                __Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<BoardingVehicle> __Game_Routes_BoardingVehicle_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Color> __Game_Routes_Color_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<TransportStation>
                __Game_Buildings_TransportStation_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;
            [ReadOnly] public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;
            [ReadOnly] public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;
            public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;
            public BufferLookup<LoadingResources> __Game_Vehicles_LoadingResources_RW_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();

                __Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(true);

                __Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(true);

                __Game_Pathfind_PathInformation_RO_ComponentTypeHandle =
                    state.GetComponentTypeHandle<PathInformation>(true);

                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);

                __Game_Routes_CurrentRoute_RO_ComponentTypeHandle =
                    state.GetComponentTypeHandle<CurrentRoute>(true);

                __Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(true);

                __Game_Vehicles_CargoTransport_RW_ComponentTypeHandle =
                    state.GetComponentTypeHandle<CargoTransport>();

                __Game_Vehicles_PublicTransport_RW_ComponentTypeHandle =
                    state.GetComponentTypeHandle<PublicTransport>();

                __Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();

                __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle =
                    state.GetComponentTypeHandle<CarCurrentLane>();

                __Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();

                __Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();

                __Game_Vehicles_Odometer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>();

                __Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle =
                    state.GetBufferTypeHandle<CarNavigationLane>();

                __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle =
                    state.GetBufferTypeHandle<ServiceDispatch>();

                __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);

                __Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(true);

                __Game_Pathfind_PathInformation_RO_ComponentLookup =
                    state.GetComponentLookup<PathInformation>(true);

                __Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(true);

                __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);

                __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup =
                    state.GetComponentLookup<PublicTransportVehicleData>(true);

                __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup =
                    state.GetComponentLookup<CargoTransportVehicleData>(true);

                __Game_Simulation_ServiceRequest_RO_ComponentLookup =
                    state.GetComponentLookup<ServiceRequest>(true);

                __Game_Simulation_TransportVehicleRequest_RO_ComponentLookup =
                    state.GetComponentLookup<TransportVehicleRequest>(true);

                __Game_Simulation_EvacuationRequest_RO_ComponentLookup =
                    state.GetComponentLookup<EvacuationRequest>(true);

                __Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup =
                    state.GetComponentLookup<PrisonerTransportRequest>(true);

                __Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(true);

                __Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(true);

                __Game_Routes_BoardingVehicle_RO_ComponentLookup = state.GetComponentLookup<BoardingVehicle>(true);

                __Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(true);

                __Game_Routes_Color_RO_ComponentLookup = state.GetComponentLookup<Color>(true);

                __Game_Companies_StorageCompany_RO_ComponentLookup =
                    state.GetComponentLookup<StorageCompany>(true);

                __Game_Buildings_TransportStation_RO_ComponentLookup =
                    state.GetComponentLookup<TransportStation>(true);

                __Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(true);

                __Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(true);

                __Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(true);

                __Game_Creatures_CurrentVehicle_RO_ComponentLookup =
                    state.GetComponentLookup<CurrentVehicle>(true);

                __Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(true);

                __Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(true);

                __Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();

                __Game_Vehicles_LoadingResources_RW_BufferLookup = state.GetBufferLookup<LoadingResources>();
            }
        }
    }
}