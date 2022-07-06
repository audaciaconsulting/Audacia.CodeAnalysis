module.exports = {
  env: {
    browser: true,
    es6: true,
  },
  extends: [
    'airbnb-base',
    'airbnb-typescript/base',
    'plugin:import/recommended',
    'plugin:import/typescript',
    require('./rules/best-practices'),
    require('./rules/errors'),
    require('./rules/es6'),
    require('./rules/style'),
    require('./rules/variables'),
    require('./rules/imports'),
  ],
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint'],
};
