# audacia-eslint-config

The `@audacia/eslint-config` npm package contains some default ESLint configuration for JavaScript and TypeScript projects.

## Getting started

First, install the package from the private npm feed:
`npm install @audacia/eslint-config --save-dev`

Next, modify your `ESLint` config file (usually either `.eslintrc.js` or `.eslintrc.json`) to use the Audacia ESLint config package by adding it to the `extends` property. For example, in `.eslintrc.json` this may look something like:
```json
"extends": [
    "@audacia/eslint-config"
]
```

Finally, ensure that your code editor is set up to use ESLint. Jetbrains Rider comes with ESLint support preinstalled, and there is a plugin for VS Code.
