# Architecture Decisions {#section-design-decisions}

# ADR Template

## Title: 
ADR xx: _Short Description_

## Context: 
This section shortly describes the problem or context, the possible options and possibly their pros and cons.

## Decision: 
This section describes our solution and explains our decision.

## Status:
For example accepted, deprecated or superseded.

## Consequences:
This section describes the resulting context, after applying the decision. All consequences should be listed here, not just the "positive" ones. A particular decision may have positive, negative, and neutral consequences, but all of them affect the team and project in the future.


# ADR 0: Example

## Context:
Our frontend project currently uses plain JavaScript (React). Recently, we've encountered recurring bugs caused by the lack of type checking (e.g., undefined errors on objects, incorrect API responses).
The team discussed several options to improve code quality and maintainability:

| Option        | Pros      | Cons      |
|---------------|:----------|:----------|
| Continue with JS | No migration | Less reliable |
| Adopt TypeScript | Strong typing | Migration effort |
| Static Analysis Tool | Lighter | Less widely used |

## Decision:
We decided to gradually adopt TypeScript, starting with new files and later migrating existing modules. The benefits (better error prevention, maintainability, developer productivity) clearly outweigh the migration cost.

## Status:
Accepted (25.07.2025)

## Consequences: 
Positive:

- Fewer runtime errors due to static type checking

- Improved auto-completion and tooling support

- Clearer interfaces for team collaboration

Negative:

- Migration effort for existing JavaScript files

- Developers need to learn TypeScript

- CI builds may initially slow down due to additional checks

Neutral:

- Build configuration needs to be updated (e.g., Babel, tsconfig.json)

- Slightly higher onboarding curve for new developers

# Sources

[Documenting Architecture Desicions by Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)