# Rule Tile Valley

An editable Unity 6 2D sample level generated entirely from small pixel-art textures in this folder.

## Scene and palette

- Open `Scenes/RuleTileLevel.unity` to view the sample.
- Open `Palettes/ValleyPalette.prefab` from **Window > 2D > Tile Palette** to paint with the grass, rock, water, and wood tiles.
- `Tiles/GrassRuleTile.asset` uses a rotated exposed-edge rule and a fill rule. `Tiles/RockRuleTile.asset` provides a separate paintable rock rule tile.
- The scene's `Level Grid` contains separate Ground, Wood Platforms, and River tilemaps. Ground has a static composite collider.

The level is a starting point: edit the tilemaps directly or paint with the palette. The setup script is available at **Tools > 2D > Create Rule Tile Valley Level**.
