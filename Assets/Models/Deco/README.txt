Unity import notes
- OBJ files import directly. 1 unit = 1 m (objects are real small sizes, scale up in Unity if needed).
- palette.png: Filter Mode = Point, no compression. Models use lowpoly_palette.mtl.
- Model Import Settings: Normals = Import.
- Glass jar needs a transparent material (not included).

Jars (new)
- jar_round / jar_tall / jar_flask .obj = glass only. *_cork.obj = cork stopper (same pivot, place both at the same position).
- Glass: make a URP/Lit material, Surface Type = Transparent, Base Color alpha ~0.25, Smoothness high, then assign to the jar object only.
  (the shared palette material is opaque, so replace it on the glass mesh)
- Inside is hollow with thick walls; jar floor top is at y = 0.6~1.2 cm.
