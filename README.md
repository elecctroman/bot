# Bot Professional (Visual Studio 2022)

Bu depo, oyun dosyası çevirisi ve encoding düzeltme süreçlerini **tek panelden yönetmek** için hazırlanmış bir WinForms uygulaması içerir.

> Not: Elinizdeki mevcut botun kaynak kodu/tasarım dosyaları paylaşılmadığı için birebir (%100 piksel/piksel) kopya üretmek teknik olarak mümkün değildir. Bu proje, aynı kullanım amacına yönelik profesyonel ve genişletilebilir bir temel sunar.

## Özellikler

- Klasör bazlı toplu dosya tarama (alt klasörler dahil)
- Uzantı filtresi (`.txt,.ini,.xml,.json,.cfg,.lua` vb.)
- Otomatik encoding tespiti (UTF-8 BOM, UTF-8 strict, fallback CP1254)
- Bozuk karakter (`�`) analizi
- Kural tabanlı çeviri/değişim uygulama (`kaynak=hedef` formatı)
- UTF-8 (BOM'lu/BOM'suz) çıktıya standartlaştırma
- İsteğe bağlı `.bak` yedek oluşturma
- İşlem logları ve dosya durum takibi

## Visual Studio 2022 ile derleme

1. **Visual Studio 2022** açın.
2. `BotProfessional.sln` dosyasını açın.
3. `Release | Any CPU` veya `Debug | Any CPU` seçin.
4. Build alın ve çalıştırın.

> Hedef framework: `net8.0-windows`  
> Gerekirse Visual Studio Installer üzerinden `.NET Desktop Development` ve .NET 8 bileşenlerini ekleyin.

## Kullanım

1. Uygulamada klasör seçin.
2. Gerekirse uzantı listesini güncelleyin.
3. `Çeviri Kuralları` sekmesinde her satıra `kaynak=hedef` yazın.
4. `Dosyaları Tara` ile analiz edin.
5. `Encoding Düzelt + Çeviri Uygula` ile dönüşümü yapın.

## Geliştirme önerileri

- Kural setini JSON dosyasından yükleme/kaydetme
- Regex tabanlı ileri seviye dönüşüm motoru
- Çeviri API entegrasyonu (DeepL/Google/Azure/OpenAI)
- Profil sistemi (oyuna göre hazır kurallar)
- Hata raporu dışa aktarma (CSV/Excel)
