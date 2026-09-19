using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die dreizehn Proben des SAMMELTRANSFERS (Auftrag PI-1, Anwenderentscheid
    /// PI-Q1 vom 19.09.2026).
    ///
    /// <para><b>Was sie prüfen, was die dreizehn Bestandsproben nicht können.</b>
    /// <c>ProjekttransferTests</c> (P1–P13) misst EIN Paket: Rundreise, Zeilenzahlen,
    /// Verknüpfung, Versionsabweisung, PV-Beilage. PI-1 fügt eine Ebene darüber ein —
    /// mehrere Gruppen in einem Export, mehrere Pakete in einem Import —, und genau
    /// dort sitzen die neuen Fehlerquellen: die Reihenfolge (Variante vor Stamm), die
    /// Namensabbildung über Paketgrenzen und der Stamm, der in zwei Paketen steckt.
    /// P1–P13 bleiben unverändert; PI13 hält fest, dass der Einzelweg keiner anderen
    /// Regel folgt als vorher.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Probe</b> — jede schreibt (schon der Export
    /// öffnet einen Vorgang); geteilt sähe ein Fall den nächsten.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjekttransferSammelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // Die zwei Stammgruppen des Testbestands, an denen die Proben rechnen.
        private const string STAMM_A = "Wöhler";                       // 1019
        private const string A_VAR_1 = "Wöhler - Test1";               // 1023
        private const string A_VAR_2 = "Wöhler - Test2";               // 1024
        private const string STAMM_B = "Beispiel WP WG 1";             // 1026
        private const string B_VAR_1 = "Beispiel WP WG 1 - Andere WP"; // 1027
        private const string B_VAR_2 = "Beispiel WP WG 1 - Erdwärme";  // 1029

        // =============================================================================
        //  PI1 — Die Wahlregel, ohne Datenbank
        // =============================================================================
        [Fact]
        public void PI1_Gruppieren_bildet_je_Stammgruppe_genau_eine_Gruppe()
        {
            IReadOnlyList<ProjektKopfZeile> bestand = Bestand();

            // (a) Eine gewaehlte VARIANTE zieht ihren Stamm; Hauptprojekt ist der Stamm.
            IReadOnlyList<Transfergruppe> a = Projektgruppierung.Gruppieren(bestand, new[] { 1023 });
            Assert.Single(a);
            Assert.Equal(STAMM_A, a[0].Stamm);
            Assert.Equal(1019, a[0].StammId);
            Assert.Contains(A_VAR_1, a[0].Varianten);
            Assert.Equal(new[] { 1019 }, Projektgruppierung.Nachgezogen(bestand, new[] { 1023 }));

            // (b) Stamm UND eigene Variante sind EINE Gruppe, nicht zwei.
            IReadOnlyList<Transfergruppe> b = Projektgruppierung.Gruppieren(bestand, new[] { 1019, 1023 });
            Assert.Single(b);
            Assert.Equal(STAMM_A, b[0].Stamm);
            Assert.Empty(Projektgruppierung.Nachgezogen(bestand, new[] { 1019, 1023 }));

            // (c) ZWEI Varianten desselben Stamms: eine Gruppe, der Stamm einmal.
            IReadOnlyList<Transfergruppe> c = Projektgruppierung.Gruppieren(bestand, new[] { 1023, 1024 });
            Assert.Single(c);
            Assert.Equal(new[] { A_VAR_1, A_VAR_2 }, c[0].Varianten.ToArray());

            // (d) Zwei verschiedene Staemme: zwei Gruppen.
            IReadOnlyList<Transfergruppe> d = Projektgruppierung.Gruppieren(bestand, new[] { 1023, 1027 });
            Assert.Equal(2, d.Count);
            Assert.Equal(new[] { STAMM_B, STAMM_A }, d.Select(g => g.Stamm).OrderBy(n => n).ToArray());

            // (e) Eine ausdruecklich GEWAEHLTE Variante laesst sich nicht abwaehlen;
            //     eine bloss mitgenommene schon.
            IReadOnlyList<Transfergruppe> e = Projektgruppierung.Gruppieren(
                bestand, new[] { 1023 }, new[] { 1023, 1024 });
            Assert.Single(e);
            Assert.Equal(new[] { A_VAR_1 }, e[0].Varianten.ToArray());

            // (f) Die Variantenzahl je Projekt — die Spalte der Liste.
            IReadOnlyDictionary<int, int> zahl = Projektgruppierung.Variantenzahl(bestand);
            Assert.Equal(2, zahl[1019]);
            Assert.Equal(2, zahl[1026]);
            Assert.False(zahl.ContainsKey(1030));
        }

        // =============================================================================
        //  PI2 — Zwei Gruppen, zwei Pakete
        // =============================================================================
        [Fact]
        public void PI2_ExportGruppen_schreibt_ein_Paket_je_Gruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            IReadOnlyList<ProjektKopfZeile> bestand = ProjektCtrl.NamenListe();
            IReadOnlyList<Transfergruppe> gruppen =
                Projektgruppierung.Gruppieren(bestand, new[] { 1019, 1026 });
            Assert.Equal(2, gruppen.Count);

            ExportBilanz bilanz = new ProjektExportImportCtrl().ExportGruppen(gruppen, ordner.Pfad);

            Assert.Equal(2, bilanz.Pakete);
            Assert.Equal(2, bilanz.Geschrieben);
            Assert.Equal(0, bilanz.Fehler);

            string a = Path.Combine(ordner.Pfad, STAMM_A + ".wpx");
            string b = Path.Combine(ordner.Pfad, STAMM_B + ".wpx");
            Assert.True(File.Exists(a), "Paket des Stamms A fehlt.");
            Assert.True(File.Exists(b), "Paket des Stamms B fehlt.");

            Paketkopf ka = ProjektExportImportCtrl.PaketKopf(a);
            Paketkopf kb = ProjektExportImportCtrl.PaketKopf(b);
            Assert.Equal(STAMM_A, ka.Quellprojekt);
            Assert.Equal(STAMM_B, kb.Quellprojekt);
            Assert.Equal(2, ka.Varianten.Count);
            Assert.Equal(2, kb.Varianten.Count);
        }

        // =============================================================================
        //  PI3 — Nur eine Variante gewaehlt: Hauptprojekt bleibt der Stamm
        // =============================================================================
        [Fact]
        public void PI3_Eine_gewaehlte_Variante_reist_mit_ihrem_Stamm_als_Hauptprojekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            IReadOnlyList<ProjektKopfZeile> bestand = ProjektCtrl.NamenListe();
            // Der Anwender hakt NUR die Variante an und waehlt die zweite ab.
            IReadOnlyList<Transfergruppe> gruppen =
                Projektgruppierung.Gruppieren(bestand, new[] { 1023 }, new[] { 1024 });

            ExportBilanz bilanz = new ProjektExportImportCtrl().ExportGruppen(gruppen, ordner.Pfad);
            Assert.Equal(1, bilanz.Geschrieben);

            Paketkopf k = ProjektExportImportCtrl.PaketKopf(
                Path.Combine(ordner.Pfad, STAMM_A + ".wpx"));
            Assert.Equal(STAMM_A, k.Quellprojekt);
            Assert.Equal(new[] { A_VAR_1 }, k.Varianten.ToArray());
            Assert.Equal("", k.StammQuelle);          // das Hauptprojekt ist ein Stamm
        }

        // =============================================================================
        //  PI4 — Dateinamen
        // =============================================================================
        [Fact]
        public void PI4_Ein_zweiter_Lauf_ueberschreibt_das_erste_Paket_nicht()
        {
            using var ordner = new Arbeitsordner();

            string a = ProjektExportImportCtrl.Paketdateiname(ordner.Pfad, "Wöhler");
            Assert.Equal(Path.Combine(ordner.Pfad, "Wöhler.wpx"), a);
            File.WriteAllText(a, "x");

            string b = ProjektExportImportCtrl.Paketdateiname(ordner.Pfad, "Wöhler");
            Assert.Equal(Path.Combine(ordner.Pfad, "Wöhler (2).wpx"), b);
            File.WriteAllText(b, "x");

            Assert.Equal(Path.Combine(ordner.Pfad, "Wöhler (3).wpx"),
                         ProjektExportImportCtrl.Paketdateiname(ordner.Pfad, "Wöhler"));

            // Verbotene Zeichen werden zum Unterstrich, die Laenge ist gedeckelt.
            string c = ProjektExportImportCtrl.Paketdateiname(ordner.Pfad, "A/B:C*D?E");
            Assert.Equal("A_B_C_D_E.wpx", Path.GetFileName(c));
            Assert.True(Path.GetFileNameWithoutExtension(
                ProjektExportImportCtrl.Paketdateiname(ordner.Pfad, new string('x', 300))).Length == 120);
        }

        // =============================================================================
        //  PI5 — Die Exportwache
        // =============================================================================
        [Fact]
        public void PI5_Ein_Variantenprojekt_nimmt_keine_weiteren_Varianten_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();

            // (a) Variante ALS Hauptprojekt mit weiteren Varianten -> benannt abgelehnt.
            bool ok = io.ExportEines(A_VAR_1, new List<string> { A_VAR_2 },
                                     ordner.Datei("verkehrt.wpx"), null, out string grund);
            Assert.False(ok);
            Assert.Contains(A_VAR_1, grund);
            Assert.False(File.Exists(ordner.Datei("verkehrt.wpx")));

            // (b) Dieselbe Variante ALLEIN bleibt erlaubt — der Weg der PV-Beilage.
            Assert.True(io.ExportEines(A_VAR_1, null, ordner.Datei("allein.wpx"), null, out string g2),
                        "Grund: " + g2);
            Assert.Equal(A_VAR_1, ProjektExportImportCtrl.PaketKopf(ordner.Datei("allein.wpx")).Quellprojekt);
        }

        // =============================================================================
        //  PI6 — Variantenpaket VOR dem Stammpaket
        // =============================================================================
        [Fact]
        public void PI6_Die_Reihenfolge_stellt_den_Stamm_vor_seine_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string stammPaket = ordner.Datei("2_stamm.wpx");
            string varPaket = ordner.Datei("1_variante.wpx");
            Assert.True(io.ExportEines(STAMM_A, null, stammPaket, null, out string g1), g1);
            Assert.True(io.ExportEines(A_VAR_1, null, varPaket, null, out string g2), g2);

            // Die Quellprojekte umbenennen: Der Lauf legt sie neu an, und die
            // Verknuepfung darf NICHT am alten Bestand haengen bleiben.
            //
            // ZURUECKBENANNT WIRD NICHT — der Name ist am Ziel jetzt vergeben (UNIQUE
            // INDEX Projektname), und die Arbeitskopie dieser Probe wird ohnehin
            // weggeworfen.
            Umbenennen(1019, STAMM_A + " (weg)");
            Umbenennen(1023, A_VAR_1 + " (weg)");

            // Variantenpaket ZUERST uebergeben — die Reihenfolge dreht der Lauf.
            SammelImportBilanz bilanz = io.ImportierenMehrere(
                new[] { varPaket, stammPaket }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName);

            Assert.Equal(2, bilanz.Importiert);
            Assert.Equal(0, bilanz.Fehler);
            Assert.Equal(STAMM_A, bilanz.Zeilen[0].Quellprojekt);    // Stamm zuerst gelaufen

            int neuStamm = Id(STAMM_A);
            int neuVar = Id(A_VAR_1);
            Assert.True(neuStamm > 0 && neuVar > 0);
            Assert.Equal(neuStamm, new VariantenCtrl().StammRefDerVariante(neuVar));
        }

        // =============================================================================
        //  PI7 — Derselbe Stamm in zwei Paketen
        // =============================================================================
        [Fact]
        public void PI7_Ein_Stamm_aus_zwei_Paketen_entsteht_nur_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string p1 = ordner.Datei("stamm_mit_var1.wpx");
            string p2 = ordner.Datei("stamm_mit_var2.wpx");
            Assert.True(io.ExportEines(STAMM_A, new List<string> { A_VAR_1 }, p1, null, out string g1), g1);
            Assert.True(io.ExportEines(STAMM_A, new List<string> { A_VAR_2 }, p2, null, out string g2), g2);

            // Die Quellen weg, damit der Lauf die Originalnamen vergeben kann.
            Umbenennen(1019, STAMM_A + " (weg)");
            Umbenennen(1023, A_VAR_1 + " (weg)");
            Umbenennen(1024, A_VAR_2 + " (weg)");

            SammelImportBilanz bilanz = io.ImportierenMehrere(
                new[] { p1, p2 }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName);

            Assert.Equal(2, bilanz.Importiert);
            Assert.Equal(1, bilanz.Uebersprungen);
            Assert.Equal(Paketbefund.StammUebersprungen, bilanz.Zeilen[1].Befund);
            Assert.Contains(bilanz.Zeilen[1].Bericht, z => z.Contains(STAMM_A));

            // Der Stamm steht EINMAL da, beide Varianten haengen an derselben Id.
            Assert.Equal(1, Anzahl(STAMM_A));
            int stamm = Id(STAMM_A);
            var v = new VariantenCtrl();
            Assert.Equal(stamm, v.StammRefDerVariante(Id(A_VAR_1)));
            Assert.Equal(stamm, v.StammRefDerVariante(Id(A_VAR_2)));
        }

        // =============================================================================
        //  PI8 — Umbenennung durch Kollision
        // =============================================================================
        [Fact]
        public void PI8_Eine_Variante_folgt_ihrem_umbenannten_Stamm()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string stammPaket = ordner.Datei("stamm.wpx");
            string varPaket = ordner.Datei("variante.wpx");
            Assert.True(io.ExportEines(STAMM_A, null, stammPaket, null, out string g1), g1);
            Assert.True(io.ExportEines(A_VAR_1, null, varPaket, null, out string g2), g2);

            // Beide Namen sind am Ziel belegt (die Quellen selbst) -> "(2)".
            SammelImportBilanz bilanz = io.ImportierenMehrere(
                new[] { stammPaket, varPaket }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName);

            Assert.Equal(2, bilanz.Importiert);
            Assert.Equal(2, bilanz.Umbenannt);
            Assert.Equal(STAMM_A + " (2)", bilanz.Zeilen[0].Zielname);

            int neuStamm = Id(STAMM_A + " (2)");
            int neuVar = Id(A_VAR_1 + " (2)");
            Assert.True(neuStamm > 0, "Der Stamm ist nicht unter „(2)“ entstanden.");
            Assert.True(neuVar > 0, "Die Variante ist nicht unter „(2)“ entstanden.");

            // ENTSCHEIDEND: Die Variante haengt am NEUEN Stamm, nicht am alten
            // gleichnamigen Fremdbestand.
            Assert.Equal(neuStamm, new VariantenCtrl().StammRefDerVariante(neuVar));
        }

        // =============================================================================
        //  PI9 — Teilerfolg
        // =============================================================================
        [Fact]
        public void PI9_Ein_defektes_Paket_haelt_den_Lauf_nicht_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string gut = ordner.Datei("gut.wpx");
            Assert.True(io.ExportEines(STAMM_B, null, gut, null, out string g1), g1);

            string kaputt = ordner.Datei("kaputt.wpx");
            File.WriteAllText(kaputt, "das ist kein ZIP");

            string fremd = ordner.Datei("fremd.wpx");
            MitSchemastand(gut, fremd, SchemaStand.Zielversion + 7);

            SammelImportBilanz bilanz = io.ImportierenMehrere(
                new[] { kaputt, fremd, gut }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName);

            Assert.Equal(3, bilanz.Pakete);
            Assert.Equal(1, bilanz.Importiert);
            Assert.Equal(2, bilanz.Fehler);
            Assert.Contains(bilanz.Zeilen, z => z.Befund == Paketbefund.Unlesbar);
            Assert.Contains(bilanz.Zeilen, z => z.Befund == Paketbefund.Schemastand);

            // Die Datenbank ist heil: das gute Paket steht da, die anderen nicht.
            Assert.Equal(1, Anzahl(STAMM_B + " (2)"));
        }

        // =============================================================================
        //  PI10 — Die PV-Beilage im Sammellauf
        // =============================================================================
        [Fact]
        public void PI10_Die_Verguetungsbeilage_greift_nur_ohne_Stamm_im_Lauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int idStamm = Id(STAMM_A);
            int idVar = Id(A_VAR_1);
            PvSchreiben(idStamm, aktiv: true, aw: 0.1234);
            PvSchreiben(idVar, aktiv: true, aw: 0.0, uebernahme: true);

            var io = new ProjektExportImportCtrl();
            string stammPaket = ordner.Datei("stamm.wpx");
            string varPaket = ordner.Datei("variante.wpx");
            Assert.True(io.ExportEines(STAMM_A, null, stammPaket, null, out string g1), g1);
            Assert.True(io.ExportEines(A_VAR_1, null, varPaket, null, out string g2), g2);

            // (a) NUR das Variantenpaket, Stamm am Ziel unauffindbar -> Beilage greift
            //     (P12 des Bestands, hier ueber den Sammelweg). Zurueckbenannt wird
            //     nicht: Der Name waere am Ziel gleich wieder vergeben.
            Umbenennen(idStamm, STAMM_A + " (weg)");

            SammelImportBilanz alleine = io.ImportierenMehrere(
                new[] { varPaket }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName);

            Assert.Equal(1, alleine.Importiert);
            int idAllein = Id(alleine.Zeilen[0].Zielname);
            ProjektPhotovoltaikModel eigen = new ProjektPhotovoltaikCtrl().Lies(idAllein);
            Assert.NotNull(eigen);
            Assert.False(eigen.UebernahmeStamm);
            Assert.Equal(0.1234, Convert.ToDouble(eigen.AwOverride), 4);

            // (b) Stamm in einem ANDEREN Paket desselben Laufs -> keine Beilage, die
            //     Variante uebernimmt wie am Quellrechner. Der Stamm traegt seinen
            //     Namen schon aus (a) nicht mehr; jetzt geht auch die Quellvariante weg.
            Umbenennen(idVar, A_VAR_1 + " (weg)");

            SammelImportBilanz zusammen = io.ImportierenMehrere(
                new[] { varPaket, stammPaket }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName);

            Assert.Equal(2, zusammen.Importiert);
            int idVarNeu = Id(A_VAR_1);
            Assert.True(idVarNeu > 0, "Die Variante ist nicht unter ihrem Quellnamen entstanden.");
            ProjektPhotovoltaikModel mit = new ProjektPhotovoltaikCtrl().Lies(idVarNeu);
            Assert.True(mit is null || mit.UebernahmeStamm,
                        "Die Variante hat eine eigene Zeile bekommen, obwohl ihr Stamm mitreiste.");
        }

        // =============================================================================
        //  PI11 — Der Paketkopf
        // =============================================================================
        [Fact]
        public void PI11_PaketKopf_liest_das_Manifest_und_wirft_nie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string paket = ordner.Datei("kopf.wpx");
            Assert.True(io.ExportEines(STAMM_A, new List<string> { A_VAR_1 }, paket, null, out string g1), g1);

            Paketkopf k = ProjektExportImportCtrl.PaketKopf(paket);
            Assert.Equal("", k.Fehler);
            Assert.Equal(STAMM_A, k.Quellprojekt);
            Assert.Equal(SchemaStand.Zielversion, k.Schemastand);
            Assert.Equal(2, k.Formatversion);
            Assert.Equal(new[] { A_VAR_1 }, k.Varianten.ToArray());
            Assert.Equal("", k.StammQuelle);
            Assert.NotEqual("", k.Exportdatum);

            // Ein Variantenpaket kennt seinen Stamm — daran haengt die Reihenfolge.
            string varPaket = ordner.Datei("var.wpx");
            Assert.True(io.ExportEines(A_VAR_1, null, varPaket, null, out string g2), g2);
            Assert.Equal(STAMM_A, ProjektExportImportCtrl.PaketKopf(varPaket).StammQuelle);

            // Und nichts davon wirft: weder eine fehlende noch eine kaputte Datei.
            Paketkopf fehlt = ProjektExportImportCtrl.PaketKopf(ordner.Datei("gibtsnicht.wpx"));
            Assert.NotEqual("", fehlt.Fehler);
            string leer = ordner.Datei("leer.wpx");
            using (var z = ZipFile.Open(leer, ZipArchiveMode.Create)) { }
            Assert.NotEqual("", ProjektExportImportCtrl.PaketKopf(leer).Fehler);

            // Reihenfolge: Stamm vor Variante, unaufloesbarer Rest ans Ende.
            var fremd = new Paketkopf("f.wpx", "X", "", 0, 2, Array.Empty<string>(), "Nirgends", "");
            IReadOnlyList<Paketkopf> folge = ProjektExportImportCtrl.Reihenfolge(new[]
            {
                ProjektExportImportCtrl.PaketKopf(varPaket),
                fremd,
                ProjektExportImportCtrl.PaketKopf(paket)
            });
            Assert.Equal(new[] { STAMM_A, A_VAR_1, "X" }, folge.Select(f => f.Quellprojekt).ToArray());
        }

        // =============================================================================
        //  PI12 — Der Fortschritt eines Laufs
        // =============================================================================
        [Fact]
        public void PI12_Der_Fortschritt_laeuft_monoton_von_null_nach_eins()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string p1 = ordner.Datei("a.wpx");
            string p2 = ordner.Datei("b.wpx");
            Assert.True(io.ExportEines(STAMM_A, null, p1, null, out string g1), g1);
            Assert.True(io.ExportEines(STAMM_B, null, p2, null, out string g2), g2);

            var stufen = new List<ImportFortschritt>();
            var melder = new Sammler(stufen);

            SammelImportBilanz bilanz = io.ImportierenMehrere(
                new[] { p1, p2 }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName, melder);

            Assert.Equal(2, bilanz.Importiert);
            Assert.NotEmpty(stufen);
            Assert.All(stufen, f => Assert.Equal("TRANSFER_LAUF_PAKET", f.Schluessel));
            Assert.All(stufen, f => Assert.True(f.Anteil >= 0.0 && f.Anteil <= 1.0,
                                                "Anteil ausserhalb 0..1: " + f.Anteil));
            for (int i = 1; i < stufen.Count; i++)
                Assert.True(stufen[i].Anteil >= stufen[i - 1].Anteil,
                            "Der Fortschritt springt zurueck: " + stufen[i - 1].Anteil +
                            " -> " + stufen[i].Anteil);

            // Die Werte nennen Paketnummer, Paketzahl und Tabelle.
            Assert.Equal(new[] { "1", "2" },
                         stufen.Select(f => f.Werte[0]).Distinct().OrderBy(x => x).ToArray());
            Assert.All(stufen, f => Assert.Equal("2", f.Werte[1]));

            // Je Paket ein eigener Bericht.
            Assert.Equal(2, bilanz.Zeilen.Count);
            Assert.All(bilanz.Zeilen, z => Assert.NotEmpty(z.Bericht));

            // Ein Abbruch VOR dem ersten Paket laesst die Datenbank unberuehrt.
            using var quelle = new CancellationTokenSource();
            quelle.Cancel();
            SammelImportBilanz ab = io.ImportierenMehrere(
                new[] { p1, p2 }, ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, quelle.Token);
            Assert.True(ab.Abgebrochen);
            Assert.Equal(0, ab.Importiert);
            Assert.All(ab.Zeilen, z => Assert.Equal(Paketbefund.Abgebrochen, z.Befund));
        }

        // =============================================================================
        //  PI13 — Die Wache: der Einzelweg bleibt der Einzelweg
        // =============================================================================
        [Fact]
        public void PI13_Importieren_ohne_Sammelstand_verhaelt_sich_wie_vor_PI1()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            var io = new ProjektExportImportCtrl();
            string paket = ordner.Datei("einzeln.wpx");
            Assert.True(io.ExportEines(STAMM_A, null, paket, null, out string g1), g1);

            // Zweimal dasselbe Paket ueber den EINZELWEG: Es entsteht ZWEIMAL, mit
            // hochgezaehltem Namen — kein Dublettenschutz, kein Ueberspringen.
            int a = io.Importieren(paket, null, ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                   null, out string f1);
            Assert.True(a > 0, f1);
            int b = io.Importieren(paket, null, ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                   null, out string f2);
            Assert.True(b > 0, f2);
            Assert.NotEqual(a, b);
            Assert.Equal(1, Anzahl(STAMM_A + " (2)"));
            Assert.Equal(1, Anzahl(STAMM_A + " (3)"));

            // Und der Modus „Abbrechen" bleibt ein Abbruch.
            int c = io.Importieren(paket, STAMM_A, ProjektExportImportCtrl.BeiVorhandenem.Abbrechen,
                                   null, out string f3);
            Assert.Equal(-1, c);
            Assert.Contains(STAMM_A, f3);
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>Ein Bestand OHNE Datenbank — die Zeilen, die PI1 braucht.</summary>
        private static IReadOnlyList<ProjektKopfZeile> Bestand() => new[]
        {
            new ProjektKopfZeile(1019, STAMM_A),
            new ProjektKopfZeile(1023, A_VAR_1, StammId: 1019, Bezeichner: "Test1", StammName: STAMM_A),
            new ProjektKopfZeile(1024, A_VAR_2, StammId: 1019, Bezeichner: "Test2", StammName: STAMM_A),
            new ProjektKopfZeile(1026, STAMM_B),
            new ProjektKopfZeile(1027, B_VAR_1, StammId: 1026, Bezeichner: "Andere WP", StammName: STAMM_B),
            new ProjektKopfZeile(1029, B_VAR_2, StammId: 1026, Bezeichner: "Erdwärme", StammName: STAMM_B),
            new ProjektKopfZeile(1030, "Referenz BHKW-Kaskade (Regressionstest)")
        };

        /// <summary>Der Melder, der die Stufen mitschreibt.</summary>
        private sealed class Sammler : IProgress<ImportFortschritt>
        {
            private readonly List<ImportFortschritt> _ziel;
            public Sammler(List<ImportFortschritt> ziel) => _ziel = ziel;
            public void Report(ImportFortschritt wert) => _ziel.Add(wert);
        }

        /// <summary>Ein Ordner fuer die Paketdateien einer Probe; er raeumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            public Arbeitsordner()
            {
                Pfad = Path.Combine(Path.GetTempPath(),
                                    "epos-sammel-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(Pfad);
            }

            public string Pfad { get; }

            public string Datei(string name) => Path.Combine(Pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(Pfad, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        private static int Id(string projektname) => new ProjektDuplizierenCtrl().GetProjektId(projektname);

        private static int Anzahl(string projektname)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?",
                new DbParam("@n", projektname));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        private static void Umbenennen(int idProjekt, string name) =>
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                new DbParam("@n", name), new DbParam("@id", idProjekt)));

        private static void PvSchreiben(int idProjekt, bool aktiv, double aw, bool uebernahme = false)
        {
            var ctrl = new ProjektPhotovoltaikCtrl();
            ProjektPhotovoltaikModel m = ctrl.LiesOderVorbelegt(idProjekt);
            m.ID_Projekt = idProjekt;
            m.Aktiv = aktiv;
            m.AwOverride = aw;
            m.Inbetriebnahme = new DateTime(2026, 1, 1);
            m.UebernahmeStamm = uebernahme;
            Assert.True(ctrl.Speichern(m));
        }

        /// <summary>Schreibt das Paket mit einem FREMDEN Schemastand neu.</summary>
        private static void MitSchemastand(string quelle, string ziel, int stand)
        {
            var eintraege = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (ZipArchive z = ZipFile.OpenRead(quelle))
                foreach (ZipArchiveEntry e in z.Entries)
                {
                    using var s = e.Open();
                    using var m = new MemoryStream();
                    s.CopyTo(m);
                    eintraege[e.FullName] = m.ToArray();
                }

            JsonObject wurzel = JsonNode.Parse(
                new UTF8Encoding(false).GetString(eintraege["manifest.json"]).TrimStart('﻿')).AsObject();
            wurzel["schemaVersion"] = stand;
            eintraege["manifest.json"] = new UTF8Encoding(false).GetBytes(
                wurzel.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            using var stream = new FileStream(ziel, FileMode.Create);
            using var neu = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (KeyValuePair<string, byte[]> kv in eintraege)
            {
                using var s = neu.CreateEntry(kv.Key, CompressionLevel.Optimal).Open();
                s.Write(kv.Value, 0, kv.Value.Length);
            }
        }
    }
}
