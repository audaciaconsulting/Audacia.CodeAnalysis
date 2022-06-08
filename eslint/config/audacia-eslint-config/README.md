# audacia-eslint-config

The `@audacia/eslint-config` npm package contains some default ESLint configuration for JavaScript and TypeScript projects.

## Getting started

First, install the package from the private npm feed:
`npm install @audacia/eslint-config --save-dev`

Next, modify your `ESLint` config file (usually either `.eslintrc.js` or `.eslintrc.json`) to extend the airbnb-base package and the Audacia ESLint config package by adding them to the `extends` property. (Note: the order in which the packages are listed is important; airbnb-base must be added first, followed by the Audacia ESLint config package).

For example, in `.eslintrc.json` this may look something like:

```json
"extends": [
    'airbnb-base', '@audacia/eslint-config'
]
```

Finally, ensure that your code editor is set up to use ESLint. Jetbrains Rider comes with ESLint support preinstalled, and there is a plugin for VS Code.

## Editing This Package
1. After making changes to this package, use `npm pack` to create a local package, and then use `npm i {path to package} --install-links` to test in a consuming app.