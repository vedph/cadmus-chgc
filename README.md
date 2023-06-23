# Cadmus CHGC Core

Components specific to the Cadmus CHGC Project.

- [API](https://github.com/vedph/cadmus-chgc-api)
- [app](https://github.com/vedph/cadmus-chgc-app)

This project uses Cadmus only as a mean of annotating images with an additional set of essential metadata. So, the only project-specific component is the `ChgcImageAnnotationsPart` part.

## ChgcImageAnnotationsPart

ID: `it.vedph.chgc.image-annotations`

- annotations (`ChgcImageAnnotation[]`):
  - id\* (`string`): this ID is a GUID automatically generated for each annotation.
  - target\* (`GalleryImage`):
    - id\* (`string`)
    - uri\* (`string`)
    - title\* (`string`)
    - description (`string`)
  - selector\* (`string`)
  - notes (`string[]`): optional notes in the annotation.
  - tags (`string[]`): optional tags in the annotation.
  - eid\* (`string`, thesaurus `chgc-ids`): the CHGC ID.
  - label (`string`)
  - note (`string`)

## History

### 1.1.2

- 2023-06-23: updated packages.

### 1.1.0

- 20233-06-17: refactored part model.

### 1.0.1

- 2023-06-02: updated packages.

### 1.0.0

- 2023-05-24: updated packages (breaking change in general parts introducing [AssertedCompositeId](https://github.com/vedph/cadmus-bricks-shell/blob/master/projects/myrmidon/cadmus-refs-asserted-ids/README.md#asserted-composite-id)).
