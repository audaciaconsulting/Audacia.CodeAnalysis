const { RuleTester } = require('eslint');

const idsMocker = require('../../utils/ids-mocker');

// use the ids mocker util to import this rule so we can mock the ids require in the rule
const rule = idsMocker.getRuleWithMock('data-test-attribute');

const tester = new RuleTester({
  parser: require.resolve('vue-eslint-parser'),
});

tester.run('data-test-attribute', rule, {
  valid: [
    // @click events
    {
      name: '@click with test attribute',
      code: '<template><div @click="foobar()" data-test="test-id"></div></template>',
    },
    {
      name: '@click with test attribute in different order',
      code: '<template><div data-test="test-id" @click="foobar()"></div></template>',
    },
    {
      name: '@click.stop with test attribute',
      code: '<template><div @click.stop="foobar()" data-test="test-id"></div></template>',
    },
    // using v-on: style
    {
      name: 'v-on:click with test attribute',
      code: '<template><div v-on:click="foobar()" data-test="test-id"></div></template>',
    },
    {
      name: 'v-on:click with test attribute in different order',
      code: '<template><div data-test="test-id" v-on:click="foobar()"></div></template>',
    },
    // input elements
    {
      name: 'input element with test attribute',
      code: '<template><input data-test="test-id"></template>',
    },
    // multiple events
    {
      name: 'multiple events with test attribute after first event',
      code: '<template><div @click="foobar()" data-test="test-id"></div></template>',
      options: [{ events: ['click', 'mouseup'] }],
    },
    {
      name: 'multiple events with test attribute after other event',
      code: '<template><div @mouseup="foobar()" data-test="test-id"></div></template>',
      options: [{ events: ['click', 'mouseup'] }],
    },
    {
      name: 'multiple events with test attribute after both events',
      code: '<template><div @click="foobar()" @mouseup="foobar()" data-test="test-id"></div></template>',
      options: [{ events: ['click', 'mouseup'] }],
    },
    {
      name: 'multiple events with test attribute between both events',
      code: '<template><div @click="foobar()" data-test="test-id" @mouseup="foobar()"></div></template>',
      options: [{ events: ['click', 'mouseup'] }],
    },
    {
      name: 'multiple events with test attribute before both events',
      code: '<template><div data-test="test-id" @click="foobar()" @mouseup="foobar()"></div></template>',
      options: [{ events: ['click', 'mouseup'] }],
    },
    // multiple elements
    {
      name: 'multiple elements with test attribute after first element',
      code: '<template><input data-test="test-id"></template>',
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
      code: '<template><div @mouseup="foobar()"></div></template>',
      options: [{ events: ['click',] }],
    },
    // data-test attribute on other element
    {
      name: 'test attribute on element that is not configured',
      code: '<template><div data-test="test-id"></div></template>',
    },
    // custom attribute @click events
    {
      name: '@click with custom test attribute',
      code: '<template><div @click="foobar()" data-other-attribute="test-id"></div></template>',
      options: [{ testAttribute: 'data-other-attribute' }],
    },
    {
      name: '@click with custom test attribute in different order',
      code: '<template><div data-other-attribute="test-id" @click="foobar()"></div></template>',
      options: [{ testAttribute: 'data-other-attribute' }],
    },
    // custom attribute @click events (camel case)
    {
      name: '@click with camel case custom test attribute',
      code: '<template><div @click="foobar()" dataOtherAttribute="test-id"></div></template>',
      options: [{ testAttribute: 'dataOtherAttribute' }],
    },
    // custom attribute using v-on: style
    {
      name: 'v-on:click with custom test attribute',
      code: '<template><div v-on:click="foobar()" data-other-attribute="test-id"></div></template>',
      options: [{ testAttribute: 'data-other-attribute' }],
    },
    {
      name: 'v-on:click with custom test attribute in different order',
      code: '<template><div data-other-attribute="test-id" v-on:click="foobar()"></div></template>',
      options: [{ testAttribute: 'data-other-attribute' }],
    },
    // custom attribute input elements
    {
      name: 'input element with custom test attribute',
      code: '<template><input data-other-attribute="test-id"></template>',
      options: [{ testAttribute: 'data-other-attribute' }],
    },
    // custom attribute input elements (camel case)
    {
      name: 'input element with camel case custom test attribute',
      code: '<template><input dataOtherAttribute="test-id"></template>',
      options: [{ testAttribute: 'dataOtherAttribute' }],
    },
  ],
  invalid: [
    // @click events
    {
      name: '@click without test attribute',
      code: '<template><div @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div @click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: '@click with no value in test attribute',
      code: '<template><div @click="foobar()" data-test></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div @click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: '@click with empty value in test attribute',
      code: '<template><div @click="foobar()" data-test=""></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div @click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: '@click.stop without test attribute',
      code: '<template><div @click.stop="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div @click.stop="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: '@click with no value in test attribute in different order',
      code: '<template><div data-test @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div data-test="test-id" @click="foobar()"></div></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: '@click with empty value in test attribute in different order',
      code: '<template><div data-test="" @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div data-test="test-id" @click="foobar()"></div></template>',
      options: [{ enableFixer: true }],
    },
    // input elements
    {
      name: 'input element without test attribute',
      code: '<template><input></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input data-test="test-id"></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'input element with no value in test attribute',
      code: '<template><input data-test></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input data-test="test-id"></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'input element with empty value in test attribute',
      code: '<template><input data-test=""></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input data-test="test-id"></template>',
      options: [{ enableFixer: true }],
    },
    // input element with @click event
    {
      name: 'input element with @click event without test attribute',
      code: '<template><input @click="foobar()"></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input @click="foobar()" data-test="test-id"></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'input element with @click event with no value in test attribute',
      code: '<template><input @click="foobar()" data-test></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input @click="foobar()" data-test="test-id"></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'input element with @click event with empty value in test attribute',
      code: '<template><input @click="foobar()" data-test=""></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input @click="foobar()" data-test="test-id"></template>',
      options: [{ enableFixer: true }],
    },
    // self closing elements
    {
      name: 'self-closing input element without test attribute',
      code: '<template><input @click="foobar()" /></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input @click="foobar()" data-test="test-id" /></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'self-closing input element with no value in test attribute',
      code: '<template><input @click="foobar()" data-test /></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input @click="foobar()" data-test="test-id" /></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'self-closing input element with empty value in test attribute',
      code: '<template><input @click="foobar()" data-test="" /></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input @click="foobar()" data-test="test-id" /></template>',
      options: [{ enableFixer: true }],
    },
    // multiple events configured
    {
      name: '@click without test attribute when multiple events configured',
      code: '<template><div @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div @click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
    },
    {
      name: '@mouseup without test attribute when multiple events configured',
      code: '<template><div @mouseup="foobar()"></div></template>',
      errors: ["Elements with mouseup events should include a 'data-test' attribute"],
      output: '<template><div @mouseup="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
    },
    {
      name: 'multiple events configured on element gives first event error',
      code: '<template><div @click="foobar()" @mouseup="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div @click="foobar()" @mouseup="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
    },
    {
      name: 'multiple events configured on element in different order gives first event error',
      code: '<template><div @mouseup="foobar()" @click="foobar()"></div></template>',
      errors: ["Elements with mouseup events should include a 'data-test' attribute"],
      output: '<template><div @mouseup="foobar()" @click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
    },
    // using v-on: style
    {
      name: 'v-on:click without test attribute',
      code: '<template><div v-on:click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
      output: '<template><div v-on:click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true }],
    },
    {
      name: 'multiple events configured on element in different order gives first v-on:event error',
      code: '<template><div v-on:mouseup="foobar()" v-on:click="foobar()"></div></template>',
      errors: ["Elements with mouseup events should include a 'data-test' attribute"],
      output: '<template><div v-on:mouseup="foobar()" v-on:click="foobar()" data-test="test-id"></div></template>',
      options: [{ enableFixer: true, events: ['click', 'mouseup'] }],
    },
    // multiple elements configured
    {
      name: 'input element without test attribute when multiple elements configured',
      code: '<template><input></template>',
      errors: ["input elements should include a 'data-test' attribute"],
      output: '<template><input data-test="test-id"></template>',
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
      name: 'no fix provided for @click event with test attribute when fixer disabled',
      code: '<template><div @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
    },
    {
      name: 'no fix provided for @click event with no value in test attribute when fixer disabled',
      code: '<template><div @click="foobar()" data-test></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
    },
    {
      name: 'no fix provided for @click event with empty value in test attribute when fixer disabled',
      code: '<template><div @click="foobar()" data-test=""></div></template>',
      errors: ["Elements with click events should include a 'data-test' attribute"],
    },
    // custom attribute @click events
    {
      name: '@click event without custom test attribute',
      code: '<template><div @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
      output: '<template><div @click="foobar()" data-other-attribute="test-id"></div></template>',
      options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
    },
    {
      name: '@click event with no value in custom test attribute',
      code: '<template><div @click="foobar()" data-other-attribute></div></template>',
      errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
      output: '<template><div @click="foobar()" data-other-attribute="test-id"></div></template>',
      options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
    },
    {
      name: '@click event with empty value in custom test attribute',
      code: '<template><div @click="foobar()" data-other-attribute=""></div></template>',
      errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
      output: '<template><div @click="foobar()" data-other-attribute="test-id"></div></template>',
      options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
    },
    {
      name: '@click event with no value in custom test attribute in different order',
      code: '<template><div data-other-attribute @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
      output: '<template><div data-other-attribute="test-id" @click="foobar()"></div></template>',
      options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
    },
    {
      code: '<template><div data-other-attribute="" @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'data-other-attribute' attribute"],
      output: '<template><div data-other-attribute="test-id" @click="foobar()"></div></template>',
      options: [{ enableFixer: true, testAttribute: 'data-other-attribute' }],
    },
    // custom attribute @click events (camel case)
    {
      name: '@click event without camel case custom test attribute',
      code: '<template><div @click="foobar()"></div></template>',
      errors: ["Elements with click events should include a 'dataOtherAttribute' attribute"],
      output: '<template><div @click="foobar()" dataOtherAttribute="test-id"></div></template>',
      options: [{ enableFixer: true, testAttribute: 'dataOtherAttribute' }],
    }
  ],
});
