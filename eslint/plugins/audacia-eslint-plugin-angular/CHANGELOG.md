# Changelog

## 2.0.0 - 2026-09-30

### Added

- Added `flat/typescript` flat config: `angular-eslint` `tsRecommended`, the inline-template processor, and selector, `prefer-standalone` and class suffix rules
- Added `flat/templates` flat config: `angular-eslint` template parser, `templateRecommended`, `templateAccessibility` and `data-test-attribute`
- Added `flat/recommended` flat config combining `flat/typescript` and `flat/templates`
- Added fixture tests that lint compliant and non-compliant components, external templates and inline templates through ESLint
- Added `docs/compiler-options.md` with recommended Angular compiler options

### Changed

- **Breaking:** Requires ESLint `^10.0.0` and Node `^20.19.0 || ^22.13.0 || >=24`
- **Breaking:** `angular-eslint`, `@angular-eslint/eslint-plugin` and `@angular-eslint/template-parser` `^21.3.0 || ^22.0.0` as peer dependencies. Angular 20 is no longer supported
- **Breaking:** `typescript-eslint` and `@typescript-eslint/utils` `^8.56.0` as peer dependencies
- Updated `data-test-attribute` rule and tests for ESLint 10 and the latest template parser
- Tests run with a 10 second mocha timeout
- Rewrote README for flat config
- Moved `proxyquire` to dev dependencies

### Removed

- **Breaking:** Legacy eslintrc `recommended` config (`lib/configs/base.js`, `lib/configs/recommended.js`)

### Fixed

- Corrected `docs/compiler-options.md`: `tsc --noEmit` does not check templates or `angularCompilerOptions`; `ng build` or `ngc` is required
- Removed duplicate `data-test-attribute` test cases rejected by ESLint 9

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
