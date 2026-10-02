const angular = require('angular-eslint');

const files = ['**/*.ts'];

module.exports = [
    // angular-eslint's TypeScript parser, plugin and recommended rules, scoped to .ts files
    ...angular.configs.tsRecommended.map((config) => ({ ...config, files })),
    {
        files,
        // Extracts inline component templates so the template rules lint them
        processor: angular.processInlineTemplates,
        // Placed after the spread above so these severities win
        rules: {
            '@angular-eslint/component-selector': [
                'error',
                { type: 'element', prefix: 'app', style: 'kebab-case' },
            ],
            '@angular-eslint/directive-selector': [
                'error',
                { type: 'attribute', prefix: 'app', style: 'camelCase' },
            ],
            '@angular-eslint/prefer-standalone': 'error',
            '@angular-eslint/component-class-suffix': 'warn',
            '@angular-eslint/directive-class-suffix': 'warn',
        },
    },
];
