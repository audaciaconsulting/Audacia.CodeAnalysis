﻿# CHANGELOG

# 1.12.1 - 2025-07-23
### Added
- No new functionality added

### Changed
- Fix Magic Number analyzer throwing exception when re-declaring a variable ([6cfca458](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/commit/6cfca45873bd16ffbbe2a11904f69cb0f28baafc))

## 1.12.0 - 2025-01-02
### Added
- Added new rule "SuppressionRequiresJustification" (ACL1018).
  -  This extends StyleCopAnalyzers Rule SA1404, which checks that the SuppressMessage, MaxMethodLength and MaxParameterCount attributes have a value supplied for their Justification argument.
  - [feat: ACL1018 - Code analysis suppression attribute requires Justification #183011 #66](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/66)
- Added new rule "AvoidBooleanParameters" (ACL1017).
    - This is similar to AV1564 but excludes positional syntax records, treating them as properties rather than parameters.
    - [feat: exclude records defined using positional syntax #151091 #37](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/37)
### Changed
- Updated rule ACL1010 to issue a project-level warning if nullable reference types are disabled, avoiding file-wide highlighting and specific code markers.


## 1.11.2 - 2024-11-01
### Added
- No new functionality added

### Changed
- Wording updated to the messages on ACL1012 to replace the usage of the word 'Where' (as in Where Clauses) ([9ab0cb7](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/commit/9ab0cb77308594955cfbb0141d688323e7a08db9))
- Nested control statements analyser logic has been updated to include ELSE IF and ELSE clauses on IF statements ([24525f5](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/commit/24525f54331a6c4d9fb52117db7d5a696c8fa269))

## 1.11.0 - 2024-09-25
### Added
- No new functionality added

### Changed
- Ignore "this" parameters from extension method parameter count ([e906e91](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/32/commits/e906e9133bc539d031c8c0db49f77c900216dbfe))
- Checks on magic number for iterators and select statements ([5585109](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/32/commits/558510915cd5a39fc6815e549772db7b4a225582))

## 1.10.0 - 2024-08-15
### Added
- Added new rule "DoNotUseProducesResponseTypeWithTypedResults" (ACL1015) ([dc9869a](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/31/commits/dc9869a388a3343ff6bedb613b224ec9a6205e86))
- Added new rule "UseTypedResultsInsteadOfIActionResult " (ACL1016) ([dc9869a](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/31/commits/dc9869a388a3343ff6bedb613b224ec9a6205e86))

### Changed
- Allow `MaxParameterCount` attribute on primary class constructors (ACL1003) ([#30]https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/30)

## 1.9.0 - 2024-06-10
### Added
- Added new rule "DoNotUseNumberInIdentifierName" (ACL1014) ([026278f](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/28/commits/026278fa0f9ce31b0092b0b507f23ef793970061))
- Added new rule "UseRecordTypes" (ACL1013) ([86cb060](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/24/commits/86cb06039e5156f56c0e1341ce81cabf6d2e8176))

### Changed
- Turned on Warnings As Errors ([01779c4](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/23/commits/01779c49a8c0ffe4ab6f8cfa30c59b84e226e747))

## 1.7.3 - 2024-02-09
### Added
- Added help link urls for readme ([6ccd4f2](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/15/commits/6ccd4f245a4ab2c66b35b43e80dbf0a0caa27613))

### Changed
- Do not raise method statement count rule from a primary constructor (ACL1002) ([ef4ce46](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/16/commits/ef4ce4679da0c30e28c3c3e4f0d3a098ccf5242f))

## 1.7.0 - 2024-02-05
### Added
- No new functionality added

### Changed
- Excluded logging statements from method statement count ([6ea1401](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/8/commits/6ea1401e45200b151faed6ec4ef0416709130abb))
- Cancellation Token to be excluded from parameter count ([89d500d](https://github.com/audaciaconsulting/Audacia.CodeAnalysis/pull/9/commits/89d500da8f5c0ba21a865ded2dc791fd2323fd49))
