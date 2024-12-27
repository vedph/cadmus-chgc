# Cadmus CHGC Core

Components specific to the Cadmus CHGC Project.

- [API](https://github.com/vedph/cadmus-chgc-api)
- [app](https://github.com/vedph/cadmus-chgc-app)

Essentially here we use the open-ended content creation system Cadmus to allow users draw on top of IIIF-serviced images, adding some specialized annotation to each shape drawn.

So, there is a totally independent software tool whose task is letting users annotate images. It has its own database, and lives on a server connected to the web. You just point your browser to the login page, enter the application, and work on it.

On the _Compendium_ project end, there are 3 inputs for the software:

- the IIIF URIs for images; there is typically one for each manuscript;
- the pages of each manuscript. Rather than having users manually create each page in the Cadmus system, we massively import all the pages definitions from a simple XML file. This provides a skeleton ready to be filled with data;
- the authoritative list of entities, which will appear in the annotations to represent the various characters involved in the genalogy.

Once these resources have been imported, you are free to use Cadmus to annotate images at will. Everything is saved in its own database. When you are ready, you can generate a full TEI document for a manuscript, or patch an existing one by just updating the visual annotations in it. You can also re-import the authoritative list if needed.

Cadmus modeling is based on macro-records, named _items_, built of components named _parts_. You can think of items as boxes, and of parts as objects put into them. This allows open and dynamic modeling, because items models are just the sum of the parts they include; add a new part, and get a new model.

In our case, items are manuscript pages; and parts are the annotations on top of each page's image. So you end up with a list of pages, grouped into manuscripts by their group ID; and inside each page you have an object representing its visual annotations. The typical process is selecting a page, editing its annotations part, and saving it when done. At any moment you can then export TEI, both from the web UI or from a dedicated CLI tool, and the same is true for data imports.

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
  - eid\* (`string`, thesaurus `chgc-ids`): the CHGC ID.
  - label (`string`)
  - note (`string`)

## CLI

The CLI tool is a multiple-platform, command-line based tool used to export TEI from a CHGC Cadmus database. This is work in progress; currently the only command is `build-tei` to build one or more TEI documents from scratch. The tool can run in Windows, MacOS, and most Linux flavors.

>The import area of this project workflow (=[importing thesauri](https://github.com/vedph/cadmus_tool#thesaurus-import-command) of IDs from Excel/CSV/plain text) can also be covered by the [generic Cadmus CLI tool](https://github.com/vedph/cadmus_tool).

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

(1) **zones**, one for each annotation in a `TEI/text/facsimile` element, with the following template. In this template:

- `IMAGE_HID` = the human-friendly image ID, like e.g. `ccc-ms029/1`, built with MS ID and page ID, so that sorting by it has the effect on sorting pages into their physical order (e.g. `akb-wettf-9-239r`).
- `IMAGE_URI`: the source image URI.
- `ANNOTATION_GUID`: the annotation GUID (without the initial `#`, which is included in the GUID generated by Recogito Annotorious library), e.g. `36c9730c-a7c9-4a28-8889-8d6015ee14fe`.
- `ENTITY_ID`: the ID of the entity picked from the project's authority list, e.g. `n-aaron`.
- `GEOMETRY_ATTRIBUTES` are attributes which vary according to the geometry of the drawn shape (see below).
- `SVG` is the SVG code, produced by Annotorious or supplied by the exporter for rectangular shapes and polygons.
- `GUID` is a newly generated GUID, which will change whenever the exporter runs.

```xml
<facsimile>
  <!-- begin for each image -->
  <surface xml:id="IMAGE_HID"
           source="IMAGE_URI">
    <!-- begin for each annotation in the image -->
    <zone xml:id="z-GUID"
          source="a-ANNOTATION_GUID"
          n="IMAGE_HID/ENTITY_ID"
          GEOMETRY_ATTRIBUTES>
      <svg xmlns="http://www.w3.org/2000/svg">
        SVG
      </svg>
    </zone>
    <!-- end for each annotation in the image -->
  </surface>
  <!-- end for each image -->
</facsimile>
```

here, `geometry-attributes` are:

- **rectangle**: attributes `ulx`, `uly`, `lrx`, `lry`.
- **polygon**: attribute `points` with a pair of `X,Y` coordinates (separated by comma), where pairs are separated by space.
- **circle**: output its bounding rectangle attributes.
- **ellipse**: output its bounding rectangle attributes.
- **freehand**: just the SVG content. A bounding rectangle here would require too much work to parse the SVG path minilanguage and do the corresponding calculations, whereas bounding rectangles are just a way for gracefully downgrade data in case of clients not able to deal with SVG. At any rate, freehand shapes are likely to be very rare, or just never be used.

(2) **pages**, in the `TEI/text/body` element, with this template:

```xml
<body>
  <!-- begin for each image -->
  <pb xml:id="p-GUID"
      n="IMAGE_HID"
      source="IMAGE_HID"/>
  <!-- a div for each annotation, merged if sharing the same ENTITY_ID; type is node, text, diagram, picture, group, connection -->
  <div xml:id="d-GUID"
       source="ANNOTATION_GUID ..."
       type="node"
       corresp="#ENTITY_ID"
       facs="#IMAGE_HID/ENTITY_ID ...">
      <!-- these elements are output only if they have content -->
      <ab type="label">...</ab>
      <note><p>...</p></note>
  </div>
  <!-- end for each image -->
</body>
```

A sample output follows, from a single item (page) having 2 annotations, the first one including a note with two paragraphs:

```xml
<TEI xmlns="http://www.tei-c.org/ns/1.0">
  <teiHeader>
    <fileDesc>
      <titleStmt>
        <title type="main">Compendium Historiae in genealogia Christi</title>
        <title type="sub">Electronic transcription of the manuscript </title>
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
    <surface xml:id="akb-wettf-9-239r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_fob_TC_46/full/1024,/0/default.jpg">
      <zone xml:id="z-af3f7ec4-628a-4309-9925-47f02c910002" source="a-ba853490-d778-4f81-aeda-cb029374f201" n="ccc-ms029/1/n-aaron" ulx="142" uly="82" lrx="322" lry="257">
        <svg xmlns="http://www.w3.org/2000/svg ">
          <rect x="142" y="82" width="180" height="175" />
        </svg>
      </zone>
      <zone xml:id="z-f9798f9d-9eeb-4a2f-aa2f-3bcf48b0f65d" source="a-22af5bee-3981-4edc-85b9-ca3288105f03" n="ccc-ms029/1/n-abacuc" ulx="363.3053113101396" uly="191.8053113101396" lrx="549.6946886898604" lry="378.1946886898604">
        <svg xmlns="http://www.w3.org/2000/svg ">
          <circle cx="456.5" cy="285" r="93.19468868986043" />
        </svg>
      </zone>
    </surface>
  </facsimile>
  <text>
    <body>
      <pb xml:id="p-87210ce5-aaa4-4d64-8cd4-57fa15418104" n="akb-wettf-9-239r" source="akb-wettf-9-239r" />
      <div xml:id="d-8887f7d9-d9cf-4010-a16c-8e40347c678e" source="a-ba853490-d778-4f81-aeda-cb029374f201" type="node" corresp="#n-aaron" facs="#ccc-ms029/1/n-aaron">
        <ab type="label">Aaron</ab>
        <note>
          <p>A note about Aaron.</p>
          <p>This is the second paragraph.</p>
        </note>
      </div>
      <div xml:id="d-3476877a-ae7d-4609-8fa3-6d22b1073696" source="a-22af5bee-3981-4edc-85b9-ca3288105f03" type="node" corresp="#n-abacuc" facs="#ccc-ms029/1/n-abacuc">
        <ab type="label">Abacuc</ab>
      </div>
    </body>
  </text>
</TEI>
```

### Import TEI Command

🎯 Import TEI documents into a CHGC database, where each document contains a `TEI` root with the TEI namespace as the default document namespace, and containing a `facsimile` element including as many `surface` elements as the pages to import. This is used to create in advance the items representing all the pages of a given manuscript, each TEI document corresponding to a single manuscript.

It is assumed that the TEI file name is equal to the manuscript ID; this will become the group ID of the imported items.

For each `surface` element, an item will be created with these data:

- facet ID = "image";
- group ID = the TEI file name (without extension);
- title = surface ID;
- description = surface ID: image uri;
- flags = 1 (=imported);
- creator ID and user ID = `zeus`;
- an empty CHGC _image annotations_ part with the corresponding target image.

Syntax:

```ps1
./chgc import-tei <InputFileMask> [-d <DatabaseName>] [-p <UriShortenerPattern>]
```

- `-d` (or `--database`): the target database name (default = `cadmus-chgc`);
- `-p` (or `--pattern`): the regular expression pattern to use to shorten the URI for using it in the imported item's description. If not specified, the URI will be copied as is; else, it will be shortened by replacing it with the _first group_ of the match.

Example:

```ps1
./chgc import-tei c:/users/dfusi/desktop/ccc-ms029.xml -p "^https:\/\/stacks\.stanford\.edu\/image\/iiif\/xj710dc7305\/([^\/]+).+"
```

In this example, the pattern is designed for URIs like this:

```txt
https://stacks.stanford.edu/image/iiif/xj710dc7305/029_vi_R_TC_46/full/1024,/0/default.jpg
```

In this case, the first group of the match will be `029_vi_R_TC_46`, which will be used as the shortened URI.

👉 Example import file:

```xml
<TEI xmlns="http://www.tei-c.org/ns/1.0">
  <facsimile>
    <surface xml:id="ccc-ms029-006r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_vi_R_TC_46/full/1024,/0/default.jpg"> 
    </surface>
    <surface xml:id="ccc-ms029-006v" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_vi_V_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-007r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_vii_R_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-007v" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_vii_V_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-008r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_viii_R_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-008v" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_viii_V_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-009r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_ix_R_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-009v" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_ix_V_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-010r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_x_R_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-010v" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_x_V_TC_46/full/1024,/0/default.jpg"/>
    <surface xml:id="ccc-ms029-011r" source="https://stacks.stanford.edu/image/iiif/xj710dc7305/029_xi_R_TC_46/full/1024,/0/default.jpg"/>
  </facsimile>
</TEI>
```

>⚠ Please notice that the `facsimile` element and its descendants must be inside the TEI namespace! So remember to include the `xmlns` attribute in the root `TEI` element, as in the above example.

## History

- 2024-12-27: updated test and CLI packages.

### 4.0.0

- 2024-11-30: ⚠️ upgraded to .NET 9.

### 3.0.2

- 2024-05-06: updated packages.

### 3.0.1

- 2023-11-21: updated packages.

### 3.0.0

- 2023-11-18: ⚠️ upgraded to .NET 8.
- 2029-09-29: changed imported item title to be equal to just the surface ID.

### 2.1.9

- 2023-09-29: changed import/export so that they use `surface @xml:id`.

### 2.1.6

- 2023-09-20: fix SVG namespace in exporter.

### 2.1.5

- 2023-09-05:
  - updated packages.
  - tolerate missing `@id` when patching.

### 2.1.4

- 2023-08-17: refactored `div` content for label and note:
  - label is `ab type="label"`;
  - note is `note` with 1 or more `p`.
- 2023-08-08:
  - added `a-` and `i-` prefixes to IDs in XML.
  - refactored `div` content for label and note so that they uniformly use `div`'s with a different `@type` and include 1 or more `p`.
  - moved source GUIDs to `@source` and made `@id` contain a newly generated GUID for `div`, `pb` and `zone` in XML.

### 2.1.3

- 2023-08-06:
  - updated packages.
  - removed `#` from `xml:id`.

### 2.1.2

- 2023-08-01: fixes to supplied SVG in export.

### 2.1.0

- 2023-07-28: refactoring XML output schema.

### 2.0.3

- 2023-07-18:
  - added `image` to CHGC image annotations part.
  - added importer.
- 2023-07-14: breaking changes: refactored imaging parts. This just implies that now the CHGC image annotation model no more has the (unused but previously inherited) `Notes` and `Tags` properties.

### 1.1.5

- 2023-07-07: sorted insert.
- 2023-07-06:
  - added svg child element to zone.
  - added numeric suffixes to image identifiers with the same entity ID.

### 1.1.4

- 2023-07-06: fixed culture in numeric parsing for selectors.

### 1.1.3

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
