# Changelog

## [Unreleased]

## [0.1.7] - 2026-10-16
### Fixed
- Giriş ve registry istekleri `ProxyFallbackHandler` ile gider: Unity Hub'ın taşıdığı ölü proxy ortam değişkeni ilk kurulumda "Cannot reach" hatası vermez (kod Setup'takinin kopyası, ikisi birlikte değişir).
- `ManifestWriter.Ensure`, başka bir scoped registry'nin zaten sahip olduğu scope'u (ör. OpenUPM'deki `com.cysharp`) Hyperlab registry'sine eklemez.

## [0.1.6] - 2026-10-15
### Changed
- Yeni projeler Setup 0.5.0 ile kurulur (`BootstrapDefaults.SetupVersion`).

## [0.1.5] - 2026-10-12
### Changed
- Başarılı girişten sonra Login penceresi kapanır, sonuç Console'a yazılır. Paketler inip alan yeniden yüklenince
  Setup sihirbazı kendiliğinden açılır (daha önce açıldı işareti olsa bile; istek `SessionState`'te taşınır).
- Giriş GitHub ile yapılır: `Hyperlab → Login` tarayıcıda GitHub'ı açar, token `localhost:8239` yönlendirmesiyle
  alınıp registry'de denenir ve `~/.upmconfig.toml`'a yazılır. Erişim `hyperlabgames` `studio-developer` ve `studio-cofounder` takımlarıdır (kurucu takımı okur, yayın yapamaz).
- **Kırıcı:** kullanıcı adı/parola girişi kalktı (`BootstrapService.RunAsync`, `RegistryLogin.LoginAsync` silindi;
  yerine `BootstrapService.ApplyTokenAsync`, `GitHubLogin.LoginAsync`). Agent komutu `bootstrap_login` artık
  `user`/`password_env` değil `token_env` alır. `BasicAuth` silindi ve `IHttp.SendAsync` artık Basic-auth parametresi (`basicUser`, `basicPassword`) almaz.
- Registry 401 (token geçersiz ya da süresi dolmuş) artık takım hatasından ayrı mesaj verir (`TokenRejectedMessage`); 403 `NotInTeamMessage` kalır. Sunucunun `message` metni pencerede 120 karakterle sınırlanır.
### Documentation
- Giriş tarifi ve ekran görüntüsü GitHub girişine göre güncellendi.
- Editör tarifi: Registry'e giriş yap (`Documentation~/tarifler/giris-yapma.md`), ekran görüntülü.

## [0.1.4] - 2026-10-10
### Fixed
- Git URL ile kurulan bootstrap, login sonrası Package Manager'da (paketler son sürümdeyken de) "Update" düğmesiyle
  görünüyordu: Hyperlab registry'si aynı paketi sunuyordu. Login ve "Complete setup" artık bootstrap'ın manifest
  girişini registry sürümüne (`BootstrapDefaults.BootstrapVersion`) çevirir; eski kurulumlarda `Complete` false olur
  ve pencere "Complete setup" düğmesini gösterir. `bootstrap_status` `bootstrap_from_git` döner.

## [0.1.3] - 2026-10-06
### Changed
- `BootstrapDefaults.SetupVersion` 0.2.1: yeni projeler setup 0.2.1 ile kurulur (voodoo 0.2.1).

## [0.1.2] - 2026-10-05
### Changed
- `BootstrapDefaults.SetupVersion` 0.2.0: yeni projeler setup 0.2.0 ile kurulur.
### Fixed
- `bootstrap_login`, Setup zaten kuruluyken de "Hyperlab Setup is being installed." diyordu; artık "Logged in. Hyperlab
  Setup is already installed." der.

## [0.1.1] - 2026-10-05

### Fixed
- Bootstrap yeni projeye `com.hyperlab.setup` 0.1.0 yazıyordu; 0.1.1'deki düzeltmeler (ör. anahtarsız "Save keys"te boş "Review Changes" yerine "No changes.") gelmiyordu. `BootstrapDefaults.SetupVersion` 0.1.1 oldu; test, sabitin setup'ın `package.json` sürümüne eşit olduğunu denetler.

## [0.1.0] - 2026-10-05
- İlk sürüm: `Hyperlab → Login` penceresi, `BootstrapService` (token + manifest yazımı), ilk açılışta otomatik pencere, `bootstrap_status` ve `bootstrap_login` agent komutları.
- Yerel (`file:`) kurulum ya da manifest'te setup varken otomatik tamamlama registry yazmaz/GET atmaz; "Complete setup" ve giriş sonucu mesajı artık silinmez, `AutoOpen` başarısızlıkta nedeni pencerede gösterir; ağ yokken ayrı "Cannot reach" mesajı; `AutoOpen` log'u yalnız istisna türünü yazar.
- Token varken parolasız manifest tamamlama (`BootstrapService.CompleteManifestAsync`, pencerede "Complete setup"); `AutoOpen` batchmode'da çalışmaz ve istisnayı log'a yazar; boş kullanıcı adı istek atmadan uyarılır; token yazıldı ama manifest yazılamazsa mesaj bunu belirtir.
