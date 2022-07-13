# @audacia/eslint-plugin-vue

An ESLint plugin which provides lint rules for Vue projects.

## Installation

You'll first need to install [eslint-plugin-vue](https://eslint.vuejs.org/user-guide/#installation).

Next, install `@audacia/eslint-plugin-vue`:

```sh
npm install @audacia/eslint-plugin-vue --save-dev
```

## Usage

It is recommended that you install the plugin using the provided config, ensuring all of the rules are enabled. To do so, add `plugin:@audacia/vue/recommended` to the `extends` section of your ESLint configuration.

```json
{
    "extends": [
        // ... other configs
        "plugin:@audacia/vue/recommended"
    ]
}
```

Or, if you wish to manually configure the rules, add `@audacia/vue` to the `plugins` section of your ESLint configuration.

```json
{
    "plugins": [ "@audacia/vue" ]
}
```

You can configure and override rules from this plugin by using `@audacia/vue/<rule_name>` in the `rules` section of your ESLint configuration.

```json
{
    "rules": {
        "@audacia/vue/example-rule": [ "off" ]
    }
}
```

## Supported Rules

- :white_check_mark: Included in the `recommended` config provided with the plugin
- :wrench: Auto-fix is available with the `--fix` command line option

| Rule | Description |   |   |
|------|-------------|---|---|
| [`@audacia/vue/data-test-attribute`](/docs/rules/data-test-attribute.md) | use data-test attributes for test automation | :white_check_mark: | :wrench: |

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
- [Vue ESLint Parser - AST Guide](https://github.com/vuejs/vue-eslint-parser/blob/master/docs/ast.md)
