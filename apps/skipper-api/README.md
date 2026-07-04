# Skipper API

The backend API for the CritterOps application, built using ASP.NET Core 8.

## Getting Started

### Prerequisites

Ensure you have the .NET SDK (version 8.0 or later) installed. You can verify this by running:

```bash
dotnet --version
```

### Build the Project

To build the project and verify there are no compilation errors:

```bash
dotnet build
```

### Run the Project

To run the application locally:

```bash
dotnet run
```

Or run with a specific launch profile (e.g., HTTP on port `5198`):

```bash
dotnet run --launch-profile http
```

### Endpoints

- **Swagger UI**: [http://localhost:5198/swagger](http://localhost:5198/swagger) (Development environment)
- **Health Check**: [http://localhost:5198/health](http://localhost:5198/health) (Returns `Healthy`)
