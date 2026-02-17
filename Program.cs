using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Data;
using OrderManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Add services
// ----------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Environment-based database configuration
//if (builder.Environment.IsDevelopment())
//{
//    // SQL Server for local development
//    builder.Services.AddDbContext<ApplicationDbContext>(options =>
//        options.UseSqlServer(
//            builder.Configuration.GetConnectionString("DefaultConnection")));
//}
//else
//{
//    // SQLite for production (Azure Free)
//    builder.Services.AddDbContext<ApplicationDbContext>(options =>
//        options.UseSqlite("Data Source=orders.db"));
//}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

// Identity + Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// AWS S3 Service
builder.Services.AddScoped<S3Service>();

var app = builder.Build();

// ----------------------------
// Create Roles + Admin at Startup
// ----------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    // Ensure DB created (important for SQLite in production)
    dbContext.Database.Migrate();

    string[] roles = { "Admin", "Manager", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    string adminEmail = "admin@test.com";
    string adminPassword = "Admin@123"; // change for production

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// ----------------------------
// Configure Middleware
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MVC default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Razor Pages (Identity)
app.MapRazorPages();

app.Run();
