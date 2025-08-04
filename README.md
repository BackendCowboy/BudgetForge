# BudgetForge 💰

A DevOps-driven personal finance tracker built with C# and ASP.NET Core, designed for scalability and enterprise-grade practices.

## 🚀 Project Overview

BudgetForge is a cloud-native budget tracking application that emphasizes clean API design, strong backend logic, and full DevOps automation. Built with modern C# practices and designed to scale from personal use to multi-user SaaS.

## 🏗️ Architecture

### Clean Architecture Implementation
```
BudgetForge/
├── src/
│   ├── BudgetForge.Domain/        # Core business rules and entities
│   ├── BudgetForge.Application/   # Business logic and use cases
│   ├── BudgetForge.Infrastructure/# External services (DB, email, etc.)
│   └── BudgetForge.Api/           # Web API controllers and middleware
├── tests/                         # Unit and integration tests
├── docs/                          # Documentation
└── docker/                        # Container configurations
```

### Tech Stack
- **Backend:** C# (ASP.NET Core 8)
- **Database:** PostgreSQL (planned)
- **Cloud:** AWS (planned)
- **Containers:** Docker + Kubernetes (planned)
- **Authentication:** JWT + ASP.NET Identity (planned)
- **Monitoring:** Prometheus + Grafana (planned)

## ✨ Current Features

### User Management API
- ✅ **Create Users** - Register new users with validation
- ✅ **Get All Users** - Retrieve all users
- ✅ **Get User by ID** - Fetch specific user details
- ✅ **Update Users** - Modify user information
- ✅ **Soft Delete** - Deactivate user accounts
- ✅ **Login Tracking** - Record user login timestamps

### API Features
- ✅ **Input Validation** - Required field validation
- ✅ **Business Rules** - Duplicate email prevention
- ✅ **Error Handling** - Proper HTTP status codes
- ✅ **Audit Trails** - Creation and update timestamps
- ✅ **Swagger Documentation** - Interactive API docs

## 🛠️ Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio Code or Visual Studio 2022
- Git

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/[your-username]/BudgetForge.git
cd BudgetForge
```

2. **Restore dependencies**
```bash
dotnet restore
```

3. **Build the solution**
```bash
dotnet build
```

4. **Run the API**
```bash
cd src/BudgetForge.Api
dotnet run
```

5. **Open Swagger UI**
Navigate to `https://localhost:5001/swagger` to explore the API

## 📚 API Documentation

### User Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | Get all users |
| POST | `/api/users` | Create new user |
| GET | `/api/users/{id}` | Get user by ID |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Deactivate user |
| POST | `/api/users/{id}/login` | Record login |

### Example: Create User
```json
POST /api/users
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com"
}
```

## 🧪 Testing the API

Use the built-in Swagger UI at `/swagger` or test endpoints directly:

```bash
# Get all users
curl -X GET "https://localhost:5001/api/users"

# Create a user
curl -X POST "https://localhost:5001/api/users" \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Jane","lastName":"Smith","email":"jane@example.com"}'
```

## 🎯 Roadmap

### Phase 1: Foundation ✅
- [x] Clean architecture setup
- [x] Basic user management
- [x] API documentation
- [x] Input validation

### Phase 2: Core Features (In Progress)
- [ ] PostgreSQL database integration
- [ ] JWT authentication system
- [ ] Account and transaction entities
- [ ] Budget management

### Phase 3: Advanced Features (Planned)
- [ ] Real-time notifications
- [ ] File import (bank statements)
- [ ] Analytics and reporting
- [ ] Multi-tenant support

### Phase 4: DevOps & Cloud (Planned)
- [ ] Docker containerization
- [ ] Kubernetes deployment
- [ ] AWS infrastructure (Terraform)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Monitoring (Prometheus + Grafana)

## 🏆 Learning Objectives

This project demonstrates:
- **Clean Architecture** principles
- **RESTful API** design
- **Modern C#** development practices
- **Domain-Driven Design** concepts
- **Enterprise-grade** project structure
- **DevOps** methodologies

## 🤝 Contributing

This is a learning project, but feedback and suggestions are welcome!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

This project is for educational purposes. See LICENSE file for details.

## 🔗 Connect

Built as part of a C# learning journey - follow the progress!

---

**BudgetForge** - Where financial management meets modern development practices! 💰🚀