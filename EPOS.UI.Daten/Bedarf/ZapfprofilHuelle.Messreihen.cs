using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Dialogs „Messdaten"</b> (Umsetzungskonzept Zapfprofilgenerator 4.8 und
    /// Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 3): baut aus <see cref="TwwMessreihenCtrl"/> und
    /// <see cref="Messreihenleser"/> die DTO des <c>TwwMessreihenDialog</c> — Liste, Dateiwahl,
    /// Prüfung ohne Schreibzugriff, Einspielen und Löschen.
    ///
    /// <para><b>Kein Arbeitsstand</b>: Einspielen und Löschen schreiben sofort in EINER Transaktion
    /// des Kerns; der Dialog liest die Liste danach neu. Jede Ablehnung kommt benannt zurück — die
    /// Sätze des Kerns als <see cref="ZapfSatz"/> (<c>ZPG_SATZ_…</c>), die Beschriftungen als
    /// <c>ZPGM_…</c>.</para>
    ///
    /// <para><b>Plattformfrei</b>: Die Dateiwahl kommt über <see cref="Dienste.Datei"/> (wartbarer
    /// Zwilling, HINTER dem Blazor-Ereignis), der Startordner aus derselben Einstellung wie die
    /// Katalog- und Typtagwahl (<see cref="EINSTELLUNG_IMPORTORDNER"/>). Den <b>Strom</b> öffnet
    /// diese Hülle — der Kern kennt nur einen <see cref="Stream"/>, keine Pfadlogik.</para>
    ///
    /// <para><b>Messreihen gehören zum Projekt</b> (Konzept Kapitel 9 K5): Die Zeilen tragen die
    /// Projekt-Id, reisen mit einer Projektkopie und einem <c>.wpx</c>-Paket und stehen in keiner
    /// Auslieferungsvorlage. Der Dialog sagt das als Herleitungszeile; diese Hülle reicht keine
    /// gemessene Reihe und keinen gemessenen Wert an eine andere Stelle als die Anzeige, die der
    /// Anwender selbst geöffnet hat.</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Der Hilfeschlüssel des Dialogs „Messdaten" (5.8).</summary>
        internal const string HILFE_MESSREIHEN = "Form_Brauchwasser_Messreihen.btn_Help";

        /// <summary>Die Kennung „aus der Kopfzeile lesen“ der Größenwahl (kein Wert des Aufzählungstyps).</summary>
        internal const int MESSGROESSE_AUS_KOPF = TwwMessreihenwahl.GroesseAusKopf;

        /// <summary>
        /// Der Parametersatz der Komponente <c>TwwMessreihenDialog.razor</c> — ohne
        /// <c>Geschlossen</c>, damit ihn auch ein Wirt ohne Fenster nehmen kann. Ohne gespeichertes
        /// Projekt (<paramref name="idProjekt"/> ≤ 0) stehen die Schreibwege nicht da: Eine
        /// Messreihe ohne Projekt hätte keinen Platz in der Ablage.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> MessreihenGaben(int idProjekt)
        {
            var gaben = new Dictionary<string, object>
            {
                ["Stand"] = new Func<TwwMessreihenstandDaten>(() => Messreihenstand(idProjekt)),
                ["DateiWaehlen"] = new Func<string, Task<string>>(MessreiheDateiWaehlen),
                // Die Pruefung liest und parst die ganze Datei (eine Jahresreihe in 15-min-Schritten
                // sind 35 040 Zeilen): Sie laeuft NEBENLAEUFIG auf einem Arbeitsfaden mit der Kultur
                // des Aufrufers, abbrechbar - nie im Zeichenfaden.
                ["Pruefen"] = new Func<string, TwwMessreiheneingabeDaten, CancellationToken, Task<TwwMessreihenpruefungDaten>>(
                    (pfad, eingabe, abbruch) => Kulturweitergabe.Starten(
                        () => MessreihePruefen(idProjekt, pfad, eingabe), abbruch)),
                ["Texte"] = MessreihenTexte(),
                ["HilfeSchluessel"] = HILFE_MESSREIHEN
            };
            if (idProjekt > 0)
            {
                gaben["Einspielen"] = new Func<string, TwwMessreiheneingabeDaten, TwwMessreihenergebnisDaten>(
                    (pfad, eingabe) => MessreiheEinspielen(idProjekt, pfad, eingabe));
                gaben["Loeschen"] = new Func<string, TwwMessreihenergebnisDaten>(
                    bezeichnung => MessreiheLoeschen(idProjekt, bezeichnung));
            }
            return gaben;
        }

        // =================================================================================
        // Stand
        // =================================================================================

        /// <summary>
        /// Die eingespielten Reihen des Projekts; ohne Tabelle, ohne Projekt und ohne Zeile mit
        /// benanntem Grund — nie eine stille Leere.
        /// </summary>
        internal static TwwMessreihenstandDaten Messreihenstand(int idProjekt)
        {
            TwwMessreihenTexte t = MessreihenTexte();
            var d = new TwwMessreihenstandDaten
            {
                TabelleDa = TwwMessreihenCtrl.TabelleVorhanden(),
                LueckenschwelleVorgabe = Lueckenschwelle(),
                MindesttageVorschlag = Mindesttage()
            };
            if (!d.TabelleDa)
            {
                d.Grund = Satztext(ZapfSatz.Neu("MESSREIHENIMPORT_TABELLE_FEHLT",
                                                ZapfSatz.Tabelle(TwwSchema.TAB_TWW_MESSREIHE)));
                return d;
            }
            if (idProjekt <= 0)
            {
                d.Grund = t.KeinProjekt;
                return d;
            }

            foreach (TwwMessreihenkopf k in TwwMessreihenCtrl.Liste(idProjekt))
                d.Reihen.Add(AlsMessreihe(k, t));
            if (d.Reihen.Count == 0) d.Grund = t.Leer;
            return d;
        }

        private static TwwMessreiheDaten AlsMessreihe(TwwMessreihenkopf k, TwwMessreihenTexte t)
            => new TwwMessreiheDaten
            {
                Bezeichnung = k.Bezeichnung ?? "",
                Groesse = Groessentext(k.Groesse, t),
                GroesseId = (int)k.Groesse,
                AufloesungMin = k.AufloesungMin,
                Beginn = k.Beginn ?? "",
                Schritte = k.Schritte,
                Tage = k.Tage,
                Nulllaeufe = k.Nulllaeufe,
                Nullanteil = k.Nullanteil,
                Quelle = k.Quelle ?? "",
                DatumImport = k.DatumImport ?? ""
            };

        /// <summary>Die Beschriftung einer gemessenen Größe samt ihrer Einheit.</summary>
        internal static string Groessentext(ZapfMessgroesse g, TwwMessreihenTexte t)
        {
            switch (g)
            {
                case ZapfMessgroesse.Volumen: return t.GroesseVolumen;
                case ZapfMessgroesse.Leistung: return t.GroesseLeistung;
                default: return t.GroesseEnergie;
            }
        }

        /// <summary>Die Vorgabe der Lückenschwelle [-] aus dem Parametersatz; ohne Satz die des Lesers.</summary>
        private static double Lueckenschwelle()
        {
            var vorgabe = new Messreihenoptionen();
            try
            {
                Parametersatz p = ZapfprofilCtrl.Parameter();
                return p != null && p.Enthaelt(ZapfParameter.VALIDIERUNG_LUECKENANTEIL)
                    ? p.Wert(ZapfParameter.VALIDIERUNG_LUECKENANTEIL) : vorgabe.LueckenanteilHoechstens;
            }
            catch (ParametersatzException) { return vorgabe.LueckenanteilHoechstens; }
        }

        /// <summary>Die kürzeste Reihe für einen Kalibriervorschlag [d] aus dem Parametersatz.</summary>
        private static int Mindesttage()
        {
            try { return WindowsFormsApplication1.Messkalibrierung.Mindesttage(ZapfprofilCtrl.Parameter()); }
            catch (ParametersatzException) { return WindowsFormsApplication1.Messkalibrierung.MINDESTTAGE_VORGABE; }
        }

        // =================================================================================
        // Dateiwahl, Optionen
        // =================================================================================

        /// <summary>
        /// Die Dateiwahl über <see cref="Dienste.Datei"/> — HINTER dem Blazor-Ereignis (wartbarer
        /// Zwilling), Startordner aus der Einstellung der letzten Wahl. <c>""</c> = abgebrochen.
        /// </summary>
        internal static Task<string> MessreiheDateiWaehlen(string filter)
        {
            TwwMessreihenTexte t = MessreihenTexte();
            string ordner = "";
            try { ordner = Dienste.Einstellungen?.Lies(EINSTELLUNG_IMPORTORDNER, "") ?? ""; } catch { }
            return Dienste.Datei.DateiOeffnenAsync(t.WahlTitel, string.IsNullOrEmpty(filter) ? t.Dateifilter : filter, ordner);
        }

        /// <summary>
        /// Die Optionen des Lesers aus den Eingaben der Maske: Bezeichnung, Quelle, Größe (0 =
        /// „aus der Kopfzeile"), Lückenschwelle (leer = Vorgabe des Parametersatzes) und die
        /// Zeitrechnung der Zeitstempel.
        /// </summary>
        internal static Messreihenoptionen AlsOptionen(TwwMessreiheneingabeDaten e)
        {
            var o = new Messreihenoptionen();
            if (e == null) return o with { LueckenanteilHoechstens = Lueckenschwelle() };
            ZapfMessgroesse? groesse = e.GroesseId is int g && g != MESSGROESSE_AUS_KOPF
                                       && Enum.IsDefined(typeof(ZapfMessgroesse), g)
                ? (ZapfMessgroesse)g : (ZapfMessgroesse?)null;
            Messzeitstempel zeit = Enum.IsDefined(typeof(Messzeitstempel), e.ZeitstempelId)
                ? (Messzeitstempel)e.ZeitstempelId : Messzeitstempel.Ortszeit;
            return o with
            {
                Bezeichnung = (e.Bezeichnung ?? "").Trim(),
                Quelle = (e.Quelle ?? "").Trim(),
                Groesse = groesse,
                LueckenanteilHoechstens = e.LueckenschwelleAnteil ?? Lueckenschwelle(),
                Zeitstempel = zeit
            };
        }

        // =================================================================================
        // Prüfung
        // =================================================================================

        /// <summary>
        /// <b>Die Prüfung einer Datei</b> — gelesen wird mit demselben Leser wie beim Einspielen,
        /// aber OHNE Schreibzugriff: Die eingespielten Reihen bleiben, bis „Einspielen" gedrückt
        /// ist. Der Ordner wird für die nächste Wahl gemerkt.
        /// </summary>
        internal static TwwMessreihenpruefungDaten MessreihePruefen(int idProjekt, string pfad,
                                                                   TwwMessreiheneingabeDaten eingabe)
        {
            TwwMessreihenTexte t = MessreihenTexte();
            var d = new TwwMessreihenpruefungDaten();
            if (string.IsNullOrWhiteSpace(pfad))
            {
                d.Abgebrochen = true;
                d.Abbruch = t.KeineDatei;
                return d;
            }
            // Der Ordner ist die Wahl des ANWENDERS, nicht das Urteil ueber die Datei: Er wird auch
            // gemerkt, wenn die Datei gleich abgelehnt wird - die naechste Wahl beginnt dort, wo der
            // Anwender zuletzt gesucht hat.
            OrdnerMerken(pfad);

            var hinweise = new List<ZapfSatz>();
            Messreihe reihe = Lesen(pfad, AlsOptionen(eingabe), out ZapfSatz fehler, hinweise);
            d.Hinweise.AddRange(hinweise.Select(Satztext));
            if (reihe == null)
            {
                d.Abgebrochen = true;
                d.Abbruch = Format(t.Abbruch, Satztext(fehler));
                return d;
            }

            d.Bezeichnung = reihe.Bezeichnung;
            d.ErsetztVorhandene = idProjekt > 0 && TwwMessreihenCtrl.TabelleVorhanden()
                && TwwMessreihenCtrl.Liste(idProjekt).Any(
                    k => string.Equals(k.Bezeichnung, reihe.Bezeichnung, StringComparison.Ordinal));
            d.Zusammenfassung = Format(t.Zusammenfassung, reihe.Schritte, reihe.AufloesungMin, Zahl(reihe.Tage, "N1"));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelBezeichnung, Wert(reihe.Bezeichnung, t)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelGroesse, Groessentext(reihe.Groesse, t)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelAufloesung, Ganz(reihe.AufloesungMin)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelBeginn, Zeitpunkt(reihe.Beginn)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelEnde, Zeitpunkt(reihe.Ende)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelSchritte, Ganz(reihe.Schritte)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelTage, Zahl(reihe.Tage, "N1")));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelLuecken, Ganz(reihe.Luecken)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelSchalttage, Ganz(reihe.Schalttage)));
            d.Angaben.Add(new TwwMessreihenangabeDaten(t.LabelQuelle, Wert(reihe.Quelle, t)));
            return d;
        }

        /// <summary>
        /// Liest die Datei über <see cref="Messreihenleser.AusStrom"/>. <b>Der Strom gehört der
        /// Hülle</b>: Der Kern kennt keinen Pfad. Ein Lesefehler des Dateisystems (fehlende Datei,
        /// gesperrte Datei) ist eine benannte Ablehnung wie jede andere, keine Ausnahme im Dialog.
        /// </summary>
        private static Messreihe Lesen(string pfad, Messreihenoptionen optionen, out ZapfSatz fehler,
                                       ICollection<ZapfSatz> hinweise)
        {
            string datei = "";
            try { datei = Path.GetFileName(pfad) ?? ""; } catch { }
            try
            {
                using (FileStream strom = File.OpenRead(pfad))
                    return Messreihenleser.AusStrom(strom, datei, optionen, out fehler, hinweise);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is ArgumentException || ex is NotSupportedException)
            {
                fehler = ZapfSatz.Neu("MESSREIHE_DATEI_UNLESBAR", datei.Length > 0 ? datei : pfad ?? "", ex.Message);
                return null;
            }
        }

        // =================================================================================
        // Einspielen und Löschen
        // =================================================================================

        /// <summary>
        /// <b>Das Einspielen</b> (<see cref="TwwMessreihenCtrl.Importieren(int, Stream, string, Messreihenoptionen, string)"/>
        /// — die Hülle reicht Projekt, Strom, Dateiname und Optionen; das <c>datumImport</c> setzt der
        /// Kern selbst): EIN Vorgang; eine gleichnamige Reihe desselben Projekts wird ersetzt. Bei
        /// einem Abbruch ist nichts geändert, die frühere Reihe steht unverändert da, und der Grund
        /// kommt benannt zurück.
        /// </summary>
        internal static TwwMessreihenergebnisDaten MessreiheEinspielen(int idProjekt, string pfad,
                                                                      TwwMessreiheneingabeDaten eingabe)
        {
            TwwMessreihenTexte t = MessreihenTexte();
            var e = new TwwMessreihenergebnisDaten { Stand = Messreihenstand(idProjekt) };
            if (string.IsNullOrWhiteSpace(pfad))
            {
                e.Meldung = t.KeineDatei;
                return e;
            }
            if (idProjekt <= 0)
            {
                e.Meldung = t.KeinProjekt;
                return e;
            }
            OrdnerMerken(pfad);

            string datei = "";
            try { datei = Path.GetFileName(pfad) ?? ""; } catch { }
            TwwMessreihenimportBericht b;
            try
            {
                using (FileStream strom = File.OpenRead(pfad))
                    b = TwwMessreihenCtrl.Importieren(idProjekt, strom, datei, AlsOptionen(eingabe));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is ArgumentException || ex is NotSupportedException)
            {
                e.Meldung = Format(t.Abbruch, Satztext(ZapfSatz.Neu("MESSREIHE_DATEI_UNLESBAR",
                                                                    datei.Length > 0 ? datei : pfad, ex.Message)));
                return e;
            }

            e.Hinweise.AddRange(b.Hinweise.Select(Satztext));
            e.Stand = Messreihenstand(idProjekt);
            if (!b.Ok)
            {
                e.Meldung = Format(t.Abbruch, Satztext(b.Abbruch));
                return e;
            }
            e.Ok = true;
            e.Bezeichnung = b.Reihe?.Bezeichnung ?? "";
            e.Meldung = Format(t.MeldungEingespielt, e.Bezeichnung, b.Zeilen, b.Ersetzt);
            return e;
        }

        /// <summary>
        /// <b>Das Löschen</b> (<see cref="TwwMessreihenCtrl.Loeschen"/>): Die Reihe ist danach aus
        /// dem Projekt weg; war sie nicht da, sagt die Meldung genau das (kein Fehler).
        /// </summary>
        internal static TwwMessreihenergebnisDaten MessreiheLoeschen(int idProjekt, string bezeichnung)
        {
            TwwMessreihenTexte t = MessreihenTexte();
            if (idProjekt <= 0)
                return new TwwMessreihenergebnisDaten { Meldung = t.KeinProjekt, Stand = Messreihenstand(idProjekt) };
            if (string.IsNullOrWhiteSpace(bezeichnung))
                return new TwwMessreihenergebnisDaten { Meldung = t.MeldungNichts, Stand = Messreihenstand(idProjekt) };

            int weg = TwwMessreihenCtrl.Loeschen(idProjekt, bezeichnung);
            return new TwwMessreihenergebnisDaten
            {
                Ok = weg > 0,
                Bezeichnung = weg > 0 ? bezeichnung : "",
                Meldung = weg > 0 ? Format(t.MeldungGeloescht, bezeichnung, weg) : t.MeldungNichts,
                Stand = Messreihenstand(idProjekt)
            };
        }

        // =================================================================================
        // Kleinkram
        // =================================================================================

        private static string Ganz(int i) => i.ToString("N0", CultureInfo.CurrentCulture);

        private static string Zahl(double d, string format) => d.ToString(format, CultureInfo.CurrentCulture);

        private static string Zeitpunkt(DateTime z) => z.ToString("g", CultureInfo.CurrentCulture);

        private static string Wert(string text, TwwMessreihenTexte t)
            => string.IsNullOrWhiteSpace(text) ? t.WertOhne : text;

        // =================================================================================
        // Texte
        // =================================================================================

        /// <summary>Das Textbündel des Dialogs in der Oberflächensprache (<c>ZPGM_…</c>); Rückfall der deutsche Vorgabewert.</summary>
        internal static TwwMessreihenTexte MessreihenTexte()
        {
            var t = new TwwMessreihenTexte();
            t.Titel = Text_("ZPGM_TITEL", t.Titel);
            t.HinweisProjekt = Text_("ZPGM_HINWEIS_PROJEKT", t.HinweisProjekt);
            t.HinweisFormat = Text_("ZPGM_HINWEIS_FORMAT", t.HinweisFormat);
            t.HinweisNulllaeufe = Text_("ZPGM_HINWEIS_NULLLAEUFE", t.HinweisNulllaeufe);
            t.GruppeListe = Text_("ZPGM_GRP_LISTE", t.GruppeListe);
            t.GruppeEinlesen = Text_("ZPGM_GRP_EINLESEN", t.GruppeEinlesen);
            t.GruppePruefung = Text_("ZPGM_GRP_PRUEFUNG", t.GruppePruefung);
            t.Leer = Text_("ZPGM_LEER", t.Leer);
            t.SpalteBezeichnung = Text_("ZPGM_SP_BEZEICHNUNG", t.SpalteBezeichnung);
            t.SpalteGroesse = Text_("ZPGM_SP_GROESSE", t.SpalteGroesse);
            t.SpalteAufloesung = Text_("ZPGM_SP_AUFLOESUNG", t.SpalteAufloesung);
            t.SpalteBeginn = Text_("ZPGM_SP_BEGINN", t.SpalteBeginn);
            t.SpalteTage = Text_("ZPGM_SP_TAGE", t.SpalteTage);
            t.SpalteNulllaeufe = Text_("ZPGM_SP_NULLLAEUFE", t.SpalteNulllaeufe);
            t.SpalteQuelle = Text_("ZPGM_SP_QUELLE", t.SpalteQuelle);
            t.SpalteImport = Text_("ZPGM_SP_IMPORT", t.SpalteImport);
            t.SpalteAngabe = Text_("ZPGM_SP_ANGABE", t.SpalteAngabe);
            t.SpalteWert = Text_("ZPGM_SP_WERT", t.SpalteWert);
            t.SpalteAktion = Text_("ZPGM_SP_AKTION", t.SpalteAktion);
            t.PruefungLaeuft = Text_("ZPGM_PRUEFUNG_LAEUFT", t.PruefungLaeuft);
            t.PruefungAbgebrochen = Text_("ZPGM_PRUEFUNG_ABGEBROCHEN", t.PruefungAbgebrochen);
            t.BerichtOffen = Text_("ZPGM_BERICHT_OFFEN", t.BerichtOffen);
            t.LabelBezeichnung = Text_("ZPGM_LBL_BEZEICHNUNG", t.LabelBezeichnung);
            t.LabelQuelle = Text_("ZPGM_LBL_QUELLE", t.LabelQuelle);
            t.LabelGroesse = Text_("ZPGM_LBL_GROESSE", t.LabelGroesse);
            t.LabelLueckenschwelle = Text_("ZPGM_LBL_LUECKENSCHWELLE", t.LabelLueckenschwelle);
            t.LabelZeitstempel = Text_("ZPGM_LBL_ZEITSTEMPEL", t.LabelZeitstempel);
            t.GroesseKopf = Text_("ZPGM_GROESSE_KOPF", t.GroesseKopf);
            t.GroesseEnergie = Text_("ZPGM_GROESSE_ENERGIE", t.GroesseEnergie);
            t.GroesseVolumen = Text_("ZPGM_GROESSE_VOLUMEN", t.GroesseVolumen);
            t.GroesseLeistung = Text_("ZPGM_GROESSE_LEISTUNG", t.GroesseLeistung);
            t.ZeitOrtszeit = Text_("ZPGM_ZEIT_ORTSZEIT", t.ZeitOrtszeit);
            t.ZeitNormalzeit = Text_("ZPGM_ZEIT_NORMALZEIT", t.ZeitNormalzeit);
            t.KnopfDatei = Text_("ZPGM_BTN_DATEI", t.KnopfDatei);
            t.KnopfEinspielen = Text_("ZPGM_BTN_EINSPIELEN", t.KnopfEinspielen);
            t.KnopfWaehlen = Text_("ZPGM_BTN_WAEHLEN", t.KnopfWaehlen);
            t.KnopfLoeschen = Text_("ZPGM_BTN_LOESCHEN", t.KnopfLoeschen);
            t.KnopfBeenden = Text_("ZPGM_BTN_BEENDEN", t.KnopfBeenden);
            t.Dateifilter = Text_("ZPGM_DATEIFILTER", t.Dateifilter);
            t.WahlTitel = Text_("ZPGM_WAHL_TITEL", t.WahlTitel);
            t.KeineDatei = Text_("ZPGM_KEINE_DATEI", t.KeineDatei);
            t.Abbruch = Text_("ZPGM_ABBRUCH", t.Abbruch);
            t.Zusammenfassung = Text_("ZPGM_ZUSAMMENFASSUNG", t.Zusammenfassung);
            t.Hinweise = Text_("ZPGM_HINWEISE", t.Hinweise);
            t.LabelSchritte = Text_("ZPGM_LBL_SCHRITTE", t.LabelSchritte);
            t.LabelTage = Text_("ZPGM_LBL_TAGE", t.LabelTage);
            t.LabelAufloesung = Text_("ZPGM_LBL_AUFLOESUNG", t.LabelAufloesung);
            t.LabelBeginn = Text_("ZPGM_LBL_BEGINN", t.LabelBeginn);
            t.LabelEnde = Text_("ZPGM_LBL_ENDE", t.LabelEnde);
            t.LabelLuecken = Text_("ZPGM_LBL_LUECKEN", t.LabelLuecken);
            t.LabelSchalttage = Text_("ZPGM_LBL_SCHALTTAGE", t.LabelSchalttage);
            t.FrageErsetzen = Text_("ZPGM_FRAGE_ERSETZEN", t.FrageErsetzen);
            t.FrageLoeschen = Text_("ZPGM_FRAGE_LOESCHEN", t.FrageLoeschen);
            t.MeldungEingespielt = Text_("ZPGM_MSG_EINGESPIELT", t.MeldungEingespielt);
            t.MeldungGeloescht = Text_("ZPGM_MSG_GELOESCHT", t.MeldungGeloescht);
            t.MeldungNichts = Text_("ZPGM_MSG_NICHTS", t.MeldungNichts);
            t.KeinProjekt = Text_("ZPGM_KEIN_PROJEKT", t.KeinProjekt);
            t.WertOhne = Text_("ZPGM_WERT_OHNE", t.WertOhne);
            return t;
        }
    }
}
