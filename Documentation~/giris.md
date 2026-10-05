<!-- tür: özellik -->
# Giriş (Hyperlab Login)

> Registry token'ını alır, `~/.upmconfig.toml` ve proje manifest'ini hazırlar, Hyperlab Setup'ı kurdurur.

## Ne zaman kullanılır

Boş bir projede Hyperlab paketlerini ilk kez kurarken. Token zaten varsa ve registry erişilebilirse gerekmez; Setup
registry'yi kendisi yoklar.

## Hızlı örnek

1. Package Manager → Add package from git URL → `https://github.com/hyperlabgames/hyperlab-bootstrap.git`.
2. Açılan `Hyperlab Login` penceresine ortak kullanıcı adı ve parolayı gir.
3. Paketler inince `Hyperlab → Setup` kendiliğinden açılır.

## Kurulum (Editor)

Menü: `Hyperlab → Login`. Pencere paket ilk yüklendiğinde bir kez kendiliğinden açılır. Token zaten varsa ve registry'ye
`GET` 200 dönüyorsa pencere açılmaz; yalnız manifest eksiği parolasız tamamlanır (pencerede de "Complete setup" düğmesi).
Batchmode'da (CI, headless editör) hiçbir şey yapılmaz.

Akış: `PUT <registry>/-/user/org.couchdb.user:<ad>` isteği `Authorization: Basic` ile atılır; dönen `token`
`~/.upmconfig.toml` içine `[npmAuth."https://upm.hyperlab.games"]` altında yazılır (dosya yoksa izin 600 ile
oluşturulur). Sonra manifest'e `Hyperlab` scoped registry'si (scope `com.hyperlab`, `com.cysharp`) ve
`com.hyperlab.setup` eklenir. Yazılan setup sürümü `BootstrapDefaults.SetupVersion`'dır; yayınlanan son setup sürümüne
eşit tutulur (test denetler), yoksa yeni projeler eski setup'ı ve onun hatalarını alır.

## Inspector alanları

Yok.

## Kod API'si

Hepsi `Hyperlab.Bootstrap` ad alanında, Editor asmdef'indedir.

- `BootstrapService.Status(BootstrapPaths)`: `~/.upmconfig.toml`'da token ve manifest'te scoped registry ile
  `com.hyperlab.setup` var mı (`BootstrapStatus`: `TokenPresent`, `ManifestHasRegistry`, `ManifestHasSetup`,
  `NeedsLogin`, `Complete`). Yalnız dosyalara bakar; ağ isteği atmaz.
- `BootstrapService.RunAsync(IHttp, BootstrapPaths, user, password)`: login olur; başarılıysa token'ı ve manifest'i
  yazar, `ResolveRequested` bayrağını kaldırır (`AutoOpen` ana iş parçacığında `Client.Resolve()` çağırır). Login ya da
  manifest okuma/ayrıştırma hatasında hiçbir dosyaya yazılmaz; token yazıldıktan sonra manifest yazılamazsa token
  kalır ve mesaj bunu söyler. Manifest'te `com.hyperlab.setup` zaten varsa (token yenileme) mesaj "Logged in. Hyperlab
  Setup is already installed." olur.
- `BootstrapService.CompleteManifestAsync(IHttp, BootstrapPaths)`: parolasız; saklı token `GET` ile doğrulanırsa yalnız
  manifest eksiğini tamamlar (PUT atmaz).
- `RegistryLogin.LoginAsync` / `TokenWorksAsync`: login çağrısı ve token yoklaması (`GET <registry>/com.hyperlab.core`).
- `UpmConfigWriter.SetToken` / `TokenFor` / `SameUrl`: `.upmconfig.toml` metninde yalnız ilgili bölümü yazar ya da okur.
- `ManifestWriter.Ensure`: manifest metnine registry ve bağımlılığı ekler; bozuk JSON'da `FormatException` atar.
- `IHttp` (`HttpClientAdapter` gerçek gerçeklemesi): test için HTTP sınırı; `Status == 0` ağ hatasıdır.
- `BasicAuth.Header(user, password)`: `Authorization: Basic` değeri.
- `BootstrapPaths.Default()`: gerçek `~/.upmconfig.toml` ve proje `Packages/manifest.json` yolları;
  `BootstrapDefaults`: registry adresi, scope'lar, Setup sürümü.

Agent komutları: `bootstrap_status`, `bootstrap_login` (parola `--password_env` ile ortam değişkeninden okunur).

## Veri (BGDatabase)

Yok.

## Davranış ayrıntıları

- Pencere proje başına bir kez, token yoksa ya da parolasız tamamlama başarısızsa (neden pencerede yazar) kendiliğinden
  açılır; sonra yalnız menüden. Bootstrap `file:` ile kuruluysa ya da setup manifest'te varsa manifest'e registry yazılmaz,
  `GET`/Resolve atılmaz (`Complete` sayılır).
- Token ve manifest girişleri tamamsa pencere "Already set up" der. Token'ın registry'de hâlâ geçerli olup
  olmadığı otomatik denetlenmez (`TokenWorksAsync` yalnız API'dir).
- Manifest bozuksa token da yazılmaz.
- Var olan registry adı ve scope'lar korunur; manifest'ten silme yok.
- `.upmconfig.toml`'da diğer bölümlere dokunulmaz.

## Tuzaklar

| Belirti | Sebep | Çözüm |
|---|---|---|
| "Wrong user name or password." | Yanlış kimlik ya da kullanıcı yok (ayırt edilmez) | Ekipten kimliği yeniden al |
| "Cannot reach the Hyperlab registry." | Ağ yok ya da proxy | Bağlantıyı kontrol et, yeniden dene |
| "Could not write ~/.upmconfig.toml: <yol>" | Dosya izni ya da yol yok | Pencere dosya yolunu gösterir; token gösterilmez. Dosyanın ve klasörünün yazılabilir olduğunu denetle, yeniden dene. Manifest'e dokunulmaz |
| "Packages/manifest.json could not be read." | Manifest yok ya da okunamıyor | Dosyayı geri getir, yeniden dene; hiçbir şey yazılmaz |
| "Packages/manifest.json is not valid JSON" | Bozuk `Packages/manifest.json` | Dosyayı düzelt, yeniden dene; token yazılmaz |
| `chmod 600` başarısız | İzin ayarı en iyi çaba | Hata değil; token yine yazılır |
| Registry 409 | Yanlış kimlik ya da kullanıcı yok | Ayırt edilmez; "Wrong user name or password." gösterilir |

## İlgili

- [README](../README.md)
- `com.hyperlab.setup` → `Documentation~/kurulum-sureci.md`
