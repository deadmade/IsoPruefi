# AGENTS.md - IsoPruefi Development Guide

## Build/Lint/Test Commands
- **Root**: `npm run init` (setup all projects), `npm run commitlint` (commit lint)
- **Frontend** (isopruefi-frontend/): `npm run dev` (start dev server), `npm run build` (build), `npm run lint` (ESLint)
- **Backend** (isopruefi-backend/): `dotnet build` (build), `dotnet test` (run all tests), `dotnet test --filter "TestName"` (single test)
- **Docs**: `cd isopruefi-docs && mkdocs serve` (local docs)

## Code Style & Conventions

### TypeScript/React (Frontend)
- Use ES6 imports, prefer named exports
- TypeScript strict mode enabled, nullable types required
- React functional components with hooks
- Component files: PascalCase.tsx, use descriptive names

### C# (Backend)
- .NET 9.0, nullable reference types enabled
- Primary constructors for controllers/services
- File-scoped namespaces
- XML documentation comments required
- Dependency injection via constructor parameters
- Comprehensive error handling with structured logging
- Use `ILogger` for all logging with structured parameters

### Error Handling
- C#: Try-catch with specific exception types, return ProblemDetails for API errors
- TypeScript: Use proper error boundaries and error state management