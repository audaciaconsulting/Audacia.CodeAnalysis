# @audacia/eslint-plugin-angular

An ESLint plugin which provides lint rules for angular projects.

## Installation

Requires ESLint 10 (flat config) and `@angular-eslint` 21.3+ or 22.

Before continuing, make sure you have the [AudaciaPublic](https://dev.azure.com/audacia/Audacia.Public/_artifacts/feed/AudaciaPublic) feed configured.
 You can find more information on how to set this up in the [Audacia wiki](https://dev.azure.com/audacia/Audacia/_wiki/wikis/Audacia.wiki/259/Configuring-a-project-to-access-the-Audacia-NPM-registry).

Install the plugin and its peers:

```sh
npm install @audacia/eslint-plugin-angular angular-eslint @angular-eslint/eslint-plugin @angular-eslint/template-parser typescript-eslint @typescript-eslint/utils --save-dev
```

The presets build on the [`angular-eslint`](https://github.com/angular-eslint/angular-eslint) package (`ng add angular-eslint` installs it), which brings the TypeScript parser, the template rules plugin and the inline-template processor. `angular-eslint` needs `typescript-eslint` `^8`, and `@typescript-eslint/utils` must be `>=8.56.0` for ESLint 10.

## Usage

Add `flat/recommended` to your `eslint.config.js`. It combines `flat/typescript` (`**/*.ts`) and `flat/templates` (`**/*.html`). You can also use either on its own.

```js
const audaciaAngular = require('@audacia/eslint-plugin-angular');

module.exports = [
    // ... your other configs
    ...audaciaAngular.configs['flat/recommended'],
];
```

| Preset | Applies to | Contents |
|--------|------------|----------|
| `flat/typescript` | `**/*.ts` | `angular-eslint` `tsRecommended` (TypeScript parser, `@angular-eslint` plugin, recommended rules), the inline-template processor, then the Audacia rules below |
| `flat/templates` | `**/*.html` | `angular-eslint` template parser, `templateRecommended`, `templateAccessibility` and the `@audacia/angular/data-test-attribute` rule (`error`) |
| `flat/recommended` | both | `flat/typescript` and `flat/templates` |

Inline templates (`template: \`...\``) are extracted by the processor in `flat/typescript` and linted with the `flat/templates` rules, so use `flat/recommended` (or both presets) to lint them.

The preset uses `app` as the selector prefix. To change it, or to override any other rule, add a later config object:

```js
module.exports = [
    ...audaciaAngular.configs['flat/typescript'],
    {
        files: ['**/*.ts'],
        rules: {
            '@angular-eslint/component-selector': ['error', { type: 'element', prefix: 'xyz', style: 'kebab-case' }],
            '@angular-eslint/directive-selector': ['error', { type: 'attribute', prefix: 'xyz', style: 'camelCase' }],
            '@angular-eslint/prefer-standalone': 'off', // e.g. NgModule projects
        },
    },
];
```

### `flat/typescript` rules

These are set after the `angular-eslint` recommended rules, so these severities apply.

| Rule | Severity |
|------|----------|
| `@angular-eslint/component-selector` (element, kebab-case, prefix `app`) | error |
| `@angular-eslint/directive-selector` (attribute, camelCase, prefix `app`) | error |
| `@angular-eslint/prefer-standalone` | error |
| `@angular-eslint/component-class-suffix` | warn |
| `@angular-eslint/directive-class-suffix` | warn |

### Custom rules

`flat/templates` already enables `data-test-attribute`. To use the custom rules without the presets, register the plugin under the `@audacia/angular` name:

```js
module.exports = [
    {
        files: ['**/*.html'],
        languageOptions: { parser: require('@angular-eslint/template-parser') },
        plugins: { '@audacia/angular': require('@audacia/eslint-plugin-angular') },
        rules: { '@audacia/angular/data-test-attribute': 'error' },
    },
];
```

## Migrating from 1.x

2.0.0 is a breaking release:

- The legacy eslintrc config is removed (`plugin:@audacia/angular/recommended`, `lib/configs/base.js` and `lib/configs/recommended.js`). ESLint 10 cannot load eslintrc, so use `eslint.config.js`.
- ESLint `^10.0.0` is required, with Node `^20.19.0 || ^22.13.0 || >=24`.
- `@angular-eslint/eslint-plugin` and `@angular-eslint/template-parser` must be `^21.3.0 || ^22.0.0`. Angular 20 is no longer supported; stay on 1.x or upgrade Angular first.
- `@typescript-eslint/utils` `>=8.56.0` is required.
- `data-test-attribute` is still enabled as `error` by `flat/templates` (and so `flat/recommended`), until it is replaced by PBI 214236. Turn it off with `'@audacia/angular/data-test-attribute': 'off'` in a later config object.

## Supported Rules

- :wrench: Auto-fix is available with the `--fix` command line option

| Rule | Description |   |
|------|-------------|---|
| [`@audacia/angular/data-test-attribute`](/docs/rules/data-test-attribute.md) | Use data-test attributes for test automation | :wrench: |

## Development

To add new rules to this plugin, you will need to install the dependencies with `npm install`.

### Structure Overview

- `docs` - a markdown file per each rule describing what is for, how it is used, how it is configured, etc
- `tests/fixtures` - compliant and non-compliant components and templates linted by `tests/lib/configs/fixtures.js`
- `lib` - the actual code for the plugin, including the custom rules
- `tests` - a test file per each custom rule

### Adding a Rule

Create a new file in the `lib/rules` folder, with the name of the rule ([see ESLint Docs for recommendations](https://eslint.org/docs/latest/developer-guide/working-with-rules#rule-naming-conventions)).

The rule should have unit tests in the `tests/lib/rules`, named the same as the rule file. Both valid and invalid cases should be tested, along with any auto-fixes where applicable.

There should be detailed documentation provided in a markdown file for each rule in the `docs/rules` folder. Use the `docs/_template.md` to get started. Again, ensure they docs file is named the same as the rule.

You should follow the [ESLint Rules Docs](https://eslint.org/docs/latest/developer-guide/working-with-rules) for how to get started on creating rules.

If suitable, the rule should be added to a flat config that is provided with this plugin, see `lib/configs/flat/`.

The rule should also be added to the above table of [Supported Rules](#supported-rules), marking if it is included in a preset and if it has an auto-fix.

### Useful Reading

- [ESLint - Working With Plugins](https://eslint.org/docs/latest/developer-guide/working-with-plugins)
- [ESLint - Working with Rules](https://eslint.org/docs/latest/developer-guide/working-with-rules)
- [Angular Eslint AST Template - AST Guide](https://astexplorer.net/#/gist/2f7851a58edbdc4aefacb48960923e06/80d646d3e697163b46e948c3139d4a2358347c05)
