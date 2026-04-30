using BankAuditSystem.DAO;
using BankAuditSystem.Data;
using BankAuditSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("AuditDbConnection") ?? throw new InvalidOperationException("Connection string 'AuditDbConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<AuditDAO>();

var app = builder.Build();

// temp insertion test

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. Manually request your AuditDAO from the DI container
        var auditDao = services.GetRequiredService<AuditDAO>();

        var NewEntry = new AuditEntry
        {
            AccountID = 42069,
            Amount = 400.69m,
            TransactionType = "DEPOSIT",
            TimeStp = DateTime.Now
        };

        auditDao.InsertAuditEntry(NewEntry);
        Console.WriteLine("Entry successfully hashed and inserted.");

        // 3. Execute the function
        Console.WriteLine("GETTING BALANCE...");
        decimal balance = auditDao.GetBalance(12345);
        Console.WriteLine($"Success! Your Balance is {balance}");


    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error occurred: {ex.Message}");
    }
}

// END 

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
