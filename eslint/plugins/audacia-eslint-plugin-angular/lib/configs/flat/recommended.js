// The Audacia custom rules for Angular templates. Inline templates reach these rules when the consumer's
// config includes the angular-eslint inline-template processor.
module.exports = (plugin) => [
    {
        files: ['**/*.html'],
        languageOptions: {
            parser: require('@angular-eslint/template-parser'),
        },
        plugins: {
            '@audacia/angular': plugin,
        },
        rules: {
            // Kept enabled until PBI 214236 replaces this rule
            '@audacia/angular/data-test-attribute': 'error',
        },
    },
];
