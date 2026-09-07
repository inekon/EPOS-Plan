#nullable enable

using System;
using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Import
{
    /// <summary>
    /// Eine Zeile der Auswahlliste, so wie die Komponente sie braucht (iU9-W13.1).
    ///
    /// <para><b>Warum eine eigene Form.</b> Der Kern liefert
    /// <c>KatalogImportSatz</c> — und der kann anlegen und ueberschreiben, also
    /// SCHREIBEN. Die Komponente darf das nicht sehen (Regel „keine Datenbank");
    /// sie bekommt deshalb genau die drei Dinge, die sie anzeigt: Bezeichner,
    /// Hersteller und den Wert, ueber den ihr Zahlenfilter laeuft, dazu die
    /// Detailtexte.</para>
    /// </summary>
    public sealed class KatalogZeile
    {
        public KatalogZeile(string name, string firma, double filterwert,
                            IDictionary<string, string> werte, double filterwert2 = 0.0)
        {
            Name = name ?? "";
            Firma = firma ?? "";
            Filterwert = filterwert;
            Filterwert2 = filterwert2;
            Werte = werte ?? new Dictionary<string, string>();
        }

        /// <summary>Der Bezeichner, wie er in der Datei steht.</summary>
        public string Name { get; }

        /// <summary>Der Hersteller — zweite Spalte des Suchfilters.</summary>
        public string Firma { get; }

        /// <summary>Der Wert des Zahlenfilters (Leistung, Volumen, Aperturflaeche).</summary>
        public double Filterwert { get; }

        /// <summary>
        /// Der Wert des ZWEITEN Zahlenfilters, wenn die Auspraegung einen fuehrt
        /// (<c>KatalogImportProfil.Zweitfilter</c>) — beim Stromspeicher die
        /// Leistung [kW] neben der Kapazitaet [kWh]. Sonst 0 und unbenutzt.
        /// </summary>
        public double Filterwert2 { get; }

        /// <summary>Die Detailtexte, Schluessel → Anzeigetext.</summary>
        public IDictionary<string, string> Werte { get; }

        /// <summary>Ein Detailtext, leer wenn die Zeile ihn nicht fuehrt.</summary>
        public string Wert(string schluessel)
        {
            return Werte.TryGetValue(schluessel, out string? wert) ? (wert ?? "") : "";
        }
    }

    /// <summary>Was das Lesen einer Katalogdatei ergeben hat.</summary>
    public sealed class KatalogLeseErgebnis
    {
        public KatalogLeseErgebnis(IReadOnlyList<KatalogZeile> zeilen,
                                   IReadOnlyList<PruefMeldung> meldungen)
        {
            Zeilen = zeilen ?? Array.Empty<KatalogZeile>();
            Meldungen = meldungen ?? Array.Empty<PruefMeldung>();
        }

        /// <summary>Die gelesenen Saetze in Dateireihenfolge.</summary>
        public IReadOnlyList<KatalogZeile> Zeilen { get; }

        /// <summary>Was beim Lesen aufgefallen ist; leer heisst: nichts.</summary>
        public IReadOnlyList<PruefMeldung> Meldungen { get; }
    }

    /// <summary>
    /// Das Ergebnis der Vorpruefung, so wie die Komponente es braucht: die
    /// Prueflisten, die Namensliste des Umbenennens und die Antwort auf die eine
    /// Frage, die ueber den Konfliktdialog entscheidet.
    /// </summary>
    public sealed class KatalogVorpruefung
    {
        public KatalogVorpruefung(IReadOnlyList<ImportPruefung> pruefungen,
                                  IReadOnlyCollection<string> vergebeneNamen,
                                  bool konfliktbehaftet,
                                  List<KonfliktEntscheidung> allesImportieren)
        {
            Pruefungen = pruefungen ?? Array.Empty<ImportPruefung>();
            VergebeneNamen = vergebeneNamen ?? Array.Empty<string>();
            Konfliktbehaftet = konfliktbehaftet;
            AllesImportieren = allesImportieren ?? new List<KonfliktEntscheidung>();
        }

        /// <summary>Je Kandidat ein Befund.</summary>
        public IReadOnlyList<ImportPruefung> Pruefungen { get; }

        /// <summary>Die normalisierten Bestandsnamen — fuer die Namensvalidierung.</summary>
        public IReadOnlyCollection<string> VergebeneNamen { get; }

        /// <summary>Muss der Konfliktdialog erscheinen?</summary>
        public bool Konfliktbehaftet { get; }

        /// <summary>Die Entscheidungsliste eines konfliktfreien Laufs.</summary>
        public List<KonfliktEntscheidung> AllesImportieren { get; }
    }

    /// <summary>
    /// Der Uebersetzer, den <see cref="KatalogImportProfil.Finde"/> braucht.
    ///
    /// <para>Der Kern kennt keine Anzeigetexte — er fuehrt Schluessel. Hier steht
    /// die Zuordnung fuer die Beschriftungen der vier Auspraegungen, damit die
    /// Komponente sie nicht selbst nachschlagen muss und iOS dieselbe bekommt.</para>
    /// </summary>
    public static class Texte
    {
        /// <summary>Schluessel → Text; ein unbekannter Schluessel bleibt stehen.</summary>
        public static string Zu(string schluessel)
        {
            switch (schluessel)
            {
                case "IMP_KAT_FILTER_LEISTUNG": return Resource.IMP_KAT_FILTER_LEISTUNG;
                case "IMP_KAT_FILTER_VOLUMEN": return Resource.IMP_KAT_FILTER_VOLUMEN;
                case "IMP_KAT_FILTER_APERTUR": return Resource.IMP_KAT_FILTER_APERTUR;
                case "IMP_KAT_FILTER_ENERGIE": return Resource.IMP_KAT_FILTER_ENERGIE;
                case "IMP_KAT_FILTER_LEISTUNG_KW": return Resource.IMP_KAT_FILTER_LEISTUNG_KW;

                case "IMP_KAT_FELD_NAME": return Resource.IMP_KAT_FELD_NAME;
                case "IMP_KAT_FELD_FIRMA": return Resource.IMP_KAT_FELD_FIRMA;
                case "IMP_KAT_FELD_BAUART": return Resource.IMP_KAT_FELD_BAUART;
                case "IMP_KAT_FELD_THLEISTUNG": return Resource.IMP_KAT_FELD_THLEISTUNG;
                case "IMP_KAT_FELD_BRENNSTOFF": return Resource.IMP_KAT_FELD_BRENNSTOFF;
                case "IMP_KAT_FELD_WIRKUNGSGRAD": return Resource.IMP_KAT_FELD_WIRKUNGSGRAD;
                case "IMP_KAT_FELD_VERLUSTE": return Resource.IMP_KAT_FELD_VERLUSTE;
                case "IMP_KAT_FELD_SPEICHERTYP": return Resource.IMP_KAT_FELD_SPEICHERTYP;
                case "IMP_KAT_FELD_VOLUMEN": return Resource.IMP_KAT_FELD_VOLUMEN;
                case "IMP_KAT_FELD_BESCHREIBUNG": return Resource.IMP_KAT_FELD_BESCHREIBUNG;
                case "IMP_KAT_FELD_APERTUR": return Resource.IMP_KAT_FELD_APERTUR;
                case "IMP_KAT_FELD_SPITZENLEISTUNG": return Resource.IMP_KAT_FELD_SPITZENLEISTUNG;
                case "IMP_KAT_FELD_H0": return Resource.IMP_KAT_FELD_H0;
                case "IMP_KAT_FELD_A1": return Resource.IMP_KAT_FELD_A1;
                case "IMP_KAT_FELD_A2": return Resource.IMP_KAT_FELD_A2;
                case "IMP_KAT_FELD_KDIR": return Resource.IMP_KAT_FELD_KDIR;
                case "IMP_KAT_FELD_KDIFF": return Resource.IMP_KAT_FELD_KDIFF;
                case "IMP_KAT_FELD_TYP": return Resource.IMP_KAT_FELD_TYP;
                case "IMP_KAT_FELD_AUFSTELLUNG": return Resource.IMP_KAT_FELD_AUFSTELLUNG;
                case "IMP_KAT_FELD_ZUSATZHEIZUNG": return Resource.IMP_KAT_FELD_ZUSATZHEIZUNG;
                case "IMP_KAT_FELD_STUFEN": return Resource.IMP_KAT_FELD_STUFEN;
                case "IMP_KAT_FELD_MAXVORLAUF": return Resource.IMP_KAT_FELD_MAXVORLAUF;
                case "IMP_KAT_FELD_KUEHLLEISTUNG": return Resource.IMP_KAT_FELD_KUEHLLEISTUNG;

                // W13-E-2 (07.09.2026), Stufe S1 — der Stromspeicherimport.
                case "IMP_KAT_FELD_MODELL": return Resource.IMP_KAT_FELD_MODELL;
                case "IMP_KAT_FELD_CHEMIE": return Resource.IMP_KAT_FELD_CHEMIE;
                case "IMP_KAT_FELD_ENERGIE": return Resource.IMP_KAT_FELD_ENERGIE;
                case "IMP_KAT_FELD_LEISTUNG": return Resource.IMP_KAT_FELD_LEISTUNG;
                case "IMP_KAT_FELD_ETA_RT": return Resource.IMP_KAT_FELD_ETA_RT;
                case "IMP_KAT_FELD_STANDBY": return Resource.IMP_KAT_FELD_STANDBY;
                case "IMP_KAT_FELD_QUELLE": return Resource.IMP_KAT_FELD_QUELLE;
                // S3.4: die Spaltenkoepfe der Kandidatenliste - ohne Doppelpunkt,
                // ein Kopf ist keine Feldbeschriftung.
                case "IMP_KAT_SP_EINTRAG": return Resource.IMP_KAT_SP_EINTRAG;
                case "IMP_KAT_SP_LEISTUNG_TH": return Resource.IMP_KAT_SP_LEISTUNG_TH;
                case "IMP_KAT_SP_VOLUMEN": return Resource.IMP_KAT_SP_VOLUMEN;
                case "IMP_KAT_SP_APERTUR": return Resource.IMP_KAT_SP_APERTUR;
                case "IMP_KAT_SP_QUELLE": return Resource.IMP_KAT_SP_QUELLE;
                case "IMP_KAT_SP_HERSTELLER": return Resource.IMP_KAT_SP_HERSTELLER;
                case "IMP_KAT_SP_MODELL": return Resource.IMP_KAT_SP_MODELL;
                case "IMP_KAT_SP_CHEMIE": return Resource.IMP_KAT_SP_CHEMIE;
                case "IMP_KAT_SP_ENERGIE": return Resource.IMP_KAT_SP_ENERGIE;
                case "IMP_KAT_SP_LEISTUNG": return Resource.IMP_KAT_SP_LEISTUNG;
                case "IMP_KAT_SP_ETA": return Resource.IMP_KAT_SP_ETA;
                case "IMP_KAT_QUELLE_CEC_NETZ": return Resource.IMP_KAT_QUELLE_CEC_NETZ;
                case "IMP_KAT_QUELLE_CEC_DATEI": return Resource.IMP_KAT_QUELLE_CEC_DATEI;
                case "IMP_KAT_QUELLE_BSLIB": return Resource.IMP_KAT_QUELLE_BSLIB;
                case "IMP_KAT_HINWEIS_KOSTEN": return Resource.IMP_KAT_HINWEIS_KOSTEN;
                case "IMP_KAT_HINWEIS_STUFEN": return Resource.IMP_KAT_HINWEIS_STUFEN;

                case "IMP_KAT_EINH_KWTH": return Resource.IMP_KAT_EINH_KWTH;
                case "IMP_KAT_EINH_KW": return Resource.IMP_KAT_EINH_KW;
                case "IMP_KAT_EINH_PROZENT": return Resource.IMP_KAT_EINH_PROZENT;
                case "IMP_KAT_EINH_KWHD": return Resource.IMP_KAT_EINH_KWHD;
                case "IMP_KAT_EINH_LITER": return Resource.IMP_KAT_EINH_LITER;
                case "IMP_KAT_EINH_M2": return Resource.IMP_KAT_EINH_M2;
                case "IMP_KAT_EINH_WM2": return Resource.IMP_KAT_EINH_WM2;
                case "IMP_KAT_EINH_WM2K": return Resource.IMP_KAT_EINH_WM2K;
                case "IMP_KAT_EINH_KWCOOL": return Resource.IMP_KAT_EINH_KWCOOL;
                case "IMP_KAT_EINH_KWH": return Resource.IMP_KAT_EINH_KWH;
                case "IMP_KAT_EINH_W": return Resource.IMP_KAT_EINH_W;

                case "IMP_KAT_PROT_LESEN": return Resource.IMP_KAT_PROT_LESEN;
                case "IMP_KAT_PROT_GELESEN": return Resource.IMP_KAT_PROT_GELESEN;
                case "IMP_KAT_PROT_LESEFEHLER": return Resource.IMP_KAT_PROT_LESEFEHLER;
                case "IMP_KAT_PROT_SCHREIBEN": return Resource.IMP_KAT_PROT_SCHREIBEN;
                case "IMP_KAT_PROT_FERTIG": return Resource.IMP_KAT_PROT_FERTIG;
                case "IMP_KAT_PROT_AUFSTELLUNG": return Resource.IMP_KAT_PROT_AUFSTELLUNG;

                case "IMP_TXT_KEIN_PFAD": return Resource.IMP_TXT_KEIN_PFAD;
                case "IMP_TXT_LESEFEHLER": return Resource.IMP_TXT_LESEFEHLER;
                case "IMP_TXT_LEERZEILE": return Resource.IMP_TXT_LEERZEILE;
                case "IMP_TXT_TRENNZEICHEN": return Resource.IMP_TXT_TRENNZEICHEN;

                // Die Rueckmeldungen der zwei Stromspeicher-Zerleger und des
                // CEC-Abrufs (SpeicherImportMeldung im Kern, W13-E-2).
                case "SPIMP_MSG_GELADEN": return Resource.SPIMP_MSG_GELADEN;
                case "SPIMP_MSG_KOPFZEILE": return Resource.SPIMP_MSG_KOPFZEILE;
                case "SPIMP_MSG_DATEI_FEHLT": return Resource.SPIMP_MSG_DATEI_FEHLT;
                case "SPIMP_MSG_LEER": return Resource.SPIMP_MSG_LEER;
                case "SPIMP_MSG_KEINE_SAETZE": return Resource.SPIMP_MSG_KEINE_SAETZE;
                case "SPIMP_MSG_FORMAT_ALT": return Resource.SPIMP_MSG_FORMAT_ALT;
                case "SPIMP_MSG_FEHLER": return Resource.SPIMP_MSG_FEHLER;
                case "SPIMP_MSG_STAND": return Resource.SPIMP_MSG_STAND;
                case "SPIMP_MSG_UEBERGANGEN": return Resource.SPIMP_MSG_UEBERGANGEN;
                case "SPIMP_MSG_CEC_SUCHEN": return Resource.SPIMP_MSG_CEC_SUCHEN;
                case "SPIMP_MSG_CEC_CACHE": return Resource.SPIMP_MSG_CEC_CACHE;
                case "SPIMP_MSG_CEC_VERBINDEN": return Resource.SPIMP_MSG_CEC_VERBINDEN;
                case "SPIMP_MSG_CEC_GEHOLT": return Resource.SPIMP_MSG_CEC_GEHOLT;
                case "SPIMP_MSG_CEC_LEER": return Resource.SPIMP_MSG_CEC_LEER;
                case "SPIMP_MSG_CEC_FEHLER": return Resource.SPIMP_MSG_CEC_FEHLER;
                case "SPIMP_MSG_CEC_ALT": return Resource.SPIMP_MSG_CEC_ALT;
                case "SPIMP_MSG_CEC_KEINE_QUELLE": return Resource.SPIMP_MSG_CEC_KEINE_QUELLE;

                default: return schluessel;
            }
        }

        /// <summary>Eine <see cref="PruefMeldung"/> als fertiger Satz.</summary>
        public static string Zu(PruefMeldung meldung)
        {
            if (meldung == null) return "";
            string vorlage = Zu(meldung.Schluessel) ?? meldung.Schluessel;
            return meldung.Werte.Length == 0
                ? vorlage
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, meldung.Werte);
        }

        /// <summary>Eine <see cref="SpeicherImportMeldung"/> als fertiger Satz.</summary>
        public static string Zu(SpeicherImportMeldung meldung)
        {
            string vorlage = Zu(meldung.Schluessel);
            return meldung.Werte.Length == 0
                ? vorlage
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, meldung.Werte);
        }

        /// <summary>Ein <see cref="ImportFortschritt"/> als fertiger Satz.</summary>
        public static string Zu(ImportFortschritt fortschritt)
        {
            string vorlage = Zu(fortschritt.Schluessel);
            return fortschritt.Werte.Length == 0
                ? vorlage
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, fortschritt.Werte);
        }
    }
}
