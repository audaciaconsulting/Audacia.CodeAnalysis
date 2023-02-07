module.exports = {
    parser: require.resolve('@angular-eslint/template-parser'),
    parserOptions: {
        ecmaVersion: 2020,
        sourceType: 'module',
    },
    env: {
        browser: true,
        es6: true,
    },
    plugins: [
        "html",
        "@angular-eslint",
        "@audacia/eslint-plugin-angular"
    ],
    rules: {},
};
