Have you ever encountered trains/subways/buses/trams that refuse to stop boarding? After a city grew beyond 250k Cims, this was a huge issue for me. After trying to fix it in-game so many different ways, I decided to attempt to fix it with a Mod instead!

tl;dr: This mod adds a hard cap on the Dwell Time of public transport vehicles, simulating a conductor calling "All Aboard!" when the train is scheduled to leave and closing the doors. This is an ALPHA version of this mod.

The default behavior has trains (and all other public transport):

  - Calculate a departureFrame (based on factors like unbunching, vehicle size, etc.)
  - Determine what cims on the platform or stop will be boarding in the cycle, and start boarding
  - Wait until the departureFrame, then make the Cims start running instead of walking
  - (the problematic part) Wait until every Cim that is predetermined to board each vehicle has entered that vehicle.

Imagine if your train never left the station until the conductor stamped every ticket sold for the train -- even if people had bought tickets and left the station!

This logic makes some sense if pathfinding is perfect, but some edge cases sometimes leave trains/buses stuck at stops with high waiting counts:

  If a cim is a leader of a group (with a child or pet in tow), the train waits until the entire group has boarded the train when checking the leader. this is problematic, as sometimes the child/pet is several blocks away, or is actually at home and still mistakenly attached to the cim.
  When the platform is very crowded, pathfinding gets a lot slower, so Cims can get temporarily stuck waiting for a path to be a path to be drawn.

A transit vehicle's Dwell Time is the amount of time it spends embarking/disembarking passengers at each stop. This mod sets an upper limit (Maximum Dwell Delay) on the amount of time beyond a transit vehicle's scheduled departureFrame and when it will actually leave a station, measured in (real-world) seconds. Right now, this is tuned to 30 seconds, with seconds calculated using a simulation frame rate of 30 frames per second. Hopefully in the future I can make this customizable, but no promises. If the vehicle is still "stuck" boarding, even after the maximum dwell delay, the vehicle will simply close its doors and depart. If a group leader has boarded, and other group members have not, they will be 'teleported' to the vehicle (a No Child Left Behind policy, if you will). If a random Cim on the platform did not board in time, they will be returned to the platform for the next scheduled bus/train/tram/subway.

This means you might se passengers "board" the train, only to have the number waiting at the platform jump back up. This is normal. Eventually, all Cims will be picked up, they just might have to wait for the next train/bus, just like the rest of us. This also helps trains/buses stay unbunched!

### Caveats:

  This does not yet work with Taxis, as they use a separate AI system
  This will be an UNSTABLE mod! It overwrites core game systems, and WILL potentially break with game updates! It is save-game safe, so it can be disabled at any time. Use at your own risk!

### Notes:

  This mod has been tested on cities up to 2.1M Cims, and seems to scale well.
  It has very good synergy with Transit Capacity Multiplier mod, as the larger vehicles will no longer have a higher chance of getting stuck boarding a rogue pet or child.
  Hopefully CO will add a similar feature, since this was a pretty easy fix, it just happens to be in a very inconvenient place for modders to get at safely.
  This is my first mod, so constructive criticism is appreciated, but hostility is not. I code for a living full time, and this is a passion project for a game I love, and would like to keep loving.
  Please report any bugs or incompatible mods in the linked Discord channel. Provide Log Files if possible. I can't fix what I don't have evidence for. (Invite Link: https://discord.gg/HTav7ARPs2)

### Incompatible With:

  No (known) incompatibilities (I play with > 60 other code mods, and this was tested by a few users in Discord), but will not be compatible with other mods that modify TransportCarAISystem and TransportTrainAISystem. It is unlikely other mods will do so, since these classes are burst compiled.

### Future Work:

  Trying to find a way of accomplishing this without replacing two core game systems wholesale -- this approach will be very fragile and likely break with game updates.
  Making the Dwell Delay and Frame Per Second configurable in settings. This is quite difficult due to the core classes under modification being Burst Compiled, and wanting to preserve the performance benefit that offers to such a heavy part of the game.

### Thanks/Credit:

  - Thanks to @darolas @domoniQC, @elGendo87, @DaRonk, @Liam and @CC-2A for testing!
  - Thanks to @domoniQC for before/after screenshots for the mod description!
  - Thanks to @RightToRepairAdvocate for the name suggestion!
  - Last but not least, thanks and credit to @Wayz for contributing the core logic that "forces" the transit vehicle to reject the boarding cims!
