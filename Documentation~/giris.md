<!-- tür: özellik -->
# Giriş (Hyperlab Login)

> GitHub girişinden gelen registry token'ını denetler, `~/.upmconfig.toml` ve proje manifest'ini hazırlar, Hyperlab Setup'ı kurdurur.

## Ne zaman kullanılır

Boş bir projede Hyperlab paketlerini ilk kez kurarken. Token zaten varsa ve registry erişilebilirse gerekmez; Setup
registry'yi kendisi yoklar.

## Hızlı örnek

1. Package Manager → Add package from git URL → `https://github.com/hyperlabgames/hyperlab-bootstrap.git`.
2. Açılan `Hyperlab Login` penceresinde `Log in with GitHub`'a bas; tarayıcıda GitHub'a izin ver.
3. Giriş başarılı olunca pencere kapanır (sonuç Console'a yazılır); paketler inip editör yeniden yüklenince
   `Hyperlab → Setup` kendiliğinden açılır.

Agent ile durum:

```bash
unity command bootstrap_status --project-path <proje>
```

Komut `token_present`, `manifest_has_registry`, `manifest_has_setup`, `bootstrap_from_git`, `complete` döner. Agent ile login:
`unity command bootstrap_login --token_env <DEĞİŞKEN> --confirm true --project-path <proje>`; token `<DEĞİŞKEN>` adlı
ortam değişkeninden **editör sürecinde** okunur (editörü değişkeni tanımladığın kabuktan aç; komut satırına token
yazılmaz) ve registry'de denenir (200 değilse yazılmaz). Kişiler pencereyle girer; komut CI/agent makineleri içindir
(`hl.read` token'ı). `confirm` olmadan ya da `--dry_run true` ile yalnız durumu döner, dosya yazmaz.

## Kurulum (Editor)

Menü: `Hyperlab → Login`. Pencere paket ilk yüklendiğinde bir kez kendiliğinden açılır. Token zaten varsa ve registry'ye
`GET` 200 dönüyorsa pencere açılmaz; yalnız manifest eksiği GitHub girişi olmadan tamamlanır (pencerede de "Complete setup" düğmesi).
Batchmode'da (CI, headless editör) hiçbir şey yapılmaz.

Akış: tarayıcıda `https://upm.hyperlab.games/-/oauth/authorize/cli` açılır; GitHub sonrası sunucu
`http://localhost:8239/?status=success&token=…` adresine yönlendirir; bootstrap bunu `HttpListener` ile alır (3 dk
zaman aşımı, iptal edilebilir). Token `GET <registry>/com.hyperlab.core` ile denenir; geçerliyse
`~/.upmconfig.toml` içine `[npmAuth."https://upm.hyperlab.games"]` altında yazılır (dosya yoksa izin 600 ile
oluşturulur). Sonra manifest'e `Hyperlab` scoped registry'si (scope `com.hyperlab`, `com.cysharp`) ve
`com.hyperlab.setup` eklenir; bootstrap Git URL ile kuruluysa bağımlılığı registry sürümüne
(`BootstrapDefaults.BootstrapVersion`) çevrilir. Yazılan setup sürümü `BootstrapDefaults.SetupVersion`'dır; yayınlanan son setup sürümüne
eşit tutulur (test denetler), yoksa yeni projeler eski setup'ı ve onun hatalarını alır.

## Inspector alanları

Inspector yok; `Hyperlab → Login` penceresinin alanları:

| Alan | Tür | Varsayılan | Açıklama |
|---|---|---|---|
| `Log in with GitHub` | düğme | — | Tarayıcıda GitHub girişini başlatır, token'ı alır ve yazar, manifest'i tamamlar. |
| `Cancel` | düğme | — | Yalnız beklerken görünür: girişi iptal eder, portu bırakır. |
| `Complete setup (token already stored)` | düğme | — | GitHub girişi olmadan: saklı token geçerliyse yalnız manifest eksiğini tamamlar. |

## Kod API'si

Hepsi `Hyperlab.Bootstrap` ad alanında, Editor asmdef'indedir.

- `BootstrapService.Status(BootstrapPaths)`: `~/.upmconfig.toml`'da token ve manifest'te scoped registry ile
  `com.hyperlab.setup` var mı (`BootstrapStatus`: `TokenPresent`, `ManifestHasRegistry`, `ManifestHasSetup`,
  `BootstrapFromGit`, `NeedsLogin`, `Complete`; bootstrap Git URL ile kuruluyken `Complete` false). Yalnız dosyalara bakar; ağ isteği atmaz.
- `BootstrapService.ApplyTokenAsync(IHttp, BootstrapPaths, token)`: token'ı registry'de yoklar (200 devam, 403
  `WrongCredentials` + `NotInTeamMessage`, 401 `WrongCredentials` + `TokenRejectedMessage`, 0 `Unreachable`, diğer `Rejected`; yoklama başarısızsa hiçbir dosya yazılmaz);
  başarılıysa token'ı ve manifest'i yazar, `ResolveRequested` bayrağını kaldırır (`AutoOpen` ana iş parçacığında `Client.Resolve()` çağırır). Yoklama ya da
  manifest okuma/ayrıştırma hatasında hiçbir dosyaya yazılmaz; token yazıldıktan sonra manifest yazılamazsa token
  kalır ve mesaj bunu söyler. Manifest'te `com.hyperlab.setup` zaten varsa (token yenileme) mesaj "Logged in. Hyperlab
  Setup is already installed." olur.
- `BootstrapService.CompleteManifestAsync(IHttp, BootstrapPaths)`: GitHub girişi olmadan; saklı token `GET` ile doğrulanırsa yalnız
  manifest eksiğini tamamlar (PUT atmaz).
- `GitHubLogin.LoginAsync(IHttp, BootstrapPaths, openUrl, ct, port, timeout)`: tarayıcıyı açtırır, yönlendirmeyi bekler,
  token'ı `ApplyTokenAsync`'e verir. `GitHubLogin.AuthorizeUrl`, `GitHubLogin.ParseCallback`, `GitHubLogin.CallbackPort`
  (8239, eklentide sabit). `LoopbackReceiver.WaitAsync(port, onListening, timeout, ct)`: yerel yönlendirme dinleyicisi.
- `RegistryLogin.ProbeTokenAsync` / `TokenWorksAsync`: token yoklaması (`GET <registry>/com.hyperlab.core`).
- `UpmConfigWriter.SetToken` / `TokenFor` / `SameUrl`: `.upmconfig.toml` metninde yalnız ilgili bölümü yazar ya da okur.
- `ManifestWriter.Ensure`: manifest metnine registry ve bağımlılığı ekler; bozuk JSON'da `FormatException` atar.
- `ManifestWriter.UseRegistryFor(json, package, version)`: paket Git URL ile kuruluysa bağımlılığı `version`'a çevirir;
  `file:`, sürüm ya da eksik bağımlılığa dokunmaz. `ManifestWriter.IsGitSource(value)`: UPM Git bağımlılığı mı.
- `IHttp` (`HttpClientAdapter` gerçek gerçeklemesi): test için HTTP sınırı; `Status == 0` ağ hatasıdır.
- `BootstrapPaths.Default()`: gerçek `~/.upmconfig.toml` ve proje `Packages/manifest.json` yolları;
  `BootstrapDefaults`: registry adresi, scope'lar, Setup sürümü, bootstrap'ın registry sürümü (`BootstrapVersion`,
  `package.json` ile eşit; test denetler).

- `LoginResult` (`Status`, `Token`, `Message`): `ApplyTokenAsync`, `CompleteManifestAsync` ve `GitHubLogin.LoginAsync`'in sonucu.
  `LoginStatus`: `Ok`, `WrongCredentials` (403: GitHub hesabı takımda değil; 401: token registry'de geçersiz ya da süresi dolmuş), `Unreachable` (ağ hatası),
  `Rejected` (diğer hatalar: token ya da manifest yazılamadı, manifest bozuk, saklı token yok). `Message` pencerede
  gösterilen metindir.
- `HttpResult` (`Status`, `Body`): `IHttp.SendAsync`'in sonucu; `Status == 0` ağ hatası.
- `ManifestResult` (`Json`, `Changed`): `ManifestWriter.Ensure`'un sonucu; `Changed == false` ise manifest zaten
  tamamdı, dosyaya yazılmaz.
- `LoginWindow.Open(string reason = null)`: pencereyi açar; `reason` verilirse mesaj alanında gösterilir (`AutoOpen`
  kendiliğinden açarken nedeni buraya yazar). Menü: `Hyperlab → Login`.
- `BootstrapCommands`: agent komutları `bootstrap_status` ve `bootstrap_login` (`token_env`, `dry_run`,
  `confirm`); token `token_env`'in adını verdiği ortam değişkeninden okunur.

## Veri (BGDatabase)

Yok.

## Davranış ayrıntıları

- Pencere proje başına bir kez, token yoksa ya da GitHub girişi olmadan tamamlama başarısızsa (neden pencerede yazar) kendiliğinden
  açılır; sonra yalnız menüden. Bootstrap `file:` ile kuruluysa ya da setup manifest'te varsa manifest'e registry yazılmaz,
  `GET`/Resolve atılmaz (`Complete` sayılır).
- Token ve manifest girişleri tamamsa pencere "Already set up" der. Token'ın registry'de hâlâ geçerli olup
  olmadığı otomatik denetlenmez (`TokenWorksAsync` yalnız API'dir).
- Bootstrap Git URL ile kuruluysa login (ya da "Complete setup") onu registry sürümüne çevirir. Çevrilmezse Hyperlab
  registry'si (scope `com.hyperlab`) aynı paketi sunduğu için Package Manager, sürüm aynı olsa da registry sürümünü
  "Update" düğmesiyle gösterir. Önceki sürümle kurulmuş projede pencere "Complete setup" düğmesini gösterir; tıklamak
  (ya da Package Manager'daki "Update") aynı düzeltmeyi yapar.
- Manifest bozuksa token da yazılmaz.
- Güvenlik notu: 3 dakikalık beklemede yerel herhangi bir süreç ya da web sayfası `localhost:8239`'a istek atabilir:
  girişi reddettirebilir (`status=error`) ya da başka geçerli bir registry token'ı enjekte edebilir (bu,
  `~/.upmconfig.toml`'u okumaktan kötü değildir). Eklentinin CLI akışında `state` parametresi yoktur. Yönlendirme
  adresi token'ı taşıdığı için token tarayıcı geçmişine düşer; erişim yine de her istekte takıma karşı yeniden denetlenir.
  Sunucunun `message` metni pencerede en çok 120 karakter gösterilir.
- Var olan registry adı ve scope'lar korunur; manifest'ten silme yok.
- `.upmconfig.toml`'da diğer bölümlere dokunulmaz.

## Tuzaklar

| Belirti | Sebep | Çözüm |
|---|---|---|
| `Your GitHub account is not in the hyperlabgames studio-developer or studio-cofounder team…` | GitHub hesabı takımda değil (registry 403) | Takıma eklenmeyi iste (`people.yaml`), sonra yeniden giriş yap |
| `The registry rejected the token (invalid or expired); log in again.` | Registry 401 verdi: token geçersiz ya da imza secret'ı döndürülmüş | `Log in with GitHub` ile yeniden giriş yap |
| Paketler 401 veriyor ve pencere parola istiyor | Eski bootstrap (GitHub girişinden önceki sürüm); sunucunun imza anahtarı döndüyse eski token'lar geçersiz | GitHub girişli bootstrap yayınlandıysa `https://github.com/hyperlabgames/hyperlab-bootstrap.git` adresinden yeniden ekle (yeni pencere GitHub ile girer); yayınlanmadıysa ya da olmazsa web token'ı elle yaz: `https://upm.hyperlab.games` → Login with GitHub → Configuration'daki token'ı `~/.upmconfig.toml`'a (bkz. `docs/contributing/registry-erisim.md` "Geçiş dönemi") |
| `Port 8239 is in use…` | Başka bir Hyperlab Login ya da `npx verdaccio-github-oauth-ui` açık | Onu kapat, yeniden dene |
| `GitHub login timed out` | Tarayıcıda izin verilmedi | Yeniden dene |
| Tarayıcı açılmadı | Varsayılan tarayıcı açılamadı | Dinleyici beklerken `https://upm.hyperlab.games/-/oauth/authorize/cli` adresini elle aç |
| "Cannot reach the Hyperlab registry." | Ağ yok ya da proxy | Bağlantıyı kontrol et, yeniden dene |
| "Could not write ~/.upmconfig.toml: <yol>" | Dosya izni ya da yol yok | Pencere dosya yolunu gösterir; token gösterilmez. Dosyanın ve klasörünün yazılabilir olduğunu denetle, yeniden dene. Manifest'e dokunulmaz |
| "Packages/manifest.json could not be read." | Manifest yok ya da okunamıyor | Dosyayı geri getir, yeniden dene; hiçbir şey yazılmaz |
| "Packages/manifest.json is not valid JSON" | Bozuk `Packages/manifest.json` | Dosyayı düzelt, yeniden dene; token yazılmaz |
| `chmod 600` başarısız | İzin ayarı en iyi çaba | Hata değil; token yine yazılır |
| Paketler son sürümdeyken Package Manager bootstrap için "Update" gösteriyor | Bootstrap Git URL ile kurulu, registry de aynı paketi sunuyor | `Hyperlab → Login` → "Complete setup" ya da Package Manager'da "Update": bootstrap registry sürümüne geçer |

## İlgili

- [Registry'e giriş yap](tarifler/giris-yapma.md): adım adım tarif
- [README](../README.md)
- `com.hyperlab.setup` → `Documentation~/kurulum-sureci.md`
