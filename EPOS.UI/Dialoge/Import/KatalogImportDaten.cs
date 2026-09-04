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
                            IDictionary<string, string> werte)
        {
            Name = name ?? "";
            Firma = firma ?? "";
            Filterwert = filterwert;
            Werte = werte ?? new Dictionary<string, string>();
        }

        /// <summary>Der Bezeichner, wie er in der Datei steht.</summary>
        public string Name { get; }

        /// <summary>Der Hersteller — zweite Spalte des Suchfilters.</summary>
        public string Firma { get; }

        /// <summary>Der Wert des Zahlenfilters (Leistung, Volumen, Aperturflaeche).</summary>
        public double Filterwert { get; }

        /// <summary>Die Detailtexte, Schluessel → Anzeigetext.</summary>
        public IDictionary<string, string> Werte { get; }

        /// <summary>Ein Detailtext, leer wenn die Zeile ihn nicht fuehrt.</summary>
        public string Wert(string schluessel)
        {
            string wert;
            return Werte.TryGetValue(schluessel, out wert) ? (wert ?? "") : "";
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

                case "IMP_KAT_EINH_KWTH": return Resource.IMP_KAT_EINH_KWTH;
                case "IMP_KAT_EINH_KW": return Resource.IMP_KAT_EINH_KW;
                case "IMP_KAT_EINH_PROZENT": return Resource.IMP_KAT_EINH_PROZENT;
                case "IMP_KAT_EINH_KWHD": return Resource.IMP_KAT_EINH_KWHD;
                case "IMP_KAT_EINH_LITER": return Resource.IMP_KAT_EINH_LITER;
                case "IMP_KAT_EINH_M2": return Resource.IMP_KAT_EINH_M2;
                case "IMP_KAT_EINH_WM2": return Resource.IMP_KAT_EINH_WM2;
                case "IMP_KAT_EINH_WM2K": return Resource.IMP_KAT_EINH_WM2K;
                case "IMP_KAT_EINH_KWCOOL": return Resource.IMP_KAT_EINH_KWCOOL;

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

                default: return schluessel;
            }
        }

        /// <summary>Eine <see cref="PruefMeldung"/> als fertiger Satz.</summary>
        public static string Zu(PruefMeldung meldung)
        {
            if (meldung == null) return "";
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
