# Hyperlab Bootstrap

Yeni geliştiricinin ilk adımı: Hyperlab registry'sine login olur, token'ı `~/.upmconfig.toml`'a yazar, projenin
`Packages/manifest.json`'una Hyperlab scoped registry'sini ve `com.hyperlab.setup` bağımlılığını ekler. Yalnız Editor;
`com.hyperlab.*` bağımlılığı yoktur (registry'den önce çözülebilsin diye).

## Kurulum

Boş Unity projesinde Package Manager → `+` → Add package from git URL:

```
https://github.com/hyperlabgames/hyperlab-bootstrap.git
```

Ortak kullanıcı adı ve parolayı ekipten al. Parola hiçbir yere yazılmaz.

## Akış

1. Paket yüklenince, token yoksa `Hyperlab Login` penceresi bir kez kendiliğinden açılır (menü: `Hyperlab → Login`).
2. Kullanıcı adı ve parola girilir; pencere registry'den token alır.
3. Token `~/.upmconfig.toml` içine `[npmAuth."https://upm.hyperlab.games"]` altında yazılır; dosyanın diğer bölümlerine
   dokunulmaz.
4. Projenin `Packages/manifest.json`'una `Hyperlab` scoped registry'si (scope `com.hyperlab` ve `com.cysharp`) ve
   `com.hyperlab.setup` eklenir. Unity paketleri çözer, Setup ilk açılışta kendini açar.
5. Pencere proje başına bir kez, giriş eksikse kendiliğinden açılır; token varsa pencere açılmaz, manifest eksiği
   parolasız tamamlanır (başarısızsa pencere nedeniyle açılır). `file:` ile kurulu (yerel / Sandbox) projede ya da
   `com.hyperlab.setup` manifest'te zaten varsa registry manifest'e yazılmaz. Sonrasında menüden açılır.

## Agent komutları

- `bootstrap_status`: token var mı, manifest'te registry ve `com.hyperlab.setup` var mı.
- `bootstrap_login`: `--user` ve `--password_env <DEĞİŞKEN>` (parola ortam değişkeninden okunur, argüman olarak geçmez); `confirm=true` ister, `dry_run` yalnız durumu döner.

Keşif: `unity command --tag hyperlab --detail compact --project-path Sandbox`.

## Dokümanlar

- [Giriş ve login akışı](Documentation~/giris.md)
- Agent kuralları için [AGENTS.md](AGENTS.md); değişiklikler için [CHANGELOG.md](CHANGELOG.md).
