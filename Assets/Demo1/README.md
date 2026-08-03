# Demo 1.0 implementation

Open `Assets/Demo1/Scenes/Demo1.unity` and enter Play Mode.

## Controls

- The full squad is selected on entry so a move or attack can be issued immediately.
- Left click / drag: select one or several witches. Shift modifies the current selection.
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

- Each side has two slots in the vanguard, main and support lines; overflow participants wait in reserve.
- Witches and guards prefer the vanguard, artillery and scouts prefer the main line, and support units and fortresses prefer the support line.
- Ordinary attacks must target the nearest occupied enemy line. Vanguard units provide screening for units behind them.
- Artillery and scouts use screen-piercing attacks. Their chance to reach the main/support lines is `penetration * (1 - screening efficiency)`; complete screening blocks penetration.
- Select a friendly card in the live battle panel to order a two-second line change. Repositioning units remain exposed but cannot attack, screen or support until the timer completes.
- Reserve units cannot attack, be targeted, screen or provide support. They automatically deploy when a slot becomes available.
- These formation rules and values are code-only prototype assumptions and have not been written back to Feishu revision 6.

## Configurable prototype assumptions

The Feishu specification revision 6 intentionally leaves formulas and concrete values open. Demo defaults are centralized in `Demo1Balance` and `DemoUnitStats`:

- defense is flat reduction with a minimum damage floor;
- a discovered core replaces (rather than stacks with) an ordinary critical multiplier;
- shield absorption consumes both shield and magic, with allied global shield bonuses improving efficiency;
- pathing is direct movement on the unobstructed prototype map; every witch owns her route and destination offset;
- enemy AI only starts a legal nearby engagement; player operational choices remain manual;
- orders may be queued while paused;
- the victory condition is destroying the enemy fortress, and defeat occurs when no player witch remains operational.

These are implementation defaults, not new game-design source of truth. They can be replaced without changing the simulation API when the corresponding Feishu sections are approved.
