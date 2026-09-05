using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Datenseite der Welle 8 auf einer Arbeitskopie der Testdatenbank:
    /// <see cref="BedarfStammCtrl"/>, <see cref="TypProfilCtrl"/> und die in W8.0a
    /// nachgezogenen Schreibwege des <see cref="ProzesswaermeStammCtrl"/>.
    ///
    /// <para><b>Warum mit Datenbank.</b> Monatswerte und Typprofile sind
    /// SIMULATIONSEINGANG (<c>Tab_*_STAMM</c> → Projektkopie → Rechenkern). Der
    /// Referenzlauf rechnet einen bestehenden Stand nach und sieht vom Schreiben nichts;
    /// ohne diese Faelle waere der Weg allein am Windows-Geraet nachweisbar.</para>
    ///
    /// <para>Jeder Fall arbeitet auf einer eigenen Kopie und raeumt nicht auf — die Kopie
    /// verschwindet mit <see cref="TestDatenbank"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfProfilTests
    {
        private static readonly BedarfsArt[] ALLE =
        {
            BedarfsArt.Stromverbraucher, BedarfsArt.Prozesswaerme, BedarfsArt.Brauchwasser
        };

        // =============================================================== BedarfStammCtrl

        [Fact]
        public void Typen_liefert_je_Art_die_sortierte_Namensliste()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (BedarfsArt art in ALLE)
            {
                IReadOnlyList<string> typen = BedarfStammCtrl.Typen(art);
                Assert.NotNull(typen);
                Assert.NotEmpty(typen);

                // Dieselbe Reihenfolge wie im Vorlaeufer: ORDER BY der Namensspalte.
                var sortiert = typen.OrderBy(t => t, StringComparer.Ordinal).ToList();
                Assert.Equal(sortiert, typen.ToList());
            }
        }

        [Fact]
        public void Typen_nimmt_beim_Stromverbraucher_die_Spalte_Typname()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(("Tab_Stromverbrauchertyp_STAMM", "Typname"),
                         BedarfStammCtrl.TypKatalog(BedarfsArt.Stromverbraucher));
            Assert.Equal(("Tab_Prozesstyp_STAMM", "Bezeichner"),
                         BedarfStammCtrl.TypKatalog(BedarfsArt.Prozesswaerme));
            Assert.Equal(("Tab_Brauchwassertyp_STAMM", "Bezeichner"),
                         BedarfStammCtrl.TypKatalog(BedarfsArt.Brauchwasser));
        }

        [Fact]
        public void Monatswerte_liefert_zwoelf_Werte_und_null_fuer_einen_unbekannten_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (BedarfsArt art in ALLE)
            {
                string bez = ErsterKopf(art);
                if (bez == null) continue;

                double[] monat = BedarfStammCtrl.Monatswerte(art, bez);
                Assert.NotNull(monat);
                Assert.Equal(12, monat.Length);

                Assert.Null(BedarfStammCtrl.Monatswerte(art, "gibt-es-nicht-" + Guid.NewGuid()));
            }
        }

        // ======================================================= ProzesswaermeStammCtrl (W8.0a)

        [Fact]
        public void SaveHead_legt_einen_neuen_Prozesskopf_an_und_ueberschreibt_ihn()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = "W8-Probe-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var ctrl = new ProzesswaermeStammCtrl();

            Assert.False(ctrl.Exists(name));

            var monat = new double[12];
            for (int i = 0; i < 12; i++) monat[i] = i + 1;
            Assert.True(ctrl.SaveHead(name, "", "Probe", monat, true));
            Assert.True(ctrl.Exists(name));

            double[] gelesen = BedarfStammCtrl.Monatswerte(BedarfsArt.Prozesswaerme, name);
            Assert.NotNull(gelesen);
            Assert.Equal(7.0, gelesen[6], 6);

            for (int i = 0; i < 12; i++) monat[i] = 100 + i;
            Assert.True(ctrl.SaveHead(name, "", "Probe 2", monat, false));
            gelesen = BedarfStammCtrl.Monatswerte(BedarfsArt.Prozesswaerme, name);
            Assert.Equal(106.0, gelesen[6], 6);
        }

        [Fact]
        public void SaveHead_verweigert_einen_schreibgeschuetzten_Prozesskopf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Prozesswaerme_STAMM WHERE ReadOnly = 1 LIMIT 1");
            if (v == null || v == DBNull.Value) return;   // kein Auslieferungssatz vorhanden

            string bez = v.ToString();
            double[] vorher = BedarfStammCtrl.Monatswerte(BedarfsArt.Prozesswaerme, bez);

            var ctrl = new ProzesswaermeStammCtrl();
            Assert.True(ctrl.IsReadOnly(bez));
            Assert.False(ctrl.SaveHead(bez, "", "", new double[12], false));

            double[] nachher = BedarfStammCtrl.Monatswerte(BedarfsArt.Prozesswaerme, bez);
            Assert.Equal(vorher, nachher);
        }

        // =============================================================== TypProfilCtrl

        [Fact]
        public void Lies_liefert_je_Art_sieben_mal_vierundzwanzig_Werte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (BedarfsArt art in ALLE)
            {
                string typ = TypProfilCtrl.Typen(art).FirstOrDefault();
                Assert.NotNull(typ);

                var gelesen = TypProfilCtrl.Lies(art, typ);
                Assert.NotNull(gelesen);
                Assert.Equal(7, gelesen.Value.Werte.GetLength(0));
                Assert.Equal(24, gelesen.Value.Werte.GetLength(1));

                Assert.Null(TypProfilCtrl.Lies(art, "gibt-es-nicht-" + Guid.NewGuid()));
            }
        }

        [Fact]
        public void Neu_Speichern_und_Loeschen_laufen_je_Art_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (BedarfsArt art in ALLE)
            {
                string name = "W8-Typ-" + Guid.NewGuid().ToString("N").Substring(0, 8);

                Assert.Equal(TypAnlageErgebnis.Angelegt, TypProfilCtrl.Neu(art, name));
                Assert.Contains(name, TypProfilCtrl.Typen(art));

                // "Neu" legt 168 Nullen und eine leere Beschreibung an.
                var frisch = TypProfilCtrl.Lies(art, name);
                Assert.NotNull(frisch);
                Assert.Equal("", frisch.Value.Beschreibung);
                Assert.Equal(0.0, frisch.Value.Werte[3, 17], 6);

                var werte = new double[7, 24];
                for (int t = 0; t < 7; t++)
                    for (int s = 0; s < 24; s++) werte[t, s] = t * 24 + s + 1;

                Assert.True(TypProfilCtrl.Speichern(art, name, werte, "Probe"));

                var zurueck = TypProfilCtrl.Lies(art, name);
                Assert.NotNull(zurueck);
                Assert.Equal("Probe", zurueck.Value.Beschreibung);
                Assert.Equal(1.0, zurueck.Value.Werte[0, 0], 6);
                Assert.Equal(168.0, zurueck.Value.Werte[6, 23], 6);
                Assert.Equal(90.0, zurueck.Value.Werte[3, 17], 6);

                Assert.False(TypProfilCtrl.IstReadOnly(art, name));
                Assert.True(TypProfilCtrl.Loeschen(art, name));
                Assert.DoesNotContain(name, TypProfilCtrl.Typen(art));
            }
        }

        [Fact]
        public void SpeichernUnter_legt_einen_zweiten_Typ_mit_denselben_Werten_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Stromverbrauchertyp ist der Fall mit der ABWEICHENDEN Schluesselspalte
            // (Typname statt Bezeichner) - deshalb genau er.
            const BedarfsArt art = BedarfsArt.Stromverbraucher;

            string quelle = TypProfilCtrl.Typen(art).First();
            var gelesen = TypProfilCtrl.Lies(art, quelle);
            Assert.NotNull(gelesen);

            string ziel = "W8-Kopie-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            Assert.Equal(TypAnlageErgebnis.Angelegt,
                         TypProfilCtrl.SpeichernUnter(art, ziel, gelesen.Value.Werte,
                                                      gelesen.Value.Beschreibung));

            var kopie = TypProfilCtrl.Lies(art, ziel);
            Assert.NotNull(kopie);
            Assert.Equal(gelesen.Value.Beschreibung, kopie.Value.Beschreibung);
            for (int t = 0; t < 7; t++)
                for (int s = 0; s < 24; s++)
                    Assert.Equal(gelesen.Value.Werte[t, s], kopie.Value.Werte[t, s], 6);
        }

        [Fact]
        public void Speichern_haelt_einen_Auslieferungstyp_nicht_auf_der_Kern_ist_nicht_die_Sperre()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Die Sperre gehoert dem Dialog (IstReadOnly VOR dem Schreiben), damit die
            // Meldung als Warnbanner IM Dialog steht und nicht als modaler Kasten darueber.
            // Hier wird nur festgehalten, dass IstReadOnly den Auslieferungsbestand kennt.
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Brauchwassertyp_STAMM WHERE ReadOnly = 1 LIMIT 1");
            if (v == null || v == DBNull.Value) return;

            Assert.True(TypProfilCtrl.IstReadOnly(BedarfsArt.Brauchwasser, v.ToString()));
        }

        // ============================================ Belegter Name (Befund W8-B-2)

        /// <summary>
        /// „Neu" und „Speichern unter" mit einem VORHANDENEN Namen: Der Controller
        /// prüft ihn VOR dem Einfügen, meldet <see cref="TypAnlageErgebnis.NameBelegt"/>
        /// und schreibt nichts.
        ///
        /// <para><b>Befund W8‑B‑2</b> der Windows-Abnahme vom 05.09.2026: Bis dahin lief
        /// der Weg bis in das <c>INSERT</c>, die eindeutige Namensspalte warf, und
        /// <c>DataRepository.FehlerMelden</c> stellte dem Anwender den Wortlaut
        /// „Datenbankfehler: SQLite Error 19: 'UNIQUE constraint failed:
        /// Tab_Stromverbrauchertyp_STAMM.Typname'" samt der Anweisung in einen modalen
        /// Kasten — mitten aus einem Blazor-Ereignis heraus (Hausregel A‑8).</para>
        ///
        /// <para>Geprüft wird deshalb dreierlei: der WERT statt eines Wurfs, KEIN Dialog
        /// (die Mitschrift bleibt leer) und KEIN zusätzlicher Datensatz.</para>
        /// </summary>
        [Fact]
        public void Ein_belegter_Name_meldet_sich_ohne_Dialog_und_schreibt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IDialogDienst vorher = Dienste.Dialog;
            var mitschrift = new MitschreibendeDialoge();

            try
            {
                Dienste.Dialog = mitschrift;

                foreach (BedarfsArt art in ALLE)
                {
                    string belegt = TypProfilCtrl.Typen(art).First();
                    Assert.True(TypProfilCtrl.TypExists(art, belegt));

                    int vorherAnzahl = TypProfilCtrl.Typen(art).Count;

                    Assert.Equal(TypAnlageErgebnis.NameBelegt, TypProfilCtrl.Neu(art, belegt));
                    Assert.Equal(TypAnlageErgebnis.NameBelegt,
                                 TypProfilCtrl.SpeichernUnter(art, belegt, new double[7, 24], "Probe"));

                    Assert.Equal(vorherAnzahl, TypProfilCtrl.Typen(art).Count);
                }

                // Kein modaler Kasten - genau das war der Befund.
                Assert.Empty(mitschrift.Zeilen);
            }
            finally
            {
                Dienste.Dialog = vorher;
            }
        }

        /// <summary>
        /// Die Gegenprobe: Ein FREIER Name geht durch — sonst wäre die Vorprüfung eine,
        /// die alles sperrt. Genommen ist der Stromverbraucher, weil er als einziger
        /// seine Namensspalte <c>Typname</c> statt <c>Bezeichner</c> nennt.
        /// </summary>
        [Fact]
        public void TypExists_laesst_einen_freien_Namen_durch_und_kennt_ihn_danach()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const BedarfsArt art = BedarfsArt.Stromverbraucher;

            string frei = "W8-Frei-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            Assert.False(TypProfilCtrl.TypExists(art, frei));
            Assert.False(TypProfilCtrl.TypExists(art, ""));
            Assert.False(TypProfilCtrl.TypExists(art, null));

            Assert.Equal(TypAnlageErgebnis.Angelegt, TypProfilCtrl.Neu(art, frei));
            Assert.True(TypProfilCtrl.TypExists(art, frei));

            // Und beim ZWEITEN Mal ist derselbe Name belegt.
            Assert.Equal(TypAnlageErgebnis.NameBelegt, TypProfilCtrl.Neu(art, frei));

            Assert.True(TypProfilCtrl.Loeschen(art, frei));
        }

        // =================================================================================

        /// <summary>Ein Dialogdienst, der nichts zeigt, sondern mitschreibt.</summary>
        private sealed class MitschreibendeDialoge : IDialogDienst
        {
            public readonly List<string> Zeilen = new List<string>();

            public void Meldung(string text, string titel = null) { Zeilen.Add("Meldung|" + text); }
            public void Warnung(string text, string titel = null) { Zeilen.Add("Warnung|" + text); }
            public void Fehler(string text, string titel = null) { Zeilen.Add("Fehler|" + text); }

            public bool Frage(string text, string titel = null, bool warnend = false,
                              bool vorgabeNein = false)
            {
                Zeilen.Add("Frage|" + text);
                return true;
            }

            public JaNeinAbbruch Wahl(string text, string titel = null)
            {
                Zeilen.Add("Wahl|" + text);
                return JaNeinAbbruch.Ja;
            }

            // Warten ist kein Dialog - es faerbt nur den Zeiger und bleibt draussen.
            public void Warten(bool an) { }
        }

        private static string ErsterKopf(BedarfsArt art)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM " + BedarfStammCtrl.KopfTabelle(art) + " ORDER BY Bezeichner LIMIT 1");
            return (v == null || v == DBNull.Value) ? null : v.ToString();
        }
    }
}
