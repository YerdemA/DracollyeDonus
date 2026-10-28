# Dracolly'e Dönüş – Teknik Doküman

Bu doküman oyunun teknoloji yığınını, mimarisini, klasör yapısını ve teknik kararlarını anlatır. Oyunun ne olduğu için [README.md](README.md), Claude Code ile çalışma kuralları için [CLAUDE.md](CLAUDE.md) dosyasına bakın.

Durum etiketleri: **Kararlaştırıldı** = uygulanacak · **Öneri** = varsayılan yol, değişebilir · **Açık** = henüz karar verilmedi.

---

## 1. Teknoloji yığını

| Katman | Seçim | Durum |
| --- | --- | --- |
| Motor | Unity **6.3 LTS** (desteği Aralık 2027'ye kadar) | Kararlaştırıldı |
| Render | Universal Render Pipeline, 2D Renderer (Universal 2D şablonu) | Kararlaştırıldı |
| Dil | C# | Kararlaştırıldı |
| Giriş | Unity Input System; mobilde On-Screen Stick + butonlar | Kararlaştırıldı |
| Ağ | Photon **Fusion 2** (2.1 kararlı), **Shared Mode** | Fusion kararlaştırıldı, mod öneri |
| Kimlik | Unity Gaming Services (UGS) Authentication: misafir (anonim) + Google Play Games | Kararlaştırıldı |
| Kayıt | UGS Cloud Save: ilerleme, kuşanılan kozmetikler | Öneri |
| Ekonomi | UGS Economy: oyun içi para, kozmetik envanteri | Öneri |
| Sunucu doğrulama | UGS Cloud Code: bölüm tamamlama ve ödül hesaplama | Öneri |
| Satın alma | Unity In-App Purchasing + Google Play Billing | Kararlaştırıldı |
| Karakter animasyonu | Kesme (cut-out) parçalar + koddan üretilen prosedürel hareket | Kararlaştırıldı |
| Platform | Android, Google Play | Kararlaştırıldı |
| Kaynak kontrol | Git + GitHub | Kararlaştırıldı |
| Geliştirme araçları | Visual Studio 2022 / VS Code, Claude Code, Python (asset hattı) | Kararlaştırıldı |

**Photon maliyeti:** Fusion'ın ücretsiz planı 100 eşzamanlı kullanıcıya (CCU) kadar ticari oyunlarda kullanılabiliyor. Sınır katıdır: 100 CCU'dan sonra yeni oyuncular reddedilir. Geliştirme sırasında 20 CCU'luk ücretsiz geliştirme planı yeterli.

**Neden Shared Mode?** İki oyunculu bir co-op oyunda ayrı bir sunucu veya "host" oyuncu gerekmez. Her oyuncu kendi karakterinin ve etkileşime geçtiği nesnelerin sahibidir (state authority). Host Mode'un mobilde getirdiği host kopması sorunlarıyla uğraşmak gerekmez. Hile riski düşük olduğu için (PvP yok) istemci yetkisi kabul edilebilir. Para ve ilerleme gibi değerli veriler zaten sunucuda (UGS) doğrulanır.

---

## 2. Mimari genel bakış

```
┌─────────────────────────── Android cihaz ───────────────────────────┐
│                                                                      │
│  Sahneler:  Boot ─► MainMenu ─► Room (karakter + bölüm seçimi) ─► Level_XX
│                                                                      │
│  Oyun kodu (C#)                                                      │
│   ├─ Player      : hareket, yetenekler, kesme karakter animasyonu    │
│   ├─ Level       : tehlikeler, mekanizmalar, spawn, portal, reset    │
│   ├─ Networking  : Fusion oturumu, oda kodu, eşleşme, bağlantı kopması
│   ├─ Services    : UGS kimlik, kayıt, ekonomi, satın alma            │
│   └─ UI          : menüler, oda ekranı, mobil kontroller, mağaza     │
└──────────┬──────────────────────────────────────┬────────────────────┘
           │ oyun durumu (gerçek zamanlı)          │ hesap verisi (istek/yanıt)
           ▼                                      ▼
   Photon Cloud (Fusion 2)                Unity Gaming Services
   oda, eşleşme, senkronizasyon           Authentication · Cloud Save
                                          Economy · Cloud Code
                                                   │
                                          Google Play Games · Play Billing
```

İki ayrı arka uç vardır ve birbirine karışmaz:
- **Photon**, yalnızca oyun sırasındaki gerçek zamanlı durumu taşır. Kalıcı veri tutmaz.
- **UGS**, hesabı ve kalıcı verileri tutar. Oyun sırasında konum gibi bilgilerle ilgilenmez.

### Sahne akışı

| Sahne | Görev |
| --- | --- |
| `Boot` | Servisleri başlatır (UGS, giriş), oyuncu verisini yükler, menüye geçer |
| `MainMenu` | Oda kur, kodla katıl, rastgele eşleş, mağaza, ayarlar |
| `Room` | Fusion oturumu açılmış durumda; karakter seçimi, oynanabilir bölüm listesi, hazır olma |
| `Level_01` … `Level_05` | Oynanış; Fusion sahne yönetimiyle iki oyuncuda aynı anda yüklenir |

---

## 3. Klasör yapısı

Depo kök dizini aynı zamanda Unity proje kök dizinidir.

```
DracollyeDonus/
├─ README.md · Technical.md · CLAUDE.md · .gitignore
├─ Assets/
│  ├─ Dracolly/                    ← bizim tüm içeriğimiz burada
│  │  ├─ Characters/
│  │  │  ├─ Frawk/                 Sprites/, frawk_rig.json, Frawk.prefab
│  │  │  └─ Sypanny/
│  │  ├─ Cosmetics/                kostüm, pelerin, şapka sprite'ları ve tanımları
│  │  ├─ Environment/              zemin, dekor, ışık alanı görselleri
│  │  ├─ Levels/                   bölüm sahneleri ve bölüm verileri
│  │  ├─ Scenes/                   Boot, MainMenu, Room (+ Prototype)
│  │  ├─ Prefabs/                  tehlikeler, mekanizmalar, UI parçaları
│  │  ├─ UI/                       menü görselleri, fontlar
│  │  ├─ Audio/
│  │  ├─ Settings/                 ScriptableObject ayarları (hız, zıplama, ödüller)
│  │  ├─ Scripts/
│  │  │  ├─ Core/                  saf C# mantığı (Unity'ye bağımsız, test edilebilir)
│  │  │  ├─ Player/
│  │  │  ├─ Level/
│  │  │  ├─ Networking/
│  │  │  ├─ Services/
│  │  │  └─ UI/
│  │  ├─ Editor/                   editör araçları (ör. FrawkBuilder)
│  │  └─ Tests/
│  │     ├─ EditMode/
│  │     └─ PlayMode/
│  ├─ Photon/                      Fusion SDK (Photon'un paketi; elle düzenlenmez)
│  └─ Settings/                    URP ayarları (şablondan gelir)
├─ Packages/                       manifest.json (Unity paketleri)
├─ ProjectSettings/
└─ Tools/
   └─ ArtPipeline/                 Python: arka plan silme, parça kesme, rig verisi üretme
```

`Library/`, `Temp/`, `Logs/`, `UserSettings/` ve `Builds/` Unity'nin ürettiği klasörlerdir, depoya girmez.

**Namespace kuralı:** `Dracolly.Core`, `Dracolly.Player`, `Dracolly.Level`, `Dracolly.Networking`, `Dracolly.Services`, `Dracolly.UI`, `Dracolly.EditorTools`. Mevcut prototip scriptleri şimdilik `Dracolly` namespace'inde; klasörlerine taşınırken düzeltilecek.

---

## 4. Karakter sistemi

### Görsellerin oyuna girişi

1. Konsept, yapay zeka ile üretilir (ana araç Gemini). Ürettiğimiz görseller şunlardır: referans sayfası, tam yan görünüş (pelerinsiz), yan kafa ifadeleri.
2. `Tools/ArtPipeline` arka planı siler ve görseli parçalara ayırır: kafa, gövde, üst kol, alt kol, el, uyluk, baldır.
3. Kolun örttüğü gövde/bacak bölgeleri otomatik doldurulur. Arka kol ve bacak, ön taraftakilerin %62 parlaklıkta kopyasıdır.
4. Her parçanın eklem noktası (pivot) ve konumu `<karakter>_rig.json` dosyasına yazılır.
5. `Dracolly → ...` menüsündeki editör aracı sprite ayarlarını yapar ve kemik hiyerarşisini kurup prefab üretir.

### Ölçek

- Pixels Per Unit: **460**. Karakter yaklaşık **1,79 birim** boyundadır (1 birim ≈ 1 metre).
- Kamera ortografik boyutu: 3, yani ekranda yaklaşık 6 birim yükseklik görünür.

### Hiyerarşi ve çizim sırası

```
Frawk (Rigidbody2D, CapsuleCollider2D, PlayerController2D)
└─ Visual (SortingGroup, CutoutWalk)       ← ölçek x = ±1 ile yön çevrilir
   └─ Hip
      ├─ torso ─┬─ head
      │         ├─ upper_arm ─ forearm ─ hand
      │         └─ upper_arm_back ─ forearm_back ─ hand_back
      ├─ thigh ─ shin
      └─ thigh_back ─ shin_back
```

Çizim sırası arkadan öne şöyledir: arka kol → arka bacak → kafa → ön bacak → gövde → ön kol.

### Animasyon

`CutoutWalk`, animasyon klibi kullanmaz. Eklemleri her karede karakterin hızına ve yere basıp basmadığına göre döndürür. Bekleme sırasında nefes alma, yürüme döngüsü ve havadayken bacakları toplama pozları bu şekilde üretilir.

- **Artısı:** Yeni bir karakter (Sypanny) yalnızca parçalarıyla aynı hareketi kazanır. Ağ üzerinden yalnızca hız ve yere basma bilgisi gönderilir.
- **Sınırı:** İtme, fırlatma, zihin kontrolü gibi özel pozlar ayrı ayrı eklenecek. Her biri prosedürel bir poz olabilir veya Unity Animation klibi olarak üst katmana yazılabilir. **Açık**

### Kozmetikler (plan)

- Kostüm, aynı kemiklere oturan alternatif sprite setidir (parça başına bir sprite değişimi).
- Pelerin ve şapka, belirli kemiklere takılan ek parçalardır (pelerin → torso, şapka → head).
- Kozmetik tanımı bir ScriptableObject'tir: kimlik, fiyat, hangi karakter, hangi parçalar.
- Kuşanılan kozmetik kimlikleri, oyuncu bilgisiyle birlikte ağ üzerinden partnere gönderilir; partner de aynı görünümü görür.

---

## 5. Oynanış kuralları (teknik karşılıkları)

| Kural | Uygulama |
| --- | --- |
| Biri ölürse ikisi de baştan başlar | Ölüm olayı ağa yayınlanır; iki istemci de bölümü spawn noktasından sıfırlar (mekanizmalar dahil) |
| Bölüm sonu | İki oyuncu da portal tetikleyicisindeyse bölüm tamamlanır |
| Güneş ışığı | Işık alanı = collider + raycast ile gölge kontrolü; perde/ayna durumları ağda senkron |
| Avcı devriyeleri | Yol noktaları + görüş konisi; davranış deterministik zamanlamayla iki istemcide aynı |
| Frawk: itme / fırlatma | İtilen sandığın state authority'si iten oyuncuya geçer |
| Sypanny: zihin kontrolü | Menzildeki etkileşimli nesneyi seçip tetikler; nesne durumu ağda senkron. Menzil ve süre **Açık** |

---

## 6. Çok oyunculu yapı (Fusion 2, Shared Mode)

| Konu | Uygulama |
| --- | --- |
| Oda kodu | Fusion oturum adı = 6 karakterlik kod (karışan harfler ve rakamlar hariç, ör. 0/O, 1/I) |
| Rastgele eşleşme | Açık ve dolu olmayan bir oturuma katıl; yoksa yeni oturum aç |
| Oda kapasitesi | 2 oyuncu |
| Karakter seçimi | Her oyuncunun seçimi ağ üzerinden paylaşılan bir değerdir; iki oyuncu aynı karakteri seçtiyse başlat butonu kapalıdır |
| Oynanabilir bölümler | `1 … min(A, B) + 1`, toplam bölüm sayısını aşamaz (A, B: tamamlanan bölüm sayıları) |
| Hareket senkronu | Her oyuncu kendi karakterinin sahibidir; konum, yön, hız ve yere basma bilgisi paylaşılır, animasyon her cihazda yerelde hesaplanır |
| Bağlantı kopması | Oyun duraklar, 120 saniyelik geri sayım başlar; partner dönerse devam eder, dönmezse lobiye dönülür |

Hesaplama kuralları (`min(A,B)+1` gibi) `Scripts/Core` altında Unity'ye bağımsız sınıflar olarak yazılır ve EditMode testleriyle doğrulanır.

---

## 7. Hesap, kayıt ve ekonomi (UGS)

### Giriş

- İlk açılışta **anonim (misafir) hesap** otomatik oluşturulur.
- Oyuncu istediği zaman **Google Play Games** hesabını bağlayabilir; ilerleme korunur.
- **Satın alma ve para yükleme için Google Play Games girişi zorunludur.**
- Misafir oyuncuya ilerleme anlarında hesap bağlama hatırlatması gösterilir (hangi anlar: **Açık**).

### Veri modeli (taslak)

| Veri | Yer | İçerik |
| --- | --- | --- |
| İlerleme | Cloud Save | tamamlanan bölüm sayısı, bölüm başına ilk tamamlama bilgisi |
| Oyun içi para | Economy (para birimi) | bakiye |
| Kozmetik envanteri | Economy (envanter) | sahip olunan kozmetik kimlikleri |
| Kuşanılanlar | Cloud Save | karakter başına kostüm / pelerin / şapka kimliği |

### Ödül kuralı

- Bölümü ilk kez tamamlamak tam ödül verir.
- Tekrar oynamak daha düşük ödül verir. Miktarlar **Açık**.
- Ödül hesaplama ve ilerleme güncelleme **Cloud Code**'da yapılır. İstemci "bölümü bitirdim" der, sunucu kuralı uygular. Böylece cihazda yapılan değişiklikle para veya ilerleme hilelenemez.

### Satın alma

Unity IAP ile Google Play'den oyun içi para paketleri satılır. Satın alma doğrulanınca bakiye Economy üzerinden artırılır. Kozmetikler yalnızca oyun içi parayla alınır.

---

## 8. Mobil kontroller

| Kontrol | Uygulama |
| --- | --- |
| Hareket | Sol tarafta Input System **On-Screen Stick** (yatay eksen) |
| Zıplama | Sağ tarafta buton |
| Yetenek | Sağ tarafta buton (Frawk: it / fırlat, Sypanny: menzildeki mekanizmayı tetikle) |
| Ekran | Yalnızca yatay (landscape), kontroller güvenli alanın (safe area / çentik) içinde |

Prototipteki geçici dokunmatik kontrol (ekranın sol/sağ yarısı) sanal joystick gelince kaldırılacak. `PlayerController2D.SetMoveInput` ve `PressJump` metotları, kontrollerin bağlanacağı noktalardır.

---

## 9. Performans hedefleri

- Orta seviye Android cihazda **60 FPS**.
- Her karede bellek ayırmayan (sıfır GC) oyun döngüsü: `Update` içinde `new`, LINQ, string birleştirme yok; bileşenler önceden alınır.
- Karakter ve çevre sprite'ları **Sprite Atlas** ile birleştirilir.
- Doku boyutları mobil için sınırlanır; karakter parçaları en fazla 512 px.
- Uygulama boyutu, ilk demo için 150 MB altında tutulur.

---

## 10. Test, derleme ve yayın

- **EditMode testleri:** `Scripts/Core` altındaki kurallar (bölüm açılması, ödül, oda kodu üretimi).
- **PlayMode testleri:** karakter hareketi, tehlike tetiklemeleri (ihtiyaç oldukça).
- **Ağ testi:** Aynı bilgisayarda Unity'nin Multiplayer Play Mode özelliği ile iki oyuncu; düzenli olarak gerçek telefonla.
- **Android derlemesi:** Google Play için AAB formatı (APK yalnızca test için).
- **Keystore** (imzalama anahtarı) ve şifreleri asla depoya girmez; ayrı ve yedekli saklanır. Kaybedilirse uygulama güncellenemez.
- **Paket adı (applicationId):** `com.yerdema.dracollyedonus` önerilir. Google Play'e ilk yüklemeden sonra **değiştirilemez**.

---

## 11. Açık teknik kararlar

- [ ] Fusion Shared Mode kesinleşsin mi? (ilk ağ prototipinden sonra karar)
- [ ] Özel yetenek pozları: prosedürel mi, animasyon klibi mi?
- [ ] Görseller büyüdükçe Git LFS'e geçiş
- [ ] Derleme süresi uzarsa Assembly Definition (asmdef) ile kod bölme
- [ ] Sypanny'nin zihin kontrolü menzili ve süresi
- [ ] Ödül miktarları ve hatırlatma anları
- [ ] Frawk görsel iyileştirmeleri: omuz, oran, kulak, pelerin
