# AGENTS.md - Developer Guidelines

## Build/Test/Lint Commands
- **Frontend**: `cd isopruefi-frontend && npm run lint` (ESLint), `npm run build` (TypeScript + Vite)
- **Backend**: `cd isopruefi-backend && dotnet test UnitTests/UnitTests.csproj` (single test project), `dotnet build`
- **Arduino**: `cd arduino && pio test -e native` (PlatformIO unit tests), `pio run -e mkrwifi1010` (build)
- **Root**: `npm run init` (setup all projects), `npm run commitlint` (commit message validation)

## Code Style Guidelines
- **C#**: PascalCase for public members, camelCase for private fields, use nullable reference types (`enable`), XML docs for public APIs
- **TypeScript/React**: camelCase variables, PascalCase components, use TypeScript strict mode, prefer function components with hooks
- **Imports**: Use absolute imports in C# (`using Database.EntityFramework`), ES6 imports in TypeScript (`import { } from`)
- **Error Handling**: Use proper exception types in C#, Result patterns for API responses, try-catch for async operations
- **Naming**: Descriptive names, avoid abbreviations, use interfaces with `I` prefix in C# (`IAuthenticationService`)
- **Formatting**: 4 spaces for C#, 2 spaces for TypeScript/JS, use EditorConfig conventions
- **Architecture**: Dependency injection in ASP.NET Core, repository pattern for data access, separate concerns (API/Service/Repository layers)

## Test Structure
- **C# Tests**: NUnit framework with FluentAssertions, Moq for mocking, arrange-act-assert pattern
- **Arduino Tests**: Unity framework for embedded testing on native platform