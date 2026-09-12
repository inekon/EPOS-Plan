using System;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Lizenzverwaltung (iU9-W15c.4).
    ///
    /// <para><b>Wozu.</b> <c>Form_LizenzVerwaltung</c> hielt zwei Zeichenketten und
    /// sonst nichts; alles Übrige kam aus <see cref="LizenzManager"/>,
    /// <see cref="LizenzServerClient"/> und <see cref="GeraeteId"/>. Die
    /// Razor-Komponente darf keinen davon rufen (Sicherheitsregel S-2 der Welle): Auf
    /// iOS liest <c>LizenzManager.Pruefe()</c> den Schlüsselbund SYNCHRON, und eine
    /// Komponente ruft immer vom Zeichenfaden (Befund W15c-B13). Dieser Controller ist
    /// die Naht dazwischen — die Hülle ruft ihn, die Komponente bekommt Werte und
    /// Delegaten.</para>
    ///
    /// <para><b>Was hier NICHT steht.</b> Kein Token, kein <c>RohJson</c>, kein
    /// Zeitanker (Regel S-3): Was die Komponente bekommt, ist im DOM sichtbar. Nach
    /// draußen gehen Anzeigetexte und ein sprachneutraler Zustandsname — nicht der
    /// Aufzählungstyp <see cref="LizenzStatus"/>, den <c>EPOS.UI</c> nicht kennen
    /// soll (dieselbe Linie wie <c>Seitenschluessel</c> gegenüber <c>Masken</c>).</para>
    ///
    /// <para><b>Der Geltungsbereich der Ablage und die gehashte Geräte-Id sind
    /// eingefroren</b> (Regeln S1/S2): Dieser Controller reicht keine Bereichsangabe
    /// durch und rechnet keine Kennung nach; er fragt nur.</para>
    /// </summary>
    internal static class LizenzCtrl
    {
        // ==================================================================
        //  Lagebild
        // ==================================================================

        /// <summary>
        /// Der Zustand als SPRACHNEUTRALER ASCII-Schlüssel — <c>GUELTIG</c>,
        /// <c>KULANZ</c>, <c>NACHPRUEFUNG</c>, <c>LESEMODUS</c>, <c>UHRMANIPULIERT</c>
        /// oder <c>NICHTAKTIVIERT</c>. Die Oberfläche entscheidet daran über die
        /// Statusfarbe, nicht über den Text.
        /// </summary>
        internal static string Zustandsname(LizenzStatus status)
        {
            switch (status)
            {
                case LizenzStatus.Gueltig: return "GUELTIG";
                case LizenzStatus.Kulanz: return "KULANZ";
                case LizenzStatus.NachpruefungFaellig: return "NACHPRUEFUNG";
                case LizenzStatus.Lesemodus: return "LESEMODUS";
                case LizenzStatus.UhrManipuliert: return "UHRMANIPULIERT";
                default: return "NICHTAKTIVIERT";
            }
        }

        /// <summary>Der heutige Zustand dieses Arbeitsplatzes.</summary>
        internal static string Zustandsname() => Zustandsname(LizenzManager.Pruefe());

        /// <summary>Der Statussatz zum heutigen Zustand (<c>LIZ_ST_*</c>).</summary>
        internal static string Statustext() => LizenzManager.StatusText();

        /// <summary>Liegt ein signaturgeprüftes Token vor?</summary>
        internal static bool HatToken => LizenzManager.Token != null;

        /// <summary>
        /// Die zweizeilige Detailangabe unter dem Status: Lizenz-Id, Firma, Benutzer,
        /// Gerätename (<c>LIZ_DETAIL</c>) — ohne Token der Ersatztext
        /// <c>LIZ_DETAIL_KEINE</c>.
        /// </summary>
        internal static string Detailtext()
        {
            LizenzToken token = LizenzManager.Token;
            if (token == null) return MyResource.Resource.LIZ_DETAIL_KEINE;

            return string.Format(MyResource.Resource.LIZ_DETAIL,
                                 token.LizenzId, token.Firma, token.Benutzer, GeraeteId.Anzeigename());
        }

        /// <summary>Anzeigename dieses Geräts (Rechnername bzw. Gerätename).</summary>
        internal static string GeraetName() => GeraeteId.Anzeigename();

        /// <summary>Adresse des Lizenzportals.</summary>
        internal static string PortalUrl => LizenzManager.PORTAL_URL;

        // ==================================================================
        //  Prüfregeln
        // ==================================================================

        /// <summary>
        /// Gleiche Maßstäbe wie WordPress' <c>is_email()</c>: Lokalteil@Domain, Domain
        /// mit mindestens einem Punkt, keine Leer- oder Sonderzeichen.
        /// </summary>
        /// <remarks>
        /// Die Regel stand bis iU9-W15c privat in <c>Form_LizenzVerwaltung</c>
        /// (<c>:281-287</c>) und gehört hierher: Sie bildet eine SERVERREGEL nach — der
        /// Lizenzserver weist eine Adresse zurück, die <c>is_email()</c> nicht besteht —
        /// und ist damit keine Anzeigefrage.
        /// </remarks>
        internal static bool EmailGueltig(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (!System.Net.Mail.MailAddress.TryCreate(email, out System.Net.Mail.MailAddress adresse))
                return false;
            // WordPress verlangt einen Punkt in der Domain (name@firma ist dort ungültig)
            return adresse.Host.Contains('.') && adresse.Address == email;
        }

        // ==================================================================
        //  Die vier Wege nach draußen
        // ==================================================================

        /// <summary>
        /// Lizenzschlüssel und E-Mail aus einer <c>.lic</c>-Datei lesen — <b>ohne
        /// Signaturprüfung</b>, denn es sind nur Eingabewerte (Regel S3, Befund
        /// W15c-B21). Geprüft wird das Token, das der Server nach <c>activate</c>
        /// zurückgibt.
        /// </summary>
        internal static (string Schluessel, string Email) LicLesen(string pfad)
        {
            LizenzManager.LicDateiLesen(pfad, out string schluessel, out string email);
            return (schluessel ?? "", email ?? "");
        }

        /// <summary>
        /// Aktivierung gegen den Lizenzserver. Der Schlüssel geht nur in DIESE Richtung
        /// hinaus und wird nirgends abgelegt (Regel S4).
        /// </summary>
        internal static async Task<(bool Ok, string Meldung)> Aktivieren(string schluessel, string email)
        {
            LizenzServerAntwort antwort = await LizenzManager.Aktivieren(schluessel, email)
                                                             .ConfigureAwait(false);
            return (antwort.Ok, antwort.Meldung);
        }

        /// <summary>
        /// Testversion anfordern; der Schlüssel kommt per E-Mail. Der Name ist eine
        /// reine Zusatzangabe und kommt von der HÜLLE (Entscheid E-16) — unter Windows
        /// der Anmeldename, auf iOS der Gerätename.
        /// </summary>
        internal static async Task<(bool Ok, string Meldung)> Trial(string email, string name)
        {
            LizenzServerAntwort antwort = await new LizenzServerClient().TrialAnfordern(email, name)
                                                                        .ConfigureAwait(false);
            return (antwort.Ok, antwort.Meldung);
        }

        /// <summary>
        /// Dieses Gerät von der Lizenz lösen. <c>Netzfehler</c> ist ausdrücklich KEIN
        /// Ablehnungsgrund — die Oberfläche meldet dann „Server nicht erreichbar", und
        /// das Token bleibt liegen.
        /// </summary>
        internal static async Task<(bool Ok, bool Netzfehler, string Meldung)> Freigeben()
        {
            LizenzServerAntwort antwort = await LizenzManager.Freigeben().ConfigureAwait(false);
            return (antwort.Ok, antwort.NetzwerkFehler, antwort.Meldung);
        }
    }
}
