using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Dialogs „VDI-4655-Typtage"</b>
    /// (<c>EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Typtage.cs</c>; Umsetzungskonzept
    /// Zapfprofilgenerator 4.2, 5.8, Kapitel 6; Stufe Z4b, Gruppe 2): Parametersatz gegen die
    /// Komponente, der Stand mit beiden Gründen, die Prüfung eines Pakets OHNE Schreibzugriff,
    /// Einspielen und Löschen samt Meldung, die Paketwahl über <see cref="Dienste.Datei"/> mit
    /// gemerktem Startordner, das Textbündel in beiden Sprachen — und
    /// <see cref="ZapfprofilHuelle.MitTyptagwahl"/> in drei Fällen samt Vorschau über die Hülle,
    /// die die Jahresenergie erhält.
    ///
    /// <para><b>Kein Wert einer Richtlinie:</b> Die Typtage kommen aus einem ERFUNDENEN Paket
    /// (<see cref="Typtagpaketbauer"/>) und entstehen nur in der Arbeitskopie bzw. einer leeren
    /// Tww-Datenbank; im Repositorium entsteht keine Datei (Konzept Kapitel 6). Gerechnet wird auf
    /// Projekt 1006 — keines der fünf Projekte der CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleTyptageTests : IDisposable
    {
        /// <summary>Projekt 1006 trägt eine Klimaregion mit 365 Tageszeilen.</summary>
        private const int PROJEKT = 1006;

        private const int ZONE = 3;
        private const string ART = "probehaus";

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static string Text(string k, CultureInfo c)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(k, c) ?? "";

        /// <summary>Ein erfundenes Paket als ZIP in einem eigenen Temp-Ordner.</summary>
        private static string Paket(out string ordner, Action<Typtagpaketbauer> feilen = null)
        {
            ordner = Path.Combine(Path.GetTempPath(), "epos-typtage-" + Guid.NewGuid().ToString("N"));
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden(ZONE, ART);
            feilen?.Invoke(b);
            return b.AlsZip(ordner);
        }

        /// <summary>Eine Dateiwahl-Attrappe: Sie merkt sich die Gaben und gibt einen festen Pfad.</summary>
        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Titel = "";
            internal string Filter = "";
            internal string Startordner = "";
            internal string Antwort = "";

            public string DateiOeffnen(string titel, string filter, string startOrdner)
            {
                Titel = titel ?? "";
                Filter = filter ?? "";
                Startordner = startOrdner ?? "";
                return Antwort;
            }

            public string DateiSpeichern(string titel, string filter, string vorschlag) => "";
            public string OrdnerWaehlen(string titel, string startOrdner) => "";
            public bool MitSystemOeffnen(string pfad) => false;
        }

        // =================================================================================
        //  Der Parametersatz
        // =================================================================================

        [Fact]
        public void Der_Parametersatz_trifft_die_Parameter_des_Importdialogs()
        {
            using var db = new TwwTestdatenbank();
            IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.TyptagGaben();
            var parameter = typeof(TwwTyptagImportDialog).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null).ToDictionary(p => p.Name, p => p.PropertyType);
            foreach (KeyValuePair<string, object> g in gaben)
            {
                Assert.True(parameter.ContainsKey(g.Key), "Kein [Parameter] " + g.Key);
                Assert.True(parameter[g.Key].IsInstanceOfType(g.Value), g.Key + ": " + g.Value.GetType().Name);
            }
            // SIEBEN: Stand, PaketWaehlen, Pruefen, Einspielen, Loeschen, Texte, HilfeSchluessel —
            // OHNE „Geschlossen", damit auch ein Wirt ohne Fenster den Satz nehmen kann.
            Assert.Equal(7, gaben.Count);
            Assert.False(gaben.ContainsKey("Geschlossen"));
            Assert.Equal(ZapfprofilHuelle.HILFE_TYPTAGE, gaben["HilfeSchluessel"]);
            Assert.Equal("Zapfprofil.Importordner", ZapfprofilHuelle.EINSTELLUNG_IMPORTORDNER);
        }

        // =================================================================================
        //  Der Stand
        // =================================================================================

        [Fact]
        public void Ohne_Tabelle_und_ohne_Zeile_nennt_der_Stand_beide_Gruende()
        {
            // Eine Datenbank vor dem Schritt der Typtage: der Grund nennt die fehlende Tabelle.
            using (var ohne = new TwwTestdatenbank())
            {
                Assert.False(TwwTyptagCtrl.TabelleVorhanden());
                TwwTyptagStandDaten d = ZapfprofilHuelle.TyptagStand();
                Assert.False(d.Vorhanden);
                Assert.Contains(TwwSchema.TAB_TWW_TYPTAG_IMPORT, d.Grund, StringComparison.Ordinal);
                Assert.Equal(0, d.Zeilen);
            }

            // Die Arbeitskopie führt die Tabelle, aber keine Zeile: der Leersatz des Bündels.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(TwwTyptagCtrl.TabelleVorhanden());
            TwwTyptagStandDaten leer = ZapfprofilHuelle.TyptagStand();
            Assert.False(leer.Vorhanden);
            Assert.Equal(Text("ZPGT_LEER", DE), leer.Grund);
        }

        // =================================================================================
        //  Prüfen, Einspielen, Löschen
        // =================================================================================

        [Fact]
        public void Die_Pruefung_beschreibt_das_Paket_und_schreibt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string zip = Paket(out string ordner);
            try
            {
                TwwTyptagPruefberichtDaten ohne = ZapfprofilHuelle.TyptagPruefen("");
                Assert.True(ohne.Abgebrochen);
                Assert.Equal(Text("ZPGT_KEIN_PAKET", DE), ohne.Abbruch);

                TwwTyptagPruefberichtDaten fort = ZapfprofilHuelle.TyptagPruefen(
                    Path.Combine(ordner, "gibtesnicht.zip"));
                Assert.True(fort.Abgebrochen);
                Assert.StartsWith("Das Paket ist abgelehnt: ", fort.Abbruch, StringComparison.Ordinal);

                TwwTyptagPruefberichtDaten b = ZapfprofilHuelle.TyptagPruefen(zip);
                Assert.False(b.Abgebrochen, b.Abbruch);
                Assert.Contains("1 Klimazone(n)", b.Zusammenfassung, StringComparison.Ordinal);
                Assert.Contains("6 Typtag(e)", b.Zusammenfassung, StringComparison.Ordinal);
                Assert.Equal(7, b.Angaben.Count);
                Assert.Equal(Text("ZPGT_LBL_QUELLE", DE), b.Angaben[0].Bezeichnung);
                Assert.Equal("3", b.Angaben[2].Wert);
                Assert.Equal(ART, b.Angaben[3].Wert);
                Assert.Equal(Text("ZPGT_WERT_OHNE_GAENGE", DE), b.Angaben[5].Wert);

                // Die Prüfung SCHREIBT NICHTS — der Stand bleibt leer.
                Assert.False(ZapfprofilHuelle.TyptagStand().Vorhanden);
            }
            finally { Aufraeumen(ordner); }
        }

        [Fact]
        public void Ein_abgelehntes_Paket_nennt_den_Grund_und_laesst_den_Stand_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string gut = Paket(out string ordner);
            try
            {
                TwwTyptagErgebnisDaten ein = ZapfprofilHuelle.TyptagEinspielen(gut);
                Assert.True(ein.Ok, ein.Meldung);

                // Ein Paket, dem ein Kalendertag fehlt, wird benannt abgelehnt — und der
                // eingespielte Stand bleibt unangetastet.
                Typtagpaketbauer kurz = Typtagpaketbauer.Erfunden(ZONE, ART);
                var schluessel = (ZONE, ART, kurz.Kategorien[0].Code);
                kurz.Anzahl[schluessel] = kurz.Anzahl[schluessel] - 1;
                TwwTyptagErgebnisDaten weg = ZapfprofilHuelle.TyptagEinspielen(kurz.AlsZip(ordner, "kurz.zip"));
                Assert.False(weg.Ok);
                Assert.StartsWith("Das Paket ist abgelehnt: ", weg.Meldung, StringComparison.Ordinal);
                Assert.True(ZapfprofilHuelle.TyptagStand().Vorhanden);

                TwwTyptagErgebnisDaten nichts = ZapfprofilHuelle.TyptagEinspielen("");
                Assert.False(nichts.Ok);
                Assert.Equal(Text("ZPGT_KEIN_PAKET", DE), nichts.Meldung);
            }
            finally { Aufraeumen(ordner); }
        }

        [Fact]
        public void Einspielen_ersetzt_den_Stand_und_Loeschen_nimmt_ihn_weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string zip = Paket(out string ordner);
            try
            {
                TwwTyptagErgebnisDaten e = ZapfprofilHuelle.TyptagEinspielen(zip);
                Assert.True(e.Ok, e.Meldung);
                Assert.Contains("eingespielt, 0 ersetzt", e.Meldung, StringComparison.Ordinal);
                Assert.True(e.Stand.Vorhanden);
                Assert.Equal(new[] { ZONE }, e.Stand.Klimazonen);
                Assert.Equal(new[] { ART }, e.Stand.Gebaeudearten);
                Assert.Equal(6, e.Stand.Typtage.Count);
                Assert.False(e.Stand.MitTagesgaenge);
                Assert.Empty(e.Stand.AufloesungenMin);
                Assert.True(e.Stand.Zeilen > 0);
                Assert.NotEqual("", e.Stand.DatumImport);
                int zeilen = e.Stand.Zeilen;

                // EIN Satz je Datenbank: der zweite Lauf ersetzt, er legt nicht daneben.
                TwwTyptagErgebnisDaten zweit = ZapfprofilHuelle.TyptagEinspielen(zip);
                Assert.True(zweit.Ok, zweit.Meldung);
                Assert.Contains(zeilen.ToString(CultureInfo.CurrentCulture) + " ersetzt", zweit.Meldung, StringComparison.Ordinal);
                Assert.Equal(zeilen, zweit.Stand.Zeilen);

                TwwTyptagErgebnisDaten weg = ZapfprofilHuelle.TyptagLoeschen();
                Assert.True(weg.Ok);
                Assert.Equal(zeilen.ToString(CultureInfo.CurrentCulture) + " Zeile(n) entfernt.", weg.Meldung);
                Assert.False(weg.Stand.Vorhanden);

                TwwTyptagErgebnisDaten nochmal = ZapfprofilHuelle.TyptagLoeschen();
                Assert.False(nochmal.Ok);
                Assert.Equal(Text("ZPGT_MSG_NICHTS", DE), nochmal.Meldung);
            }
            finally { Aufraeumen(ordner); }
        }

        [Fact]
        public void Ein_Paket_mit_Tagesgaengen_nennt_das_Zeitraster()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string zip = Paket(out string ordner, b =>
            {
                foreach (var k in b.Kategorien) b.Gaenge.Add((ART, k.Code, 720, new[] { 0.25, 0.75 }));
            });
            try
            {
                TwwTyptagErgebnisDaten e = ZapfprofilHuelle.TyptagEinspielen(zip);
                Assert.True(e.Ok, e.Meldung);
                Assert.True(e.Stand.MitTagesgaenge);
                Assert.Equal(new[] { 720 }, e.Stand.AufloesungenMin);

                TwwTyptagStandDaten d = ZapfprofilHuelle.TyptagStand();
                Assert.Equal(new[] { 720 }, d.AufloesungenMin);
            }
            finally { Aufraeumen(ordner); }
        }

        // =================================================================================
        //  Die Paketwahl über Dienste.Datei
        // =================================================================================

        [Fact]
        public async Task Die_Paketwahl_nimmt_den_gemerkten_Ordner_und_merkt_den_neuen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string zip = Paket(out string ordner);
            IDateiDienst dateiVorher = Dienste.Datei;
            IEinstellungen ablageVorher = Dienste.Einstellungen;
            try
            {
                var probe = new Dateiprobe { Antwort = zip };
                Dienste.Datei = probe;
                Dienste.Einstellungen = new FluechtigeEinstellungen();

                // Noch ist nichts gemerkt: der Startordner bleibt leer, der Filter kommt aus dem Bündel.
                Assert.Equal(zip, await ZapfprofilHuelle.TyptagPaketWaehlen(""));
                Assert.Equal(Text("ZPGT_WAHL_TITEL", DE), probe.Titel);
                Assert.Equal(Text("ZPGT_DATEIFILTER", DE), probe.Filter);
                Assert.Equal("", probe.Startordner);

                // Nach der Prüfung steht der Ordner in der Einstellung — und die nächste Wahl nimmt ihn.
                Assert.False(ZapfprofilHuelle.TyptagPruefen(zip).Abgebrochen);
                Assert.Equal(ordner, Dienste.Einstellungen.Lies(ZapfprofilHuelle.EINSTELLUNG_IMPORTORDNER, ""));
                await ZapfprofilHuelle.TyptagPaketWaehlen("Eigen (*.csv)|*.csv");
                Assert.Equal(ordner, probe.Startordner);
                Assert.Equal("Eigen (*.csv)|*.csv", probe.Filter);   // ein eigener Filter geht vor

                // Ein abgebrochener Dialog ist "" — kein Fehler.
                probe.Antwort = "";
                Assert.Equal("", await ZapfprofilHuelle.TyptagPaketWaehlen(""));
            }
            finally
            {
                Dienste.Datei = dateiVorher;
                Dienste.Einstellungen = ablageVorher;
                Aufraeumen(ordner);
            }
        }

        // =================================================================================
        //  Das Textbündel
        // =================================================================================

        [Fact]
        public void Das_Textbuendel_kommt_in_der_Oberflaechensprache()
        {
            TwwTyptagImportTexte de = ZapfprofilHuelle.TyptagTexte();
            Assert.Equal(Text("ZPGT_TITEL", DE), de.Titel);
            Assert.Equal(Text("ZPGT_LEER", DE), de.Leer);
            Assert.Equal(Text("ZPGT_BTN_EINSPIELEN", DE), de.KnopfEinspielen);

            using var en = new Kulturvorrichtung("en-US");
            TwwTyptagImportTexte texte = ZapfprofilHuelle.TyptagTexte();
            Assert.Equal(Text("ZPGT_TITEL", EN), texte.Titel);
            Assert.Equal(Text("ZPGT_LEER", EN), texte.Leer);
            Assert.NotEqual(de.Leer, texte.Leer);
        }

        // =================================================================================
        //  MitTyptagwahl
        // =================================================================================

        [Fact]
        public void Die_Wahl_geht_nur_bei_Abweichung_in_die_Projektzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ProjektStand basis = ZapfprofilCtrl.ProjektVorgabe();

            // 1. Ohne Abweichung bleibt die Basis DIESELBE Instanz.
            var gleich = new ZapfprofilEingabeDaten
            {
                TyptageAktiv = basis.TyptageAktiv,
                TyptageKlimazone = basis.TyptageKlimazone,
                TyptageGebaeudeart = basis.TyptageGebaeudeart ?? ""
            };
            Assert.Same(basis, ZapfprofilHuelle.MitTyptagwahl(basis, gleich));
            Assert.Null(ZapfprofilHuelle.MitTyptagwahl(null, gleich));    // ohne Zeile und ohne Wahl: keine Zeile

            // 2. Mit Wahl entsteht eine Zeile aus der Vorgabe; die Gebäudeart wird getrimmt.
            var mit = new ZapfprofilEingabeDaten
            {
                TyptageAktiv = true,
                TyptageKlimazone = ZONE,
                TyptageGebaeudeart = "  " + ART + " "
            };
            ProjektStand neu = ZapfprofilHuelle.MitTyptagwahl(null, mit);
            Assert.NotNull(neu);
            Assert.True(neu.TyptageAktiv);
            Assert.Equal(ZONE, neu.TyptageKlimazone);
            Assert.Equal(ART, neu.TyptageGebaeudeart);

            // 3. Eine leere Gebäudeart ist „keine Wahl" und wird NULL.
            ProjektStand ohne = ZapfprofilHuelle.MitTyptagwahl(neu, new ZapfprofilEingabeDaten
            {
                TyptageAktiv = false,
                TyptageKlimazone = null,
                TyptageGebaeudeart = "   "
            });
            Assert.False(ohne.TyptageAktiv);
            Assert.Null(ohne.TyptageKlimazone);
            Assert.Null(ohne.TyptageGebaeudeart);
            Assert.Null(ZapfprofilHuelle.MitTyptagwahl(null, null));
        }

        // =================================================================================
        //  Vorschau über die Hülle: die Jahresenergie bleibt
        // =================================================================================

        [Fact]
        public void Die_Vorschau_ueber_die_Huelle_rechnet_die_Typtage_und_erhaelt_die_Jahresenergie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string zip = Paket(out string ordner);
            try
            {
                Assert.True(ZapfprofilHuelle.TyptagEinspielen(zip).Ok);
                int art = ZapfprofilCtrl.Katalog().First(n => n.Bezug == ZapfBezugsart.Personen).Id;
                ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);

                ZapfprofilVorschauDaten bestand = ZapfprofilHuelle.Vorschau(PROJEKT, Eingabe(art, false), basis);
                ZapfprofilVorschauDaten typtage = ZapfprofilHuelle.Vorschau(PROJEKT, Eingabe(art, true), basis);
                Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, bestand.Zustand);
                Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, typtage.Zustand);
                Assert.False(typtage.Summe.Abgelehnt);

                // DIE JAHRESENERGIE BLEIBT — anders über das Jahr verteilt.
                double jahr = bestand.Summe.Kennzahlen.JahresbedarfZapfungKwh;
                Assert.True(jahr > 0);
                Assert.Equal(jahr, typtage.Summe.Kennzahlen.JahresbedarfZapfungKwh, 6);
                Assert.True(bestand.Summe.MonateZapfungKwh
                                .Zip(typtage.Summe.MonateZapfungKwh, (a, b) => Math.Abs(a - b)).Max() > 1e-6,
                            "Der Typtagweg verteilt wie der Formvektor — die Weiche greift nicht.");

                // Ohne eingespielte Typtage lehnt der Weg BENANNT ab — kein stiller Rückfall.
                Assert.True(ZapfprofilHuelle.TyptagLoeschen().Ok);
                ZapfprofilVorschauDaten leer = ZapfprofilHuelle.Vorschau(PROJEKT, Eingabe(art, true), basis);
                Assert.True(leer.Ansichten.Any(a => a.Abgelehnt) || leer.Meldungen.Count > 0);
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Eine Eingabe mit EINER Zone auf einer Wohnnutzung (erfundene, runde Werte).</summary>
        private static ZapfprofilEingabeDaten Eingabe(int idNutzungsart, bool typtage)
            => new ZapfprofilEingabeDaten
            {
                TyptageAktiv = typtage,
                TyptageKlimazone = typtage ? ZONE : (int?)null,
                TyptageGebaeudeart = typtage ? ART : "",
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone Typtage", IdNutzungsart = idNutzungsart, Bezugsmenge = 10 } }
            };

        private static void Aufraeumen(string ordner)
        {
            try { if (Directory.Exists(ordner)) Directory.Delete(ordner, true); } catch { }
        }
    }
}
