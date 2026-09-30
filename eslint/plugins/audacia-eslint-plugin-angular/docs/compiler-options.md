# Angular compiler options

Strict TypeScript and strict template checking are enforced by the TypeScript and Angular compilers, not by ESLint. This page lists the recommended settings. It contains no lint configuration.

Run `ng build`, `ngc` or `tsc --noEmit` (in CI as well as locally) to enforce these options. ESLint does not report compiler diagnostics.

## `tsconfig.json`

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitOverride": true,
    "noPropertyAccessFromIndexSignature": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

| Option | Purpose |
| --- | --- |
| `strict` | Enables the TypeScript strict family (`strictNullChecks`, `noImplicitAny`, `strictFunctionTypes` and others). |
| `noImplicitOverride` | Requires the `override` keyword when a member overrides a base class member. |
| `noPropertyAccessFromIndexSignature` | Requires bracket access for properties that come from an index signature. |
| `noImplicitReturns` | Reports code paths in a function that do not return a value. |
| `noFallthroughCasesInSwitch` | Reports `switch` cases that fall through. |

## `angularCompilerOptions`

```json
{
  "angularCompilerOptions": {
    "strictTemplates": true,
    "strictInjectionParameters": true,
    "strictInputAccessModifiers": true,
    "extendedDiagnostics": {
      "defaultCategory": "error"
    }
  }
}
```

| Option | Purpose |
| --- | --- |
| `strictTemplates` | Type checks templates using the strictest mode (inputs, outputs, `@for`/`@if` context, DOM events). |
| `strictInjectionParameters` | Makes it an error when the compiler cannot determine the type of an injection parameter. |
| `strictInputAccessModifiers` | Makes it an error for a template to bind to an input whose class member is `private`, `protected` or `readonly`. |
| `extendedDiagnostics` | Controls the severity of extended template diagnostics (for example `nullishCoalescingNotNullable`). `defaultCategory: "error"` makes them blocking. |

## Adopting incrementally

Enabling `strict` or `strictTemplates` on an existing application usually produces many errors. Enable the options one at a time, or set `extendedDiagnostics.defaultCategory` to `warning` first and move to `error` once the warnings are cleared.

## Version notes

Option availability depends on the Angular version. Check the [Angular compiler options reference](https://angular.dev/reference/configs/angular-compiler-options) for the version you use.
