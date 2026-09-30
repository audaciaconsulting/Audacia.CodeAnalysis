module.exports = {
    rules: {
      'data-test-attribute': require('./rules/data-test-attribute'),
    },
    configs: {
      recommended: require('./configs/recommended'),
      'flat/typescript': require('./configs/flat/typescript'),
    }
  };
