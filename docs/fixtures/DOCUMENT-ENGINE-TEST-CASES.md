# Document engine conformance fixtures

Use synthetic or fully anonymized documents only. Preserve the structure and formatting of representative office instruments while replacing names, addresses, national identifiers, deeds, signatures and all other personal data. Never commit real office documents to this repository.

For each candidate/version, run the same fixture set through: open → inspect → edit a harmless test marker → save DOCX → close → reopen → export PDF. Keep input/output hashes, engine/version, logs, page counts and reviewer notes in the controlled test report. Compare PDF pages visually against a Microsoft Word reference export generated on the same Windows test machine.

| ID | Fixture content | Critical observations |
|---|---|---|
| DOC-01 | Plain Arabic RTL paragraphs, punctuation, Arabic and Latin digits, diacritics and Kashida | Correct paragraph direction, shaping, joining, cursor movement, punctuation order and line wrapping |
| DOC-02 | Mixed Arabic and French on the same line and in adjacent paragraphs | Bidi runs, parentheses, dates, currency amounts, Latin abbreviations and punctuation |
| DOC-03 | French-only LTR instrument | LTR paragraph direction, numbering, tabs and alignment unaffected by RTL UI |
| DOC-04 | A4 multi-page document with margins, page numbers, section/page breaks and widow/orphan settings | Page count, text flow, section boundaries, page numbering and no clipped content |
| DOC-05 | Nested/merged tables, RTL table direction, repeated header rows and fixed column widths | Cell order, borders, row splitting, merged cells and text direction |
| DOC-06 | First/even/odd headers and footers, logos and page-number fields | Header/footer inheritance, correct section behavior, image anchoring and field updates |
| DOC-07 | Footnotes/endnotes with Arabic and French content | Reference placement, numbering, page-bottom layout and save/reopen integrity |
| DOC-08 | Signature blocks, stamps, anchored and inline images | Image position, dimensions, text wrapping and page flow |
| DOC-09 | Legacy DOC input with tables, sections and RTL text | Source file remains byte-identical; a separate DOCX working copy can be produced and reviewed |
| DOC-10 | DOCX with styles, numbered legal clauses, multilevel lists and cross-references | Style inheritance, numbering continuity, list restart/continuation and reference targets |
| DOC-11 | Large 30–50 page fixture with repeated structures | Open/save/export duration, peak memory, responsiveness and pagination consistency |
| DOC-12 | Protected, malformed or unsupported test files | Clear safe error, no silent data loss, no source overwrite, no unexpected external network calls |

## Result rubric

Record each test as **Pass**, **Pass with documented limitation**, **Fail**, or **Not tested**. A limitation affecting legal text, page boundaries, footnotes, direction, numbering or table structure is a critical failure.

For DOCX round-trip, use a structured package comparison in addition to visual PDF comparison. Ignore expected volatile metadata and timestamps, but inspect document XML, relationships, numbering, styles, section properties, footnotes, headers/footers and embedded media. Human review remains mandatory.

## Performance profile

Measure cold and warm open, edit responsiveness, save, PDF export, peak memory and CPU for DOC-01, DOC-05, DOC-07 and DOC-11. Test both a representative client machine and the local dedicated server. Keep the editor client responsive during large document operations. Establish numeric thresholds only after recording the baseline hardware and reference Word behavior.
