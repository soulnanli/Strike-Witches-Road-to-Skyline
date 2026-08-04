# Demo 1.0 implementation

Open `Assets/Demo1/Scenes/Demo1.unity` and enter Play Mode.

## Controls

- The full squad is selected on entry so a move or attack can be issued immediately.
- Left click / drag: select one or several witches. Shift modifies the current selection.
- Single-selecting a witch opens a live character detail panel on the right. It hides for multi-selection and while the battle panel is open.
- Right click ground: give every selected witch an independent move order into the same small destination area.
- Right click a discovered enemy, or `A` then left click it: approach automatically and initiate combat when in engagement range.
- `G`: send selected eligible witches to reinforce the nearest active battle.
- `R`: start the delayed retreat process for selected participants.
- `B` then left click: schedule an area-level remote strike from the selected purple artillery witch.
- `Space`: pause/resume simulation. Camera, selection and orders remain available while paused.
- `F`: focus the selected units. WASD/arrow keys, edge scrolling, middle drag and wheel control the camera.
- `Ctrl+1..9`: save a control group; `1..9`: recall and focus it.
- Click a battle bubble: open the live three-line battle panel. Opening it does not pause the simulation.

## Three-line battle prototype

- Vanguard, main and support lines have no participant limit; every reinforcement enters its preferred line directly.
- Witches and guards prefer the vanguard, artillery and scouts prefer the main line, and support units and fortresses prefer the support line.
- Ordinary attacks must target the nearest occupied enemy line. Vanguard units provide screening for units behind them.
- Artillery and scouts use screen-piercing attacks. Their chance to reach the main/support lines is `penetration * (1 - screening efficiency)`; complete screening blocks penetration.
- Select a friendly card in the live battle panel to order a two-second line change. Repositioning units remain exposed but cannot attack, screen or support until the timer completes.
- The lines now have explicit tradeoffs: vanguard deals 10% less damage but takes 15% less damage and provides 25% more screening; main deals 15% more damage; support deals 20% less damage but multiplies active support effects by 1.5. Global shield support only works from the support line.
- Every role has a combat identity beyond base stats:
  - witches prioritize enemy screen-piercing threats on the currently exposed line;
  - artillery performs a 1.45x calibrated salvo every third attack;
  - scouts mark hit targets for four seconds, causing 20% more incoming damage;
  - support witches pulse allied shield and magic every four seconds while active on the support line;
  - guards on the vanguard have a 65% chance to intercept a successful attack that pierced toward the rear;
  - fortresses enter an emergency barrage below 50% health, attacking 35% faster and dealing 15% more damage.
- Opening the battle panel suppresses map-space unit names, health bars and combat IDs so tactical overlays never cover the panel.
- Enemy scouts, guards and the fortress use reinforced prototype durability and attack values, giving line changes and reinforcement deployment time to affect the battle.
- These formation rules and values are code-only prototype assumptions and have not been written back to Feishu revision 6.

## Witch vision and player intelligence prototype

- Witch vision is independent from combat role. The original four witches are ordinary witches with a 100-degree forward visual sector and no circular radar.
- Sanya V. Litvyak is added as the fifth player unit. She is a night witch with a 28-unit, 360-degree circular detection area and no visual sector.
- Moving or engaging turns an ordinary witch's sector toward her destination or target. Selecting a witch shows only the area produced by her own vision type.
- A newly observed enemy starts as an unknown contact, becomes identified after 0.5 seconds of observation, and becomes assessed after another 1.5 seconds. Only assessed intelligence exposes health on the strategic map.
- When observation is lost, the enemy marker freezes at its last known position. Intelligence degrades from assessed to identified after 3 seconds, to contact after 7 seconds, and disappears after 15 seconds.
- Stale contacts cannot be directly engaged, but their last known area remains a valid movement or remote-strike destination. Mission-known fixed objectives keep persistent identified intelligence.
- These vision shapes, durations and sharing rules are code-only prototype assumptions and have not been written back to Feishu revision 6.

## Independent enemy AI prototype

- Enemy decisions run at a 0.5-second interval. Every mobile enemy owns its target, last-known position and state; there is no director, shared blackboard, contact sharing or coordinated reinforcement logic.
- The scout cycles through a configured patrol route, pursues the nearest player inside its own circular vision, starts combat inside engagement range and investigates only its own last-known contact position after losing sight. Below 30% health it requests the normal delayed retreat.
- Guards hold individual home positions, pursue the nearest player visible inside their own vision and 18-unit home leash, engage through the normal combat system, and return to their own post after losing the target. Guards do not voluntarily retreat.
- Independent enemies can still enter the same battle when their personal behavior takes them into its forced-engagement area. This is a consequence of existing battle geography, not a joint AI decision.
- The fortress remains fixed and reactive. It has no strategic AI layer.
- These behaviors and values are code-only prototype assumptions because Feishu revision 6 explicitly leaves AI engagement and retreat decisions for later design.

## Configurable prototype assumptions

The Feishu specification revision 6 intentionally leaves formulas and concrete values open. Demo defaults are centralized in `Demo1Balance` and `DemoUnitStats`:

- defense is flat reduction with a minimum damage floor;
- a discovered core replaces (rather than stacks with) an ordinary critical multiplier;
- shield absorption consumes both shield and magic, with allied global shield bonuses improving efficiency;
- pathing is direct movement on the unobstructed prototype map; every witch owns her route and destination offset;
- enemy AI uses the independent scout/guard state machines described above; player operational choices remain manual;
- orders may be queued while paused;
- the victory condition is destroying the enemy fortress, and defeat occurs when no player witch remains operational.

These are implementation defaults, not new game-design source of truth. They can be replaced without changing the simulation API when the corresponding Feishu sections are approved.
