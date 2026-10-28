# CLAUDE.md – Dracolly'e Dönüş

Bu dosya Claude Code'un bu projede nasıl çalışacağını tanımlar. Oyunun ne olduğu için `README.md`, mimari ve teknik kararlar için `Technical.md` dosyasını oku. **Kod yazmadan önce ilgili bölümü Technical.md'den kontrol et.**

## Proje özeti

- 2D, yandan görünüm, iki kişilik online co-op bulmaca-platform oyunu. Hedef platform Android.
- Unity **6.3 LTS**, URP 2D, C#, Input System, Photon **Fusion 2 (Shared Mode)**, Unity Gaming Services.
- **Tek geliştirici:** Erdem. Unity'de yeni; C#/.NET, React ve Flutter deneyimi var. Çizim yapmıyor, görseller yapay zeka ile üretiliyor.
- Şu anki hedef: 5 bölümlük oynanabilir demo.

## İletişim

- Erdem ile **Türkçe** konuş.
- Unity editöründe yapılacak her işlemi menü yolu ve tıklama sırasıyla anlat: `Edit → Project Settings → Player`. "Ayarlardan aç" deyip geçme.
- Unity'ye özgü bir kavramı ilk kullandığında bir cümleyle açıkla (ör. prefab, ScriptableObject, state authority).

## Çalışma yöntemi

1. **Önce plan:** 3'ten fazla dosyaya dokunan veya yeni bir sistem ekleyen işlerde önce kısa bir plan yaz ve onay bekle. Küçük düzeltmelerde doğrudan yap.
2. **Küçük adımlar:** Her adım Unity'de açılıp denenebilir bir sonuç vermeli. Büyük özellikleri parçalara böl.
3. **Test talimatı zorunlu:** Unity'de Play'e basamazsın. Her değişiklikten sonra şunları yaz: hangi sahne açılacak, ne yapılacak, ne görülmeli. Kısa bir kontrol listesi yeterli.
4. **Saf mantık ayrı:** Kurallar (bölüm açılması, ödül, oda kodu, zamanlayıcılar) `Assets/Dracolly/Scripts/Core` altında, Unity'ye bağımlı olmayan C# sınıfları olarak yazılır ve EditMode testleri eklenir.
5. **Sahne kurulumu koddan:** Sahneye/prefab'a çok sayıda nesne veya bileşen eklemek gerekiyorsa elle talimat listesi yerine bir **editör aracı** (`Assets/Dracolly/Editor`, `Dracolly/...` menüsü) yaz. Örnek: `FrawkBuilder.cs`.
6. **Doküman güncel kalır:** Mimari, klasör yapısı veya bir teknik karar değişirse aynı işte `Technical.md`'yi güncelle. Bir "Açık" karar netleşirse durumunu değiştir.
7. **Belirsizlikte sor:** Oynanış kuralı, görsel yön veya maliyetli bir teknik seçim Technical.md'de yoksa varsayım yapma, sor.

## Kod kuralları

- **Dil:** Tanımlayıcılar (sınıf, metot, değişken) İngilizce. Yorumlar ve Inspector `Tooltip` metinleri Türkçe olabilir.
- **Namespace:** `Dracolly.Core`, `Dracolly.Player`, `Dracolly.Level`, `Dracolly.Networking`, `Dracolly.Services`, `Dracolly.UI`, `Dracolly.EditorTools`.
- **Dosya:** Bir dosyada bir public sınıf, dosya adı = sınıf adı.
- **Ayarlar:** Hız, zıplama gücü, ödül miktarları gibi oynanış değerleri koda gömülmez; `[SerializeField]` alanlar veya `Assets/Dracolly/Settings` altındaki ScriptableObject'ler kullanılır.
- **Mobil performans:** `Update` / `FixedUpdate` / `FixedUpdateNetwork` içinde bellek ayırma yok (`new`, LINQ, string birleştirme, boxing). `GetComponent`, `Find*`, `Camera.main` sonuçları önbelleğe alınır.
- **Unity sürümü:** Unity 6 API'sini kullan (ör. `Rigidbody2D.linearVelocity`, `FindFirstObjectByType`). Eski API'ye geri dönme.
- **Input:** Yalnızca Input System. Eski `UnityEngine.Input` sınıfı yeni kodda kullanılmaz. Mobil kontroller `PlayerController2D.SetMoveInput` / `PressJump` gibi metotlar üzerinden bağlanır.

## Ağ kuralları (Fusion 2)

- Ağda paylaşılan her durum Fusion `[Networked]` özellikleri veya RPC ile taşınır. Yerel değişkende tutulan bir durum partnerde görünmez.
- Ağdaki nesnelerin hareketi `FixedUpdateNetwork` içinde, Fusion'ın input yapısıyla yapılır.
- Bir nesneyi yalnızca **state authority**'si değiştirir. Etkileşimde (sandık itme, kol çekme) yetki devri veya authority'ye RPC kullanılır.
- Animasyon ağdan gönderilmez. Hız ve yere basma bilgisi gönderilir, `CutoutWalk` her cihazda yerelde hesaplar.
- Kalıcı veri (ilerleme, para, envanter) Photon üzerinden taşınmaz ve istemcide hesaplanmaz. Sunucu (UGS Cloud Code) doğrular.

## Dokunma kuralları

**Asla:**
- `.meta` dosyalarını silme, elle düzenleme veya ayrı bırakma. Bir asset taşınır veya yeniden adlandırılırsa `.meta` dosyası da birlikte taşınır. Mümkünse taşımayı Unity içinde yaptırmayı öner.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` klasörlerini okuma veya değiştirme.
- `Assets/Photon/` altındaki SDK dosyalarını değiştirme.
- Keystore, şifre, API anahtarı, Photon App ID veya UGS proje bilgilerini koda ya da depoya yazma.
- Push, force-push veya `main` dalında geçmişi değiştirme.

**Önce sor:**
- `.unity` (sahne) ve `.prefab` dosyalarını metin olarak düzenlemek. Bunun yerine editör aracı yazmayı tercih et.
- `ProjectSettings/` veya `Packages/manifest.json` değişiklikleri, yeni paket eklemek.
- Bir asset'i silmek veya yeniden adlandırmak.
- Commit atmak. Erdem istediğinde commit at.

## Git

- Dal: `main` her zaman açılabilir ve çalışır durumda. Özellikler `feature/<kısa-ad>` dalında geliştirilir.
- Commit mesajları İngilizce, Conventional Commits biçiminde: `feat: add virtual joystick`, `fix: crate push authority`, `docs: update Technical.md`.

## Görseller ve asset hattı

- Görselleri sen üretmezsin. Erdem yapay zeka ile üretir ve depoya koyar.
- Görsel işleme (arka plan silme, parça kesme, rig verisi üretme) `Tools/ArtPipeline` altındaki Python scriptleriyle yapılır. Çıktılar `Assets/Dracolly/Characters/<Karakter>/` altına gider.
- Karakter ölçeği: Pixels Per Unit **460**, karakter boyu yaklaşık 1,79 birim. Yeni karakter ve kozmetik parçaları bu ölçeğe uyar.

## Komutlar

Unity editörü dışında derleme veya test çalıştırmak istenirse Unity batch mode kullanılır. Unity kurulum yolu Erdem'in bilgisayarına göre değişir; ilk seferde sor ve buraya ekle:

```
"<Unity.exe yolu>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./TestResults.xml -logFile -
```

Not: Unity editörü açıkken aynı proje batch mode'da açılamaz.
