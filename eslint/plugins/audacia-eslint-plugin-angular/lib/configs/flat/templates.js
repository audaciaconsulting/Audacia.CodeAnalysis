const angular = require('angular-eslint');

// angular-eslint's template parser, plugin, recommended rules and accessibility rules for .html files.
// Inline templates reach these rules through the processor in flat/typescript.
module.exports = [
    ...angular.configs.templateRecommended,
    ...angular.configs.templateAccessibility,
].map((config) => ({ ...config, files: ['**/*.html'] }));
