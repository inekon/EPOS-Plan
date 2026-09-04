using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die AP5-Importkette als EIN Ablauf (<see cref="GanglinienImportAblauf"/>,
    /// iU9-W12.0d, Befund W12-B1).
    ///
    /// <para><b>Das ist der bitgleiche Nachweis der Welle an seiner eigentlichen
    /// Stelle.</b> <c>GanglinienProbenTests</c> haelt fest, was Erkennen, Lesen und
    /// Pruefen aus jeder Probe machen; hier laufen DIESELBEN Proben durch den neuen
    /// Ablauf, und heraus muessen DIESELBEN Zahlen kommen — Summe und Stichwerte auf
    /// die letzte Stelle. Waere die Kette beim Zusammenlegen der beiden
    /// WinForms-Fassungen an einer Stelle anders geworden, faellt es hier auf.</para>
    ///
    /// <para><b>Die Rueckrufe sind Attrappen.</b> Der Ablauf zeigt nichts an; er legt
    /// drei Entscheidungen vor. Die Faelle hier beantworten sie fest und pruefen, was
    /// die Kette daraus macht — auch den Abbruch an jeder der drei Stellen.</para>
    ///
    /// <para><c>OhneAblage</c> braucht keine Datenbank. <c>MitAblage</c> schreibt und
    /// bleibt deshalb den Referenzlaeufen und der Windows-Abnahme vorbehalten; hier
    /// steht von ihm nur, was OHNE Schreiben entscheidbar ist.</para>
    /// </summary>
    public class GanglinienImportAblaufTests
    {
        // ------------------------------------------------------------ Hilfsmittel

        private static string Probe(string name)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests", "Proben", "Ganglinien", name);
                if (File.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Die Probe fehlt: " + name);
            return null;
        }

        /// <summary>Rueckrufe, die alles bestaetigen — der Regelfall „Anwender klickt OK".</summary>
        private static GanglinienImportRueckrufe Bestaetigt(
            Action<GanglinienVorschau> optionenAnpassen = null,
            List<string> spur = null)
        {
            return new GanglinienImportRueckrufe
            {
                Optionen = (pfad, vorschau) =>
                {
                    spur?.Add("optionen");
                    optionenAnpassen?.Invoke(vorschau);
                    return Task.FromResult(vorschau.Vorschlag);
                },
                Protokoll = (meldungen, moeglich, bestaetigen) =>
                {
                    spur?.Add("protokoll");
                    return Task.FromResult(true);
                },
                Konflikte = (pruefungen, namen) =>
                {
                    spur?.Add("konflikte");
                    return Task.FromResult<List<KonfliktEntscheidung>>(null);
                }
            };
        }

        private static double Summe(double[] w)
        {
            double s = 0.0;
            for (int i = 0; i < w.Length; i++) s += w[i];
            return s;
        }

        // ==================================================================
        // OhneAblage — die Zahlen der Proben, unveraendert
        // ==================================================================

        /// <summary>
        /// Die Stundenprobe P01 durch den Ablauf: dieselben 8 760 Werte, dieselbe
        /// Summe wie in <c>GanglinienProbenTests.P01_liefert_die_eingefrorene_Stundenreihe</c>.
        /// </summary>
        [Fact]
        public async Task OhneAblage_liefert_fuer_P01_die_eingefrorene_Reihe()
        {
            List<string> spur = new List<string>();
            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p01_stunden_semikolon_komma_kopf.csv"), Bestaetigt(spur: spur));

            Assert.Equal(ImportAusgang.Erfolg, erg.Ausgang);
            Assert.True(erg.Erfolgreich);
            Assert.Equal("p01_stunden_semikolon_komma_kopf", erg.Bezeichner);
            Assert.Equal(1, erg.Zeitinterval);
            Assert.Equal(8760, erg.Werte.Length);
            Assert.Equal(220.0, erg.Werte[0]);
            Assert.Equal(232.23, erg.Werte[1]);
            Assert.Equal(280.57, erg.Werte[100]);
            Assert.Equal(215.33, erg.Werte[4000]);
            Assert.Equal(221.77, erg.Werte[8759]);
            Assert.Equal(2005977.0000000068, Summe(erg.Werte));
            Assert.Equal("", erg.Meldung);

            // Ein sauberer Lauf legt das Protokoll GAR NICHT vor - dieselbe Regel wie
            // Form_GanglinieProtokoll.Zeigen. Und OhneAblage kennt keine Konflikte.
            Assert.Equal(new[] { "optionen" }, spur);
        }

        /// <summary>
        /// Die Viertelstundenprobe P05 — dasselbe Ergebnis wie in den Probentests,
        /// jetzt ueber die Kette.
        /// </summary>
        [Fact]
        public async Task OhneAblage_liefert_fuer_P05_die_eingefrorene_Viertelstundenreihe()
        {
            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p05_viertelstunden_semikolon_punkt_kopf.csv"), Bestaetigt());

            Assert.Equal(ImportAusgang.Erfolg, erg.Ausgang);
            Assert.Equal(4, erg.Zeitinterval);
            Assert.Equal(35040, erg.Werte.Length);
            Assert.Equal(220.0, erg.Werte[0]);
            Assert.Equal(223.46, erg.Werte[1]);
            Assert.Equal(8024150.999999962, Summe(erg.Werte));
        }

        /// <summary>
        /// Die Excelprobe P11 — der Zweig, der bis Befund W12-B27 gar nicht lief.
        /// </summary>
        [Fact]
        public async Task OhneAblage_liest_auch_eine_Excelmappe()
        {
            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p11_stunden_excel.xlsx"), Bestaetigt());

            Assert.Equal(ImportAusgang.Erfolg, erg.Ausgang);
            Assert.Equal("p11_stunden_excel", erg.Bezeichner);
            Assert.Equal(8760, erg.Werte.Length);
            Assert.Equal(2005977.0000000068, Summe(erg.Werte));
        }

        /// <summary>
        /// Eine Reihe mit Eingriff (Schaltjahr) legt das Protokoll vor — und zwar mit
        /// „Import moeglich = true" und „Bestaetigung noetig = true".
        /// </summary>
        [Fact]
        public async Task Ein_Eingriff_legt_das_Protokoll_zur_Bestaetigung_vor()
        {
            bool gesehenMoeglich = false, gesehenBestaetigen = false;
            int zeilen = 0;

            GanglinienImportRueckrufe r = new GanglinienImportRueckrufe
            {
                Optionen = (p, v) => Task.FromResult(v.Vorschlag),
                Protokoll = (meldungen, moeglich, bestaetigen) =>
                {
                    gesehenMoeglich = moeglich;
                    gesehenBestaetigen = bestaetigen;
                    zeilen = meldungen.Count;
                    return Task.FromResult(true);
                }
            };

            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p07_schaltjahr_stunden_semikolon_kopf.csv"), r);

            Assert.True(gesehenMoeglich);
            Assert.True(gesehenBestaetigen);
            Assert.True(zeilen > 0);
            Assert.Equal(ImportAusgang.Erfolg, erg.Ausgang);
            Assert.Equal(8760, erg.Werte.Length);         // 8 784 -> 8 760
            Assert.Equal(2003479.3700000064, Summe(erg.Werte));
        }

        // ==================================================================
        // Die drei Abbruchstellen
        // ==================================================================

        [Fact]
        public async Task Abbruch_im_Optionendialog_bricht_die_Kette_still_ab()
        {
            GanglinienImportRueckrufe r = new GanglinienImportRueckrufe
            {
                Optionen = (p, v) => Task.FromResult<GanglinienImportOptionen>(null),
                Protokoll = (m, a, b) => throw new InvalidOperationException("darf nicht kommen")
            };

            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p01_stunden_semikolon_komma_kopf.csv"), r);

            Assert.Equal(ImportAusgang.Abgebrochen, erg.Ausgang);
            Assert.Empty(erg.Werte);
            Assert.Equal("", erg.Meldung);
        }

        [Fact]
        public async Task Abbruch_im_Protokoll_bricht_die_Kette_still_ab()
        {
            GanglinienImportRueckrufe r = new GanglinienImportRueckrufe
            {
                Optionen = (p, v) => Task.FromResult(v.Vorschlag),
                Protokoll = (m, a, b) => Task.FromResult(false)
            };

            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p07_schaltjahr_stunden_semikolon_kopf.csv"), r);

            Assert.Equal(ImportAusgang.Abgebrochen, erg.Ausgang);
            Assert.Empty(erg.Werte);
        }

        [Fact]
        public async Task Ohne_Rueckruf_fuer_die_Optionen_laeuft_die_Kette_nicht_an()
        {
            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(
                Probe("p01_stunden_semikolon_komma_kopf.csv"), new GanglinienImportRueckrufe());

            Assert.Equal(ImportAusgang.Abgebrochen, erg.Ausgang);
        }

        // ==================================================================
        // Nicht lesbare Quelle
        // ==================================================================

        /// <summary>
        /// Eine Datei, die es nicht gibt: Der Ablauf legt das Protokoll mit
        /// <c>(false, true)</c> vor — genau wie der Vorlaeufer (:143) — und meldet
        /// <see cref="ImportAusgang.Fehler"/>.
        /// </summary>
        [Fact]
        public async Task Eine_nicht_lesbare_Datei_endet_mit_Fehler_und_zeigt_das_Protokoll()
        {
            string pfad = Path.Combine(Path.GetTempPath(), "epos-w12-gibt-es-nicht.csv");
            File.WriteAllText(pfad, "");    // leer: lesbar als Datei, aber ohne Zeile

            bool gesehenMoeglich = true, gesehenBestaetigen = false;
            GanglinienImportRueckrufe r = new GanglinienImportRueckrufe
            {
                Optionen = (p, v) => throw new InvalidOperationException("darf nicht kommen"),
                Protokoll = (m, moeglich, bestaetigen) =>
                {
                    gesehenMoeglich = moeglich;
                    gesehenBestaetigen = bestaetigen;
                    return Task.FromResult(true);
                }
            };

            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage(pfad, r);

            Assert.Equal(ImportAusgang.Fehler, erg.Ausgang);
            Assert.False(gesehenMoeglich);
            Assert.True(gesehenBestaetigen);
            Assert.NotEmpty(erg.Protokoll);
            Assert.Equal(GanglinienDatei.SchluesselDateiLeer, erg.Protokoll[0].Schluessel);

            File.Delete(pfad);
        }

        [Fact]
        public async Task Ein_leerer_Pfad_liefert_ein_leeres_Ergebnis_ohne_Rueckruf()
        {
            GanglinienImportErgebnis erg = await GanglinienImportAblauf.OhneAblage("", Bestaetigt());

            Assert.Equal(ImportAusgang.Abgebrochen, erg.Ausgang);
            Assert.Empty(erg.Werte);
            Assert.Empty(erg.Protokoll);
        }

        // ==================================================================
        // MitAblage — was ohne Schreiben entscheidbar ist
        // ==================================================================

        /// <summary>
        /// Der Ablageordner ist <c>&lt;BenutzerLokal&gt;\Strom</c> — er kommt aus
        /// <c>Dienste.Pfade</c> und nicht mehr aus <c>Program.ApplicationPath_User</c>
        /// (im Kern verboten). Angelegt wird er dabei NICHT.
        /// </summary>
        [Fact]
        public void AblageOrdner_liegt_unter_dem_lokalen_Benutzerordner()
        {
            string ordner = GanglinienImportAblauf.AblageOrdner();

            Assert.EndsWith(Path.Combine("", "Strom"), ordner, StringComparison.Ordinal);
            Assert.StartsWith(Dienste.Pfade.BenutzerLokal, ordner, StringComparison.Ordinal);
        }

        /// <summary>
        /// Auch mit Ablage bricht ein „Abbrechen" im Optionendialog still ab — es wird
        /// nichts geschrieben und nichts gemeldet. Die Datenbank wird auf diesem Weg
        /// gar nicht angefasst.
        /// </summary>
        [Fact]
        public async Task MitAblage_bricht_vor_jedem_Datenbankzugriff_ab()
        {
            GanglinienImportRueckrufe r = new GanglinienImportRueckrufe
            {
                Optionen = (p, v) => Task.FromResult<GanglinienImportOptionen>(null),
                Konflikte = (pr, n) => throw new InvalidOperationException("darf nicht kommen")
            };

            GanglinienImportErgebnis erg = await GanglinienImportAblauf.MitAblage(
                Probe("p01_stunden_semikolon_komma_kopf.csv"), GanglinienRaster.Unbekannt, r);

            Assert.Equal(ImportAusgang.Abgebrochen, erg.Ausgang);
            Assert.Equal("", erg.Meldung);
        }

        /// <summary>
        /// Die Rastervorgabe der Auswahlliste uebersteuert die Erkennung (Vorlaeufer
        /// :149) — <see cref="GanglinienRaster.Unbekannt"/> laesst sie stehen.
        /// </summary>
        [Fact]
        public async Task Die_Rastervorgabe_uebersteuert_den_Vorschlag_der_Erkennung()
        {
            GanglinienRaster gesehen = GanglinienRaster.Minute;
            GanglinienImportRueckrufe r = new GanglinienImportRueckrufe
            {
                Optionen = (p, v) =>
                {
                    gesehen = v.Vorschlag.Raster;
                    return Task.FromResult<GanglinienImportOptionen>(null);   // danach Schluss
                }
            };

            await GanglinienImportAblauf.MitAblage(
                Probe("p01_stunden_semikolon_komma_kopf.csv"), GanglinienRaster.Viertelstunde, r);
            Assert.Equal(GanglinienRaster.Viertelstunde, gesehen);

            await GanglinienImportAblauf.MitAblage(
                Probe("p01_stunden_semikolon_komma_kopf.csv"), GanglinienRaster.Unbekannt, r);
            Assert.Equal(GanglinienRaster.Unbekannt, gesehen);
        }
    }
}
