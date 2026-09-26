using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Seiten.Berichte;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;
using Meldungsstufe = EPOS.UI.Dialoge.Berichte.Pruefstufe;
using R = WindowsFormsApplication1.MyResource.Resource;
using UiStartweg = EPOS.UI.Seiten.Berichte.Startweg;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Gruppe „Vorlage" der Berichtsseite</b> (Etappe BV-E1; Konzept
    /// Berichtsvorlagen 8.3, 10.2, 10.3) — die Vorlagenliste mit stabilen Ids, die Wahl als
    /// Abweichung des Stammprojekts, die Handlungen des Menüs „…" je Plattform, die Prüfzeile, die
    /// Prüfliste, der Platzhalterkatalog und die erweiterte Startrückfrage. Die Seite
    /// (<see cref="BerichtSeite"/>) zeigt an; gerechnet, gelesen und geschrieben wird hier, über
    /// <see cref="BerichtsvorlagenCtrl"/> und <see cref="BerichtCtrl"/>.
    ///
    /// <para><b>Stabile Ids.</b> Die Seite meldet eine Zahl, nie einen Text (Hausregel
    /// <c>Auswahlfeld</c>); der Controller kennt die Kennung <c>standard</c> bzw.
    /// <c>eigen:</c> + Dateiname. Die Hülle vergibt je Kennung EINMAL je Sitzung eine Zahl — eine
    /// Vorlage behält ihre Id über jedes Nachladen, auch nach „Entfernen" und neuem Anlegen
    /// desselben Namens.</para>
    ///
    /// <para><b>Die Wahl ist die Abweichung des Stammprojekts</b> (Konzept 10.3): gespeichert in
    /// seiner <see cref="BerichtsKonfiguration"/> über <see cref="BerichtCtrl.Speichere"/> — derselbe
    /// Weg wie die Häkchen. Wer die Vorgabe der Installation wählt, braucht keine Abweichung; sie
    /// wird dann entfernt. Eine gespeicherte Vorlage, deren Datei fehlt, bleibt als gesperrter
    /// Eintrag in der Liste und gewählt, mit dem Satz „… nicht vorhanden – … verwendet".</para>
    ///
    /// <para><b>Die Schnellprüfung läuft beim Laden, nach jeder Handlung und beim
    /// Vorlagenwechsel</b> — jedes Nachladen (<see cref="Stand"/>) ist eine Vorprüfung
    /// (<see cref="BerichtCtrl.PruefeVorStart"/>). Ihr Befund samt den gelesenen Bytes bleibt bis
    /// zum Lauf stehen (<see cref="StartFuerLauf"/>): Gefüllt wird, was geprüft wurde (6.8). Die
    /// volle Prüfung („Prüfen", „Hinzufügen…", „Ersetzen…", die Prüfliste) ersetzt die Zeile, solange
    /// die Prüfsumme dieselbe ist.</para>
    ///
    /// <para><b>Plattformen.</b> Welche Einträge das Menü „…" trägt, entscheiden die belegten Wege
    /// der Naht <see cref="Berichtsvorlagenwege"/> und die Quelle der Vorlage: Windows „In Word
    /// öffnen", „Im Ordner zeigen", „Ersetzen…", „Entfernen"; ohne Wege (iOS) „Teilen…",
    /// „Ersetzen…", „Entfernen"; eine mitgelieferte Vorlage nur „Schreibgeschützt öffnen" bzw.
    /// „Teilen…". Was nicht geht, meldet die Gruppe benannt.</para>
    ///
    /// <para><b>BV-E2 (Konzept 10.2, „Häkchen (BV-Q1 c)"): der Kapitelstand.</b> Jedes Nachladen
    /// sagt der Seite, welche Kapitel die geprüfte Vorlage führt (<see cref="Kapitel(Pruefbefund)"/>) —
    /// die Häkchen folgen ihm nach jedem Vorlagenwechsel.</para>
    ///
    /// <para><b>BV-E2 (Konzept 9.5, 11 Nr. 3): die Anhang-E-Stellen.</b> Die Überlagerung
    /// „Anhang-E-Checkliste…" der Wirtschaftlichkeitsseite nennt die Stellen der gewählten Vorlage
    /// (<see cref="AnhangEStellenDerVorlage"/>) — die Kapitelstellen kommen aus dem Kern über den
    /// Delegaten <see cref="Kapitelstellen"/>, die Spalte „Stelle" baut die Checkliste des Kerns.</para>
    /// </summary>
    internal sealed class BerichtsvorlagenGaben
    {
        // ---- Die Handlungen des Menüs „…" (sprachneutral, wie HandlungGewaehlt sie meldet) ----

        /// <summary>„In Word öffnen" (Windows, eigene Vorlage).</summary>
        internal const string HANDLUNG_WORD = "word";

        /// <summary>„Im Ordner zeigen" (Windows, eigene Vorlage).</summary>
        internal const string HANDLUNG_ORDNER = "ordner";

        /// <summary>„Ersetzen…" (eigene Vorlage).</summary>
        internal const string HANDLUNG_ERSETZEN = "ersetzen";

        /// <summary>„Entfernen" (eigene Vorlage, mit Rückfrage).</summary>
        internal const string HANDLUNG_ENTFERNEN = "entfernen";

        /// <summary>„Teilen…" (ohne Wege der Plattform: das Teilen-Blatt über <c>Dienste.Datei</c>).</summary>
        internal const string HANDLUNG_TEILEN = "teilen";

        /// <summary>„Schreibgeschützt öffnen" (Windows, mitgelieferte Vorlage).</summary>
        internal const string HANDLUNG_SCHREIBGESCHUETZT = "schreibgeschuetzt";

        /// <summary>Hilfeschlüssel der Überlagerung „Prüfliste".</summary>
        internal const string HILFE_PRUEFLISTE = "UcBericht.btn_Help_Pruefliste";

        /// <summary>Hilfeschlüssel der Überlagerung „Platzhalterkatalog".</summary>
        internal const string HILFE_PLATZHALTERKATALOG = "UcBericht.btn_Help_Platzhalterkatalog";

        /// <summary>Die Zeichen der Prüfzeile — reine Dekoration, der Text trägt die Aussage.</summary>
        internal const string SYMBOL_OK = "✓";
        internal const string SYMBOL_WARNUNG = "⚠";
        internal const string SYMBOL_FEHLER = "✖";
        internal const string SYMBOL_HINWEIS = "ℹ";

        private readonly int _idStamm;
        private readonly BerichtCtrl _bericht;
        private readonly BerichtsvorlagenCtrl _vorlagen;
        private readonly Berichtsvorlagenwege _wege;
        private readonly Func<int> _sicht;

        /// <summary>Kennung → Id und zurück — je Sitzung stabil.</summary>
        private readonly Dictionary<string, int> _ids = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<int, string> _kennungen = new Dictionary<int, string>();

        /// <summary>Der Befund der letzten Vorprüfung samt Bytes — gehalten bis zum Lauf.</summary>
        private Startbefund _start;

        /// <summary>BV-E7-3: der Befund der Excel-Vorlage aus derselben Vorprüfung — bis zum Lauf gehalten.</summary>
        private Excelstartbefund _excelStart;

        /// <summary>Die letzte volle Prüfung und die Kennung ihrer Vorlage.</summary>
        private Pruefbefund _voll;
        private string _vollId;

        /// <summary>BV-E7: die letzte volle Prüfung einer Excel-Vorlage und ihre Kennung.</summary>
        private Pruefbefund _vollExcel;
        private string _vollExcelId;

        /// <summary>Meldung und Fehler der letzten Handlung — einmal ausgeliefert, dann leer.</summary>
        private string _meldung = "";
        private string _fehler = "";

        /// <param name="idStamm">Das Stammprojekt der Vergleichsgruppe.</param>
        /// <param name="bericht">Der Bericht-Controller (Konfiguration, Vorprüfung) — mit <paramref name="vorlagen"/> gebaut.</param>
        /// <param name="vorlagen">Der Vorlagen-Controller.</param>
        /// <param name="wege">Die Wege der Plattform; <c>null</c> = <see cref="Berichtsvorlagenwege.Plattform"/>.</param>
        /// <param name="sicht">Die Vergleichssicht der Ergebnisansicht: 1 oder 2; <c>null</c> = 1.</param>
        internal BerichtsvorlagenGaben(int idStamm, BerichtCtrl bericht, BerichtsvorlagenCtrl vorlagen,
                                       Berichtsvorlagenwege wege = null, Func<int> sicht = null)
        {
            _idStamm = idStamm;
            _vorlagen = vorlagen ?? new BerichtsvorlagenCtrl();
            _bericht = bericht ?? new BerichtCtrl(_vorlagen);
            _wege = wege;
            _sicht = sicht ?? (() => 1);

            // BV-E2: die Kapitelstellen der Vorlage fuer die Anhang-E-Ueberlagerung liefert der Kern.
            Kapitelstellen = _bericht.KapitelstellenDerVorlage;
        }

        /// <summary>
        /// BV-E2 (Konzept 9.5, 11 Nr. 3): <b>die Kapitelstellen der gewählten Word-Vorlage</b> — je
        /// Stellenschlüssel (<see cref="Berichtskapitel.Stellenschluessel"/>) die Überschrift des Kapitels im
        /// Bericht, <c>null</c> = nicht im Bericht; <c>bool</c> = Bericht auf Englisch. Im Konstruktor
        /// eingehängt: <see cref="BerichtCtrl.KapitelstellenDerVorlage"/>. <c>null</c> = die
        /// Anhang-E-Überlagerung nennt die Stellen der Standardvorlage (<see cref="AnhangEStellenDerVorlage"/>).
        /// Ein Prüfstand setzt eigene Stellen.
        /// </summary>
        internal Func<BerichtsKonfiguration, bool, IReadOnlyDictionary<string, string>> Kapitelstellen { get; set; }

        private Berichtsvorlagenwege Wege { get { return _wege ?? Berichtsvorlagenwege.Plattform ?? new Berichtsvorlagenwege(); } }

        /// <summary>Die Sprache der Oberfläche — zugleich die des Berichts (<see cref="BerichtTexte.Englisch"/>).</summary>
        private static bool Englisch { get { return BerichtTexte.Englisch; } }

        // =====================================================================
        //  Der Parametersatz
        // =====================================================================

        /// <summary>
        /// Belegt die Parameter der Gruppe im Satz der Seite. Werte, die <c>null</c> wären
        /// (<c>VorlageId</c>, <c>Pruefzeile</c>, <c>Startrueckfrage</c>), bleiben weg — die Seite
        /// hat dafür ihre Vorgabe.
        /// </summary>
        internal void Belegen(IDictionary<string, object> gaben)
        {
            Vorlagenstand stand = Stand();

            gaben["Vorlagen"] = stand.Vorlagen;
            if (stand.VorlageId.HasValue) gaben["VorlageId"] = stand.VorlageId.Value;
            gaben["VorlageIdChanged"] = EventCallback.Factory.Create<int?>(this, VorlageGewaehlt);
            gaben["Vorlagenhandlungen"] = stand.Handlungen;
            gaben["HandlungGewaehlt"] = EventCallback.Factory.Create<string>(this, HandlungAusfuehren);
            gaben["NeueVorlage"] = EventCallback.Factory.Create<string>(this, NeueVorlageAnlegen);
            gaben["Vorlagenmuster"] = Mustereintraege();
            gaben["NeueVorlageAus"] = EventCallback.Factory.Create<Neuvorlage>(this, NeueVorlageAusMuster);
            gaben["VorlagennamePruefen"] = new Func<string, string>(NamePruefen);
            gaben["Hinzufuegen"] = EventCallback.Factory.Create(this, Hinzufuegen);
            gaben["Pruefen"] = EventCallback.Factory.Create(this, Pruefen);
            gaben["PlatzhalterkatalogGaben"] = new Func<IReadOnlyDictionary<string, object>>(KatalogGaben);
            if (stand.Pruefzeile != null) gaben["Pruefzeile"] = stand.Pruefzeile;
            gaben["PrueflisteGaben"] = new Func<IReadOnlyDictionary<string, object>>(PrueflisteGaben);
            if (stand.Startrueckfrage != null) gaben["Startrueckfrage"] = stand.Startrueckfrage;
            if (stand.Kapitelstand != null) gaben["Kapitelstand"] = stand.Kapitelstand;
            gaben["StartGewaehlt"] = EventCallback.Factory.Create<string>(this, StartGewaehlt);
            gaben["VorlagenNeuLaden"] = new Func<Vorlagenstand>(Stand);
            gaben["Vorlagentexte"] = new BerichtSeiteVorlagentexte();

            // BV-E7 (Konzept 10.2): die Zeile „Excel-Vorlage".
            gaben["ExcelVorlagen"] = stand.ExcelVorlagen;
            if (stand.ExcelVorlageId.HasValue) gaben["ExcelVorlageId"] = stand.ExcelVorlageId.Value;
            gaben["ExcelVorlageIdChanged"] = EventCallback.Factory.Create<int?>(this, ExcelVorlageGewaehlt);
            if (stand.ExcelPruefzeile != null) gaben["ExcelPruefzeile"] = stand.ExcelPruefzeile;
            gaben["ExcelPrueflisteGaben"] = new Func<IReadOnlyDictionary<string, object>>(ExcelPrueflisteGaben);
        }

        // =====================================================================
        //  Der Stand der Gruppe
        // =====================================================================

        /// <summary>
        /// Der FRISCHE Stand der Gruppe (<c>VorlagenNeuLaden</c>): Liste, Wahl, Menü, Prüfzeile und
        /// — wo nötig — die erweiterte Startrückfrage. Jeder Aufruf ist eine Vorprüfung; ihr Befund
        /// bleibt bis zum Lauf stehen. Meldung und Fehler der letzten Handlung gehen einmal mit.
        /// </summary>
        internal Vorlagenstand Stand()
        {
            string meldung = _meldung;
            string fehler = _fehler;
            _meldung = "";
            _fehler = "";

            BerichtsKonfiguration konfig = Lade();
            Startbefund start = null;
            string prueffehler = null;
            try { start = _bericht.PruefeVorStart(konfig, Englisch, Sicht()); }
            catch (Exception ex) { prueffehler = ex.Message; }
            _start = start;

            // BV-E7-3: die Excel-Vorlage in derselben Vorprüfung — ihre Fehler stehen in derselben Rückfrage.
            Excelstartbefund excelStart = null;
            if (MitExcel(konfig))
                try { excelStart = _bericht.PruefeExcelVorStart(konfig, Englisch, Sicht()); }
                catch (Exception) { excelStart = null; }   // die Prüfzeile nennt den Grund; der Lauf fällt selbst zurück
            _excelStart = excelStart;

            Vorlagenwahl wahl = start?.Wahl ?? Wahl(konfig);
            List<Vorlagenzeile> zeilen = Zeilen(wahl, out int? gewaehlt);

            IReadOnlyList<Handlung> handlungen = wahl == null || wahl.FehlendeId != null
                ? Array.Empty<Handlung>()
                : Handlungen(wahl.Eintrag);

            // BV-E7: die Zeile „Excel-Vorlage" — Liste, Wahl, Prüfzeile (Schnellprüfung der gewählten Excel-Vorlage).
            Vorlagenwahl excel = ExcelWahl(konfig);
            List<Vorlagenzeile> excelZeilen = ExcelZeilen(excel, out int? excelGewaehlt);
            Pruefstand excelPruefzeile = ExcelPruefzeile(excel, konfig);

            return new Vorlagenstand
            {
                Vorlagen = zeilen,
                VorlageId = gewaehlt,
                Handlungen = handlungen,
                Pruefzeile = Pruefzeile(start, wahl, prueffehler),
                Startrueckfrage = Rueckfrage(MitWord(konfig) ? start : null, excelStart),
                Kapitelstand = Kapitel(start?.Pruefbefund),
                ExcelVorlagen = excelZeilen,
                ExcelVorlageId = excelGewaehlt,
                ExcelPruefzeile = excelPruefzeile,
                Meldung = meldung ?? "",
                Fehler = fehler ?? ""
            };
        }

        /// <summary>
        /// Die Einträge des Auswahlfelds: die Standardvorlage, die eigenen des Vorlagenordners und
        /// — gewählt und gesperrt — eine gespeicherte Vorlage, deren Datei fehlt.
        /// </summary>
        private List<Vorlagenzeile> Zeilen(Vorlagenwahl wahl, out int? gewaehlt)
        {
            var zeilen = new List<Vorlagenzeile>();
            foreach (Vorlageneintrag e in Liste())
                zeilen.Add(new Vorlagenzeile(IdFuer(e.Id), e.Name, false, "", e.IstStandard));

            gewaehlt = null;
            if (wahl == null) return zeilen;

            if (wahl.FehlendeId != null)
            {
                int id = IdFuer(wahl.FehlendeId);
                string name = NameAusKennung(wahl.FehlendeId);
                string hinweis = wahl.Meldungen.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                                 ?? Format(R.BV_VORLAGEN_NICHT_VORHANDEN, name, wahl.Eintrag?.Name ?? "");
                if (zeilen.All(z => z.Id != id)) zeilen.Add(new Vorlagenzeile(id, name, true, hinweis));
                gewaehlt = id;
            }
            else if (wahl.Eintrag != null)
            {
                gewaehlt = IdFuer(wahl.Eintrag.Id);
            }
            return zeilen;
        }

        // =====================================================================
        //  BV-E7: die Zeile „Excel-Vorlage" (Konzept 10.2, 10.3)
        // =====================================================================

        /// <summary>
        /// Die Einträge des Auswahlfelds „Excel-Vorlage": „Ohne Vorlage (EPOS-Plan)", die eigenen Excel-Vorlagen des
        /// Vorlagenordners und — gewählt und gesperrt — eine gespeicherte Excel-Vorlage, deren Datei fehlt.
        /// </summary>
        private List<Vorlagenzeile> ExcelZeilen(Vorlagenwahl wahl, out int? gewaehlt)
        {
            var zeilen = new List<Vorlagenzeile>();
            IReadOnlyList<Vorlageneintrag> liste;
            try { liste = _vorlagen.ListeExcel(); }
            catch (Exception) { liste = Array.Empty<Vorlageneintrag>(); }
            foreach (Vorlageneintrag e in liste)
                zeilen.Add(new Vorlagenzeile(IdFuer(e.Id), e.Name, false, "", false));

            gewaehlt = null;
            if (wahl == null) return zeilen;
            if (wahl.FehlendeId != null)
            {
                int id = IdFuer(wahl.FehlendeId);
                string hinweis = wahl.Meldungen.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? R.BK_BER_VORLAGE_NICHT_WAEHLBAR;
                if (zeilen.All(z => z.Id != id)) zeilen.Add(new Vorlagenzeile(id, NameAusKennung(wahl.FehlendeId), true, hinweis));
                gewaehlt = id;
            }
            else if (wahl.Eintrag != null)
            {
                gewaehlt = IdFuer(wahl.Eintrag.Id);
            }
            return zeilen;
        }

        /// <summary>Die Prüfzeile der Excel-Vorlage: ohne Vorlage keine, sonst die Schnellprüfung (eine volle derselben Bytes zählt).</summary>
        private Pruefstand ExcelPruefzeile(Vorlagenwahl wahl, BerichtsKonfiguration konfig)
        {
            if (wahl?.Eintrag == null || IstOhne(wahl.Eintrag))
            {
                string satz = wahl == null ? "" : string.Join(" ", wahl.Meldungen.Where(m => !string.IsNullOrWhiteSpace(m)));
                return satz.Length == 0 ? null : new Pruefstand(SYMBOL_WARNUNG, satz);
            }
            Pruefbefund befund;
            try { befund = _vorlagen.Pruefe(wahl.Eintrag, Pruefstufe.Schnell, Kontext(konfig)); }
            catch (Exception ex) { return new Pruefstand(SYMBOL_FEHLER, Format(R.BV_VORLAGEN_NICHT_LESBAR, wahl.Eintrag.Name, ex.Message)); }
            if (_vollExcel != null && string.Equals(_vollExcelId, wahl.Eintrag.Id, StringComparison.Ordinal)
                && string.Equals(_vollExcel.Pruefsumme, befund.Pruefsumme, StringComparison.OrdinalIgnoreCase))
                befund = _vollExcel;
            return Zeile(befund);
        }

        /// <summary>Die Seite meldet eine andere Excel-Vorlage: die Abweichung des Stammprojekts setzen und speichern.</summary>
        internal Task ExcelVorlageGewaehlt(int? id)
        {
            if (!id.HasValue)
            {
                WaehleExcel(null);
                return Task.CompletedTask;
            }
            Vorlageneintrag e = _kennungen.TryGetValue(id.Value, out string kennung) ? _vorlagen.FindeExcel(kennung) : null;
            if (e == null) _fehler = R.BK_BER_VORLAGE_MSG_UNBEKANNT;
            else WaehleExcel(e);
            return Task.CompletedTask;
        }

        /// <summary>Speichert die Excel-Wahl als Abweichung des Stammprojekts; die Vorgabe der Installation braucht keine.</summary>
        private void WaehleExcel(Vorlageneintrag e)
        {
            BerichtsKonfiguration konfig = Lade();
            if (e == null || string.Equals(e.Id, _vorlagen.VorgabeExcelId, StringComparison.OrdinalIgnoreCase))
                BerichtsvorlagenCtrl.EntferneAbweichungExcel(konfig);
            else
                BerichtsvorlagenCtrl.SetzeAbweichungExcel(konfig, e);

            bool gespeichert;
            try { gespeichert = _bericht.Speichere(_idStamm, konfig); }
            catch (Exception) { gespeichert = false; }
            if (!gespeichert) _fehler = R.BK_BER_VORLAGE_MSG_NICHT_GESPEICHERT;
        }

        /// <summary>Die volle Prüfung einer Excel-Vorlage (Paketschutz); die Excel-Prüfzeile zeigt sie.</summary>
        private Pruefbefund PruefeVollExcel(Vorlageneintrag e)
        {
            if (e == null || IstOhne(e)) return null;
            try
            {
                _vollExcel = _vorlagen.Pruefe(e, Pruefstufe.Voll, Kontext(Lade()));
                _vollExcelId = e.Id;
            }
            catch (Exception ex)
            {
                _vollExcel = null;
                _vollExcelId = null;
                _fehler = Format(R.BV_VORLAGEN_NICHT_LESBAR, e.Name, ex.Message);
            }
            return _vollExcel;
        }

        /// <summary>Der Parametersatz der Prüfliste der gewählten Excel-Vorlage — die VOLLE Prüfung samt Paketschutz.</summary>
        internal IReadOnlyDictionary<string, object> ExcelPrueflisteGaben()
        {
            Vorlagenwahl wahl = ExcelWahl(Lade());
            if (wahl?.Eintrag == null || IstOhne(wahl.Eintrag)) return null;
            return Pruefliste(PruefeVollExcel(wahl.Eintrag), wahl.Eintrag.Name);
        }

        private Vorlagenwahl ExcelWahl(BerichtsKonfiguration konfig)
        {
            try { return _vorlagen.ExcelVorlageFuer(konfig); }
            catch (Exception) { return null; }
        }

        private static bool IstOhne(Vorlageneintrag e)
        {
            return e != null && string.Equals(e.Id, BerichtsvorlagenCtrl.ID_OHNE, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Das Menü „…" zur gewählten Vorlage — aus den belegten Wegen der Plattform und der Quelle
        /// der Vorlage. Ein Eintrag, der gerade nicht geht, bleibt stehen und trägt seinen Grund.
        /// </summary>
        internal IReadOnlyList<Handlung> Handlungen(Vorlageneintrag e)
        {
            var liste = new List<Handlung>();
            if (e == null) return liste;
            Berichtsvorlagenwege wege = Wege;

            if (e.IstStandard)
            {
                bool da = Lesepfad(e) != null;
                string grund = da ? "" : Format(R.BV_VORLAGEN_FEHLT, e.Name);
                if (wege.SchreibgeschuetztOeffnen != null)
                    liste.Add(new Handlung(HANDLUNG_SCHREIBGESCHUETZT, R.BK_BER_VORLAGE_HANDLUNG_SCHREIBGESCHUETZT, da, grund,
                                           R.BK_BER_VORLAGE_TIP_SCHREIBGESCHUETZT));
                else
                    liste.Add(new Handlung(HANDLUNG_TEILEN, R.BK_BER_VORLAGE_HANDLUNG_TEILEN, da, grund,
                                           R.BK_BER_VORLAGE_TIP_TEILEN));
                return liste;
            }

            bool inWord = false;
            try { inWord = _vorlagen.IstInWordGeoeffnet(e); } catch (Exception) { inWord = false; }
            string sperre = inWord ? R.BV_VORLAGEN_IN_WORD : "";

            if (wege.InWordOeffnen != null)
                liste.Add(new Handlung(HANDLUNG_WORD, R.BK_BER_VORLAGE_HANDLUNG_WORD, Kurztext: R.BK_BER_VORLAGE_TIP_WORD));
            else
                liste.Add(new Handlung(HANDLUNG_TEILEN, R.BK_BER_VORLAGE_HANDLUNG_TEILEN, Kurztext: R.BK_BER_VORLAGE_TIP_TEILEN));
            if (wege.ImOrdnerZeigen != null)
                liste.Add(new Handlung(HANDLUNG_ORDNER, R.BK_BER_VORLAGE_HANDLUNG_ORDNER, Kurztext: R.BK_BER_VORLAGE_TIP_ORDNER));
            liste.Add(new Handlung(HANDLUNG_ERSETZEN, R.BK_BER_VORLAGE_HANDLUNG_ERSETZEN, !inWord, sperre,
                                   R.BK_BER_VORLAGE_TIP_ERSETZEN));
            liste.Add(new Handlung(HANDLUNG_ENTFERNEN, R.BK_BER_VORLAGE_HANDLUNG_ENTFERNEN, !inWord, sperre,
                                   R.BK_BER_VORLAGE_TIP_ENTFERNEN, Format(R.BK_BER_VORLAGE_FRAGE_ENTFERNEN, e.Name)));
            return liste;
        }

        /// <summary>
        /// Die Prüfzeile (Konzept 9.7): „geprüft, n Platzhalter, keine Befunde", „…, n Befunde" mit
        /// „anzeigen", „In Word geöffnet – ungespeicherte Änderungen fehlen", die Lesefehler und — ohne
        /// Standardvorlage — der benannte Rückfall. Gilt eine volle Prüfung derselben Bytes, zählt sie.
        /// </summary>
        private Pruefstand Pruefzeile(Startbefund start, Vorlagenwahl wahl, string prueffehler)
        {
            if (prueffehler != null)
                return new Pruefstand(SYMBOL_FEHLER, Format(R.BV_VORLAGEN_NICHT_LESBAR, wahl?.Eintrag?.Name ?? "", prueffehler));

            Pruefbefund befund = start?.Pruefbefund;
            if (befund == null)
            {
                // Die Standardvorlage selbst fehlt — es gibt nichts zu prüfen, der Lauf nimmt den
                // bisherigen Weg und sagt es.
                string satz = wahl == null ? "" : string.Join(" ", wahl.Meldungen.Where(m => !string.IsNullOrWhiteSpace(m)));
                return satz.Length == 0 ? null : new Pruefstand(SYMBOL_WARNUNG, satz);
            }

            bool inWord = IstInWord(befund);
            if (_voll != null && wahl?.Eintrag != null && string.Equals(_vollId, wahl.Eintrag.Id, StringComparison.Ordinal)
                && string.Equals(_voll.Pruefsumme, befund.Pruefsumme, StringComparison.OrdinalIgnoreCase)
                && IstInWord(_voll) == inWord)
                befund = _voll;

            return Zeile(befund);
        }

        /// <summary>Die Prüfzeile zu einem Befund.</summary>
        internal static Pruefstand Zeile(Pruefbefund befund)
        {
            if (befund == null) return null;
            if (!befund.IstLesbar)
            {
                string text = befund.Meldungen.FirstOrDefault()?.Text ?? "";
                return new Pruefstand(SYMBOL_FEHLER, text, befund.Meldungen.Count > 0);
            }

            int n = befund.Meldungen.Count;
            if (n == 0) return new Pruefstand(SYMBOL_OK, Format(R.BV_VORLAGEN_PRUEFZEILE_OK, befund.AnzahlPlatzhalter));

            string symbol = befund.HatFehler ? SYMBOL_FEHLER : befund.Warnungen > 0 ? SYMBOL_WARNUNG : SYMBOL_HINWEIS;
            string zeile = IstInWord(befund)
                ? R.BV_VORLAGEN_IN_WORD
                : n == 1
                    ? Format(R.BV_VORLAGEN_PRUEFZEILE_BEFUND, befund.AnzahlPlatzhalter)
                    : Format(R.BV_VORLAGEN_PRUEFZEILE_BEFUNDE, befund.AnzahlPlatzhalter, n);
            return new Pruefstand(symbol, zeile, true);
        }

        private static bool IstInWord(Pruefbefund befund)
        {
            return befund != null && befund.Meldungen.Any(m => string.Equals(m.Kennung, nameof(R.BV_VORLAGEN_IN_WORD), StringComparison.Ordinal));
        }

        /// <summary>
        /// Die erweiterte Startrückfrage (Konzept 10.2, BV-Q6) — nur, wenn die Vorprüfung sie braucht.
        /// Der Text trägt <c>{0}</c> für die Zahl der Versionen, die die Seite beim Start einsetzt; die
        /// Befunde stehen darunter. Die Wege folgen dem Befund: Ließ sich die gewählte Vorlage nicht
        /// lesen oder IST sie die Standardvorlage, bleiben „Mit Standardvorlage" und „Abbrechen".
        /// </summary>
        internal static Startrueckfrage Rueckfrage(Startbefund start)
        {
            return Rueckfrage(start, null);
        }

        /// <summary>
        /// Die erweiterte Startrückfrage mit den Befunden der Excel-Vorlage (Anwenderentscheid BV-E7-3): Hat die gewählte
        /// Excel-Vorlage Fehler, stehen sie in DERSELBEN Rückfrage — unter den Befunden der Word-Vorlage, je mit
        /// „Excel-Vorlage:“ davor. Die Wege bleiben dieselben: „Mit meiner Vorlage“ füllt beide gewählten Vorlagen, der
        /// zweite Weg nimmt für diesen Lauf die Standardvorlage (Word, nur wenn sie selbst befragt ist) und erzeugt die
        /// Mappe ohne Vorlage (Excel); Abbrechen. Braucht nur die Excel-Vorlage die Rückfrage, heißt der zweite Weg
        /// „Ohne Excel-Vorlage“. „Mit meiner Vorlage“ steht, solange jede befragte Vorlage lesbar ist.
        /// </summary>
        internal static Startrueckfrage Rueckfrage(Startbefund start, Excelstartbefund excel)
        {
            bool wordFrage = start != null && start.BrauchtRueckfrage && start.Wahl?.Eintrag != null;
            bool excelFrage = excel != null && excel.BrauchtRueckfrage && excel.Wahl?.Eintrag != null;
            if (!excelFrage) return RueckfrageWord(start);

            var text = new System.Text.StringBuilder();
            if (wordFrage)
            {
                text.Append(Format(R.BV_START_KOPF, "{0}", start.Wahl.Eintrag.Name));
                foreach (string m in start.Wahl.Meldungen)
                    if (!string.IsNullOrWhiteSpace(m)) text.Append("\r\n").Append(m);
                if (start.KannGewaehlteFuellen && start.HatFehler) text.Append("\r\n\r\n").Append(R.BV_START_GELB);
                text.Append("\r\n\r\n");
            }
            else
            {
                text.Append(Format(R.BV_XL_START_KOPF, "{0}", excel.Wahl.Eintrag.Name)).Append("\r\n\r\n");
            }
            text.Append(Format(R.BV_XL_START_FEHLER, excel.Wahl.Eintrag.Name));
            text.Append("\r\n\r\n").Append(R.BV_START_BEFUNDE);

            List<string> punkte = new List<string>(wordFrage ? Punkte(start) : Array.Empty<string>());
            punkte.AddRange(Punkte(excel));

            bool eigene = excel.KannGewaehlteFuellen &&
                          (!wordFrage || (start.KannGewaehlteFuellen && start.StandardAngeboten));
            string wegEigene = wordFrage ? start.WegGewaehlt : R.BV_START_WEG_EIGENE;
            string wegZwei = wordFrage ? start.WegStandard : R.BV_XL_START_WEG_OHNE;
            string wegAbbrechen = wordFrage ? start.WegAbbrechen : R.BV_START_WEG_ABBRECHEN;
            return new Startrueckfrage(R.BK_BER_TITEL_ERSTELLEN, text.ToString(), punkte, wegEigene, wegZwei, wegAbbrechen, eigene);
        }

        /// <summary>Die Befunde der Excel-Vorlage als Zeilen der Rückfrage (gekappt wie die der Word-Vorlage).</summary>
        internal static IReadOnlyList<string> Punkte(Excelstartbefund excel)
        {
            List<string> punkte = (excel?.Befunde ?? Array.Empty<Berichtsmeldung>())
                .Select(b => b.Text).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (punkte.Count <= BerichtCtrl.MAX_PUNKTE) return punkte;
            var gekappt = punkte.Take(BerichtCtrl.MAX_PUNKTE).ToList();
            gekappt.Add(Format(R.BV_LAUF_WEITERE, punkte.Count - BerichtCtrl.MAX_PUNKTE));
            return gekappt;
        }

        /// <summary>Die erweiterte Startrückfrage allein aus dem Befund der Word-Vorlage.</summary>
        private static Startrueckfrage RueckfrageWord(Startbefund start)
        {
            if (start == null || !start.BrauchtRueckfrage || start.Wahl?.Eintrag == null) return null;

            var text = new System.Text.StringBuilder();
            text.Append(Format(R.BV_START_KOPF, "{0}", start.Wahl.Eintrag.Name));
            foreach (string m in start.Wahl.Meldungen)
                if (!string.IsNullOrWhiteSpace(m)) text.Append("\r\n").Append(m);
            if (start.KannGewaehlteFuellen && start.HatFehler) text.Append("\r\n\r\n").Append(R.BV_START_GELB);
            text.Append("\r\n\r\n").Append(R.BV_START_BEFUNDE);

            bool eigene = start.KannGewaehlteFuellen && start.StandardAngeboten;
            return new Startrueckfrage(R.BK_BER_TITEL_ERSTELLEN, text.ToString(), Punkte(start),
                                       start.WegGewaehlt, start.WegStandard, start.WegAbbrechen, eigene);
        }

        // =====================================================================
        //  BV-E2 — die Häkchen folgen der Vorlage (Konzept 10.2, „Häkchen (BV-Q1 c)")
        // =====================================================================

        /// <summary>
        /// <b>Was die geprüfte Word-Vorlage an Kapiteln führt</b> — aus der Schnellprüfung
        /// (<see cref="Pruefbefund.Bausteine"/>, <see cref="Pruefbefund.HatKapitel"/>,
        /// <see cref="Pruefbefund.DeckblattAusPlatzhaltern"/>). Die Häkchen schalten Kapitelplatzhalter,
        /// keine Einzelplatzhalter: Ein Baustein, den kein Kapitel der Vorlage einsetzt, ist „in dieser
        /// Vorlage nicht enthalten" — außer dem Deckblatt, das die Vorlage aus Platzhaltern selbst trägt:
        /// Es „kommt aus der Vorlage". Führt die Vorlage gar kein Kapitel (weder <c>{{bericht.inhalt}}</c>
        /// noch <c>kapitel.*</c>), bestimmt sie den Inhalt allein.
        /// <c>null</c> = jeder Eintrag frei: Es ist keine Vorlage geprüft (die Standardvorlage fehlt, der
        /// Lauf nimmt den bisherigen Weg), die Vorlage ist nicht lesbar (der Lauf fällt auf die
        /// Standardvorlage) oder sie trägt keinen Platzhalter (der Bericht kommt an ihr Ende, als stünde
        /// dort <c>{{bericht.inhalt}}</c>).
        /// </summary>
        internal static Kapitelstand Kapitel(Pruefbefund befund)
        {
            if (befund == null) return null;
            return Kapitel(befund.IstLesbar, befund.AnzahlPlatzhalter, befund.HatKapitel, befund.Bausteine,
                           befund.DeckblattAusPlatzhaltern);
        }

        /// <summary>
        /// Die Regel von <see cref="Kapitel(Pruefbefund)"/> auf ihren fünf Größen: lesbar, Zahl der
        /// Platzhalter, führt Kapitel, die Bausteine der Kapitel, Deckblatt aus Platzhaltern. Ohne Kapitel
        /// führt die Vorlage keinen Baustein — auch keinen, den ein Einzelplatzhalter berührt. Das
        /// Deckblatt aus Platzhaltern zählt nur, wo kein Kapitel Deckblatt es einsetzt.
        /// </summary>
        internal static Kapitelstand Kapitel(bool lesbar, int platzhalter, bool hatKapitel, IEnumerable<string> bausteine,
                                             bool deckblattAusPlatzhaltern = false)
        {
            if (!lesbar || platzhalter <= 0) return null;
            var gefuehrt = hatKapitel
                ? new HashSet<string>(bausteine ?? Enumerable.Empty<string>(), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            string deckblatt = deckblattAusPlatzhaltern && !gefuehrt.Contains(BerichtsKonfiguration.B_DECKBLATT)
                ? BerichtsKonfiguration.B_DECKBLATT : null;
            List<string> fehlen = BerichtsKonfiguration.AlleBausteine
                .Select(b => b.Schluessel)
                .Where(s => !gefuehrt.Contains(s) && !string.Equals(s, deckblatt, StringComparison.Ordinal))
                .ToList();
            return new Kapitelstand(fehlen, !hatKapitel, deckblatt);
        }

        // =====================================================================
        //  BV-E2 — die Stellen der Anhang-E-Checkliste (Konzept 9.5, 11 Nr. 3)
        // =====================================================================

        /// <summary>
        /// <b>Die Stellen der Anhang-E-Checkliste in der gewählten Word-Vorlage</b> — für die Überlagerung
        /// der Wirtschaftlichkeitsseite. Der Kern nennt die Kapitelstellen der Vorlage (Delegat
        /// <see cref="Kapitelstellen"/>), seine Checkliste macht daraus je Punkt die Spalte „Stelle"
        /// (<see cref="Stellen"/>) — derselbe Weg wie im Bericht. Gefragt wird mit der gespeicherten
        /// Konfiguration samt Vorlagenwahl und mit den Häkchen des zweiten Einstiegs
        /// (<see cref="BerichtSeiteGaben.BausteineFuerVergleich"/>): Die Checkliste steht nur in einem
        /// Bericht mit Wirtschaftlichkeit, und „Bericht erzeugen" neben der Überlagerung setzt sie. Die
        /// leise Zeile nennt eine eigene Vorlage beim Namen; mit der Standardvorlage — auch als Ersatz
        /// einer fehlenden Vorlage — heißt sie „bezogen auf die Standardvorlage". Ohne Delegat oder wenn
        /// der Kern wirft oder schweigt: keine Stellen, die Überlagerung nimmt ihre eigenen.
        /// </summary>
        internal AnhangEStellen AnhangEStellenDerVorlage()
        {
            var standard = new AnhangEStellen(R.WIRT_AE_BEZUG_STANDARD, new Dictionary<string, string>());
            Func<BerichtsKonfiguration, bool, IReadOnlyDictionary<string, string>> stellen = Kapitelstellen;
            if (stellen == null) return standard;

            BerichtsKonfiguration konfig = Lade();
            konfig.AktiveBausteine = BerichtSeiteGaben.BausteineFuerVergleich(konfig);

            IReadOnlyDictionary<string, string> kapitel;
            try { kapitel = stellen(konfig, Englisch); }
            catch (Exception) { return standard; }
            if (kapitel == null) return standard;

            Vorlagenwahl wahl = Wahl(konfig);
            bool eigen = wahl?.Eintrag != null && !wahl.Eintrag.IstStandard && wahl.FehlendeId == null;
            string bezug = eigen ? Format(R.WIRT_AE_BEZUG_VORLAGE, wahl.Eintrag.Name) : R.WIRT_AE_BEZUG_STANDARD;
            return new AnhangEStellen(bezug, Stellen(kapitel));
        }

        /// <summary>
        /// Je Punktnummer (<c>ChecklistenPunkt.Nummer</c>) die Spalte „Stelle" der Checkliste des Kerns
        /// (<see cref="AnhangECheckliste.Punkte(ChecklistenLage, IReadOnlyDictionary{string, string})"/>) zu
        /// diesen Kapitelstellen — die Stelle hängt nicht von der Lage ab.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> Stellen(IReadOnlyDictionary<string, string> kapitelstellen)
        {
            var stellen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ChecklistenPunkt p in AnhangECheckliste.Punkte(new ChecklistenLage(), kapitelstellen))
                stellen[p.Nummer] = p.Stelle;
            return stellen;
        }

        /// <summary>Die Befunde der Rückfrage, höchstens <see cref="BerichtCtrl.MAX_PUNKTE"/> und „… und n weitere".</summary>
        internal static IReadOnlyList<string> Punkte(Startbefund start)
        {
            List<string> punkte = (start?.Befunde ?? Array.Empty<Berichtsmeldung>())
                .Select(b => b.Text).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (punkte.Count <= BerichtCtrl.MAX_PUNKTE) return punkte;
            var gekappt = punkte.Take(BerichtCtrl.MAX_PUNKTE).ToList();
            gekappt.Add(Format(R.BV_LAUF_WEITERE, punkte.Count - BerichtCtrl.MAX_PUNKTE));
            return gekappt;
        }

        // =====================================================================
        //  Die Wahl
        // =====================================================================

        /// <summary>
        /// Die Seite meldet eine andere Vorlage (Auswahlfeld oder Assistent): die Abweichung des
        /// Stammprojekts setzen — bzw. entfernen, wenn die Vorgabe gewählt ist — und speichern.
        /// Eine Id, die die Hülle nicht kennt, oder eine Vorlage, die es nicht mehr gibt, lehnt sie
        /// benannt ab; der Stand danach zeigt die frische Liste.
        /// </summary>
        internal Task VorlageGewaehlt(int? id)
        {
            if (!id.HasValue)
            {
                Waehle(null);
                return Task.CompletedTask;
            }

            Vorlageneintrag e = _kennungen.TryGetValue(id.Value, out string kennung) ? _vorlagen.Finde(kennung) : null;
            if (e == null) _fehler = R.BK_BER_VORLAGE_MSG_UNBEKANNT;
            else Waehle(e);
            return Task.CompletedTask;
        }

        /// <summary>Speichert die Wahl als Abweichung des Stammprojekts; <c>null</c> = keine Abweichung.</summary>
        private void Waehle(Vorlageneintrag e)
        {
            BerichtsKonfiguration konfig = Lade();
            if (e == null || string.Equals(e.Id, _vorlagen.VorgabeWordId, StringComparison.OrdinalIgnoreCase))
                BerichtsvorlagenCtrl.EntferneAbweichung(konfig);
            else
                BerichtsvorlagenCtrl.SetzeAbweichung(konfig, e);

            bool gespeichert;
            try { gespeichert = _bericht.Speichere(_idStamm, konfig); }
            catch (Exception) { gespeichert = false; }
            if (!gespeichert) _fehler = R.BK_BER_VORLAGE_MSG_NICHT_GESPEICHERT;
        }

        // =====================================================================
        //  Die Handlungen des Menüs „…"
        // =====================================================================

        /// <summary>Führt einen Eintrag des Menüs „…" an der gewählten Vorlage aus.</summary>
        internal async Task HandlungAusfuehren(string handlung)
        {
            Vorlagenwahl wahl = Wahl(Lade());
            Vorlageneintrag e = wahl?.FehlendeId == null ? wahl?.Eintrag : null;
            if (e == null)
            {
                _fehler = R.BK_BER_VORLAGE_MSG_UNBEKANNT;
                return;
            }

            Berichtsvorlagenwege wege = Wege;
            switch (handlung)
            {
                case HANDLUNG_WORD:
                    // Die mitgelieferte Vorlage wird nie zum Bearbeiten geöffnet — nur als Kopie.
                    if (e.IstStandard) { _fehler = R.BV_VORLAGEN_SCHREIBGESCHUETZT; return; }
                    if (!Vorhanden(e)) return;
                    if (wege.InWordOeffnen != null && Versuche(wege.InWordOeffnen, e.Pfad))
                        _meldung = Format(R.BK_BER_VORLAGE_MSG_IN_WORD, e.Name);
                    else
                        _fehler = Format(R.BK_BER_VORLAGE_MSG_WORD_FEHLT, e.Name, e.Pfad);
                    return;

                case HANDLUNG_ORDNER:
                    string ordner = Path.GetDirectoryName(e.Pfad) ?? "";
                    if (wege.ImOrdnerZeigen == null || !Versuche(wege.ImOrdnerZeigen, e.Pfad))
                        _fehler = Format(R.BK_BER_VORLAGE_MSG_ORDNER_FEHLER, ordner);
                    return;

                case HANDLUNG_SCHREIBGESCHUETZT:
                    string lesen = Lesepfad(e);
                    if (lesen == null) { _fehler = Format(R.BV_VORLAGEN_FEHLT, e.Name); return; }
                    if (wege.SchreibgeschuetztOeffnen != null && Versuche(wege.SchreibgeschuetztOeffnen, lesen))
                        _meldung = Format(R.BK_BER_VORLAGE_MSG_SCHREIBGESCHUETZT, e.Name);
                    else
                        _fehler = Format(R.BK_BER_VORLAGE_MSG_WORD_FEHLT, e.Name, lesen);
                    return;

                case HANDLUNG_TEILEN:
                    string teilen = e.IstStandard ? Lesepfad(e) : (File.Exists(e.Pfad) ? e.Pfad : null);
                    if (teilen == null) { _fehler = Format(R.BV_VORLAGEN_FEHLT, e.Name); return; }
                    if (!Versuche(p => Dienste.Datei.MitSystemOeffnen(p), teilen))
                        _fehler = Format(R.BK_BER_VORLAGE_MSG_TEILEN_FEHLER, e.Name);
                    return;

                case HANDLUNG_ERSETZEN:
                    await Ersetzen(e);
                    return;

                case HANDLUNG_ENTFERNEN:
                    Entfernen(e, wahl);
                    return;

                default:
                    _fehler = R.BK_BER_VORLAGE_MSG_UNBEKANNT;
                    return;
            }
        }

        /// <summary>„Ersetzen…": Dateiwahl, dann <see cref="BerichtsvorlagenCtrl.Ersetzen"/> und die volle Prüfung.</summary>
        private async Task Ersetzen(Vorlageneintrag e)
        {
            string quelle = await Dateiwahl(Format(R.BK_BER_VORLAGE_DLG_ERSETZEN, e.Name));
            if (string.IsNullOrWhiteSpace(quelle)) return;

            Vorlagenergebnis r = _vorlagen.Ersetzen(quelle, e);
            if (!r.Erfolg) { _fehler = r.Meldung; return; }
            PruefeVoll(r.Eintrag ?? e);
            _meldung = r.Meldung;
        }

        /// <summary>„Entfernen": in den Unterordner „Entfernt"; war sie die Wahl des Stammprojekts, fällt die Abweichung.</summary>
        private void Entfernen(Vorlageneintrag e, Vorlagenwahl wahl)
        {
            Vorlagenergebnis r = _vorlagen.Entfernen(e);
            if (!r.Erfolg) { _fehler = r.Meldung; return; }
            if (wahl.Grund == Vorlagenwahlgrund.Abweichung) Waehle(null);
            _meldung = r.Meldung;
        }

        /// <summary>Ein Weg der Plattform — eine Ausnahme ist ein „ging nicht".</summary>
        private static bool Versuche(Func<string, bool> weg, string pfad)
        {
            try { return weg(pfad); }
            catch (Exception) { return false; }
        }

        /// <summary>Liegt die Datei vor? Sonst die benannte Meldung.</summary>
        private bool Vorhanden(Vorlageneintrag e)
        {
            if (File.Exists(e.Pfad)) return true;
            _fehler = Format(R.BV_VORLAGEN_FEHLT, e.Name);
            return false;
        }

        // =====================================================================
        //  „Neue Vorlage…", „Hinzufügen…", „Prüfen"
        // =====================================================================

        /// <summary>„Neue Vorlage…": die Kopie der Standardvorlage unter dem Namen — und gewählt.</summary>
        internal Task NeueVorlageAnlegen(string name)
        {
            return NeueVorlageAusMuster(new Neuvorlage(name, (int)WindowsFormsApplication1.Vorlagenmuster.Standard));
        }

        /// <summary>
        /// Die Muster von „Neue Vorlage…“ (Konzept 10.2, BV-E5): die Standardvorlage und der Kurzbericht in der Sprache der
        /// Oberfläche — Kennung ist der Wert von <see cref="WindowsFormsApplication1.Vorlagenmuster"/>. Der Kurzbericht
        /// steht nur da, wenn seine Datei mitgeliefert ist.
        /// </summary>
        internal IReadOnlyList<(int Id, string Text)> Mustereintraege()
        {
            var muster = new List<(int Id, string Text)> { ((int)WindowsFormsApplication1.Vorlagenmuster.Standard, R.BK_BER_VORLAGE_NEU_MUSTER_STANDARD) };
            if (_vorlagen.Musterpfad(WindowsFormsApplication1.Vorlagenmuster.Kurzbericht, Englisch) != null)
                muster.Add(((int)WindowsFormsApplication1.Vorlagenmuster.Kurzbericht, R.BK_BER_VORLAGE_NEU_MUSTER_KURZBERICHT));
            return muster;
        }

        /// <summary>
        /// „Neue Vorlage…“ aus einem Muster: die Kopie der Standardvorlage oder des Kurzberichts in der Sprache der
        /// Oberfläche unter dem Namen — und gewählt.
        /// </summary>
        internal Task NeueVorlageAusMuster(Neuvorlage wahl)
        {
            string name = wahl?.Name;
            var muster = Enum.IsDefined(typeof(WindowsFormsApplication1.Vorlagenmuster), wahl?.Muster ?? 0)
                ? (WindowsFormsApplication1.Vorlagenmuster)(wahl?.Muster ?? 0)
                : WindowsFormsApplication1.Vorlagenmuster.Standard;
            Vorlagenergebnis r = _vorlagen.NeueVorlage(name, muster, Englisch);
            if (!r.Erfolg)
            {
                _fehler = r.Art == Vorlagenergebnisart.NameVergeben
                    ? Format(R.BK_BER_VORLAGE_NAME_VORHANDEN, Stamm(name))
                    : r.Meldung;
                return Task.CompletedTask;
            }
            Waehle(r.Eintrag);
            _meldung = r.Meldung;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Die Namensprüfung des Namensdialogs: leer, verbotene Zeichen, kein gültiger Dateiname,
        /// im Vorlagenordner schon vorhanden. <c>null</c> = der Name ist frei.
        /// </summary>
        internal string NamePruefen(string name)
        {
            string n = (name ?? "").Trim();
            if (n.Length == 0) return R.BK_BER_VORLAGE_NEU_LEER;
            if (BerichtsvorlagenCtrl.HatVerboteneZeichen(n)) return R.BK_BER_VORLAGE_NAME_ZEICHEN;
            if (!BerichtsvorlagenCtrl.IstGueltigerName(n)) return Format(R.BV_VORLAGEN_NAME_UNGUELTIG, n);

            string stamm = Stamm(n);
            bool vergeben = Liste().Any(e => !e.IstStandard && string.Equals(e.Name, stamm, StringComparison.OrdinalIgnoreCase));
            if (!vergeben)
            {
                try { vergeben = File.Exists(Path.Combine(_vorlagen.Vorlagenordner, stamm + ".docx")); }
                catch (Exception) { vergeben = false; }
            }
            return vergeben ? Format(R.BK_BER_VORLAGE_NAME_VORHANDEN, stamm) : null;
        }

        /// <summary>
        /// „Hinzufügen…": die Dateiwahl der Plattform (Word-Dokumente und -Vorlagen), kopieren, voll
        /// prüfen und wählen. Gibt es den Namen im Vorlagenordner schon, kommt die Vorlage unter einem
        /// freien Namen hinzu („Angebot (2)") — ersetzen geht ausdrücklich über „…" › „Ersetzen…".
        /// </summary>
        internal async Task Hinzufuegen()
        {
            string quelle = await Dateiwahl(R.BK_BER_VORLAGE_DLG_HINZUFUEGEN);
            if (string.IsNullOrWhiteSpace(quelle)) return;

            Vorlagenergebnis r = _vorlagen.Hinzufuegen(quelle);
            string meldung = r.Meldung;
            if (r.Art == Vorlagenergebnisart.NameVergeben)
            {
                string frei = FreierName(quelle);
                Vorlagenergebnis als = _vorlagen.HinzufuegenAls(quelle, frei);
                if (als.Erfolg)
                    meldung = Format(R.BK_BER_VORLAGE_MSG_UMBENANNT, r.Vorhandener?.Name ?? Stamm(quelle), als.Eintrag?.Name ?? frei);
                r = als;
            }
            if (!r.Erfolg) { _fehler = r.Meldung; return; }

            // BV-E7: eine Excel-Vorlage wird die Excel-Wahl, nicht die Word-Wahl.
            if (BerichtsvorlagenCtrl.IstExcel(r.Eintrag))
            {
                WaehleExcel(r.Eintrag);
                PruefeVollExcel(r.Eintrag);
            }
            else
            {
                Waehle(r.Eintrag);
                PruefeVoll(r.Eintrag);
            }
            _meldung = meldung;
        }

        /// <summary>„Prüfen": die volle Prüfung der gewählten Vorlage; die Prüfzeile zeigt sie.</summary>
        internal Task Pruefen()
        {
            Vorlagenwahl wahl = Wahl(Lade());
            if (wahl?.Eintrag != null) PruefeVoll(wahl.Eintrag);
            return Task.CompletedTask;
        }

        private Pruefbefund PruefeVoll(Vorlageneintrag e)
        {
            if (e == null) return null;
            try
            {
                _voll = _vorlagen.Pruefe(e, Pruefstufe.Voll, Kontext(Lade()));
                _vollId = e.Id;
            }
            catch (Exception ex)
            {
                _voll = null;
                _vollId = null;
                _fehler = Format(R.BV_VORLAGEN_NICHT_LESBAR, e.Name, ex.Message);
            }
            return _voll;
        }

        /// <summary>Ein freier Name für eine Quelle, deren Name im Vorlagenordner schon steht: „Name (2)", „Name (3)" …</summary>
        private string FreierName(string quelle)
        {
            string stamm = Stamm(quelle);
            string endung = Path.GetExtension(quelle).ToLowerInvariant();
            string ordner = _vorlagen.Vorlagenordner;
            for (int n = 2; n < 1000; n++)
            {
                string kandidat = stamm + " (" + n.ToString(CultureInfo.InvariantCulture) + ")";
                if (!File.Exists(Path.Combine(ordner, kandidat + endung))) return kandidat;
            }
            return stamm + " (" + Guid.NewGuid().ToString("N").Substring(0, 6) + ")";
        }

        /// <summary>Die Dateiwahl der Plattform für Word-Dokumente und -Vorlagen; <c>""</c> = abgebrochen.</summary>
        private static async Task<string> Dateiwahl(string titel)
        {
            string start = "";
            try { start = Dienste.Pfade.Dokumente ?? ""; } catch (Exception) { start = ""; }
            try { return await Dienste.Datei.DateiOeffnenAsync(titel, R.BK_BER_VORLAGE_DATEIFILTER, start) ?? ""; }
            catch (Exception) { return ""; }
        }

        // =====================================================================
        //  Prüfliste und Platzhalterkatalog
        // =====================================================================

        /// <summary>
        /// Der Parametersatz der Überlagerung „Prüfliste" — die VOLLE Prüfung der gewählten Vorlage,
        /// je Meldung Stufe, Text, Fundort, „Was tun" und die Kennung für „erklären lassen".
        /// </summary>
        internal IReadOnlyDictionary<string, object> PrueflisteGaben()
        {
            Vorlagenwahl wahl = Wahl(Lade());
            if (wahl?.Eintrag == null) return null;
            return Pruefliste(PruefeVoll(wahl.Eintrag), wahl.Eintrag.Name);
        }

        /// <summary>Der Parametersatz der Überlagerung „Prüfliste" zu einem Befund — Word und Excel gleich.</summary>
        private static IReadOnlyDictionary<string, object> Pruefliste(Pruefbefund befund, string vorlagenname)
        {
            if (befund == null) return null;

            var erklaerbar = new HashSet<string>(KiMeldungskennung.Berichtsvorlagen, StringComparer.Ordinal);
            var gaben = new Dictionary<string, object>
            {
                ["Meldungen"] = befund.Meldungen.Select(m => new Pruefmeldungszeile(
                    Stufe(m.Stufe), m.Text, m.Fundort, m.WasTun, erklaerbar.Contains(m.Kennung) ? m.Kennung : "")).ToList(),
                ["Vorlagenname"] = vorlagenname,
                ["Texte"] = new PrueflisteTexte(),
                ["HilfeSchluessel"] = HILFE_PRUEFLISTE
            };
            if (befund.IstLesbar) gaben["Platzhalterzahl"] = befund.AnzahlPlatzhalter;
            return gaben;
        }

        private static string Stufe(Befundstufe stufe)
        {
            switch (stufe)
            {
                case Befundstufe.Fehler: return Meldungsstufe.Fehler;
                case Befundstufe.Warnung: return Meldungsstufe.Warnung;
                default: return Meldungsstufe.Hinweis;
            }
        }

        /// <summary>
        /// Der Parametersatz der Überlagerung „Platzhalterkatalog": die Einträge des Katalogs dieser
        /// Programmfassung (<see cref="Vorlagenfeld.Seit"/> ≤ <see cref="Vorlagenfeldkatalog.Katalogfassung"/>)
        /// mit Art, Kontext und Beschreibung in der Sprache der Oberfläche.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben()
        {
            bool englisch = Englisch;
            List<Katalogzeile> zeilen = Vorlagenfeldkatalog.Alle
                .Where(f => f != null && f.Seit <= Vorlagenfeldkatalog.Katalogfassung)
                .Select(f => new Katalogzeile(f.Schluessel, Arttext(f.Art), Kontexttext(f.Kontext),
                                              Vorlagenfeldkatalog.Beschreibung(f, englisch), Beispiel(f)))
                .ToList();
            return new Dictionary<string, object>
            {
                ["Eintraege"] = zeilen,
                ["Texte"] = new PlatzhalterkatalogTexte(),
                ["HilfeSchluessel"] = HILFE_PLATZHALTERKATALOG,
                ["BaukastenSpeichern"] = new Func<Task<string>>(() => BaukastenSpeichern())
            };
        }

        /// <summary>
        /// „Baukasten speichern…" (Konzept 6.3 Nr. 3, 9.7; BV-E5): der Speichern-Dialog der Plattform über
        /// <c>Dienste.Datei</c> (Windows der Dialog, iOS ein Pfad in den Dokumenten), dann erzeugt der Kern den
        /// Baukasten in der Sprache der Oberfläche und schreibt ihn — abseits des Oberflächenfadens. Rückgabe: die
        /// Meldung für die Fußleiste; <c>""</c> = abgebrochen. Den Excel-Baukasten gibt es erst mit der Ausgabe Excel.
        /// <para><b>iOS</b> (Anwenderentscheid BV-E5-5): Nach dem Schreiben öffnet das Teilen-Blatt über
        /// <c>Dienste.Datei.MitSystemOeffnen</c> — derselbe Weg wie beim gbXML-Export (<c>GebaeudeExportHuelle</c>);
        /// scheitert es, nennt die Meldung den Pfad (<c>VF_BAUKASTEN_TEILEN_FEHLER</c>). Windows bleibt beim Speichern.</para>
        /// </summary>
        /// <param name="speichern">Der Schreibweg; <c>null</c> = <see cref="BerichtsvorlagenCtrl.SpeichereBaukasten"/> (Prüfstand).</param>
        /// <param name="ios">Teilen nach dem Speichern; <c>null</c> = <see cref="OperatingSystem.IsIOS"/> (Prüfstand).</param>
        internal static async Task<string> BaukastenSpeichern(Func<string, bool, Vorlagenergebnis> speichern = null, bool? ios = null)
        {
            bool englisch = Englisch;
            string vorschlag = R.VF_BAUKASTEN_DATEINAME + ".docx";
            try
            {
                string dokumente = Dienste.Pfade.Dokumente ?? "";
                if (dokumente.Length > 0) vorschlag = Path.Combine(dokumente, vorschlag);
            }
            catch (Exception) { /* ohne Ordner nur der Name */ }

            string pfad;
            try { pfad = await Dienste.Datei.DateiSpeichernAsync(R.VF_BAUKASTEN_DIALOGTITEL, R.VF_BAUKASTEN_DATEIFILTER, vorschlag) ?? ""; }
            catch (Exception ex) { return Format(R.VF_BAUKASTEN_FEHLER, ex.Message); }
            if (string.IsNullOrWhiteSpace(pfad)) return "";

            Func<string, bool, Vorlagenergebnis> weg = speichern ?? BerichtsvorlagenCtrl.SpeichereBaukasten;
            try
            {
                Vorlagenergebnis e = await Kulturweitergabe.Starten(() => weg(pfad, englisch));
                if (e == null) return "";
                if (!e.Erfolg || !(ios ?? OperatingSystem.IsIOS())) return e.Meldung;

                string datei = e.Zielpfad ?? pfad;
                bool geteilt;
                try { geteilt = Dienste.Datei.MitSystemOeffnen(datei); }
                catch (Exception) { geteilt = false; }
                return geteilt ? e.Meldung : Format(R.VF_BAUKASTEN_TEILEN_FEHLER, datei);
            }
            catch (Exception ex) { return Format(R.VF_BAUKASTEN_FEHLER, ex.Message); }
        }

        /// <summary>Die Art als Anzeigetext (<c>VF_KATALOG_ART_*</c>).</summary>
        internal static string Arttext(Vorlagenfeldart art)
        {
            return Ressource("VF_KATALOG_ART_" + art.ToString().ToUpperInvariant(), art.ToString());
        }

        /// <summary>Der Kontext als Anzeigetext (<c>VF_KATALOG_KONTEXT_*</c>).</summary>
        internal static string Kontexttext(Vorlagenfeldkontext kontext)
        {
            return Ressource("VF_KATALOG_KONTEXT_" + kontext.ToString().ToUpperInvariant(), kontext.ToString());
        }

        private static string Beispiel(Vorlagenfeld feld)
        {
            return string.IsNullOrEmpty(feld.BeispielId) ? "" : Ressource(feld.BeispielId, "");
        }

        // =====================================================================
        //  Start und Lauf
        // =====================================================================

        /// <summary>
        /// Die Antwort der erweiterten Startrückfrage. „abbruch" gibt die gehaltenen Bytes frei; die
        /// übrigen Wege kommen mit dem Auftrag noch einmal (<see cref="BerichtAuftrag.Vorlagenweg"/>).
        /// </summary>
        internal Task StartGewaehlt(string weg)
        {
            if (string.Equals(weg, UiStartweg.Abbruch, StringComparison.Ordinal)) { _start = null; _excelStart = null; }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Der Startbefund des Laufs: der gehaltene der letzten Vorprüfung, wenn er zu diesem Lauf
        /// passt — dieselbe Vorlage mit demselben Grund, dieselbe Sprache, dieselbe Zahl der
        /// Versionen —, sonst eine frische Vorprüfung. Einmal abgeholt, ist er fort.
        /// </summary>
        internal Startbefund StartFuerLauf(BerichtsKonfiguration konfig, bool englisch, int sicht,
                                           bool erzwingtWirtschaftlichkeit)
        {
            Startbefund gehalten = _start;
            _start = null;

            if (!erzwingtWirtschaftlichkeit && gehalten?.Wahl?.Eintrag != null && gehalten.Englisch == englisch
                && gehalten.AnzahlProjekte == (konfig?.VariantenIds?.Count ?? 0) + 1)
            {
                Vorlagenwahl jetzt = Wahl(konfig);
                if (jetzt?.Eintrag != null
                    && string.Equals(jetzt.Eintrag.Id, gehalten.Wahl.Eintrag.Id, StringComparison.Ordinal)
                    && jetzt.Grund == gehalten.Wahl.Grund
                    && string.Equals(jetzt.FehlendeId, gehalten.Wahl.FehlendeId, StringComparison.Ordinal))
                    return gehalten;
            }
            return _bericht.PruefeVorStart(konfig, englisch, sicht, erzwingtWirtschaftlichkeit);
        }

        /// <summary>
        /// Der Excel-Befund des Laufs (BV-E7-3): der gehaltene der letzten Vorprüfung, wenn er zu diesem Lauf passt —
        /// dieselbe Excel-Vorlage mit demselben Grund, dieselbe Sprache —, sonst eine frische Vorprüfung. Einmal
        /// abgeholt, ist er fort.
        /// </summary>
        internal Excelstartbefund ExcelStartFuerLauf(BerichtsKonfiguration konfig, bool englisch, int sicht)
        {
            Excelstartbefund gehalten = _excelStart;
            _excelStart = null;
            if (gehalten?.Wahl?.Eintrag != null && gehalten.Englisch == englisch)
            {
                Vorlagenwahl jetzt = ExcelWahl(konfig);
                if (jetzt?.Eintrag != null
                    && string.Equals(jetzt.Eintrag.Id, gehalten.Wahl.Eintrag.Id, StringComparison.Ordinal)
                    && jetzt.Grund == gehalten.Wahl.Grund)
                    return gehalten;
            }
            return _bericht.PruefeExcelVorStart(konfig, englisch, sicht);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Die Id einer Kennung — einmal vergeben, bleibt sie.</summary>
        internal int IdFuer(string kennung)
        {
            string k = kennung ?? "";
            if (_ids.TryGetValue(k, out int id)) return id;
            id = _ids.Count + 1;
            _ids[k] = id;
            _kennungen[id] = k;
            return id;
        }

        /// <summary>Die Kennung zu einer Id; <c>null</c>, wenn die Hülle sie nicht vergeben hat.</summary>
        internal string KennungFuer(int id)
        {
            return _kennungen.TryGetValue(id, out string k) ? k : null;
        }

        private BerichtsKonfiguration Lade()
        {
            try { return _bericht.Lade(_idStamm) ?? BerichtsKonfiguration.Standard(); }
            catch (Exception) { return BerichtsKonfiguration.Standard(); }
        }

        private Vorlagenwahl Wahl(BerichtsKonfiguration konfig)
        {
            try { return _vorlagen.VorlageFuer(konfig); }
            catch (Exception) { return null; }
        }

        private IReadOnlyList<Vorlageneintrag> Liste()
        {
            try { return _vorlagen.Liste(); }
            catch (Exception) { return Array.Empty<Vorlageneintrag>(); }
        }

        private int Sicht()
        {
            try { return _sicht() == 2 ? 2 : 1; }
            catch (Exception) { return 1; }
        }

        private Pruefkontext Kontext(BerichtsKonfiguration konfig)
        {
            return Pruefkontext.Aus(konfig, Englisch, Sicht());
        }

        /// <summary>Schreibt der Lauf einen Word-Bericht (Ausgabe Word oder Beide)?</summary>
        private static bool MitWord(BerichtsKonfiguration konfig)
        {
            return (Pruefkontext.AusgabeAus(konfig?.Ausgabe) & Vorlagenausgabe.Word) != 0;
        }

        /// <summary>Schreibt der Lauf eine Excel-Mappe (Ausgabe Excel oder Beide)?</summary>
        private static bool MitExcel(BerichtsKonfiguration konfig)
        {
            return (Pruefkontext.AusgabeAus(konfig?.Ausgabe) & Vorlagenausgabe.Excel) != 0;
        }

        /// <summary>Die Datei, die „Schreibgeschützt öffnen" und „Teilen…" nehmen: die Vorlage selbst, sonst ihr Rückfall.</summary>
        private static string Lesepfad(Vorlageneintrag e)
        {
            if (e == null) return null;
            if (!string.IsNullOrEmpty(e.Pfad) && File.Exists(e.Pfad)) return e.Pfad;
            if (!string.IsNullOrEmpty(e.Rueckfallpfad) && File.Exists(e.Rueckfallpfad)) return e.Rueckfallpfad;
            return null;
        }

        /// <summary>Der Name einer Kennung: <c>eigen:Angebot.docx</c> → „Angebot".</summary>
        internal static string NameAusKennung(string kennung)
        {
            string k = (kennung ?? "").Trim();
            if (k.StartsWith(BerichtsvorlagenCtrl.ID_PRAEFIX_EIGEN, StringComparison.OrdinalIgnoreCase))
                k = k.Substring(BerichtsvorlagenCtrl.ID_PRAEFIX_EIGEN.Length).Trim();
            return Stamm(k);
        }

        /// <summary>Der Dateiname ohne Pfad und ohne Word-Endung.</summary>
        private static string Stamm(string name)
        {
            string n = (name ?? "").Trim();
            try { n = Path.GetFileName(n); } catch (ArgumentException) { /* bleibt */ }
            string endung = "";
            try { endung = Path.GetExtension(n); } catch (ArgumentException) { endung = ""; }
            bool vorlage = BerichtsvorlagenCtrl.Endungen.Contains(endung.ToLowerInvariant()) ||
                           BerichtsvorlagenCtrl.ExcelEndungen.Contains(endung.ToLowerInvariant());
            return vorlage ? n.Substring(0, n.Length - endung.Length) : n;
        }

        private static string Format(string muster, params object[] argumente)
        {
            try { return string.Format(CultureInfo.CurrentCulture, muster ?? "", argumente); }
            catch (FormatException) { return muster ?? ""; }
        }

        private static string Ressource(string schluessel, string rueckfall)
        {
            try
            {
                string t = R.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(t) ? rueckfall : t;
            }
            catch (Exception) { return rueckfall; }
        }
    }
}
