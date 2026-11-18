# History

## 5.0.1

- 2026-11-18: fixes to `Cadmus.Chgc.Export` for selectors and annotation IDs coming from Annotorious V3:
  - TEI export failed with `Invalid selector` for the annotations saved by the frontend with Annotorious V3, whose selectors are JSON-serialized shapes. JSON rectangles and polygons are now accepted.
  - added rectangles in SVG selectors, as Annotorious V3 rectangles can be rotated: the zone gets the bounding box of the rotated shape, and its SVG keeps the rotation.
  - the `d` attribute of an exported SVG path contained the whole selector rather than the path data.
  - the first character of an annotation ID was always dropped assuming it to be `#`, but this is no longer true for Annotorious V3 IDs. Now only an initial `#` is removed.

## 5.0.0

- 2026-11-16: ⚠️ upgraded to .NET 11.

## 4.0.1

- 2025-01-30: updated packages.
- 2024-12-27: updated test and CLI packages.

## 4.0.0

- 2024-11-30: ⚠️ upgraded to .NET 9.

## 3.0.2

- 2024-05-06: updated packages.

## 3.0.1

- 2023-11-21: updated packages.

## 3.0.0

- 2023-11-18: ⚠️ upgraded to .NET 8.
- 2029-09-29: changed imported item title to be equal to just the surface ID.

## 2.1.9

- 2023-09-29: changed import/export so that they use `surface @xml:id`.

## 2.1.6

- 2023-09-20: fix SVG namespace in exporter.

## 2.1.5

- 2023-09-05:
  - updated packages.
  - tolerate missing `@id` when patching.

## 2.1.4

- 2023-08-17: refactored `div` content for label and note:
  - label is `ab type="label"`;
  - note is `note` with 1 or more `p`.
- 2023-08-08:
  - added `a-` and `i-` prefixes to IDs in XML.
  - refactored `div` content for label and note so that they uniformly use `div`'s with a different `@type` and include 1 or more `p`.
  - moved source GUIDs to `@source` and made `@id` contain a newly generated GUID for `div`, `pb` and `zone` in XML.

## 2.1.3

- 2023-08-06:
  - updated packages.
  - removed `#` from `xml:id`.

## 2.1.2

- 2023-08-01: fixes to supplied SVG in export.

## 2.1.0

- 2023-07-28: refactoring XML output schema.

## 2.0.3

- 2023-07-18:
  - added `image` to CHGC image annotations part.
  - added importer.
- 2023-07-14: breaking changes: refactored imaging parts. This just implies that now the CHGC image annotation model no more has the (unused but previously inherited) `Notes` and `Tags` properties.

## 1.1.5

- 2023-07-07: sorted insert.
- 2023-07-06:
  - added svg child element to zone.
  - added numeric suffixes to image identifiers with the same entity ID.

## 1.1.4

- 2023-07-06: fixed culture in numeric parsing for selectors.

## 1.1.3

- 2023-07-01: added RAM-based item composer.
- 2023-06-29:
  - added `source` attribute.
  - adapted `FSChgcTeiItemComposer` for patching.
- 2023-06-24: adding export library.

## 1.1.2

- 2023-06-23: updated packages.

## 1.1.0

- 20233-06-17: refactored part model.

## 1.0.1

- 2023-06-02: updated packages.

## 1.0.0

- 2023-05-24: updated packages (breaking change in general parts introducing [AssertedCompositeId](https://github.com/vedph/cadmus-bricks-shell/blob/master/projects/myrmidon/cadmus-refs-asserted-ids/README.md#asserted-composite-id)).
