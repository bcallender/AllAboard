# Hook splices

This is the fragile, judgement-dependent half of a migration — the part a static script
can't own, because CO reshapes this code on most updates. Find each hook by its *role*, not
by matching last version's text, then apply the documented transform.

There is exactly one hook per system, both inside the job's `StopBoarding` method.

## The intent (why the hook exists)

Vanilla ends boarding only when every assigned passenger has the `Ready` flag, OR when its
own hardcoded dwell cap trips (`m_SimulationFrameIndex >= departureFrame + 1800`, ≈9.9 min).
All Aboard! replaces that with a **configurable** cap via `PublicTransportBoardingHelper`,
which reports "ready" once the boarding has run longer than the per-mode slider
(`TrainMaxAllowedMinutesLate` / `BusMaxAllowedMinutesLate`) regardless of the `Ready` flags.

**We deliberately excise vanilla's 1800-frame cap entirely** so the slider is the sole
authority — otherwise vanilla's 9.9-min cap would override a user who set the slider higher.
Excision means: delete the `num`/`flagN` cap locals, drop the `|| flagN` term from the
`m_MaxBoardingDistance = math.select(...)` line, and remove the `if (!flagN)` guard around the
ready-check. (`flagN` is whatever the decompiler named it — `flag2` in 1.5.9 car, `flag3` in
1.5.9 train. Don't trust the number; trust the `+ 1800` shape.)

## Finding the hook in a fresh decomp

1. Jump to `private bool StopBoarding(...)` (the script prints its line number).
2. Inside the `if (!forcedStop) { ... }` block, find the passenger-readiness check — the loop
   (or method call) that inspects `CreatureVehicleFlags.Ready`. That is the hook site.
3. Just above it sits vanilla's dwell-cap: a `uint num = math.max(...DepartureFrame...)` and a
   `bool flagN = num != 0 && m_SimulationFrameIndex >= num + 1800;`, with `flagN` also folded
   into the `math.select` for `m_MaxBoardingDistance` and gating the ready-check.

If the surrounding code looks different from the examples below, that's expected — read what's
actually there and preserve all of it except the cap and the ready-check.

## Car — `PatchedTransportCarAISystem`

The car ready-check is an inline `for` loop. Block-swap it; the car has no helper method.

**Vanilla 1.5.9 (the part to change), inside `if (!forcedStop)`:**
```csharp
uint num = math.max(cargoTransport.m_DepartureFrame, publicTransport.m_DepartureFrame);
bool flag2 = num != 0 && m_SimulationFrameIndex >= num + 1800;
publicTransport.m_MaxBoardingDistance = math.select(publicTransport.m_MinWaitingDistance + 1f, float.MaxValue, publicTransport.m_MinWaitingDistance == float.MaxValue || publicTransport.m_MinWaitingDistance == 0f || flag2);
publicTransport.m_MinWaitingDistance = float.MaxValue;
if ((flag || ...) && (...))
{
    return false;
}
if (!flag2 && passengers.IsCreated)
{
    for (int i = 0; i < passengers.Length; i++)
    {
        Entity passenger = passengers[i].m_Passenger;
        if (m_CurrentVehicleData.TryGetComponent(passenger, out var componentData3) && (componentData3.m_Flags & CreatureVehicleFlags.Ready) == 0)
        {
            return false;
        }
    }
}
```

**After splice** (cap excised, helper call; leave the unchanged `if ((flag || ...))` block as-is):
```csharp
publicTransport.m_MaxBoardingDistance = math.select(publicTransport.m_MinWaitingDistance + 1f, float.MaxValue, publicTransport.m_MinWaitingDistance == float.MaxValue || publicTransport.m_MinWaitingDistance == 0f);
publicTransport.m_MinWaitingDistance = float.MaxValue;
if ((flag || ...) && (...))
{
    return false;
}
if (passengers.IsCreated)
{
    bool boardingComplete = PublicTransportBoardingHelper.ArePassengersReady(passengers, m_CurrentVehicleData, publicTransport, PublicTransportBoardingHelper.TransportFamily.Bus, m_SimulationFrameIndex);
    if (!boardingComplete)
    {
        return false;
    }
}
```

Note `TransportFamily.Bus` (the car system covers buses) and `m_CurrentVehicleData` — the
`ComponentLookup<CurrentVehicle>` field the vanilla loop already used. If CO renames that
field, use the new name; the helper just needs the current-vehicle lookup.

## Train — `PatchedTransportTrainAISystem`

Trains are multi-car. In 1.5.9 vanilla already factors the per-vehicle check into a private
`ArePassengersReady(Entity)` and iterates the `layout` in `StopBoarding`. The splice has two
parts: (a) swap the inline layout loop for a call to our layout-aware overload, and (b) replace
the vanilla per-vehicle method with that overload.

### (a) In `StopBoarding`

**Vanilla 1.5.9:**
```csharp
uint num = math.max(cargoTransport.m_DepartureFrame, publicTransport.m_DepartureFrame);
bool flag3 = num != 0 && m_SimulationFrameIndex >= num + 1800;
publicTransport.m_MaxBoardingDistance = math.select(... || flag3);
publicTransport.m_MinWaitingDistance = float.MaxValue;
if (flag2 && (...)) { return false; }
if (!flag3)
{
    if (layout.Length != 0)
    {
        for (int i = 0; i < layout.Length; i++)
        {
            if (!ArePassengersReady(layout[i].m_Vehicle)) { return false; }
        }
    }
    else if (!ArePassengersReady(vehicleEntity)) { return false; }
}
```
(Here `flag2` is the unrelated "is this the boarding vehicle" flag — keep it. `flag3` is the
dwell cap — excise it.)

**After splice:**
```csharp
publicTransport.m_MaxBoardingDistance = math.select(publicTransport.m_MinWaitingDistance + 1f, float.MaxValue, publicTransport.m_MinWaitingDistance == float.MaxValue || publicTransport.m_MinWaitingDistance == 0f);
publicTransport.m_MinWaitingDistance = float.MaxValue;
if (flag2 && (...)) { return false; }
bool boardingComplete = ArePassengersReady(vehicleEntity, ref layout, publicTransport);
if (!boardingComplete)
{
    return false;
}
```

### (b) Replace the private ready method

Replace vanilla's `private bool ArePassengersReady(Entity vehicleEntity)` with this overload.
It mirrors vanilla's layout iteration but defers each car's decision to the helper:
```csharp
private bool ArePassengersReady(Entity vehicleEntity, ref DynamicBuffer<LayoutElement> layout, PublicTransport publicTransport)
{
    bool boardingComplete = true;
    if (layout.Length != 0)
    {
        for (int i = 0; i < layout.Length; i++)
        {
            Entity layoutVehicle = layout[i].m_Vehicle;
            if (!m_Passengers.HasBuffer(layoutVehicle))
            {
                continue;
            }
            DynamicBuffer<Passenger> layoutVehiclePassengers = m_Passengers[layoutVehicle];
            if (!PublicTransportBoardingHelper.ArePassengersReady(layoutVehiclePassengers, m_CurrentVehicleData, publicTransport, PublicTransportBoardingHelper.TransportFamily.Train, m_SimulationFrameIndex))
            {
                boardingComplete = false;
                break;
            }
        }
    }
    else
    {
        boardingComplete = PublicTransportBoardingHelper.ArePassengersReady(m_Passengers[vehicleEntity], m_CurrentVehicleData, publicTransport, PublicTransportBoardingHelper.TransportFamily.Train, m_SimulationFrameIndex);
    }
    return boardingComplete;
}
```

The field names this leans on (`m_Passengers`, `m_CurrentVehicleData`, `m_SimulationFrameIndex`,
and the `layout` / `LayoutElement` types) are job-struct fields that have been stable. Verify
each still exists in the new decomp before trusting the paste; if vanilla dropped its own
single-arg `ArePassengersReady(Entity)`, then there's nothing to replace — just add this method
and wire the call site in (a).

## Verifying a splice (before the build)

From the directory holding the spliced files:
```
grep -c "+ 1800" Patched*.cs          # expect 0,0 — the vanilla cap is gone
grep -c "PublicTransportBoardingHelper.ArePassengersReady" Patched*.cs   # car 1, train 2
```
Then check no dwell-cap local is left dangling inside `StopBoarding` (other methods may have
their own unrelated `flag2`/`flag3` — those are fine; only the `+ 1800` one is yours to remove).

The real gate is the compile: `dotnet build`. See SKILL.md.
