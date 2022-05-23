module.exports = {
  rules:
  {
    // disallow specific imports
    // https://eslint.org/docs/rules/no-restricted-imports
    'no-restricted-imports': ['off', {
      paths: ['rxjs'],
      patterns: []
    }],

    // import sorting
    // https://eslint.org/docs/rules/sort-imports
    'sort-imports': [1, {
      ignoreCase: false,
      ignoreDeclarationSort: false,
      ignoreMemberSort: false,
      memberSyntaxSortOrder: ['none', 'all', 'multiple', 'single']
    }]
  }
};
