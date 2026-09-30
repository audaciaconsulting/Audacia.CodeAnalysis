# Changelog

## 2.0.0 - 2026-09-30

### Added

- Added `flat/typescript` flat config with selector, `prefer-standalone` and class suffix rules
- Added `docs/compiler-options.md` with recommended Angular compiler options

### Changed

- **Breaking:** Requires ESLint `^9.0.0`
- **Breaking:** `@angular-eslint` `^20.0.0 || ^21.0.0 || ^22.0.0` as peer dependencies
- Updated `data-test-attribute` rule and tests for ESLint 9 and the latest template parser
- Rewrote README for flat config
- Moved `proxyquire` to dev dependencies

### Fixed

- Removed duplicate `data-test-attribute` test cases rejected by ESLint 9

## 2.0.0 - 2026-09-30

## 1.2.0 - 2024-09-10

### Added

- No new functionality added

### Changed

- Updated Angular Location logic to match Vue ([67fae1f](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/41/commits/67fae1fec964185d13aa8e71b1dbc79f95c39ab9))
- Updated Angular default elements and event to match Vue ([3933fd5](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/41/commits/3933fd5b7b02632fef0dae25ded540486f4f6db5))

### Fixed

- Allow Angular to check elements nested in structural directives ([5ceaf6d](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/41/commits/5ceaf6d9ddcef0fc1a7cf9658060d209667ec0a8))
- Audit fix for micromatch module ([87b5733](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/41/commits/87b5733e0c6f78c4e26c3b5c4b79e1829c334dcf))

## 1.1.0 - 2023-07-04

### Added

- No new functionality added

### Changed

- Updated ES Lint dependency version to `>=6.0.0`

## 1.0.0 - 2023-02-06

### Added

- Added dependencies for `@angular-eslint/eslint-plugin` and `@angular-eslint/template-parser`
