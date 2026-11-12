using System;
using System.IO;
using System.Text;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using PublicTransport = Game.Vehicles.PublicTransport;
using PublicTransportVehicleData = Game.Prefabs.PublicTransportVehicleData;

namespace AllAboard.System.Diagnostics
{
    public partial class BoardingDiagnosticsSystem : GameSystemBase
    {
        public const int UpdateIntervalFrames = 256;
        private const uint MinFramesPastDeparture = 1800;
        private const int MaxEventsPerUpdate = 200;

        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_VehicleQuery;
        private StreamWriter m_Writer;
        private string m_LogPath;
        private long m_TotalEvents;
        private bool m_FailedToOpen;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => UpdateIntervalFrames;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_VehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PublicTransport>(),
                    ComponentType.ReadOnly<Passenger>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });
            RequireForUpdate(m_VehicleQuery);
            // The log file is opened lazily on the first update where diagnostics is actually
            // enabled (see OnUpdate), so a disabled session never creates the file or writes a header.
        }

        protected override void OnDestroy()
        {
            try
            {
                if (m_Writer != null)
                {
                    AllAboard.log.InfoFormat("Boarding diagnostics captured {0} event(s) this session.", m_TotalEvents);
                    m_Writer.Flush();
                    m_Writer.Dispose();
                }
            }
            catch
            {
                // closing should never block teardown
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (AllAboard.m_AllAboardSettings == null || !AllAboard.m_AllAboardSettings.EnableDiagnostics)
                return;
            if (m_FailedToOpen) return;
            if (m_Writer == null)
            {
                OpenWriter();
                if (m_FailedToOpen || m_Writer == null) return;
            }

            var simFrame = m_SimulationSystem.frameIndex;
            m_VehicleQuery.CompleteDependency();
            var vehicles = m_VehicleQuery.ToEntityArray(Allocator.Temp);
            var events = 0;

            try
            {
                for (var v = 0; v < vehicles.Length && events < MaxEventsPerUpdate; v++)
                {
                    var vehicle = vehicles[v];
                    if (!EntityManager.HasComponent<PublicTransport>(vehicle)) continue;

                    var pt = EntityManager.GetComponentData<PublicTransport>(vehicle);
                    if ((pt.m_State & PublicTransportFlags.Boarding) == 0) continue;
                    if ((pt.m_State & (PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport)) != 0)
                        continue;
                    if (pt.m_DepartureFrame == 0) continue;
                    if (simFrame < pt.m_DepartureFrame + MinFramesPastDeparture) continue;

                    var passengers = EntityManager.GetBuffer<Passenger>(vehicle);
                    var totalPassengers = passengers.Length;
                    var stuckCount = CountStuck(passengers);
                    var transportTypeId = GetTransportType(vehicle);
                    var dwellFrames = simFrame - pt.m_DepartureFrame;

                    for (var i = 0; i < passengers.Length && events < MaxEventsPerUpdate; i++)
                    {
                        var passenger = passengers[i].m_Passenger;
                        if (!EntityManager.Exists(passenger)) continue;

                        if (!EntityManager.HasComponent<CurrentVehicle>(passenger))
                        {
                            EmitRow(simFrame, vehicle, passenger, "NO_CURRENT_VEHICLE",
                                transportTypeId, dwellFrames, totalPassengers, stuckCount,
                                isLeader: false, hasGroup: false,
                                groupSize: 0, gMissing: 0, gNotReady: 0,
                                entering: false, pathFlags: 0);
                            events++;
                            continue;
                        }

                        var cv = EntityManager.GetComponentData<CurrentVehicle>(passenger);
                        if ((cv.m_Flags & CreatureVehicleFlags.Ready) != 0) continue;

                        ClassifyAndEmit(simFrame, vehicle, passenger, cv,
                            transportTypeId, dwellFrames, totalPassengers, stuckCount);
                        events++;
                    }
                }
            }
            catch (Exception ex)
            {
                AllAboard.log.Warn($"BoardingDiagnostics pass aborted: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                vehicles.Dispose();
                if (events > 0)
                {
                    try
                    {
                        m_Writer.Flush();
                    }
                    catch
                    {
                        // ignore flush failure; next pass will try again
                    }
                    m_TotalEvents += events;
                }
            }
        }

        private int CountStuck(DynamicBuffer<Passenger> passengers)
        {
            var stuck = 0;
            for (var i = 0; i < passengers.Length; i++)
            {
                var p = passengers[i].m_Passenger;
                if (!EntityManager.Exists(p))
                {
                    stuck++;
                    continue;
                }
                if (!EntityManager.HasComponent<CurrentVehicle>(p))
                {
                    stuck++;
                    continue;
                }
                var cv = EntityManager.GetComponentData<CurrentVehicle>(p);
                if ((cv.m_Flags & CreatureVehicleFlags.Ready) == 0) stuck++;
            }
            return stuck;
        }

        private void ClassifyAndEmit(uint simFrame, Entity vehicle, Entity passenger, CurrentVehicle cv,
            int transportTypeId, uint dwellFrames, int totalPassengers, int stuckCount)
        {
            var entering = (cv.m_Flags & CreatureVehicleFlags.Entering) != 0;
            uint pathFlags = 0;
            if (EntityManager.HasComponent<PathOwner>(passenger))
                pathFlags = (uint)EntityManager.GetComponentData<PathOwner>(passenger).m_State;

            // Leader case: this passenger has its own group buffer.
            if (EntityManager.HasBuffer<GroupCreature>(passenger))
            {
                var members = EntityManager.GetBuffer<GroupCreature>(passenger);
                var missing = 0;
                var notReady = 0;
                for (var i = 0; i < members.Length; i++)
                {
                    var member = members[i].m_Creature;
                    if (!EntityManager.Exists(member))
                    {
                        missing++;
                        continue;
                    }
                    if (!EntityManager.HasComponent<CurrentVehicle>(member))
                    {
                        missing++;
                        continue;
                    }
                    var mcv = EntityManager.GetComponentData<CurrentVehicle>(member);
                    if ((mcv.m_Flags & CreatureVehicleFlags.Ready) == 0) notReady++;
                }

                var kind = missing > 0
                    ? "GROUP_LEADER_MEMBER_MISSING"
                    : notReady > 0
                        ? "GROUP_LEADER_MEMBER_NOT_READY"
                        : "GROUP_LEADER_FALSE_POSITIVE";

                EmitRow(simFrame, vehicle, passenger, kind, transportTypeId, dwellFrames,
                    totalPassengers, stuckCount,
                    isLeader: true, hasGroup: true,
                    groupSize: members.Length, gMissing: missing, gNotReady: notReady,
                    entering: entering, pathFlags: pathFlags);
                return;
            }

            // Follower case.
            if (EntityManager.HasComponent<GroupMember>(passenger))
            {
                var gm = EntityManager.GetComponentData<GroupMember>(passenger);
                var leaderHasCv = EntityManager.Exists(gm.m_Leader)
                                  && EntityManager.HasComponent<CurrentVehicle>(gm.m_Leader);
                var kind = leaderHasCv ? "FOLLOWER_STUCK" : "FOLLOWER_LEADER_MISSING";
                EmitRow(simFrame, vehicle, passenger, kind, transportTypeId, dwellFrames,
                    totalPassengers, stuckCount,
                    isLeader: false, hasGroup: true,
                    groupSize: 0, gMissing: 0, gNotReady: 0,
                    entering: entering, pathFlags: pathFlags);
                return;
            }

            // Solo case.
            string soloKind;
            if (!EntityManager.HasBuffer<PathElement>(passenger))
            {
                soloKind = "SOLO_NO_PATH_BUFFER";
            }
            else
            {
                var pathElements = EntityManager.GetBuffer<PathElement>(passenger);
                var startIndex = 0;
                if (EntityManager.HasComponent<PathOwner>(passenger))
                {
                    var po = EntityManager.GetComponentData<PathOwner>(passenger);
                    startIndex = Math.Max(0, po.m_ElementIndex);
                }
                var targetsVehicle = false;
                for (var i = startIndex; i < pathElements.Length; i++)
                {
                    if (pathElements[i].m_Target == vehicle)
                    {
                        targetsVehicle = true;
                        break;
                    }
                }
                soloKind = targetsVehicle ? "SOLO_PATH_TARGETING_VEHICLE" : "SOLO_PATH_NOT_TARGETING_VEHICLE";
            }

            EmitRow(simFrame, vehicle, passenger, soloKind, transportTypeId, dwellFrames,
                totalPassengers, stuckCount,
                isLeader: false, hasGroup: false,
                groupSize: 0, gMissing: 0, gNotReady: 0,
                entering: entering, pathFlags: pathFlags);
        }

        private int GetTransportType(Entity vehicle)
        {
            if (!EntityManager.HasComponent<PrefabRef>(vehicle)) return -1;
            var prefab = EntityManager.GetComponentData<PrefabRef>(vehicle).m_Prefab;
            if (!EntityManager.HasComponent<PublicTransportVehicleData>(prefab)) return -1;
            return (int)EntityManager.GetComponentData<PublicTransportVehicleData>(prefab).m_TransportType;
        }

        private void EmitRow(uint simFrame, Entity vehicle, Entity passenger, string kind,
            int transportType, uint dwellFrames, int totalPassengers, int stuckPassengers,
            bool isLeader, bool hasGroup, int groupSize, int gMissing, int gNotReady,
            bool entering, uint pathFlags)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"f\":").Append(simFrame);
            sb.Append(",\"v\":\"").Append(vehicle.Index).Append(':').Append(vehicle.Version).Append('"');
            sb.Append(",\"p\":\"").Append(passenger.Index).Append(':').Append(passenger.Version).Append('"');
            sb.Append(",\"k\":\"").Append(kind).Append('"');
            sb.Append(",\"vt\":").Append(transportType);
            sb.Append(",\"dwell\":").Append(dwellFrames);
            sb.Append(",\"np\":").Append(totalPassengers);
            sb.Append(",\"nstuck\":").Append(stuckPassengers);
            sb.Append(",\"isldr\":").Append(isLeader ? "true" : "false");
            sb.Append(",\"grp\":").Append(hasGroup ? "true" : "false");
            if (isLeader)
            {
                sb.Append(",\"gn\":").Append(groupSize);
                sb.Append(",\"gmis\":").Append(gMissing);
                sb.Append(",\"gnr\":").Append(gNotReady);
            }
            sb.Append(",\"ent\":").Append(entering ? "true" : "false");
            sb.Append(",\"pf\":").Append(pathFlags);
            sb.Append('}');

            try
            {
                m_Writer.WriteLine(sb.ToString());
            }
            catch (Exception ex)
            {
                AllAboard.log.Warn($"BoardingDiagnostics write failed; disabling: {ex.GetType().Name}: {ex.Message}");
                m_FailedToOpen = true;
            }
        }

        private void OpenWriter()
        {
            try
            {
                var dir = Path.Combine(UnityEngine.Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(dir);
                m_LogPath = Path.Combine(dir, "AllAboard.diagnostics.jsonl");
                m_Writer = new StreamWriter(m_LogPath, append: true, encoding: Encoding.UTF8);
                m_Writer.WriteLine(BuildHeaderRow());
                m_Writer.Flush();
                AllAboard.log.InfoFormat("Boarding diagnostics writing to {0}", m_LogPath);
            }
            catch (Exception ex)
            {
                m_FailedToOpen = true;
                AllAboard.log.Warn($"Could not open diagnostic log file: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string BuildHeaderRow()
        {
            // _meta rows are easy to filter with `jq 'select(._meta == null)'`.
            return "{\"_meta\":\"schema\",\"v\":\"1\",\"keys\":\"f=simFrame v=vehicleId p=passengerId k=kind vt=transportType dwell=framesPastDeparture np=totalPassengers nstuck=stuckPassengers isldr=isLeader grp=hasGroup gn=groupSize gmis=groupMissing gnr=groupNotReady ent=enteringFlag pf=pathOwnerStateFlags\"}";
        }
    }
}