# PhatStats v5 Icon Standard

- Runtime canvas is exactly **82×82** pixels.
- Runtime identifiers and selection logic are unchanged from v4.
- Every source semantic layer is a true **1-bit BMP** (`white`, `gray`, `accent`, `yellow`).
- Layers are composited offline into the existing packed 4-bpp Indexed4 format, so the v4 renderer and UI geometry remain untouched.
- Brand edge color is normalized to one exact RGB565 palette entry per icon family; anti-alias color drift is not retained.
- NVIDIA uses the existing green plus yellow connector/accent entry. AMD uses the existing red entry. Intel uses the existing blue entry; Iris Xe uses the existing cyan entry.
- No stretch, edge diffusion, or runtime scaling is used. The approved square icon panel is padded to a square before conversion and then sampled to 82×82.
- Hardware target, GPIO assignments, partitions, serial protocol, portrait layout, and 82×82 placement are outside the icon asset boundary and are unchanged.
