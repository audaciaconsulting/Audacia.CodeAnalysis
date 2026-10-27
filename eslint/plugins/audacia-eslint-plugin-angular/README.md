# @audacia/eslint-plugin-angular

An ESLint plugin which provides lint rules for angular projects.

## Installation

Requires ESLint 10 (flat config) and `@angular-eslint/template-parser` 21.3+ or 22.

Before continuing, make sure you have the [AudaciaPublic](https://dev.azure.com/audacia/Audacia.Public/_artifacts/feed/AudaciaPublic) feed configured.
 You can find more information on how to set this up in the [Audacia wiki](https://dev.azure.com/audacia/Audacia/_wiki/wikis/Audacia.wiki/259/Configuring-a-project-to-access-the-Audacia-NPM-registry).

Install the plugin and its peers:

```sh
npm install @audacia/eslint-plugin-angular @angular-eslint/template-parser --save-dev
```

## Usage

The plugin provides the Audacia custom rules only. Combine its `flat/recommended` config with your own Angular configuration in `eslint.config.js`.

`flat/recommended` applies to `**/*.html`. It sets the `angular-eslint` template parser, registers the plugin as `@audacia/angular` and enables `@audacia/angular/data-test-attribute` (`error`).

For example, with the [`angular-eslint`](https://github.com/angular-eslint/angular-eslint) presets (`ng add angular-eslint` installs them):

```js
const angular = require('angular-eslint');
const tseslint = require('typescript-eslint');
const audaciaAngular = require('@audacia/eslint-plugin-angular');

module.exports = tseslint.config(
    {
        files: ['**/*.ts'],
        extends: [...tseslint.configs.recommended, ...angular.configs.tsRecommended],
        // Extracts inline templates so the template rules, including the Audacia ones, lint them
        processor: angular.processInlineTemplates,
    },
    {
        files: ['**/*.html'],
        extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
    },
    ...audaciaAngular.configs['flat/recommended'],
);
```

Inline templates (`template: \`...\``) are only linted when your config includes the `angular-eslint` inline-template processor.

To change a rule's severity or options, add a later config object:

```js
module.exports = [
    ...audaciaAngular.configs['flat/recommended'],
    {
        files: ['**/*.html'],
        rules: { '@audacia/angular/data-test-attribute': 'off' },
    },
];
```

### Custom rules

To use the rules without `flat/recommended`, register the plugin under the `@audacia/angular` name:

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

- The legacy eslintrc config is removed (`plugin:@audacia/angular/recommended`, `lib/configs/base.js` and `lib/configs/recommended.js`). ESLint 10 cannot load eslintrc, so use `eslint.config.js` with `flat/recommended`, which has the same scope: the template parser and `data-test-attribute` on `.html` files.
- ESLint `^10.0.0` is required, with Node `^20.19.0 || ^22.13.0 || >=24`.
- `@angular-eslint/template-parser` must be `^21.3.0 || ^22.0.0` and is now a peer dependency. Angular 20 is no longer supported; stay on 1.x or upgrade Angular first.
- `@angular-eslint/eslint-plugin` is no longer installed by the plugin. Add `angular-eslint` yourself if you use its rules.
- `data-test-attribute` is still enabled as `error` by `flat/recommended`, until it is replaced by PBI 214236.

## Supported Rules

- :wrench: Auto-fix is available with the `--fix` command line option

| Rule | Description |   |
|------|-------------|---|
| [`@audacia/angular/data-test-attribute`](/docs/rules/data-test-attribute.md) | Use data-test attributes for test automation | :wrench: |

## Development

To add new rules to this plugin, you will need to install the dependencies with `npm install`.

### Structure Overview

- `docs` - a markdown file per each rule describing what is for, how it is used, how it is configured, etc
- `tests/fixtures` - compliant and non-compliant templates linted by `tests/lib/configs/fixtures.js`
- `lib` - the actual code for the plugin, including the custom rules
- `tests` - a test file per each custom rule

### Adding a Rule

Create a new file in the `lib/rules` folder, with the name of the rule ([see ESLint Docs for recommendations](https://eslint.org/docs/latest/developer-guide/working-with-rules#rule-naming-conventions)).

The rule should have unit tests in the `tests/lib/rules`, named the same as the rule file. Both valid and invalid cases should be tested, along with any auto-fixes where applicable.

There should be detailed documentation provided in a markdown file for each rule in the `docs/rules` folder. Use the `docs/_template.md` to get started. Again, ensure they docs file is named the same as the rule.

You should follow the [ESLint Rules Docs](https://eslint.org/docs/latest/developer-guide/working-with-rules) for how to get started on creating rules.

If suitable, the rule should be enabled in the `flat/recommended` config, see `lib/configs/flat/recommended.js`.

The rule should also be added to the above table of [Supported Rules](#supported-rules), marking if it is included in a preset and if it has an auto-fix.

### Useful Reading

- [ESLint - Working With Plugins](https://eslint.org/docs/latest/developer-guide/working-with-plugins)
- [ESLint - Working with Rules](https://eslint.org/docs/latest/developer-guide/working-with-rules)
- [Angular Eslint AST Template - AST Guide](https://astexplorer.net/#/gist/2f7851a58edbdc4aefacb48960923e06/80d646d3e697163b46e948c3139d4a2358347c05)
