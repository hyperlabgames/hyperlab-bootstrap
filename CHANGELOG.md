# Changelog

## [Unreleased]

## [0.1.1] - 2026-10-05

### Fixed
- Bootstrap yeni projeye `com.hyperlab.setup` 0.1.0 yazıyordu; 0.1.1'deki düzeltmeler (ör. anahtarsız "Save keys"te boş "Review Changes" yerine "No changes.") gelmiyordu. `BootstrapDefaults.SetupVersion` 0.1.1 oldu; test, sabitin setup'ın `package.json` sürümüne eşit olduğunu denetler.

## [0.1.0] - 2026-10-05
- İlk sürüm: `Hyperlab → Login` penceresi, `BootstrapService` (token + manifest yazımı), ilk açılışta otomatik pencere, `bootstrap_status` ve `bootstrap_login` agent komutları.
- Yerel (`file:`) kurulum ya da manifest'te setup varken otomatik tamamlama registry yazmaz/GET atmaz; "Complete setup" ve giriş sonucu mesajı artık silinmez, `AutoOpen` başarısızlıkta nedeni pencerede gösterir; ağ yokken ayrı "Cannot reach" mesajı; `AutoOpen` log'u yalnız istisna türünü yazar.
- Token varken parolasız manifest tamamlama (`BootstrapService.CompleteManifestAsync`, pencerede "Complete setup"); `AutoOpen` batchmode'da çalışmaz ve istisnayı log'a yazar; boş kullanıcı adı istek atmadan uyarılır; token yazıldı ama manifest yazılamazsa mesaj bunu belirtir.
