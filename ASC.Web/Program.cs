using ASC.Web.Configuration;
using ASC.Web.Data;
using ASC.Web.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cấu hình dịch vụ
builder.Services.AddConfig(builder.Configuration)
    .AddMyDependencyGroup();

// 🔹 Lấy Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 🔹 Cấu hình DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<DbContext, ApplicationDbContext>();

// 🔹 Cấu hình Identity (Chỉ đăng ký nếu chưa có)
if (!builder.Services.Any(s => s.ServiceType == typeof(UserManager<IdentityUser>)))
{
    builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
}

// 🔹 Đăng ký RoleManager
builder.Services.AddScoped<RoleManager<IdentityRole>>();

// 🔹 Cấu hình AppSettings
builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("AppSettings"));

// 🔹 Đăng ký các dịch vụ khác
builder.Services.AddScoped<IIdentitySeed, IdentitySeed>();
builder.Services.AddSingleton<ASC.Web.Services.IEmailSender, AuthMessageSender>();
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddTransient<ISmsSender, AuthMessageSender>();

// 🔹 Cấu hình Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // Hết hạn sau 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔹 Cấu hình MVC & Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// 🔹 Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 🔹 Đặt `UseSession()` TRƯỚC `UseAuthorization()`
app.UseSession();  // ✅ Đặt trước Authorization
app.UseAuthentication();  // ✅ Thêm Authentication trước Authorization
app.UseAuthorization();

// 🔹 Cấu hình Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 🔹 Khởi tạo database và seed dữ liệu
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // 🔹 Thực hiện Migration trước khi seed dữ liệu
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }

    var storageSeed = serviceProvider.GetRequiredService<IIdentitySeed>();
    await storageSeed.Seed(
        serviceProvider.GetRequiredService<UserManager<IdentityUser>>(),
        serviceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
        serviceProvider.GetRequiredService<IOptions<ApplicationSettings>>()
    );
}
using (var scope = app.Services.CreateScope())
{
    var navigationCacheOperations = scope.ServiceProvider.GetRequiredService<INavigationCacheOperations>();
    await navigationCacheOperations.CreateNavigationCacheAsync();
}

// 🔹 Chạy ứng dụng
app.Run();
