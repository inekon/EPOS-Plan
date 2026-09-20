using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE E2 — die beiden CO₂-Zeilen der Kohärenzprüfung und die
    /// Erlaubnisschwelle</b> (Analysepapier 2026-09-19, Befunde R5, R6 und S-5).
    ///
    /// <para><b>R5 — CO₂ doppelt gebucht.</b> Der erfasste Arbeitspreis darf den
    /// CO₂-Anteil nach BEHG als Preisbestandteil ausweisen; dann steckt die Abgabe schon
    /// in den Energiekosten. Bucht der Lauf zusätzlich eine BEHG-Reihe, steht derselbe
    /// Betrag zweimal im Kapitalwert. Der Anwenderentscheid zu Frage Q3 der
    /// Mockup-Prüfung ist <b>Weg (a)</b>: ausweisen, nicht umrechnen — die Prüfung meldet
    /// mit Betrag, die Zahlen bleiben, wie sie sind.</para>
    ///
    /// <para><b>R6 — der Strommix-Rückfall.</b> Bis E2 war er ein gewöhnlicher
    /// Laufhinweis OHNE die Zahl. Jetzt ist er eine Kohärenzzeile MIT Wert und erreicht
    /// damit dieselben drei Ausgaben wie jede andere (Rubrik, Wort- und
    /// Excelbericht).</para>
    ///
    /// <para><b>S-5 — die Erlaubnisschwelle.</b> <c>STROMST_ERLAUBNISSCHWELLE</c> war der
    /// einzige gesäte Stromsteuer-Schlüssel ohne Leser. Ein Rechenwerk gibt es dazu
    /// nicht, eine Pflicht des Betreibers schon.</para>
    ///
    /// <para>Gemessen wird an <see cref="KohaerenzPruefung.Pruefe"/> selbst: Die
    /// Eingabegrößen des Laufs sind Felder des <c>KohaerenzLauf</c>, die Trägerseite
    /// kommt aus der Testdatenbank. Die Kultur ist gepinnt, weil die Texte aus den
    /// Satellitenressourcen kommen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KohaerenzCo2Tests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — führt mehrere Energieträger mit
        /// Preiszerlegung.</summary>
        private const int PROJEKT = 1030;

        /// <summary>Ein gepflegter CO₂-Anteil [ct/kWh]. Die Höhe ist gleichgültig —
        /// geprüft wird, DASS einer aktiv dasteht.</summary>
        private const double ANTEIL_CT_KWH = 1.44;

        /// <summary>Die gebuchte CO₂-Abgabe des Jahres 1 [€/a].</summary>
        private const double BEHG_EUR = 12_345.67;

        // =================================================================
        //  R5 — CO₂ doppelt gebucht
        // =================================================================

        /// <summary>
        /// <b>Aktiver CO₂-Bestandteil UND gebuchte BEHG-Reihe</b> — eine WARNUNG mit dem
        /// doppelt gebuchten Jahresbetrag und den Trägern, deren Preis ihn schon enthält.
        /// </summary>
        [Fact]
        public void Aktiver_CO2_Anteil_neben_gebuchter_BEHG_Reihe_warnt_mit_Betrag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(ANTEIL_CT_KWH, true);
            List<KohaerenzHinweis> l = KohaerenzPruefung.Pruefe(PROJEKT, Lauf(BEHG_EUR, null));

            KohaerenzHinweis zeile = Co2Zeile(l);
            Assert.True(zeile != null,
                "Keine CO₂-Doppelansatzzeile. Gefunden: " +
                string.Join(" | ", l.Select(h => h.Schwere + ": " + h.Text)));
            Assert.Equal(KohaerenzSchwere.WARNUNG, zeile.Schwere);
            Assert.Equal(BEHG_EUR, zeile.Betrag.Value, 2);
            Assert.Contains("12.345,67", zeile.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Gegenprobe ohne Doppelung — der abgeschaltete Anteil.</b> Derselbe Wert
        /// steht im Feld, sein Aktiv-Schalter ist aber aus: Dann rechnet der Arbeitspreis
        /// nicht mit ihm, und es gibt nichts doppelt zu buchen.
        /// </summary>
        [Fact]
        public void Abgeschalteter_CO2_Anteil_erzeugt_keine_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(ANTEIL_CT_KWH, false);
            Assert.Null(Co2Zeile(KohaerenzPruefung.Pruefe(PROJEKT, Lauf(BEHG_EUR, null))));
        }

        /// <summary>
        /// <b>Gegenprobe ohne Doppelung — keine BEHG-Reihe.</b> Der Anteil ist aktiv,
        /// der Lauf bucht aber keine CO₂-Abgabe: Dann steht der Betrag genau einmal da,
        /// nämlich im Arbeitspreis.
        /// </summary>
        [Fact]
        public void Ohne_gebuchte_CO2_Abgabe_bleibt_die_Zeile_weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(ANTEIL_CT_KWH, true);
            Assert.Null(Co2Zeile(KohaerenzPruefung.Pruefe(PROJEKT, Lauf(0.0, null))));
        }

        /// <summary>
        /// <b>Die Zeile hängt an KEINEM Steuerpfad.</b> Ein Lauf ohne Steuereingabe — ein
        /// reines Kesselprojekt ohne jede Entlastungswahl — trägt den Doppelansatz
        /// genauso. Ohne diesen Fall stünde die Prüfung hinter der Abfrage auf
        /// <c>lauf.Steuer</c> und schwiege dort.
        /// </summary>
        [Fact]
        public void Auch_ohne_Steuerpfad_steht_die_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(ANTEIL_CT_KWH, true);
            var ohneSteuer = new KohaerenzLauf { Jahr = 2026, Steuer = null, Co2AbgabeEur = BEHG_EUR };

            Assert.NotNull(Co2Zeile(KohaerenzPruefung.Pruefe(PROJEKT, ohneSteuer)));
        }

        /// <summary>
        /// <b>Rechenneutral.</b> Die Prüfung ist reine Leselogik: Sie liefert Zeilen und
        /// rührt die gebuchte Abgabe nicht an — dieselbe Haltung wie in der ganzen Klasse
        /// (Entscheidung BF2, „nur warnen").
        /// </summary>
        [Fact]
        public void Die_Zeile_aendert_den_gebuchten_Betrag_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(ANTEIL_CT_KWH, true);
            KohaerenzLauf lauf = Lauf(BEHG_EUR, null);
            KohaerenzPruefung.Pruefe(PROJEKT, lauf);

            Assert.Equal(BEHG_EUR, lauf.Co2AbgabeEur, 6);
        }

        // =================================================================
        //  R6 — der Strommix-Rückfall als Zeile MIT Wert
        // =================================================================

        /// <summary>
        /// Der Rückfall steht als HINWEIS und nennt den angewandten Vorgabewert — bis E2
        /// stand er als Laufbemerkung ohne die Zahl da.
        /// </summary>
        [Fact]
        public void Der_Strommix_Rueckfall_nennt_seinen_Wert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(null, false);
            List<KohaerenzHinweis> l = KohaerenzPruefung.Pruefe(
                PROJEKT, Lauf(0.0, KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH));

            KohaerenzHinweis zeile = l.FirstOrDefault(
                h => h.Text.Contains("Strommix-Vorgabewert", StringComparison.Ordinal));
            Assert.True(zeile != null,
                "Keine Strommix-Zeile. Gefunden: " +
                string.Join(" | ", l.Select(h => h.Schwere + ": " + h.Text)));
            Assert.Equal(KohaerenzSchwere.HINWEIS, zeile.Schwere);
            Assert.Contains("435", zeile.Text, StringComparison.Ordinal);
            Assert.Null(zeile.Betrag);   // ein Emissionsfaktor ist kein Geldbetrag
        }

        /// <summary>Gegenprobe: ohne Rückfall keine Zeile.</summary>
        [Fact]
        public void Ohne_Rueckfall_bleibt_die_Strommix_Zeile_weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Co2AnteileSetzen(null, false);
            Assert.DoesNotContain(KohaerenzPruefung.Pruefe(PROJEKT, Lauf(0.0, null)),
                h => h.Text.Contains("Strommix-Vorgabewert", StringComparison.Ordinal));
        }

        // =================================================================
        //  S-5 — die Erlaubnisschwelle
        // =================================================================

        /// <summary>
        /// Eine Anlage ab der Katalogschwelle (1.000 kW) bekommt die Zeile; die Schwelle
        /// steht im Text, und die Zeile sagt ausdrücklich, dass sie nichts rechnet.
        /// </summary>
        [Fact]
        public void Ab_der_Erlaubnisschwelle_steht_die_Hinweiszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<KohaerenzHinweis> l = KohaerenzPruefung.Pruefe(PROJEKT, LaufMitAnlage(1500.0));

            KohaerenzHinweis zeile = l.FirstOrDefault(
                h => h.Text.Contains("Erlaubnispflicht", StringComparison.Ordinal));
            Assert.True(zeile != null,
                "Keine Erlaubniszeile. Gefunden: " +
                string.Join(" | ", l.Select(h => h.Schwere + ": " + h.Text)));
            Assert.Equal(KohaerenzSchwere.HINWEIS, zeile.Schwere);
            Assert.Contains("1.000", zeile.Text, StringComparison.Ordinal);
        }

        /// <summary>Gegenprobe: knapp darunter bleibt die Zeile weg.</summary>
        [Fact]
        public void Unter_der_Erlaubnisschwelle_bleibt_die_Zeile_weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.DoesNotContain(KohaerenzPruefung.Pruefe(PROJEKT, LaufMitAnlage(999.0)),
                h => h.Text.Contains("Erlaubnispflicht", StringComparison.Ordinal));
        }

        /// <summary>
        /// Ein Kessel erzeugt keinen Strom — die Schwelle des StromStG geht ihn nichts
        /// an, auch wenn seine (thermische) Leistung darüber liegt. Derselbe Befund wie
        /// S-3: Eine elektrische Angabe gehört nicht an eine Anlage ohne Stromerzeugung.
        /// </summary>
        [Fact]
        public void Eine_Anlage_ohne_Stromerzeugung_bleibt_aussen_vor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KohaerenzLauf lauf = LaufMitAnlage(1500.0);
            lauf.Steuer.Anlagen[0].Stromerzeuger = false;

            Assert.DoesNotContain(KohaerenzPruefung.Pruefe(PROJEKT, lauf),
                h => h.Text.Contains("Erlaubnispflicht", StringComparison.Ordinal));
        }

        // =================================================================
        //  S-3 — „(0 kW)" am Kessel
        // =================================================================

        /// <summary>
        /// <b>Befund S-3.</b> Der Klartext einer Anlage nennt die ELEKTRISCHE
        /// Nennleistung. Ein Kessel führt dort 0, und die Meldung las sich als
        /// „Heizkessel (0 kW)" — eine Angabe, die es an einem Kessel gar nicht gibt.
        /// </summary>
        [Fact]
        public void Der_Klartext_eines_Kessels_nennt_keine_elektrische_Leistung()
        {
            var kessel = new SteuerAnlage
            { Bezeichner = "Heizkessel 1", PelKW = 0.0, Stromerzeuger = false };
            var bhkw = new SteuerAnlage
            { Bezeichner = "BHKW 1", PelKW = 250.0, Stromerzeuger = true };

            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("Heizkessel 1", kessel.Klartext(de));
            Assert.Equal("BHKW 1 (250 kW)", bhkw.Klartext(de));
        }

        // =================================================================
        //  Prüfstand
        // =================================================================

        private static KohaerenzHinweis Co2Zeile(List<KohaerenzHinweis> l)
        {
            return l.FirstOrDefault(
                h => h.Text.Contains("CO₂ doppelt angesetzt", StringComparison.Ordinal));
        }

        /// <summary>Ein Lauf ohne Steueranlagen — er trägt nur die beiden E2-Größen.</summary>
        private static KohaerenzLauf Lauf(double co2Eur, double? strommix)
        {
            return new KohaerenzLauf
            {
                Jahr = 2026,
                Steuer = new SteuerEingabe { Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE },
                Co2AbgabeEur = co2Eur,
                StrommixRueckfallGJeKwh = strommix
            };
        }

        /// <summary>Ein Lauf mit EINER stromerzeugenden Anlage der gewünschten
        /// Nennleistung.</summary>
        private static KohaerenzLauf LaufMitAnlage(double pelKW)
        {
            KohaerenzLauf lauf = Lauf(0.0, null);
            lauf.Steuer.Anlagen.Add(new SteuerAnlage
            {
                Bezeichner = "Prüfanlage E2",
                PelKW = pelKW,
                Stromerzeuger = true
            });
            return lauf;
        }

        /// <summary>
        /// Setzt den CO₂-Anteil JEDES Energieträgers des Projekts.
        /// <paramref name="wert"/> = <c>null</c> löscht ihn („kein Anteil erfasst").
        /// </summary>
        private static void Co2AnteileSetzen(double? wert, bool aktiv)
        {
            var ctrl = new BrennstoffBestandteilCtrl();
            foreach (int carrier in Traeger())
            {
                BrennstoffBestandteilModel m = ctrl.Read(PROJEKT, carrier);
                m.ID_Projekt = PROJEKT;
                m.ID_Energietraeger = carrier;
                m.CO2 = wert;
                m.CO2_Aktiv = aktiv;
                ctrl.Update(m);
            }
        }

        /// <summary>Die Energieträger, für die das Projekt eine Preiszerlegung führt.</summary>
        private static List<int> Traeger()
        {
            var l = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID_Energieträger] FROM [" + BrennstoffBestandteilCtrl.TABLE + "] " +
                "WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            if (dt == null) return l;
            foreach (DataRow r in dt.Rows)
                if (r[0] != null && r[0] != DBNull.Value) l.Add(Convert.ToInt32(r[0]));
            return l;
        }
    }
}
