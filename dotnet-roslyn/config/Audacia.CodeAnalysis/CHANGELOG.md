# Changelog

## 1.12.0 - 2025-01-02
### Added
- Added configuration for several new rules in the `Audacia.CodeAnalysis` base .editorconfig ([05a1b94](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/68/files))

### Changed
- Reduced the severity of several overzealous analyzers the base .editorconfig ([05a1b94](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/68/files)).
- The following packages have been updated ([3a34df9e](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/71/files)):
  - `Audacia.CodeAnalysis.Analyzers` from version 1.11.0 to version 1.12.0.
  - `IDisposableAnalyzers` from version 4.0.7 to version 4.0.8.
  - `Roslynator.Analyzers` from version 4.12.3 to version 4.12.10.
  - As part of the package updates the base editorconfig has been updated so that:
    - any deprecated Roslynator rules between RCS1001 to RCS1268 have been removed.
    - any new Roslynator rules between RCS1001 to RCS1268 have been added with an appropriate severity.
    - the analyzer SA1404 has been suppressed as it has now been superseded by custom analyzer ACL1018.

## 1.11.0 - 2024-09-25
### Added
- No new functionality added

### Changed
- Updated the `Audacia.CodeAnalysis.Analyzers` package to version 1.11.0 ([e685957](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/commit/e685957748ac98304bdf3f0dc8c693848d928a7d))
- Included missing Roslyn security analyzers to the base .editorconfig ([4742110](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/commit/4742110aafc8de0df1e8def6150089c3aae9848c))

### Fixed
- No fixes implemented

## 1.10.1 - 2024-09-23
### Added
- No new functionality added

### Changed
- No functionality changed

### Fixed
- Correct package reference for "CSharpGuidelinesAnalyzer" ([e843945](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/commit/e843945f9a791fac19ab1e7fe0f53415a6839ae6))

### 1.10.0 - 2024-09-06
### Added
- No new functionality added

### Changed
- Upgraded the Analyzers version to 1.10.0 ([92fd5f0](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/34/commits/92fd5f0f6b2aac0cc9103c2cda3f496d6acccc7b))
- Editor config amended ([973e29e](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/34/commits/973e29eecef1d74d546c66c53767413fd98fa568))

## 1.9.0 - 2024-06-10
### Added
- Added new rule "DoNotUseNumberInIdentifierName" (ACL1014) ([026278f](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/28/commits/026278fa0f9ce31b0092b0b507f23ef793970061))

### Changed
- Editor config amended ([3d9daa3](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/28/commits/3d9daa37685795c592959a205e1125c6441a3f53))

## 1.8.0 - 2024-02-22
### Added
- No new functionality added

### Changes
- Upgraded StyleCop Analyzers version to 1.2.0-beta.556 ([4abd217](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/21/commits/4abd217c57064e2c5a8bbcc5f5560fac35632648))

## 1.7.0 - 2024-02-09
### Added
- No new functionality added

### Changed
- Upgraded the Analyzers version to 1.7.3 ([a1afc86](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/17/commits/a1afc8676b85e47b05f5c1087ed59f3899dc587e))

## 1.6.0 - 2024-02-05
### Added
- No new functionality added

### Changed
- Upgraded the Analyzers version to 1.7.0 ([cae88b7](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/13/commits/cae88b7952e615fcb6ef6344ca6256b3c0945164))
