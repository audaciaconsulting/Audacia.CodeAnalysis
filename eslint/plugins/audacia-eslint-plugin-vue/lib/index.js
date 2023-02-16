module.exports = {
  rules: {
    'data-test-attribute': require('./rules/data-test-attribute'),
  },
  configs: {
    recommended: require('./configs/recommended'),
  },
  processors: {
    '.vue': require('eslint-plugin-vue/lib/processor'),
  },
};
