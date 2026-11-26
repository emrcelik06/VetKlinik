using Microsoft.AspNetCore.Identity;
using VetKlinik.Models;

namespace VetKlinik.Data;

// Uygulama açılırken rolleri ve admin hesabını oluşturur (yoksa).
public static class DbSeeder
{
    public const string RolAdmin = "Admin";
    public const string RolVeteriner = "Veteriner";
    public const string RolUye = "Uye";

    
    private const string AdminEmail = "Y255012160@sakarya.edu.tr";
    private const string AdminSifre = "sau";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var rol in new[] { RolAdmin, RolVeteriner, RolUye })
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));

        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                AdSoyad = "Sistem Yöneticisi"
            };
            var sonuc = await userManager.CreateAsync(admin, AdminSifre);
            if (sonuc.Succeeded)
                await userManager.AddToRoleAsync(admin, RolAdmin);
            else
                throw new Exception("Admin oluşturulamadı: " +
                    string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        }
    }
}
