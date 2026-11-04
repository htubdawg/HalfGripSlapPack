# Signal stalk artwork

`turn-signal-stalk.png` is original automotive pixel artwork created for Slap Pack. It is not copied from the helicopter-control reference
or from the game's private reference assets. Keep the PNG with the mod's `src` and `mod.json` when installing or distributing.

The transparent 2172×724 source contains a shaded plastic grip, etched arrows, tapered metal shaft, collar, and rubber bellows.
`SignalStalkLayout` maps three source regions onto the control: the grip and boot retain their proportions while only the shaft stretches.
Point filtering keeps its edges crisp. The visible grip's center stays at the native grab collider's center across configured lengths.

If the artwork changes, update the crop and slice coordinates in `SignalStalkLayout` together. The original generated source is retained
outside the checkout during development; this PNG is the runtime asset.
