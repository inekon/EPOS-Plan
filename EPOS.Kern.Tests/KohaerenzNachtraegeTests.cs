using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE B6 — die Nachträge der Kohärenzprüfung</b> (offene Punkte 6 und 7 der
    /// Liste „Nach B6" des Wirtschaftlichkeitskonzepts).
    ///
    /// <para><b>Punkt 7 — die positive Nennung (Fall 1).</b> Bis B5 blieb der stimmige
    /// Fall stumm: Wahl und Preisanteil passten zusammen, und es stand nichts da. Der
    /// Anwender konnte „keine Zeile" nicht von „nicht geprüft" unterscheiden. Seit B6
    /// steht eine ruhige Zeile — ohne Betrag, ohne Warnzeichen.</para>
    ///
    /// <para><b>Punkt 6 — die unvergleichbare Einheit.</b> Steht der gesetzliche Satz je
    /// 1.000 kg und rechnet das Projekt je Liter, bräuchte der Vergleich die Dichte des
    /// Trägers; <c>energy_carrier.density</c> ist im ganzen Bestand leer. Fall 4 schwieg
    /// dazu; seit B6 sagt er, dass er hier nicht prüfen kann.</para>
    ///
    /// <para>Gemessen wird am Weg der Anwendung: Parametersatz schreiben, Preisanteile
    /// setzen, <c>WirtschaftlichkeitCtrl.Berechne</c> — die Kohärenzzeilen des Laufs
    /// sind die Aussage. Die Kultur ist gepinnt, weil die Texte aus den
    /// Satellitenressourcen kommen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KohaerenzNachtraegeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — zwei Gasmodule und ein Kessel, also
        /// mehrere Energieträger in einem Lauf.</summary>
        private const int PROJEKT = 1030;

        /// <summary>Ein gepflegter Energiesteueranteil [ct/kWh]. Die Höhe ist
        /// gleichgültig — geprüft wird, DASS einer dasteht.</summary>
        private const double ANTEIL_CT_KWH = 0.55;

        // =================================================================
        // Punkt 7 — Fall 1: die positive Nennung
        // =================================================================

        /// <summary>
        /// <b>Wahl und Preisanteil stimmen überein</b> — die Zeile steht, sie trägt die
        /// Schwere BESTAETIGUNG und keinen Betrag. Und Fall 3 („Anteil ausgewiesen, aber
        /// keine Entlastung gewählt") steht dann NICHT: Die beiden sind die zwei Seiten
        /// derselben Bedingung, und zwei Zeilen zu einer Lage wären ein Widerspruch.
        /// </summary>
        [Fact]
        public void Stimmt_die_Wahl_zum_Preisanteil_steht_die_positive_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            AnteileSetzen();
            WirtschaftlichkeitErgebnis e = Rechne(DbWerte.ENERGIESTEUER_WAHL_54);

            KohaerenzHinweis zeile = e.KohaerenzHinweise.FirstOrDefault(
                h => h.Schwere == KohaerenzSchwere.BESTAETIGUNG);

            Assert.True(zeile != null,
                "Keine Bestätigungszeile im Lauf. Gefunden: " +
                string.Join(" | ", e.KohaerenzHinweise.Select(h => h.Schwere + ": " + h.Text)));
            Assert.Null(zeile.Betrag);       // eine Bestätigung bilanziert nicht
            Assert.Contains("Wahl und Preisanteil stimmen überein", zeile.Text, StringComparison.Ordinal);

            Assert.DoesNotContain(e.KohaerenzHinweise,
                h => h.Text.Contains("keine Entlastung gewählt", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Die Gegenprobe im Fall selbst.</b> Derselbe Preisanteil, nur ohne gewählte
        /// Entlastung: Jetzt steht Fall 3 und KEINE Bestätigung. Ohne diesen Fall bliebe
        /// unbemerkt, wenn die Bestätigungszeile einfach immer stünde.
        /// </summary>
        [Fact]
        public void Ohne_gewaehlte_Entlastung_steht_Fall3_statt_der_Bestaetigung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            AnteileSetzen();
            WirtschaftlichkeitErgebnis e = Rechne(DbWerte.ENERGIESTEUER_WAHL_KEINE);

            Assert.Contains(e.KohaerenzHinweise,
                h => h.Schwere == KohaerenzSchwere.HINWEIS &&
                     h.Text.Contains("keine Entlastung gewählt", StringComparison.Ordinal));
            Assert.DoesNotContain(e.KohaerenzHinweise,
                h => h.Schwere == KohaerenzSchwere.BESTAETIGUNG);
        }

        /// <summary>Die Zeile steht in beiden Sprachen — derselbe Schlüssel, zwei
        /// Ressourcendateien.</summary>
        [Fact]
        public void Die_positive_Zeile_steht_auch_englisch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            AnteileSetzen();
            using (new Kulturvorrichtung("en-US"))
            {
                WirtschaftlichkeitErgebnis e = Rechne(DbWerte.ENERGIESTEUER_WAHL_54);
                Assert.Contains(e.KohaerenzHinweise,
                    h => h.Schwere == KohaerenzSchwere.BESTAETIGUNG &&
                         h.Text.Contains("the chosen relief and the price component match",
                                         StringComparison.Ordinal));
            }
        }

        // =================================================================
        // Punkt 6 — der Satz in einer Einheit, die sich nicht umrechnen lässt
        // =================================================================

        /// <summary>
        /// <b>Katalogsatz je 1.000 kg, Projekt rechnet je Liter.</b> Die Brücke bräuchte
        /// die Dichte des Trägers, und die ist im Bestand nirgends gepflegt. Fall 4 kann
        /// hier nicht vergleichen — und sagt es jetzt, statt zu schweigen.
        ///
        /// <para>Gemessen wird an der Prüfung selbst, nicht am Lauf: Die Lage braucht
        /// einen Träger, dessen gesetzlicher Satz in einer Masseeinheit steht, während
        /// das Projekt in Litern abrechnet — das führt kein Projekt der Testdatenbank.
        /// Der Anlagensatz wird deshalb hier gestellt; alles andere (Preisanteil,
        /// Trägername, Katalogsatz) kommt aus der Datenbank.</para>
        /// </summary>
        [Fact]
        public void Passt_die_Einheit_des_Katalogsatzes_nicht_sagt_Fall4_dass_er_nicht_pruefen_kann()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            AnteileSetzen();
            List<KohaerenzHinweis> l = KohaerenzPruefung.Pruefe(PROJEKT, LaufMitFluessiggasJeLiter());

            Assert.Contains(l, h => h.Schwere == KohaerenzSchwere.HINWEIS &&
                                    h.Text.Contains("Ohne Dichte des Energieträgers",
                                                    StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Die Gegenprobe.</b> Derselbe Träger, aber das Projekt rechnet in Kilogramm:
        /// Jetzt TRÄGT die Umrechnung, Fall 4 vergleicht wieder, und die Zeile bleibt
        /// weg. Ohne diesen Fall stünde nicht fest, dass die Meldung an der EINHEIT hängt
        /// und nicht einfach immer erscheint.
        /// </summary>
        [Fact]
        public void Passt_die_Einheit_bleibt_die_Meldung_weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            AnteileSetzen();
            KohaerenzLauf lauf = LaufMitFluessiggasJeLiter();
            lauf.Steuer.Anlagen[0].Abrechnungseinheit = "kg";

            List<KohaerenzHinweis> l = KohaerenzPruefung.Pruefe(PROJEKT, lauf);

            Assert.DoesNotContain(l, h => h.Text.Contains("Ohne Dichte des Energieträgers",
                                                          StringComparison.Ordinal));
        }

        /// <summary>Ein Lauf mit EINER Anlage: Flüssiggas (Katalogsatz je 1.000 kg),
        /// abgerechnet je Liter.</summary>
        private static KohaerenzLauf LaufMitFluessiggasJeLiter()
        {
            var a = new SteuerAnlage
            {
                Bezeichner = "Prüfanlage B6",
                CarrierId = Traeger()[0],
                SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_FLUESSIGGAS,
                EffHi = 6.6,                       // kWh je Einheit — irgendein tragender Wert
                EffHs = 7.2,
                BrennstoffMWh = 100.0,             // ohne Menge prüft die Brennstoffseite den Träger nicht
                Abrechnungseinheit = "l",
                EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_54
            };

            return new KohaerenzLauf
            {
                Jahr = 2026,
                Steuer = new SteuerEingabe
                {
                    Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                    EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_54,
                    Anlagen = { a }
                }
            };
        }

        // =================================================================
        // Prüfstand
        // =================================================================

        /// <summary>
        /// Gibt JEDEM Energieträger des Projekts einen ausgewiesenen, eingeschalteten
        /// Energiesteueranteil. Fall 1 verlangt genau das: kein Träger ohne Anteil.
        /// </summary>
        private static void AnteileSetzen()
        {
            var ctrl = new BrennstoffBestandteilCtrl();
            foreach (int carrier in Traeger())
            {
                BrennstoffBestandteilModel m = ctrl.Read(PROJEKT, carrier);
                m.ID_Projekt = PROJEKT;
                m.ID_Energietraeger = carrier;
                m.Energiesteuer = ANTEIL_CT_KWH;
                m.Energiesteuer_Aktiv = true;
                ctrl.Update(m);
            }
        }

        /// <summary>Die Energieträger, für die das Projekt eine Preiszerlegung führt.</summary>
        private static List<int> Traeger()
        {
            var l = new List<int>();
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID_Energieträger] FROM [" + BrennstoffBestandteilCtrl.TABLE + "] " +
                "WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            if (dt == null) return l;
            foreach (System.Data.DataRow r in dt.Rows)
                if (r[0] != null && r[0] != DBNull.Value) l.Add(Convert.ToInt32(r[0]));
            return l;
        }

        /// <summary>Rechnet das Projekt als Stamm mit der gewünschten Entlastungswahl.</summary>
        private static WirtschaftlichkeitErgebnis Rechne(string wahl)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            p.EnergiesteuerWahl = wahl;
            p.Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE;
            ctrl.SpeichereParameter(p);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall B6",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);

            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);

            List<WirtschaftlichkeitErgebnis> alle = new WirtschaftlichkeitCtrl().Berechne(daten, p);
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            return e;
        }
    }
}
