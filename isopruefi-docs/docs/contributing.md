# Contributing Guide

## Getting Started

Welcome to IsoPrüfi! We appreciate your interest in contributing. Please read this guide before making your first contribution.

### Prerequisites

Before contributing, ensure you have:
- Read the [build setup guide](build.md)
- Completed the initial project setup
- Familiarized yourself with our [development guidelines](guidelines.md)

## Development Workflow

### 1. Branch Creation

Create branches following our naming convention:

```bash
git checkout -b [type]/[description-with-hyphens]
```

**Branch Types:**
- `feat/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Adding/updating tests
- `chore/` - Maintenance tasks

**Examples:**
```bash
git checkout -b feat/temperature-alerts
git checkout -b fix/mqtt-connection-timeout
git checkout -b docs/api-reference-update
```

### 2. Development Process

1. **Make your changes** following our coding standards
2. **Test thoroughly** - run relevant tests for your changes
3. **Update documentation** if needed
4. **Commit with conventional messages** (see below)

### 3. Testing

Run tests before committing:

```bash
# Frontend tests
cd isopruefi-frontend && npm test

# Backend tests  
cd isopruefi-backend && dotnet test

# Arduino tests
cd isopruefi-arduino && pio test -e native
```

### 4. Pre-commit Hooks

Our project uses pre-commit hooks that automatically run:
- Commit message validation (Conventional Commits)
- Code formatting checks
- Basic linting

If hooks fail, fix the issues before committing again.

## Commit Message Standards

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>: <description>

[optional body]

[optional footer]
```

**Allowed Types:**
- `feat` - New features
- `fix` - Bug fixes
- `docs` - Documentation changes
- `style` - Code style/formatting
- `refactor` - Code refactoring
- `test` - Adding/updating tests
- `chore` - Maintenance tasks
- `ci` - CI/CD changes
- `perf` - Performance improvements
- `revert` - Reverting commits

**Examples:**
```
feat: add temperature alert notifications

fix: resolve MQTT connection timeout issue

docs: update API reference with new endpoints

refactor: improve error handling in sensor module
```

## Pull Request Process

### 1. Create Pull Request

1. Push your branch to GitHub
2. Open a Pull Request against the current `sprint` branch
3. Use a descriptive title following commit conventions
4. Fill out the PR template completely

### 2. PR Requirements

Your PR must:
- Pass all automated checks (CI/CD)
- Have clear, descriptive commit messages
- Include tests for new functionality
- Update relevant documentation
- Be reviewed by at least one maintainer

### 3. Review Process

- Address all reviewer feedback
- Keep PR scope focused and manageable
- Squash commits if requested
- Ensure CI passes before merge

## Documentation

### When to Update Documentation
- Adding new features or APIs
- Changing existing behavior
- Fixing bugs that affect user experience
- Adding configuration options

### Documentation Types
- **API changes**: Update `api-reference.md`
- **Setup changes**: Update `build.md` or `docker-dev.md`
- **Architecture changes**: Update relevant arc42 sections
- **New features**: Update main documentation

## Issue Reporting

### Bug Reports
Include:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, Docker version, etc.)
- Relevant log output

### Feature Requests
Include:
- Clear description of the proposed feature
- Use case/problem it solves
- Suggested implementation approach
- Consider alternatives

## Getting Help

- **Technical questions**: Open a GitHub issue
- **Setup problems**: Check [troubleshooting guide](troubleshooting.md)
- **Architecture questions**: Reference [arc42 documentation](documentation/01_introduction_and_goals.md)

## Code of Conduct

- Be respectful and constructive
- Focus on the code, not the person
- Help others learn and improve
- Keep discussions technical and relevant

---

Thank you for contributing to IsoPrüfi! Your efforts help make the project better for everyone.