# Demo 1.0 implementation

The playable entry is `Assets/Prototype/BaseScene/Scenes/BaseCommand.unity`. The 501st base, sortie preparation, flight, combat, return and servicing all run on one persistent world map and one camera; sortie never replaces the theatre with a smaller operational scene. `Assets/Demo1/Scenes/Demo1.unity` remains an isolated developer scene for combat iteration.

## Real-scale operational map and historical movement

- The English Channel operational map uses a defined prototype extent of `560 x 315 km`; one simulation/world unit represents one kilometre.
- Base Command's Folkestone anchor and all scenario spawn/patrol coordinates use the same kilometre coordinate system.
- The initial camera fits the complete `560 x 315 km` theatre. The base is a permanent, invulnerable world landmark at `(187.6, 0, 100.8)`; its readiness UI is only an overlay.
- Enemy AI begins on scene load, including while every witch is still at base. Witches move through `Standby`, `Active`, `Returning`, `Servicing` and `Lost` deployment states. Return is a separate order, can be intercepted, and resumes after combat; landing within 2 km starts a 20-second full-service turnaround before another sortie is allowed.
- Aircraft maximum level speed is converted with `move speed = historical km/h / 3600 x 12`. The `12x` strategic time compression keeps a real-distance theatre playable while preserving the historical speed ratios between witches.
- Historical metadata lives on each `DemoUnitConfig` ScriptableObject, so an individual witch's model, source basis or speed can be tuned without code changes.

| Witch | Striker model basis | Historical reference | Runtime speed (km-map units/s) |
| --- | --- | ---: | ---: |
| Miyafuji | A6M3a / A6M3 Model 22 | 541 km/h | 1.803 |
| Sakamoto | N1K5-J Shiden Kai 5 | 595 km/h | 1.983 |
| Lynette | Spitfire F Mk 22 | 730 km/h | 2.433 |
| Perrine | Arsenal VG.39bis | 625 km/h | 2.083 |
| Sanya | Mikoyan-Gurevich I-225 | 726 km/h | 2.420 |

The official *Strike Witches* character pages establish the Striker-model mapping. N1K5-J did not complete flight trials, so Sakamoto uses the Smithsonian-documented N1K2-J value as a conservative family reference. VG.39bis was not completed, so Perrine uses the VG.39 prototype figure. I-225 and VG.39 figures are prototype/test values rather than production-service ratings; these caveats are serialized beside the affected units.

References: [official character/Striker mapping](https://w-witch.jp/strike_witches-rtb/character/), [Smithsonian N1K2-J collection record](https://airandspace.si.edu/collection-objects/kawanishi-n1k2-j-shiden-kai-george/nasm_A19600333000), [RAF Museum Spitfire F24 collection record](https://www.rafmuseum.org.uk/research/collections/supermarine-spitfire-f24/).

This real-scale conversion is a user-requested code prototype. It follows the Feishu Demo 1.0 requirement that witches have independent position, route and speed, but does not write new balance values back to Feishu revision 6.

## ScriptableObject configuration

- Global combat, vision, AI and trait tuning lives in `Assets/Demo1/Resources/Configs/Demo1Balance.asset`.
- Every scenario unit has an independent `DemoUnitConfig` asset under `Assets/Demo1/Resources/Configs/Units`. The asset contains identity, team, role, spawn position, all combat/vision stats, traits, persistent intelligence and individual AI setup.
- Runtime models clone values from these assets, so health, cooldowns and other battle changes never write back into project assets.
- The controller discovers the default assets through `Resources` without scene rebinding. Inspector-assigned configs override the default resource set, while the previous code defaults remain only as a missing-asset safety fallback.
- New configs can be created through `Assets > Create > SWRTS > Demo1 > Balance Config` and `Unit Config`.

### Player witch balance pass

After the doubled values proved too strong, HP, attack, defense, magic, shield and magic recovery were reduced by 33% (multiplied by 0.67 and rounded to whole numbers). Attack intervals, critical rates, traits, vision and movement remain unchanged:

| Witch | HP | Attack | Defense | Magic | Shield | Magic recovery |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Miyafuji | 181 | 27 | 11 | 181 | 96 | 9 |
| Sakamoto | 221 | 44 | 12 | 121 | 74 | 5 |
| Lynette | 174 | 36 | 11 | 121 | 67 | 5 |
| Perrine | 188 | 38 | 12 | 121 | 74 | 5 |
| Sanya | 181 | 35 | 11 | 168 | 91 | 7 |

These values remain code-prototype balance and are not additions to Feishu revision 6.

### Interface presentation pass

- The strategic HUD uses a unified dark tactical palette with cyan player, red enemy, amber warning and green success semantics. Mission, commands, feedback, selected units and recent events are visually separated instead of relying on Unity's default controls.
- Strategic map lines use fixed screen-pixel widths. Selection, vision, warning radius, attack range, target lock, route, remote-strike and grid lines remain legible across the complete camera zoom range instead of shrinking with world scale.
- Selected-unit cards prioritize name, activity and the three live resources. Single selection keeps the detailed character panel on the right; multi-selection remains summarized on the left.
- Selected units show an amber warning-radius circle, a red attack-range circle and a red line to the current locked target or its last known position.

This visual pass implements the information requirements of Feishu revision 6 without defining a final production art direction.

### Level selector prototype

- A compact selector is pinned to the top edge of the Game View and remains above the strategic and live-battle interfaces.
- Open the dropdown, choose a level and press **Load**. Choosing an item alone does not interrupt the current simulation; loading the current item acts as a clean reset.
- Level identity, mission objective, outcome copy, unit roster, team spawn offsets and enemy health/attack multipliers are stored in `DemoLevelConfig` ScriptableObjects under `Assets/Demo1/Resources/Configs/Levels`.
- **English Channel - Raid Interception** deploys three scouts and a mobile escort from the French coast. All four use independent northbound patrol routes into the Channel; the mission ends only after the entire raid is destroyed.
- **French Coast - Forward Assault** uses one scout, two guards and a fixed fortress. The player must cross the Channel, break the coastal screen and destroy the mission-known fortress.
- **French Interior - High-pressure Nest Raid** moves the fortress group farther inland, adds a third guard and applies 1.25x enemy health plus 1.1x attack. Destroying the reinforced fortress completes the mission even if escorts remain.
- Level selection is deliberately session-local: it does not add unlocking, save data, campaign progression or a separate front-end menu.
- Feishu revision 6 excludes out-of-battle level selection from Demo 1.0. This selector is therefore a user-requested code prototype and has not been written back to the design document.

## Controls

- Click the 501 base marker to open the readiness overlay, select standby witches and launch them into the existing theatre.
- Left click / drag: select one or several witches. Shift modifies the current selection.
- Single-selecting a witch opens a live character detail panel on the right. It hides for multi-selection.
- Right click ground: give every selected witch an independent move order into the same small destination area.
- Right click a discovered enemy, or `A` then left click it: lock the target, pursue it and fire whenever it enters attack range.
- **Auto** toggles automatic acquisition of the nearest identified enemy inside the unit's warning radius. **Stop attack** disables automatic acquisition and clears the current lock.
- `H`: clear the current target and order selected witches to return to Folkestone.
- `B` then left click: use an area-level remote strike if the selected unit has that capability. No witch in the current scenario has it after Lynette's rework.
- `Space`: pause/resume simulation. Camera, selection and orders remain available while paused.
- `F`: focus the selected units. WASD/arrow keys, edge scrolling, middle drag and wheel control the camera.
- `Ctrl+1..9`: save a control group; `1..9`: recall and focus it.

## Individual target-lock combat experiment

- Combat never leaves the operational map and does not create a battle container, formation, reserve queue, reinforcement state, retreat timer, battle bubble or separate battle panel.
- Every active unit owns its current target, target order type, last-known target position, warning radius, attack range, attack cooldown and auto-attack stance.
- A manual attack order keeps pursuing the target while it is observable; after contact is lost the attacker flies to the last-known position and then clears the order. A normal move order cancels the lock.
- With auto attack enabled, a unit acquires the nearest identified enemy inside its warning radius. It fires only inside its independent attack range and automatically chooses another eligible target after the current one is destroyed.
- Support pulses and Sakamoto/Miyafuji trait auras are distance based. Their `SupportRadius` is serialized per character; providers affect themselves as well as nearby active allies.
- Artillery keeps the every-third-attack calibrated salvo, scouts keep their damage-amplifying mark and fortresses keep the low-health emergency barrage. These resolve directly between individual world units.
- This branch intentionally deviates from the battle-instance flow in Feishu revision 6 so the team can compare an individual-combat prototype against the formation-combat branch. No Feishu revision was changed.

## Witch trait prototype

- Traits are passive, data-driven flags on witch stats. Multiple traits can be combined later without adding character-name checks to combat resolution.
- Sakamoto's **Magic Eye Command** increases base core discovery by 100% for herself and active allies within her 12 km support radius.
- Miyafuji's **Guardian Heart** increases shield absorption efficiency by 15% for herself and active allies within her 12 km support radius. Her support pulse uses the same radius.
- An aura stops contributing when its holder is not active or is destroyed. Multiple sources stack additively.
- Lynette's **Precision Shooter** adds 18 percentage points of critical chance (12% to 30% in the scenario) and multiplies her attack interval by 1.375 (1.6s to 2.2s). She uses standard single-target attacks and has no map remote strike or every-third-shot artillery salvo.
- The character detail panel shows each unit's trait and effective critical chance, attack interval and core-discovery value. Adjusted values include their base value for comparison.
- These traits and values are code-only prototype assumptions and have not been written back to Feishu revision 6.

## Witch vision and player intelligence prototype

- Witch vision is independent from combat role. The original four witches are ordinary witches with a 100-degree forward visual sector and no circular radar.
- Sanya V. Litvyak is added as the fifth player unit. She is a night witch with a 48-unit, 360-degree circular detection area and no visual sector.
- Moving or engaging turns an ordinary witch's sector toward her destination or target. Selecting a witch shows only the area produced by her own vision type.
- A newly observed enemy starts as an unknown contact, becomes identified after 0.5 seconds of observation, and becomes assessed after another 1.5 seconds. Only assessed intelligence exposes health on the strategic map.
- When observation is lost, the enemy marker freezes at its last known position. Intelligence degrades from assessed to identified after 3 seconds, to contact after 7 seconds, and disappears after 15 seconds.
- Stale contacts cannot be directly engaged, but their last known area remains a valid movement or remote-strike destination. Mission-known fixed objectives keep persistent identified intelligence.
- Strategic unit models and world-space labels stay hidden while the entire witch roster is at base. Standby and servicing witches never render on the theatre map; mobile interception enemies never receive mission-known persistent intelligence.
- These vision shapes, durations and sharing rules are code-only prototype assumptions and have not been written back to Feishu revision 6.

## Independent enemy AI prototype

- Enemy decisions run at a 0.5-second interval. Every mobile enemy owns its target, last-known position and state; there is no director, shared blackboard, contact sharing or coordinated reinforcement logic.
- The scout cycles through a configured patrol route, locks the nearest player inside its own circular vision, pursues and attacks in range, and investigates only its own last-known contact position after losing sight. Below 30% health it breaks its lock and returns along its route.
- Guards hold individual home positions, pursue and directly attack the nearest player visible inside their own vision and 18-unit home leash, then return to their own post after losing the target.
- Enemies never merge decisions or enter a shared battle; overlapping fights are simply several independent target locks in the same world area.
- The fortress remains fixed and reactive. It has no strategic AI layer.
- These behaviors and values are code-only prototype assumptions because Feishu revision 6 explicitly leaves AI engagement and retreat decisions for later design.

## Configurable prototype assumptions

The Feishu specification revision 6 intentionally leaves formulas and concrete values open. Demo defaults are serialized in the balance and unit ScriptableObject assets described above; `Demo1Balance` and `DemoUnitStats` remain their runtime value types:

- defense is flat reduction with a minimum damage floor;
- a discovered core replaces (rather than stacks with) an ordinary critical multiplier;
- shield absorption consumes both shield and magic, with allied global shield bonuses improving efficiency;
- pathing is direct movement on the unobstructed prototype map; every witch owns her route and destination offset;
- enemy AI uses the independent scout/guard state machines described above; player operational choices remain manual;
- orders may be queued while paused;
- victory is objective-driven per level: interception requires every raider to be destroyed, while both assault missions require their fortress to be destroyed; defeat occurs only after every witch in the base roster is lost, so standby, returning and servicing witches prevent premature defeat.

These are implementation defaults, not new game-design source of truth. They can be replaced without changing the simulation API when the corresponding Feishu sections are approved.
