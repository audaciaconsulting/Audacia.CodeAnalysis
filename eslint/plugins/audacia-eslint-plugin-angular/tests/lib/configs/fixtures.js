const assert = require('assert');
const path = require('path');
const { ESLint } = require('eslint');
const plugin = require('../../../lib');

const fixtures = path.join(__dirname, '..', '..', 'fixtures');

const lint = async (preset, file) => {
    // overrideConfigFile: true ignores any eslint.config.js, so only the preset under test applies
    const eslint = new ESLint({
        overrideConfigFile: true,
        overrideConfig: plugin.configs[preset],
        cwd: fixtures,
    });
    const [result] = await eslint.lintFiles([file]);
    return result.messages;
};

// [description, preset, fixture, expected ruleId, expected severity (2 = error, 1 = warn)]
const nonCompliant = [
    ['component selector prefix', 'flat/typescript', 'non-compliant/bad-selector.component.ts', '@angular-eslint/component-selector', 2],
    ['directive selector prefix', 'flat/typescript', 'non-compliant/bad-selector.directive.ts', '@angular-eslint/directive-selector', 2],
    ['non-standalone component', 'flat/typescript', 'non-compliant/not-standalone.component.ts', '@angular-eslint/prefer-standalone', 2],
    ['component class suffix', 'flat/typescript', 'non-compliant/bad-suffix.component.ts', '@angular-eslint/component-class-suffix', 1],
    ['directive class suffix', 'flat/typescript', 'non-compliant/bad-suffix.directive.ts', '@angular-eslint/directive-class-suffix', 1],
    ['external template accessibility', 'flat/templates', 'non-compliant/no-alt.component.html', '@angular-eslint/template/alt-text', 2],
    ['banana in box', 'flat/templates', 'non-compliant/banana.component.html', '@angular-eslint/template/banana-in-box', 2],
    ['structural directive instead of control flow', 'flat/templates', 'non-compliant/ngif.component.html', '@angular-eslint/template/prefer-control-flow', 2],
    ['click handler without data-test attribute', 'flat/templates', 'non-compliant/click.component.html', '@audacia/angular/data-test-attribute', 2],
    ['external template accessibility via recommended', 'flat/recommended', 'non-compliant/no-alt.component.html', '@angular-eslint/template/alt-text', 2],
    ['inline template accessibility', 'flat/recommended', 'non-compliant/inline.component.ts', '@angular-eslint/template/alt-text', 2],
];

const compliant = [
    ['flat/typescript', 'compliant/user-card.component.ts'],
    ['flat/typescript', 'compliant/highlight.directive.ts'],
    ['flat/typescript', 'compliant/inline.component.ts'],
    ['flat/templates', 'compliant/user-card.component.html'],
    ['flat/recommended', 'compliant/user-card.component.ts'],
    ['flat/recommended', 'compliant/user-card.component.html'],
];

describe('flat configs linting fixtures', () => {
    nonCompliant.forEach(([description, preset, file, ruleId, severity]) => {
        it(`${preset} reports ${description} in ${file}`, async () => {
            const messages = await lint(preset, file);
            const matches = messages.filter((m) => m.ruleId === ruleId);
            assert.strictEqual(matches.length, 1, JSON.stringify(messages));
            assert.strictEqual(matches[0].severity, severity);
        });
    });

    compliant.forEach(([preset, file]) => {
        it(`${preset} reports nothing for ${file}`, async () => {
            assert.deepStrictEqual(await lint(preset, file), []);
        });
    });
});
