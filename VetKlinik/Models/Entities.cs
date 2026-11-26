using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace VetKlinik.Models;

// ---------------------------------------------------------------
// Kullanıcı: Identity'nin kullanıcısını genişletiyoruz.
// Hem hayvan sahibi (Uye) hem veteriner hem admin bu tablodadır;
// hangisi olduğunu ROL belirler.
// ---------------------------------------------------------------
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string AdSoyad { get; set; } = "";

    public ICollection<EvcilHayvan> EvcilHayvanlar { get; set; } = new List<EvcilHayvan>();
}

// ---------------------------------------------------------------
// Klinik çalışma saatleri (tek klinik → haftanın her günü için 1 satır)
// ---------------------------------------------------------------
public class CalismaSaati
{
    public int Id { get; set; }

    public DayOfWeek Gun { get; set; }

    public bool KapaliMi { get; set; }

    public TimeOnly Acilis { get; set; }
    public TimeOnly Kapanis { get; set; }
}

// ---------------------------------------------------------------
// Hizmet: muayene, aşı, kısırlaştırma...
// Süre, çakışma hesabında kullanılır.
// ---------------------------------------------------------------
public class Hizmet
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur."), StringLength(100)]
    [Display(Name = "Hizmet Adı")]
    public string Ad { get; set; } = "";

    [Range(5, 480, ErrorMessage = "Süre 5-480 dakika arasında olmalıdır.")]
    [Display(Name = "Süre (dk)")]
    public int SureDakika { get; set; }

    [Range(0, 100000, ErrorMessage = "Geçerli bir ücret giriniz.")]
    [Column(TypeName = "numeric(10,2)")]
    [Display(Name = "Ücret (₺)")]
    public decimal Ucret { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    // Bu hizmeti verebilen hekimler (çoka-çok)
    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();
}

// ---------------------------------------------------------------
// Uzmanlık alanı: küçük hayvan, egzotik, cerrahi, dermatoloji...
// ---------------------------------------------------------------
public class Uzmanlik
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Ad { get; set; } = "";

    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();
}

// ---------------------------------------------------------------
// Veteriner hekim. Sisteme giriş yapabilmesi için bir kullanıcı
// hesabına (rolü "Veteriner") bağlanır.
// ---------------------------------------------------------------
public class Veteriner
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur."), StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = "";

    [StringLength(30)]
    public string? Unvan { get; set; }   // "Vet. Hekim", "Doç. Dr." ...

    public string? KullaniciId { get; set; }
    public ApplicationUser? Kullanici { get; set; }

    public ICollection<Uzmanlik> Uzmanliklar { get; set; } = new List<Uzmanlik>();
    public ICollection<Hizmet> Hizmetler { get; set; } = new List<Hizmet>();
    public ICollection<VeterinerMusaitlik> Musaitlikler { get; set; } = new List<VeterinerMusaitlik>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}

// ---------------------------------------------------------------
// Hekimin haftalık müsaitlik aralığı.
// Örn: Pazartesi 09:00-13:00 ve Pazartesi 14:00-18:00 → 2 satır.
// ---------------------------------------------------------------
public class VeterinerMusaitlik
{
    public int Id { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    public DayOfWeek Gun { get; set; }
    public TimeOnly Baslangic { get; set; }
    public TimeOnly Bitis { get; set; }
}

// ---------------------------------------------------------------
// Evcil hayvan. Sahibi bir kullanıcıdır (rol: Uye).
// ---------------------------------------------------------------
public class EvcilHayvan
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hayvanın adı zorunludur."), StringLength(50)]
    public string Ad { get; set; } = "";

    [Required(ErrorMessage = "Tür zorunludur."), StringLength(30)]
    [Display(Name = "Tür")]
    public string Tur { get; set; } = "";      // Kedi, Köpek, Kuş...

    [StringLength(50)]
    [Display(Name = "Irk")]
    public string? Irk { get; set; }

    [Display(Name = "Doğum Tarihi")]
    public DateOnly? DogumTarihi { get; set; }

    [Range(0.01, 200, ErrorMessage = "Kilo 0.01-200 kg arasında olmalıdır.")]
    [Column(TypeName = "numeric(6,2)")]
    [Display(Name = "Kilo (kg)")]
    public decimal Kilo { get; set; }

    public string? FotografYolu { get; set; }

    // Sahiplik: yetkilendirme kontrollerinin dayandığı alan
    [Required]
    public string SahipId { get; set; } = "";
    public ApplicationUser? Sahip { get; set; }

    public ICollection<AsiKaydi> AsiKayitlari { get; set; } = new List<AsiKaydi>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}

// ---------------------------------------------------------------
// Aşı kaydı
// ---------------------------------------------------------------
public class AsiKaydi
{
    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Required(ErrorMessage = "Aşı adı zorunludur."), StringLength(80)]
    [Display(Name = "Aşı Adı")]
    public string AsiAdi { get; set; } = "";

    [Display(Name = "Yapıldığı Tarih")]
    public DateOnly YapildigiTarih { get; set; }

    [Display(Name = "Sonraki Doz")]
    public DateOnly? SonrakiDozTarihi { get; set; }
}

public enum RandevuDurum
{
    Bekliyor = 0,
    Onaylandi = 1,
    Reddedildi = 2,
    Tamamlandi = 3,
    IptalEdildi = 4
}

// ---------------------------------------------------------------
// Randevu. Süre ve ücret, oluşturulduğu andaki hizmetten KOPYALANIR:
// hizmetin fiyatı sonradan değişse bile eski randevunun ücreti bozulmaz.
// ---------------------------------------------------------------
public class Randevu
{
    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    public int HizmetId { get; set; }
    public Hizmet? Hizmet { get; set; }

    [Display(Name = "Başlangıç")]
    public DateTime Baslangic { get; set; }

    [Display(Name = "Bitiş")]
    public DateTime Bitis { get; set; }          // = Baslangic + SureDakika

    public int SureDakika { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Ucret { get; set; }

    public RandevuDurum Durum { get; set; } = RandevuDurum.Bekliyor;

    [StringLength(250)]
    public string? RedNedeni { get; set; }

    [StringLength(2000)]
    [Display(Name = "Muayene Notu")]
    public string? MuayeneNotu { get; set; }     // Tamamlanınca hekim girer

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
