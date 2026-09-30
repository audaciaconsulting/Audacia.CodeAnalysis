const assert = require('assert');
const angular = require('@angular-eslint/eslint-plugin');
const plugin = require('../../../lib');

const config = plugin.configs['flat/typescript'];
const rules = Object.assign({}, ...config.map((c) => c.rules));

describe('flat/typescript config', () => {
    it('is exported from the plugin', () => {
        assert.ok(Array.isArray(config));
    });

    it('applies to .ts files only', () => {
        config.forEach((c) => assert.deepStrictEqual(c.files, ['**/*.ts']));
    });

    it('only references rules that exist in @angular-eslint/eslint-plugin', () => {
        Object.keys(rules).forEach((name) => {
            const ruleName = name.replace('@angular-eslint/', '');
            assert.ok(angular.rules[ruleName], `${name} does not exist`);
        });
    });

    it('sets the agreed severities', () => {
        assert.strictEqual(rules['@angular-eslint/prefer-standalone'], 'error');
        assert.strictEqual(rules['@angular-eslint/component-selector'][0], 'error');
        assert.strictEqual(rules['@angular-eslint/directive-selector'][0], 'error');
        assert.strictEqual(rules['@angular-eslint/component-class-suffix'], 'warn');
        assert.strictEqual(rules['@angular-eslint/directive-class-suffix'], 'warn');
    });
});
