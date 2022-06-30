module.exports = {
  env: { browser: true, es6: true },
  extends: ['airbnb-base', 'airbnb-typescript/base', './index.js'],
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint'],
};
