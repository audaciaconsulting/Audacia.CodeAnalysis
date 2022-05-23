module.exports = {
  rules: {
    // specify the maximum cyclomatic complexity allowed in a program
    complexity: ['off', 11],

    // disallow reassignment of function parameters
    // disallow parameter object manipulation except for specific exclusions
    // rule: https://eslint.org/docs/rules/no-param-reassign.html
    'no-param-reassign': ['error', {
      props: true,
      ignorePropertyModificationsFor: [
        'acc', // for reduce accumulators
        'accumulator', // for reduce accumulators
        'e', // for e.returnvalue
        'ctx', // for Koa routing
        'req', // for Express requests
        'request', // for Express requests
        'res', // for Express responses
        'response', // for Express responses
        '$scope', // for Angular 1 scopes
        'staticContext', // for ReactRouter context
      ]
    }],

    // disallow usage of configurable warning terms in comments: e.g. todo
    'no-warning-comments': ['warn', { terms: ['todo', 'fixme', 'xxx'], location: 'start' }]
  }
};
