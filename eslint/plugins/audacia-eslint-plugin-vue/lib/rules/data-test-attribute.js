const VueUtils = require('eslint-plugin-vue/lib/utils');
const utils = require('eslint-plugin-vue/lib/utils');

const ids = require('../utils/ids');

module.exports = {
  meta: {
    type: 'suggestion',
    docs: {
      description: 'use data-test attributes for test automation',
      category: 'Suggestions',
      recommended: true,
      url: 'https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=/eslint/plugins/audacia-eslint-plugin-vue/docs/rules/data-test-attribute.md&_a=preview'
    },
    fixable: 'code',
    schema: [
      {
        type: 'object',
        properties: {
          testAttribute: {
            type: 'string',
          },
          events: {
            type: 'array',
            items: {
              type: 'string',
            },
          },
          elements: {
            type: 'array',
            items: {
              type: 'string',
            },
          },
          enableFixer: {
            type: 'boolean',
          },
          idLength: {
            type: 'number',
            minimum: 4,
          },
        },
        additionalProperties: false,
      },
    ],
  },
  create(context) {
    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

    function isStringArray(obj) {
      return Array.isArray(obj) && obj.every((e) => typeof e === 'string');
    }

    function parseOptions(options) {
      const defaults = {
        testAttribute: 'data-test',
        elements: ['input', 'button'],
        events: ['click'],
        enableFixer: false,
        idLength: 10,
      };

      if (options) {
        if (typeof options.testAttribute === 'string') {
          defaults.testAttribute = options.testAttribute;
        }

        if (isStringArray(options.events)) {
          defaults.events = options.events;
        }

        if (isStringArray(options.elements)) {
          defaults.elements = options.elements;
        }

        if (Number.isInteger(options.idLength)) {
          defaults.idLength = options.idLength;
        }

        if (typeof options.enableFixer === 'boolean') {
          defaults.enableFixer = options.enableFixer;
        }
      }

      return defaults;
    }

    // ----------------------------------------------------------------------
    // Rule
    // ----------------------------------------------------------------------
    const configuration = parseOptions(context.options[0]);

    function getIncludedEvent(node) {
      const events = (node.startTag.attributes ?? [])
        .filter((a) => a.directive && a.key.name.name === 'on')
        .map((a) => a.key.argument.name)
        .filter((n) => configuration.events.includes(n));

      return events.length > 0
        ? events[0]
        : undefined;
    }

    function isIncludedElement(name) {
      return configuration.elements.includes(name);
    }

    function getDataTestAttribute(element) {
      const attrName = configuration.testAttribute;

      const dataAttr = utils.getAttribute(element, attrName)
          || utils.getDirective(element, 'bind', attrName);

      return dataAttr;
    }

    function isAttributeValid(attribute) {
      if (!attribute || !attribute.value || !attribute.value.value) {
        return false;
      }

      return true;
    }

    function generateFixer(node, attribute) {
      let range;
      let fixValue;

      const id = ids.create(configuration.idLength);

      if (!attribute) {
        // If we don't already have the attribute add it at the end of the start tag
        const endOfStartTag = node.startTag.range[1] - (node.startTag.selfClosing ? 3 : 1);
        range = [endOfStartTag, endOfStartTag];
        fixValue = ` ${configuration.testAttribute}="${id}"`;
      } else if (attribute.value === null) {
        // The attribute exists but has no value so add the id
        range = [attribute.range[1], attribute.range[1]];
        fixValue = `="${id}"`;
      } else if (attribute.value.value === '') {
        // The attribute has an empty value so add the id
        range = [attribute.range[1] - 1, attribute.range[1] - 1];
        fixValue = `${id}`;
      } else {
        // In any other scenario do no return a fix method
        return undefined;
      }

      const fix = (fixer) => fixer.insertTextAfterRange(range, fixValue);

      return fix;
    }

    return VueUtils.defineTemplateBodyVisitor(context, {
      VElement: (node) => {
        let message;

        if (isIncludedElement(node.name)) {
          message = `${node.name} elements should include a '${configuration.testAttribute}' attribute`;
        } else {
          const event = getIncludedEvent(node);

          if (event) {
            message = `Elements with ${event} events should include a '${configuration.testAttribute}' attribute`;
          } else {
            return;
          }
        }

        const attribute = getDataTestAttribute(node);

        if (isAttributeValid(attribute)) {
          return;
        }

        let fix = null;

        if (configuration.enableFixer) {
          fix = generateFixer(node, attribute);
        }

        context.report({
          node,
          message,
          fix,
        });
      },
    });
  },
};
