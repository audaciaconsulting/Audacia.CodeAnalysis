# audacia-eslint-config

The `@audacia/eslint-config` npm package contains some default ESLint configuration for JavaScript and TypeScript projects.

## Getting started

First, install the package from the private npm feed:
`npm install @audacia/eslint-config --save-dev`

The Audacia package has a number of peer dependencies which must also be installed:
`npm install eslint eslint-plugin-import @typescript-eslint/eslint-plugin @typescript-eslint/parser --save-dev`

Next, modify your `ESLint` config file (usually either `.eslintrc.js` or `.eslintrc.json`) to extend the Audacia ESLint config package by adding it to the `extends` property. You also need to tell `ESLint` where your TypeScript configuration is located (i.e. your `tsconfig.json` file):

For example, in `.eslintrc.json` this may look something like:

```json
"extends": [
    '@audacia/eslint-config'
],
"parserOptions": {
    "project": './tsconfig.json'
}
```

Finally, ensure that your code editor is set up to use ESLint. Jetbrains Rider comes with ESLint support preinstalled, and there is a plugin for VS Code.

## Editing This Package

1. After making changes to this package, use `npm pack` to create a local package, and then use `npm i {path to package} --install-links` to test in a consuming app.