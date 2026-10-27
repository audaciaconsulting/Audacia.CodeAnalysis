const assert = require('assert');
const path = require('path');
const { ESLint } = require('eslint');
const plugin = require('../../../lib');

const fixtures = path.join(__dirname, '..', '..', 'fixtures');
const config = plugin.configs['flat/recommended'];

const lint = async (file) => {
    // overrideConfigFile: true ignores any eslint.config.js, so only the config under test applies
    const eslint = new ESLint({
        overrideConfigFile: true,
        overrideConfig: config,
        cwd: fixtures,
    });
    const [result] = await eslint.lintFiles([file]);
    return result.messages;
};

describe('flat/recommended config', () => {
    it('no longer exports the legacy eslintrc or angular-eslint presets', () => {
        assert.deepStrictEqual(Object.keys(plugin.configs), ['flat/recommended']);
    });

    it('applies to .html files only', () => {
        config.forEach((c) => assert.deepStrictEqual(c.files, ['**/*.html']));
    });

    it('registers the plugin itself', () => {
        assert.strictEqual(config[0].plugins['@audacia/angular'], plugin);
    });

    it('reports a click handler without a data-test attribute', async () => {
        const messages = await lint('non-compliant/click.component.html');
        assert.strictEqual(messages.length, 1, JSON.stringify(messages));
        assert.strictEqual(messages[0].ruleId, '@audacia/angular/data-test-attribute');
        assert.strictEqual(messages[0].severity, 2);
    });

    it('reports nothing for a compliant template', async () => {
        assert.deepStrictEqual(await lint('compliant/user-card.component.html'), []);
    });
});
