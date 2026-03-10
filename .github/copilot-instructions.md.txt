# Audacia Coding Standards for AI

Source: https://standards.audacia.tech/engineering/coding-standards

## How to apply these rules
- **MUST**: follow by default.
- **SHOULD**: follow unless local codebase conventions or a clear technical reason justify otherwise.
- **COULD**: optional improvement; avoid churn.
- If repo conventions are stricter, follow the repo.
- Separate secure coding and automated testing standards exist but are outside this summary.
- Vue-specific content on the site is not publicly readable; do not invent Vue rules.

## Global principles
- Prefer readability, maintainability, low surprise, and minimal accidental complexity.
- Follow official ecosystem conventions where referenced:
  - C#: Microsoft conventions and framework guidance.
  - TypeScript: Airbnb-style guidance enforced via ESLint where possible.
  - HTML/CSS: Google HTML/CSS Style Guide plus Audacia deviations.
- Prefer KISS, YAGNI, DRY.

---

## C#
### Design
- Single responsibility for classes/interfaces.
- Keep interfaces small and focused.
- Use interfaces to decouple implementations.
- Prefer interfaces over base classes for multiple implementations.
- Do not hide inherited members with `new`.
- Do not reference derived types from base classes.
- Avoid bidirectional dependencies.
- Use `record` for data-only types.

### Maintainability
- Keep methods to **10 statements or fewer**.
- Default to **`private` members** and **`internal` types**.
- Do not use magic numbers.
- Keep one variable assignment per statement.
- Avoid explicit `== true` / `== false` checks.
- Do not mutate loop variables inside `for` / `foreach`.
- Always use braces for control flow.
- Always include a `default` branch in `switch`.
- Extract complex expressions into a method/property.
- Do not exceed **4 parameters**.
- Avoid `ref`, `out`, and boolean flag parameters.
- Prefer pattern matching with `is` over `as`.
- Do not leave commented-out code.
- Enable nullable reference types.
- Prefer switch expressions.
- Prefer using declarations.
- Prefer object/collection initializers.
- Avoid nested loops, double negatives, and unnecessary multiple returns.

### Member design
- Properties should be settable in any order.
- Use a method instead of a property when it performs work.
- Do not use mutually exclusive properties.
- Each method/property should do one thing.
- Use `init` setters for values that should be immutable after initialization.
- Prefer returning `IEnumerable<T>` / `ICollection<T>` over concrete collections.
- Strings and collections should not be `null`.
- Use the most specific parameter types possible.
- Consider domain-specific value types over primitives when helpful.

### Framework and layout
- Use C# aliases like `int`, `string`, `bool` instead of `System.*` names.
- Do not hardcode deployment-dependent strings.
- Build at the highest warning level.
- Use `dynamic` only when truly required.
- Prefer `async` / `await` over low-level `Task` composition.
- Use a consistent file layout and defined member ordering.
- Avoid `#region` unless clearly justified.
- Use file-scoped namespaces.
- Prefer global and implicit usings where appropriate.

### Documentation and exceptions
- Comment only complex algorithms or non-obvious decisions.
- Do not use comments for TODO/work tracking.
- Public/internal/protected API docs are optional but helpful.
- Throw exceptions rather than status values where appropriate.
- Use specific exception types and meaningful messages.
- Do not swallow generic exceptions.
- Null-check event handlers and do not raise events with a `null` sender.
- Materialize LINQ results before returning when deferred execution would be risky.

### Data access and performance
- Prefer Entity Framework Core.
- Use `AsNoTracking()` for read-only EF queries.
- Configure maximum string lengths in EF mappings.
- Prefer eager loading and returning `IEnumerable` over leaking `IQueryable`.
- Use `Any()` to test whether an `IEnumerable<T>` is empty.
- Use async mainly for I/O.
- Prefer `Task.Run` for CPU-bound work when needed.
- Avoid mixing `await` with blocking waits like `Task.Wait()`.

### Naming
- Use descriptive names and correct casing.
- Do not use numbers in variables, parameters, or members.
- Do not prefix fields.
- Do not repeat class/enum names in member names.
- Keep naming aligned with related .NET APIs.
- Use noun/adjective names for types and verb-object names for methods.
- Name namespaces by name/layer/verb/feature.
- Name boolean members consistently.
- Name events correctly; prefix event raisers/handlers with `On`.
- Use `_` for intentionally ignored lambda parameters.
- Put extension methods in classes ending with `Extensions`.
- Suffix async methods with `Async` or `TaskAsync`.
- Avoid vague “Data”-like suffixes in entity names.

---

## TypeScript
### General
- Prefer explicit, readable typing.
- Annotate function parameters and return types.
- Add explicit types where inference is unclear.
- Prefer `const` over `let`.
- Use interfaces for object shapes.
- Use classes where object/class modeling is intended.
- Prefer `unknown` over `any`.
- Prefer `readonly` for immutable properties.
- Prefer `undefined` over `null`.
- Prefer optional parameters over overloads differing only by trailing args.

### Maintainability
- Keep methods to **10 statements or fewer**.
- Do not duplicate code.
- Limit nested control depth to **2**.
- Do not exceed **4 parameters**.
- Do not use magic numbers or strings.
- Use enums for fixed sets of constants.
- Allow gaps in enum numeric values.
- Do not comment out code.
- Pin or explicitly range dependency versions in `package.json`.
- Implement error logging for observability.
- Prefer one type per file.
- Enable `strictNullChecks`.
- Optimize imports.

### Naming
- File pattern: `{description}.{type}.{extension}`.
- Use conventional file suffixes.
- Use `PascalCase` for symbols.
- Do not prefix private properties.
- Components: kebab-case selectors/files, class ends with `Component`.
- Directives: camelCase selectors, class ends with `Directive`.
- Pipes: camelCase names, class ends with `Pipe`.
- Modules: class ends with `Module`.
- Prefix component/directive/pipe selectors meaningfully.
- Name modules after their feature/folder.

### Performance
- Lazy-load non-critical resources.

### Angular
- Keep component HTML and styles in separate files.
- Put functionality under a `components` folder.
- Group methods and properties.
- Include a `SharedModule`.
- Prefer including `CommonModule`.
- Feature module name should match its parent directory.
- Feature modules should have their own routing module.
- Include a `CoreModule`.
- Single-use components should be declared in `CoreModule`.
- Prefer `providedIn` over manually populating `providers`.
- Avoid re-importing single-use modules.

### Vue
- Public overview exists, but detailed rules are behind login.
- Do not infer or fabricate Vue-specific standards from this site.

---

## HTML and CSS
### Baseline
- Use Google HTML/CSS Style Guide as the base.
- Audacia deviations:
  - indentation style can follow repo consistency,
  - TODO/action-item comments are discouraged,
  - example markup closes all elements,
  - do not omit optional tags.

### HTML
- Use HTML5.
- Use valid, semantic HTML for its intended purpose.
- Use HTTPS for embedded resources.
- Separate structure, presentation, and behavior.
- Do not use entity references unnecessarily.
- Omit `type` attributes for stylesheets/scripts.
- Avoid unnecessary `id` attributes.
- Use lowercase.
- Remove trailing whitespace.
- Put block/list/table elements on new lines and indent children.
- Use double quotes for attribute values.
- Use UTF-8 without BOM.
- Provide alternative content for multimedia where possible.
- Break long lines and comment only when helpful.

### CSS
- Use valid CSS.
- Use meaningful, concise class names.
- Separate words in class names with hyphens.
- Avoid qualifying class names with element/type selectors.
- Avoid `#id` selectors.
- Use shorthand properties where possible.
- Omit units on zero values.
- Include leading zeroes.
- Prefer 3-character hex values when possible.
- Use HTTPS for embedded resources.
- Use lowercase.
- Avoid `!important` unless genuinely necessary.
- Avoid CSS hacks and user-agent detection.
- Indent block content.
- End every declaration with `;`.
- Put a space after property colons.
- Put a space before declaration blocks.
- Put selectors/declarations/rules on separate lines.
- Use single quotes for attribute selectors and property values.
- Alphabetize declarations when practical.
- Group sections and comment only when helpful.

### CSS selectors for UI automation
- Add selectors to support automated UI tests.
- Use a consistent format.
- Follow the site’s 3-part selector structure:
  - component verb,
  - section,
  - element.
- Use standardized element types.

---

## SQL
### Formatting
- Capitalize SQL keywords.
- Use `camelCase` variable names.
- Use single quotes for string literals.
- Put `SELECT` columns on new lines and indent them.
- Put major clauses on new lines.
- Indent key clause bodies.
- Keep most other keywords on the same line when readable.
- Add extra whitespace/new lines only to improve readability.

### Queries
- End every statement with `;`.
- Use full object names.
- Use table aliases, but not SQL Server keywords as aliases.
- Always specify columns in `INSERT` statements.
- Use transactions.
- Prefer `TRY...CATCH` for error handling.
- Use `SET NOCOUNT ON`.
- Avoid cursors, dynamic SQL, `GOTO`, triggers, and `SELECT *`.
- Wrap stored procedure bodies in `BEGIN...END`.
- Prefer table variables where suitable.
- Use `--` comments.
- Avoid joins with subqueries where clearer alternatives exist.
- Use CTEs for complex queries.

### Naming
- Use `PascalCase` for object names.
- Prefix CTEs with `Cte`.
- Start each new CTE on a new line.
- Do not prefix stored procedure names.
- Name objects descriptively.

---

## AI default behavior
- Apply all **MUST** rules automatically.
- Apply **SHOULD** rules unless the repository already uses a stronger or conflicting local convention.
- Use **COULD** rules only when they improve clarity without causing unnecessary churn.
- When information is unavailable from the public site, state that clearly rather than guessing.
