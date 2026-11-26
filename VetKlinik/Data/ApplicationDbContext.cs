using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Models;

namespace VetKlinik.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<CalismaSaati> CalismaSaatleri => Set<CalismaSaati>();
    public DbSet<Hizmet> Hizmetler => Set<Hizmet>();
    public DbSet<Uzmanlik> Uzmanliklar => Set<Uzmanlik>();
    public DbSet<Veteriner> Veterinerler => Set<Veteriner>();
    public DbSet<VeterinerMusaitlik> VeterinerMusaitlikleri => Set<VeterinerMusaitlik>();
    public DbSet<EvcilHayvan> EvcilHayvanlar => Set<EvcilHayvan>();
    public DbSet<AsiKaydi> AsiKayitlari => Set<AsiKaydi>();
    public DbSet<Randevu> Randevular => Set<Randevu>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // Identity tabloları için ŞART

        // --- Çoka-çok ilişkiler (EF Core ara tabloyu kendisi oluşturur) ---
        b.Entity<Veteriner>()
            .HasMany(v => v.Uzmanliklar).WithMany(u => u.Veterinerler)
            .UsingEntity(j => j.ToTable("VeterinerUzmanliklari"));

        b.Entity<Veteriner>()
            .HasMany(v => v.Hizmetler).WithMany(h => h.Veterinerler)
            .UsingEntity(j => j.ToTable("VeterinerHizmetleri"));

        // --- Veteriner ↔ kullanıcı hesabı (1-1, opsiyonel) ---
        b.Entity<Veteriner>()
            .HasOne(v => v.Kullanici).WithMany()
            .HasForeignKey(v => v.KullaniciId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Entity<Veteriner>().HasIndex(v => v.KullaniciId).IsUnique();

        // --- Evcil hayvan ↔ sahip ---
        b.Entity<EvcilHayvan>()
            .HasOne(e => e.Sahip).WithMany(u => u.EvcilHayvanlar)
            .HasForeignKey(e => e.SahipId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Randevu: geçmiş kayıtlar yanlışlıkla silinmesin diye Restrict ---
        b.Entity<Randevu>().HasOne(r => r.Veteriner).WithMany(v => v.Randevular)
            .HasForeignKey(r => r.VeterinerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Randevu>().HasOne(r => r.Hizmet).WithMany()
            .HasForeignKey(r => r.HizmetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Randevu>().HasOne(r => r.EvcilHayvan).WithMany(e => e.Randevular)
            .HasForeignKey(r => r.EvcilHayvanId).OnDelete(DeleteBehavior.Restrict);

        // Çakışma sorgusu hep "bu hekimin bu aralıktaki randevuları" diye çalışacak
        b.Entity<Randevu>().HasIndex(r => new { r.VeterinerId, r.Baslangic });

        // PostgreSQL: yerel saat tutuyoruz → "timestamp without time zone"
        // (Aksi halde Npgsql DateTime.Kind=Utc ister ve hata verir.)
        b.Entity<Randevu>().Property(r => r.Baslangic).HasColumnType("timestamp without time zone");
        b.Entity<Randevu>().Property(r => r.Bitis).HasColumnType("timestamp without time zone");
        b.Entity<Randevu>().Property(r => r.OlusturmaTarihi).HasColumnType("timestamp without time zone");

        // Enum'u veritabanında okunabilir metin olarak sakla ("Bekliyor" vs.)
        b.Entity<Randevu>().Property(r => r.Durum).HasConversion<string>().HasMaxLength(20);

        // --- Başlangıç verileri ---
        b.Entity<Uzmanlik>().HasData(
            new Uzmanlik { Id = 1, Ad = "Küçük Hayvan" },
            new Uzmanlik { Id = 2, Ad = "Egzotik Hayvan" },
            new Uzmanlik { Id = 3, Ad = "Cerrahi" },
            new Uzmanlik { Id = 4, Ad = "Dermatoloji" });

        b.Entity<Hizmet>().HasData(
            new Hizmet { Id = 1, Ad = "Genel Muayene", SureDakika = 30, Ucret = 750 },
            new Hizmet { Id = 2, Ad = "Aşı", SureDakika = 15, Ucret = 500 },
            new Hizmet { Id = 3, Ad = "Kısırlaştırma", SureDakika = 120, Ucret = 6000 },
            new Hizmet { Id = 4, Ad = "Diş Temizliği", SureDakika = 60, Ucret = 2500 },
            new Hizmet { Id = 5, Ad = "Tıraş ve Bakım", SureDakika = 45, Ucret = 900 });

        // Pazartesi-Cuma 09-18, Cumartesi 10-14, Pazar kapalı
        var saatler = new List<CalismaSaati>();
        for (int g = 0; g < 7; g++)
        {
            var gun = (DayOfWeek)g;
            saatler.Add(gun switch
            {
                DayOfWeek.Sunday => new CalismaSaati { Id = g + 1, Gun = gun, KapaliMi = true },
                DayOfWeek.Saturday => new CalismaSaati { Id = g + 1, Gun = gun, Acilis = new(10, 0), Kapanis = new(14, 0) },
                _ => new CalismaSaati { Id = g + 1, Gun = gun, Acilis = new(9, 0), Kapanis = new(18, 0) }
            });
        }
        b.Entity<CalismaSaati>().HasData(saatler);
    }
}
