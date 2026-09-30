const angular = require('@angular-eslint/eslint-plugin');

module.exports = [
    {
        files: ['**/*.ts'],
        plugins: {
            '@angular-eslint': angular,
        },
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
