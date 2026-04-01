# ADR-0011: Legacy XML Import with XSD Validation

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub must support migrating equipment data from legacy fleet management systems. These legacy systems commonly export data in XML format. Design 02 describes an XML import endpoint (POST /api/v1/equipment/import) that accepts XML files, validates them against an XSD schema, and processes valid records into the system. The import must enforce a maximum file size of 10MB and prevent XML External Entity (XXE) attacks by prohibiting DTD processing. Clear error reporting is required so administrators can identify and correct invalid records before re-importing.

## Decision

Support legacy equipment data import via XML files with XSD schema validation and XXE prevention using DtdProcessing.Prohibit.

## Options Considered

### Option 1: XML Import with XSD Schema Validation (Chosen)
- **Pros:** Legacy fleet management systems commonly export XML, making this the most natural migration path; XSD schema validation ensures data integrity and catches structural errors before any database writes; validation produces clear, line-level error messages for invalid records; .NET has mature built-in XML and XSD support via System.Xml; well-understood format with strong typing via schema.
- **Cons:** XML is verbose compared to other formats, leading to larger file sizes; XSD schemas can be complex to maintain as the data model evolves; XML parsing has known security risks (XXE, billion laughs) that must be explicitly mitigated.

### Option 2: CSV Import Only
- **Pros:** Simple format that is easy to generate and parse; smaller file sizes than XML; widely supported by spreadsheet tools.
- **Cons:** No built-in schema validation; no support for hierarchical/nested data (e.g., equipment with nested component lists); type information is lost (everything is a string); ambiguity around delimiters, quoting, and encoding; poor error reporting for malformed records.

### Option 3: JSON Import
- **Pros:** Lightweight and human-readable; supports nested structures; native support in modern APIs and tools.
- **Cons:** Legacy fleet management systems typically do not export JSON; no widely adopted schema validation standard equivalent to XSD (JSON Schema is less mature in the .NET ecosystem); would require legacy system operators to perform an additional conversion step.

### Option 4: Direct Database Migration Scripts
- **Pros:** Fastest possible data transfer with no intermediate file format; can handle complex transformations inline.
- **Cons:** Requires direct access to the legacy database, which may not be available or permitted; tightly couples migration to the legacy schema; no reusable import mechanism for future migrations; higher risk of data corruption without a validation layer; requires database expertise to write and maintain scripts.

### Option 5: API-Based Data Sync
- **Pros:** Real-time or near-real-time synchronization; can be incremental rather than bulk.
- **Cons:** Legacy systems rarely expose modern APIs; requires building and maintaining a sync adapter per legacy system; more complex error handling and retry logic; overkill for one-time migration scenarios.

## Consequences

### Positive
- Provides a natural migration path from legacy systems that already export XML.
- XSD schema validation catches structural and type errors before any data reaches the database, ensuring data integrity.
- Clear error reporting (success/failure counts with details per record) enables administrators to fix and re-import invalid data without developer intervention.
- Built-in .NET XML libraries provide robust, well-tested parsing and validation capabilities.
- The 10MB file size limit prevents abuse and ensures predictable server resource consumption.

### Negative
- XML verbosity means import files are larger than equivalent CSV or JSON files, consuming more bandwidth and memory during processing.
- Maintaining the XSD schema requires effort each time the equipment data model changes.
- The endpoint only handles XML, so data from systems that export in other formats must be converted to XML before import.

### Risks
- XXE and related XML attacks (billion laughs, external entity expansion) could compromise the server if not properly mitigated. Mitigation: DtdProcessing.Prohibit is enforced on all XML readers, preventing DTD processing entirely.
- Large import files close to the 10MB limit could consume significant memory during parsing. Mitigation: use streaming XML readers (XmlReader) rather than loading the entire document into memory (XmlDocument/XDocument).
- Malformed XML from legacy systems may fail XSD validation in unexpected ways. Mitigation: provide detailed validation error messages including line numbers and field names to help administrators diagnose issues.

## Implementation Notes
- Expose a POST /api/v1/equipment/import endpoint restricted to Admin role only.
- Configure XmlReaderSettings with DtdProcessing.Prohibit to prevent XXE attacks.
- Validate incoming XML against the equipment XSD schema before processing any records.
- Enforce a maximum file size of 10MB via request size limits and model validation.
- Process records individually: valid records are imported, invalid records are skipped and reported.
- Return a response containing total records count, successfully imported count, failed count, and a list of validation errors with record identifiers.
- Store the XSD schema as an embedded resource or in a well-known configuration path.

## References
- [L1-002: Equipment Management Design](../../design/L1-002-equipment-management.md)
- [L1-013: Data Import/Export Design](../../design/L1-013-data-import-export.md)
- [OWASP XXE Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/XML_External_Entity_Prevention_Cheat_Sheet.html)
- [Microsoft XmlReaderSettings.DtdProcessing Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlreadersettings.dtdprocessing)
