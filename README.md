# gov-compliance-manager

Full-stack compliance management platform based on Costa Rica's MiCITT IT 
Governance and Management Practices Implementation Guide (2021), a public 
government framework aligned with COBIT 2019.


## Tech Stack

**Backend:** ASP.NET Core 9 · Entity Framework Core · MySQL · BCrypt  
**Frontend:** React 18 · React Router · TanStack Query  
**Auth:** JWT (cookie-based) · Two-Factor Authentication via email  
**Testing:** xUnit · Moq · FluentAssertions (36 tests across unit and integration)

## Features

- **Role-based access control** with SUPERADMIN and ADMIN roles
- **Process and activity tracking** aligned to COBIT 2019 domains
- **Document version control** with expiration dates and status management
- **Automated compliance alerts** via a daily background service that emails 
  users when documents or activities are approaching their deadline
- **In-app notifications** with read/unread state and redirect support
- **Gantt chart views** for activity planning (admin and personal views)
- **Audit trail logging** capturing user, IP, browser, and before/after data 
  for every relevant action
- **Two-factor authentication** via time-limited email code
- **Automated email notifications** for account registration and password recovery
- **Dashboard** with compliance statistics and progress tracking

## Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js 18+
- MySQL 8.0+

### Environment variables

The following must be set before running. Never commit real values to source control.

```bash
# Database
ConnectionStrings__DefaultConnection="server=localhost;port=3306;database=normas_db;user=YOUR_USER;password=YOUR_PASSWORD;"

# JWT
JWT__Key="your-secret-key-of-at-least-32-characters"

# Email (SMTP)
Email__SmtpUser="your@email.com"
Email__SmtpPass="your-app-password"
```

### Run the backend

```bash
cd backend
dotnet run
```

Migrations are applied automatically on startup. Seed data is loaded on first run.

### Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on `http://localhost:5173`, backend on `http://localhost:5000`.

### Run tests

```bash
cd backend
dotnet test
```

## Domain context

This system is structured around the domains and processes defined in the 
MiCITT Implementation Guide, a publicly available Costa Rican government 
document. The framework organizes IT governance practices into domains, 
processes, subdomains, and activities — each with implementation status, 
progress percentage, responsible parties, and supporting documentation.

##  Disclaimer

This project was built by a software engineering student as a portfolio piece 
and real-world practice exercise. While it follows industry conventions for 
authentication, authorization, and architecture, it may contain logic or design 
decisions that a more experienced developer would approach differently.

It is intended as a demonstration of technical skills, not as production-ready 
software. Feedback and suggestions are welcome.


## License

MIT
