using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Vorlagen-Controller</b> (Konzept Berichtsvorlagen 10.2, 10.3; Etappe BV-E1, Teil A3):
    /// Vorlagenordner mit Vorgabe, Liste, Hinzufügen mit Namenskonflikt, Ersetzen, Neue Vorlage,
    /// Entfernen, Vorgabe und Abweichung, die Auflösungskette samt fehlender Datei, Ablagedatei,
    /// Sperrdatei, einmaliges Lesen und Ersteller.
    ///
    /// <para><b>Rahmen.</b> Alles in einem Temp-Ordner je Fall; Pfade und Einstellungen werden
    /// HEREINGEREICHT (<see cref="Probepfade"/>, <see cref="FluechtigeEinstellungen"/>) — kein
    /// <c>Dienste.*</c> wird getauscht, keine Datenbank berührt. Die Firma der Lizenz kommt aus
    /// einem Delegaten.</para>
    /// </summary>
    public class BerichtsvorlagenCtrlTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-a3-ctrl");
        private readonly string _dokumente;
        private readonly string _app;
        private readonly string _quellen;
        private readonly FluechtigeEinstellungen _einstellungen = new FluechtigeEinstellungen();
        private readonly Probepfade _pfade;
        private readonly BerichtsvorlagenCtrl _ctrl;
        private readonly byte[] _standard;

        public BerichtsvorlagenCtrlTests()
        {
            _dokumente = Directory.CreateDirectory(Path.Combine(_wurzel, "Dokumente")).FullName;
            _app = Directory.CreateDirectory(Path.Combine(_wurzel, "App", "Vorlagen")).FullName;
            _quellen = Directory.CreateDirectory(Path.Combine(_wurzel, "Quellen")).FullName;
            _standard = Probevorlagen.Baue(b => b.Absatz("{{bericht.inhalt}}").DeutscheUeberschriften());
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD), _standard);
            _pfade = new Probepfade(_dokumente, _app);
            _ctrl = new BerichtsvorlagenCtrl(_pfade, _einstellungen, () => "Lizenz GmbH");
        }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_wurzel);
        }

        /// <summary>Pfade, deren Dokumente- und Auslieferungsordner der Fall bestimmt.</summary>
        private sealed class Probepfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Probepfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }

        private string Vorgabeordner => Path.Combine(_dokumente, "EPOS-Plan", "Berichtsvorlagen");

        /// <summary>Eine Quelldatei außerhalb des Vorlagenordners.</summary>
        private string Quelle(string name, string text = "{{projekt.kunde}}", string ordner = null)
        {
            string pfad = Path.Combine(ordner ?? _quellen, name);
            File.WriteAllBytes(pfad, Probevorlagen.AusAbsaetzen(text));
            return pfad;
        }

        private Vorlageneintrag Hinzu(string name, string text = "{{projekt.kunde}}")
        {
            Vorlagenergebnis r = _ctrl.Hinzufuegen(Quelle(name, text));
            Assert.True(r.Erfolg, r.Meldung);
            return r.Eintrag;
        }

        private string Ablagetext => File.ReadAllText(Path.Combine(_ctrl.Vorlagenordner, BerichtsvorlagenCtrl.ABLAGEDATEI));

        // =====================================================================
        //  Vorlagenordner
        // =====================================================================

        [Fact]
        public void Vorlagenordner_hat_eine_Vorgabe_und_wird_nur_erreichbar_gesetzt()
        {
            Assert.Equal(Vorgabeordner, _ctrl.Vorgabeordner);
            Assert.Equal(Vorgabeordner, _ctrl.Vorlagenordner);
            Assert.True(_ctrl.IstVorgabeordner);

            Assert.Equal(Ordnerzustand.NichtErreichbar, _ctrl.PruefeOrdner().Zustand);
            Assert.False(Directory.Exists(Vorgabeordner));
            Ordnerbefund angelegt = _ctrl.PruefeOrdner(anlegen: true);
            Assert.Equal(Ordnerzustand.Angelegt, angelegt.Zustand);
            Assert.True(angelegt.Erfolg);
            Assert.True(Directory.Exists(Vorgabeordner));
            Assert.Equal(Ordnerzustand.Vorhanden, _ctrl.PruefeOrdner().Zustand);

            string fehlt = Path.Combine(_wurzel, "gibtsnicht");
            Ordnerbefund nicht = _ctrl.SetzeVorlagenordner(fehlt);
            Assert.Equal(Ordnerzustand.NichtErreichbar, nicht.Zustand);
            Assert.Equal("Der Vorlagenordner " + fehlt + " ist nicht erreichbar", nicht.Meldung);
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_ORDNER));
            Assert.Equal(Ordnerzustand.Ungueltig, _ctrl.SetzeVorlagenordner("relativ" + Path.DirectorySeparatorChar + "ordner").Zustand);
            if (OperatingSystem.IsWindows())
                Assert.Equal(Ordnerzustand.Ungueltig, _ctrl.SetzeVorlagenordner("C:ordner").Zustand);   // laufwerksrelativ
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_ORDNER));

            string buero = Directory.CreateDirectory(Path.Combine(_wurzel, "Buero")).FullName;
            Ordnerbefund gesetzt = _ctrl.SetzeVorlagenordner(buero);
            Assert.Equal(Ordnerzustand.Vorhanden, gesetzt.Zustand);
            Assert.Equal(buero, _einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_ORDNER));
            Assert.Equal(buero, _ctrl.Vorlagenordner);
            Assert.False(_ctrl.IstVorgabeordner);

            _ctrl.SetzeVorlagenordner(Vorgabeordner);
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_ORDNER));
            Assert.True(_ctrl.IstVorgabeordner);

            _ctrl.SetzeVorlagenordner(buero);
            _ctrl.SetzeVorlagenordner(null);
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_ORDNER));
        }

        [Fact]
        public void Ein_eingestellter_Ordner_wird_beim_Auflisten_nicht_angelegt()
        {
            string buero = Directory.CreateDirectory(Path.Combine(_wurzel, "Buero")).FullName;
            _ctrl.SetzeVorlagenordner(buero);
            Directory.Delete(buero);
            Assert.Single(_ctrl.Liste());
            Assert.False(Directory.Exists(buero));
            Assert.Equal(Ordnerzustand.NichtErreichbar, _ctrl.PruefeOrdner().Zustand);
            Assert.Equal(Vorlagenergebnisart.Erledigt, _ctrl.Hinzufuegen(Quelle("Neu.docx")).Art);   // beim Schreiben „bei Bedarf“
        }

        // =====================================================================
        //  Liste
        // =====================================================================

        [Fact]
        public void Liste_nennt_zuerst_die_Standardvorlage_dann_die_eigenen_sortiert()
        {
            IReadOnlyList<Vorlageneintrag> leer = _ctrl.Liste();
            Vorlageneintrag standard = Assert.Single(leer);
            Assert.True(Directory.Exists(Vorgabeordner), "Der Vorgabeordner wird beim Auflisten angelegt.");
            Assert.Equal(BerichtsvorlagenCtrl.ID_STANDARD, standard.Id);
            Assert.Equal("Standard (EPOS-Plan)", standard.Name);
            Assert.Equal(Vorlagenquelle.Mitgeliefert, standard.Quelle);
            Assert.True(standard.Schreibgeschuetzt);
            Assert.True(standard.Vorhanden);
            Assert.True(standard.IstStandard);
            Assert.Equal(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD), standard.Pfad);

            byte[] inhalt = Probevorlagen.AusAbsaetzen("{{projekt.kunde}}");
            foreach (string name in new[] { "b.docx", "A.dotx", "~$b.docx", ".versteckt.docx", "c.xlsx", "d.txt" })
                File.WriteAllBytes(Path.Combine(Vorgabeordner, name), inhalt);
            Directory.CreateDirectory(Path.Combine(Vorgabeordner, "Unter"));
            File.WriteAllBytes(Path.Combine(Vorgabeordner, "Unter", "u.docx"), inhalt);
            if (OperatingSystem.IsWindows())
            {
                string versteckt = Path.Combine(Vorgabeordner, "e.docx");
                File.WriteAllBytes(versteckt, inhalt);
                File.SetAttributes(versteckt, FileAttributes.Hidden);
            }

            IReadOnlyList<Vorlageneintrag> liste = _ctrl.Liste();
            Assert.Equal(new[] { "standard", "eigen:A.dotx", "eigen:b.docx" }, liste.Select(e => e.Id));
            Assert.Equal(new[] { "Standard (EPOS-Plan)", "A", "b" }, liste.Select(e => e.Name));
            Assert.All(liste.Skip(1), e =>
            {
                Assert.Equal(Vorlagenquelle.Eigen, e.Quelle);
                Assert.True(e.Vorhanden);
                Assert.False(e.Schreibgeschuetzt);
                Assert.Null(e.Pruefsumme);   // ohne Hinzufügen kein Eintrag in der Ablagedatei
            });
        }

        [Fact]
        public void Fehlt_die_Standardvorlage_bleibt_der_Eintrag_mit_benanntem_Rueckfall()
        {
            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD));
            Vorlageneintrag ohne = _ctrl.Standardeintrag();
            Assert.False(ohne.Vorhanden);
            Assert.Null(ohne.Rueckfallpfad);
            Assert.Null(_ctrl.LiesBytes(ohne));

            string rueckfall = Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_RUECKFALL);
            byte[] alt = Probevorlagen.AusAbsaetzen("Stilvorlage");
            File.WriteAllBytes(rueckfall, alt);
            Vorlageneintrag mit = _ctrl.Standardeintrag();
            Assert.False(mit.Vorhanden);
            Assert.Equal(rueckfall, mit.Rueckfallpfad);
            Assert.Equal(alt, _ctrl.LiesBytes(mit));
        }

        // =====================================================================
        //  Hinzufügen, Ersetzen, Neue Vorlage, Entfernen
        // =====================================================================

        [Fact]
        public void Hinzufuegen_kopiert_und_merkt_Herkunft_und_Pruefsumme()
        {
            string quelle = Quelle("Angebot.docx");
            Vorlagenergebnis r = _ctrl.Hinzufuegen(quelle);

            Assert.Equal(Vorlagenergebnisart.Erledigt, r.Art);
            Assert.Equal("„Angebot“ hinzugefügt", r.Meldung);
            Assert.Equal("eigen:Angebot.docx", r.Eintrag.Id);
            Assert.Equal(Path.Combine(Vorgabeordner, "Angebot.docx"), r.Eintrag.Pfad);
            Assert.Equal(File.ReadAllBytes(quelle), File.ReadAllBytes(r.Eintrag.Pfad));
            Assert.Equal(Path.GetFullPath(quelle), r.Eintrag.Herkunftspfad);
            Assert.Equal(Vorlagenpruefer.Pruefsumme(File.ReadAllBytes(quelle)), r.Eintrag.Pruefsumme);
            Assert.NotNull(r.Eintrag.Hinzugefuegt);

            using (JsonDocument ablage = JsonDocument.Parse(Ablagetext))
            {
                JsonElement eintrag = Assert.Single(ablage.RootElement.GetProperty("Vorlagen").EnumerateArray());
                Assert.Equal("Angebot.docx", eintrag.GetProperty("Datei").GetString());
                Assert.Equal(r.Eintrag.Pruefsumme, eintrag.GetProperty("Pruefsumme").GetString());
            }
            Vorlageneintrag gelistet = Assert.Single(_ctrl.Liste(), e => e.Id == "eigen:Angebot.docx");
            Assert.Equal(r.Eintrag.Pruefsumme, gelistet.Pruefsumme);

            // Eine Datei, die schon im Vorlagenordner liegt, wird nicht noch einmal kopiert.
            Vorlagenergebnis nochmal = _ctrl.Hinzufuegen(r.Eintrag.Pfad);
            Assert.Equal(Vorlagenergebnisart.Erledigt, nochmal.Art);
            Assert.Equal("„Angebot.docx“ liegt bereits im Vorlagenordner", nochmal.Meldung);
        }

        [Fact]
        public void Namenskonflikt_fuehrt_auf_Ersetzen_oder_neuen_Namen()
        {
            string erste = Quelle("Angebot.docx", "{{projekt.kunde}}");
            _ctrl.Hinzufuegen(erste);
            string zweite = Quelle("Angebot.docx", "{{projekt.name}}", Directory.CreateDirectory(Path.Combine(_quellen, "andere")).FullName);

            Vorlagenergebnis konflikt = _ctrl.Hinzufuegen(zweite);
            Assert.Equal(Vorlagenergebnisart.NameVergeben, konflikt.Art);
            Assert.Equal("Im Vorlagenordner gibt es schon „Angebot.docx“ – ersetzen oder unter neuem Namen hinzufügen?", konflikt.Meldung);
            Assert.Equal("eigen:Angebot.docx", konflikt.Vorhandener.Id);
            Assert.Equal(File.ReadAllBytes(erste), File.ReadAllBytes(konflikt.Vorhandener.Pfad));

            Vorlagenergebnis ersetzt = _ctrl.Ersetzen(zweite, konflikt.Vorhandener);
            Assert.Equal(Vorlagenergebnisart.Erledigt, ersetzt.Art);
            Assert.Equal("„Angebot“ ersetzt", ersetzt.Meldung);
            Assert.Equal(File.ReadAllBytes(zweite), File.ReadAllBytes(ersetzt.Eintrag.Pfad));
            Assert.Equal(Vorlagenpruefer.Pruefsumme(File.ReadAllBytes(zweite)), ersetzt.Eintrag.Pruefsumme);
            Assert.Equal(Path.GetFullPath(zweite), ersetzt.Eintrag.Herkunftspfad);

            Vorlagenergebnis als = _ctrl.HinzufuegenAls(zweite, "Angebot 2025");
            Assert.Equal(Vorlagenergebnisart.Erledigt, als.Art);
            Assert.Equal("Angebot 2025.docx", als.Eintrag.Dateiname);
            Assert.Equal(Vorlagenergebnisart.NameVergeben, _ctrl.HinzufuegenAls(zweite, "Angebot 2025.docx").Art);
            foreach (string schlecht in new[] { "a/b", "a\\b", "~$x", ".x", "CON", "", "   ", "a?" })
                Assert.Equal(Vorlagenergebnisart.NameUngueltig, _ctrl.HinzufuegenAls(zweite, schlecht).Art);

            Vorlagenergebnis endung = _ctrl.Ersetzen(Quelle("Andere.dotx"), ersetzt.Eintrag);
            Assert.Equal(Vorlagenergebnisart.FormatAbgelehnt, endung.Art);
            Assert.Equal("Die Endung muss gleich bleiben: .docx", endung.Meldung);
            Assert.Equal(Vorlagenergebnisart.Schreibgeschuetzt, _ctrl.Ersetzen(zweite, _ctrl.Standardeintrag()).Art);
        }

        [Fact]
        public void Nur_docx_und_dotx_werden_hinzugefuegt()
        {
            Vorlagenergebnis docm = _ctrl.Hinzufuegen(Quelle("Makro.docm"));
            Assert.Equal(Vorlagenergebnisart.FormatAbgelehnt, docm.Art);
            Assert.Equal("Nur Word-Dokumente (.docx) und Word-Vorlagen (.dotx) – „Makro.docm“ in Word als .docx speichern", docm.Meldung);
            Assert.Equal(Vorlagenergebnisart.QuelleFehlt, _ctrl.Hinzufuegen(Path.Combine(_quellen, "fehlt.docx")).Art);
            Assert.Equal(Vorlagenergebnisart.QuelleFehlt, _ctrl.Hinzufuegen(null).Art);
            Assert.Equal(Vorlagenergebnisart.Erledigt, _ctrl.Hinzufuegen(Quelle("Vorlage.dotx")).Art);
        }

        [Fact]
        public void Neue_Vorlage_kopiert_die_Standardvorlage()
        {
            Vorlagenergebnis neu = _ctrl.NeueVorlage("Kurzbericht");
            Assert.Equal(Vorlagenergebnisart.Erledigt, neu.Art);
            Assert.Equal("Neue Vorlage „Kurzbericht“ aus der Standardvorlage angelegt", neu.Meldung);
            Assert.Equal("Kurzbericht.docx", neu.Eintrag.Dateiname);
            Assert.Equal(_standard, File.ReadAllBytes(neu.Eintrag.Pfad));
            Assert.Null(neu.Eintrag.Herkunftspfad);
            Assert.Equal(Vorlagenpruefer.Pruefsumme(_standard), neu.Eintrag.Pruefsumme);
            Assert.False(neu.Eintrag.Schreibgeschuetzt);
            Assert.Null(_ctrl.OriginalGeaendert(neu.Eintrag));

            Assert.Equal(Vorlagenergebnisart.NameVergeben, _ctrl.NeueVorlage("Kurzbericht.docx").Art);
            Assert.Equal(Vorlagenergebnisart.NameUngueltig, _ctrl.NeueVorlage(" ").Art);

            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD));
            Assert.Equal(Vorlagenergebnisart.QuelleFehlt, _ctrl.NeueVorlage("Ohne").Art);
        }

        /// <summary>
        /// „Neue Vorlage…“ aus dem Kurzbericht (Konzept 10.2, 6.3 Nr. 2): die Kopie des mitgelieferten Kurzberichts der
        /// Sprache aus dem Ordner der mitgelieferten Vorlagen — auf Deutsch <c>Berichtsvorlage_Kurzbericht.docx</c>, auf
        /// Englisch <c>…_en.docx</c>; ohne Rückfall auf die Standardvorlage, eine fehlende Datei nennt das Ergebnis. Der
        /// Kurzbericht selbst steht nicht in der Liste — er ist nur als Kopie wählbar.
        /// </summary>
        [Fact]
        public void Neue_Vorlage_aus_dem_Kurzbericht_kopiert_den_Kurzbericht_der_Sprache()
        {
            byte[] deutsch = Encoding.UTF8.GetBytes("kurzbericht de");
            byte[] englisch = Encoding.UTF8.GetBytes("kurzbericht en");
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT), deutsch);
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN), englisch);

            Assert.Equal(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT), _ctrl.Musterpfad(Vorlagenmuster.Kurzbericht, false));
            Assert.Equal(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD), _ctrl.Musterpfad(Vorlagenmuster.Standard, true));
            Assert.Equal(BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN, BerichtsvorlagenCtrl.DateiKurzbericht(true));

            Vorlagenergebnis de = _ctrl.NeueVorlage("Angebot kurz", Vorlagenmuster.Kurzbericht, false);
            Assert.Equal(Vorlagenergebnisart.Erledigt, de.Art);
            Assert.Equal("Neue Vorlage „Angebot kurz“ aus dem Kurzbericht angelegt – die Erläuterungen stehen als Kommentare in Word",
                         de.Meldung);
            Assert.Equal(deutsch, File.ReadAllBytes(de.Eintrag.Pfad));
            Assert.Equal(englisch, File.ReadAllBytes(_ctrl.NeueVorlage("Offer short", Vorlagenmuster.Kurzbericht, true).Eintrag.Pfad));
            Assert.Equal(_standard, File.ReadAllBytes(_ctrl.NeueVorlage("Voll", Vorlagenmuster.Standard, true).Eintrag.Pfad));
            Assert.DoesNotContain(_ctrl.Liste(), e => e.Dateiname.StartsWith("Berichtsvorlage_Kurzbericht", StringComparison.Ordinal));

            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN));
            Assert.Null(_ctrl.Musterpfad(Vorlagenmuster.Kurzbericht, true));
            Vorlagenergebnis fehlt = _ctrl.NeueVorlage("Ohne", Vorlagenmuster.Kurzbericht, true);
            Assert.Equal(Vorlagenergebnisart.QuelleFehlt, fehlt.Art);
            Assert.Contains(BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN, fehlt.Meldung, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(Vorgabeordner, "Ohne.docx")));
        }

        [Fact]
        public void Entfernen_verschiebt_in_Entfernt_und_setzt_die_Vorgabe_zurueck()
        {
            Vorlageneintrag alt = Hinzu("Alt.docx");
            Hinzu("Bleibt.docx");
            _ctrl.SetzeVorgabeWord(alt);

            Vorlagenergebnis r = _ctrl.Entfernen(alt);
            string ablage = Path.Combine(Vorgabeordner, BerichtsvorlagenCtrl.ORDNER_ENTFERNT);
            Assert.Equal(Vorlagenergebnisart.Erledigt, r.Art);
            Assert.Equal("„Alt“ entfernt – die Datei liegt jetzt in " + ablage, r.Meldung);
            Assert.False(File.Exists(alt.Pfad));
            Assert.Equal(Path.Combine(ablage, "Alt.docx"), r.Zielpfad);
            Assert.True(File.Exists(r.Zielpfad));
            Assert.DoesNotContain(_ctrl.Liste(), e => e.Id == alt.Id);
            Assert.Equal(BerichtsvorlagenCtrl.ID_STANDARD, _ctrl.VorgabeWordId);
            Assert.DoesNotContain("Alt.docx", Ablagetext);
            Assert.Contains("Bleibt.docx", Ablagetext);

            Vorlageneintrag nochmal = Hinzu("Alt.docx");
            Assert.Equal(Path.Combine(ablage, "Alt (2).docx"), _ctrl.Entfernen(nochmal).Zielpfad);

            Assert.Equal(Vorlagenergebnisart.Schreibgeschuetzt, _ctrl.Entfernen(_ctrl.Standardeintrag()).Art);
            Assert.True(File.Exists(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD)));
            Assert.Equal(Vorlagenergebnisart.QuelleFehlt, _ctrl.Entfernen(nochmal).Art);
        }

        // =====================================================================
        //  Vorgabe, Abweichung, Auflösung
        // =====================================================================

        [Fact]
        public void Vorgabe_steht_als_Kennung_in_der_Einstellung()
        {
            Assert.Equal(BerichtsvorlagenCtrl.ID_STANDARD, _ctrl.VorgabeWordId);
            Vorlageneintrag mein = Hinzu("Mein.docx");
            _ctrl.SetzeVorgabeWord(mein);
            Assert.Equal("eigen:Mein.docx", _einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_VORGABE_WORD));
            Assert.Equal("eigen:Mein.docx", _ctrl.VorgabeWordId);
            _ctrl.SetzeVorgabeWord((string)null);
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_VORGABE_WORD));
            Assert.Equal(BerichtsvorlagenCtrl.ID_STANDARD, _ctrl.VorgabeWordId);
        }

        [Fact]
        public void Abweichung_steht_in_der_Konfiguration_des_Stammprojekts()
        {
            var konfig = BerichtsKonfiguration.Standard();
            Vorlageneintrag mein = Hinzu("Mein.docx");

            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, mein);
            Assert.Equal(BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN, konfig.VorlageWordQuelle);
            Assert.Equal("Mein.docx", konfig.VorlageWordDatei);
            Assert.Equal("eigen:Mein.docx", BerichtsvorlagenCtrl.AbweichungId(konfig));

            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, _ctrl.Standardeintrag());
            Assert.Equal(BerichtsKonfiguration.VORLAGE_QUELLE_STANDARD, konfig.VorlageWordQuelle);
            Assert.Null(konfig.VorlageWordDatei);
            Assert.Equal(BerichtsvorlagenCtrl.ID_STANDARD, BerichtsvorlagenCtrl.AbweichungId(konfig));

            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, null);
            Assert.Null(konfig.VorlageWordQuelle);
            Assert.Null(konfig.VorlageWordDatei);
            Assert.Null(BerichtsvorlagenCtrl.AbweichungId(konfig));

            konfig.VorlageWordQuelle = "irgendwas";
            Assert.Null(BerichtsvorlagenCtrl.AbweichungId(konfig));
            konfig.VorlageWordQuelle = "EIGEN";
            konfig.VorlageWordDatei = " Mein.docx ";
            Assert.Equal("eigen:Mein.docx", BerichtsvorlagenCtrl.AbweichungId(konfig));
            konfig.VorlageWordDatei = null;
            Assert.Null(BerichtsvorlagenCtrl.AbweichungId(konfig));
        }

        [Fact]
        public void VorlageFuer_Abweichung_Vorgabe_Standard_Rueckfall_mit_fehlenden_Dateien()
        {
            var konfig = BerichtsKonfiguration.Standard();

            Vorlagenwahl standard = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Standard, standard.Grund);
            Assert.Equal("Standardvorlage", standard.GrundText);
            Assert.True(standard.Eintrag.IstStandard);
            Assert.Empty(standard.Meldungen);
            Assert.Null(standard.FehlendeId);

            Vorlageneintrag vorgabe = Hinzu("Vorgabe.docx");
            _ctrl.SetzeVorgabeWord(vorgabe);
            Vorlagenwahl mitVorgabe = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Vorgabe, mitVorgabe.Grund);
            Assert.Equal("Ihre Vorgabe", mitVorgabe.GrundText);
            Assert.Equal("eigen:Vorgabe.docx", mitVorgabe.Eintrag.Id);

            Vorlageneintrag projekt = Hinzu("Projekt.docx");
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, projekt);
            Vorlagenwahl abweichung = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Abweichung, abweichung.Grund);
            Assert.Equal("Für dieses Projekt gewählt", abweichung.GrundText);
            Assert.Equal("eigen:Projekt.docx", abweichung.Eintrag.Id);
            Assert.Empty(abweichung.Meldungen);

            File.Delete(projekt.Pfad);
            Vorlagenwahl ohneProjekt = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Vorgabe, ohneProjekt.Grund);
            Assert.Equal("„Projekt“ nicht vorhanden – „Vorgabe“ verwendet", Assert.Single(ohneProjekt.Meldungen));
            Assert.Equal("eigen:Projekt.docx", ohneProjekt.FehlendeId);

            File.Delete(vorgabe.Pfad);
            Vorlagenwahl nurStandard = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Standard, nurStandard.Grund);
            Assert.True(nurStandard.Eintrag.IstStandard);
            Assert.Equal(new[]
            {
                "„Projekt“ nicht vorhanden – „Standard (EPOS-Plan)“ verwendet",
                "„Vorgabe“ nicht vorhanden – „Standard (EPOS-Plan)“ verwendet",
            }, nurStandard.Meldungen);
            Assert.Equal("eigen:Projekt.docx", nurStandard.FehlendeId);
            Assert.Equal(string.Join(" ", nurStandard.Meldungen), nurStandard.Meldung);

            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD));
            Vorlagenwahl rueckfall = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Rueckfall, rueckfall.Grund);
            Assert.False(rueckfall.Eintrag.Vorhanden);
            Assert.Equal("Die Standardvorlage Berichtsvorlage_Standard.docx fehlt – der Bericht entsteht mit den eingebauten Formaten",
                         rueckfall.Meldungen.Last());
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_RUECKFALL), _standard);
            Assert.Equal("Die Standardvorlage Berichtsvorlage_Standard.docx fehlt – verwendet wird Berichtsvorlage.docx",
                         _ctrl.VorlageFuer(konfig).Meldungen.Last());
        }

        [Fact]
        public void Ausdrueckliche_Abweichung_Standard_schlaegt_die_Vorgabe()
        {
            _ctrl.SetzeVorgabeWord(Hinzu("Vorgabe.docx"));
            var konfig = BerichtsKonfiguration.Standard();
            BerichtsvorlagenCtrl.SetzeAbweichung(konfig, _ctrl.Standardeintrag());
            Vorlagenwahl wahl = _ctrl.VorlageFuer(konfig);
            Assert.Equal(Vorlagenwahlgrund.Abweichung, wahl.Grund);
            Assert.True(wahl.Eintrag.IstStandard);
        }

        [Fact]
        public void Finde_nimmt_nur_einfache_Dateinamen_im_Vorlagenordner()
        {
            Hinzu("Gross.docx");
            Assert.True(_ctrl.Finde("standard").IstStandard);
            Assert.Equal("eigen:Gross.docx", _ctrl.Finde("eigen:Gross.docx").Id);
            Assert.Null(_ctrl.Finde("eigen:.." + Path.DirectorySeparatorChar + "Gross.docx"));
            Assert.Null(_ctrl.Finde("eigen:../Gross.docx"));
            Assert.Null(_ctrl.Finde("eigen:gibtsnicht.docx"));
            Assert.Null(_ctrl.Finde("fremd"));
            Assert.Null(_ctrl.Finde(null));
            if (!OperatingSystem.IsLinux()) Assert.Equal("eigen:Gross.docx", _ctrl.Finde("eigen:gross.docx").Id);
        }

        // =====================================================================
        //  Lesen, Sperrdatei, Prüfen
        // =====================================================================

        [Fact]
        public void LiesBytes_liest_einmal_und_geteilt_auch_waehrend_Word_die_Datei_haelt()
        {
            Vorlageneintrag e = Hinzu("Offen.docx");
            byte[] erwartet = File.ReadAllBytes(e.Pfad);
            using (new FileStream(e.Pfad, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                Assert.Equal(erwartet, _ctrl.LiesBytes(e));
            Assert.Null(_ctrl.LiesBytes(null));

            File.Delete(e.Pfad);
            Assert.Null(_ctrl.LiesBytes(e));
        }

        [Fact]
        public void Sperrdatei_heisst_in_Word_geoeffnet()
        {
            Assert.Equal(new[] { "~$Angebot.docx", "~$ngebot.docx", "~$gebot.docx" }, BerichtsvorlagenCtrl.Sperrdateinamen("Angebot.docx"));

            Vorlageneintrag e = Hinzu("Angebot.docx");
            Assert.False(_ctrl.IstInWordGeoeffnet(e));
            string gekuerzt = Path.Combine(Vorgabeordner, "~$gebot.docx");
            File.WriteAllText(gekuerzt, "Sperre");
            Assert.True(_ctrl.IstInWordGeoeffnet(e));
            File.Delete(gekuerzt);
            string voll = Path.Combine(Vorgabeordner, "~$Angebot.docx");
            File.WriteAllText(voll, "Sperre");
            Assert.True(_ctrl.IstInWordGeoeffnet(e));
            Assert.DoesNotContain(_ctrl.Liste(), x => x.Dateiname.StartsWith("~$", StringComparison.Ordinal));

            Pruefbefund befund = _ctrl.Pruefe(e, Pruefstufe.Schnell, new Pruefkontext());
            Pruefmeldung w = Assert.Single(befund.Meldungen);
            Assert.Equal("BV_VORLAGEN_IN_WORD", w.Kennung);
            Assert.Equal(Befundstufe.Warnung, w.Stufe);
            Assert.Equal("In Word geöffnet – ungespeicherte Änderungen fehlen", w.Text);
            Assert.Equal("Angebot", w.Fundort);

            Assert.Equal(Vorlagenergebnisart.InWordGeoeffnet, _ctrl.Ersetzen(Quelle("Angebot.docx"), e).Art);
            Assert.Equal(Vorlagenergebnisart.InWordGeoeffnet, _ctrl.Entfernen(e).Art);
            Assert.True(File.Exists(e.Pfad));
        }

        [Fact]
        public void Pruefe_liest_die_Vorlage_und_benennt_eine_fehlende()
        {
            Vorlageneintrag standard = _ctrl.Standardeintrag();
            Pruefbefund voll = _ctrl.Pruefe(standard, Pruefstufe.Voll, new Pruefkontext());
            Assert.True(voll.OhneBefund, Probevorlagen.Liste(voll));
            Assert.Equal(Vorlagenpruefer.Pruefsumme(_standard), voll.Pruefsumme);
            Assert.Equal(1, voll.AnzahlPlatzhalter);

            byte[] bytes = _ctrl.LiesBytes(standard);
            Assert.Equal(voll.Pruefsumme, _ctrl.Pruefe(bytes, standard, Pruefstufe.Schnell, null).Pruefsumme);

            Vorlageneintrag fremd = Hinzu("Unbekannt.docx", "{{projekt.gibtsnicht}}");
            Assert.Equal("VF_PRUEF_UNBEKANNT", Assert.Single(_ctrl.Pruefe(fremd, Pruefstufe.Schnell, null).Meldungen).Kennung);

            File.Delete(standard.Pfad);
            Pruefbefund fehlt = _ctrl.Pruefe(_ctrl.Standardeintrag(), Pruefstufe.Schnell, new Pruefkontext());
            Pruefmeldung m = Assert.Single(fehlt.Meldungen);
            Assert.Equal("BV_VORLAGEN_FEHLT", m.Kennung);
            Assert.Equal("Die Vorlage „Standard (EPOS-Plan)“ ist nicht vorhanden", m.Text);
            Assert.False(fehlt.IstLesbar);
            Assert.Equal("The template “Standard (EPOS-Plan)” does not exist",
                         Assert.Single(_ctrl.Pruefe(_ctrl.Standardeintrag(), Pruefstufe.Schnell, new Pruefkontext { Englisch = true }).Meldungen).Text);
        }

        // =====================================================================
        //  Ablagedatei, Original
        // =====================================================================

        [Fact]
        public void Ablagedatei_ist_duldsam_und_raeumt_verwaiste_Eintraege()
        {
            Vorlageneintrag a = Hinzu("A.docx");
            Vorlageneintrag b = Hinzu("B.docx");
            File.WriteAllText(Path.Combine(Vorgabeordner, BerichtsvorlagenCtrl.ABLAGEDATEI), "{ kaputt");
            IReadOnlyList<Vorlageneintrag> liste = _ctrl.Liste();
            Assert.Equal(3, liste.Count);
            Assert.All(liste.Skip(1), e => Assert.Null(e.Pruefsumme));

            Hinzu("C.docx");
            File.Delete(b.Pfad);
            Hinzu("D.docx");
            using (JsonDocument ablage = JsonDocument.Parse(Ablagetext))
                Assert.Equal(new[] { "C.docx", "D.docx" },
                             ablage.RootElement.GetProperty("Vorlagen").EnumerateArray().Select(v => v.GetProperty("Datei").GetString()));
            Assert.True(File.Exists(a.Pfad));
        }

        [Fact]
        public void Original_geaendert_vergleicht_die_Herkunft_mit_der_gemerkten_Pruefsumme()
        {
            string quelle = Quelle("Orig.docx");
            Vorlageneintrag e = _ctrl.Hinzufuegen(quelle).Eintrag;
            Assert.False(_ctrl.OriginalGeaendert(e));
            File.WriteAllBytes(quelle, Probevorlagen.AusAbsaetzen("{{projekt.name}}"));
            Assert.True(_ctrl.OriginalGeaendert(_ctrl.Finde(e.Id)));
            File.Delete(quelle);
            Assert.Null(_ctrl.OriginalGeaendert(_ctrl.Finde(e.Id)));
        }

        // =====================================================================
        //  Ersteller
        // =====================================================================

        [Fact]
        public void Ersteller_nimmt_die_Einstellung_sonst_die_Firma_der_Lizenz()
        {
            Erstellerangaben lizenz = _ctrl.Ersteller();
            Assert.Equal("Lizenz GmbH", lizenz.Firma);
            Assert.Null(lizenz.Programm);
            Assert.Null(lizenz.Version);
            Assert.Equal("Lizenz GmbH", _ctrl.FirmaAusLizenz());

            _ctrl.SchreibeFirma("  Ingenieurbüro Muster  ");
            Assert.Equal("Ingenieurbüro Muster", _einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_FIRMA));
            Assert.Equal("Ingenieurbüro Muster", _ctrl.Ersteller().Firma);

            _ctrl.SchreibeFirma("");
            Assert.Null(_ctrl.Ersteller().Firma);   // ausdrücklich leer — kein Rückfall auf die Lizenz

            _ctrl.SchreibeFirma(null);
            Assert.Equal("Lizenz GmbH", _ctrl.Ersteller().Firma);

            var ohneLizenz = new BerichtsvorlagenCtrl(_pfade, _einstellungen, () => throw new InvalidOperationException("keine Lizenz"));
            Assert.Null(ohneLizenz.Ersteller().Firma);
            Assert.Null(new BerichtsvorlagenCtrl(_pfade, _einstellungen, () => "  ").Ersteller().Firma);
        }

        // =====================================================================
        //  Logo (BV-E2-1: Platzhalterbild bild.ersteller.logo)
        // =====================================================================

        /// <summary>
        /// Das Logo steht als Pfad in der Einstellung „BerichtLogo“; <see cref="BerichtsvorlagenCtrl.Ersteller"/>
        /// lädt die Datei EINMAL (PNG oder JPEG). Leer oder <c>null</c> entfernt die Einstellung — dann gibt es
        /// weder Logo noch Warnung.
        /// </summary>
        [Fact]
        public void Logo_steht_als_Pfad_in_der_Einstellung_und_wird_einmal_geladen()
        {
            Assert.Equal("BerichtLogo", BerichtsvorlagenCtrl.EINSTELLUNG_LOGO);
            Assert.Equal(5L * 1024 * 1024, BerichtsvorlagenCtrl.GRENZE_LOGO);
            Assert.Null(_ctrl.LogoPfad);
            Assert.False(_ctrl.LogoVorhanden());
            Erstellerangaben ohne = _ctrl.Ersteller();
            Assert.Null(ohne.Logo);
            Assert.Null(ohne.LogoDateiname);
            Assert.Null(ohne.LogoWarnung);

            byte[] png = WordVorlagenfuellerTests.Png(40, 20);
            string pfad = Path.Combine(_quellen, "Firmenlogo.png");
            File.WriteAllBytes(pfad, png);
            _ctrl.SchreibeLogo("  " + pfad + "  ");
            Assert.Equal(pfad, _einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_LOGO));
            Assert.Equal(pfad, _ctrl.LogoPfad);
            Assert.True(_ctrl.LogoVorhanden());

            Erstellerangaben mit = _ctrl.Ersteller();
            Assert.Equal(png, mit.Logo);
            Assert.Equal("Firmenlogo.png", mit.LogoDateiname);
            Assert.Null(mit.LogoWarnung);
            Assert.Equal("Lizenz GmbH", mit.Firma);

            // Die Erstellerangaben tragen die geladenen Bytes — die Datei darf danach verschwinden.
            File.Delete(pfad);
            Assert.Equal(png, mit.Logo);
            Bildinhalt bild = Bildinhalt.Aus(mit.Logo, mit.LogoDateiname);
            Assert.Equal(Bildformat.Png, bild.Format);
            Assert.Equal(40, bild.Breite);
            Assert.Equal(20, bild.Hoehe);

            string jpeg = Path.Combine(_quellen, "Logo.jpg");
            File.WriteAllBytes(jpeg, WordVorlagenfuellerTests.Jpeg(30, 60));
            _ctrl.SchreibeLogo(jpeg);
            Assert.Equal("Logo.jpg", _ctrl.Ersteller().LogoDateiname);
            Assert.Equal(Bildformat.Jpeg, Bildinhalt.Aus(_ctrl.Ersteller().Logo, "Logo.jpg").Format);

            _ctrl.SchreibeLogo("");
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_LOGO));
            Assert.Null(_ctrl.LogoPfad);
            _ctrl.SchreibeLogo(jpeg);
            _ctrl.SchreibeLogo(null);
            Assert.Null(_ctrl.LogoPfad);
            Assert.Null(_ctrl.Ersteller().Logo);
            Assert.Null(_ctrl.Ersteller().LogoWarnung);
        }

        /// <summary>
        /// Fehlt die Datei, ist sie zu groß (über 5 MB) oder kein PNG/JPEG, bleibt das Logo <c>null</c>; die
        /// Warnung nennt den Grund und den Pfad — der Lauf warnt damit einmal („Logo nicht gefunden: …“).
        /// </summary>
        [Fact]
        public void Fehlendes_zu_grosses_oder_fremdes_Logo_bleibt_leer_und_nennt_den_Grund()
        {
            string fehlt = Path.Combine(_quellen, "gibt-es-nicht.png");
            _ctrl.SchreibeLogo(fehlt);
            Erstellerangaben a = _ctrl.Ersteller();
            Assert.Null(a.Logo);
            Assert.Null(a.LogoDateiname);
            Assert.Equal("Logo nicht gefunden: " + fehlt, a.LogoWarnung);
            Assert.False(_ctrl.LogoVorhanden());
            Assert.Equal("Lizenz GmbH", a.Firma);   // die übrigen Angaben bleiben

            string gross = Path.Combine(_quellen, "riesig.png");
            byte[] kopf = WordVorlagenfuellerTests.Png(2, 2);
            using (var s = new FileStream(gross, FileMode.Create, FileAccess.Write))
            {
                s.Write(kopf, 0, kopf.Length);
                s.SetLength(BerichtsvorlagenCtrl.GRENZE_LOGO + 1);
            }
            _ctrl.SchreibeLogo(gross);
            a = _ctrl.Ersteller();
            Assert.Null(a.Logo);
            Assert.StartsWith("Logo zu groß: " + gross + " (", a.LogoWarnung);
            Assert.EndsWith("MB, höchstens 5 MB)", a.LogoWarnung);
            Assert.False(_ctrl.LogoVorhanden());

            // Genau 5 MB sind erlaubt — die Grenze zählt die Bytes der Datei.
            using (var s = new FileStream(gross, FileMode.Open, FileAccess.Write)) s.SetLength(BerichtsvorlagenCtrl.GRENZE_LOGO);
            Assert.True(_ctrl.LogoVorhanden());

            string fremd = Path.Combine(_quellen, "logo.png");
            File.WriteAllText(fremd, "kein Bild");
            _ctrl.SchreibeLogo(fremd);
            a = _ctrl.Ersteller();
            Assert.Null(a.Logo);
            Assert.Equal("Logo ist kein PNG- oder JPEG-Bild: " + fremd, a.LogoWarnung);
            Assert.False(_ctrl.LogoVorhanden());

            // Ein Ordner statt einer Datei zählt als „nicht gefunden“.
            _ctrl.SchreibeLogo(_quellen);
            Assert.Equal("Logo nicht gefunden: " + _quellen, _ctrl.Ersteller().LogoWarnung);
        }
    }
}
