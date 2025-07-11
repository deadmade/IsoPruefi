## Commit Messages

### How should my commit messages look like?

Our repo follows the <a href="https://www.conventionalcommits.org/en/v1.0.0/">Conventional Commits</a> guidelines.

Allowed commit types are specified as following:

- feat -> Introduces a new features
- fix -> Fixes a bug
- docs -> Updates on the docs
- chore -> Updates a grunt task; no-production code change
- style -> Formatting code style (missing semicolon, prettier execution, etc)
- refactor -> Refactoring existing code e.g. renaming a variable, reworking a function
- ci -> CI Tasks e.g. adding a hook
- test -> Adding new tests, refactoring tests, deleting old tests
- revert -> Revert old commits
- perf -> Performance related refactoring, without functional changes

## Branch Naming

Your branche names should follow this style:

[commit-type]/[topic-of-branch-seperated-by-hyphen]

F.e. if you want to introduce a new cool type of button your branch should have the name:

feat/cool-new-button
