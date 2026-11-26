using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Veritabanı: bağlantı dizesi User Secrets'tan gelir (repo'ya girmez)
// ---------------------------------------------------------------
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection bulunamadı. Projeye sağ tık → Manage User Secrets ile ekleyin.");

builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connStr));

// ---------------------------------------------------------------
// Kimlik (Identity) + roller
// ---------------------------------------------------------------
builder.Services
    .AddDefaultIdentity<ApplicationUser>(o =>
    {
        o.SignIn.RequireConfirmedAccount = false;
        // Ödev gereği admin şifresi "sau" (3 karakter) → şifre kuralları gevşetildi
        o.Password.RequiredLength = 3;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Identity'nin Login/Register sayfaları Razor Pages'tir

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // önce "kimsin?"
app.UseAuthorization();  // sonra "buna yetkin var mı?"

app.MapStaticAssets();

// Admin paneli için area rotası (ileride: Areas/Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

// Roller ve admin hesabı yoksa oluştur
await DbSeeder.SeedAsync(app.Services);

app.Run();
