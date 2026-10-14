# Acxiom CRM

Enterprise Customer Relationship Management (CRM) system built with **ASP.NET Core 10 MVC**, **Entity Framework Core**, and **ASP.NET Core Identity** featuring Role-Based Access Control (RBAC).

---

## 🚀 Features

- **Role-Based Access Control (RBAC)**: Distinct views and capabilities for **Admin**, **Manager**, and **Sales Executive** roles.
- **Identity & Authentication**: Secure sign-in, session management, account lockout protection, and CSRF defense via antiforgery tokens.
- **Automated Database Seeding**: Configurable seeding for default roles and initial administrator account on startup.
- **Responsive Dashboard**: Modern UI layouts tailored per user authorization level.

---

## 🛠️ Tech Stack

- **Framework**: [.NET 10 (ASP.NET Core MVC)](https://dotnet.microsoft.com/)
- **ORM**: Entity Framework Core 10
- **Database**: Microsoft SQL Server / LocalDB
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap 5, Modern CSS & jQuery Validation

---

## 📁 Solution Architecture

```text
AcxiomCRM/
├── AcxiomCRM.slnx                      # Modern solution definition
├── AcxiomCRM/
│   ├── Controllers/                    # MVC Controllers (Account, Home, Admin, Manager, Sales)
│   ├── Data/                           # ApplicationDbContext & Entity configurations
│   ├── Migrations/                     # EF Core migration history
│   ├── Models/                         # Data and view models
│   ├── Services/                       # Background services & DbSeeder
│   ├── ViewModels/                     # Request and form view models
│   ├── Views/                          # Razor views (Admin, Manager, Sales, Account, Shared)
│   ├── wwwroot/                        # Static assets (CSS, JS, libraries)
│   ├── appsettings.json                # Production configuration template
│   └── Program.cs                      # Dependency injection & HTTP pipeline
└── AcxiomCRM_Complete_Project_Assignment (1).pdf # Project specification
```

---

## ⚙️ Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express, Developer edition, or LocalDB)

### 1. Clone the Repository

```bash
git clone https://github.com/Rohith-M123/ACXIOM_CRM.git
cd ACXIOM_CRM
```

### 2. Configure Database & Seed Admin

Create or edit `AcxiomCRM/appsettings.Development.json` (ignored from version control for security):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=AcxiomCRM;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "SeedAdmin": {
    "Email": "admin@acxiomcrm.local",
    "Password": "YourStrongPassword123!"
  }
}
```

### 3. Apply Migrations

```bash
dotnet ef database update --project AcxiomCRM
```

### 4. Run the Project

```bash
dotnet run --project AcxiomCRM
```

Open your browser and navigate to `https://localhost:7196` or `http://localhost:5246`.

---

## 🔒 Default Roles

- **Admin**: Full system management, user management, and administrative controls.
- **Manager**: Team oversight, pipeline tracking, and performance analytics.
- **Sales Executive**: Lead tracking, customer interactions, and deal management.
