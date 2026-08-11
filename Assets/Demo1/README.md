# Demo 1.0 implementation

The playable entry is `Assets/Prototype/BaseScene/Scenes/BaseCommand.unity`. The 501st base, sortie preparation, flight, combat, return and servicing all run on one persistent world map and one camera; sortie never replaces the theatre with a smaller operational scene. `Assets/Demo1/Scenes/Demo1.unity` remains an isolated developer scene for combat iteration.

## Real-scale operational map and historical movement

- The English Channel operational map uses a defined prototype extent of `560 x 315 km`; one simulation/world unit represents one kilometre.
- Base Command's Folkestone anchor and all scenario spawn/patrol coordinates use the same kilometre coordinate system.
- The initial camera fits the complete `560 x 315 km` theatre. The base is a permanent, invulnerable world landmark at `(187.6, 0, 100.8)`; its readiness UI is only an overlay.
- Enemy AI begins on scene load, including while every witch is still at base. Witches move through `Standby`, `Active`, `Returning`, `Servicing` and `Lost` deployment states. Return is a separate order, can be intercepted, and resumes after combat; landing within 2 km starts a 20-second full-service turnaround before another sortie is allowed. A limited tactical supply call can extend a sortie, but never repairs health or replaces base service.
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

- Global combat, vision, AI, lock and suppression tuning lives in `Assets/Demo1/Resources/Configs/Demo1Balance.asset`.
- Every scenario unit has an independent `DemoUnitConfig` asset under `Assets/Demo1/Resources/Configs/Units`. The asset contains identity, team, role, spawn position, movement, weapon, ammunition, armour, active-ability, vision, persistent-intelligence and individual-AI values.
- Runtime models clone values from these assets, so health, cooldowns and other battle changes never write back into project assets.
- The controller discovers the default assets through `Resources` without scene rebinding. Inspector-assigned configs override the default resource set, while the previous code defaults remain only as a missing-asset safety fallback.
- New configs can be created through `Assets > Create > SWRTS > Demo1 > Balance Config` and `Unit Config`.

### Player witch balance pass

After the doubled values proved too strong, HP, attack, defense, magic, shield and magic recovery were reduced by 33% (multiplied by 0.67 and rounded to whole numbers). Vision and historical movement values remain unchanged; weapon cadence, ammunition and active mechanisms now follow the individual-combat specification:

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
- Strategic map lines use fixed screen-pixel widths. Routes remain at 3 px; the background grid uses 2 px at 55% opacity; selection, merged detection, attack range, ability, target lock, remote-strike and projectile lines use at least 4 px and 85% opacity. Merged detection contours interpolate their signed-distance boundary and use rounded joins to avoid grid-step aliasing.
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
- Right click a discovered enemy, or `A` then left click it: clear the current route/loiter/hover order, lock the target, pursue it and fire whenever it enters attack range.
- **Auto** toggles automatic acquisition of the nearest identified enemy inside the unit's current effective attack range. **Stop attack** disables automatic acquisition and clears the current lock.
- `H`: clear the current target and order selected witches to return to Folkestone.
- `V` / **Hover**: decelerate selected witches to a stationary hover; use it again to resume the capsule loiter route. Moving, returning or manually designating an attack target exits hover.
- `G` / **Supply drop**: enter placement mode, then left click within 140 units of Folkestone to call a supply package. Each battle has three packages; delivery takes eight seconds and calls share a 20-second cooldown.
- `R` / **Resupply**: send selected active witches to the nearest active supply zone. They approach, hover and replenish in ammo, magic, then shield order. Any move, attack, return or hover-exit order cancels resupply.
- `S` / **Skill**: use the single selected witch's active mechanism. Sakamoto and Perrine resolve immediately; Miyafuji requires a friendly map-unit target. Lynette's fire-control solution is passive.
- `Space`: pause/resume simulation. Camera, selection and orders remain available while paused.
- `F`: focus the selected units. WASD/arrow keys, edge scrolling, middle drag and wheel control the camera.
- `Ctrl+1..9`: save a control group; `1..9`: recall and focus it.

## Individual target-lock combat experiment

- Combat never leaves the operational map and does not create a battle container, formation, reserve queue, reinforcement state, retreat timer, battle bubble or separate battle panel.
- Every active unit owns its current target, target order type, last-known target position, lock quality, ammunition, reload, suppression, current velocity, attack cooldown and auto-attack stance.
- A manual attack order always clears the previous move, loiter and hover state. It keeps pursuing the target while observable, fires as soon as it enters attack range and settles at 75% of the current effective range. After contact is lost it flies to the last-known position and then clears the order.
- Movement and firing are independent. A normal move order keeps the current lock, allows automatic target acquisition and preserves the requested route; any locked target inside attack range can be fired on without stopping movement. The move order takes priority over automatic pursuit.
- With auto attack enabled, a unit acquires the nearest identified enemy inside its current effective attack range. There is no independent warning radius; suppression and Lynette's prepared fire-control state alter the same acquisition and firing boundary.
- Movement uses eight-second acceleration to maximum speed and `60 degrees/s x Mobility` turning. A 180-degree turn limits speed to 30%. Entering the 1.8-unit arrival radius, or crossing it during one simulation step, completes a destination or loiter waypoint without snapping the unit to its center. Arrival creates a 10 x 5 capsule loiter route with 2.5-unit end radii and two straight segments aligned to the arrival heading. Movement and ordinary fire remain independent.
- Lock quality grows from intelligence, turning and suppression, must reach 25 before firing, and decays by 35 per second without observation or outside effective attack range. Distance within range affects neither lock growth nor accuracy; accuracy applies lock and suppression modifiers before a separate evasion roll.
- Standard witch machine guns use an 8/32 magazine/reserve load and a three-second reload. Empty reserves automatically order a return; the existing 20-second base service restores health, magic, shield and ammunition.
- A tactical supply package has an eight-second inbound delay, a 35-second active window, six-unit service radius and 180 shared supply points. Transfer requires a stationary hover and two seconds without being hit; the unit cannot acquire, lock or fire while receiving supplies. Ammo, magic and shield transfer at 8, 12 and 10 per second with costs of 1, 0.5 and 0.75 supply points respectively. Supply does not restore health, clear suppression, reset cooldowns or bypass normal magazine reloads.
- Enemy armour uses the 100% / 35% / 10% penetration tiers. Core marks last six seconds, halve effective armour and apply the non-stacking 2.4x core multiplier. Suppression only applies linear combat penalties and never clears orders or forces retreat.
- This branch implements the target specification in Feishu `Demo1 战斗 个体` revision 399 and intentionally does not create battle instances, formations or a separate battle panel.

## Witch active mechanisms

- Active mechanisms are data-driven through `DemoSpecialAbility` and per-unit serialized parameters; the simulation does not branch on display names.
- Sakamoto's **Magic Eye Search** spends 30 magic to assess and core-mark enemies in a 45-degree, 36-unit forward sector for six seconds.
- Miyafuji's **Heal** channels on another ally within six units for three seconds, restoring 12% maximum health per second at 15 magic per second. She moves at half speed and cannot fire while channeling; range, magic or a new attack order interrupts it.
- Lynette has a 5/20 single-shot anti-armour weapon with a 2.2-second interval. **Fire-control Solution** is passive: after three seconds of stable hover, every normal attack against an assessed or core-marked target can reach 48 units, has at least 85% accuracy, penetration 32 and double damage. Moving cancels the prepared state.
- Sanya uses a 1/24 rocket load with a five-second reload. Her radar and attack range are both 72 units. Rockets travel at 12 units/s, turn at 120 degrees/s for up to ten seconds, track their target after launch and resolve the 2.5-unit explosion only on contact.
- Perrine's **Lightning Strike** spends 40 magic to damage enemies within five units, treats armour at half value and adds 35 suppression.
- These mechanisms replace the old Sakamoto core-discovery aura, Miyafuji shield aura, support pulse and Lynette critical trait.

## Witch vision and player intelligence prototype

- Witch vision is independent from combat role. The original four witches are ordinary witches with a 100-degree forward visual sector and no circular radar.
- Sanya V. Litvyak is the fifth player unit. She is a night witch with a 72-unit, 360-degree circular detection area and no visual sector.
- Every active witch uses her effective attack range as the forced-reveal boundary. Enemies entering it immediately become identified, but do not gain assessment progress from range reveal alone. Suppression therefore reduces attack, auto-targeting and forced-reveal reach together.
- Moving or engaging turns an ordinary witch's sector toward her destination or target. Night radar and ordinary vision use the same cyan line style, and detection shapes from all selected active witches are rendered as one union. Attack circles remain per unit and are the only visual indicator for attack, auto-targeting and forced reveal; disconnected detection regions remain separate contours.
- A newly observed enemy starts as an unknown contact, becomes identified after 0.5 seconds of observation, and becomes assessed after another 1.5 seconds. Only assessed intelligence exposes health on the strategic map.
- When observation is lost, the enemy marker freezes at its last known position. Intelligence degrades from assessed to identified after 3 seconds, to contact after 7 seconds, and disappears after 15 seconds.
- Stale contacts cannot be directly engaged, but their last known area remains a valid movement or remote-strike destination. Mission-known fixed objectives keep persistent identified intelligence.
- Strategic unit models and world-space labels stay hidden while the entire witch roster is at base. Standby and servicing witches never render on the theatre map; mobile interception enemies never receive mission-known persistent intelligence.
- These vision shapes, durations and sharing rules are tracked in Feishu `Demo1 战斗 个体` revision 395.

## Independent enemy AI prototype

- Enemy decisions run at a 0.5-second interval. Every mobile enemy owns its target, last-known position and state; there is no director, shared blackboard, contact sharing or coordinated reinforcement logic.
- The scout cycles through a configured patrol route, locks the nearest player inside its own circular vision, pursues and attacks in range, and investigates only its own last-known contact position after losing sight. Below 30% health it breaks its lock and returns along its route.
- Guards hold individual home positions, pursue and directly attack the nearest player visible inside their own vision and 18-unit home leash, then return to their own post after losing the target.
- Enemies never merge decisions or enter a shared battle; overlapping fights are simply several independent target locks in the same world area.
- The fortress remains fixed and reactive. It has no strategic AI layer.
- These behaviors and values are code-only prototype assumptions because Feishu revision 6 explicitly leaves AI engagement and retreat decisions for later design.

## Configurable prototype assumptions

The individual-combat specification revision 395 defines the current prototype formulas. Demo defaults are serialized in the balance and unit ScriptableObject assets described above; `Demo1Balance` and `DemoUnitStats` remain their runtime value types:

- hit, evasion, penetration, armour, core, shield and HP resolve in the documented order;
- shield absorption consumes one shield capacity and 0.55 magic per absorbed damage;
- pathing remains direct on the unobstructed prototype map, but acceleration, turning and loitering are continuous simulation states;
- enemy AI uses the independent scout/guard state machines described above; player operational choices remain manual;
- orders may be queued while paused;
- victory is objective-driven per level: interception requires every raider to be destroyed, while both assault missions require their fortress to be destroyed; defeat occurs only after every witch in the base roster is lost, so standby, returning and servicing witches prevent premature defeat.

These are implementation defaults, not new game-design source of truth. They can be replaced without changing the simulation API when the corresponding Feishu sections are approved.
