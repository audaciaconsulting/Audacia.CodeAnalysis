module.exports = {
    rules: {
      'data-test-attribute': require('./rules/data-test-attribute'),
    },
    configs: {
      'flat/typescript': require('./configs/flat/typescript'),
      'flat/templates': require('./configs/flat/templates'),
      'flat/recommended': require('./configs/flat/recommended'),
    }
  };
