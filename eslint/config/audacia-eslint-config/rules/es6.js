module.exports = {
  rules:
  {
    // disallow specific imports
    // https://eslint.org/docs/rules/no-restricted-imports
    'no-restricted-imports': ['off', {
      paths: ['rxjs'],
      patterns: []
    }]
  }
};
