# CommerceHub

CommerceHub is a full-featured e-commerce platform developed using ASP.NET Core MVC and SQL Server.

The platform provides customers with a seamless online shopping experience and enables administrators to efficiently manage products, categories, orders, customers, and payments.

---

## Overview

CommerceHub was developed to provide a complete online shopping solution that simplifies product management, order processing, and customer interactions.

---

## Features

### Authentication & Authorization

- User Registration
- User Login
- Role-Based Authorization
- Secure Authentication
- User Profile Management

---

### Customer Features

- Browse Products
- Search Products
- Filter Products by Category
- View Product Details
- Add Products to Cart
- Update Cart Quantity
- Remove Products from Cart
- Checkout Orders
- View Order History
- Manage User Profile

---

### Admin Features

- Admin Dashboard
- Manage Products
- Manage Categories
- Manage Orders
- Manage Customers
- View Sales Reports
- Manage Product Inventory

---

### E-Commerce Features

- Shopping Cart System
- Order Management
- Product Catalog
- Product Images
- Category Management
- Payment Integration
- Responsive Design

---

## Technologies Used

### Backend

- ASP.NET Core MVC
- C#
- Entity Framework Core
- LINQ

### Frontend

- HTML5
- CSS3
- Bootstrap 5
- JavaScript
- jQuery

### Database

- SQL Server

### Development Tools

- Visual Studio
- Git
- GitHub

---

## Project Structure

```text
CommerceHub
│
├── Controllers
├── Models
├── Views
├── Data
├── Services
├── Repositories
├── ViewModels
├── wwwroot
├── Migrations
└── Program.cs
```

---

## Installation

### Clone Repository

```bash
git clone https://github.com/your-username/CommerceHub.git
```

### Navigate to Project Folder

```bash
cd CommerceHub
```

### Restore Packages

```bash
dotnet restore
```

### Apply Database Migrations

```bash
dotnet ef database update
```

### Run Application

```bash
dotnet run
```

---

## Database Configuration

Configure the connection string inside:

```json
appsettings.json
```

Example:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "YOUR_CONNECTION_STRING"
    }
}
```

---

## Screenshots

Add screenshots for:

- Home Page
- Product Catalog
- Product Details Page
- Shopping Cart
- Checkout Page
- Login Page
- Register Page
- Admin Dashboard
- Product Management Page
- Orders Management Page

---

## Future Enhancements

- Wishlist System
- Product Reviews and Ratings
- Coupon & Discount System
- Email Notifications
- Inventory Tracking
- Advanced Sales Analytics
- Multi-Vendor Support

---

## License

This project was developed for educational purposes.
