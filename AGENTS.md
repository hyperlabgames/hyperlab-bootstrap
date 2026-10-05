# com.hyperlab.bootstrap — agent rehberi

Yalnız Editor paketi. Boş projede registry token'ını ve manifest girişlerini hazırlar; sonra `com.hyperlab.setup` devralır.

## Paket ne yapar, ne yapmaz

- Yapar: `Hyperlab Login` penceresi (`Hyperlab → Login`); registry'den token alıp `~/.upmconfig.toml`'a yazar;
  `Packages/manifest.json`'a `Hyperlab` scoped registry'sini ve `com.hyperlab.setup`'ı ekler.
- Yapmaz: parola saklamaz, paket yayınlamaz, hesap açmaz, Setup'ın işini (oyun kurulumu, kart akışı) yapmaz.

## Bağlayıcı kurallar

- Parola hiçbir yere yazılmaz: dosya, log, hata mesajı, agent çıktısı. Yalnız bellekte kalır.
- `com.hyperlab.*` bağımlılığı yok: paket registry'den önce çözülmek zorunda.
- `.upmconfig.toml`'da yalnız kendi bölümüne (`[npmAuth."https://upm.hyperlab.games"]`) dokun; diğer bölümler korunur.
- Manifest'ten hiçbir şey silinmez; var olan registry adı ve scope'lar korunur. Bozuk manifest'e dokunulmaz.
- `BootstrapDefaults.SetupVersion` = `com.hyperlab.setup` `package.json` sürümü; setup yayınlanırken ikisi aynı
  release commit'inde değişir ve bootstrap da yayınlanır (`SetupVersionMatchesTheRegisteredSetupPackage`).

## Görev → sayfa haritası

| Görev | Oku |
|---|---|
| Login akışı, hata tablosu | [Giriş](Documentation~/giris.md) |
| Kurulum ve ilk açılış | [README](README.md) |

### Agent girişleri

| Komut | Ne yapar |
|---|---|
| `bootstrap_status` | Okur: token, scoped registry ve `com.hyperlab.setup` girişi var mı (`complete`). |
| `bootstrap_login` | `--user`, `--password_env` (parolanın okunacağı ortam değişkeni; parola argüman değildir), `confirm` / `dry_run`. `confirm=true` olmadan yalnız durumu döner. |

## Sık yapılan hatalar

- Registry 409 döner: yanlış kimlik ya da kullanıcı yok; ikisi ayırt edilmez, pencere "Wrong user name or password." der.
- `chmod` (izin 600) başarısız olursa hata değil uyarıdır; token yine yazılır.
- Token yazıldı ama paketler inmiyor: Unity'nin manifest'i yeniden çözmesini bekle ya da `Assets → Refresh`.

## Doğrulama

- `python3 Tools/affected-tests.py --run`
- `python3 Tools/ArchitectureChecks/check.py .` ve `python3 Tools/ArchitectureChecks/agent_guard.py .`
