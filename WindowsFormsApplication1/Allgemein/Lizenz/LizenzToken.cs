using System;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Signiertes Lizenz-Token des Lizenzservers (Format "epos-signiert-1").
    ///
    /// Der Server liefert { format, nutzdaten, signatur }: nutzdaten sind die
    /// Base64-codierten, exakten JSON-Bytes; die Ed25519-Signatur wird über
    /// genau diese Bytes geprüft. Erst nach erfolgreicher Prüfung wird das
    /// JSON geparst — auf Client-Seite ist keinerlei Kanonisierung nötig.
    /// </summary>
    public class LizenzToken
    {
        /// <summary>
        /// Öffentlicher Ed25519-Signaturschlüssel des Lizenzservers
        /// (epos-plan.de, Base64). Muss zum privaten Schlüssel auf dem Server
        /// passen; bei einem Schlüsseltausch hier aktualisieren.
        /// </summary>
        public const string OEFFENTLICHER_SCHLUESSEL_BASE64 = "sMcmb2GQqE1cGv98J01FvJ/+W1faogMUQfK+lPfG3Kk=";

        // ---- Nutzdaten (Felder des inneren JSON "epos-token-1") ----
        public string LizenzId { get; private set; }        // z. B. "EPOS-2026-04795"
        public int Nummer { get; private set; }             // Lizenznummer
        public string Firma { get; private set; }
        public string Benutzer { get; private set; }        // E-Mail
        public string GeraeteId { get; private set; }
        public string TokenId { get; private set; }         // UUID für /validate
        public string Typ { get; private set; }             // "demo" | "person" | "firma"
        public string Edition { get; private set; }
        public DateTime? GueltigAb { get; private set; }
        public DateTime? GueltigBis { get; private set; }   // Lizenzende (einschließlich)
        public int KulanzTage { get; private set; }
        public DateTime? TokenBis { get; private set; }     // Offline-Leine
        public DateTimeOffset? Ausgestellt { get; private set; }

        /// <summary>Die signierte Rohform, wie sie gespeichert und erneut geladen wird.</summary>
        public string RohJson { get; private set; }

        /// <summary>
        /// Parst und prüft ein signiertes Token. Liefert null bei ungültiger
        /// Signatur oder unbrauchbarem Format; der Grund steht in <paramref name="fehler"/>.
        /// </summary>
        public static LizenzToken Laden(string signiertesJson, out string fehler)
        {
            fehler = null;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(signiertesJson);
                JsonElement wurzel = doc.RootElement;

                if (!wurzel.TryGetProperty("format", out JsonElement fmt) ||
                    fmt.GetString() != "epos-signiert-1")
                {
                    fehler = "Unbekanntes Token-Format.";
                    return null;
                }

                byte[] nutzdaten = Convert.FromBase64String(wurzel.GetProperty("nutzdaten").GetString() ?? "");
                byte[] signatur = Convert.FromBase64String(wurzel.GetProperty("signatur").GetString() ?? "");

                if (!SignaturPruefen(nutzdaten, signatur))
                {
                    fehler = "Die Signatur des Lizenz-Tokens ist ungültig.";
                    return null;
                }

                LizenzToken token = NutzdatenParsen(nutzdaten);
                if (token == null)
                {
                    fehler = "Die Nutzdaten des Lizenz-Tokens sind unlesbar.";
                    return null;
                }
                token.RohJson = signiertesJson;
                return token;
            }
            catch (Exception ex)
            {
                fehler = "Lizenz-Token unlesbar: " + ex.Message;
                return null;
            }
        }

        /// <summary>Ed25519-Signaturprüfung über die exakten Nutzdaten-Bytes.</summary>
        private static bool SignaturPruefen(byte[] nachricht, byte[] signatur)
        {
            try
            {
                byte[] oeffentlich = Convert.FromBase64String(OEFFENTLICHER_SCHLUESSEL_BASE64);
                var schluessel = new Ed25519PublicKeyParameters(oeffentlich, 0);
                var pruefer = new Ed25519Signer();
                pruefer.Init(false, schluessel);
                pruefer.BlockUpdate(nachricht, 0, nachricht.Length);
                return pruefer.VerifySignature(signatur);
            }
            catch
            {
                return false;
            }
        }

        private static LizenzToken NutzdatenParsen(byte[] nutzdaten)
        {
            using JsonDocument doc = JsonDocument.Parse(Encoding.UTF8.GetString(nutzdaten));
            JsonElement e = doc.RootElement;

            if (!e.TryGetProperty("format", out JsonElement fmt) || fmt.GetString() != "epos-token-1")
                return null;

            var t = new LizenzToken
            {
                LizenzId = Text(e, "lizenz_id"),
                Nummer = Zahl(e, "nummer"),
                Firma = Text(e, "firma"),
                Benutzer = Text(e, "benutzer"),
                GeraeteId = Text(e, "geraete_id"),
                TokenId = Text(e, "token_id"),
                Typ = Text(e, "typ"),
                Edition = Text(e, "edition"),
                GueltigAb = Datum(e, "gueltig_ab"),
                GueltigBis = Datum(e, "gueltig_bis"),
                KulanzTage = Zahl(e, "kulanz_tage"),
                TokenBis = Datum(e, "token_bis"),
            };
            string ausgestellt = Text(e, "ausgestellt");
            if (DateTimeOffset.TryParse(ausgestellt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTimeOffset dto))
                t.Ausgestellt = dto;
            return t;
        }

        private static string Text(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static int Zahl(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

        private static DateTime? Datum(JsonElement e, string name)
        {
            string s = Text(e, name);
            if (string.IsNullOrEmpty(s)) return null;
            return DateTime.TryParse(s, out DateTime d) ? d.Date : (DateTime?)null;
        }

        /// <summary>Lizenztyp als Anzeigetext.</summary>
        public string TypText()
        {
            switch (Typ)
            {
                case "demo": return "Demoversion";
                case "person": return "Personenbezogene Lizenz";
                case "firma": return "Firmenlizenz";
                default: return Typ ?? "-";
            }
        }
    }
}
