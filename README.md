# ShAK-12-Sys

SPT 4.1.6 server mod by **Avehulian**.

## What it adds

- ASh-12 12.7x55 full-size sound suppressor
- ASh-12 full-size sight carry handle
- MTs-558 12.7x55 muzzle brake-compensator
- MTs-558 barrel handguard
- 12.7x55mm PS12 Sabot cartridge

The parts are sold by Mechanic and the ammunition by Skier. Handbook entries, flea prices, English/Chinese locales, weapon/magazine compatibility and custom asset bundles are included.

## Installation

Copy the `ShAK-12-Sys` folder into `SPT_Runtime/user/mods/` so the layout is:

```
user/mods/ShAK-12-Sys/
  ShAK-12-Sys.dll
  bundles.json
  bundles/shak12/*.bundle
  db/...
```

## Building

Requires the .NET 10 SDK and an SPT 4.1.6 runtime (for the server assemblies).

```
dotnet build -c Release -p:SptRuntime="C:\path\to\SPT_Runtime"
```

The build output under `bin/Release/ShAK-12-Sys/` is the mod folder.

## Notes

- Item registration runs at `OnLoadOrder.Preload` because SPT 4.1.6 loads profiles before `PostLoad`; adding items later is rejected by `CustomItemService`.
- The item definitions are data-driven from the JSON files in `db/`.
- The asset bundles have their external shader reference patched to the current `p0/Reflective/Bumped Specular SMap` shader (`CAB-56d9…`, path ID `6014991791773097075`) so the materials render correctly.

## License

MIT. See [LICENSE](LICENSE).
