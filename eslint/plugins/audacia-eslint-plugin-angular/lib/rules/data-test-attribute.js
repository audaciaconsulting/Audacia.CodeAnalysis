
const ids = require('../utils/ids');

module.exports = {
    meta: {
        type: 'suggestion',
        docs: {
            description: 'use data-test attributes for test automation',
            category: 'Suggestions',
            recommended: true,
            url: 'https://dev.azure.com/audacia/Audacia/_git/Audacia.CodeAnalysis?path=/eslint/plugins/audacia-eslint-plugin-angular/docs/rules/data-test-attribute.md&_a=preview'
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
    create: function (context) {
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
            // Get angular outputs: i.e. `(click)="foobar()"`
            const outputs = node.outputs.map((o) => o.name);

            // Get attributes: i.e. `onclick="foobar()"`
            const attributes = node.attributes?.map((a) => a.name)
                .filter((a) => a.startsWith('on'))
                .map((a) => a.slice(2));

            // Combine outputs and attributes (by name)
            const events = outputs.concat(attributes)
                .filter((n) => configuration.events.includes(n));

            return events.length > 0
                ? events[0]
                : undefined;
        }

        function isIncludedElement(name) {
            return configuration.elements.includes(name);
        }

        function getDataTestAttribute(node) {
            const attrName = configuration.testAttribute;

            const dataAttr = node.attributes?.find((a) => a.name === attrName);

            return dataAttr;
        }

        function isAttributeValid(attribute) {
            if (!attribute || !attribute.valueSpan || !attribute.value) {
                return false;
            }

            return true;
        }

        function getLocation(node) {
            return node.loc;
        }

        function generateFixer(node, attribute) {
            let range;
            let fixValue;

            const id = ids.create(configuration.idLength);

            if (!attribute) {
                // If we don't already have the attribute add it at the start of node
                // We can't add it to the end because we don't know if the span ends on the same line
                const startOfNode = node.sourceSpan.start.offset + node.name.length + 1;

                range = [startOfNode, startOfNode];
                fixValue = ` ${configuration.testAttribute}="${id}"`;
            } else if (attribute.valueSpan === undefined) {
                // The attribute exists but has no value so add the id
                range = [attribute.sourceSpan.end.offset, attribute.sourceSpan.end.offset];
                fixValue = `="${id}"`;
            } else if (attribute.value === '') {
                // The attribute has an empty value so add the id
                range = [attribute.sourceSpan.end.offset - 1, attribute.sourceSpan.end.offset - 1];
                fixValue = `${id}`;
            } else {
                // In any other scenario do no return a fix method
                return undefined;
            }

            const fix = (fixer) => fixer.insertTextAfterRange(range, fixValue);

            return fix;
        }

        // declare the state of the rule
        return {
            onCodePathStart: function (_, node) {
                // At the start of analyzing a code path
            },
            onCodePathEnd: function (_, node) {
                //https://astexplorer.net/#/gist/2f7851a58edbdc4aefacb48960923e06/80d646d3e697163b46e948c3139d4a2358347c05
                let childNodes = node.templateNodes
                    .filter(n => n.type == 'Element$1');

                let message = '';
                // Go through each node
                for (var i = 0; i < childNodes.length; i++) {
                    const currentNode = childNodes[i];

                    // Add any children so they can be checked too
                    childNodes.push(...currentNode.children.filter(n => n.type == 'Element$1'));

                    // Check element first
                    if (isIncludedElement(currentNode.name)) {
                        message = `${currentNode.name} elements should include a '${configuration.testAttribute}' attribute`;
                    }
                    else {
                        const event = getIncludedEvent(currentNode);

                        if (event) {
                            message = `Elements with ${event} events should include a '${configuration.testAttribute}' attribute`;
                        }
                        else {
                            continue;
                        }
                    }

                    const attribute = getDataTestAttribute(currentNode);

                    if (isAttributeValid(attribute)) {
                        continue;
                    }

                    let fix = null;

                    if (configuration.enableFixer) {
                        fix = generateFixer(currentNode, attribute);
                    }

                    const loc = getLocation(currentNode);

                    // Don't return node on it's own as the angular ESlint can be aggressive and highligh the whole file.
                    // Return the location instead to highlight only the issue.
                    context.report({
                        currentNode,
                        message,
                        fix,
                        loc
                    });
                }
            }
        };
    }
};