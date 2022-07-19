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
    require.resolve('./rules/best-practices'),
    require.resolve('./rules/errors'),
    require.resolve('./rules/es6'),
    require.resolve('./rules/style'),
    require.resolve('./rules/variables'),
    require.resolve('./rules/imports'),
  ],
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint'],
};
