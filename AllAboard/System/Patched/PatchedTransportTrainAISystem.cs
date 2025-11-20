// Decompiled with JetBrains decompiler
// Type: Game.Simulation.TransportTrainAISystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 56B6C274-2D52-4DAC-BD5B-A4AB43BF9875

using Game.Achievements;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using System.Runtime.CompilerServices;
using AllAboard.System.Utility;
using Game;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using PublicTransport = Game.Vehicles.PublicTransport;

#nullable disable
namespace AllAboard.System.Patched
{
    public partial class PatchedTransportTrainAISystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private SimulationSystem m_SimulationSystem;
        private PathfindSetupSystem m_PathfindSetupSystem;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private TransportUsageTrackSystem m_TransportUsageTrackSystem;
        private AchievementTriggerSystem m_AchievementTriggerSystem;
        private CityConfigurationSystem m_CityConfigurationSystem;
        private EntityQuery m_VehicleQuery;
        private EntityQuery m_CarriagePrefabQuery;
        private EntityArchetype m_TransportVehicleRequestArchetype;
        private EntityArchetype m_HandleRequestArchetype;
        private ComponentTypeSet m_MovingToParkedTrainRemoveTypes;
        private ComponentTypeSet m_MovingToParkedTrainAddTypes;
        private TransportTrainCarriageSelectData m_TransportTrainCarriageSelectData;
        private TransportBoardingHelpers.BoardingLookupData m_BoardingLookupData;
        private TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 16;
            /*0x10*/
        }

        public override int GetUpdateOffset(SystemUpdatePhase phase)
        {
            return 3;
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_PathfindSetupSystem = World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            m_TransportUsageTrackSystem = World.GetOrCreateSystemManaged<TransportUsageTrackSystem>();
            m_AchievementTriggerSystem = World.GetOrCreateSystemManaged<AchievementTriggerSystem>();
            m_CityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            m_TransportTrainCarriageSelectData = new TransportTrainCarriageSelectData((SystemBase)this);
            m_BoardingLookupData = new TransportBoardingHelpers.BoardingLookupData((SystemBase)this);
            m_VehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[5]
                {
                    ComponentType.ReadWrite<TrainCurrentLane>(),
                    ComponentType.ReadOnly<Owner>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadWrite<PathOwner>(),
                    ComponentType.ReadWrite<Target>()
                },
                Any = new ComponentType[2]
                {
                    ComponentType.ReadWrite<Game.Vehicles.CargoTransport>(),
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
            m_HandleRequestArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(),
                ComponentType.ReadWrite<Event>());
            m_MovingToParkedTrainRemoveTypes = new ComponentTypeSet(new ComponentType[13]
            {
                ComponentType.ReadWrite<Moving>(),
                ComponentType.ReadWrite<TransformFrame>(),
                ComponentType.ReadWrite<InterpolatedTransform>(),
                ComponentType.ReadWrite<TrainNavigation>(),
                ComponentType.ReadWrite<TrainNavigationLane>(),
                ComponentType.ReadWrite<TrainCurrentLane>(),
                ComponentType.ReadWrite<TrainBogieFrame>(),
                ComponentType.ReadWrite<PathOwner>(),
                ComponentType.ReadWrite<Target>(),
                ComponentType.ReadWrite<Blocker>(),
                ComponentType.ReadWrite<PathElement>(),
                ComponentType.ReadWrite<PathInformation>(),
                ComponentType.ReadWrite<ServiceDispatch>()
            });
            m_MovingToParkedTrainAddTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedTrain>(),
                ComponentType.ReadWrite<Stopped>(), ComponentType.ReadWrite<Updated>());
            m_CarriagePrefabQuery = GetEntityQuery(TransportTrainCarriageSelectData.GetEntityQueryDesc());
            RequireForUpdate(m_VehicleQuery);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            var boardingData =
                new TransportBoardingHelpers.BoardingData(Allocator.TempJob);
            JobHandle jobHandle1;
            m_TransportTrainCarriageSelectData.PreUpdate((SystemBase)this, m_CityConfigurationSystem,
                m_CarriagePrefabQuery, Allocator.TempJob, out jobHandle1);
            m_BoardingLookupData.Update((SystemBase)this);
            var jobHandle2 = new TransportTrainTickJob
            {
                m_EntityType =
                    InternalCompilerInterface.GetEntityTypeHandle(
                        ref __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        ref CheckedStateRef),
                m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(
                    ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle<Unspawned>(
                    ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(
                    ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_CurrentRouteType = InternalCompilerInterface.GetComponentTypeHandle<CurrentRoute>(
                    ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentTypeHandle, ref CheckedStateRef),
                m_CargoTransportType = InternalCompilerInterface.GetComponentTypeHandle<Game.Vehicles.CargoTransport>(
                    ref __TypeHandle.__Game_Vehicles_CargoTransport_RW_ComponentTypeHandle,
                    ref CheckedStateRef),
                m_PublicTransportType = InternalCompilerInterface.GetComponentTypeHandle<PublicTransport>(
                    ref __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentTypeHandle,
                    ref CheckedStateRef),
                m_TargetType = InternalCompilerInterface.GetComponentTypeHandle<Target>(
                    ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref CheckedStateRef),
                m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle<PathOwner>(
                    ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref CheckedStateRef),
                m_OdometerType = InternalCompilerInterface.GetComponentTypeHandle<Odometer>(
                    ref __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle, ref CheckedStateRef),
                m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle<LayoutElement>(
                    ref __TypeHandle.__Game_Vehicles_LayoutElement_RW_BufferTypeHandle, ref CheckedStateRef),
                m_NavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle<TrainNavigationLane>(
                    ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle,
                    ref CheckedStateRef),
                m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle<ServiceDispatch>(
                    ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle,
                    ref CheckedStateRef),
                m_EntityLookup =
                    InternalCompilerInterface.GetEntityStorageInfoLookup(
                        ref __TypeHandle.__EntityStorageInfoLookup,
                        ref CheckedStateRef),
                m_TransformData = InternalCompilerInterface.GetComponentLookup<Transform>(
                    ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref CheckedStateRef),
                m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup<Game.Objects.SpawnLocation>(
                    ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref CheckedStateRef),
                m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(
                    ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref CheckedStateRef),
                m_PathInformationData = InternalCompilerInterface.GetComponentLookup<PathInformation>(
                    ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref CheckedStateRef),
                m_TransportVehicleRequestData = InternalCompilerInterface.GetComponentLookup<TransportVehicleRequest>(
                    ref __TypeHandle.__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup,
                    ref CheckedStateRef),
                m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup<ParkedTrain>(
                    ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref CheckedStateRef),
                m_ControllerData = InternalCompilerInterface.GetComponentLookup<Controller>(
                    ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref CheckedStateRef),
                m_CurveData =
                    InternalCompilerInterface.GetComponentLookup<Curve>(
                        ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref CheckedStateRef),
                m_LaneData =
                    InternalCompilerInterface.GetComponentLookup<Lane>(
                        ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref CheckedStateRef),
                m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup<EdgeLane>(
                    ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref CheckedStateRef),
                m_EdgeData = InternalCompilerInterface.GetComponentLookup<Game.Net.Edge>(
                    ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref CheckedStateRef),
                m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup<TrainData>(
                    ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref CheckedStateRef),
                m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(
                    ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref CheckedStateRef),
                m_PublicTransportVehicleData = InternalCompilerInterface.GetComponentLookup<PublicTransportVehicleData>(
                    ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup,
                    ref CheckedStateRef),
                m_CargoTransportVehicleData = InternalCompilerInterface.GetComponentLookup<CargoTransportVehicleData>(
                    ref __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup,
                    ref CheckedStateRef),
                m_WaypointData = InternalCompilerInterface.GetComponentLookup<Waypoint>(
                    ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref CheckedStateRef),
                m_ConnectedData = InternalCompilerInterface.GetComponentLookup<Connected>(
                    ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref CheckedStateRef),
                m_BoardingVehicleData = InternalCompilerInterface.GetComponentLookup<BoardingVehicle>(
                    ref __TypeHandle.__Game_Routes_BoardingVehicle_RO_ComponentLookup, ref CheckedStateRef),
                m_RouteColorData = InternalCompilerInterface.GetComponentLookup<Game.Routes.Color>(
                    ref __TypeHandle.__Game_Routes_Color_RO_ComponentLookup, ref CheckedStateRef),
                m_StorageCompanyData = InternalCompilerInterface.GetComponentLookup<Game.Companies.StorageCompany>(
                    ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref CheckedStateRef),
                m_TransportStationData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.TransportStation>(
                    ref __TypeHandle.__Game_Buildings_TransportStation_RO_ComponentLookup,
                    ref CheckedStateRef),
                m_TransportDepotData = InternalCompilerInterface.GetComponentLookup<Game.Buildings.TransportDepot>(
                    ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentLookup, ref CheckedStateRef),
                m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup<CurrentVehicle>(
                    ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref CheckedStateRef),
                m_Passengers = InternalCompilerInterface.GetBufferLookup<Passenger>(
                    ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref CheckedStateRef),
                m_EconomyResources = InternalCompilerInterface.GetBufferLookup<Resources>(
                    ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref CheckedStateRef),
                m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup<RouteWaypoint>(
                    ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref CheckedStateRef),
                m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup<ConnectedEdge>(
                    ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref CheckedStateRef),
                m_SubLanes = InternalCompilerInterface.GetBufferLookup<Game.Net.SubLane>(
                    ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref CheckedStateRef),
                m_TrainData = InternalCompilerInterface.GetComponentLookup<Train>(
                    ref __TypeHandle.__Game_Vehicles_Train_RW_ComponentLookup, ref CheckedStateRef),
                m_CurrentLaneData = InternalCompilerInterface.GetComponentLookup<TrainCurrentLane>(
                    ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup,
                    ref CheckedStateRef),
                m_NavigationData = InternalCompilerInterface.GetComponentLookup<TrainNavigation>(
                    ref __TypeHandle.__Game_Vehicles_TrainNavigation_RW_ComponentLookup, ref CheckedStateRef),
                m_BlockerData = InternalCompilerInterface.GetComponentLookup<Blocker>(
                    ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentLookup, ref CheckedStateRef),
                m_PathElements = InternalCompilerInterface.GetBufferLookup<PathElement>(
                    ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref CheckedStateRef),
                m_LoadingResources = InternalCompilerInterface.GetBufferLookup<LoadingResources>(
                    ref __TypeHandle.__Game_Vehicles_LoadingResources_RW_BufferLookup, ref CheckedStateRef),
                m_LayoutElements = InternalCompilerInterface.GetBufferLookup<LayoutElement>(
                    ref __TypeHandle.__Game_Vehicles_LayoutElement_RW_BufferLookup, ref CheckedStateRef),
                m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
                m_RandomSeed = RandomSeed.Next(),
                m_TransportVehicleRequestArchetype = m_TransportVehicleRequestArchetype,
                m_HandleRequestArchetype = m_HandleRequestArchetype,
                m_MovingToParkedTrainRemoveTypes = m_MovingToParkedTrainRemoveTypes,
                m_MovingToParkedTrainAddTypes = m_MovingToParkedTrainAddTypes,
                m_TransportTrainCarriageSelectData = m_TransportTrainCarriageSelectData,
                m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_PathfindQueue = m_PathfindSetupSystem.GetQueue((object)this, 64 /*0x40*/).AsParallelWriter(),
                m_BoardingData = boardingData.ToConcurrent()
            }.ScheduleParallel<TransportTrainTickJob>(m_VehicleQuery,
                JobHandle.CombineDependencies(Dependency, jobHandle1));
            var inputDeps = boardingData.ScheduleBoarding((SystemBase)this, m_CityStatisticsSystem,
                m_TransportUsageTrackSystem, m_AchievementTriggerSystem, m_BoardingLookupData,
                m_SimulationSystem.frameIndex, jobHandle2);
            m_TransportTrainCarriageSelectData.PostUpdate(jobHandle2);
            boardingData.Dispose(inputDeps);
            m_PathfindSetupSystem.AddQueueWriter(jobHandle2);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            Dependency = inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref CheckedStateRef);
            __TypeHandle.__AssignHandles(ref CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public PatchedTransportTrainAISystem()
        {
        }

        [BurstCompile]
        private struct TransportTrainTickJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<Owner> m_OwnerType;
            [ReadOnly] public ComponentTypeHandle<Unspawned> m_UnspawnedType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            [ReadOnly] public ComponentTypeHandle<CurrentRoute> m_CurrentRouteType;
            public ComponentTypeHandle<Game.Vehicles.CargoTransport> m_CargoTransportType;
            public ComponentTypeHandle<PublicTransport> m_PublicTransportType;
            public ComponentTypeHandle<Target> m_TargetType;
            public ComponentTypeHandle<PathOwner> m_PathOwnerType;
            public ComponentTypeHandle<Odometer> m_OdometerType;
            public BufferTypeHandle<LayoutElement> m_LayoutElementType;
            public BufferTypeHandle<TrainNavigationLane> m_NavigationLaneType;
            public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;
            [ReadOnly] public EntityStorageInfoLookup m_EntityLookup;
            [ReadOnly] public ComponentLookup<Transform> m_TransformData;
            [ReadOnly] public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;
            [ReadOnly] public ComponentLookup<Owner> m_OwnerData;
            [ReadOnly] public ComponentLookup<PathInformation> m_PathInformationData;
            [ReadOnly] public ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;
            [ReadOnly] public ComponentLookup<ParkedTrain> m_ParkedTrainData;
            [ReadOnly] public ComponentLookup<Controller> m_ControllerData;
            [ReadOnly] public ComponentLookup<Curve> m_CurveData;
            [ReadOnly] public ComponentLookup<Lane> m_LaneData;
            [ReadOnly] public ComponentLookup<EdgeLane> m_EdgeLaneData;
            [ReadOnly] public ComponentLookup<Game.Net.Edge> m_EdgeData;
            [ReadOnly] public ComponentLookup<TrainData> m_PrefabTrainData;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly] public ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleData;
            [ReadOnly] public ComponentLookup<CargoTransportVehicleData> m_CargoTransportVehicleData;
            [ReadOnly] public ComponentLookup<Waypoint> m_WaypointData;
            [ReadOnly] public ComponentLookup<Connected> m_ConnectedData;
            [ReadOnly] public ComponentLookup<BoardingVehicle> m_BoardingVehicleData;
            [ReadOnly] public ComponentLookup<Game.Routes.Color> m_RouteColorData;
            [ReadOnly] public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanyData;
            [ReadOnly] public ComponentLookup<Game.Buildings.TransportStation> m_TransportStationData;
            [ReadOnly] public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;
            [ReadOnly] public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;
            [ReadOnly] public BufferLookup<Passenger> m_Passengers;
            [ReadOnly] public BufferLookup<Resources> m_EconomyResources;
            [ReadOnly] public BufferLookup<RouteWaypoint> m_RouteWaypoints;
            [ReadOnly] public BufferLookup<ConnectedEdge> m_ConnectedEdges;
            [ReadOnly] public BufferLookup<Game.Net.SubLane> m_SubLanes;

            [NativeDisableContainerSafetyRestriction] [ReadOnly]
            public BufferLookup<LayoutElement> m_LayoutElements;

            [NativeDisableParallelForRestriction] public ComponentLookup<Train> m_TrainData;
            [NativeDisableParallelForRestriction] public ComponentLookup<TrainCurrentLane> m_CurrentLaneData;
            [NativeDisableParallelForRestriction] public ComponentLookup<TrainNavigation> m_NavigationData;
            [NativeDisableParallelForRestriction] public ComponentLookup<Blocker> m_BlockerData;
            [NativeDisableParallelForRestriction] public BufferLookup<PathElement> m_PathElements;
            [NativeDisableParallelForRestriction] public BufferLookup<LoadingResources> m_LoadingResources;
            [ReadOnly] public uint m_SimulationFrameIndex;
            [ReadOnly] public RandomSeed m_RandomSeed;
            [ReadOnly] public EntityArchetype m_TransportVehicleRequestArchetype;
            [ReadOnly] public EntityArchetype m_HandleRequestArchetype;
            [ReadOnly] public ComponentTypeSet m_MovingToParkedTrainRemoveTypes;
            [ReadOnly] public ComponentTypeSet m_MovingToParkedTrainAddTypes;
            [ReadOnly] public TransportTrainCarriageSelectData m_TransportTrainCarriageSelectData;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
            public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;
            public TransportBoardingHelpers.BoardingData.Concurrent m_BoardingData;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var nativeArray1 = chunk.GetNativeArray(m_EntityType);
                var nativeArray2 = chunk.GetNativeArray<Owner>(ref m_OwnerType);
                var nativeArray3 = chunk.GetNativeArray<PrefabRef>(ref m_PrefabRefType);
                var nativeArray4 =
                    chunk.GetNativeArray<CurrentRoute>(ref m_CurrentRouteType);
                var nativeArray5 =
                    chunk.GetNativeArray<Game.Vehicles.CargoTransport>(ref m_CargoTransportType);
                var nativeArray6 =
                    chunk.GetNativeArray<PublicTransport>(ref m_PublicTransportType);
                var nativeArray7 = chunk.GetNativeArray<Target>(ref m_TargetType);
                var nativeArray8 = chunk.GetNativeArray<PathOwner>(ref m_PathOwnerType);
                var nativeArray9 = chunk.GetNativeArray<Odometer>(ref m_OdometerType);
                var bufferAccessor1 =
                    chunk.GetBufferAccessor<LayoutElement>(ref m_LayoutElementType);
                var bufferAccessor2 =
                    chunk.GetBufferAccessor<TrainNavigationLane>(ref m_NavigationLaneType);
                var bufferAccessor3 =
                    chunk.GetBufferAccessor<ServiceDispatch>(ref m_ServiceDispatchType);
                var random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
                var isUnspawned = chunk.Has<Unspawned>(ref m_UnspawnedType);
                for (var index = 0; index < nativeArray1.Length; ++index)
                {
                    var vehicleEntity = nativeArray1[index];
                    var owner = nativeArray2[index];
                    var prefabRef = nativeArray3[index];
                    var pathOwner = nativeArray8[index];
                    var target = nativeArray7[index];
                    var odometer = nativeArray9[index];
                    var layout = bufferAccessor1[index];
                    var navigationLanes = bufferAccessor2[index];
                    var serviceDispatches = bufferAccessor3[index];
                    var currentRoute = new CurrentRoute();
                    if (nativeArray4.Length != 0)
                        currentRoute = nativeArray4[index];
                    var cargoTransport = new Game.Vehicles.CargoTransport();
                    if (nativeArray5.Length != 0)
                        cargoTransport = nativeArray5[index];
                    var publicTransport = new PublicTransport();
                    if (nativeArray6.Length != 0)
                        publicTransport = nativeArray6[index];
                    Tick(unfilteredChunkIndex, ref random, vehicleEntity, owner, prefabRef, currentRoute, layout,
                        navigationLanes, serviceDispatches, isUnspawned, ref cargoTransport, ref publicTransport,
                        ref pathOwner, ref target, ref odometer);
                    nativeArray8[index] = pathOwner;
                    nativeArray7[index] = target;
                    nativeArray9[index] = odometer;
                    if (nativeArray5.Length != 0)
                        nativeArray5[index] = cargoTransport;
                    if (nativeArray6.Length != 0)
                        nativeArray6[index] = publicTransport;
                }
            }

            private void Tick(
                int jobIndex,
                ref Random random,
                Entity vehicleEntity,
                Owner owner,
                PrefabRef prefabRef,
                CurrentRoute currentRoute,
                DynamicBuffer<LayoutElement> layout,
                DynamicBuffer<TrainNavigationLane> navigationLanes,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                bool isUnspawned,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref PathOwner pathOwner,
                ref Target target,
                ref Odometer odometer)
            {
                if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
                {
                    DynamicBuffer<LoadingResources> bufferData;
                    if (((cargoTransport.m_State & CargoTransportFlags.DummyTraffic) != (CargoTransportFlags)0 ||
                         (publicTransport.m_State & PublicTransportFlags.DummyTraffic) != (PublicTransportFlags)0) &&
                        m_LoadingResources.TryGetBuffer(vehicleEntity, out bufferData))
                    {
                        if (bufferData.Length != 0) QuantityUpdated(jobIndex, vehicleEntity, layout);

                        if (CheckLoadingResources(jobIndex, ref random, vehicleEntity, true, layout, bufferData))
                        {
                            pathOwner.m_State |= PathFlags.Updated;
                            return;
                        }
                    }

                    cargoTransport.m_State &= ~CargoTransportFlags.Arriving;
                    publicTransport.m_State &= ~PublicTransportFlags.Arriving;
                    var pathElement = m_PathElements[vehicleEntity];
                    var length = VehicleUtils.CalculateLength(vehicleEntity, layout, ref m_PrefabRefData,
                        ref m_PrefabTrainData);
                    var prevElement = new PathElement();
                    if ((pathOwner.m_State & PathFlags.Append) != (PathFlags)0)
                    {
                        if (navigationLanes.Length != 0)
                        {
                            var navigationLane = navigationLanes[navigationLanes.Length - 1];
                            prevElement = new PathElement(navigationLane.m_Lane, navigationLane.m_CurvePosition);
                        }
                    }
                    else
                    {
                        if (VehicleUtils.IsReversedPath(pathElement, pathOwner, vehicleEntity, layout, m_CurveData,
                                m_CurrentLaneData, m_TrainData, m_TransformData))
                            VehicleUtils.ReverseTrain(vehicleEntity, layout, ref m_TrainData,
                                ref m_CurrentLaneData, ref m_NavigationData);
                    }

                    PathUtils.ExtendReverseLocations(prevElement, pathElement, pathOwner, length, m_CurveData,
                        m_LaneData, m_EdgeLaneData, m_OwnerData, m_EdgeData, m_ConnectedEdges,
                        m_SubLanes);
                    if (!m_WaypointData.HasComponent(target.m_Target) ||
                        (m_ConnectedData.HasComponent(target.m_Target) &&
                         m_BoardingVehicleData.HasComponent(m_ConnectedData[target.m_Target].m_Connected)))
                    {
                        var distance = length * 0.5f;
                        PathUtils.ExtendPath(pathElement, pathOwner, ref distance, ref m_CurveData,
                            ref m_LaneData, ref m_EdgeLaneData, ref m_OwnerData, ref m_EdgeData,
                            ref m_ConnectedEdges, ref m_SubLanes);
                    }

                    UpdatePantograph(layout);
                }

                var entity1 = vehicleEntity;
                if (layout.Length != 0)
                    entity1 = layout[0].m_Vehicle;
                var train = m_TrainData[entity1];
                var currentLane = m_CurrentLaneData[entity1];
                VehicleUtils.CheckUnspawned(jobIndex, vehicleEntity, currentLane, isUnspawned, m_CommandBuffer);
                var num1 = (cargoTransport.m_State & CargoTransportFlags.EnRoute) != (CargoTransportFlags)0
                    ? 0
                    : (publicTransport.m_State & PublicTransportFlags.EnRoute) == (PublicTransportFlags)0
                        ? 1
                        : 0;
                if (m_PublicTransportVehicleData.HasComponent(prefabRef.m_Prefab))
                {
                    var transportVehicleData =
                        m_PublicTransportVehicleData[prefabRef.m_Prefab];
                    if ((double)odometer.m_Distance >= (double)transportVehicleData.m_MaintenanceRange &&
                        (double)transportVehicleData.m_MaintenanceRange > 0.10000000149011612 &&
                        (publicTransport.m_State & PublicTransportFlags.Refueling) == (PublicTransportFlags)0)
                        publicTransport.m_State |= PublicTransportFlags.RequiresMaintenance;
                }

                var isCargoVehicle = false;
                if (m_CargoTransportVehicleData.HasComponent(prefabRef.m_Prefab))
                {
                    var transportVehicleData =
                        m_CargoTransportVehicleData[prefabRef.m_Prefab];
                    if ((double)odometer.m_Distance >= (double)transportVehicleData.m_MaintenanceRange &&
                        (double)transportVehicleData.m_MaintenanceRange > 0.10000000149011612 &&
                        (cargoTransport.m_State & CargoTransportFlags.Refueling) == (CargoTransportFlags)0)
                        cargoTransport.m_State |= CargoTransportFlags.RequiresMaintenance;
                    isCargoVehicle = true;
                }

                if (num1 != 0)
                {
                    CheckServiceDispatches(vehicleEntity, serviceDispatches, ref cargoTransport,
                        ref publicTransport);
                    if (serviceDispatches.Length == 0 &&
                        (cargoTransport.m_State & (CargoTransportFlags.RequiresMaintenance |
                                                   CargoTransportFlags.DummyTraffic | CargoTransportFlags.Disabled)) ==
                        (CargoTransportFlags)0 &&
                        (publicTransport.m_State & (PublicTransportFlags.RequiresMaintenance |
                                                    PublicTransportFlags.DummyTraffic |
                                                    PublicTransportFlags.Disabled)) ==
                        (PublicTransportFlags)0)
                        RequestTargetIfNeeded(jobIndex, vehicleEntity, ref publicTransport, ref cargoTransport);
                }
                else
                {
                    serviceDispatches.Clear();
                    cargoTransport.m_RequestCount = 0;
                    publicTransport.m_RequestCount = 0;
                }

                var flag = false;
                if (VehicleUtils.IsStuck(pathOwner))
                {
                    var blocker = m_BlockerData[vehicleEntity];
                    var num2 = m_ParkedTrainData.HasComponent(blocker.m_Blocker) ? 1 : 0;
                    if (num2 != 0)
                    {
                        var entity2 = blocker.m_Blocker;
                        Controller componentData;
                        if (m_ControllerData.TryGetComponent(entity2, out componentData))
                            entity2 = componentData.m_Controller;
                        DynamicBuffer<LayoutElement> bufferData;
                        m_LayoutElements.TryGetBuffer(entity2, out bufferData);
                        VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity2, bufferData);
                    }

                    if (num2 != 0 || blocker.m_Blocker == Entity.Null)
                    {
                        pathOwner.m_State &= ~PathFlags.Stuck;
                        m_BlockerData[vehicleEntity] = new Blocker();
                    }
                }

                if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
                {
                    if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != (CargoTransportFlags)0 ||
                        (publicTransport.m_State & PublicTransportFlags.Boarding) != (PublicTransportFlags)0)
                    {
                        flag = true;
                        StopBoarding(jobIndex, ref random, vehicleEntity, currentRoute, layout, ref cargoTransport,
                            ref publicTransport, ref target, ref odometer, isCargoVehicle, true);
                    }

                    if (VehicleUtils.IsStuck(pathOwner) ||
                        (cargoTransport.m_State & (CargoTransportFlags.Returning | CargoTransportFlags.DummyTraffic)) !=
                        (CargoTransportFlags)0 ||
                        (publicTransport.m_State &
                         (PublicTransportFlags.Returning | PublicTransportFlags.DummyTraffic)) !=
                        (PublicTransportFlags)0)
                    {
                        VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
                        m_TrainData[entity1] = train;
                        m_CurrentLaneData[entity1] = currentLane;
                        return;
                    }

                    ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                        ref cargoTransport,
                        ref publicTransport, ref train, ref pathOwner, ref target);
                }
                else if (VehicleUtils.PathEndReached(currentLane))
                {
                    if ((cargoTransport.m_State & (CargoTransportFlags.Returning | CargoTransportFlags.DummyTraffic)) !=
                        (CargoTransportFlags)0 ||
                        (publicTransport.m_State &
                         (PublicTransportFlags.Returning | PublicTransportFlags.DummyTraffic)) !=
                        (PublicTransportFlags)0)
                    {
                        if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != (CargoTransportFlags)0 ||
                            (publicTransport.m_State & PublicTransportFlags.Boarding) != (PublicTransportFlags)0)
                        {
                            if (StopBoarding(jobIndex, ref random, vehicleEntity, currentRoute, layout,
                                    ref cargoTransport, ref publicTransport, ref target, ref odometer, isCargoVehicle,
                                    false))
                            {
                                flag = true;
                                if (!SelectNextDispatch(jobIndex, vehicleEntity, currentRoute, layout,
                                        navigationLanes,
                                        serviceDispatches, ref cargoTransport, ref publicTransport, ref train,
                                        ref currentLane, ref pathOwner, ref target))
                                {
                                    if (!TryParkTrain(jobIndex, vehicleEntity, owner, layout, navigationLanes,
                                            ref train, ref cargoTransport, ref publicTransport, ref currentLane))
                                        VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity,
                                            layout);

                                    m_TrainData[entity1] = train;
                                    m_CurrentLaneData[entity1] = currentLane;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if ((CountPassengers(vehicleEntity, layout) <= 0 || !StartBoarding(jobIndex,
                                    vehicleEntity, currentRoute, prefabRef, ref cargoTransport, ref publicTransport,
                                    ref target, isCargoVehicle)) && !SelectNextDispatch(jobIndex, vehicleEntity,
                                    currentRoute, layout, navigationLanes, serviceDispatches, ref cargoTransport,
                                    ref publicTransport, ref train, ref currentLane, ref pathOwner, ref target))
                            {
                                if (!TryParkTrain(jobIndex, vehicleEntity, owner, layout, navigationLanes,
                                        ref train,
                                        ref cargoTransport, ref publicTransport, ref currentLane))
                                    VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);

                                m_TrainData[entity1] = train;
                                m_CurrentLaneData[entity1] = currentLane;
                                return;
                            }
                        }
                    }
                    else if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != (CargoTransportFlags)0 ||
                             (publicTransport.m_State & PublicTransportFlags.Boarding) != (PublicTransportFlags)0)
                    {
                        if (StopBoarding(jobIndex, ref random, vehicleEntity, currentRoute, layout,
                                ref cargoTransport,
                                ref publicTransport, ref target, ref odometer, isCargoVehicle, false))
                        {
                            flag = true;
                            if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) == (CargoTransportFlags)0 &&
                                (publicTransport.m_State & PublicTransportFlags.EnRoute) == (PublicTransportFlags)0)
                                ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                    ref cargoTransport, ref publicTransport, ref train, ref pathOwner, ref target);
                            else
                                SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                        }
                    }
                    else
                    {
                        if (!m_RouteWaypoints.HasBuffer(currentRoute.m_Route) ||
                            !m_WaypointData.HasComponent(target.m_Target))
                        {
                            ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                ref cargoTransport, ref publicTransport, ref train, ref pathOwner, ref target);
                        }
                        else
                        {
                            if (!StartBoarding(jobIndex, vehicleEntity, currentRoute, prefabRef,
                                    ref cargoTransport,
                                    ref publicTransport, ref target, isCargoVehicle))
                            {
                                if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) == (CargoTransportFlags)0 &&
                                    (publicTransport.m_State & PublicTransportFlags.EnRoute) == (PublicTransportFlags)0)
                                    ReturnToDepot(jobIndex, vehicleEntity, currentRoute, owner, serviceDispatches,
                                        ref cargoTransport, ref publicTransport, ref train, ref pathOwner, ref target);
                                else
                                    SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                            }
                        }
                    }
                }
                else if (VehicleUtils.ReturnEndReached(currentLane))
                {
                    m_TrainData[entity1] = train;
                    m_CurrentLaneData[entity1] = currentLane;
                    VehicleUtils.ReverseTrain(vehicleEntity, layout, ref m_TrainData, ref m_CurrentLaneData,
                        ref m_NavigationData);
                    UpdatePantograph(layout);
                    entity1 = vehicleEntity;
                    if (layout.Length != 0)
                        entity1 = layout[0].m_Vehicle;
                    train = m_TrainData[entity1];
                    currentLane = m_CurrentLaneData[entity1];
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != (CargoTransportFlags)0 ||
                         (publicTransport.m_State & PublicTransportFlags.Boarding) != (PublicTransportFlags)0)
                {
                    flag = true;
                    StopBoarding(jobIndex, ref random, vehicleEntity, currentRoute, layout, ref cargoTransport,
                        ref publicTransport, ref target, ref odometer, isCargoVehicle, true);
                }

                train.m_Flags &= ~(Game.Vehicles.TrainFlags.BoardingLeft | Game.Vehicles.TrainFlags.BoardingRight);
                publicTransport.m_State &= ~(PublicTransportFlags.StopLeft | PublicTransportFlags.StopRight);
                var skipWaypoint = Entity.Null;
                if ((cargoTransport.m_State & CargoTransportFlags.Boarding) != (CargoTransportFlags)0 ||
                    (publicTransport.m_State & PublicTransportFlags.Boarding) != (PublicTransportFlags)0)
                {
                    if (!flag)
                    {
                        var controllerTrain = m_TrainData[vehicleEntity];
                        UpdateStop(entity1, controllerTrain, true, ref train, ref publicTransport, ref target);
                    }
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.Returning) != (CargoTransportFlags)0 ||
                         (publicTransport.m_State & PublicTransportFlags.Returning) != (PublicTransportFlags)0)
                {
                    if (CountPassengers(vehicleEntity, layout) == 0)
                        SelectNextDispatch(jobIndex, vehicleEntity, currentRoute, layout, navigationLanes,
                            serviceDispatches, ref cargoTransport, ref publicTransport, ref train, ref currentLane,
                            ref pathOwner, ref target);
                }
                else if ((cargoTransport.m_State & CargoTransportFlags.Arriving) != (CargoTransportFlags)0 ||
                         (publicTransport.m_State & PublicTransportFlags.Arriving) != (PublicTransportFlags)0)
                {
                    var controllerTrain = m_TrainData[vehicleEntity];
                    UpdateStop(entity1, controllerTrain, false, ref train, ref publicTransport, ref target);
                }
                else
                {
                    CheckNavigationLanes(currentRoute, navigationLanes, ref cargoTransport, ref publicTransport,
                        ref currentLane, ref pathOwner, ref target, out skipWaypoint);
                }

                if ((((cargoTransport.m_State & CargoTransportFlags.Boarding) != (CargoTransportFlags)0
                         ? 0
                         : (publicTransport.m_State & PublicTransportFlags.Boarding) == (PublicTransportFlags)0
                             ? 1
                             : 0) |
                     (flag ? 1 : 0)) != 0)
                {
                    if (VehicleUtils.RequireNewPath(pathOwner))
                        FindNewPath(vehicleEntity, prefabRef, skipWaypoint, ref currentLane, ref cargoTransport,
                            ref publicTransport, ref pathOwner, ref target);
                    else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) ==
                             (PathFlags)0)
                        CheckParkingSpace(navigationLanes, ref train, ref pathOwner);
                }

                m_TrainData[entity1] = train;
                m_CurrentLaneData[entity1] = currentLane;
            }

            private void CheckParkingSpace(
                DynamicBuffer<TrainNavigationLane> navigationLanes,
                ref Train train,
                ref PathOwner pathOwner)
            {
                if (navigationLanes.Length == 0)
                    return;
                var navigationLane = navigationLanes[navigationLanes.Length - 1];
                Game.Objects.SpawnLocation componentData;
                if ((navigationLane.m_Flags & TrainLaneFlags.ParkingSpace) == (TrainLaneFlags)0 ||
                    !m_SpawnLocationData.TryGetComponent(navigationLane.m_Lane, out componentData))
                    return;
                if ((componentData.m_Flags & SpawnLocationFlags.ParkedVehicle) != (SpawnLocationFlags)0)
                {
                    if ((train.m_Flags & Game.Vehicles.TrainFlags.IgnoreParkedVehicle) != (Game.Vehicles.TrainFlags)0)
                        return;
                    train.m_Flags |= Game.Vehicles.TrainFlags.IgnoreParkedVehicle;
                    pathOwner.m_State |= PathFlags.Obsolete;
                }
                else
                {
                    train.m_Flags &= ~Game.Vehicles.TrainFlags.IgnoreParkedVehicle;
                }
            }

            private bool TryParkTrain(
                int jobIndex,
                Entity entity,
                Owner owner,
                DynamicBuffer<LayoutElement> layout,
                DynamicBuffer<TrainNavigationLane> navigationLanes,
                ref Train train,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref TrainCurrentLane currentLane)
            {
                if (navigationLanes.Length == 0)
                    return false;
                var navigationLane = navigationLanes[navigationLanes.Length - 1];
                if ((navigationLane.m_Flags & TrainLaneFlags.ParkingSpace) == (TrainLaneFlags)0)
                    return false;
                train.m_Flags &= ~(Game.Vehicles.TrainFlags.BoardingLeft | Game.Vehicles.TrainFlags.BoardingRight |
                                   Game.Vehicles.TrainFlags.Pantograph | Game.Vehicles.TrainFlags.IgnoreParkedVehicle);
                cargoTransport.m_State &= CargoTransportFlags.RequiresMaintenance;
                publicTransport.m_State &= PublicTransportFlags.RequiresMaintenance;
                Game.Buildings.TransportDepot componentData;
                if (m_TransportDepotData.TryGetComponent(owner.m_Owner, out componentData) &&
                    (componentData.m_Flags & TransportDepotFlags.HasAvailableVehicles) == (TransportDepotFlags)0)
                {
                    cargoTransport.m_State |= CargoTransportFlags.Disabled;
                    publicTransport.m_State |= PublicTransportFlags.Disabled;
                }

                for (var index = 0; index < layout.Length; ++index)
                {
                    var vehicle = layout[index].m_Vehicle;
                    m_CommandBuffer.RemoveComponent(jobIndex, vehicle, in m_MovingToParkedTrainRemoveTypes);
                    m_CommandBuffer.AddComponent(jobIndex, vehicle, in m_MovingToParkedTrainAddTypes);
                    if (index == 0)
                    {
                        m_CommandBuffer.SetComponent<ParkedTrain>(jobIndex, vehicle,
                            new ParkedTrain(navigationLane.m_Lane, currentLane));
                    }
                    else
                    {
                        var currentLane1 = m_CurrentLaneData[vehicle];
                        m_CommandBuffer.SetComponent<ParkedTrain>(jobIndex, vehicle,
                            new ParkedTrain(navigationLane.m_Lane, currentLane1));
                    }
                }

                if (m_SpawnLocationData.HasComponent(navigationLane.m_Lane))
                    m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, navigationLane.m_Lane);
                else
                    m_CommandBuffer.AddComponent<FixParkingLocation>(jobIndex, entity,
                        new FixParkingLocation(Entity.Null, entity));

                return true;
            }

            private void UpdatePantograph(DynamicBuffer<LayoutElement> layout)
            {
                var flag = false;
                for (var index = 0; index < layout.Length; ++index)
                {
                    var vehicle = layout[index].m_Vehicle;
                    var train = m_TrainData[vehicle];
                    var trainData = m_PrefabTrainData[m_PrefabRefData[vehicle].m_Prefab];
                    if (flag || (trainData.m_TrainFlags & Game.Prefabs.TrainFlags.Pantograph) ==
                        (Game.Prefabs.TrainFlags)0)
                    {
                        train.m_Flags &= ~Game.Vehicles.TrainFlags.Pantograph;
                    }
                    else
                    {
                        train.m_Flags |= Game.Vehicles.TrainFlags.Pantograph;
                        flag = (trainData.m_TrainFlags & Game.Prefabs.TrainFlags.MultiUnit) != 0;
                    }

                    m_TrainData[vehicle] = train;
                }
            }

            private void UpdateStop(
                Entity vehicleEntity,
                Train controllerTrain,
                bool isBoarding,
                ref Train train,
                ref PublicTransport publicTransport,
                ref Target target)
            {
                var transform = m_TransformData[vehicleEntity];
                Connected componentData1;
                Transform componentData2;
                if (!m_ConnectedData.TryGetComponent(target.m_Target, out componentData1) ||
                    !m_TransformData.TryGetComponent(componentData1.m_Connected, out componentData2))
                    return;
                var flag = (double)math.dot(math.mul(transform.m_Rotation, math.right()),
                    componentData2.m_Position - transform.m_Position) < 0.0;
                if (isBoarding)
                {
                    if (flag)
                        train.m_Flags |= Game.Vehicles.TrainFlags.BoardingLeft;
                    else
                        train.m_Flags |= Game.Vehicles.TrainFlags.BoardingRight;
                }

                if (flag ^ (((controllerTrain.m_Flags ^ train.m_Flags) & Game.Vehicles.TrainFlags.Reversed) >
                            (Game.Vehicles.TrainFlags)0))
                    publicTransport.m_State |= PublicTransportFlags.StopLeft;
                else
                    publicTransport.m_State |= PublicTransportFlags.StopRight;
            }

            private void FindNewPath(
                Entity vehicleEntity,
                PrefabRef prefabRef,
                Entity skipWaypoint,
                ref TrainCurrentLane currentLane,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref PathOwner pathOwner,
                ref Target target)
            {
                var trainData = m_PrefabTrainData[prefabRef.m_Prefab];
                var parameters = new PathfindParameters
                {
                    m_MaxSpeed = (float2)trainData.m_MaxSpeed,
                    m_WalkSpeed = (float2)5.555556f,
                    m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
                    m_Methods = PathMethod.Track,
                    m_IgnoredRules = RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic |
                                     RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic |
                                     RuleFlags.AvoidBicycles
                };
                var origin = new SetupQueueTarget
                {
                    m_Type = SetupTargetType.CurrentLocation,
                    m_Methods = PathMethod.Track,
                    m_TrackTypes = trainData.m_TrackType
                };
                var destination = new SetupQueueTarget
                {
                    m_Type = SetupTargetType.CurrentLocation,
                    m_Methods = PathMethod.Track,
                    m_TrackTypes = trainData.m_TrackType,
                    m_Entity = target.m_Target
                };
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
                else if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) == (CargoTransportFlags)0 &&
                         (publicTransport.m_State & PublicTransportFlags.EnRoute) == (PublicTransportFlags)0)
                {
                    cargoTransport.m_State &= ~CargoTransportFlags.RouteSource;
                    publicTransport.m_State &= ~PublicTransportFlags.RouteSource;
                }

                if ((cargoTransport.m_State & CargoTransportFlags.Returning) != (CargoTransportFlags)0 ||
                    (publicTransport.m_State & PublicTransportFlags.Returning) != (PublicTransportFlags)0)
                    destination.m_RandomCost = 30f;
                var setupQueueItem = new SetupQueueItem(vehicleEntity, parameters, origin, destination);
                VehicleUtils.SetupPathfind(ref currentLane, ref pathOwner, m_PathfindQueue, setupQueueItem);
            }

            private void CheckNavigationLanes(
                CurrentRoute currentRoute,
                DynamicBuffer<TrainNavigationLane> navigationLanes,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref TrainCurrentLane currentLane,
                ref PathOwner pathOwner,
                ref Target target,
                out Entity skipWaypoint)
            {
                skipWaypoint = Entity.Null;
                if (navigationLanes.Length == 0 || navigationLanes.Length >= 10)
                    return;
                var navigationLane = navigationLanes[navigationLanes.Length - 1];
                if ((navigationLane.m_Flags & TrainLaneFlags.EndOfPath) == (TrainLaneFlags)0)
                    return;
                if (m_WaypointData.HasComponent(target.m_Target) &&
                    m_RouteWaypoints.HasBuffer(currentRoute.m_Route) &&
                    (!m_ConnectedData.HasComponent(target.m_Target) ||
                     !m_BoardingVehicleData.HasComponent(m_ConnectedData[target.m_Target].m_Connected)))
                {
                    if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete)) !=
                        (PathFlags)0)
                        return;
                    skipWaypoint = target.m_Target;
                    SetNextWaypointTarget(currentRoute, ref pathOwner, ref target);
                    navigationLane.m_Flags &= ~TrainLaneFlags.EndOfPath;
                    navigationLanes[navigationLanes.Length - 1] = navigationLane;
                    cargoTransport.m_State |= CargoTransportFlags.RouteSource;
                    publicTransport.m_State |= PublicTransportFlags.RouteSource;
                }
                else
                {
                    cargoTransport.m_State |= CargoTransportFlags.Arriving;
                    publicTransport.m_State |= PublicTransportFlags.Arriving;
                }
            }

            private void SetNextWaypointTarget(
                CurrentRoute currentRoute,
                ref PathOwner pathOwnerData,
                ref Target targetData)
            {
                var routeWaypoint = m_RouteWaypoints[currentRoute.m_Route];
                var falseValue = m_WaypointData[targetData.m_Target].m_Index + 1;
                var index = math.select(falseValue, 0, falseValue >= routeWaypoint.Length);
                VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, routeWaypoint[index].m_Waypoint);
            }

            private void CheckServiceDispatches(
                Entity vehicleEntity,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport)
            {
                if (serviceDispatches.Length > 1)
                    serviceDispatches.RemoveRange(1, serviceDispatches.Length - 1);
                cargoTransport.m_RequestCount = math.min(1, cargoTransport.m_RequestCount);
                publicTransport.m_RequestCount = math.min(1, publicTransport.m_RequestCount);
                var index1 = cargoTransport.m_RequestCount + publicTransport.m_RequestCount;
                if (serviceDispatches.Length <= index1)
                    return;
                var num = -1f;
                var request1 = Entity.Null;
                for (var index2 = index1; index2 < serviceDispatches.Length; ++index2)
                {
                    var request2 = serviceDispatches[index2].m_Request;
                    if (m_TransportVehicleRequestData.HasComponent(request2))
                    {
                        var transportVehicleRequest = m_TransportVehicleRequestData[request2];
                        if (m_PrefabRefData.HasComponent(transportVehicleRequest.m_Route) &&
                            (double)transportVehicleRequest.m_Priority > (double)num)
                        {
                            num = transportVehicleRequest.m_Priority;
                            request1 = request2;
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
                ref Game.Vehicles.CargoTransport cargoTransport)
            {
                if (m_TransportVehicleRequestData.HasComponent(publicTransport.m_TargetRequest) ||
                    m_TransportVehicleRequestData.HasComponent(cargoTransport.m_TargetRequest) ||
                    ((int)m_SimulationFrameIndex & ((int)math.max(256U /*0x0100*/, 16U /*0x10*/) - 1)) != 3)
                    return;
                var entity1 = m_CommandBuffer.CreateEntity(jobIndex, m_TransportVehicleRequestArchetype);
                m_CommandBuffer.SetComponent<ServiceRequest>(jobIndex, entity1, new ServiceRequest(true));
                m_CommandBuffer.SetComponent<TransportVehicleRequest>(jobIndex, entity1,
                    new TransportVehicleRequest(entity, 1f));
                m_CommandBuffer.SetComponent<RequestGroup>(jobIndex, entity1, new RequestGroup(8U));
            }

            private bool SelectNextDispatch(
                int jobIndex,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                DynamicBuffer<LayoutElement> layout,
                DynamicBuffer<TrainNavigationLane> navigationLanes,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Train train,
                ref TrainCurrentLane currentLane,
                ref PathOwner pathOwner,
                ref Target target)
            {
                if ((cargoTransport.m_State & CargoTransportFlags.Returning) == (CargoTransportFlags)0 &&
                    (publicTransport.m_State & PublicTransportFlags.Returning) == (PublicTransportFlags)0 &&
                    cargoTransport.m_RequestCount + publicTransport.m_RequestCount > 0 && serviceDispatches.Length > 0)
                {
                    serviceDispatches.RemoveAt(0);
                    cargoTransport.m_RequestCount = math.max(0, cargoTransport.m_RequestCount - 1);
                    publicTransport.m_RequestCount = math.max(0, publicTransport.m_RequestCount - 1);
                }

                if ((cargoTransport.m_State &
                     (CargoTransportFlags.RequiresMaintenance | CargoTransportFlags.Disabled)) !=
                    (CargoTransportFlags)0 ||
                    (publicTransport.m_State &
                     (PublicTransportFlags.RequiresMaintenance | PublicTransportFlags.Disabled)) !=
                    (PublicTransportFlags)0)
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
                    var destination = Entity.Null;
                    if (m_TransportVehicleRequestData.HasComponent(request))
                    {
                        route = m_TransportVehicleRequestData[request].m_Route;
                        if (m_PathInformationData.HasComponent(request))
                            destination = m_PathInformationData[request].m_Destination;
                    }

                    if (!m_PrefabRefData.HasComponent(destination))
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
                                    m_CommandBuffer.AddComponent<CurrentRoute>(jobIndex, vehicleEntity,
                                        new CurrentRoute(route));
                                    m_CommandBuffer.AppendToBuffer<RouteVehicle>(jobIndex, route,
                                        new RouteVehicle(vehicleEntity));
                                    Game.Routes.Color componentData;
                                    if (m_RouteColorData.TryGetComponent(route, out componentData))
                                    {
                                        m_CommandBuffer.AddComponent<Game.Routes.Color>(jobIndex, vehicleEntity,
                                            componentData);
                                        UpdateBatches(jobIndex, vehicleEntity, layout);
                                    }
                                }

                                cargoTransport.m_State |= CargoTransportFlags.EnRoute;
                                publicTransport.m_State |= PublicTransportFlags.EnRoute;
                            }
                            else
                            {
                                m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);
                            }

                            var entity = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
                            m_CommandBuffer.SetComponent<HandleRequest>(jobIndex, entity,
                                new HandleRequest(request, vehicleEntity, true));
                        }
                        else
                        {
                            m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);
                            var entity = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
                            m_CommandBuffer.SetComponent<HandleRequest>(jobIndex, entity,
                                new HandleRequest(request, vehicleEntity, false, true));
                        }

                        cargoTransport.m_State &= ~CargoTransportFlags.Returning;
                        publicTransport.m_State &= ~PublicTransportFlags.Returning;
                        train.m_Flags &= ~Game.Vehicles.TrainFlags.IgnoreParkedVehicle;
                        if (m_TransportVehicleRequestData.HasComponent(publicTransport.m_TargetRequest))
                        {
                            var entity = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
                            m_CommandBuffer.SetComponent<HandleRequest>(jobIndex, entity,
                                new HandleRequest(publicTransport.m_TargetRequest, Entity.Null, true));
                        }

                        if (m_TransportVehicleRequestData.HasComponent(cargoTransport.m_TargetRequest))
                        {
                            var entity = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
                            m_CommandBuffer.SetComponent<HandleRequest>(jobIndex, entity,
                                new HandleRequest(cargoTransport.m_TargetRequest, Entity.Null, true));
                        }

                        if (m_PathElements.HasBuffer(request))
                        {
                            var pathElement1 = m_PathElements[request];
                            if (pathElement1.Length != 0)
                            {
                                var pathElement2 = m_PathElements[vehicleEntity];
                                PathUtils.TrimPath(pathElement2, ref pathOwner);
                                var num = math.max(cargoTransport.m_PathElementTime,
                                        publicTransport.m_PathElementTime) *
                                    (float)pathElement2.Length + m_PathInformationData[request].m_Duration;
                                if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, pathElement2,
                                        pathElement1))
                                {
                                    cargoTransport.m_PathElementTime = num / (float)math.max(1, pathElement2.Length);
                                    publicTransport.m_PathElementTime = cargoTransport.m_PathElementTime;
                                    target.m_Target = destination;
                                    VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
                                    cargoTransport.m_State &= ~CargoTransportFlags.Arriving;
                                    publicTransport.m_State &= ~PublicTransportFlags.Arriving;
                                    var length = VehicleUtils.CalculateLength(vehicleEntity, layout,
                                        ref m_PrefabRefData, ref m_PrefabTrainData);
                                    var prevElement = new PathElement();
                                    if (navigationLanes.Length != 0)
                                    {
                                        var navigationLane =
                                            navigationLanes[navigationLanes.Length - 1];
                                        prevElement = new PathElement(navigationLane.m_Lane,
                                            navigationLane.m_CurvePosition);
                                    }

                                    PathUtils.ExtendReverseLocations(prevElement, pathElement2, pathOwner, length,
                                        m_CurveData, m_LaneData, m_EdgeLaneData, m_OwnerData,
                                        m_EdgeData, m_ConnectedEdges, m_SubLanes);
                                    if (!m_WaypointData.HasComponent(target.m_Target) ||
                                        (m_ConnectedData.HasComponent(target.m_Target) &&
                                         m_BoardingVehicleData.HasComponent(m_ConnectedData[target.m_Target]
                                             .m_Connected)))
                                    {
                                        var distance = length * 0.5f;
                                        PathUtils.ExtendPath(pathElement2, pathOwner, ref distance,
                                            ref m_CurveData,
                                            ref m_LaneData, ref m_EdgeLaneData, ref m_OwnerData,
                                            ref m_EdgeData, ref m_ConnectedEdges, ref m_SubLanes);
                                    }

                                    return true;
                                }
                            }
                        }

                        VehicleUtils.SetTarget(ref pathOwner, ref target, destination);
                        return true;
                    }
                }

                return false;
            }

            private void UpdateBatches(
                int jobIndex,
                Entity vehicleEntity,
                DynamicBuffer<LayoutElement> layout)
            {
                if (layout.Length != 0)
                    m_CommandBuffer.AddComponent<BatchesUpdated>(jobIndex,
                        layout.Reinterpret<Entity>().AsNativeArray());
                else
                    m_CommandBuffer.AddComponent<BatchesUpdated>(jobIndex, vehicleEntity);
            }

            private void ReturnToDepot(
                int jobIndex,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                Owner ownerData,
                DynamicBuffer<ServiceDispatch> serviceDispatches,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Train train,
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
                train.m_Flags &= ~Game.Vehicles.TrainFlags.IgnoreParkedVehicle;
                m_CommandBuffer.RemoveComponent<CurrentRoute>(jobIndex, vehicleEntity);
                VehicleUtils.SetTarget(ref pathOwner, ref target, ownerData.m_Owner);
            }

            private bool StartBoarding(
                int jobIndex,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                PrefabRef prefabRef,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Target target,
                bool isCargoVehicle)
            {
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
                            var trainData = m_PrefabTrainData[prefabRef.m_Prefab];
                            refuel = (m_TransportStationData[transportStationFromStop].m_TrainRefuelTypes &
                                      trainData.m_EnergyType) != 0;
                        }

                        if ((!refuel &&
                             ((cargoTransport.m_State & CargoTransportFlags.RequiresMaintenance) !=
                              (CargoTransportFlags)0 ||
                              (publicTransport.m_State & PublicTransportFlags.RequiresMaintenance) !=
                              (PublicTransportFlags)0)) ||
                            (cargoTransport.m_State & CargoTransportFlags.AbandonRoute) != (CargoTransportFlags)0 ||
                            (publicTransport.m_State & PublicTransportFlags.AbandonRoute) != (PublicTransportFlags)0)
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

            private bool TryChangeCarriagePrefab(
                int jobIndex,
                ref Random random,
                Entity vehicleEntity,
                bool dummyTraffic,
                DynamicBuffer<LoadingResources> loadingResources)
            {
                if (m_EconomyResources.HasBuffer(vehicleEntity))
                {
                    var economyResource = m_EconomyResources[vehicleEntity];
                    var prefabRef = m_PrefabRefData[vehicleEntity];
                    if (economyResource.Length == 0 &&
                        m_CargoTransportVehicleData.HasComponent(prefabRef.m_Prefab))
                        while (loadingResources.Length > 0)
                        {
                            var loadingResource = loadingResources[0];
                            var entity = m_TransportTrainCarriageSelectData.SelectCarriagePrefab(ref random,
                                loadingResource.m_Resource, loadingResource.m_Amount);
                            if (entity != Entity.Null)
                            {
                                var transportVehicleData =
                                    m_CargoTransportVehicleData[entity];
                                var num = math.min(loadingResource.m_Amount, transportVehicleData.m_CargoCapacity);
                                loadingResource.m_Amount -= transportVehicleData.m_CargoCapacity;
                                if (loadingResource.m_Amount <= 0)
                                    loadingResources.RemoveAt(0);
                                else
                                    loadingResources[0] = loadingResource;
                                if (dummyTraffic)
                                    m_CommandBuffer.SetBuffer<Resources>(jobIndex, vehicleEntity).Add(
                                        new Resources
                                        {
                                            m_Resource = loadingResource.m_Resource,
                                            m_Amount = num
                                        });

                                m_CommandBuffer.SetComponent<PrefabRef>(jobIndex, vehicleEntity,
                                    new PrefabRef(entity));
                                m_CommandBuffer.AddComponent<Updated>(jobIndex, vehicleEntity, new Updated());
                                return true;
                            }

                            loadingResources.RemoveAt(0);
                        }
                }

                return false;
            }

            private bool CheckLoadingResources(
                int jobIndex,
                ref Random random,
                Entity vehicleEntity,
                bool dummyTraffic,
                DynamicBuffer<LayoutElement> layout,
                DynamicBuffer<LoadingResources> loadingResources)
            {
                var flag = false;
                if (loadingResources.Length != 0)
                {
                    if (layout.Length != 0)
                        for (var index = 0; index < layout.Length && loadingResources.Length != 0; ++index)
                            flag |= TryChangeCarriagePrefab(jobIndex, ref random, layout[index].m_Vehicle,
                                dummyTraffic, loadingResources);
                    else
                        flag |= TryChangeCarriagePrefab(jobIndex, ref random, vehicleEntity, dummyTraffic,
                            loadingResources);

                    loadingResources.Clear();
                }

                return flag;
            }

            private bool StopBoarding(
                int jobIndex,
                ref Random random,
                Entity vehicleEntity,
                CurrentRoute currentRoute,
                DynamicBuffer<LayoutElement> layout,
                ref Game.Vehicles.CargoTransport cargoTransport,
                ref PublicTransport publicTransport,
                ref Target target,
                ref Odometer odometer,
                bool isCargoVehicle,
                bool forcedStop)
            {
                var flag1 = false;
                if (m_LoadingResources.HasBuffer(vehicleEntity))
                {
                    var loadingResource = m_LoadingResources[vehicleEntity];
                    if (forcedStop)
                    {
                        loadingResource.Clear();
                    }
                    else
                    {
                        var dummyTraffic =
                            (cargoTransport.m_State & CargoTransportFlags.DummyTraffic) != (CargoTransportFlags)0 ||
                            (publicTransport.m_State & PublicTransportFlags.DummyTraffic) > (PublicTransportFlags)0;
                        flag1 |= CheckLoadingResources(jobIndex, ref random, vehicleEntity, dummyTraffic, layout,
                            loadingResource);
                    }
                }

                if (flag1)
                    return false;
                var flag2 = false;
                Connected componentData1;
                BoardingVehicle componentData2;
                if (m_ConnectedData.TryGetComponent(target.m_Target, out componentData1) &&
                    m_BoardingVehicleData.TryGetComponent(componentData1.m_Connected, out componentData2))
                    flag2 = componentData2.m_Vehicle == vehicleEntity;
                if (!forcedStop)
                {
                    publicTransport.m_MaxBoardingDistance = math.select(publicTransport.m_MinWaitingDistance + 1f,
                        float.MaxValue,
                        (double)publicTransport.m_MinWaitingDistance == 3.4028234663852886E+38 ||
                        (double)publicTransport.m_MinWaitingDistance == 0.0);
                    publicTransport.m_MinWaitingDistance = float.MaxValue;
                    if (flag2 && (m_SimulationFrameIndex < cargoTransport.m_DepartureFrame ||
                                  m_SimulationFrameIndex < publicTransport.m_DepartureFrame ||
                                  (double)publicTransport.m_MaxBoardingDistance != 3.4028234663852886E+38))
                        return false;
                    var boardingComplete = ArePassengersReady(vehicleEntity, ref layout, publicTransport);

                    //if boarding is complete, still want to run the rest of the code below, which will clear the boarding flag.
                    //if it's not complete, we can short circuit this, as per original impl.
                    if (!boardingComplete)
                        return false;
                }

                if ((cargoTransport.m_State & CargoTransportFlags.Refueling) != (CargoTransportFlags)0 ||
                    (publicTransport.m_State & PublicTransportFlags.Refueling) != (PublicTransportFlags)0)
                    odometer.m_Distance = 0.0f;
                if (isCargoVehicle) QuantityUpdated(jobIndex, vehicleEntity, layout);

                if (flag2)
                {
                    var storageCompanyFromStop = Entity.Null;
                    var nextStorageCompany = Entity.Null;
                    if (isCargoVehicle && !forcedStop)
                    {
                        storageCompanyFromStop = GetStorageCompanyFromStop(componentData1.m_Connected);
                        if ((cargoTransport.m_State & CargoTransportFlags.EnRoute) != (CargoTransportFlags)0)
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

            private void QuantityUpdated(
                int jobIndex,
                Entity vehicleEntity,
                DynamicBuffer<LayoutElement> layout)
            {
                if (layout.Length != 0)
                    for (var index = 0; index < layout.Length; ++index)
                        m_CommandBuffer.AddComponent<Updated>(jobIndex, layout[index].m_Vehicle, new Updated());
                else
                    m_CommandBuffer.AddComponent<Updated>(jobIndex, vehicleEntity, new Updated());
            }

            private int CountPassengers(Entity vehicleEntity, DynamicBuffer<LayoutElement> layout)
            {
                var num = 0;
                if (layout.Length != 0)
                {
                    for (var index = 0; index < layout.Length; ++index)
                    {
                        var vehicle = layout[index].m_Vehicle;
                        if (m_Passengers.HasBuffer(vehicle)) num += m_Passengers[vehicle].Length;
                    }
                }
                else
                {
                    if (m_Passengers.HasBuffer(vehicleEntity)) num += m_Passengers[vehicleEntity].Length;
                }

                return num;
            }

            private bool ArePassengersReady(Entity vehicleEntity, ref DynamicBuffer<LayoutElement> layout,
                PublicTransport publicTransport)
            {
                var boardingComplete = true;
                if (layout.Length != 0)
                    for (var index = 0; index < layout.Length; ++index)
                    {
                        var layoutIndexVehicle = layout[index].m_Vehicle;
                        if (!m_Passengers.HasBuffer(layoutIndexVehicle)) continue;
                        var layoutIndexVehiclePassengers = m_Passengers[layoutIndexVehicle];
                        if (PublicTransportBoardingHelper.ArePassengersReady(layoutIndexVehiclePassengers,
                                m_CurrentVehicleData,
                                publicTransport,
                                PublicTransportBoardingHelper.TransportFamily.Train,
                                m_SimulationFrameIndex)) continue;
                        boardingComplete = false;
                        break;
                    }
                else
                    boardingComplete = PublicTransportBoardingHelper.ArePassengersReady(
                        m_Passengers[vehicleEntity],
                        m_CurrentVehicleData, publicTransport,
                        PublicTransportBoardingHelper.TransportFamily.Train,
                        m_SimulationFrameIndex);

                return boardingComplete;
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
                var falseValue = m_WaypointData[currentWaypoint].m_Index + 1;
                for (var index1 = 0; index1 < routeWaypoint.Length; ++index1)
                {
                    var index2 = math.select(falseValue, 0, falseValue >= routeWaypoint.Length);
                    var waypoint = routeWaypoint[index2].m_Waypoint;
                    if (m_ConnectedData.HasComponent(waypoint))
                    {
                        var storageCompanyFromStop =
                            GetStorageCompanyFromStop(m_ConnectedData[waypoint].m_Connected);
                        if (storageCompanyFromStop != Entity.Null)
                            return storageCompanyFromStop;
                    }

                    falseValue = index2 + 1;
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
            [ReadOnly] public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentTypeHandle;

            public ComponentTypeHandle<Game.Vehicles.CargoTransport>
                __Game_Vehicles_CargoTransport_RW_ComponentTypeHandle;

            public ComponentTypeHandle<PublicTransport>
                __Game_Vehicles_PublicTransport_RW_ComponentTypeHandle;

            public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;
            public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;
            public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RW_ComponentTypeHandle;
            public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RW_BufferTypeHandle;
            public BufferTypeHandle<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle;
            public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;
            [ReadOnly] public EntityStorageInfoLookup __EntityStorageInfoLookup;
            [ReadOnly] public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<TransportVehicleRequest>
                __Game_Simulation_TransportVehicleRequest_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<PublicTransportVehicleData>
                __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<CargoTransportVehicleData>
                __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<BoardingVehicle> __Game_Routes_BoardingVehicle_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Game.Routes.Color> __Game_Routes_Color_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<Game.Buildings.TransportStation>
                __Game_Buildings_TransportStation_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RO_ComponentLookup;

            [ReadOnly] public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;
            [ReadOnly] public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;
            [ReadOnly] public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;
            [ReadOnly] public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;
            [ReadOnly] public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;
            [ReadOnly] public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;
            public ComponentLookup<Train> __Game_Vehicles_Train_RW_ComponentLookup;
            public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RW_ComponentLookup;
            public ComponentLookup<TrainNavigation> __Game_Vehicles_TrainNavigation_RW_ComponentLookup;
            public ComponentLookup<Blocker> __Game_Vehicles_Blocker_RW_ComponentLookup;
            public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;
            public BufferLookup<LoadingResources> __Game_Vehicles_LoadingResources_RW_BufferLookup;
            public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RW_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(true);
                __Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(true);
                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                __Game_Routes_CurrentRoute_RO_ComponentTypeHandle =
                    state.GetComponentTypeHandle<CurrentRoute>(true);
                __Game_Vehicles_CargoTransport_RW_ComponentTypeHandle =
                    state.GetComponentTypeHandle<Game.Vehicles.CargoTransport>();
                __Game_Vehicles_PublicTransport_RW_ComponentTypeHandle =
                    state.GetComponentTypeHandle<PublicTransport>();
                __Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
                __Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
                __Game_Vehicles_Odometer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>();
                __Game_Vehicles_LayoutElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>();
                __Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle =
                    state.GetBufferTypeHandle<TrainNavigationLane>();
                __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle =
                    state.GetBufferTypeHandle<ServiceDispatch>();
                __EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
                __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
                __Game_Objects_SpawnLocation_RO_ComponentLookup =
                    state.GetComponentLookup<Game.Objects.SpawnLocation>(true);
                __Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(true);
                __Game_Pathfind_PathInformation_RO_ComponentLookup =
                    state.GetComponentLookup<PathInformation>(true);
                __Game_Simulation_TransportVehicleRequest_RO_ComponentLookup =
                    state.GetComponentLookup<TransportVehicleRequest>(true);
                __Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(true);
                __Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(true);
                __Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(true);
                __Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(true);
                __Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(true);
                __Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(true);
                __Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(true);
                __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup =
                    state.GetComponentLookup<PublicTransportVehicleData>(true);
                __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup =
                    state.GetComponentLookup<CargoTransportVehicleData>(true);
                __Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(true);
                __Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(true);
                __Game_Routes_BoardingVehicle_RO_ComponentLookup = state.GetComponentLookup<BoardingVehicle>(true);
                __Game_Routes_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Color>(true);
                __Game_Companies_StorageCompany_RO_ComponentLookup =
                    state.GetComponentLookup<Game.Companies.StorageCompany>(true);
                __Game_Buildings_TransportStation_RO_ComponentLookup =
                    state.GetComponentLookup<Game.Buildings.TransportStation>(true);
                __Game_Buildings_TransportDepot_RO_ComponentLookup =
                    state.GetComponentLookup<Game.Buildings.TransportDepot>(true);
                __Game_Creatures_CurrentVehicle_RO_ComponentLookup =
                    state.GetComponentLookup<CurrentVehicle>(true);
                __Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(true);
                __Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(true);
                __Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(true);
                __Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(true);
                __Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(true);
                __Game_Vehicles_Train_RW_ComponentLookup = state.GetComponentLookup<Train>();
                __Game_Vehicles_TrainCurrentLane_RW_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>();
                __Game_Vehicles_TrainNavigation_RW_ComponentLookup = state.GetComponentLookup<TrainNavigation>();
                __Game_Vehicles_Blocker_RW_ComponentLookup = state.GetComponentLookup<Blocker>();
                __Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
                __Game_Vehicles_LoadingResources_RW_BufferLookup = state.GetBufferLookup<LoadingResources>();
                __Game_Vehicles_LayoutElement_RW_BufferLookup = state.GetBufferLookup<LayoutElement>();
            }
        }
    }
}