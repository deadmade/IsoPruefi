# Testing Strategy

## Overview

IsoPrüfi uses a multi-layered testing approach across all components to ensure reliability and quality.

## Test Types

### Unit Tests
- **Backend**: NUnit framework with FluentAssertions and Moq for mocking
- **Arduino**: Unity framework for embedded testing on native platform
- **Frontend**: Not currently implemented

### Integration Tests
- **Backend**: Full API testing with real database connections
- **Load Tests**: Performance testing with realistic data loads

## Running Tests

### Backend Tests
```bash
cd isopruefi-backend
dotnet test UnitTests/UnitTests.csproj
dotnet test IntegrationTests/IntegrationTests.csproj
dotnet test LoadTests/LoadTests.csproj
```

### Arduino Tests
```bash
cd isopruefi-arduino
pio test -e native
```

## CI/CD Testing

All tests run automatically on GitHub Actions:
- Unit tests on every push
- Integration tests on pull requests
- Docker build tests for deployment verification