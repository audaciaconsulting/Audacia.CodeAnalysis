import dataTestAttribute from './rules/data-test-attribute.js';
import recommended from './configs/flat/recommended.js';

const plugin = {
    rules: {
      'data-test-attribute': dataTestAttribute,
    },
    configs: {},
  };

// The config registers this same plugin object, so consumers can also register the plugin themselves without a conflict
plugin.configs['flat/recommended'] = recommended(plugin);

export default plugin;