using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Projekt;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE von „Projekt Speichern unter" (iU9-W15a.4; Umzug nach
    /// <c>EPOS.UI.Daten</c> mit Auftrag KI‑F8).
    ///
    /// <para><b>Der Fachteil lag schon vorher im Kern.</b> <c>ProjektDuplizierenCtrl</c>
    /// (768 Z.) kopiert alle Projekttabellen in EINER Transaktion; seit iU9-W15a.0b/0c
    /// liegen dort auch die drei Vorprüfungen und das Schreiben der drei
    /// Verwaltungsfelder. Diese Hülle reicht nur noch durch — und genau deshalb steht
    /// sie plattformfrei: Sie führte keine Windows-Zeile, war auf dem iPad aber
    /// unerreichbar.</para>
    ///
    /// <para><b>Der Kopierlauf läuft NEBENLÄUFIG</b>, wie beim Vorläufer: Der Bedienfaden
    /// liest und zeigt, der Arbeitsfaden kopiert, <c>Progress&lt;T&gt;</c> besorgt das
    /// Marshalling. Neu ist der Abbruch (A-2): Ein <c>CancellationToken</c> geht mit in den
    /// Kern, und ein Abbruch rollt die eine Transaktion zurück.</para>
    ///
    /// <para>Das FENSTER steht unter Windows in <c>Views/Projekt/ProjektKopieFenster</c>;
    /// auf iOS zeigt die <c>AppWurzel</c> dieselbe Komponente als Ansicht. Muster
    /// <see cref="NutzungsdauerHuelle"/>.</para>
    /// </summary>
    internal static class ProjektKopieHuelle
    {
        /// <summary>Der Fenstertitel — derselbe Text wie die Dialogüberschrift.</summary>
        internal static string Titel() => Text_("PRJ_KOPIE_TITEL", "Projekt Speichern unter");

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Zeilen"] = ProjektCtrl.NamenListe(),
                ["Quellfelder"] = new Func<string, ProjektKopfDaten>(ProjektCtrl.Kopf),
                ["Pruefen"] = new Func<string, string, DuplizierBefund>(
                    (quelle, neu) => new ProjektDuplizierenCtrl().PruefeNamen(quelle, neu)),
                ["Duplizieren"] = new Func<string, string,
                                           IProgress<KopierStand>, CancellationToken, Task<int>>(Duplizieren),
                ["Verwaltungsfelder"] = new Func<string, string, string, string,
                                                 VerwaltungsfelderErgebnis>(Verwaltungsfelder),

                ["TitelText"] = Text_("PRJ_KOPIE_TITEL", "Projekt Speichern unter"),
                ["LabelAuswahl"] = Text_("PRJ_KOPIE_LBL_AUSWAHL", "Projektauswahl:"),
                ["LabelNeuerName"] = Text_("PRJ_KOPIE_LBL_NEUERNAME", "Neuer Projektname:"),
                ["LabelBeschreibung"] = Text_("PRJ_KOPIE_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelKunde"] = Text_("PRJ_KOPIE_LBL_KUNDE", "Kunde:"),
                ["LabelBearbeiter"] = Text_("PRJ_KOPIE_LBL_BEARBEITER", "Bearbeiter:"),

                // A-1: das ❌ des Vorlaeufers ist weg - eine Beschriftung "Abbrechen"
                // neben einer "OK" braucht kein Symbol (Befund W15a-B16).
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,

                ["MeldungNameLeer"] = Text_("PRJ_KOPIE_MSG_NAME_LEER",
                    "Bitte einen neuen Projektnamen eingeben."),
                ["MeldungNameBelegt"] = Text_("PRJ_KOPIE_MSG_NAME_BELEGT",
                    "Projektname bereits vorhanden!"),
                ["MeldungQuelleFehlt"] = Text_("PRJ_KOPIE_MSG_QUELLE_FEHLT",
                    "Quellprojekt '{0}' wurde nicht gefunden."),
                ["MeldungFehler"] = Text_("PRJ_KOPIE_MSG_FEHLER", "Fehler beim Speichern unter: {0}"),
                ["MeldungKopieFehlt"] = Text_("PRJ_KOPIE_MSG_KOPIE_FEHLT",
                    "Die Kopie '{0}' wurde nicht gefunden. Beschreibung, Kunde und Bearbeiter "
                    + "wurden nicht übernommen."),
                ["MeldungFelderNicht"] = Text_("PRJ_KOPIE_MSG_FELDER_NICHT",
                    "Beschreibung, Kunde und Bearbeiter konnten nicht gespeichert werden. "
                    + "Die Projektkopie selbst ist angelegt."),
                ["MeldungFelderFehler"] = Text_("PRJ_KOPIE_MSG_FELDER_FEHLER",
                    "Beschreibung, Kunde und Bearbeiter konnten nicht gespeichert werden: {0}\n"
                    + "Die Projektkopie selbst ist angelegt."),

                ["FortschrittFormat"] = Text_("PRJ_KOPIE_FORTSCHRITT", "Kopiere Tabelle {0}/{1}: {2}"),
                ["FortschrittFertigstellen"] = Text_("PRJ_KOPIE_FERTIGSTELLEN", "Fertigstellen ..."),
                ["FortschrittFertig"] = Text_("PRJ_KOPIE_FERTIG", "Fertig"),

                ["AnzahlFormat"] = Text_("PRJ_LIST_ANZAHL", "{0} von {1} Projekten"),
                ["SpalteName"] = Text_("PRJ_LIST_SP_NAME", "Projektname"),
                ["SpalteKunde"] = Text_("PRJ_LIST_SP_KUNDE", "Kunde"),
                ["SpalteGeaendert"] = Text_("PRJ_LIST_SP_GEAENDERT", "Geändert"),
                ["SpalteArt"] = Text_("PRJ_LIST_SP_ART", "Art"),
                ["ArtStammText"] = Text_("PRJ_LIST_ART_STAMM", "Stamm"),
                ["ArtVarianteText"] = Text_("PRJ_LIST_ART_VARIANTE", "Variante"),
                ["VarianteVonFormat"] = Text_("PRJ_LIST_VARIANTE_VON", "Variante von {0}"),
                ["SucheText"] = Text_("PRJ_LIST_LBL_SUCHE", "Suchen:"),
                ["LeerText"] = Text_("PRJ_LIST_LEER", "Es ist noch kein Projekt angelegt."),

                ["HilfeSchluessel"] = "Form_ProjektSpeichernUnter.btn_Help"
            };
        }

        /// <summary>
        /// Der Kopierlauf im Hintergrund. Der Bedienfaden hat den
        /// <c>Progress&lt;T&gt;</c> erzeugt und bekommt die Meldungen deshalb auf sich
        /// zurück (Hausmuster <c>Form_SpeicherOptimierung</c>).
        /// </summary>
        private static Task<int> Duplizieren(string quelle, string neu,
                                             IProgress<KopierStand> melder, CancellationToken abbruch)
        {
            IProgress<ProjektDuplizierenCtrl.Fortschritt> brueckeninhalt =
                new Progress<ProjektDuplizierenCtrl.Fortschritt>(f =>
                    melder?.Report(new KopierStand(f.Aktuell, f.Gesamt, f.Tabelle ?? "")));

            // Mit der Kultur des Aufrufers auf dem Arbeitsfaden (Auftrag #232).
            return Kulturweitergabe.Starten(
                () => new ProjektDuplizierenCtrl()
                          .Duplizieren(quelle, neu, brueckeninhalt, abbruch),
                abbruch);
        }

        private static VerwaltungsfelderErgebnis Verwaltungsfelder(
            string neuerName, string beschreibung, string kunde, string bearbeiter)
        {
            VerwaltungsfelderBefund befund = new ProjektDuplizierenCtrl()
                .VerwaltungsfelderSetzen(neuerName, beschreibung, kunde, bearbeiter, out string fehlertext);
            return new VerwaltungsfelderErgebnis(befund, fehlertext ?? "");
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
