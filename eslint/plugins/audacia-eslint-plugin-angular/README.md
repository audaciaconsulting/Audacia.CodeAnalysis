# @audacia/eslint-plugin-angular

An ESLint plugin which provides lint rules for angular projects.

## Installation

You'll first need to install [@angular-eslint/eslint-plugin](https://www.npmjs.com/package/@angular-eslint/template-parser).

If the plugin is going to be used with HTML files, you'll also need [@angular-eslint](https://eslint.org/docs/latest/use/getting-started) and [eslint-plugin-html](https://www.npmjs.com/package/eslint-plugin-html). This is because Angular ESLint doesn't parse HTML files by default.

Next, install `@audacia/eslint-plugin-angular`:

```sh
npm install @audacia/eslint-plugin-angular --save-dev
```

## Usage

It is recommended that you install the plugin using the provided config, ensuring all of the rules are enabled. To do so, add `plugin:@audacia/angular/recommended` to the `extends` section of your ESLint configuration.

```json
{
    "extends": [
        // ... other configs
        "plugin:@audacia/angular/recommended"
    ]
}
```

Or, if you wish to manually configure the rules, add `@audacia/angular` to the `plugins` section of your ESLint configuration.

```json
{
    "plugins": [ "@audacia/angular" ]
}
```

You can configure and override rules from this plugin by using `@audacia/angular/<rule_name>` in the `rules` section of your ESLint configuration.

```json
{
    "rules": {
        "@audacia/angular/example-rule": [ "off" ]
    }
}
```

## Supported Rules

- :white_check_mark: Included in the `recommended` config provided with the plugin
- :wrench: Auto-fix is available with the `--fix` command line option

| Rule | Description |   |   |
|------|-------------|---|---|
| [`@audacia/angular/data-test-attribute`](/docs/rules/data-test-attribute.md) | Use data-test attributes for test automation | :white_check_mark: | :wrench: |

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
