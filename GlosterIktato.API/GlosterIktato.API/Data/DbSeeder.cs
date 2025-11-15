using GlosterIktato.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1. Cégek
            if (!await context.Companies.AnyAsync())
            {
                var companies = new List<Company>
                {
                    new Company
                    {
                        Name = "Gloster Péksütemény Kft.",
                        TaxNumber = "12345678-2-41",
                        Address = "1011 Budapest, Fő utca 1.",
                        IsActive = true
                    },
                    new Company
                    {
                        Name = "Gloster Logistics Kft.",
                        TaxNumber = "87654321-2-42",
                        Address = "6725 Szeged, Kossuth utca 10.",
                        IsActive = true
                    }
                };
                await context.Companies.AddRangeAsync(companies);
                await context.SaveChangesAsync();
            }

            // 2. Szerepkörök
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role { Name = "Admin", Description = "Rendszergazda - teljes hozzáférés" },
                    new Role { Name = "User", Description = "Normál felhasználó - iktatás, jóváhagyás" },
                    new Role { Name = "Accountant", Description = "Könyvelő - számlák véglegesítése" },
                    new Role { Name = "Approver", Description = "Jóváhagyó - dokumentumok jóváhagyása" }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // 3. Felhasználók
            if (!await context.Users.AnyAsync())
            {
                var company1 = await context.Companies.FirstAsync(c => c.Name.Contains("Péksütemény"));
                var company2 = await context.Companies.FirstAsync(c => c.Name.Contains("Logistics"));
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                var userRole = await context.Roles.FirstAsync(r => r.Name == "User");
                var accountantRole = await context.Roles.FirstAsync(r => r.Name == "Accountant");

                var users = new List<User>
                {
                    new User
                    {
                        Email = "admin@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // DEMO jelszó!
                        FirstName = "Admin",
                        LastName = "Teszt",
                        IsActive = true
                    },
                    new User
                    {
                        Email = "iktato@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("iktato123"),
                        FirstName = "Iktatós",
                        LastName = "Péter",
                        IsActive = true
                    },
                    new User
                    {
                        Email = "konyvelő@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("konyvelo123"),
                        FirstName = "Könyvelős",
                        LastName = "Anna",
                        IsActive = true
                    }
                };
                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();

                // User ↔ Company hozzárendelések (ÚJ!)
                var admin = await context.Users.FirstAsync(u => u.Email == "admin@gloster.hu");
                var iktato = await context.Users.FirstAsync(u => u.Email == "iktato@gloster.hu");
                var konyvelő = await context.Users.FirstAsync(u => u.Email == "konyvelő@gloster.hu");

                var userCompanies = new List<UserCompany>
                {
                    // Admin mindkét cégnél
                    new UserCompany { UserId = admin.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = admin.Id, CompanyId = company2.Id },
                    
                    // Iktató csak Péksüteménynél
                    new UserCompany { UserId = iktato.Id, CompanyId = company1.Id },
                    
                    // Könyvelő mindkét cégnél
                    new UserCompany { UserId = konyvelő.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = konyvelő.Id, CompanyId = company2.Id }
                };
                await context.UserCompanies.AddRangeAsync(userCompanies);
                await context.SaveChangesAsync();

                // Szerepkörök hozzárendelése
                var userRoles = new List<UserRole>
                {
                    new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
                    new UserRole { UserId = admin.Id, RoleId = userRole.Id },
                    new UserRole { UserId = iktato.Id, RoleId = userRole.Id },
                    new UserRole { UserId = konyvelő.Id, RoleId = accountantRole.Id },
                    new UserRole { UserId = konyvelő.Id, RoleId = userRole.Id }
                };
                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();
            }

            // 4. Dokumentumtípusok
            if (!await context.DocumentTypes.AnyAsync())
            {
                var documentTypes = new List<DocumentType>
                {
                    new DocumentType { Name = "Számla", Code = "SZLA", IsActive = true },
                    new DocumentType { Name = "Teljesítésigazolás", Code = "TIG", IsActive = true },
                    new DocumentType { Name = "Szerződés", Code = "SZ", IsActive = true },
                    new DocumentType { Name = "Egyéb", Code = "E", IsActive = true }
                };
                await context.DocumentTypes.AddRangeAsync(documentTypes);
                await context.SaveChangesAsync();
            }

            // 5. Szállítók
            if (!await context.Suppliers.AnyAsync())
            {
                var suppliers = new List<Supplier>
                {
                    new Supplier
                    {
                        Name = "Magyar Telekom Nyrt.",
                        TaxNumber = "10101010-2-44",
                        Address = "1013 Budapest, Krisztina krt. 55.",
                        ContactPerson = "Kovács János",
                        Email = "szamlazas@telekom.hu",
                        Phone = "+36-1-234-5678",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "E.ON Hungária Zrt.",
                        TaxNumber = "20202020-2-45",
                        Address = "1134 Budapest, Váci út 17.",
                        ContactPerson = "Nagy Éva",
                        Email = "ugyfelszolgalat@eon.hu",
                        Phone = "+36-1-345-6789",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Office Depot Kft.",
                        TaxNumber = "30303030-2-46",
                        Address = "1117 Budapest, Dombóvári út 27.",
                        ContactPerson = "Tóth Péter",
                        Email = "rendeles@officedepot.hu",
                        Phone = "+36-1-456-7890",
                        IsActive = true
                    }
                };
                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("Seed data successfully created!");
        }
    }
}