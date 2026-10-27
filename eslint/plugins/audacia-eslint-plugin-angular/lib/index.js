const plugin = {
    rules: {
      'data-test-attribute': require('./rules/data-test-attribute'),
    },
    configs: {},
  };

// The config registers this same plugin object, so consumers can also register the plugin themselves without a conflict
plugin.configs['flat/recommended'] = require('./configs/flat/recommended')(plugin);

module.exports = plugin;
