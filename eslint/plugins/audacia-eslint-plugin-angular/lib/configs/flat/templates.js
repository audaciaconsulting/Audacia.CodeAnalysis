const angular = require('angular-eslint');

// angular-eslint's template parser, plugin, recommended rules and accessibility rules for .html files,
// plus the Audacia custom rule. Inline templates reach these rules through the processor in flat/typescript.
module.exports = [
    ...angular.configs.templateRecommended,
    ...angular.configs.templateAccessibility,
    {
        plugins: {
            '@audacia/angular': {
                rules: { 'data-test-attribute': require('../../rules/data-test-attribute') },
            },
        },
        rules: {
            // Kept enabled until PBI 214236 replaces this rule
            '@audacia/angular/data-test-attribute': 'error',
        },
    },
].map((config) => ({ ...config, files: ['**/*.html'] }));
