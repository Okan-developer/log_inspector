namespace LogInspector.Services;

public static class ErrorKnowledge
{
    private static readonly Dictionary<string, string> Ora = new()
    {
        ["ORA-00001"] = "Benzersiz kısıt ihlali: aynı anahtarla kayıt zaten var.",
        ["ORA-00904"] = "Geçersiz tanımlayıcı: sütun adı yok veya yanlış.",
        ["ORA-00942"] = "Tablo veya view yok ya da erişim yetkisi eksik.",
        ["ORA-01017"] = "Kullanıcı adı/parola geçersiz.",
        ["ORA-01400"] = "NOT NULL sütuna NULL değer eklenmeye çalışıldı.",
        ["ORA-01403"] = "Sorgu satır döndürmedi (no data found).",
        ["ORA-01422"] = "Sorgu beklenenden fazla satır döndürdü.",
        ["ORA-01722"] = "Sayıya dönüştürülemeyen geçersiz değer.",
        ["ORA-01843"] = "Geçersiz ay değeri / tarih formatı uyumsuz.",
        ["ORA-02291"] = "Yabancı anahtar ihlali: üst kayıt bulunamadı.",
        ["ORA-02292"] = "Yabancı anahtar ihlali: bağlı alt kayıtlar var.",
        ["ORA-04031"] = "Paylaşılan bellek (shared pool) yetersiz.",
        ["ORA-12154"] = "Bağlantı tanımlayıcısı (TNS) çözümlenemedi.",
        ["ORA-12514"] = "Dinleyici istenen servisi tanımıyor.",
        ["ORA-12541"] = "Dinleyici yok veya erişilemiyor.",
    };

    private static readonly Dictionary<string, string> Exceptions = new()
    {
        ["NullPointerException"] = "Null bir nesne referansı kullanıldı.",
        ["NullReferenceException"] = "Null bir nesne referansı kullanıldı.",
        ["ArgumentNullException"] = "Metoda null bir argüman verildi.",
        ["ArgumentException"] = "Metoda geçersiz bir argüman verildi.",
        ["IllegalArgumentException"] = "Metoda geçersiz bir argüman verildi.",
        ["IllegalStateException"] = "Nesne bu işlem için uygun durumda değil.",
        ["InvalidOperationException"] = "İşlem nesnenin mevcut durumunda geçersiz.",
        ["ClassCastException"] = "Uyumsuz tipe dönüştürme denendi.",
        ["InvalidCastException"] = "Uyumsuz tipe dönüştürme denendi.",
        ["NumberFormatException"] = "Metin sayıya çevrilemedi.",
        ["FormatException"] = "Değer beklenen formatta değil.",
        ["ArrayIndexOutOfBoundsException"] = "Dizi sınırları dışında indeks kullanıldı.",
        ["IndexOutOfRangeException"] = "Dizi sınırları dışında indeks kullanıldı.",
        ["KeyNotFoundException"] = "Sözlükte aranan anahtar yok.",
        ["ConcurrentModificationException"] = "Koleksiyon yinelenirken değiştirildi.",
        ["OutOfMemoryError"] = "JVM belleği yetersiz (heap).",
        ["OutOfMemoryException"] = "Bellek yetersiz.",
        ["StackOverflowError"] = "Çağrı yığını taştı; muhtemelen sonsuz özyineleme.",
        ["ClassNotFoundException"] = "Sınıf classpath içinde bulunamadı.",
        ["NoSuchBeanDefinitionException"] = "Spring bean tanımı bulunamadı.",
        ["BeanCreationException"] = "Spring bean oluşturulamadı; iç içe nedene bakın.",
        ["UnsatisfiedDependencyException"] = "Spring bağımlılığı karşılanamadı.",
        ["DataIntegrityViolationException"] = "Veritabanı bütünlük kısıtı ihlal edildi.",
        ["SQLException"] = "Veritabanı hatası; ORA/SQL koduna bakın.",
        ["DbUpdateException"] = "EF Core veritabanı güncellemesi başarısız.",
        ["SocketTimeoutException"] = "Ağ bağlantısı zaman aşımına uğradı.",
        ["ConnectException"] = "Uzak sunucuya bağlanılamadı.",
        ["HttpRequestException"] = "HTTP isteği başarısız oldu.",
        ["TimeoutException"] = "İşlem zaman aşımına uğradı.",
        ["FileNotFoundException"] = "Dosya bulunamadı.",
        ["UnauthorizedAccessException"] = "Kaynağa erişim yetkisi yok.",
    };

    public static string? Explain(string type)
    {
        if (Ora.TryGetValue(type, out var ora)) return ora;
        var dot = type.LastIndexOf('.');
        var simple = dot >= 0 ? type[(dot + 1)..] : type;
        return Exceptions.GetValueOrDefault(simple);
    }
}
