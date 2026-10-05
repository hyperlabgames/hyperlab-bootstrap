using System;
using System.Text;

namespace Hyperlab.Bootstrap
{
    /// <summary>HTTP Basic kimlik doğrulama başlığı değerini üretir (saf yardımcı, test edilebilir).</summary>
    public static class BasicAuth
    {
        /// <summary>"Basic " önekinden sonraki değeri döndürür: UTF-8 "kullanıcı:parola" baytlarının base64'ü.</summary>
        public static string Header(string user, string password)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password));
    }
}
