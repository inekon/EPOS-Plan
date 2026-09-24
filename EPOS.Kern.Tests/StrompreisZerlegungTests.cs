using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// „Strompreis Details" — die Anteile ZERLEGEN den Arbeitspreis, sie schlagen
    /// nicht mehr auf ihn auf (Anwenderentscheide SP-E-2/SP-E-3 vom 17.09.2026).
    ///
    /// <para>Geprüft wird beides: die Leseregel („NULL heißt kein Anteil" — ein
    /// ungepflegtes Projekt trägt 0 statt der 11,746 ct/kWh der Vorschlagssumme)
    /// und die Faltung des Schemaschritts 83, die den bis dahin wirksamen
    /// Aufschlag in den Arbeitspreis überführt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StrompreisZerlegungTests
    {
        // Testdatenbank: 1017 führt zwei Stromzeilen, die Schritt 83 gefaltet hat;
        // 1030 führt eine Stromzeile, in der alle Anteilsspalten NULL sind.
        private const int PROJEKT_GEFALTET = 1017;
        private const int PROJEKT_UNGEPFLEGT = 1030;

        /// <summary>Der Aufschlag, mit dem der Bestand bis SP-W2/W3 gerechnet hat.</summary>
        private const double AUFSCHLAG_REGELFALL = 11.746;

        // =================================================================
        //  Die Leseregel
        // =================================================================

        [Fact]
        public void Ein_frisches_Modell_traegt_die_Vorschlagswerte_aber_keinen_aktiven_Anteil()
        {
            StrompreisZerlegungModel m = new StrompreisZerlegungModel();

            // Die Werte stehen als VORSCHLAG in den Feldern …
            Assert.Equal(StrompreisZerlegungModel.NETZENTGELT_VORGABE, m.Netzentgelt);
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE, m.Umlagen);
            Assert.Equal(StrompreisZerlegungModel.STROMSTEUER_REGELFALL, m.Stromsteuer);
            Assert.Equal(StrompreisZerlegungModel.KONZESSION_VORGABE, m.Konzession);
            Assert.Equal(StrompreisZerlegungModel.VERTRIEB_VORGABE, m.Vertrieb);

            // … ohne Haken. Restpunkt „Nach #266" und E5-Restpunkt „Aktiv-Flags kein
            // verlässliches Aus": Ein Projekt, an dem niemand etwas eingestellt hat,
            // rechnet mit einer Anteilssumme von 0.
            Assert.False(m.Netzentgelt_Aktiv || m.Umlagen_Aktiv || m.Stromsteuer_Aktiv
                         || m.Konzession_Aktiv || m.Vertrieb_Aktiv || m.Beschaffung_Aktiv);

            Preiszerlegung satz = StrompreisZerlegungCtrl.AlsPreiszerlegung(m);
            Assert.Equal(0.0, satz.SummeAktivCtKwh);
            Assert.Equal(0.0, StrompreisZerlegungCtrl.SummeOhneBeschaffungCtKwh(m));
        }

        /// <summary>
        /// Die Vorschlagssumme ohne Beschaffung ist wertgleich dem Aufschlag des
        /// Bestands (11,746 ct/kWh) — die drei benannten Einzelumlagen ergeben
        /// zusammen genau das alte Summenfeld.
        /// </summary>
        [Fact]
        public void Die_Vorschlagswerte_sind_wertgleich_zum_Bestandsaufschlag()
        {
            Assert.Equal(AUFSCHLAG_REGELFALL,
                         StrompreisZerlegungModel.SUMME_OHNE_BESCHAFFUNG_REGELFALL, 9);
            Assert.Equal(2.946, StrompreisZerlegungModel.UMLAGEN_VORGABE, 9);
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE,
                         StrompreisZerlegungModel.UMLAGE_KWKG_VORGABE
                         + StrompreisZerlegungModel.UMLAGE_OFFSHORE_VORGABE
                         + StrompreisZerlegungModel.UMLAGE_STROMNEV19_VORGABE, 9);
        }

        /// <summary>
        /// <b>Restpunkt C5.</b> Die Konstanten im Modell sind nur noch die
        /// Rückfallebene; die Quelle ist der Gesetzeskatalog. Beide müssen
        /// wertgleich sein — auseinanderlaufen dürfen sie nicht, sonst rechnet die
        /// Maske anders als der Katalog anzeigt.
        ///
        /// <para>ETAPPE E18 (E18‑Q1 a): verglichen wird die ÄLTESTE Saatzeile je
        /// Schlüssel, nicht ein festes Stichjahr. Vor ihr greift die Rückfallebene
        /// (<c>GesetzKatalog.WertMitHerkunft</c> liefert für frühere Jahre nichts);
        /// eine spätere Jahreszeile — eine Novelle — lässt die Konstante unberührt
        /// und diese Wache grün.</para>
        /// </summary>
        [Fact]
        public void Die_Katalogwerte_und_die_Rueckfallebene_sind_wertgleich()
        {
            var aelteste = new Dictionary<string, GesetzParameter>();
            foreach (GesetzParameter p in GesetzKatalog.Vorbelegung())
            {
                if (!p.Wert.HasValue) continue;
                GesetzParameter bisher;
                if (!aelteste.TryGetValue(p.Schluessel, out bisher) || p.JahrVon < bisher.JahrVon)
                    aelteste[p.Schluessel] = p;
            }

            var abweichungen = new List<string>();
            foreach (KeyValuePair<string, double> k in RUECKFALLEBENE)
            {
                GesetzParameter p;
                if (!aelteste.TryGetValue(k.Key, out p))
                {
                    abweichungen.Add("  " + k.Key + ": keine Saatzeile in GesetzKatalog.Vorbelegung().");
                    continue;
                }
                PruefeGegenKonstante(abweichungen, "Saat (GesetzKatalog.Vorbelegung)", k.Key, k.Value, p);
            }

            Assert.True(abweichungen.Count == 0, Meldung(abweichungen));
        }

        /// <summary>
        /// <b>ETAPPE E18 — die Wache Konstante gegen Katalog der Datenbank</b> (Konzept
        /// § 6.3 Nr. 14, § 6.5 „Doppelte Wahrheiten", E18‑Q1 a). Die Saat ist nur die
        /// Vorlage; gelesen wird zur Laufzeit <c>Tab_Gesetzesparameter</c>. Diese Wache
        /// hält die älteste Katalogzeile der TESTDATENBANK je Schlüssel gegen die
        /// Rückfallebene in <see cref="StrompreisZerlegungModel"/> — die beiden
        /// Stromsteuersätze (EUR/MWh) und die drei Umlagen (ct/kWh).
        /// </summary>
        [Fact]
        public void Die_Rueckfallebene_steht_wertgleich_im_Katalog_der_Testdatenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var katalog = new GesetzKatalog();
            var abweichungen = new List<string>();
            foreach (KeyValuePair<string, double> k in RUECKFALLEBENE)
            {
                string klasse = k.Key.StartsWith("UMLAGE_", System.StringComparison.Ordinal)
                    ? DbWerte.GESETZ_KLASSE_UMLAGEN
                    : DbWerte.GESETZ_KLASSE_STROMSTEUER;

                GesetzParameter aelteste = null;
                foreach (GesetzParameter p in katalog.AlleDerKlasse(klasse))
                    if (p.Schluessel == k.Key && p.Wert.HasValue
                        && (aelteste == null || p.JahrVon < aelteste.JahrVon))
                        aelteste = p;

                if (aelteste == null)
                {
                    abweichungen.Add("  " + k.Key + ": keine Zeile mit Wert in Tab_Gesetzesparameter " +
                                     "der Testdatenbank (Klasse " + klasse + ").");
                    continue;
                }
                PruefeGegenKonstante(abweichungen, "Testdatenbank (Tab_Gesetzesparameter)",
                                     k.Key, k.Value, aelteste);
            }

            Assert.True(abweichungen.Count == 0, Meldung(abweichungen));
        }

        /// <summary>Die Rückfallebene je Katalogschlüssel, in ct/kWh.</summary>
        private static readonly KeyValuePair<string, double>[] RUECKFALLEBENE =
        {
            new KeyValuePair<string, double>(DbWerte.GESETZ_STROMST_REGELSATZ,
                                             StrompreisZerlegungModel.STROMSTEUER_REGELFALL),
            new KeyValuePair<string, double>(DbWerte.GESETZ_STROMST_REDUZIERT,
                                             StrompreisZerlegungModel.STROMSTEUER_REDUZIERT),
            new KeyValuePair<string, double>(DbWerte.GESETZ_UMLAGE_KWKG,
                                             StrompreisZerlegungModel.UMLAGE_KWKG_VORGABE),
            new KeyValuePair<string, double>(DbWerte.GESETZ_UMLAGE_OFFSHORE,
                                             StrompreisZerlegungModel.UMLAGE_OFFSHORE_VORGABE),
            new KeyValuePair<string, double>(DbWerte.GESETZ_UMLAGE_STROMNEV19,
                                             StrompreisZerlegungModel.UMLAGE_STROMNEV19_VORGABE),
        };

        /// <summary>
        /// Rechnet die Katalogzeile in ct/kWh (EUR/MWh ÷ 10, ct/kWh × 1) und merkt eine
        /// Abweichung von der Konstante. Eine andere Einheit ist selbst eine Abweichung.
        /// </summary>
        private static void PruefeGegenKonstante(List<string> abweichungen, string ort, string schluessel,
                                                 double konstanteCtKwh, GesetzParameter p)
        {
            string einheit = (p.Einheit ?? "").Trim();
            double ct;
            if (string.Equals(einheit, DbWerte.GESETZ_EINHEIT_EUR_MWH, System.StringComparison.OrdinalIgnoreCase))
                ct = p.Wert.Value / 10.0;
            else if (string.Equals(einheit, DbWerte.GESETZ_EINHEIT_CT_KWH, System.StringComparison.OrdinalIgnoreCase))
                ct = p.Wert.Value;
            else
            {
                abweichungen.Add("  " + ort + ", " + schluessel + " ab " + p.JahrVon +
                                 ": Einheit „" + einheit + "\" ist weder EUR/MWh noch ct/kWh.");
                return;
            }

            if (System.Math.Abs(ct - konstanteCtKwh) > 1e-9)
                abweichungen.Add("  " + ort + ", " + schluessel + " ab " + p.JahrVon + ": " +
                                 p.Wert.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                 " " + einheit + " = " +
                                 ct.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                 " ct/kWh, die Rückfallebene trägt " +
                                 konstanteCtKwh.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                 " ct/kWh.");
        }

        private static string Meldung(List<string> abweichungen)
        {
            return "Rückfallebene und Gesetzeskatalog laufen auseinander:" + System.Environment.NewLine +
                   string.Join(System.Environment.NewLine, abweichungen) + System.Environment.NewLine +
                   "Nachzuziehen sind alle drei Orte gemeinsam: die Konstante in " +
                   "EPOS.Kern/Model/StrompreisZerlegungModel.cs (STROMSTEUER_REGELFALL, " +
                   "STROMSTEUER_REDUZIERT, UMLAGE_*_VORGABE), die Saatzeile in " +
                   "GesetzKatalog.Vorbelegung (EPOS.Kern/Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs, " +
                   "mit neuer Generation) und die Zeile der Testdatenbank " +
                   "(Referenzlaeufe/Kenndaten_Test.sqlite, Tab_Gesetzesparameter). Eine Novelle " +
                   "ist eine NEUE, spätere Jahreszeile — sie lässt die älteste Zeile und die " +
                   "Rückfallebene stehen.";
        }

        /// <summary>
        /// Die Umlagen zählen EINMAL: entweder das Summenfeld oder die drei
        /// Einzelposten. Beides zugleich wäre eine Doppelzählung.
        /// </summary>
        [Fact]
        public void Die_Umlagen_zaehlen_entweder_als_Summe_oder_einzeln()
        {
            StrompreisZerlegungModel m = new StrompreisZerlegungModel();
            m.Umlagen_Aktiv = true;
            m.Umlage_KWKG_Aktiv = true;
            m.Umlage_Offshore_Aktiv = true;
            m.Umlage_StromNEV19_Aktiv = true;

            m.Umlagen_Einzeln = false;
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE,
                         StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh, 9);
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE,
                         StrompreisZerlegungCtrl.UmlagenCtKwh(m), 9);

            m.Umlagen_Einzeln = true;
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE,
                         StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh, 9);
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE,
                         StrompreisZerlegungCtrl.UmlagenCtKwh(m), 9);
        }

        [Fact]
        public void Ohne_Zeile_traegt_der_Controller_keinen_Anteil()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Es gibt keinen Energieträger 0 — der Leseweg fällt auf die Vorgabe.
            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().Read(PROJEKT_GEFALTET, 0);

            Assert.False(m.AusDatenbank);
            Assert.Equal(0.0, StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh);
        }

        [Fact]
        public void Eine_nie_gepflegte_Zeile_traegt_nichts_bei()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_UNGEPFLEGT);
            Assert.True(traeger > 0);

            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().Read(PROJEKT_UNGEPFLEGT, traeger);

            Assert.True(m.AusDatenbank);   // die Zeile gibt es — gepflegt ist sie nicht
            Assert.Equal(0.0, StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh);
            Assert.Equal(0.0, StrompreisZerlegungCtrl.SummeOhneBeschaffungCtKwh(m));
        }

        /// <summary>
        /// Die gefaltete Zeile des Referenzprojekts: Die Summe der Anteile IST der
        /// Arbeitspreis, und was ohne die Beschaffung übrig bleibt, ist genau der
        /// Aufschlag, mit dem der Bestand gerechnet hat.
        /// </summary>
        [Fact]
        public void Die_gefaltete_Zeile_zerlegt_ihren_Arbeitspreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().ReadStrom(PROJEKT_GEFALTET);
            Assert.True(m.AusDatenbank);
            Assert.True(m.Beschaffung_Aktiv);

            double summe = StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh;
            double ohne = StrompreisZerlegungCtrl.SummeOhneBeschaffungCtKwh(m);

            Assert.Equal(AUFSCHLAG_REGELFALL, ohne, 6);
            Assert.Equal(m.Beschaffung, summe - ohne, 6);
            Assert.True(m.Beschaffung > 0.0);
        }

        [Fact]
        public void Anteile_und_Merkspalte_ueberleben_Schreiben_und_Lesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StrompreisZerlegungCtrl ctrl = new StrompreisZerlegungCtrl();
            StrompreisZerlegungModel m = ctrl.ReadStrom(PROJEKT_GEFALTET);
            Assert.True(m.AusDatenbank);

            m.Umlagen_Einzeln = true;
            m.Umlage_KWKG = 0.446; m.Umlage_KWKG_Aktiv = true;
            m.Umlage_Offshore = 0.941; m.Umlage_Offshore_Aktiv = true;
            m.Umlage_StromNEV19 = 1.559; m.Umlage_StromNEV19_Aktiv = true;
            m.Umlagen_Aktiv = false;
            Assert.True(ctrl.Update(m));

            StrompreisZerlegungModel neu = ctrl.Read(m.ID_Projekt, m.ID_Energietraeger);
            Assert.True(neu.Umlagen_Einzeln);
            Assert.Equal(0.446, neu.Umlage_KWKG, 9);
            Assert.Equal(0.941, neu.Umlage_Offshore, 9);
            Assert.Equal(1.559, neu.Umlage_StromNEV19, 9);
            Assert.Equal(StrompreisZerlegungModel.UMLAGEN_VORGABE,
                         StrompreisZerlegungCtrl.UmlagenCtKwh(neu), 9);
        }

        // =================================================================
        //  Die Faltung des Schemaschritts 83
        // =================================================================

        /// <summary>
        /// Der Fall „aufgeschlüsselt": Die Zeile wird auf den Bestandsstand
        /// zurückgesetzt (fünf aktive Anteile, Modus „Aufgeschluesselt",
        /// Beschaffung leer), dann faltet der Schritt. Danach muss der
        /// Arbeitspreis um genau den alten Aufschlag höher stehen und die
        /// Beschaffung den alten Arbeitspreis tragen.
        /// </summary>
        [Fact]
        public void Die_Faltung_hebt_den_Arbeitspreis_um_den_alten_Aufschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_GEFALTET);
            Assert.True(traeger > 0);

            BestandsstandHerstellen(PROJEKT_GEFALTET, traeger,
                                    DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT, 0.0);
            double vorher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);
            Assert.True(vorher > 0.0);

            Assert.True(StrompreisZerlegung.ZaehlungFaltung() > 0);
            IReadOnlyList<string> protokoll = StrompreisZerlegung.Falten();
            Assert.NotEmpty(protokoll);

            double nachher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);
            Assert.Equal(vorher + AUFSCHLAG_REGELFALL, nachher, 6);

            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().Read(PROJEKT_GEFALTET, traeger);
            Assert.True(m.Beschaffung_Aktiv);
            Assert.Equal(vorher, m.Beschaffung, 6);
            Assert.Equal(nachher, StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh, 6);
            Assert.Equal(AUFSCHLAG_REGELFALL,
                         StrompreisZerlegungCtrl.SummeOhneBeschaffungCtKwh(m), 6);

            // Wiederholbar: Ein zweiter Lauf findet nichts mehr.
            Assert.Equal(0, StrompreisZerlegung.ZaehlungFaltung());
            Assert.Empty(StrompreisZerlegung.Falten());
        }

        /// <summary>
        /// Der Fall „Gesamtwert": Er lässt sich nicht in Anteile zerlegen. Der
        /// Betrag wandert vollständig in den Arbeitspreis, die Anteilsfelder
        /// bleiben stehen und werden inaktiv, und es gibt eine Protokollzeile.
        /// </summary>
        [Fact]
        public void Ein_Gesamtwert_wandert_vollstaendig_in_den_Arbeitspreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_GEFALTET);
            Assert.True(traeger > 0);

            BestandsstandHerstellen(PROJEKT_GEFALTET, traeger,
                                    DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT, 20.0);
            double vorher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);

            IReadOnlyList<string> protokoll = StrompreisZerlegung.Falten();
            Assert.Contains(protokoll, z => z.Contains("Gesamtaufschlag"));

            double nachher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);
            Assert.Equal(vorher + 20.0, nachher, 6);

            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().Read(PROJEKT_GEFALTET, traeger);
            Assert.True(m.Beschaffung_Aktiv);
            Assert.Equal(nachher, m.Beschaffung, 6);
            Assert.False(m.Netzentgelt_Aktiv || m.Umlagen_Aktiv || m.Stromsteuer_Aktiv
                         || m.Konzession_Aktiv || m.Vertrieb_Aktiv);

            // Die Werte selbst sind NICHT gelöscht — sie bleiben als Vorschlag stehen.
            Assert.Equal(StrompreisZerlegungModel.NETZENTGELT_VORGABE, m.Netzentgelt, 6);
        }

        /// <summary>
        /// Eine Zeile mit gepflegten Werten, aber ohne wirksamen Aufschlag, wird
        /// stillgelegt statt geleert: Ohne Modus rechneten ihre Werte ab Schritt 83
        /// sonst mit, obwohl sie es nie taten.
        /// </summary>
        [Fact]
        public void Gepflegte_Werte_ohne_wirksamen_Aufschlag_werden_stillgelegt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_GEFALTET);
            Assert.True(traeger > 0);

            BestandsstandHerstellen(PROJEKT_GEFALTET, traeger,
                                    DbWerte.SP_AUFSCHLAG_MODUS_KEINER, 0.0);
            double vorher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);

            Assert.Equal(0, StrompreisZerlegung.ZaehlungFaltung());
            Assert.NotEmpty(StrompreisZerlegung.Falten());

            Assert.Equal(vorher, ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger), 6);

            StrompreisZerlegungModel m = new StrompreisZerlegungCtrl().Read(PROJEKT_GEFALTET, traeger);
            Assert.Equal(0.0, StrompreisZerlegungCtrl.AlsPreiszerlegung(m).SummeAktivCtKwh, 6);
            Assert.Equal(StrompreisZerlegungModel.NETZENTGELT_VORGABE, m.Netzentgelt, 6);
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        /// <summary>
        /// Stellt auf der Arbeitskopie den Stand VOR Schritt 83 her: fünf aktive
        /// Anteile mit den Vorschlagswerten, keine Beschaffung, der gewünschte
        /// Modus samt Gesamtwert.
        /// </summary>
        private static void BestandsstandHerstellen(int projekt, int traeger,
                                                    string modus, double gesamtwert)
        {
            // Modus und Gesamtwert sind mit Schemaschritt 85 entfallen. Der
            // Bestandsstand VOR Schritt 83 hatte sie - fuer diese Arbeitskopie kommen
            // sie deshalb zurueck; sie verschwindet mit dem Prueflauf.
            TestDatenbank.AltspaltenStrompreisWiederherstellen();

            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET " +
                "Aufschlag_Netzentgelt = ?, Aufschlag_Netzentgelt_Aktiv = 1, " +
                "Aufschlag_Umlagen = ?, Aufschlag_Umlagen_Aktiv = 1, " +
                "Aufschlag_Stromsteuer = ?, Aufschlag_Stromsteuer_Aktiv = 1, " +
                "Aufschlag_Konzession = ?, Aufschlag_Konzession_Aktiv = 1, " +
                "Aufschlag_Vertrieb = ?, Aufschlag_Vertrieb_Aktiv = 1, " +
                "Aufschlag_Beschaffung = NULL, Aufschlag_Beschaffung_Aktiv = 0, " +
                "Aufschlag_UmlagenEinzeln = 0, " +
                "Aufschlag_Modus = ?, Aufschlag_Override = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@n", DbParamTyp.Double) { Wert = StrompreisZerlegungModel.NETZENTGELT_VORGABE },
                new DbParam("@u", DbParamTyp.Double) { Wert = StrompreisZerlegungModel.UMLAGEN_VORGABE },
                new DbParam("@s", DbParamTyp.Double) { Wert = StrompreisZerlegungModel.STROMSTEUER_REGELFALL },
                new DbParam("@k", DbParamTyp.Double) { Wert = StrompreisZerlegungModel.KONZESSION_VORGABE },
                new DbParam("@v", DbParamTyp.Double) { Wert = StrompreisZerlegungModel.VERTRIEB_VORGABE },
                new DbParam("@m", DbParamTyp.VarWChar) { Wert = modus },
                new DbParam("@o", DbParamTyp.Double) { Wert = gesamtwert },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });
        }

        /// <summary>
        /// Der wirksame Arbeitspreis [ct/kWh] nach derselben Vorrangkette, die
        /// auch die Faltung liest: jüngste Preisversion &gt; 0, sonst die
        /// Projekteinstellung.
        /// </summary>
        private static double ArbeitspreisCtKwh(int projekt, int traeger)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT arbeitspreis FROM energy_price " +
                "WHERE ID_Projekt = ? AND carrier_id = ? AND arbeitspreis > 0 " +
                "ORDER BY valid_from DESC LIMIT 1",
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });

            if (v != null && v != System.DBNull.Value)
                return System.Convert.ToDouble(v) * 100.0;

            object w = DataRepository.ExecuteScalar(
                "SELECT custom_price_work FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });

            return (w == null || w == System.DBNull.Value)
                ? 0.0 : System.Convert.ToDouble(w) * 100.0;
        }
    }
}
