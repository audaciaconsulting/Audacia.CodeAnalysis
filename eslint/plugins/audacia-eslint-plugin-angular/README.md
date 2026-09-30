# @audacia/eslint-plugin-angular

An ESLint plugin which provides lint rules for angular projects.

## Installation

Requires ESLint 9 (flat config) and `@angular-eslint` 20, 21 or 22.

Before continuing, make sure you have the [AudaciaPublic](https://dev.azure.com/audacia/Audacia.Public/_artifacts/feed/AudaciaPublic) feed configured.
 You can find more information on how to set this up in the [Audacia wiki](https://dev.azure.com/audacia/Audacia/_wiki/wikis/Audacia.wiki/259/Configuring-a-project-to-access-the-Audacia-NPM-registry).

Install the plugin and its peers:

```sh
npm install @audacia/eslint-plugin-angular @angular-eslint/eslint-plugin @angular-eslint/template-parser --save-dev
```

This package does not install or configure a TypeScript parser. You must supply one, for example [typescript-eslint](https://typescript-eslint.io/) (including `@typescript-eslint/utils`, which `@angular-eslint/eslint-plugin` needs), or get it from a shared base config.

## Usage

Add the `flat/typescript` preset to your `eslint.config.js`. It applies to `**/*.ts` files.

```js
const audaciaAngular = require('@audacia/eslint-plugin-angular');

module.exports = [
    // ... your TypeScript parser and other configs
    ...audaciaAngular.configs['flat/typescript'],
];
```

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

| Rule | Severity |
|------|----------|
| `@angular-eslint/component-selector` (element, kebab-case, prefix `app`) | error |
| `@angular-eslint/directive-selector` (attribute, camelCase, prefix `app`) | error |
| `@angular-eslint/prefer-standalone` | error |
| `@angular-eslint/component-class-suffix` | warn |
| `@angular-eslint/directive-class-suffix` | warn |

### Custom rules

To use the custom rules in this plugin, register it under the `@audacia/angular` name:

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

The legacy eslintrc `recommended` config is still exported but will be removed in 2.0.0.

## Supported Rules

- :wrench: Auto-fix is available with the `--fix` command line option

| Rule | Description |   |
|------|-------------|---|
| [`@audacia/angular/data-test-attribute`](/docs/rules/data-test-attribute.md) | Use data-test attributes for test automation | :wrench: |

## Development

To add new rules to this plugin, you will need to install the dependencies with `npm install`.

### Structure Overview

- `docs` - a markdown file per each rule describing what is for, how it is used, how it is configured, etc
- `lib` - the actual code for the plugin, including the custom rules
- `tests` - a test file per each custom rule

### Adding a Rule

Create a new file in the `lib/rules` folder, with the name of the rule ([see ESLint Docs for recommendations](https://eslint.org/docs/latest/developer-guide/working-with-rules#rule-naming-conventions)).

The rule should have unit tests in the `tests/lib/rules`, named the same as the rule file. Both valid and invalid cases should be tested, along with any auto-fixes where applicable.

There should be detailed documentation provided in a markdown file for each rule in the `docs/rules` folder. Use the `docs/_template.md` to get started. Again, ensure they docs file is named the same as the rule.

You should follow the [ESLint Rules Docs](https://eslint.org/docs/latest/developer-guide/working-with-rules) for how to get started on creating rules.

If suitable, the rule should be added to the recommended config that is provided with this plugin, see `lib/configs/recommended.js`.

The rule should also be added to the above table of [Supported Rules](#supported-rules), marking if it is included in the recommended config and if it has an auto-fix.

### Useful Reading

- [ESLint - Working With Plugins](https://eslint.org/docs/latest/developer-guide/working-with-plugins)
- [ESLint - Working with Rules](https://eslint.org/docs/latest/developer-guide/working-with-rules)
- [Angular Eslint AST Template - AST Guide](https://astexplorer.net/#/gist/2f7851a58edbdc4aefacb48960923e06/80d646d3e697163b46e948c3139d4a2358347c05)
