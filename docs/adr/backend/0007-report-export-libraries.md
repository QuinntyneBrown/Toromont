# ADR-0007: Report Export Libraries

**Date:** 2026-04-01
**Category:** backend
**Status:** Accepted
**Deciders:** Toromont Fleet Hub Architecture Team

## Context

Toromont Fleet Hub's reporting module must support multi-format export for fleet utilization reports and maintenance cost reports. Users need to download reports as PDF documents for management presentations and audits, Excel spreadsheets for further data analysis and manipulation, and CSV files for integration with external systems and bulk data processing. The export pipeline must generate reports covering up to 90 days of data across 100+ pieces of equipment within a 3-second performance target. The chosen libraries must be open-source or permissively licensed to avoid commercial licensing complications.

## Decision

Use QuestPDF for PDF generation, ClosedXML for Excel (.xlsx) export, and CsvHelper for CSV export.

## Options Considered

### Option 1: QuestPDF + ClosedXML + CsvHelper (Chosen)
- **Pros:** QuestPDF provides a fluent C# API for PDF layout with no external dependencies like wkhtmltopdf; ClosedXML is MIT-licensed and generates native .xlsx files without requiring Excel installation; CsvHelper is the de facto standard for CSV reading/writing in .NET with robust type mapping; all three libraries are open-source with active community maintenance; each library is focused on its specific format, following the single responsibility principle.
- **Cons:** Three separate libraries to learn and maintain; PDF layout with QuestPDF requires learning its fluent document model; no unified abstraction across the three export formats.

### Option 2: iTextSharp (PDF) + EPPlus (Excel) + CsvHelper (CSV)
- **Pros:** iTextSharp is a mature PDF library with extensive features; EPPlus is well-documented with a rich API for Excel manipulation.
- **Cons:** iTextSharp uses AGPL license requiring commercial license for proprietary use; EPPlus moved to a commercial Polyform Noncommercial license in v5+; licensing costs and compliance overhead make this combination unsuitable for commercial fleet management software.

### Option 3: NPOI for All Formats
- **Pros:** Single library covering both Excel and Word document generation; Apache 2.0 license.
- **Cons:** No native PDF generation capability (requires additional libraries); Excel API is lower-level and more verbose than ClosedXML; less idiomatic .NET API design; would still need a separate PDF and CSV library.

### Option 4: HTML-to-PDF Conversion (e.g., Puppeteer, wkhtmltopdf)
- **Pros:** Familiar HTML/CSS-based layout; reuse frontend templates for reports.
- **Cons:** Requires external browser/rendering engine dependency; significantly slower for server-side generation; harder to containerize; inconsistent rendering across environments; poor fit for the 3-second performance target.

## Consequences

### Positive
- All chosen libraries are open-source with permissive licenses, eliminating commercial licensing concerns.
- QuestPDF's fluent API enables programmatic PDF layouts with precise control over tables, charts, headers, and footers needed for fleet reports.
- ClosedXML generates native .xlsx files that open directly in Excel without compatibility warnings.
- CsvHelper's automatic type mapping and configuration options handle edge cases like quoted fields, custom delimiters, and culture-specific number formatting.
- Focused, single-purpose libraries are individually testable and replaceable.

### Negative
- Three separate libraries require developers to learn three different APIs.
- No shared abstraction means report layout logic is duplicated across PDF, Excel, and CSV generators.
- QuestPDF's document model has a learning curve for developers unfamiliar with programmatic document layout.

### Risks
- Performance may degrade for reports with very large datasets (e.g., 365 days across 500+ equipment). Mitigation: implement streaming generation where possible, paginate large datasets, and set reasonable maximum ranges.
- QuestPDF is a newer library with a smaller community than iTextSharp. Mitigation: the library is actively maintained, has comprehensive documentation, and the team can contribute fixes if needed.
- Memory consumption during PDF generation of large reports could cause issues under concurrent load. Mitigation: limit concurrent report generation and implement request queuing.

## Implementation Notes
- Create a `ReportExportService` with methods for each format: `GeneratePdfAsync()`, `GenerateExcelAsync()`, `GenerateCsvAsync()`.
- QuestPDF: Define document components for report header (logo, title, date range), data tables, summary sections, and page footers with page numbers.
- ClosedXML: Create worksheets with styled headers, auto-filtered columns, number formatting for currency and percentages, and a summary row.
- CsvHelper: Configure with appropriate culture settings, header mapping, and UTF-8 BOM for Excel compatibility.
- All export methods accept a common report data model and return a `byte[]` or `Stream` for HTTP response streaming.
- Performance target: generate reports for 90 days of data across 100+ equipment in under 3 seconds.
- Return appropriate `Content-Type` headers: `application/pdf`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `text/csv`.

## References
- [L1-009: Reporting & Analytics Design](../design/L1-009-reporting-analytics.md)
- [QuestPDF Documentation](https://www.questpdf.com/)
- [ClosedXML GitHub Repository](https://github.com/ClosedXML/ClosedXML)
- [CsvHelper Documentation](https://joshclose.github.io/CsvHelper/)
- [ADR-0003: Dapper for High-Performance Queries](0003-dapper-high-performance-queries.md)
