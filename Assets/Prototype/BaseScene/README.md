# Base Command Prototype

`Assets/Prototype/BaseScene/Scenes/BaseCommand.unity` is the playable Demo1 entry scene. It presents the 501st base readiness screen over the English Channel map, then hands the selected sortie roster to the live strategic/combat simulation without loading a second scene.

## Usage

- Open `Scenes/BaseCommand.unity` and enter Play Mode.
- Click the 501st base marker near Folkestone.
- Select one or more available witches in the readiness panel and press Deploy.
- The base UI closes and the Demo1 operational HUD opens on the same map. Only the selected witches join the scenario; enemy deployment remains defined by the chosen level config.

## Coordinate contract

- The operational texture represents an approximate `560 x 315 km` English Channel theatre.
- One simulation/world unit equals one kilometre.
- The 501st base anchor is normalized on the texture and converted into the same kilometre coordinate system used by witch spawns, enemy spawns and patrol routes.
- Strategic movement uses `12x` time compression. Per-witch historical aircraft speed data and caveats are documented in `Assets/Demo1/README.md`.

## Prototype boundaries

- The map is an illustrative prototype rather than GIS survey data; the gameplay extent is an explicit coordinate contract, not a claim that every coastline pixel is geographically exact.
- Supply consumption, equipment selection and persistent campaign state are still outside this scene's scope.
- Map and reference materials are for internal prototype review and require rights/art review before public release.

Use `Strike Witches/Base Prototype/Rebuild Base Command Scene` to rebuild the scene if its serialized hierarchy needs regeneration.
