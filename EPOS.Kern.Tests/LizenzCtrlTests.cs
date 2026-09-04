using System;
using System.IO;
using System.Text;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="LizenzCtrl"/> — die Datenseite der Lizenzverwaltung (iU9-W15c.4).
    ///
    /// <para>Geprüft wird, was OHNE Netz und ohne Ablage entscheidbar ist: die
    /// sprachneutralen Zustandsnamen, die E-Mail-Regel (WordPress' <c>is_email()</c>)
    /// und das Lesen einer <c>.lic</c>-Datei. Die vier <c>await</c>-Wege gehen an den
    /// Lizenzserver und sind deshalb kein Fall dieses Zeugen — sie werden in der
    /// Komponente über Delegaten geprüft (<c>LizenzVerwaltungDialogTests</c>).</para>
    /// </summary>
    public class LizenzCtrlTests
    {
        // ==================================================================
        //  Die sechs Zustandsnamen
        // ==================================================================

        /// <summary>
        /// Jeder Zustand hat genau einen sprachneutralen ASCII-Namen. Die Oberfläche
        /// entscheidet daran über die Statusfarbe; ein Anzeigetext an dieser Stelle
        /// wäre nach der Drei-Schichten-Regel ein Fehler.
        /// </summary>
        [Theory]
        [InlineData(LizenzStatus.Gueltig, "GUELTIG")]
        [InlineData(LizenzStatus.Kulanz, "KULANZ")]
        [InlineData(LizenzStatus.NachpruefungFaellig, "NACHPRUEFUNG")]
        [InlineData(LizenzStatus.Lesemodus, "LESEMODUS")]
        [InlineData(LizenzStatus.UhrManipuliert, "UHRMANIPULIERT")]
        [InlineData(LizenzStatus.NichtAktiviert, "NICHTAKTIVIERT")]
        public void Jeder_Zustand_hat_einen_sprachneutralen_Namen(LizenzStatus status, string erwartet)
        {
            Assert.Equal(erwartet, LizenzCtrl.Zustandsname(status));
        }

        // ==================================================================
        //  Die E-Mail-Regel
        // ==================================================================

        /// <summary>
        /// Gültige Adressen nach denselben Maßstäben, die der Lizenzserver anlegt.
        /// </summary>
        [Theory]
        [InlineData("name@firma.de")]
        [InlineData("vorname.nachname@teil.firma.example")]
        [InlineData("a@b.co")]
        public void Eine_gueltige_Adresse_wird_angenommen(string email)
        {
            Assert.True(LizenzCtrl.EmailGueltig(email));
        }

        /// <summary>
        /// <b>Der Punkt in der Domain ist die Besonderheit</b>: <c>name@firma</c> ist
        /// nach .NET eine gültige Adresse, nach WordPress' <c>is_email()</c> nicht — und
        /// der Server ist die Instanz, die entscheidet. Ebenso wird ein Anzeigename
        /// abgewiesen: <c>MailAddress</c> nimmt „Max &lt;a@b.de&gt;" an, der Vergleich
        /// <c>Address == email</c> nicht.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("kein-at-zeichen")]
        [InlineData("name@firma")]
        [InlineData("Max Mustermann <max@firma.de>")]
        [InlineData("zwei@@firma.de")]
        public void Eine_ungueltige_Adresse_wird_abgewiesen(string email)
        {
            Assert.False(LizenzCtrl.EmailGueltig(email));
        }

        // ==================================================================
        //  Die .lic-Datei
        // ==================================================================

        /// <summary>Schreibt eine <c>.lic</c>-Datei im Serverformat in den Temp-Ordner.</summary>
        private static string LicSchreiben(string inhalt)
        {
            string pfad = Path.Combine(Path.GetTempPath(),
                                       "epos-lic-" + Guid.NewGuid().ToString("N") + ".lic");
            File.WriteAllText(pfad, inhalt, Encoding.UTF8);
            return pfad;
        }

        private static string LicInhalt(string schluessel, string email)
        {
            string innen = JsonSerializer.Serialize(new { schluessel = schluessel, email = email });
            return JsonSerializer.Serialize(new
            {
                format = "epos-signiert-1",
                nutzdaten = Convert.ToBase64String(Encoding.UTF8.GetBytes(innen)),
                signatur = "",
            });
        }

        /// <summary>
        /// Aus einer <c>.lic</c>-Datei kommen die beiden EINGABEWERTE heraus — mehr
        /// nicht. Geprüft wird die Signatur hier ausdrücklich NICHT (Regel S3): Sie
        /// gehört an das Token, das der Server nach <c>activate</c> zurückgibt.
        /// </summary>
        [Fact]
        public void Aus_einer_lic_Datei_kommen_Schluessel_und_Email()
        {
            string pfad = LicSchreiben(LicInhalt("EPOS-F-04795-LFKP-XYYU-ML", "kunde@firma.de"));
            try
            {
                var (schluessel, email) = LizenzCtrl.LicLesen(pfad);

                Assert.Equal("EPOS-F-04795-LFKP-XYYU-ML", schluessel);
                Assert.Equal("kunde@firma.de", email);
            }
            finally { File.Delete(pfad); }
        }

        /// <summary>
        /// Eine Datei ohne Schlüssel liefert leere Zeichenketten statt <c>null</c> —
        /// die Oberfläche meldet dann <c>LIZ_MSG_LIC_OHNE_SCHLUESSEL</c>.
        /// </summary>
        [Fact]
        public void Eine_lic_Datei_ohne_Schluessel_liefert_leere_Werte()
        {
            string pfad = LicSchreiben(LicInhalt(null, "kunde@firma.de"));
            try
            {
                var (schluessel, _) = LizenzCtrl.LicLesen(pfad);
                Assert.Equal("", schluessel);
            }
            finally { File.Delete(pfad); }
        }

        /// <summary>
        /// Eine fremde Datei bringt den Leser nicht zu Fall — er meldet leer, und die
        /// Oberfläche sagt „kein gültiger Lizenzschlüssel gefunden".
        /// </summary>
        [Fact]
        public void Eine_fremde_Datei_bringt_den_Leser_nicht_zu_Fall()
        {
            string pfad = LicSchreiben("das ist kein Lizenzdokument");
            try
            {
                var (schluessel, email) = LizenzCtrl.LicLesen(pfad);
                Assert.Equal("", schluessel);
                Assert.Equal("", email);
            }
            finally { File.Delete(pfad); }
        }

        /// <summary>Eine gar nicht vorhandene Datei ebenso wenig.</summary>
        [Fact]
        public void Eine_fehlende_Datei_meldet_leer()
        {
            var (schluessel, email) = LizenzCtrl.LicLesen(
                Path.Combine(Path.GetTempPath(), "gibt-es-nicht-" + Guid.NewGuid().ToString("N") + ".lic"));

            Assert.Equal("", schluessel);
            Assert.Equal("", email);
        }

        /// <summary>
        /// Die Portaladresse kommt aus dem Kern und nicht aus der Oberfläche — sie
        /// steht in genau einer Konstante.
        /// </summary>
        [Fact]
        public void Die_Portaladresse_steht_im_Kern()
        {
            Assert.Equal("https://epos-plan.de/lizenzportal/", LizenzCtrl.PortalUrl);
            Assert.Equal(LizenzManager.PORTAL_URL, LizenzCtrl.PortalUrl);
        }
    }
}
