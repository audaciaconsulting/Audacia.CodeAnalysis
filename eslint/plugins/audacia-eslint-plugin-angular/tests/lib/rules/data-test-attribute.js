const { RuleTester } = require('eslint');
const idsMocker = require('../../utils/ids-mocker');

// use the ids mocker util to import this rule so we can mock the ids require in the rule
const rule = idsMocker.getRuleWithMock('data-test-attribute');

const ruleTester = new RuleTester({
    parser: require.resolve('@angular-eslint/template-parser'),
    parserOptions: {
        ecmaVersion: 2020,
        sourceType: 'module',
    },
    env: {
        browser: true,
        es6: true,
    },
});

ruleTester.run("data-test-attribute", rule, {
    valid: [
        // (click) events
        {
            name: '(click) with test attribute',
            code: '<template><div (click)="foobar()" data-test="test-id"></div></template>',
        },
        {
            name: '(click) with test attribute in different order',
            code: '<template><div data-test="test-id" (click)="foobar()"></div></template>',
        },
        // using on style
        {
            name: 'onclick with test attribute',
            code: '<template><div onclick="foobar()" data-test="test-id"></div></template>',
        },
        {
            name: 'onclick with test attribute in different order',
            code: '<template><div data-test="test-id" onclick="foobar()"></div></template>',
        },
        // input elements
        {
            name: 'input element with test attribute',
            code: '<template><input data-test="test-id" /></template>',
        },
        // multiple events
        {
            name: 'multiple events with test attribute after first event',
            code: '<template><div (click)="foobar()" data-test="test-id"></div></template>',
            options: [{ events: ['click', 'mouseup'] }],
        },
        {
            name: 'multiple events with test attribute after other event',
            code: '<template><div (mouseup)="foobar()" data-test="test-id"></div></template>',
            options: [{ events: ['click', 'mouseup'] }],
        },
        {
            name: 'multiple events with test attribute after both events',
            code: '<template><div (click)="foobar()" (mouseup)="foobar()" data-test="test-id"></div></template>',
            options: [{ events: ['click', 'mouseup'] }],
        },
        {
            name: 'multiple events with test attribute between both events',
            code: '<template><div (click)="foobar()" data-test="test-id" (mouseup)="foobar()"></div></template>',
            options: [{ events: ['click', 'mouseup'] }],
        },
        {
            name: 'multiple events with test attribute before both events',
            code: '<template><div data-test="test-id" (click)="foobar()" (mouseup)="foobar()"></div></template>',
            options: [{ events: ['click', 'mouseup'] }],
        },
        // multiple elements
        {
            name: 'multiple elements with test attribute after first element',
            code: '<template><input data-test="test-id" /></template>',
            options: [{ elements: ['input', 'div'] }],
        },
        {
            name: 'multiple elements with test attribute after other element',
            code: '<template><div data-test="test-id"></div></template>',
            options: [{ elements: ['input', 'div'] }],
        },
        // no click event
        {
            name: 'no click event',
            code: '<template><div></div></template>',
        },
        // not configured event
        {
            name: 'event that is not configured',
            code: '<template><div (mouseup)="foobar()"></div></template>',
        },
        // data-test attribute on other element
        {
            name: 'test attribute on element that is not configured',
            code: '<template><div data-test="test-id"></div></template>',
        },
        // custom attribute (click) events
        {
            name: '(click) with custom test attribute',
            code: '<template><div (click)="foobar()" data-other-attribute="test-id"></div></template>',
            options: [{ testAttribute: 'data-other-attribute' }],
        },
        {
            name: '(click) with custom test attribute in different order',
            code: '<template><div data-other-attribute="test-id" (click)="foobar()"></div></template>',
            options: [{ testAttribute: 'data-other-attribute' }],
        },
        // custom attribute using on style
        {
            name: 'onclick with custom test attribute',
            code: '<template><div onclick="foobar()" data-other-attribute="test-id"></div></template>',
            options: [{ testAttribute: 'data-other-attribute' }],
        },
        {
            name: 'onclick with custom test attribute in different order',
            code: '<template><div data-other-attribute="test-id" onclick="foobar()"></div></template>',
            options: [{ testAttribute: 'data-other-attribute' }],
        },
        // custom attribute input elements
        {
            name: 'input element with custom test attribute',
            code: '<template><input data-other-attribute="test-id"/></template>',
            options: [{ testAttribute: 'data-other-attribute' }]
        },
        // child input elements
        {
            name: 'test attribute check on child elements',
            code: '<template><div><input data-test="test-id" onclick="foobar()"/></div></template>',
        },
        {
            name: 'test attribute check on multiple templates elements',
            code: `
                <template><div><input data-test="test-id" onclick="foobar()"/></div></template>
                <template><div><input data-test="test-id" onclick="foobar()"/></div></template>
                <template><div><input data-test="test-id" onclick="foobar()"/></div></template>`,
        }
    ],
    invalid: [
        // (click) events
        {
            name: '(click) without test attribute',
            code: '<template><div (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: '(click) with no value in test attribute',
            code: '<template><div (click)="foobar()" data-test></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div (click)="foobar()" data-test="test-id"></div></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: '(click) with empty value in test attribute',
            code: '<template><div (click)="foobar()" data-test=""></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div (click)="foobar()" data-test="test-id"></div></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: '(click) with no value in test attribute in different order',
            code: '<template><div data-test (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: '(click) with empty value in test attribute in different order',
            code: '<template><div data-test="" (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true }],
        },
        // input elements
        {
            name: 'input element without test attribute',
            code: '<template><input/></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input data-test="test-id"/></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: 'input element with no value in test attribute',
            code: '<template><input data-test/></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input data-test="test-id"/></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: 'input element with empty value in test attribute',
            code: '<template><input data-test=""/></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input data-test="test-id"/></template>',
            options: [{ enableFixer: true }],
        },
        // input element with (click) event
        {
            name: 'input element with (click) event without test attribute',
            code: '<template><input (click)="foobar()"/></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input data-test="test-id" (click)="foobar()"/></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: 'input element with (click) event with no value in test attribute',
            code: '<template><input (click)="foobar()" data-test /></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input (click)="foobar()" data-test="test-id" /></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: 'input element with (click) event with empty value in test attribute',
            code: '<template><input (click)="foobar()" data-test="" /></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input (click)="foobar()" data-test="test-id" /></template>',
            options: [{ enableFixer: true }],
        },
        // self closing elements
        {
            name: 'self-closing input element without test attribute',
            code: '<template><input (click)="foobar()"/></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input data-test="test-id" (click)="foobar()"/></template>',
            options: [{ enableFixer: true }]
        },
        {
            name: 'self-closing input element with no value in test attribute',
            code: '<template><input (click)="foobar()" data-test /></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input (click)="foobar()" data-test="test-id" /></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: 'self-closing input element with empty value in test attribute',
            code: '<template><input (click)="foobar()" data-test="" /></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input (click)="foobar()" data-test="test-id" /></template>',
            options: [{ enableFixer: true }],
        },
        // multiple events configured
        {
            name: '(click) without test attribute when multiple events configured',
            code: '<template><div (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
        },
        {
            name: '(mouseup) without test attribute when multiple events configured',
            code: '<template><div (mouseup)="foobar()"></div></template>',
            errors: ["Elements with mouseup events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (mouseup)="foobar()"></div></template>',
            options: [{ enableFixer: true, events: ['click', 'mouseup'] }]
        },
        {
            name: 'multiple events configured on element gives first event error',
            code: '<template><div (click)="foobar()" (mouseup)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (click)="foobar()" (mouseup)="foobar()"></div></template>',
            options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
        },
        {
            name: 'multiple events configured on element in different order gives first event error',
            code: '<template><div (mouseup)="foobar()" (click)="foobar()"></div></template>',
            errors: ["Elements with mouseup events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" (mouseup)="foobar()" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
        },
        // using on style
        {
            name: 'onclick without test attribute',
            code: '<template><div onclick="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" onclick="foobar()"></div></template>',
            options: [{ enableFixer: true }],
        },
        {
            name: 'multiple events configured on element in different order gives first onevent error',
            code: '<template><div onmouseup="foobar()" onclick="foobar()"></div></template>',
            errors: ["Elements with mouseup events should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id" onmouseup="foobar()" onclick="foobar()"></div></template>',
            options: [{ enableFixer: true, events: ['click', 'mouseup'] }]
        },
        // multiple elements configured
        {
            name: 'input element without test attribute when multiple elements configured',
            code: '<template><input/></template>',
            errors: ["input elements should include a 'data-test' attribute"],
            output: '<template><input data-test="test-id"/></template>',
            options: [{ enableFixer: true, elements: ['input', 'div'] }],
        },
        {
            name: 'div element without test attribute when multiple elements configured',
            code: '<template><div></div></template>',
            errors: ["div elements should include a 'data-test' attribute"],
            output: '<template><div data-test="test-id"></div></template>',
            options: [{ enableFixer: true, elements: ['input', 'div'] }],
        },
        // enableFixer false
        {
            name: 'no fix provided for (click) event with test attribute when fixer disabled',
            code: '<template><div (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
        },
        {
            name: 'no fix provided for (click) event with no value in test attribute when fixer disabled',
            code: '<template><div (click)="foobar()" data-test></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
        },
        {
            name: 'no fix provided for (click) event with empty value in test attribute when fixer disabled',
            code: '<template><div (click)="foobar()" data-test=""></div></template>',
            errors: ["Elements with click events should include a 'data-test' attribute"],
        },
        // custom attribute (click) events
        {
            name: '(click) event without custom test attribute',
            code: '<template><div (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
            output: '<template><div data-other-attribute="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],

        },
        {
            name: '(click) event with no value in custom test attribute',
            code: '<template><div (click)="foobar()" data-other-attribute></div></template>',
            errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
            output: '<template><div (click)="foobar()" data-other-attribute="test-id"></div></template>',
            options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
        },
        {
            name: '(click) event with empty value in custom test attribute',
            code: '<template><div (click)="foobar()" data-other-attribute=""></div></template>',
            errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
            output: '<template><div (click)="foobar()" data-other-attribute="test-id"></div></template>',
            options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
        },
        {
            name: '(click) event with no value in custom test attribute in different order',
            code: '<template><div data-other-attribute (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
            output: '<template><div data-other-attribute="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }]
        },
        {
            name: '(click) event with empty value in custom test attribute in different order',
            code: '<template><div data-other-attribute="" (click)="foobar()"></div></template>',
            errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
            output: '<template><div data-other-attribute="test-id" (click)="foobar()"></div></template>',
            options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }]
        }
    ],
});