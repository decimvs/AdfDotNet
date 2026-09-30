# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-09-30

First public release.

### Added

- `AdfDotNet.Core`: typed document model covering every node type (32) and mark type (10) on the Jira ADF
  spec, plus `taskList`/`taskItem`; fluent `AdfDocumentBuilder`; spec-based `Validate()`; `Normalize()`.
- `AdfDotNet.Converters.Html`: ADF ↔ HTML conversion, static or through dependency injection.
- `AdfDotNet.Converters.Markdown`: ADF ↔ Markdown (CommonMark + GFM) conversion, static or through
  dependency injection.
- `AdfDotNet.Json.Newtonsoft`: lossless ADF ↔ JSON serialization, static or through dependency injection.
- `AdfDotNet`: meta-package referencing Core and every satellite package.

[1.0.0]: https://github.com/decimvs/AdfDotNet/releases/tag/v1.0.0
