# Medical Information System (.NET)

This repository contains a learning project developed as part of an academic practice. The project represents a simple medical information system built on the .NET platform using C# and demonstrates the principles of client–server architecture, clean code organization, and basic backend development.

## Project Overview

The system is designed to manage core processes of a medical institution, including user management, appointment handling, and data access based on user roles. It focuses on demonstrating modern approaches to backend development rather than providing a full production-ready solution.

The project consists of:
- A server-side REST API built with ASP.NET Core  
- Data access implemented using Entity Framework Core and SQLite  
- A console-based client application for interacting with the API  

## Key Features

- RESTful API following standard HTTP conventions  
- Role-based access (patient, doctor, administrator)  
- User authentication and authorization  
- CRUD operations for main domain entities  
- Data persistence using SQLite  
- Clear separation of concerns based on Clean Architecture principles  

## Technologies Used

- C#  
- .NET / ASP.NET Core  
- Entity Framework Core  
- SQLite  
- Swagger (OpenAPI) for API testing and documentation  

## Project Structure

The solution is organized into logical layers:
- API layer for handling HTTP requests and responses  
- Application layer containing business logic and services  
- Infrastructure layer for database access and external dependencies  
- Client layer implemented as a console application  

This structure improves maintainability, testability, and scalability of the application.

## How to Run

1. Clone the repository to your local machine.  
2. Open the solution in Visual Studio or another compatible IDE.  
3. Restore NuGet packages.  
4. Run database migrations if required.  
5. Start the ASP.NET Core API project.  
6. Run the console client to interact with the system.  

Swagger UI will be available when running the API and can be used to test endpoints directly.

## Purpose of the Project

The main goal of this project is educational. It demonstrates:
- Practical usage of ASP.NET Core for building Web APIs  
- Working with databases using Entity Framework Core  
- Applying Clean Architecture concepts in a real project  
- Understanding client–server interaction in .NET applications  

## Future Improvements

Possible enhancements include:
- Adding a graphical user interface  
- Implementing more advanced security mechanisms  
- Extending business logic and validation rules  
- Writing automated unit and integration tests
