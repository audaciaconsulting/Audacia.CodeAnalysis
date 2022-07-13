const proxyquire = require('proxyquire');

function getRuleWithMock(ruleName) {
  // Overrides the import of '../utils/ids' with a custom stub
  // The stub mocks the create() method so the returned Id is constant rather than random from nanoid
  const rule = proxyquire(
    // relative path from this util to the rules folder
    `../../lib/rules/${ruleName}`,
    {
      '../utils/ids': {
        create: () => 'test-id',
      },
    },
  );

  return rule;
}

module.exports = {
  getRuleWithMock,
};
