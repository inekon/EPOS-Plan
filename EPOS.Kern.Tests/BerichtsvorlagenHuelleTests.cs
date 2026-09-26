using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Seiten.Berichte;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;
using Startweg = WindowsFormsApplication1.Startweg;
using UiStartweg = EPOS.UI.Seiten.Berichte.Startweg;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Berichtsvorlagen</b> (Etappe BV-E1, Teil B1a-Hülle; Konzept Berichtsvorlagen
    /// 10.2, 10.3): der Parametersatz der Gruppe „Vorlage" gegen die Parameter der Seite, die Wahl
    /// als Abweichung des Stammprojekts, der gesperrte Eintrag einer fehlenden Vorlage, das Menü
    /// „…" je Plattform und Quelle, „Hinzufügen…" bei Namensgleichheit, „Neue Vorlage…" mit
    /// Namensprüfung, „Ersetzen…" und „Entfernen", Prüfzeile, Prüfliste und Platzhalterkatalog,
    /// die erweiterte Startrückfrage mit ihrem Weg, der Lauf mit Laufmeldung und der Abschnitt
    /// „Bericht" der Programmeinstellungen samt dem Fall ohne Ordnerwahl (iOS).
    ///
    /// <para><b>Rahmen.</b> Pfade und Einstellungen werden hereingereicht wie in
    /// <see cref="BerichtCtrlVorlagenTests"/>: die Standardvorlage als Kopie aus dem Repositorium in
    /// einem Temp-Ordner, der als Auslieferungsordner gilt; der Vorlagenordner ist der Vorgabeordner
    /// darunter. Die Konfiguration der Gruppe 1040 liegt in einer Kopie der Testdatenbank. Getauscht
    /// werden allein <c>Dienste.Datei</c> und <c>Dienste.Dialog</c> (Dateiwahl, Teilen, Meldung) —
    /// und im Dispose zurückgesetzt. Der Lauf sammelt ohne Simulation (<see cref="BerichtSeiteGaben.Sammler"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BerichtsvorlagenHuelleTests : IDisposable
    {
        /// <summary>Das Stammprojekt der Prüfgruppe (Versionen 1041, 1042).</summary>
        private const int GRUPPE = 1040;

        private const string FIRMA = "Probe GmbH";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-huelle");
        private readonly string _dokumente;
        private readonly string _app;
        private readonly string _quellen;
        private readonly string _ziel;
        private readonly FluechtigeEinstellungen _einstellungen = new FluechtigeEinstellungen();
        private readonly BerichtsvorlagenCtrl _vorlagen;
        private readonly byte[] _standard;
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly IDialogDienst _dialogVorher = Dienste.Dialog;
        private readonly Berichtsvorlagenwege _wegeVorher = Berichtsvorlagenwege.Plattform;

        public BerichtsvorlagenHuelleTests()
        {
            _dokumente = Directory.CreateDirectory(Path.Combine(_wurzel, "Dokumente")).FullName;
            _app = Directory.CreateDirectory(Path.Combine(_wurzel, "App", "Vorlagen")).FullName;
            _quellen = Directory.CreateDirectory(Path.Combine(_wurzel, "Quellen")).FullName;
            _ziel = Directory.CreateDirectory(Path.Combine(_wurzel, "Berichte")).FullName;
            _standard = Repovorlage(BerichtsvorlageDateiWacheTests.STANDARD);
            if (_standard != null) File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD), _standard);
            _vorlagen = new BerichtsvorlagenCtrl(new Probepfade(_dokumente, _app), _einstellungen, () => FIRMA);
        }

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            Dienste.Dialog = _dialogVorher;
            Berichtsvorlagenwege.Plattform = _wegeVorher;
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_wurzel);
        }

        // =====================================================================
        //  Der Parametersatz
        // =====================================================================

        /// <summary>
        /// Jeder Schlüssel des Satzes ist ein <c>[Parameter]</c> der Seite mit passendem Typ, und die
        /// Gruppe „Vorlage" ist vollständig belegt. Mit der Standardvorlage allein: ein Eintrag mit
        /// Schloss, gewählt, das Menü „Schreibgeschützt öffnen", die Prüfzeile „geprüft, …" — und keine
        /// erweiterte Rückfrage.
        /// </summary>
        [Fact]
        public void Gaben_belegen_die_Gruppe_Vorlage_mit_den_Parametern_der_Seite()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();

            var seite = new BerichtSeiteGaben(GRUPPE, "Stamm", _vorlagen, new Wegeprobe().Wege());
            IReadOnlyDictionary<string, object> gaben = seite.Gaben();

            PasstZu(typeof(BerichtSeite), gaben);
            foreach (string k in new[]
                     {
                         "Vorlagen", "VorlageId", "VorlageIdChanged", "Vorlagenhandlungen", "HandlungGewaehlt",
                         "NeueVorlage", "VorlagennamePruefen", "Hinzufuegen", "Pruefen", "PlatzhalterkatalogGaben",
                         "Pruefzeile", "PrueflisteGaben", "StartGewaehlt", "VorlagenNeuLaden", "Vorlagentexte"
                     })
                Assert.True(gaben.ContainsKey(k), "Es fehlt " + k);
            Assert.False(gaben.ContainsKey("Startrueckfrage"));

            var vorlagen = (IReadOnlyList<Vorlagenzeile>)gaben["Vorlagen"];
            Vorlagenzeile standard = Assert.Single(vorlagen);
            Assert.Equal(R.BV_VORLAGEN_STANDARD, standard.Text);
            Assert.True(standard.Mitgeliefert);
            Assert.False(standard.Gesperrt);
            Assert.Equal(standard.Id, (int)gaben["VorlageId"]);

            var handlungen = (IReadOnlyList<Handlung>)gaben["Vorlagenhandlungen"];
            Assert.Equal(new[] { BerichtsvorlagenGaben.HANDLUNG_SCHREIBGESCHUETZT }, handlungen.Select(h => h.Id));
            Assert.StartsWith("geprüft, ", ((Pruefstand)gaben["Pruefzeile"]).Text, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Die Wahl
        // =====================================================================

        /// <summary>
        /// Der Wechsel auf eine eigene Vorlage speichert sie als Abweichung des Stammprojekts — die
        /// übrige Konfiguration bleibt; die Wahl der Vorgabe (hier die Standardvorlage) nimmt die
        /// Abweichung zurück. Eine Id, die die Hülle nicht vergeben hat, wird benannt abgelehnt; die
        /// Ids bleiben über jedes Nachladen dieselben.
        /// </summary>
        [Fact]
        public async Task Der_Vorlagenwechsel_speichert_die_Abweichung_und_die_Vorgabe_entfernt_sie()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Angebot.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kunde}}"));
            BerichtsvorlagenGaben gruppe = Gruppe(new Wegeprobe().Wege());

            Vorlagenstand stand = gruppe.Stand();
            int idStandard = Id(stand, R.BV_VORLAGEN_STANDARD);
            int idAngebot = Id(stand, "Angebot");
            Assert.Equal(idStandard, stand.VorlageId);

            await gruppe.VorlageGewaehlt(idAngebot);
            BerichtsKonfiguration k = Lade();
            Assert.Equal(BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN, k.VorlageWordQuelle);
            Assert.Equal("Angebot.docx", k.VorlageWordDatei);
            Assert.Equal(_ziel, k.ZielOrdner);
            Assert.Equal(new[] { BerichtsKonfiguration.B_DECKBLATT }, k.AktiveBausteine);
            stand = gruppe.Stand();
            Assert.Equal(idAngebot, stand.VorlageId);
            Assert.Equal("", stand.Fehler);

            await gruppe.VorlageGewaehlt(idStandard);
            k = Lade();
            Assert.Null(k.VorlageWordQuelle);
            Assert.Null(k.VorlageWordDatei);
            Assert.Equal(idStandard, gruppe.Stand().VorlageId);

            await gruppe.VorlageGewaehlt(999);
            Assert.Equal(R.BK_BER_VORLAGE_MSG_UNBEKANNT, gruppe.Stand().Fehler);
            Assert.Equal("", gruppe.Stand().Fehler);   // einmal ausgeliefert

            Assert.Equal(idAngebot, Id(gruppe.Stand(), "Angebot"));
            Assert.Equal("eigen:Angebot.docx", gruppe.KennungFuer(idAngebot));
        }

        /// <summary>
        /// Eine gespeicherte Vorlage, deren Datei fehlt, bleibt als gesperrter Eintrag in der Liste und
        /// gewählt — mit dem Satz „… nicht vorhanden – … verwendet"; ein Menü hat sie nicht.
        /// </summary>
        [Fact]
        public void Eine_gespeicherte_fehlende_Vorlage_steht_gesperrt_und_gewaehlt()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig(k =>
            {
                k.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
                k.VorlageWordDatei = "Weg.docx";
            });

            Vorlagenstand stand = Gruppe(new Wegeprobe().Wege()).Stand();

            Vorlagenzeile weg = Assert.Single(stand.Vorlagen, z => z.Gesperrt);
            Assert.Equal("Weg", weg.Text);
            Assert.Equal(Format(R.BV_VORLAGEN_NICHT_VORHANDEN, "Weg", R.BV_VORLAGEN_STANDARD), weg.GesperrtHinweis);
            Assert.Equal(weg.Id, stand.VorlageId);
            Assert.Empty(stand.Handlungen);
            Assert.Null(stand.Startrueckfrage);
        }

        // =====================================================================
        //  Das Menü „…"
        // =====================================================================

        /// <summary>
        /// Windows (alle Wege belegt): eigene Vorlage „In Word öffnen", „Im Ordner zeigen",
        /// „Ersetzen…", „Entfernen" (mit Rückfrage); mitgeliefert nur „Schreibgeschützt öffnen". Ohne
        /// Wege (iOS): „Teilen…", „Ersetzen…", „Entfernen" bzw. nur „Teilen…". In Word geöffnet bleiben
        /// Ersetzen und Entfernen stehen, gesperrt mit Grund. Ausgeführt wird über den Weg der
        /// Plattform — und benannt, wenn er nicht geht.
        /// </summary>
        [Fact]
        public async Task Das_Menue_folgt_den_Wegen_der_Plattform_und_der_Quelle()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Vorlageneintrag angebot = Hinzu("Angebot.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kunde}}"));
            var probe = new Wegeprobe();
            BerichtsvorlagenGaben windows = Gruppe(probe.Wege());
            BerichtsvorlagenGaben ios = Gruppe(new Berichtsvorlagenwege());

            Vorlageneintrag standard = _vorlagen.Standardeintrag();
            Handlung lesen = Assert.Single(windows.Handlungen(standard));
            Assert.Equal(BerichtsvorlagenGaben.HANDLUNG_SCHREIBGESCHUETZT, lesen.Id);
            Assert.Equal(R.BK_BER_VORLAGE_HANDLUNG_SCHREIBGESCHUETZT, lesen.Text);
            Assert.Equal(R.BK_BER_VORLAGE_TIP_SCHREIBGESCHUETZT, lesen.Kurztext);
            Assert.Equal(new[] { BerichtsvorlagenGaben.HANDLUNG_TEILEN }, ios.Handlungen(standard).Select(h => h.Id));

            Assert.Equal(new[]
                         {
                             BerichtsvorlagenGaben.HANDLUNG_WORD, BerichtsvorlagenGaben.HANDLUNG_ORDNER,
                             BerichtsvorlagenGaben.HANDLUNG_ERSETZEN, BerichtsvorlagenGaben.HANDLUNG_ENTFERNEN
                         }, windows.Handlungen(angebot).Select(h => h.Id));
            Assert.Equal(new[]
                         {
                             BerichtsvorlagenGaben.HANDLUNG_TEILEN, BerichtsvorlagenGaben.HANDLUNG_ERSETZEN,
                             BerichtsvorlagenGaben.HANDLUNG_ENTFERNEN
                         }, ios.Handlungen(angebot).Select(h => h.Id));
            Assert.All(windows.Handlungen(angebot), h => Assert.True(h.Aktiv, h.Id));
            Handlung entfernen = windows.Handlungen(angebot).Last();
            Assert.Equal(Format(R.BK_BER_VORLAGE_FRAGE_ENTFERNEN, "Angebot"), entfernen.Rueckfrage);
            Assert.All(windows.Handlungen(angebot).Take(3), h => Assert.Equal("", h.Rueckfrage));

            string sperre = Path.Combine(Path.GetDirectoryName(angebot.Pfad)!, "~$" + angebot.Dateiname);
            File.WriteAllText(sperre, "x");
            try
            {
                List<Handlung> gesperrt = windows.Handlungen(angebot).Where(h => !h.Aktiv).ToList();
                Assert.Equal(new[] { BerichtsvorlagenGaben.HANDLUNG_ERSETZEN, BerichtsvorlagenGaben.HANDLUNG_ENTFERNEN },
                             gesperrt.Select(h => h.Id));
                Assert.All(gesperrt, h => Assert.Equal(R.BV_VORLAGEN_IN_WORD, h.Grund));
            }
            finally
            {
                File.Delete(sperre);
            }

            // Ausgeführt an der GEWÄHLTEN Vorlage, über den Weg der Plattform.
            await windows.VorlageGewaehlt(Id(windows.Stand(), "Angebot"));
            Assert.Equal(new[] { BerichtsvorlagenGaben.HANDLUNG_WORD, BerichtsvorlagenGaben.HANDLUNG_ORDNER,
                                 BerichtsvorlagenGaben.HANDLUNG_ERSETZEN, BerichtsvorlagenGaben.HANDLUNG_ENTFERNEN },
                         windows.Stand().Handlungen.Select(h => h.Id));
            await windows.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_WORD);
            Assert.Equal("word:" + angebot.Pfad, probe.Aufrufe.Last());
            Assert.Equal(Format(R.BK_BER_VORLAGE_MSG_IN_WORD, "Angebot"), windows.Stand().Meldung);
            await windows.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_ORDNER);
            Assert.Equal("ordner:" + angebot.Pfad, probe.Aufrufe.Last());

            probe.Antwort = false;
            await windows.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_WORD);
            Assert.Equal(Format(R.BK_BER_VORLAGE_MSG_WORD_FEHLT, "Angebot", angebot.Pfad), windows.Stand().Fehler);

            var datei = new Dateiprobe { TeilenAntwort = true };
            Dienste.Datei = datei;
            await ios.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_TEILEN);
            Assert.Equal(angebot.Pfad, Assert.Single(datei.Geteilt));
            Assert.Equal("", ios.Stand().Fehler);

            datei.TeilenAntwort = false;
            await ios.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_TEILEN);
            Assert.Equal(Format(R.BK_BER_VORLAGE_MSG_TEILEN_FEHLER, "Angebot"), ios.Stand().Fehler);

            await ios.HandlungAusfuehren("gibtesnicht");
            Assert.Equal(R.BK_BER_VORLAGE_MSG_UNBEKANNT, ios.Stand().Fehler);
        }

        /// <summary>
        /// „Entfernen" legt die Datei in den Unterordner „Entfernt" und nimmt die Abweichung zurück;
        /// „Ersetzen…" nimmt eine neue Datei derselben Endung über die Dateiwahl und prüft sie voll.
        /// Wieder angelegt, bekommt derselbe Name dieselbe Id.
        /// </summary>
        [Fact]
        public async Task Ersetzen_und_Entfernen_wirken_an_der_gewaehlten_Vorlage()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Vorlageneintrag angebot = Hinzu("Angebot.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kunde}}"));
            BerichtsvorlagenGaben gruppe = Gruppe(new Wegeprobe().Wege());
            int id = Id(gruppe.Stand(), "Angebot");
            await gruppe.VorlageGewaehlt(id);

            string neu = Path.Combine(_quellen, "Neu.docx");
            File.WriteAllBytes(neu, Probevorlagen.AusAbsaetzen("Kunde {{projekt.kundename}}"));
            var datei = new Dateiprobe { Antwort = neu };
            Dienste.Datei = datei;
            await gruppe.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_ERSETZEN);
            Assert.Equal(Format(R.BK_BER_VORLAGE_DLG_ERSETZEN, "Angebot"), datei.Titel);
            Vorlagenstand stand = gruppe.Stand();
            Assert.Equal(Format(R.BV_VORLAGEN_ERSETZT, "Angebot"), stand.Meldung);
            Assert.Equal(File.ReadAllBytes(neu), File.ReadAllBytes(angebot.Pfad));
            Assert.Equal(BerichtsvorlagenGaben.SYMBOL_FEHLER, stand.Pruefzeile!.Symbol);   // voll geprüft: unbekannter Platzhalter
            Assert.True(stand.Pruefzeile.HatBefunde);

            await gruppe.HandlungAusfuehren(BerichtsvorlagenGaben.HANDLUNG_ENTFERNEN);
            Assert.False(File.Exists(angebot.Pfad));
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(angebot.Pfad)!, BerichtsvorlagenCtrl.ORDNER_ENTFERNT,
                                                 "Angebot.docx")));
            Assert.Null(Lade().VorlageWordQuelle);
            stand = gruppe.Stand();
            Assert.StartsWith("„Angebot“ entfernt", stand.Meldung, StringComparison.Ordinal);
            Assert.DoesNotContain(stand.Vorlagen, z => z.Text == "Angebot");
            Assert.Equal(Id(stand, R.BV_VORLAGEN_STANDARD), stand.VorlageId);

            Hinzu("Angebot.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kunde}}"));
            Assert.Equal(id, Id(gruppe.Stand(), "Angebot"));
        }

        // =====================================================================
        //  „Hinzufügen…" und „Neue Vorlage…"
        // =====================================================================

        /// <summary>
        /// „Hinzufügen…" mit einem Namen, den es im Vorlagenordner schon gibt: Die Vorlage kommt unter
        /// „Angebot (2)" hinzu, die Meldung sagt es, sie ist gewählt und voll geprüft. Die Dateiwahl
        /// bekommt Titel und Filter der Word-Vorlagen; ein Abbruch tut nichts.
        /// </summary>
        [Fact]
        public async Task Hinzufuegen_mit_vergebenem_Namen_nimmt_einen_freien_und_sagt_es()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Angebot.docx", Probevorlagen.AusAbsaetzen("Erste {{projekt.kunde}}"));
            string andere = Directory.CreateDirectory(Path.Combine(_wurzel, "Andere")).FullName;
            string quelle = Path.Combine(andere, "Angebot.docx");
            File.WriteAllBytes(quelle, Probevorlagen.AusAbsaetzen("Zweite {{projekt.kundename}}"));
            var datei = new Dateiprobe { Antwort = quelle };
            Dienste.Datei = datei;
            BerichtsvorlagenGaben gruppe = Gruppe(new Wegeprobe().Wege());

            await gruppe.Hinzufuegen();

            Assert.Equal(R.BK_BER_VORLAGE_DLG_HINZUFUEGEN, datei.Titel);
            Assert.Equal(R.BK_BER_VORLAGE_DATEIFILTER, datei.Filter);
            Assert.Contains("*.dotx", datei.Filter, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(_vorlagen.Vorlagenordner, "Angebot (2).docx")));

            Vorlagenstand stand = gruppe.Stand();
            Assert.Equal(Format(R.BK_BER_VORLAGE_MSG_UMBENANNT, "Angebot", "Angebot (2)"), stand.Meldung);
            Assert.Equal(Id(stand, "Angebot (2)"), stand.VorlageId);
            Assert.Equal("Angebot (2).docx", Lade().VorlageWordDatei);
            Assert.Equal(BerichtsvorlagenGaben.SYMBOL_FEHLER, stand.Pruefzeile!.Symbol);
            Assert.NotNull(stand.Startrueckfrage);

            datei.Antwort = "";
            await gruppe.Hinzufuegen();
            stand = gruppe.Stand();
            Assert.Equal("", stand.Meldung);
            Assert.Equal("", stand.Fehler);
        }

        /// <summary>
        /// „Neue Vorlage…": Die Namensprüfung lehnt leer, verbotene Zeichen, reservierte Namen und einen
        /// vergebenen Namen ab; ein freier Name wird die Kopie der Standardvorlage — gewählt.
        /// </summary>
        [Fact]
        public async Task Neue_Vorlage_prueft_den_Namen_legt_die_Kopie_an_und_waehlt_sie()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Angebot.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kunde}}"));
            BerichtsvorlagenGaben gruppe = Gruppe(new Wegeprobe().Wege());

            Assert.Equal(R.BK_BER_VORLAGE_NEU_LEER, gruppe.NamePruefen("  "));
            Assert.Equal(R.BK_BER_VORLAGE_NAME_ZEICHEN, gruppe.NamePruefen("Kunde/A"));
            Assert.Equal(Format(R.BV_VORLAGEN_NAME_UNGUELTIG, "CON"), gruppe.NamePruefen("CON"));
            Assert.Equal(Format(R.BK_BER_VORLAGE_NAME_VORHANDEN, "angebot"), gruppe.NamePruefen("angebot"));
            Assert.Null(gruppe.NamePruefen("Kurzbericht Kunde"));

            await gruppe.NeueVorlageAnlegen("Kurzbericht Kunde");
            Assert.True(File.Exists(Path.Combine(_vorlagen.Vorlagenordner, "Kurzbericht Kunde.docx")));
            Vorlagenstand stand = gruppe.Stand();
            Assert.Equal(Id(stand, "Kurzbericht Kunde"), stand.VorlageId);
            Assert.Equal(Format(R.BV_VORLAGEN_NEU, "Kurzbericht Kunde"), stand.Meldung);
            Assert.Equal("Kurzbericht Kunde.docx", Lade().VorlageWordDatei);
        }

        // =====================================================================
        //  Prüfliste und Platzhalterkatalog
        // =====================================================================

        /// <summary>
        /// Die Prüfliste ist die VOLLE Prüfung der gewählten Vorlage — je Meldung Stufe, Text, Fundort,
        /// „Was tun" und die Kennung für „erklären lassen"; danach zeigt die Prüfzeile denselben Befund.
        /// Der Satz passt auf die Parameter des Dialogs.
        /// </summary>
        [Fact]
        public async Task Die_Pruefliste_ist_die_volle_Pruefung_der_gewaehlten_Vorlage()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Fehler.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kundename}}"));
            BerichtsvorlagenGaben gruppe = Gruppe(new Wegeprobe().Wege());
            await gruppe.VorlageGewaehlt(Id(gruppe.Stand(), "Fehler"));

            IReadOnlyDictionary<string, object> liste = gruppe.PrueflisteGaben();
            PasstZu(typeof(PrueflisteDialog), liste);
            var meldungen = (IReadOnlyList<Pruefmeldungszeile>)liste["Meldungen"];
            Pruefmeldungszeile unbekannt = Assert.Single(meldungen, m => m.Kennung == nameof(R.VF_PRUEF_UNBEKANNT));
            Assert.Equal(EPOS.UI.Dialoge.Berichte.Pruefstufe.Fehler, unbekannt.Stufe);
            Assert.Contains("projekt.kundename", unbekannt.Text, StringComparison.Ordinal);
            Assert.NotEqual("", unbekannt.Fundort);
            Assert.NotEqual("", unbekannt.WasTun);
            Assert.Equal("Fehler", liste["Vorlagenname"]);
            Assert.Equal(1, liste["Platzhalterzahl"]);
            Assert.Equal(BerichtsvorlagenGaben.HILFE_PRUEFLISTE, liste["HilfeSchluessel"]);

            Pruefstand zeile = gruppe.Stand().Pruefzeile!;
            Assert.Equal(BerichtsvorlagenGaben.SYMBOL_FEHLER, zeile.Symbol);
            Assert.True(zeile.HatBefunde);
            Assert.Equal(meldungen.Count == 1
                             ? Format(R.BV_VORLAGEN_PRUEFZEILE_BEFUND, 1)
                             : Format(R.BV_VORLAGEN_PRUEFZEILE_BEFUNDE, 1, meldungen.Count), zeile.Text);
        }

        /// <summary>
        /// Der Platzhalterkatalog führt die Einträge dieser Katalogfassung mit Art und Kontext als
        /// lesbarem Text und der Beschreibung des Katalogs; jede Art und jeder Kontext hat seinen
        /// Text in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Der_Platzhalterkatalog_zeigt_die_Eintraege_dieser_Fassung_lesbar()
        {
            IReadOnlyDictionary<string, object> katalog = BerichtsvorlagenGaben.KatalogGaben();
            PasstZu(typeof(PlatzhalterkatalogDialog), katalog);

            var eintraege = (IReadOnlyList<Katalogzeile>)katalog["Eintraege"];
            Assert.Equal(Vorlagenfeldkatalog.Alle.Count(f => f.Seit <= Vorlagenfeldkatalog.Katalogfassung), eintraege.Count);
            Katalogzeile kunde = Assert.Single(eintraege, z => z.Schluessel == "projekt.kunde");
            Assert.Equal("Text", kunde.Art);
            Assert.Equal("Stammprojekt", kunde.Kontext);
            Assert.Equal(Vorlagenfeldkatalog.Beschreibung(Vorlagenfeldkatalog.Finde("projekt.kunde"), false), kunde.Beschreibung);
            Assert.All(eintraege, z =>
            {
                Assert.NotEqual("", z.Art);
                Assert.NotEqual("", z.Kontext);
                Assert.NotEqual("", z.Beschreibung);
            });
            Assert.Equal(BerichtsvorlagenGaben.HILFE_PLATZHALTERKATALOG, katalog["HilfeSchluessel"]);

            ResourceSet deutsch = R.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
            ResourceSet englisch = R.ResourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en-US"), true, false)!;
            Assert.NotNull(englisch);
            var fehlt = new List<string>();
            IEnumerable<string> schluessel =
                Enum.GetNames(typeof(Vorlagenfeldart)).Select(n => "VF_KATALOG_ART_" + n.ToUpperInvariant())
                    .Concat(Enum.GetNames(typeof(Vorlagenfeldkontext)).Select(n => "VF_KATALOG_KONTEXT_" + n.ToUpperInvariant()));
            foreach (string k in schluessel)
            {
                if (string.IsNullOrWhiteSpace(deutsch.GetString(k))) fehlt.Add(k + " (de)");
                if (string.IsNullOrWhiteSpace(englisch.GetString(k))) fehlt.Add(k + " (en)");
            }
            Assert.True(fehlt.Count == 0, string.Join(", ", fehlt));

            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Number", BerichtsvorlagenGaben.Arttext(Vorlagenfeldart.Zahl));
                Assert.Equal("Base project", BerichtsvorlagenGaben.Kontexttext(Vorlagenfeldkontext.Stamm));
            }
        }

        /// <summary>
        /// „Baukasten speichern…" (BV-E5, Konzept 6.3 Nr. 3, 9.7): Der Katalog gibt den Weg; er fragt den Ort über
        /// den Speichern-Dialog der Plattform (Vorschlag „…docx" in den Dokumenten, Filter Word-Dokument), ein
        /// Abbruch liefert <c>""</c>; sonst schreibt der Kern den Baukasten — ohne Endung mit <c>.docx</c> — und die
        /// Meldung nennt den Pfad. Ein Fehler des Schreibwegs wird die Fehlermeldung, nicht eine Ausnahme.
        /// </summary>
        [Fact]
        public async Task Baukasten_speichern_schreibt_den_Baukasten_ueber_den_Speichern_Dialog()
        {
            IReadOnlyDictionary<string, object> katalog = BerichtsvorlagenGaben.KatalogGaben();
            Assert.IsType<Func<Task<string>>>(katalog["BaukastenSpeichern"]);

            var datei = new Dateiprobe();
            Dienste.Datei = datei;
            Assert.Equal("", await BerichtsvorlagenGaben.BaukastenSpeichern());
            Assert.EndsWith(R.VF_BAUKASTEN_DATEINAME + ".docx", datei.SpeichernVorschlag, StringComparison.Ordinal);
            Assert.Equal(R.VF_BAUKASTEN_DATEIFILTER, datei.Filter);
            Assert.Equal(R.VF_BAUKASTEN_DIALOGTITEL, datei.Titel);

            string ziel = Path.Combine(_ziel, "Baukasten");
            datei.SpeichernAntwort = ziel;
            string meldung = await ((Func<Task<string>>)katalog["BaukastenSpeichern"])();
            Assert.Equal(Format(R.VF_BAUKASTEN_GESPEICHERT, ziel + ".docx"), meldung);
            using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel + ".docx", false))
            {
                Assert.Contains(doc.CustomFilePropertiesPart.Properties.Elements<DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty>(),
                                e => e.Name == WordBaukasten.EIGENSCHAFT_VORLAGE && e.InnerText == WordBaukasten.VORLAGENART);
                Assert.Contains("{{projekt.kunde}}", doc.MainDocumentPart.Document.Body.InnerText, StringComparison.Ordinal);
            }

            string fehler = await BerichtsvorlagenGaben.BaukastenSpeichern((p, e) => throw new IOException("gesperrt"));
            Assert.Equal(Format(R.VF_BAUKASTEN_FEHLER, "gesperrt"), fehler);
            Assert.Equal(Vorlagenergebnisart.NameUngueltig, BerichtsvorlagenCtrl.SpeichereBaukasten(" ", false).Art);

            // Windows: kein Teilen.
            Assert.Empty(datei.Geteilt);
            await BerichtsvorlagenGaben.BaukastenSpeichern(ios: false);
            Assert.Empty(datei.Geteilt);

            // iOS (BV-E5-5): nach dem Speichern das Teilen-Blatt; scheitert es, nennt die Meldung den Pfad.
            string iosZiel = Path.Combine(_ziel, "Baukasten_ios.docx");
            datei.SpeichernAntwort = iosZiel;
            Assert.Equal(Format(R.VF_BAUKASTEN_GESPEICHERT, iosZiel), await BerichtsvorlagenGaben.BaukastenSpeichern(ios: true));
            Assert.Equal(iosZiel, Assert.Single(datei.Geteilt));
            datei.TeilenAntwort = false;
            Assert.Equal(Format(R.VF_BAUKASTEN_TEILEN_FEHLER, iosZiel), await BerichtsvorlagenGaben.BaukastenSpeichern(ios: true));

            // Ein Fehler beim Schreiben teilt nichts.
            int geteilt = datei.Geteilt.Count;
            await BerichtsvorlagenGaben.BaukastenSpeichern((p, e) => throw new IOException("gesperrt"), ios: true);
            Assert.Equal(geteilt, datei.Geteilt.Count);
        }

        // =====================================================================
        //  Startrückfrage, Weg und Lauf
        // =====================================================================

        /// <summary>
        /// Ohne Befund keine erweiterte Rückfrage; mit einem Fehler steht sie — mit <c>{0}</c> für die
        /// Zahl der Versionen, dem Namen der Vorlage, den Befunden und drei Wegen. Der Weg des Laufs
        /// folgt der Antwort; ohne Antwort die gewählte Vorlage, und die Befunde gehen in die Meldung.
        /// Im zweiten Einstieg nimmt eine Vorlage ohne Platzhalter der Wirtschaftlichkeit die
        /// Standardvorlage.
        /// </summary>
        [Fact]
        public async Task Die_Startrueckfrage_steht_nur_mit_Befund_und_ihr_Weg_bestimmt_den_Lauf()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Sauber.docx", Probevorlagen.AusAbsaetzen("Projekt {{projekt.name}}"));
            Hinzu("Fehler.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kundename}}"));
            BerichtsvorlagenGaben gruppe = Gruppe(new Wegeprobe().Wege());
            Vorlagenstand stand = gruppe.Stand();

            await gruppe.VorlageGewaehlt(Id(stand, "Sauber"));
            Assert.Null(gruppe.Stand().Startrueckfrage);

            await gruppe.VorlageGewaehlt(Id(stand, "Fehler"));
            Startrueckfrage frage = gruppe.Stand().Startrueckfrage!;
            Assert.NotNull(frage);
            Assert.Contains("{0}", frage.Text, StringComparison.Ordinal);
            Assert.Contains("„Fehler“", frage.Text, StringComparison.Ordinal);
            Assert.Contains(R.BV_START_GELB, frage.Text, StringComparison.Ordinal);
            Assert.Contains(frage.Befunde, b => b.Contains("projekt.kundename", StringComparison.Ordinal));
            Assert.True(frage.EigeneMoeglich);
            Assert.Equal(R.BV_START_WEG_EIGENE, frage.WegEigene);
            Assert.Equal(R.BV_START_WEG_STANDARD, frage.WegStandard);
            Assert.Equal(R.BV_START_WEG_ABBRECHEN, frage.WegAbbrechen);

            Startbefund start = gruppe.StartFuerLauf(Lade(), false, 1, false);
            Assert.True(start.BrauchtRueckfrage);
            Assert.Equal(Startweg.Standard, BerichtSeiteGaben.Weg(UiStartweg.Standard, start, false, out IReadOnlyList<string> leer));
            Assert.Empty(leer);
            Assert.Equal(Startweg.Gewaehlt, BerichtSeiteGaben.Weg(UiStartweg.Eigene, start, false, out _));
            Assert.Equal(Startweg.Gewaehlt, BerichtSeiteGaben.Weg("", start, false, out IReadOnlyList<string> ungefragt));
            Assert.Contains(ungefragt, b => b.Contains("projekt.kundename", StringComparison.Ordinal));

            await gruppe.VorlageGewaehlt(Id(stand, "Sauber"));
            Startbefund zweiter = gruppe.StartFuerLauf(Lade(), false, 1, erzwingtWirtschaftlichkeit: true);
            Assert.True(zweiter.OhneWirtschaftlichkeit);
            Assert.Equal(Startweg.Standard, BerichtSeiteGaben.Weg("", zweiter, true, out IReadOnlyList<string> genannt));
            Assert.NotEmpty(genannt);
        }

        /// <summary>
        /// Der Lauf der Berichtsseite füllt die gewählte Vorlage — die Laufmeldung nennt sie samt Grund
        /// in der Meldung — und merkt sich die Auswahl, ohne die Vorlagenwahl zu verlieren.
        /// </summary>
        [Fact]
        public async Task Der_Lauf_fuellt_die_gewaehlte_Vorlage_und_die_Laufmeldung_steht_in_der_Meldung()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Deckblatt.docx", Probevorlagen.AusAbsaetzen("Projekt {{projekt.name}}"));
            (IReadOnlyDictionary<string, object> gaben, Func<Vorlagenstand> neuLaden) = Seite();

            await ((EventCallback<int?>)gaben["VorlageIdChanged"]).InvokeAsync(Id(neuLaden(), "Deckblatt"));
            Vorlagenstand stand = neuLaden();
            Assert.Null(stand.Startrueckfrage);

            LaufErgebnis erg = await Erstellen(gaben, stand, "");

            Assert.True(erg.Erfolg, erg.Fehler);
            Assert.True(File.Exists(erg.Datei));
            Assert.StartsWith(R.BK_BER_MSG_ERSTELLT_KOPF, erg.Meldung, StringComparison.Ordinal);
            Assert.Contains(Format(R.BV_LAUF_VORLAGE, "Deckblatt", R.BV_VORLAGEN_GRUND_ABWEICHUNG), erg.Meldung,
                            StringComparison.Ordinal);

            BerichtsKonfiguration k = Lade();
            Assert.Equal("Deckblatt.docx", k.VorlageWordDatei);
            Assert.Equal(new[] { BerichtsKonfiguration.B_DECKBLATT }, k.AktiveBausteine);
        }

        /// <summary>
        /// Der Weg „standard" der erweiterten Rückfrage: DIESER Bericht entsteht aus der
        /// Standardvorlage, die Laufmeldung nennt die ersetzte — und die Wahl des Stammprojekts bleibt.
        /// </summary>
        [Fact]
        public async Task Der_Weg_standard_nimmt_fuer_diesen_Lauf_die_Standardvorlage_und_nennt_die_ersetzte()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Fehler.docx", Probevorlagen.AusAbsaetzen("Kunde {{projekt.kundename}}"));
            (IReadOnlyDictionary<string, object> gaben, Func<Vorlagenstand> neuLaden) = Seite();

            await ((EventCallback<int?>)gaben["VorlageIdChanged"]).InvokeAsync(Id(neuLaden(), "Fehler"));
            Vorlagenstand stand = neuLaden();
            Assert.NotNull(stand.Startrueckfrage);
            await ((EventCallback<string>)gaben["StartGewaehlt"]).InvokeAsync(UiStartweg.Standard);

            LaufErgebnis erg = await Erstellen(gaben, stand, UiStartweg.Standard);

            Assert.True(erg.Erfolg, erg.Fehler);
            Assert.Contains(Format(R.BV_LAUF_ERSETZT, "Fehler", R.BV_VORLAGEN_STANDARD), erg.Meldung, StringComparison.Ordinal);
            Assert.Equal("Fehler.docx", Lade().VorlageWordDatei);
        }

        /// <summary>
        /// BV-E3 (Konzept 5.1, 8.5): Der Lauf reicht den BEDARF an den Sammler — eine Vorlage nur mit
        /// Projektangaben erhebt auch mit allen Häkchen weder Stundenreihen noch Verlauf noch Emissionsbilanz
        /// (<see cref="Startbefund.Bedarf"/>); entsteht die Mappe mit, folgt der Bedarf ihren Häkchen
        /// (<see cref="Berichtsbedarf.Vorgabe"/>), ohne Word allein der Mappe.
        /// </summary>
        [Fact]
        public async Task Der_Lauf_reicht_den_Bedarf_der_Vorlage_an_den_Sammler()
        {
            if (_standard == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Konfig();
            Hinzu("Deckblatt.docx", Probevorlagen.AusAbsaetzen("Projekt {{projekt.name}}"));
            var bedarfe = new List<Berichtsbedarf>();
            (IReadOnlyDictionary<string, object> gaben, Func<Vorlagenstand> neuLaden) = Seite(bedarfe);
            await ((EventCallback<int?>)gaben["VorlageIdChanged"]).InvokeAsync(Id(neuLaden(), "Deckblatt"));

            string[] alle = BerichtsKonfiguration.AlleBausteine.Select(b => b.Schluessel).ToArray();
            string[] ohneErgebnisse = alle.Where(b => b != BerichtsKonfiguration.B_ERGEBNISSE).ToArray();
            LaufErgebnis nurWord = await Erstellen(gaben, neuLaden(), "", alle, 0);
            LaufErgebnis beide = await Erstellen(gaben, neuLaden(), "", ohneErgebnisse, 2);
            LaufErgebnis nurExcel = await Erstellen(gaben, neuLaden(), "", new[] { BerichtsKonfiguration.B_ERGEBNISSE }, 1);

            Assert.True(nurWord.Erfolg, nurWord.Fehler);
            Assert.True(beide.Erfolg, beide.Fehler);
            Assert.True(nurExcel.Erfolg, nurExcel.Fehler);
            Assert.Equal(new[]
            {
                Berichtsbedarf.Nichts,
                new Berichtsbedarf(Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz),
                new Berichtsbedarf(Vorlagenbedarf.Zeitreihen),
            }, bedarfe);
        }

        // =====================================================================
        //  Programmeinstellungen, Abschnitt „Bericht"
        // =====================================================================

        /// <summary>
        /// Die Gaben passen auf die Parameter des Dialogs; die Firma kommt aus der Lizenz und geht
        /// geschrieben zurück (die der Lizenz heißt: keine eigene Angabe), der Ordner nur, wenn es ihn
        /// gibt — sonst sagt es eine Meldung. Ohne Ordnerwahl der Plattform (iOS) ist er gesperrt, mit
        /// Grund.
        /// </summary>
        [Fact]
        public async Task Die_Einstellungen_schreiben_Firma_und_Ordner_und_ohne_Ordnerwahl_ist_der_Ordner_gesperrt()
        {
            var dialog = new Dialogprobe();
            Dienste.Dialog = dialog;
            IReadOnlyDictionary<string, object> gaben =
                EinstellungenBerichtGaben.Gaben(_vorlagen, new Berichtsvorlagenwege { OrdnerWaehlbar = true });

            PasstZu(typeof(EinstellungenDialog), gaben);
            Assert.Equal(FIRMA, gaben["Firma"]);
            Assert.Equal(FIRMA, gaben["FirmaVorgabe"]);
            Assert.Equal(_vorlagen.Vorgabeordner, gaben["Vorlagenordner"]);
            Assert.Equal(_vorlagen.Vorgabeordner, gaben["VorlagenordnerVorgabe"]);
            Assert.False(gaben.ContainsKey("VorlagenordnerGesperrtGrund"));
            Assert.IsType<EinstellungenBerichtTexte>(gaben["BerichtTexte"]);
            Assert.Equal(EinstellungenBerichtGaben.HILFE_BERICHT, gaben["HilfeSchluesselBericht"]);

            var firma = (EventCallback<string>)gaben["FirmaChanged"];
            await firma.InvokeAsync("Büro Nord");
            Assert.Equal("Büro Nord", _vorlagen.Ersteller().Firma);
            await firma.InvokeAsync(FIRMA);
            Assert.Null(_einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_FIRMA, null));
            Assert.Equal(FIRMA, _vorlagen.Ersteller().Firma);
            await firma.InvokeAsync("");
            Assert.Equal("", _einstellungen.Lies(BerichtsvorlagenCtrl.EINSTELLUNG_FIRMA, null));
            Assert.Null(_vorlagen.Ersteller().Firma);

            var ordner = (EventCallback<string>)gaben["VorlagenordnerChanged"];
            string buero = Directory.CreateDirectory(Path.Combine(_wurzel, "Buero")).FullName;
            await ordner.InvokeAsync(buero);
            Assert.Equal(buero, _vorlagen.Vorlagenordner);
            Assert.Empty(dialog.Warnungen);

            string weg = Path.Combine(_wurzel, "GibtEsNicht");
            await ordner.InvokeAsync(weg);
            Assert.Equal(buero, _vorlagen.Vorlagenordner);
            Assert.Equal(Format(R.BV_VORLAGEN_ORDNER_NICHT_ERREICHBAR, weg), Assert.Single(dialog.Warnungen));

            IReadOnlyDictionary<string, object> ios = EinstellungenBerichtGaben.Gaben(_vorlagen, new Berichtsvorlagenwege());
            Assert.Equal(R.EIN_BERICHT_ORDNER_FEST, ios["VorlagenordnerGesperrtGrund"]);
            PasstZu(typeof(EinstellungenDialog), ios);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Der Satz der Seite samt Nachladen — mit einem Sammler ohne Simulation; er merkt sich
        /// den Bedarf jedes Laufs in <paramref name="bedarfe"/>, wenn gesetzt.</summary>
        private (IReadOnlyDictionary<string, object> Gaben, Func<Vorlagenstand> NeuLaden) Seite(List<Berichtsbedarf> bedarfe = null)
        {
            var seite = new BerichtSeiteGaben(GRUPPE, "Stamm", _vorlagen, new Wegeprobe().Wege())
            {
                Sammler = (konfig, bedarf, melde, abbruch, sicht) =>
                {
                    bedarfe?.Add(bedarf);
                    return Berichtsdatenproben.Gruppendaten(2);
                }
            };
            IReadOnlyDictionary<string, object> gaben = seite.Gaben();
            return (gaben, (Func<Vorlagenstand>)gaben["VorlagenNeuLaden"]);
        }

        /// <summary>Der Lauf der Seite mit dem Deckblatt, Ausgabe Word, in den Zielordner des Falls.</summary>
        private Task<LaufErgebnis> Erstellen(IReadOnlyDictionary<string, object> gaben, Vorlagenstand stand, string weg)
        {
            return Erstellen(gaben, stand, weg, new[] { BerichtsKonfiguration.B_DECKBLATT }, 0);
        }

        /// <summary>Der Lauf der Seite mit diesen Häkchen und dieser Ausgabe (0 Word, 1 Excel, 2 beide).</summary>
        private Task<LaufErgebnis> Erstellen(IReadOnlyDictionary<string, object> gaben, Vorlagenstand stand, string weg,
                                             IReadOnlyList<string> bausteine, int ausgabe)
        {
            var erstellen = (Func<BerichtAuftrag, Action<Laufschritt>, Task<LaufErgebnis>>)gaben["Erstellen"];
            return erstellen(new BerichtAuftrag
            {
                VariantenIds = Array.Empty<int>(),
                Bausteine = bausteine,
                AusgabeId = ausgabe,
                Zielordner = _ziel,
                AnzahlMitStamm = 1,
                VorlageId = stand.VorlageId,
                Vorlagenweg = weg
            }, _ => { });
        }

        /// <summary>Jeder Schlüssel des Satzes ist ein <c>[Parameter]</c> der Komponente mit passendem Typ.</summary>
        private static void PasstZu(Type komponente, IReadOnlyDictionary<string, object> gaben)
        {
            Assert.NotNull(gaben);
            Dictionary<string, Type> parameter = komponente.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
                .ToDictionary(p => p.Name, p => p.PropertyType);
            foreach (KeyValuePair<string, object> g in gaben)
            {
                Assert.True(parameter.ContainsKey(g.Key), komponente.Name + ": kein [Parameter] " + g.Key);
                Assert.True(g.Value != null && parameter[g.Key].IsInstanceOfType(g.Value),
                            komponente.Name + "." + g.Key + ": " + (g.Value?.GetType().Name ?? "null"));
            }
        }

        private BerichtsvorlagenGaben Gruppe(Berichtsvorlagenwege wege)
        {
            return new BerichtsvorlagenGaben(GRUPPE, new BerichtCtrl(_vorlagen), _vorlagen, wege);
        }

        /// <summary>Eine frische Konfiguration der Gruppe: Deckblatt, Word, Zielordner des Falls.</summary>
        private void Konfig(Action<BerichtsKonfiguration> setzen = null)
        {
            var k = new BerichtsKonfiguration { Ausgabe = "Word", ZielOrdner = _ziel };
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_DECKBLATT);
            setzen?.Invoke(k);
            Assert.True(new BerichtCtrl(_vorlagen).Speichere(GRUPPE, k));
        }

        private BerichtsKonfiguration Lade()
        {
            return new BerichtCtrl(_vorlagen).Lade(GRUPPE);
        }

        /// <summary>Legt eine Quelldatei an und fügt sie dem Vorlagenordner hinzu.</summary>
        private Vorlageneintrag Hinzu(string name, byte[] inhalt)
        {
            string quelle = Path.Combine(_quellen, name);
            File.WriteAllBytes(quelle, inhalt);
            Vorlagenergebnis r = _vorlagen.Hinzufuegen(quelle);
            Assert.True(r.Erfolg, r.Meldung);
            return r.Eintrag;
        }

        private static int Id(Vorlagenstand stand, string text)
        {
            return Assert.Single(stand.Vorlagen, z => z.Text == text).Id;
        }

        private static string Format(string muster, params object[] argumente)
        {
            return string.Format(CultureInfo.CurrentCulture, muster, argumente);
        }

        /// <summary>Eine Vorlage des Repositoriums als Bytes; <c>null</c> ohne Repositorium.</summary>
        private static byte[] Repovorlage(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
            Assert.True(File.Exists(pfad), "Die Vorlage fehlt: " + pfad);
            return File.ReadAllBytes(pfad);
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

        /// <summary>Die Wege einer Windows-artigen Plattform, die mitschreiben und eine feste Antwort geben.</summary>
        private sealed class Wegeprobe
        {
            internal readonly List<string> Aufrufe = new List<string>();
            internal bool Antwort = true;

            internal Berichtsvorlagenwege Wege()
            {
                return new Berichtsvorlagenwege
                {
                    ImOrdnerZeigen = p => { Aufrufe.Add("ordner:" + p); return Antwort; },
                    InWordOeffnen = p => { Aufrufe.Add("word:" + p); return Antwort; },
                    SchreibgeschuetztOeffnen = p => { Aufrufe.Add("lesen:" + p); return Antwort; },
                    OrdnerWaehlbar = true
                };
            }
        }

        /// <summary>Eine Dateiwahl-Attrappe: feste Antwort, Titel und Filter gemerkt, Teilen mitgeschrieben.</summary>
        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Antwort = "";
            internal string Titel = "";
            internal string Filter = "";
            internal bool TeilenAntwort = true;
            internal readonly List<string> Geteilt = new List<string>();

            public string DateiOeffnen(string titel, string filter, string startOrdner)
            {
                Titel = titel ?? "";
                Filter = filter ?? "";
                return Antwort;
            }

            internal string SpeichernAntwort = "";
            internal string SpeichernVorschlag = "";

            public string DateiSpeichern(string titel, string filter, string vorschlag)
            {
                Titel = titel ?? "";
                Filter = filter ?? "";
                SpeichernVorschlag = vorschlag ?? "";
                return SpeichernAntwort;
            }

            public string OrdnerWaehlen(string titel, string startOrdner) => "";

            public bool MitSystemOeffnen(string pfad)
            {
                Geteilt.Add(pfad);
                return TeilenAntwort;
            }
        }

        /// <summary>Ein Dialogdienst, der die Warnungen mitschreibt.</summary>
        private sealed class Dialogprobe : IDialogDienst
        {
            internal readonly List<string> Warnungen = new List<string>();

            public void Meldung(string text, string titel = null) { }

            public void Warnung(string text, string titel = null) { Warnungen.Add(text); }

            public void Fehler(string text, string titel = null) { }

            public bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false) => false;

            public JaNeinAbbruch Wahl(string text, string titel = null) => JaNeinAbbruch.Abbruch;

            public void Warten(bool an) { }
        }
    }
}
