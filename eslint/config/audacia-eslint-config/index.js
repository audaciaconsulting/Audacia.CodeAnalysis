module.exports = {
  env: {
    browser: true,
    es6: true,
  },
  extends: [
    'plugin:import/recommended',
    'plugin:import/typescript',
    require.resolve('./rules/best-practices'),
    require.resolve('./rules/errors'),
    require.resolve('./rules/es6'),
    require.resolve('./rules/style'),
    require.resolve('./rules/variables'),
    require.resolve('./rules/imports'),
    require.resolve('./rules/typescript'),
  ],
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint']
};
