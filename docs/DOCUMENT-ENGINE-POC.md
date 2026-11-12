# Document engine proof-of-concept (POC)

**Status: candidate research only — no engine selected.**

This POC is a release gate. Product feature lists are not evidence of DOCX fidelity. A candidate is not accepted until the same anonymized fixtures pass import, edit, save, reopen, layout and PDF comparison on a Windows workstation and the actual office LAN topology.

## Candidate shortlist

| Candidate | Fit with WPF / workflow | Relevant documented capabilities | Risks / questions before selection |
|---|---|---|---|
| TX Text Control .NET for WPF (Enterprise trial) | Native WPF editor, ribbon controls and document-processing API | DOC/DOCX and PDF import/export; footnotes in Enterprise; explicit Arabic RTL paragraph direction, mixed-direction runs, mirrored UI and Kashida justification | Enterprise edition is needed for footnotes. Official single-developer list price currently shows USD 3,398, with USD 1,398 annual renewal after the included first year. Confirm regional tax/reseller terms, number of licensed developers, support and redistribution terms. |
| DevExpress WPF RichEditControl | Native WPF Word-like editor with integrated ribbon | DOC/DOCX family, PDF export, footnotes/endnotes and RTL paragraph direction are documented | Validate Arabic mixed runs, complex tables and PDF pagination empirically. Confirm the exact subscription/SKU, developer seats and deployment terms in a written quote. |
| ONLYOFFICE Docs Developer (self-hosted) | Web editor hosted on each office server; potentially embedded in the WPF client through WebView2 | Commercial Developer edition is designed for integrating editors into a product; on-premises licensing is offered | Not a native WPF control. Requires a document server per office, LAN connectivity, JWT/security configuration and careful local storage/temp-file handling. Obtain quote for simultaneous connections, servers, updates, branding and redistribution before implementation. |
| Telerik UI for WPF RadRichTextBox | Native WPF rich-text control | DOCX import/export, PDF export, RTL support and headers/footers are documented | Verify footnotes/endnotes and complex Word constructs. Telerik documents that unsupported elements can be stripped on import; create explicit loss tests. Confirm subscription and runtime distribution terms. |
| Aspose.Words for .NET | Supporting document-processing/conversion library, not a full interactive WPF editor | Commercial library can load DOC and DOCX and export PDF; broad conversion and processing API | Not a replacement for the interactive editor. Could be evaluated as a secondary conversion/validation component only after licensing and output fidelity checks. |

## Licensing gate

NOTAIRE DESK is intended to be a proprietary product for multiple independent notarial offices. Do not ship an AGPL-licensed editor component in a closed-source distribution without a written legal review and an explicit decision to meet the license obligations. ONLYOFFICE states that Community Edition is AGPL v3, while its Developer edition is commercial and intended for integration into a developer's own product. Community white-labeling is restricted.

No vendor trial binaries, license files, keys or private package credentials may be committed to GitHub. Every candidate must be trialled under its actual license and deployment terms. The prices above are vendor list prices observed during this research, not a quote or final purchasing recommendation.

## Required architecture constraints

1. Keep document-engine-specific packages and controls behind the `NotaireDesk.Documents.Abstractions` boundary.
2. Store original input bytes and their hash before any conversion or editing. Importing a legacy DOC creates a separate DOCX working copy; never overwrite the source.
3. Keep all source documents, working copies, temporary document files, recovery data and exports within the office installation. No cloud conversion endpoint.
4. Record engine name/version, import/export format, conversion time, warnings and output hash in the document audit trail.
5. A document saved by one candidate must be reopened by that same candidate and by Microsoft Word during acceptance. A successful save call alone is not acceptance.
6. Do not allow document generation or validation to grant legal approval or signing authority.

## Exit criteria

A candidate must pass every critical fixture in [the conformance matrix](fixtures/DOCUMENT-ENGINE-TEST-CASES.md), with no unexplained loss of text, direction, tables, footnotes, numbering, field values, headers/footers or page boundaries. It must also pass the LAN-only, security, performance and written licensing reviews. If no single candidate passes, evaluate a split architecture (WPF editor + separate local conversion engine) without weakening the preservation requirements.

## Official vendor references

- [TX Text Control WPF overview](https://docs.textcontrol.com/tx-text-control-dotnet-wpf/)
- [TX Text Control editions and format support](https://docs.textcontrol.com/tx-text-control-dotnet-wpf/overview/editions/)
- [TX Text Control WPF right-to-left support](https://docs.textcontrol.com/tx-text-control-dotnet-wpf/document-editor/ribbon-ui/right-to-left/)
- [TX Text Control WPF footnotes](https://docs.textcontrol.com/tx-text-control-dotnet-wpf/document-editor/footnotes/)
- [TX Text Control WPF licensing](https://docs.textcontrol.com/tx-text-control-dotnet-wpf/overview/licensing/)
- [TX Text Control WPF pricing](https://www.textcontrol.com/product/tx-text-control-dotnet-wpf/pricing/)
- [DevExpress WPF Rich Text Editor overview](https://docs.devexpress.com/WPF/8651/controls-and-libraries/rich-text-editor)
- [DevExpress WPF feature overview](https://docs.devexpress.com/WPF/9532/controls-and-libraries/rich-text-editor/feature-overview)
- [DevExpress footnotes/endnotes](https://docs.devexpress.com/WPF/401717/controls-and-libraries/rich-text-editor/rich-edit-control-document/footnotes-and-endnotes)
- [ONLYOFFICE edition comparison](https://www.onlyoffice.com/compare-editions)
- [ONLYOFFICE Developer licensing FAQ](https://helpcenter.onlyoffice.com/docs/faq/developer.aspx)
- [ONLYOFFICE Community licensing FAQ](https://helpcenter.onlyoffice.com/docs/installation/community/faq)
- [Telerik WPF RichTextBox overview](https://www.telerik.com/products/wpf/documentation/controls/radrichtextbox/overview)
- [Telerik DOCX import/export](https://www.telerik.com/products/wpf/documentation/controls/radrichtextbox/import-export/docx/docxformatprovider)
- [Aspose.Words supported formats](https://docs.aspose.com/words/net/supported-document-formats/)
