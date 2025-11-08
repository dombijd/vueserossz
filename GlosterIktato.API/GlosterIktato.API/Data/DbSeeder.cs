using GlosterIktato.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {            
            // 1. CÉGEK
            if (!await context.Companies.AnyAsync())
            {
                var companies = new List<Company>
                {
                    new Company
                    {
                        Name = "P92 Gloster Péksütemény Kft.",
                        TaxNumber = "12345678-2-41",
                        Address = "1011 Budapest, Fő utca 1.",
                        IsActive = true
                    },
                    new Company
                    {
                        Name = "P93 Gloster Logistics Kft.",
                        TaxNumber = "87654321-2-42",
                        Address = "6725 Szeged, Kossuth utca 10.",
                        IsActive = true
                    },
                    new Company
                    {
                        Name = "P94 Gloster Trade Kft.",
                        TaxNumber = "11223344-2-43",
                        Address = "9024 Győr, Baross út 5.",
                        IsActive = true
                    }
                };
                await context.Companies.AddRangeAsync(companies);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ 3 cég létrehozva (P92, P93, P94)");
            }
            
            // 2. SZEREPKÖRÖK
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role { Name = "Admin", Description = "Rendszergazda - teljes hozzáférés" },
                    new Role { Name = "User", Description = "Normál felhasználó - iktatás" },
                    new Role { Name = "Accountant", Description = "Könyvelő - számlák véglegesítése" },
                    new Role { Name = "Approver", Description = "Jóváhagyó - dokumentumok jóváhagyása" },
                    new Role { Name = "ElevatedApprover", Description = "Emelt szintű jóváhagyó - nagy értékű számlák" }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ 5 szerepkör létrehozva");
            }

            // 3. FELHASZNÁLÓK
            if (!await context.Users.AnyAsync())
            {
                var company1 = await context.Companies.FirstAsync(c => c.Name.Contains("P92"));
                var company2 = await context.Companies.FirstAsync(c => c.Name.Contains("P93"));
                var company3 = await context.Companies.FirstAsync(c => c.Name.Contains("P94"));

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
                        Email = "jóváhagyó@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("jovahagyo123"),
                        FirstName = "Jóváhagyós",
                        LastName = "Katalin",
                        IsActive = true
                    },
                    new User
                    {
                        Email = "vezeto@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("vezeto123"),
                        FirstName = "Vezetős",
                        LastName = "László",
                        IsActive = true
                    },
                    new User
                    {
                        Email = "konyvelő@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("konyvelo123"),
                        FirstName = "Könyvelős",
                        LastName = "Anna",
                        IsActive = true
                    },
                    new User
                    {
                        Email = "asszisztens@gloster.hu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("asszisztens123"),
                        FirstName = "Asszisztenős",
                        LastName = "Éva",
                        IsActive = true
                    }
                };
                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ 6 felhasználó létrehozva");

                // 3.1 USER ↔ COMPANY HOZZÁRENDELÉSEK
                var admin = await context.Users.FirstAsync(u => u.Email == "admin@gloster.hu");
                var iktato = await context.Users.FirstAsync(u => u.Email == "iktato@gloster.hu");
                var jovahagyo = await context.Users.FirstAsync(u => u.Email == "jóváhagyó@gloster.hu");
                var vezeto = await context.Users.FirstAsync(u => u.Email == "vezeto@gloster.hu");
                var konyvelő = await context.Users.FirstAsync(u => u.Email == "konyvelő@gloster.hu");
                var asszisztens = await context.Users.FirstAsync(u => u.Email == "asszisztens@gloster.hu");

                var userCompanies = new List<UserCompany>
                {
                    // Admin - mindhárom cégnél
                    new UserCompany { UserId = admin.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = admin.Id, CompanyId = company2.Id },
                    new UserCompany { UserId = admin.Id, CompanyId = company3.Id },
                    
                    // Iktató - P92 és P93
                    new UserCompany { UserId = iktato.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = iktato.Id, CompanyId = company2.Id },
                    
                    // Jóváhagyó - P92 és P94
                    new UserCompany { UserId = jovahagyo.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = jovahagyo.Id, CompanyId = company3.Id },
                    
                    // Vezető - mindhárom cégnél
                    new UserCompany { UserId = vezeto.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = vezeto.Id, CompanyId = company2.Id },
                    new UserCompany { UserId = vezeto.Id, CompanyId = company3.Id },
                    
                    // Könyvelő - mindhárom cégnél
                    new UserCompany { UserId = konyvelő.Id, CompanyId = company1.Id },
                    new UserCompany { UserId = konyvelő.Id, CompanyId = company2.Id },
                    new UserCompany { UserId = konyvelő.Id, CompanyId = company3.Id },
                    
                    // Asszisztens - csak P92
                    new UserCompany { UserId = asszisztens.Id, CompanyId = company1.Id }
                };
                await context.UserCompanies.AddRangeAsync(userCompanies);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ User-Company kapcsolatok létrehozva");

                // 3.2 USER ↔ ROLE HOZZÁRENDELÉSEK                
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                var userRole = await context.Roles.FirstAsync(r => r.Name == "User");
                var approverRole = await context.Roles.FirstAsync(r => r.Name == "Approver");
                var elevatedApproverRole = await context.Roles.FirstAsync(r => r.Name == "ElevatedApprover");
                var accountantRole = await context.Roles.FirstAsync(r => r.Name == "Accountant");

                var userRoles = new List<UserRole>
                {
                    // Admin - Admin + User
                    new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
                    new UserRole { UserId = admin.Id, RoleId = userRole.Id },
                    
                    // Iktató - User
                    new UserRole { UserId = iktato.Id, RoleId = userRole.Id },
                    
                    // Jóváhagyó - User + Approver
                    new UserRole { UserId = jovahagyo.Id, RoleId = userRole.Id },
                    new UserRole { UserId = jovahagyo.Id, RoleId = approverRole.Id },
                    
                    // Vezető - User + Approver + ElevatedApprover
                    new UserRole { UserId = vezeto.Id, RoleId = userRole.Id },
                    new UserRole { UserId = vezeto.Id, RoleId = approverRole.Id },
                    new UserRole { UserId = vezeto.Id, RoleId = elevatedApproverRole.Id },
                    
                    // Könyvelő - User + Accountant
                    new UserRole { UserId = konyvelő.Id, RoleId = userRole.Id },
                    new UserRole { UserId = konyvelő.Id, RoleId = accountantRole.Id },
                    
                    // Asszisztens - User
                    new UserRole { UserId = asszisztens.Id, RoleId = userRole.Id }
                };
                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ User-Role kapcsolatok létrehozva");
            }

            // 4. DOKUMENTUMTÍPUSOK
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
                Console.WriteLine("✓ 4 dokumentumtípus létrehozva");
            }

            // 5. SZÁLLÍTÓK
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
                    },
                    new Supplier
                    {
                        Name = "MOL Magyar Olaj- és Gázipari Nyrt.",
                        TaxNumber = "40404040-2-47",
                        Address = "1117 Budapest, Október huszonharmadika u. 18.",
                        ContactPerson = "Szabó Gábor",
                        Email = "flotta@mol.hu",
                        Phone = "+36-1-567-8901",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "METRO Cash & Carry Kft.",
                        TaxNumber = "50505050-2-48",
                        Address = "1211 Budapest, Hungária körút 168.",
                        ContactPerson = "Kiss Andrea",
                        Email = "b2b@metro.hu",
                        Phone = "+36-1-678-9012",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Vodafone Magyarország Zrt.",
                        TaxNumber = "60606060-2-49",
                        Address = "1096 Budapest, Lechner Ödön fasor 6.",
                        ContactPerson = "Horváth Zsolt",
                        Email = "vallalatiszolgalat@vodafone.hu",
                        Phone = "+36-1-789-0123",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Tesco-Global Áruházak Zrt.",
                        TaxNumber = "70707070-2-50",
                        Address = "2040 Budaörs, Neumann János út 1.",
                        ContactPerson = "Molnár Ildikó",
                        Email = "beszallito@tesco.hu",
                        Phone = "+36-1-890-1234",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "DHL Express Hungary Kft.",
                        TaxNumber = "80808080-2-51",
                        Address = "1097 Budapest, Fehérakác utca 2.",
                        ContactPerson = "Farkas Tamás",
                        Email = "info@dhl.hu",
                        Phone = "+36-1-901-2345",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Printnet Nyomdaipari Kft.",
                        TaxNumber = "90909090-2-52",
                        Address = "1116 Budapest, Fehérvári út 89-95.",
                        ContactPerson = "Lakatos Ágnes",
                        Email = "rendeles@printnet.hu",
                        Phone = "+36-1-012-3456",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Nestlé Hungária Kft.",
                        TaxNumber = "11111111-2-53",
                        Address = "1033 Budapest, Szentendrei út 89-93.",
                        ContactPerson = "Papp Róbert",
                        Email = "kapcsolat@nestle.hu",
                        Phone = "+36-1-123-4567",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Coca-Cola HBC Magyarország Kft.",
                        TaxNumber = "22222222-2-54",
                        Address = "1097 Budapest, Könyves Kálmán krt. 76.",
                        ContactPerson = "Balogh Mária",
                        Email = "ugyfelszolgalat@coca-cola.hu",
                        Phone = "+36-1-234-5678",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Spar Magyarország Kft.",
                        TaxNumber = "33333333-2-55",
                        Address = "2051 Biatorbágy, Mészárosok útja 2.",
                        ContactPerson = "Simon Ferenc",
                        Email = "beszallitok@spar.hu",
                        Phone = "+36-1-345-6789",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Budapest Bank Zrt.",
                        TaxNumber = "44444444-2-56",
                        Address = "1138 Budapest, Váci út 188.",
                        ContactPerson = "Németh Klára",
                        Email = "info@budapestbank.hu",
                        Phone = "+36-1-456-7890",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Waberer's International Nyrt.",
                        TaxNumber = "55555555-2-57",
                        Address = "2040 Budaörs, Neumann János út 1/C.",
                        ContactPerson = "Varga István",
                        Email = "logisztika@waberers.hu",
                        Phone = "+36-1-567-8901",
                        IsActive = true
                    },
                    new Supplier
                    {
                        Name = "Continental Reifen Hungary Kft.",
                        TaxNumber = "66666666-2-58",
                        Address = "5000 Szolnok, Tiszaligeti sétány 19.",
                        ContactPerson = "Török Márta",
                        Email = "kapcsolat@continental.hu",
                        Phone = "+36-1-678-9012",
                        IsActive = true
                    }
                };
                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ {suppliers.Count} szállító létrehozva");
            }

            // 6. DEMO DOKUMENTUMOK (20-30 db különböző státuszokkal)
            if (!await context.Documents.AnyAsync())
            {
                var company1 = await context.Companies.FirstAsync(c => c.Name.Contains("P92"));
                var company2 = await context.Companies.FirstAsync(c => c.Name.Contains("P93"));
                var company3 = await context.Companies.FirstAsync(c => c.Name.Contains("P94"));

                var szlaType = await context.DocumentTypes.FirstAsync(dt => dt.Code == "SZLA");
                var tigType = await context.DocumentTypes.FirstAsync(dt => dt.Code == "TIG");
                var szType = await context.DocumentTypes.FirstAsync(dt => dt.Code == "SZ");

                var suppliers = await context.Suppliers.ToListAsync();
                var iktato = await context.Users.FirstAsync(u => u.Email == "iktato@gloster.hu");
                var jovahagyo = await context.Users.FirstAsync(u => u.Email == "jóváhagyó@gloster.hu");
                var vezeto = await context.Users.FirstAsync(u => u.Email == "vezeto@gloster.hu");
                var konyvelő = await context.Users.FirstAsync(u => u.Email == "konyvelő@gloster.hu");

                var random = new Random(42); // Fixed seed reproducibility
                var documents = new List<Document>();

                // Státuszok eloszlása
                var statuses = new[] { "Draft", "PendingApproval", "ElevatedApproval", "Accountant", "Done", "Rejected" };
                var currencies = new[] { "HUF", "EUR", "USD" };

                for (int i = 1; i <= 30; i++)
                {
                    var createdDate = DateTime.UtcNow.AddDays(-random.Next(1, 60));
                    var supplier = suppliers[random.Next(suppliers.Count)];
                    var company = i % 3 == 0 ? company3 : (i % 2 == 0 ? company2 : company1);
                    var status = statuses[random.Next(statuses.Length)];

                    // Assigned user státusz alapján
                    int? assignedUserId = status switch
                    {
                        "Draft" => iktato.Id,
                        "PendingApproval" => jovahagyo.Id,
                        "ElevatedApproval" => vezeto.Id,
                        "Accountant" => konyvelő.Id,
                        "Done" => null,
                        "Rejected" => null,
                        _ => iktato.Id
                    };

                    var doc = new Document
                    {
                        ArchiveNumber = $"{company.Name[0]}-SZLA-{createdDate:yyMMdd}-{i:D4}",
                        OriginalFileName = $"szamla_{supplier.Name.Replace(" ", "_")}_{i}.pdf",
                        StoragePath = $"/temp/uploads/szamla_{i}.pdf",
                        Status = status,
                        InvoiceNumber = $"SZ-{2025}-{random.Next(1000, 9999)}",
                        IssueDate = createdDate.AddDays(-5),
                        PerformanceDate = createdDate.AddDays(-2),
                        PaymentDeadline = createdDate.AddDays(random.Next(15, 45)),
                        GrossAmount = random.Next(10000, 500000),
                        Currency = currencies[random.Next(currencies.Length)],
                        CompanyId = company.Id,
                        DocumentTypeId = szlaType.Id,
                        SupplierId = supplier.Id,
                        CreatedByUserId = iktato.Id,
                        AssignedToUserId = assignedUserId,
                        CreatedAt = createdDate,
                        ModifiedAt = status != "Draft" ? createdDate.AddHours(random.Next(1, 48)) : null
                    };

                    documents.Add(doc);
                }

                await context.Documents.AddRangeAsync(documents);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ {documents.Count} demo dokumentum létrehozva");

                // 6.1 DEMO DOCUMENT HISTORY
                var histories = new List<DocumentHistory>();
                foreach (var doc in documents)
                {
                    // Created history mindig
                    histories.Add(new DocumentHistory
                    {
                        DocumentId = doc.Id,
                        UserId = doc.CreatedByUserId,
                        Action = "Created",
                        Comment = "Dokumentum feltöltve",
                        CreatedAt = doc.CreatedAt
                    });

                    // Ha nem Draft, akkor státusz változás
                    if (doc.Status != "Draft")
                    {
                        histories.Add(new DocumentHistory
                        {
                            DocumentId = doc.Id,
                            UserId = doc.CreatedByUserId,
                            Action = "StatusChanged",
                            FieldName = "Status",
                            OldValue = "Draft",
                            NewValue = doc.Status,
                            Comment = $"Státusz változtatva: {doc.Status}",
                            CreatedAt = doc.CreatedAt.AddHours(1)
                        });
                    }

                    // Ha Done vagy Rejected, további history bejegyzés
                    if (doc.Status == "Done")
                    {
                        histories.Add(new DocumentHistory
                        {
                            DocumentId = doc.Id,
                            UserId = konyvelő.Id,
                            Action = "Completed",
                            Comment = "Számla könyvelésbe rögzítve",
                            CreatedAt = doc.ModifiedAt ?? doc.CreatedAt.AddDays(2)
                        });
                    }
                    else if (doc.Status == "Rejected")
                    {
                        histories.Add(new DocumentHistory
                        {
                            DocumentId = doc.Id,
                            UserId = jovahagyo.Id,
                            Action = "Rejected",
                            Comment = "Hiányos számlaadatok - kérjük javítani",
                            CreatedAt = doc.ModifiedAt ?? doc.CreatedAt.AddDays(1)
                        });
                    }
                }

                await context.DocumentHistories.AddRangeAsync(histories);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ {histories.Count} history bejegyzés létrehozva");
            }
        }
    }
}