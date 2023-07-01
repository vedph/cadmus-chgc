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

## CLI

The CLI tool is a multiple-platform, command-line based tool used to export TEI from a CHGC Cadmus database. This is work in progress; currently the only command is `build-tei` to build one or more TEI documents from scratch. The tool can run in Windows, MacOS, and most Linux flavors.

>The import area of this project workflow (=[importing thesauri](https://github.com/vedph/cadmus_tool#thesaurus-import-command) of IDs from Excel/CSV/plain text) is covered by the [generic Cadmus CLI tool](https://github.com/vedph/cadmus_tool).

### Build TEI Command

🎯 Build and/or patch TEI documents from a CHGC database. If a TEI document exists, it will be updated. Otherwise, a new one will be created.

Syntax:

```ps1
./chgc build-tei <OutputDirectory> [-d <DatabaseName>] [-g <GroupId>]
```

- `-d` (or `--database`): the source database name (default = `cadmus-chgc`).
- `-g` (or `--group`): the group ID to limit extraction to. When not specified, all the groups will be exported. Each group corresponds to a manuscript, and each manuscript corresponds to a TEI file.

Example:

```ps1
./chgc build-tei c:/users/dfusi/desktop/out -g ms-a
```

A new TEI file is generated for each manuscript (group ID). So, with relation to the Cadmus database, we are exporting items grouped by their groups. In turn, each item usually includes an images annotations part. When this is not present, the item is not exported; otherwise, the contents of this part are used to populate the TEI output.

Each TEI document is named after its group ID (=manuscript ID). Its header is somewhat fixed, except for some trivial data like current year. As for the `text` element, it is structured as follows:

(1) **zones**, in a `TEI/text/facsimile` element, with this template:

```xml
<facsimile>
  <!-- begin for each image -->
  <surface n="{{image-id}}">
    <!-- begin for each annotation in the image -->
    <zone geometry-attributes xml:id="{{annotation-id}}" />
    <!-- end for each annotation in the image -->
  </surface>
  <!-- end for each image -->
</facsimile>
```

here, `geometry-attributes` are:

- for **rectangles**, attributes `ulx`, `uly`, `lrx`, `lry`.
- for **polygons**, attribute `points` with a pair of `X,Y` coordinates (separated by comma), where pairs are separated by space.
- for all the other shapes, we have not yet defined a proper TEI rendition. So, currently the behavior is as follows:
  - **circle**: output its bounding rectangle attributes, plus a comment inside the zone element with the original content of the zone selector (SVG).
  - **ellipse**: output its bounding rectangle attributes, plus a comment inside the zone element with the original content of the zone selector (SVG).
  - **freehand**: output only a comment inside the zone element with the original content of the zone selector (SVG). A bounding rectangle here would require too much work to parse the SVG path minilanguage and do the corresponding calculations, whereas bounding rectangles are just a temporary hack to avoid an empty output. At any rate, freehand shapes are likely to be very rare, or just never be used.

Also, `annotation-id` is the annotation ID. This can be generated at will, within these constraints:

- the ID should be short and human friendly, because most of the work will be manually done in TEI.
- it is advisable to have an ID which, though human-friendly, can be traced back to the annotation source.

Presently I use these components to build such an ID (all separated by slash):

1. group ID (=manuscript ID);
2. image ordinal number in its source;
3. annotation ID.

The first two components build the image ID.

For instance, the image ID `ms-a/7` represents the 7th image from the source of manuscript `ms-a`; and `ms-a/7/n-aaron` represents the ID of the annotation for Aaron in that image.

This of course is a compromise, and may not be ideal should our IIIF source manifests change. The ordinal number here is just the ordinal number of the collection of images from a specific manifest. We could of course use other more robust IDs, though they are less human friendly and more verbose, e.g. the image URI, or the annotation GUID which is globally unique (at any rate, this is output as a comment before each element it generated).

(2) **pages**, in the `TEI/text/body` element, with this template:

```xml
<body>
  <!-- begin for each image -->
  <pb n="{{image-id}}"/>
  <!-- node template: -->
  <div type="node" corresp="{{eid}}" facs="#{{annotation-id}}">
      <!-- these elements are output only if their placeholder is defined -->
      <label>{{ann-label}}</label>
      <note>{{ann-text}}</note>
  </div>
  <!-- text template: -->
  <div type="text" corresp="{{WHERE-FROM??}}" facs="#{{annotation-id}}">
    <!-- as above -->
  </div>
  <!-- diagram template: -->
  <div type="diagram" corresp="{{WHERE-FROM??}}" facs="#{{annotation-id}}">
    <!-- as above -->
  </div>
  <!-- picture template: -->
  <div type="picture" corresp="{{WHERE-FROM??}}" facs="#{{annotation-id}}">
    <!-- as above -->
  </div>
  <!-- group template: -->
  <div type="group" corresp="{{eid}}" facs="#{{annotation-id}}">
    <!-- as above -->
  </div>
  <!-- connection template: -->
  <div type="connection" corresp="{{WHERE-FROM??}}" facs="#{{annotation-id}}">
    <!-- as above -->
  </div>
  <!-- end for each image -->
</body>
```

where:

- `eid` is the entity ID (e.g. `n-abacuc`, etc.).
- `annotation-id` is the annotation ID, as above.

> ⚠️ `WHERE-FROM??` currently marks those template values which are not sure. For the moment I've just included `eid` here too.

Also, I add a `source` attribute to each surface, zone, pb, and div elements, just to keep track of the original source of the data. You can get rid of it if you want.

A sample output follows, from a couple of mock items where only the first one has two rectangular annotations:

```xml
<?xml version="1.0" encoding="utf-8"?>
<TEI xmlns="http://www.tei-c.org/ns/1.0">
  <teiHeader>
    <fileDesc>
      <titleStmt>
        <title type="main">Compendium Historiae in genealogia Christi</title>
        <title type="sub">Electronic transcription of the manuscript ms-a</title>
        <author>Petrus von Poitiers<ex>Petrus Pictaviensis</ex></author>
        <respStmt>
          <resp>edited by</resp>
          <persName ref="#" />
        </respStmt>
      </titleStmt>
      <publicationStmt>
        <publisher>
          <orgName corresp="https://kunstgeschichte.unigraz.at">Institut für Kunstgeschichte, Karl-Franzens-Universität Graz</orgName>
        </publisher>
        <authority>
          <orgName corresp="https://informationsmodellierung.unigraz.at">Zentrum für Informationsmodellierung - Austrian Centre for Digital Humanities, Karl-Franzens-Universität Graz</orgName>
        </authority>
        <distributor>
          <orgName ref="https://gams.uni-graz.at">GAMS - Geisteswissenschaftliches Asset Management System</orgName>
        </distributor>
        <availability>
          <licence target="https://creativecommons.org/licenses/by-ncsa/4.0">Creative Commons BY-NC-SA 4.0</licence>
        </availability>
        <date>2023</date>
        <pubPlace>Graz</pubPlace>
      </publicationStmt>
      <sourceDesc>
        <p />
      </sourceDesc>
    </fileDesc>
  </teiHeader>
  <facsimile>
    <surface n="ms-a/7" source="#f134aaf2-fe44-4057-8723-0d94f7068fdd">
      <zone xml:id="ms-a/7/n-aaron" source="#4a706b03-b651-4a89-b4a4-b2b61b85b863" ulx="456" uly="100" lrx="698" lry="337" />
      <zone xml:id="ms-a/7/n-abacuc" source="#4325e8eb-40c7-4dc9-a891-f7f27510cef3" ulx="220" uly="786" lrx="418" lry="934" />
    </surface>
  </facsimile>
  <text>
    <body>
      <pb n="ms-a/7" source="#f134aaf2-fe44-4057-8723-0d94f7068fdd" />
      <div source="#4a706b03-b651-4a89-b4a4-b2b61b85b863" type="node" corresp="#n-aaron" facs="#ms-a/7/n-aaron">
        <label>Aaron</label>
        <text>
          <p>A note about Aaron</p>
        </text>
      </div>
      <div source="#4325e8eb-40c7-4dc9-a891-f7f27510cef3" type="node" corresp="#n-abacuc" facs="#ms-a/7/n-abacuc" />
    </body>
  </text>
</TEI>
```

## History

- 2023-07-01: added RAM-based item composer.
- 2023-06-29:
  - added `source` attribute.
  - adapted `FSChgcTeiItemComposer` for patching.
- 2023-06-24: adding export library.

### 1.1.2

- 2023-06-23: updated packages.

### 1.1.0

- 20233-06-17: refactored part model.

### 1.0.1

- 2023-06-02: updated packages.

### 1.0.0

- 2023-05-24: updated packages (breaking change in general parts introducing [AssertedCompositeId](https://github.com/vedph/cadmus-bricks-shell/blob/master/projects/myrmidon/cadmus-refs-asserted-ids/README.md#asserted-composite-id)).
