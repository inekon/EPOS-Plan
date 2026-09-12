using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Das AKTIONSREGISTER IM KERN und der Setzweg der Stufe S3 (Auftrag #201).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum diese Faelle erst jetzt moeglich sind.</b> Bis #201 lag das gefuellte
    /// Register in der Windows-Anwendung, und die darf ein Kerntest nicht referenzieren
    /// (COM-Referenzen, MSB4803 — dieselbe Lage wie bei <c>SpeicherEngine.Tests</c>).
    /// <c>KiKern.Tests</c> half sich mit einem ABBILD (<c>Registerabbild</c>), das eine
    /// neue Aktion nicht von selbst bemerkt. Seit der Umzug im Kern ist, prueft dieser
    /// Fall das ECHTE Register.
    /// </para>
    /// <para>
    /// <b>Ohne Oberflaeche, aber MIT Arbeitskopie.</b> Geprueft wird die DEKLARATION
    /// (Namen, Stufen, Vollstaendigkeit, Verbotsliste) und der Setzweg ueber die
    /// <see cref="KiMaskenbruecke"/> — der laeuft gegen ein Daten-Objekt dieses
    /// Prueflings, nicht gegen einen Dialog. Die Arbeitskopie braucht genau EINES: den
    /// SICHERUNGSPUNKT, den jede datenbankwirksame Aktion vor sich her schiebt
    /// (Fachkonzept 4.4, Punkt 1) — ohne Datenbank gibt es keine Kopie, und dann wird
    /// jede Rechenaktion abgelehnt. Fehlt die Datei, schweigen die betroffenen Faelle
    /// (<c>TestDatenbank.Vorhanden</c>).
    /// </para>
    /// </remarks>
    [Collection("Testdatenbank")]
    public sealed class KiRegisterS3Tests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        /// <summary>Die Aktionen, die das Register des Kerns fuehren MUSS.</summary>
        /// <remarks>
        /// Die Liste ist AUSGESCHRIEBEN und nicht aus dem Register abgeleitet — sonst
        /// prüfte sie sich selbst. Sie ist der Bestand der Etappe 3b (19 Aktionen) plus
        /// die fuenf der Etappe S3.
        /// </remarks>
        private static readonly string[] ERWARTET =
        {
            // Stufe 1 — lesend
            "projekte_auflisten", "projekt_aktiv", "projekt_suchen", "projekt_lesen",
            "varianten_auflisten", "speichervarianten_auflisten",
            "ergebnisse_lesen", "wirtschaftlichkeit_parameter_lesen", "kostenlage_pruefen",
            "energietraeger_pruefen",
            "uebernahme_vorschau", "merkmal_vorschau",
            "lastgang_pruefen", "ganglinien_auflisten", "minimale_spitze_ermitteln",
            "letzte_aktionen",
            "dialog_lesen", "dialog_parameter_erklaeren",
            // Stufe 1, NEU mit S3
            "dialog_oeffnen",
            // Stufe 2 — schreibend
            "variante_anlegen", "speichervariante_aktiv_setzen", "kostenposition_setzen",
            "feld_setzen", "formular_ausfuellen", "dialog_aktion_ausfuehren",
            // Stufe 2, NEU mit S3
            "dialog_speichern",
            // Stufe 3, NEU mit S3
            "simulation_rechnen", "peak_ziel_bestimmen", "flotte_bewerten"
        };

        /// <summary>
        /// Was nach Fachkonzept 5.4 NIE im Register stehen darf — als Wortbestandteil.
        /// </summary>
        private static readonly string[] VERBOTEN =
        {
            "loesch", "delete", "entfern", "lizenz", "migration", "migrier",
            "api_schluessel", "tageslimit", "sql"
        };

        private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

        public KiRegisterS3Tests(TestDatenbank db)
        {
            _db = db;

            // Der Lesemodus ist eine der geprueften Ablehnungen; er wird je Fall gesetzt.
            Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
            KiMaskenbruecke.Leeren();
        }

        /// <summary>
        /// <b>Der Sicherungspunkt wird NICHT zwischen den Faellen zurueckgesetzt</b> — und
        /// das ist kein Versehen: Er entsteht EINMAL JE SITZUNG (Fachkonzept 4.4,
        /// Punkt 1), und sein Dateiname traegt die Sekunde. Zwei Faelle derselben Sekunde
        /// stolperten sonst uebereinander („output file already exists"), und zwar an
        /// einer Stelle, die mit der geprueften Sache nichts zu tun hat.
        /// </summary>
        public void Dispose()
        {
            KiMaskenbruecke.Leeren();
            Schreibnaht.Schreibrecht = _schreibrechtVorher;
        }

        private static KiAusfuehrung Frisch()
        {
            // Der Ausfuehrer traegt seit #201 seinen Zustand als INSTANZ - genau dafuer:
            // Jeder Fall bekommt ein unberuehrtes Sitzungsgedaechtnis und eine eigene
            // Laufmarke, ohne dass jemand ein Zuruecksetzen vergessen kann.
            return new KiAusfuehrung { Schreibrecht = () => true };
        }

        // ==================================================================
        //  Das Register
        // ==================================================================

        [Fact]
        public void Das_Register_liegt_im_Kern_und_fuehrt_alle_Aktionen()
        {
            KiRegister register = Frisch().Register;

            foreach (string name in ERWARTET)
                Assert.True(register.Finde(name) != null, "Aktion fehlt: " + name);

            Assert.Equal(ERWARTET.Length, register.Alle.Count);
        }

        [Fact]
        public void Kein_Aktionsname_kommt_doppelt_vor()
        {
            var gesehen = new HashSet<string>(StringComparer.Ordinal);

            foreach (KiAktion a in Frisch().Register.Alle)
                Assert.True(gesehen.Add(a.Name), "Aktionsname doppelt: " + a.Name);
        }

        /// <summary>
        /// Die Verbotsliste aus Fachkonzept 5.4 — geprueft an Aktionsname UND
        /// Andockpunkt. Der Name allein reichte nicht: Eine Aktion „aufraeumen", die
        /// <c>ProjektCtrl.Delete</c> ruft, hiesse harmlos.
        /// </summary>
        [Fact]
        public void Die_Verbotsliste_steht_nicht_im_Register()
        {
            foreach (KiAktion a in Frisch().Register.Alle)
            {
                string name = a.Name.ToLowerInvariant();
                string andock = (a.Andockpunkt ?? "").ToLowerInvariant();

                foreach (string wort in VERBOTEN)
                {
                    Assert.False(name.Contains(wort),
                                 "Verbotenes Wort '" + wort + "' im Aktionsnamen: " + a.Name);

                    // Der Andockpunkt darf das Wort im Klartext einer Begruendung tragen
                    // („abgeloest"), aber nicht als METHODENNAME hinter dem Punkt.
                    if (wort == "loesch" || wort == "delete")
                        Assert.False(andock.Contains("." + wort),
                                     "Verbotener Andockpunkt in " + a.Name + ": " + a.Andockpunkt);
                }
            }
        }

        /// <summary>
        /// Jede Aktion oberhalb von Stufe 1 traegt eine Vorschau — sonst bestaetigte der
        /// Anwender eine Ueberschrift (Fachkonzept 3.5, Punkt 1).
        /// </summary>
        [Fact]
        public void Jede_schreibende_und_rechnende_Aktion_traegt_eine_Vorschau()
        {
            foreach (KiAktion a in Frisch().Register.Alle)
                if (a.Stufe != Schutzstufe.Lesen)
                    Assert.True(a.Vorschau != null, "Vorschau fehlt: " + a.Name);
        }

        /// <summary>Jede Aktion hat einen Ausfuehrungsweg — kurz oder lang.</summary>
        [Fact]
        public void Jede_Aktion_ist_ausfuehrbar()
        {
            foreach (KiAktion a in Frisch().Register.Alle)
                Assert.True(a.Ausfuehrbar, "Ohne Ausfuehrungsweg: " + a.Name);
        }

        /// <summary>Die drei Rechenaktionen gehoeren zu Stufe 3 und laufen lang.</summary>
        [Fact]
        public void Die_drei_Rechenaktionen_tragen_Stufe_drei_und_eine_Laufumgebung()
        {
            KiRegister register = Frisch().Register;

            foreach (string name in new[] { "simulation_rechnen", "peak_ziel_bestimmen", "flotte_bewerten" })
            {
                KiAktion a = register.Finde(name);
                Assert.Equal(Schutzstufe.Rechnen, a.Stufe);
                Assert.True(a.AusfuehrenLang != null, "Ohne Laufumgebung: " + name);
            }
        }

        // ==================================================================
        //  dialog_oeffnen: die Zieltabelle
        // ==================================================================

        /// <summary>
        /// <b>Der Waechter der Tabelle:</b> Jeder Katalogeintrag hat ein Ziel. Waechst
        /// dem Dialogkatalog eine sechste Maske zu, faellt die fehlende Zeile HIER auf
        /// und nicht beim Anwender.
        /// </summary>
        [Fact]
        public void Jede_Katalogmaske_hat_ein_Oeffnungsziel()
        {
            foreach (string maske in KiDialoge.Katalog.Maskennamen())
                Assert.True(KiMaskenziele.Kennt(maske), "Ohne Ziel: " + maske);
        }

        [Fact]
        public void Eine_unbekannte_Maske_hat_kein_Ziel()
        {
            Assert.False(KiMaskenziele.Kennt("Form_GibtEsNicht"));
            Assert.False(KiMaskenziele.Kennt(""));
            Assert.Equal("", KiMaskenziele.Ziel(null));
        }

        [Fact]
        public async Task Dialog_oeffnen_lehnt_eine_unbekannte_Maske_benannt_ab()
        {
            KiAusfuehrung schicht = Frisch();

            KiErgebnis ergebnis = await schicht.AusfuehrenAsync(
                "dialog_oeffnen",
                new Dictionary<string, object> { ["maske"] = "Form_GibtEsNicht" });

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("Form_GibtEsNicht", ergebnis.Text);
        }

        // ==================================================================
        //  feld_setzen ueber die Bruecke
        // ==================================================================

        [Fact]
        public async Task Feld_setzen_schreibt_ueber_die_Bruecke_in_den_offenen_Dialog()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden();

            KiAusfuehrung schicht = Frisch();
            KiErgebnis ergebnis = await Mit(schicht, "feld_setzen", Werte("gesamtvolumen", "1500"));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Equal(1500, dialog.Gesamtvolumen);
            Assert.True(dialog.Aufgefrischt, "Die Maske wurde nicht aufgefrischt.");
        }

        /// <summary>
        /// Die KULTURFRAGE: Der Anwender tippt „1,5", das Modell schreibt „1.5" — beide
        /// muessen ankommen (<see cref="KiFeldwandler"/>).
        /// </summary>
        [Theory]
        [InlineData("2500", 2500)]
        [InlineData("2500.4", 2500)]
        [InlineData("2500,4", 2500)]
        public async Task Feld_setzen_nimmt_beide_Zahlschreibweisen(string text, int erwartet)
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden();

            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", text));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Equal(erwartet, dialog.Gesamtvolumen);
        }

        [Fact]
        public async Task Ohne_offene_Maske_wird_benannt_abgelehnt()
        {
            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", "1500"));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.NotEqual("", ergebnis.Text);
        }

        [Fact]
        public async Task Ein_nicht_deklariertes_Feld_wird_benannt_abgelehnt()
        {
            new Pruefdialog().Anmelden();

            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gibtsnicht", "1"));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("gibtsnicht", ergebnis.Text);
        }

        /// <summary>
        /// Ein LESENDES Feld (die Diagnose der Stromspeicher-Ansicht) hat keinen Setzer —
        /// und wird deshalb abgelehnt, nicht still uebergangen.
        /// </summary>
        [Fact]
        public async Task Ein_nur_lesbares_Feld_wird_benannt_abgelehnt()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden(nurLesbar: true);

            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", "1500"));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Null(dialog.Gesamtvolumen);
        }

        [Fact]
        public async Task Im_Lesemodus_wird_benannt_abgelehnt()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden();

            Schreibnaht.Schreibrecht = () => false;
            try
            {
                KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", "1500"));

                Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
                Assert.Null(dialog.Gesamtvolumen);
            }
            finally { Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt; }
        }

        [Fact]
        public async Task Ein_schreibgeschuetzter_Katalogsatz_wird_benannt_abgelehnt()
        {
            var dialog = new Pruefdialog { Geschuetzt = true };
            dialog.Anmelden();

            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", "1500"));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Null(dialog.Gesamtvolumen);
        }

        [Fact]
        public async Task Ein_Typfehler_wird_benannt_abgelehnt()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden();

            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", "abc"));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("abc", ergebnis.Text);
            Assert.Null(dialog.Gesamtvolumen);
        }

        /// <summary>
        /// Der BESTAETIGUNGSTEXT nennt alt und neu — er entsteht im Kern
        /// (<see cref="KiFeldBlock"/>) und nie aus Modelltext.
        /// </summary>
        [Fact]
        public async Task Der_Bestaetigungstext_nennt_alt_und_neu()
        {
            var dialog = new Pruefdialog { Gesamtvolumen = 800 };
            dialog.Anmelden();

            KiAusfuehrung schicht = Frisch();
            KiAufruf aufruf = Aufruf(schicht, "feld_setzen", Werte("gesamtvolumen", "1500"));

            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, CancellationToken.None);

            Assert.True(vorbereitung.Freigabe != null, vorbereitung.Ablehnung?.Text);
            Assert.Contains("800", vorbereitung.Freigabe.Text);
            Assert.Contains("1500", vorbereitung.Freigabe.Text);
        }

        /// <summary>
        /// Der BEFUND des Dialogs geht als Meldung zurueck — der Assistent ersetzt die
        /// Pruefung des Bestands nicht, er loest sie aus.
        /// </summary>
        [Fact]
        public async Task Der_Befund_der_Dialogpruefung_steht_im_Ergebnis()
        {
            var dialog = new Pruefdialog { Befund = "Das Volumen ist zu gross." };
            dialog.Anmelden();

            KiErgebnis ergebnis = await Mit(Frisch(), "feld_setzen", Werte("gesamtvolumen", "99999"));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Contains(ergebnis.Meldungen, m => m.Contains("zu gross"));
        }

        /// <summary>
        /// MEHRERE Felder in EINER Bestaetigung (Auftrag #201, Punkt 2) — der Weg ist
        /// <c>formular_ausfuellen</c> mit „feld=wert; feld=wert".
        /// </summary>
        [Fact]
        public async Task Formular_ausfuellen_setzt_mehrere_Felder_in_einem_Block()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden();

            KiErgebnis ergebnis = await Mit(Frisch(), "formular_ausfuellen",
                                            new Dictionary<string, object>
                                            {
                                                ["maske"] = KiMaskennamen.PUFFERSPEICHER,
                                                ["werte"] = "gesamtvolumen=2000"
                                            });

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Equal(2000, dialog.Gesamtvolumen);
        }

        // ==================================================================
        //  dialog_speichern — ohne Freigabe geschieht nichts
        // ==================================================================

        /// <summary>
        /// <b>Der Riegel:</b> Ohne ausdrueckliche Freigabe wird nicht gespeichert — und
        /// der Speicherweg des Dialogs wird nicht einmal gerufen.
        /// </summary>
        [Fact]
        public async Task Dialog_speichern_laeuft_nie_ohne_Freigabe()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden();

            KiAusfuehrung schicht = Frisch();
            KiAufruf aufruf = Aufruf(schicht, "dialog_speichern",
                                     new Dictionary<string, object>
                                     {
                                         ["maske"] = KiMaskennamen.PUFFERSPEICHER
                                     });

            KiErgebnis ergebnis = await schicht.AusfuehrenAsync(aufruf, null, CancellationToken.None);

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.False(dialog.Gespeichert, "Der Speicherweg wurde ohne Freigabe gerufen.");
        }

        /// <summary>
        /// Eine Maske OHNE Speicherweg lehnt benannt ab, statt still nichts zu tun.
        /// </summary>
        [Fact]
        public async Task Eine_Maske_ohne_Speicherweg_lehnt_benannt_ab()
        {
            var dialog = new Pruefdialog();
            dialog.Anmelden(ohneSpeicherweg: true);

            KiAusfuehrung schicht = Frisch();
            KiAufruf aufruf = Aufruf(schicht, "dialog_speichern",
                                     new Dictionary<string, object>
                                     {
                                         ["maske"] = KiMaskennamen.PUFFERSPEICHER
                                     });

            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, CancellationToken.None);

            Assert.Null(vorbereitung.Freigabe);
            Assert.Equal(KiStatus.Abgelehnt, vorbereitung.Ablehnung.Status);
        }

        /// <summary>
        /// <b>Der Sicherungspunkt steht VOR dem Speicherweg</b> (Fachkonzept 4.4,
        /// Punkt 1): Wenn der Dialog gerufen wird, liegt die Kopie schon — und ihr Pfad
        /// steht im Ergebnis, damit der Anwender weiss, wohin der Vorzustand gesichert
        /// ist.
        /// </summary>
        [Fact]
        public async Task Dialog_speichern_legt_den_Sicherungspunkt_vor_dem_Aufruf_an()
        {
            if (!_db.Vorhanden) return;

            var dialog = new Pruefdialog();
            dialog.Anmelden();

            KiErgebnis ergebnis = await MitFreigabe(Frisch(), "dialog_speichern",
                                                    new Dictionary<string, object>
                                                    {
                                                        ["maske"] = KiMaskennamen.PUFFERSPEICHER
                                                    });

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.True(dialog.Gespeichert);
            Assert.NotEqual("", dialog.SicherungBeimAufruf);
            Assert.Contains(ergebnis.Meldungen, m => m.Contains(dialog.SicherungBeimAufruf));
        }

        // ==================================================================
        //  variante_anlegen: das optionale QUELLPROJEKT (Auftrag #240)
        // ==================================================================

        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030) — der STAMM.</summary>
        private const string STAMM = "Referenz BHKW-Kaskade (Regressionstest)";

        /// <summary>
        /// Ein anderes Projekt der Testdatenbank — es fuehrt PV-Zeilen, der Stamm
        /// nicht; daran ist die Tiefkopie zu erkennen (dieselbe Ausgangslage wie in
        /// <c>ProjektpflegeTests</c>).
        /// </summary>
        private const string QUELLE = "Laurentiuskirche";

        /// <summary>
        /// <b>Mit Quellprojekt</b> (Auftrag #240): Die Variante traegt den INHALT der
        /// Quelle, Name und Gruppenzugehoerigkeit kommen weiter vom STAMM.
        /// </summary>
        [Fact]
        public async Task Variante_anlegen_nimmt_den_Inhalt_aus_dem_Quellprojekt()
        {
            if (!_db.Vorhanden) return;

            KiErgebnis ergebnis = await MitFreigabe(Frisch(), "variante_anlegen",
                new Dictionary<string, object>
                {
                    ["stammprojekt"] = STAMM,
                    ["bezeichner"] = "KI aus Quelle",
                    ["quellprojekt"] = QUELLE
                });

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);

            int neueId = new ProjektDuplizierenCtrl().GetProjektId(STAMM + " - KI aus Quelle");
            Assert.True(neueId > 0, "Die Variante traegt nicht den Namen aus Zielname.");

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(QUELLE);
            Assert.Equal(new VariantenCtrl().StammRefDerVariante(neueId),
                         new ProjektDuplizierenCtrl().GetProjektId(STAMM));
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_PV WHERE ID_Projekt = " + quelle),
                         Zahl("SELECT COUNT(*) FROM Tab_PV WHERE ID_Projekt = " + neueId));
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_PV WHERE ID_Projekt = " + neueId) > 0,
                        "Die Ausgangslage traegt nicht: die Quelle fuehrt keine PV-Zeilen.");
        }

        /// <summary>
        /// <b>Gegenprobe:</b> OHNE Quellprojekt bleibt alles wie vor #240 — kopiert wird
        /// der Stamm, und der fuehrt keine PV-Zeilen.
        /// </summary>
        [Fact]
        public async Task Variante_anlegen_ohne_Quelle_kopiert_weiterhin_den_Stamm()
        {
            if (!_db.Vorhanden) return;

            KiErgebnis ergebnis = await MitFreigabe(Frisch(), "variante_anlegen",
                new Dictionary<string, object>
                {
                    ["stammprojekt"] = STAMM,
                    ["bezeichner"] = "KI ohne Quelle"
                });

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);

            int neueId = new ProjektDuplizierenCtrl().GetProjektId(STAMM + " - KI ohne Quelle");
            Assert.True(neueId > 0);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_PV WHERE ID_Projekt = " + neueId));
        }

        /// <summary>
        /// Die VORSCHAU nennt den Zielnamen aus <c>VariantenCtrl.Zielname</c> — dieselbe
        /// Regel, die das Anlegen gleich darauf anwendet (seit #240 kein Spiegel mehr) —
        /// und bei gewaehlter Quelle auch diese.
        /// </summary>
        [Fact]
        public async Task Die_Vorschau_nennt_Zielname_und_Quelle()
        {
            if (!_db.Vorhanden) return;

            KiAusfuehrung schicht = Frisch();
            KiAufruf aufruf = Aufruf(schicht, "variante_anlegen",
                new Dictionary<string, object>
                {
                    ["stammprojekt"] = STAMM,
                    ["bezeichner"] = "Vorschauprobe",
                    ["quellprojekt"] = QUELLE
                });

            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, CancellationToken.None);

            Assert.True(vorbereitung.Freigabe != null, vorbereitung.Ablehnung?.Text);
            Assert.Contains(new VariantenCtrl().Zielname(STAMM, "Vorschauprobe"),
                            vorbereitung.Freigabe.Text);
            Assert.Contains(QUELLE, vorbereitung.Freigabe.Text);
        }

        /// <summary>
        /// Die Quelle DARF nicht der Stamm sein: „Inhalt aus dem Stamm" ist genau der
        /// Fall ohne Angabe. Die Vorbedingung sagt das benannt und legt nichts an.
        /// </summary>
        [Fact]
        public async Task Die_Quelle_darf_nicht_das_Stammprojekt_selbst_sein()
        {
            if (!_db.Vorhanden) return;

            KiErgebnis ergebnis = await MitFreigabe(Frisch(), "variante_anlegen",
                new Dictionary<string, object>
                {
                    ["stammprojekt"] = STAMM,
                    ["bezeichner"] = "Quelle gleich Stamm",
                    ["quellprojekt"] = STAMM
                });

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = '" +
                                 STAMM + " - Quelle gleich Stamm'"));
        }

        /// <summary>Zaehlwert aus der Arbeitskopie.</summary>
        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        // ==================================================================
        //  Rechnen: Fortschritt und Abbruch
        // ==================================================================

        /// <summary>
        /// Der Rechenweg der offenen Ansicht bekommt die <see cref="KiLaufumgebung"/> —
        /// er MELDET seinen Fortschritt, und der Empfaenger haengt am Ausfuehrer.
        /// </summary>
        [Fact]
        public async Task Eine_Rechenaktion_meldet_ihren_Fortschritt()
        {
            if (!_db.Vorhanden) return;

            var ansicht = new Pruefansicht();
            ansicht.Anmelden();

            var gemeldet = new List<KiFortschritt>();
            KiAusfuehrung schicht = Frisch();
            schicht.Fortschritt = new Sammelfortschritt(gemeldet);

            KiErgebnis ergebnis = await MitFreigabe(schicht, "flotte_bewerten",
                                                    new Dictionary<string, object>());

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Contains(gemeldet, f => f.Text.Length > 0);
            Assert.True(ansicht.Gerechnet, "Der Rechenweg der Ansicht wurde nicht gerufen.");
        }

        /// <summary>
        /// Ein ABBRUCH hinterlaesst keinen halben Zustand: Die Aktion endet als
        /// abgebrochen, und die Ansicht hat ihr Ergebnis nicht gesetzt.
        /// </summary>
        [Fact]
        public async Task Eine_Rechenaktion_laesst_sich_abbrechen()
        {
            if (!_db.Vorhanden) return;

            var ansicht = new Pruefansicht { BrichtAb = true };
            ansicht.Anmelden();

            KiAusfuehrung schicht = Frisch();
            var quelle = new CancellationTokenSource();

            KiAufruf aufruf = Aufruf(schicht, "flotte_bewerten", new Dictionary<string, object>());
            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, quelle.Token);
            Assert.True(vorbereitung.Freigabe != null, vorbereitung.Ablehnung?.Text);
            vorbereitung.Freigabe.Erteilen();

            KiErgebnis ergebnis = await schicht.AusfuehrenAsync(aufruf, vorbereitung.Freigabe, quelle.Token);

            Assert.Equal(KiStatus.Abgebrochen, ergebnis.Status);
            Assert.False(ansicht.Gerechnet, "Die Ansicht hat trotz Abbruch gerechnet.");
        }

        /// <summary>
        /// Ohne offene Ansicht gibt es keinen Rechenweg — und die Ablehnung nennt sie.
        /// </summary>
        [Fact]
        public async Task Ohne_offene_Ansicht_wird_die_Rechnung_benannt_abgelehnt()
        {
            KiAusfuehrung schicht = Frisch();

            KiErgebnis ergebnis = await schicht.AusfuehrenAsync(
                "flotte_bewerten", new Dictionary<string, object>());

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.NotEqual("", ergebnis.Text);
        }

        /// <summary>
        /// <b>Eine LANGE Aktion laeuft nicht auf dem Oberflaechenfaden</b> (Auftrag
        /// #214). Der Weg dorthin wird genau EINMAL benutzt — fuer die Vorschau der
        /// Vorbereitung; der Lauf selbst geht in den Hintergrund.
        /// </summary>
        /// <remarks>
        /// <b>Warum das der Kern der Sache ist.</b> Liefe die Rechnung ueber
        /// <c>AufOberflaeche</c>, waere der Bedienfaden fuer ihre Dauer belegt: Der
        /// Fortschrittsbalken zeichnete nicht, und der Klick auf „Abbrechen" erreichte
        /// die Warteschlange erst NACH dem Lauf. Genau das war der Restpunkt aus #201.
        /// </remarks>
        [Fact]
        public async Task Eine_Rechenaktion_laeuft_nicht_auf_dem_Oberflaechenfaden()
        {
            if (!_db.Vorhanden) return;

            var ansicht = new Pruefansicht();
            ansicht.Anmelden();

            int wege = 0;
            KiAusfuehrung schicht = Frisch();
            schicht.AufOberflaeche = arbeit => { Interlocked.Increment(ref wege); return arbeit(); };

            KiErgebnis ergebnis = await MitFreigabe(schicht, "flotte_bewerten",
                                                    new Dictionary<string, object>());

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.True(ansicht.Gerechnet, "Der Rechenweg der Ansicht wurde nicht gerufen.");
            Assert.Equal(1, wege);
        }

        /// <summary>
        /// Die GEGENPROBE: Eine kurze Schreibaktion benutzt den Weg auf den
        /// Oberflaechenfaden ZWEIMAL — fuer die Vorschau und fuer den Lauf. Sie
        /// beruehrt die Bestandscontroller, und die sind nicht threadsicher.
        /// </summary>
        [Fact]
        public async Task Eine_kurze_Aktion_laeuft_weiterhin_auf_dem_Oberflaechenfaden()
        {
            if (!_db.Vorhanden) return;

            var dialog = new Pruefdialog { Gesamtvolumen = 1000 };
            dialog.Anmelden();

            int wege = 0;
            KiAusfuehrung schicht = Frisch();
            schicht.AufOberflaeche = arbeit => { Interlocked.Increment(ref wege); return arbeit(); };

            KiErgebnis ergebnis = await MitFreigabe(schicht, "feld_setzen",
                                                    Werte("gesamtvolumen", "1500"));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Equal(2, wege);
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static Dictionary<string, object> Werte(string feld, string wert)
            => new Dictionary<string, object>
               {
                   ["maske"] = KiMaskennamen.PUFFERSPEICHER,
                   ["feld"] = feld,
                   ["wert"] = wert
               };

        private static KiAufruf Aufruf(KiAusfuehrung schicht, string name,
                                       IReadOnlyDictionary<string, object> werte)
        {
            KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, name, werte);
            Assert.True(geprueft.Gueltig, geprueft.FehlerText());
            return geprueft.Aufruf;
        }

        /// <summary>Vorbereiten, freigeben, ausfuehren — der ganze Weg einer Schreibaktion.</summary>
        private static async Task<KiErgebnis> MitFreigabe(KiAusfuehrung schicht, string name,
                                                          IReadOnlyDictionary<string, object> werte)
        {
            KiAufruf aufruf = Aufruf(schicht, name, werte);
            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, CancellationToken.None);
            if (vorbereitung.Freigabe == null) return vorbereitung.Ablehnung;

            vorbereitung.Freigabe.Erteilen();
            return await schicht.AusfuehrenAsync(aufruf, vorbereitung.Freigabe, CancellationToken.None);
        }

        private static Task<KiErgebnis> Mit(KiAusfuehrung schicht, string name,
                                            IReadOnlyDictionary<string, object> werte)
            => MitFreigabe(schicht, name, werte);

        /// <summary>Ein Fortschrittsempfaenger, der mitschreibt.</summary>
        private sealed class Sammelfortschritt : IProgress<KiFortschritt>
        {
            private readonly List<KiFortschritt> _ziel;
            internal Sammelfortschritt(List<KiFortschritt> ziel) { _ziel = ziel; }
            public void Report(KiFortschritt wert) { lock (_ziel) _ziel.Add(wert); }
        }

        /// <summary>
        /// Ein Dialog dieses Prueflings: EIN Feld, dieselbe Deklaration wie der
        /// Pufferspeichereditor, aber ohne Oberflaeche.
        /// </summary>
        private sealed class Pruefdialog
        {
            internal int? Gesamtvolumen { get; set; }
            internal bool Aufgefrischt { get; private set; }
            internal bool Gespeichert { get; private set; }

            /// <summary>Der Sicherungspfad, wie er BEIM AUFRUF des Speicherwegs stand.</summary>
            internal string SicherungBeimAufruf { get; private set; } = "";
            internal bool Geschuetzt { get; set; }
            internal string Befund { get; set; } = "";

            internal void Anmelden(bool nurLesbar = false, bool ohneSpeicherweg = false)
            {
                KiDialog eintrag = KiDialoge.Katalog.Finde(KiMaskennamen.PUFFERSPEICHER);
                KiDialogFeld feld = eintrag.FindeFeld("gesamtvolumen");

                Action<object> setzen = nurLesbar
                    ? null
                    : (Action<object>)(wert => Gesamtvolumen = (int?)wert);

                var zugang = new KiFeldzugang(feld, () => Gesamtvolumen, setzen, typeof(int?));

                var haken = new KiMaskenhaken
                {
                    Auffrischen = () => Aufgefrischt = true,
                    Pruefen = () => Befund,
                    Schreibgeschuetzt = () => Geschuetzt
                };

                if (!ohneSpeicherweg)
                    haken.Speichern = () =>
                    {
                        Gespeichert = true;
                        SicherungBeimAufruf = KiSicherungspunkt.Pfad;
                        return Task.FromResult(KiErgebnis.Ok("gespeichert", anzahl: 1));
                    };

                KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, new[] { zugang }, haken);
            }
        }

        /// <summary>
        /// Eine Stromspeicher-Ansicht dieses Prueflings: keine Felder ausser dem
        /// Pflichtfeld, dafuer ein Rechenweg.
        /// </summary>
        private sealed class Pruefansicht
        {
            internal bool Gerechnet { get; private set; }
            internal bool BrichtAb { get; set; }

            internal void Anmelden()
            {
                KiDialog eintrag = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG);
                KiDialogFeld feld = eintrag.FindeFeld("betriebsziel");

                var zugang = new KiFeldzugang(feld, () => "PeakShaving", null, typeof(string));

                var haken = new KiMaskenhaken();
                haken.Rechenweg("flotte_bewerten", umgebung =>
                {
                    umgebung.Melde(0.5, "rechnet");
                    if (BrichtAb) throw new OperationCanceledException();
                    Gerechnet = true;
                    return Task.FromResult(KiErgebnis.Ok("bewertet", anzahl: 1));
                });

                KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, new[] { zugang }, haken);
            }
        }
    }
}
