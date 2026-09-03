using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>Antwort des Lizenzservers.</summary>
    public class LizenzServerAntwort
    {
        public bool Ok { get; set; }
        public string Code { get; set; }        // Fehlercode des Servers
        public string Meldung { get; set; }     // Meldung für den Benutzer
        public string TokenJson { get; set; }   // signiertes Token (Rohform) bei Erfolg
        public bool NetzwerkFehler { get; set; } // true = Server nicht erreichbar (kein Ablehnungsgrund!)
    }

    /// <summary>
    /// HTTP-Anbindung an den Lizenzserver (WordPress-Plugin "epos-lizenz" auf
    /// epos-plan.de). Zustandslos: Die Aktivierung authentifiziert sich über
    /// den Lizenzschlüssel, die Nachprüfung über die Token-ID — es werden
    /// keine Passwörter und keine Projekt- oder Kundendaten übertragen.
    /// </summary>
    public class LizenzServerClient
    {
        public const string BASIS_URL = "https://epos-plan.de/wp-json/epos/v1/";

        private static readonly HttpClient _http = ErzeugeClient();

        private static HttpClient ErzeugeClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EPOS-Plan-Lizenz/1.0");
            return client;
        }

        /// <summary>Gerät mit Lizenzschlüssel und E-Mail aktivieren.</summary>
        public Task<LizenzServerAntwort> Aktivieren(string schluessel, string email)
        {
            return Anfrage("activate", new
            {
                schluessel = schluessel,
                email = email,
                geraete_id = GeraeteId.Ermitteln(),
                geraete_name = GeraeteId.Anzeigename(),
            });
        }

        /// <summary>Periodische Nachprüfung; liefert bei Erfolg ein frisches Token.</summary>
        public Task<LizenzServerAntwort> Nachpruefen(string tokenId)
        {
            return Anfrage("validate", new
            {
                token_id = tokenId,
                geraete_id = GeraeteId.Ermitteln(),
            });
        }

        /// <summary>Dieses Gerät von der Lizenz lösen.</summary>
        public Task<LizenzServerAntwort> Deaktivieren(string tokenId)
        {
            return Anfrage("deactivate", new
            {
                token_id = tokenId,
                geraete_id = GeraeteId.Ermitteln(),
            });
        }

        /// <summary>Testversion anfordern; der Schlüssel kommt per E-Mail.</summary>
        public Task<LizenzServerAntwort> TrialAnfordern(string email, string name)
        {
            return Anfrage("trial", new
            {
                email = email,
                geraete_id = GeraeteId.Ermitteln(),
                name = name ?? "",
            });
        }

        private async Task<LizenzServerAntwort> Anfrage(string endpunkt, object daten)
        {
            var antwort = new LizenzServerAntwort();
            try
            {
                string json = JsonSerializer.Serialize(daten);
                using var inhalt = new StringContent(json, Encoding.UTF8, "application/json");
                using HttpResponseMessage http = await _http.PostAsync(BASIS_URL + endpunkt, inhalt).ConfigureAwait(false);
                string koerper = await http.Content.ReadAsStringAsync().ConfigureAwait(false);

                using JsonDocument doc = JsonDocument.Parse(koerper);
                JsonElement wurzel = doc.RootElement;

                antwort.Ok = wurzel.TryGetProperty("ok", out JsonElement ok) && ok.ValueKind == JsonValueKind.True;
                if (wurzel.TryGetProperty("code", out JsonElement code)) antwort.Code = code.GetString();
                if (wurzel.TryGetProperty("meldung", out JsonElement meldung)) antwort.Meldung = meldung.GetString();
                if (antwort.Ok && wurzel.TryGetProperty("token", out JsonElement token))
                    antwort.TokenJson = token.GetRawText();
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Kein Netz / Server nicht erreichbar: ausdrücklich KEIN
                // Ablehnungsgrund — die Karenzzeit im LizenzManager greift.
                antwort.NetzwerkFehler = true;
                antwort.Meldung = "Der Lizenzserver ist zurzeit nicht erreichbar.";
            }
            catch (Exception ex)
            {
                antwort.NetzwerkFehler = true;
                antwort.Meldung = "Unerwartete Antwort des Lizenzservers: " + ex.Message;
            }
            return antwort;
        }
    }
}
