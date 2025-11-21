The Contract Monthly Claim System (CMCS) is a web-based ASP.NET Core MVC application designed to streamline the submission, approval, and processing of lecturer claims at an academic institution.
This project forms Part 3 of the POE, implementing automation, reporting, role-based access control, and a professionally themed UI.
Technologies Used
Backend
ASP.NET Core MVC 8
Entity Framework Core
Identity Framework (Users + Roles)
LINQ
C# 12

Frontend
Bootstrap 5 (Custom Pink Theme)
Razor Views
JavaScript / jQuery
Responsive Dashboard Layouts

Database
SQL Server / LocalDB / Azure SQL compatible
Reporting
QuestPDF (for generating PDF claim reports)

Tools
Visual Studio / VS Code
NuGet Package Manager
GitHub Version Control

User Roles & Features
Lecturer
Submit new claims
Auto-calculation of total payment
Upload supporting documents (PDF, DOCX, XLSX)
Track claim status (Pending / Approved / Rejected)
Personal dashboard with history

Coordinator / Manager
View all pending claims
Approve or reject claims
View claim details and documents
Dashboard with approval statistics

HR Super User
Full system access
Create new users
Edit or delete users
Assign roles (Lecturer, Coordinator, Manager, HR)
Generate PDF claim reports
View full claim history
Admin-style dashboard with analytics

SEEDED ROLES
Role:HR  Email:hr@cmcs.com  Passwrd:Hr12345
Role:Lecturer  Email:lecturer@cmcs.com  Password:Lect123
Role:Coordinator  Email:coordinator@cmcs.com  Password:Coord123
Role:Manager  Email:manager@cmcs.com   Password:Man12345

How to Run the Program
git clone https://github.com/yourusername/ContractClaimSystem.git
cd ContractClaimSystem
Then use the Seeded Roles to login
