# `data-test-attribute`

Requires elements to use a `data-test` attribute for test automation.

## Rule Details

This rule aims to enforce the use of `data-test` attributes on elements that automation tests will interact with, making it easier for them to be found on the page.

The `data-test` attribute must be provided with a value.

If an element is matched multiple times (matches on event and element, or matches on multiple events), then only one data-test attribute is required. The rule will try to match on the elements first, then the events.

:x: Examples of **incorrect** code for this rule:

```html
<input type="text">

<button type="button">Click Me!</button>

<div @click="foobar()" data-test></div>

<div v-on:click="foobar()" data-test=""></div>
```

:white_check_mark: Examples of **correct** code for this rule:

```html
<input type="text" data-test="test-id">

<button type="button" data-test="test-id">Click Me!</button>

<div @click="foobar()" data-test="test-id"></div>

<div v-on:click="foobar()" data-test="test-id"></div>
```

## Options

This rule has an object option.

```ts
type Options = {
  /**
   * The attribute that must be present on an element.
   */
  testAttribute: string,
  /**
   * The elements that are required to have the attribute.
   * These take precedence over the events option.
   */
  elements: string[],
  /**
   * The event types that an element could have that require it to have the attribute.
   * These are detected even if the element they are on is not in the elements option.
   */
  events: string[],
  /**
   * If true, an autofix will be suggested using a randomly generated alphanumeric ID.
   */
  enableFixer: boolean,
  /**
   * The length of the ID that will be generated if the autofix is enabled.
   */
  idLength: number
};

const defaults = {
  testAttribute: 'data-test',
  elements: ['input', 'button'],
  events: ['click'],
  enableFixer: false,
  idLength: 10
};
```

### `testAttribute`

:x: Examples of **incorrect** code for this rule with the `{ "testAttribute": "data-custom-test" }` option:

```html
<input type="text">

<button type="button" data-test="test-id">Click Me!</button>
```

:white_check_mark: Examples of **correct** code for this rule with the `{ "testAttribute": "data-custom-test" }` option:

```html
<input type="text" data-custom-test="test-id">

<button type="button" data-custom-test="test-id">Click Me!</button>
```

### `elements`

:x: Examples of **incorrect** code for this rule with the `{ "elements": ["p"] }` option:

```html
<p>Lorem ipsum dolor, sit amet consectetur adipisicing elit.</p>
```

:white_check_mark: Examples of **correct** code for this rule with the `{ "elements": ["p"] }` option:

```html
<p data-test="test-id">Lorem ipsum dolor, sit amet consectetur adipisicing elit.</p>
```

### `events`

:x: Examples of **incorrect** code for this rule with the `{ "events": ["mousedown", "mouseover"] }` option:

```html
<div @mousedown="foobar()">Do something on mousedown</div>

<div @mouseover="fizzbuzz()">Do something else on mouseover</div>
```

:white_check_mark: Examples of **correct** code for this rule with the `{ "events": ["mousedown", "mouseover"] }` option:

```html
<div @mousedown="foobar()" data-test="test-id">Do something on mousedown</div>

<div @mouseover="fizzbuzz()" data-test="test-id">Do something else on mouseover</div>
```

## When Not To Use It

If you do not use automation testing, then you do not need this rule.

## Further Reading

- https://docs.cypress.io/guides/references/best-practices#Selecting-Elements
