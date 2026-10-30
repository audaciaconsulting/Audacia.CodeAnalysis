import templateParser from '@angular-eslint/template-parser';

// The Audacia custom rules for Angular templates. Inline templates reach these rules when the consumer's
// config includes the angular-eslint inline-template processor.
export default (plugin) => [
    {
        files: ['**/*.html'],
        languageOptions: {
            parser: templateParser,
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
