using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Das Trennzeichen der Klimadatei (Auftrag KD-5).</summary>
    public enum KlimaTrennzeichen
    {
        Semikolon = 0,
        Komma = 1,
        Tabulator = 2
    }

    /// <summary>Das Dezimalzeichen der Klimadatei.</summary>
    public enum KlimaDezimalzeichen
    {
        Komma = 0,
        Punkt = 1
    }

    /// <summary>Die Zeitspalte(n) vor den Werten.</summary>
    public enum KlimaZeitspalte
    {
        /// <summary>Laufende Stunde 1 … 8760 — wie <see cref="ZeitreihenCsv"/>.</summary>
        Stunde = 0,

        /// <summary>Datum und Uhrzeit im Gemeinjahr, <c>TT.MM. hh:00</c> (Beginn der Stunde).</summary>
        DatumUhrzeit = 1,

        /// <summary>Beide Spalten: laufende Stunde und Datum mit Uhrzeit.</summary>
        StundeUndDatum = 2,

        /// <summary>ISO 8601 mit Bezugsjahr und Versatz (<c>2025-01-01T00:00+01:00</c> bzw. <c>…Z</c>).</summary>
        Iso = 3,

        /// <summary>Keine Zeitspalte.</summary>
        Keine = 4
    }

    /// <summary>Der Zeitbezug der Ausgabe. Vorgabe MEZ ohne Sommerzeit (Entscheid KD-Q8).</summary>
    public enum KlimaZeitbezug
    {
        /// <summary>Mitteleuropäische Zeit ohne Sommerzeit (UTC+1) — Raster der TRY und der Anwender.</summary>
        Mez = 0,

        /// <summary>Koordinierte Weltzeit — Raster von PVGIS und der Rechentabelle.</summary>
        Utc = 1
    }

    /// <summary>Wie der Spaltenkopf aussieht.</summary>
    public enum KlimaKopfart
    {
        /// <summary>„Name [Einheit]“ in einer Zeile — wie <see cref="ZeitreihenCsv"/>.</summary>
        EineZeile = 0,

        /// <summary>Namenzeile und darunter eine Einheitenzeile.</summary>
        MitEinheitenzeile = 1,

        /// <summary>Kein Spaltenkopf, nur Werte.</summary>
        Ohne = 2
    }

    /// <summary>Woher ein Wert stammt.</summary>
    public enum KlimaWertart
    {
        /// <summary>Eine Spalte der Rechentabelle <c>Tab_Solar(_STAMM)</c> — so, wie die Simulation sie liest.</summary>
        Rechenwert = 0,

        /// <summary>Ein Rohwert der Quelle aus <see cref="KlimaRohwerteSchema.TAB_ROH"/> (nur bei <c>Rohdaten = 1</c>).</summary>
        Rohwert = 1,

        /// <summary>Beim Export gerechnet, nie gespeichert (Taupunkt).</summary>
        Abgeleitet = 2
    }

    /// <summary>Die Gruppe einer Größe in der Parameterliste.</summary>
    public enum KlimaGroessengruppe
    {
        Rechenwerte = 0,
        Fassadenwerte = 1,
        Rohwerte = 2,
        Abgeleitet = 3
    }

    /// <summary>
    /// <b>Eine exportierbare Größe</b> (Auftrag KD-5): Schlüssel, Anzeigename (zugleich Spaltenkopf), Einheit, Herkunft
    /// und die Spalte der Datenbank. <see cref="Gerechnet"/> kennzeichnet, was nicht die Quelle führt, sondern das Haus
    /// rechnet (Fassadenwerte, Sonnenwinkel, Taupunkt).
    /// </summary>
    public sealed record KlimaExportGroesse(string Schluessel, Func<string> Name, string Einheit, KlimaWertart Art,
                                            KlimaGroessengruppe Gruppe, string Spalte, bool Gerechnet = false,
                                            bool Ganzzahl = false)
    {
        /// <summary>Der Spaltenkopf ohne Einheit: Rohwerte tragen den Zusatz „(Rohwert)“, damit kein Kopf doppelt steht.</summary>
        public string Kopfname => Art == KlimaWertart.Rohwert ? string.Format(CultureInfo.CurrentCulture, R.KEXP_ROH_NAME, Name()) : Name();
    }

    /// <summary>
    /// <b>Die Wahl des Klimadatenexports</b> (Auftrag KD-5, Konzept Klimadatenimport § 5): Parameter, Trennzeichen,
    /// Dezimalzeichen, Zeitspalte, Zeitbezug, Kopf und Herkunftsblock. Eine Region je Datei (KD-Q6). Die Formatfelder
    /// folgen den Namen von <see cref="GanglinienImportOptionen"/> (Trennzeichen, Dezimaltrenner, Kopfzeile), damit der
    /// spätere Rückweg (KD-6) dieselbe Beschreibung liest.
    /// </summary>
    public sealed class KlimaExportOptionen
    {
        /// <summary>Die gewählten Größen (Schlüssel aus <see cref="Groessen"/>), in Katalogreihenfolge geschrieben.</summary>
        public List<string> Parameter = new List<string>(VORGABE_PARAMETER);

        public KlimaTrennzeichen Trennzeichen = KlimaTrennzeichen.Semikolon;
        public KlimaDezimalzeichen Dezimalzeichen = KlimaDezimalzeichen.Komma;
        public KlimaZeitspalte Zeitspalte = KlimaZeitspalte.Stunde;

        /// <summary>Vorgabe MEZ ohne Sommerzeit (Entscheid KD-Q8).</summary>
        public KlimaZeitbezug Zeitbezug = KlimaZeitbezug.Mez;

        public KlimaKopfart Kopfart = KlimaKopfart.EineZeile;

        /// <summary>Herkunftsblock mit <c>#</c>-Zeilen (Region, Quelle, Ausführung, Koordinaten, Höhe, Lizenz).</summary>
        public bool Herkunft = true;

        /// <summary>
        /// Volle Genauigkeit (Rundlaufformat <c>R</c>) statt <c>0.0##</c> wie <see cref="ZeitreihenCsv"/>. Für den
        /// Rückweg KD-6, der die Rechenspalten bitgleich zurücklesen soll; der Dialog lässt die Hausvorgabe stehen.
        /// </summary>
        public bool VolleGenauigkeit;

        // ---------------------------------------------------------------------------------------------------

        // Schlüssel der Größen - EINE Quelle: der Reihenkatalog (KlimaReihenkatalog, KD-8), hier als Aliasse, damit
        // Export und Reihenwahl dieselben Schlüssel schreiben und lesen. R_ = Rechenwert, F_ = Fassadenwert/
        // Sonnenwinkel, ROH_ = Rohwert; A_ = abgeleitet (nur im Export).
        public const string R_TEMPERATUR = KlimaReihenkatalog.R_TEMPERATUR;
        public const string R_LUFTFEUCHTE = KlimaReihenkatalog.R_LUFTFEUCHTE;
        public const string R_GLOBAL = KlimaReihenkatalog.R_GLOBAL;
        public const string R_DIREKT_NORMAL = KlimaReihenkatalog.R_DIREKT_NORMAL;
        public const string R_DIFFUS = KlimaReihenkatalog.R_DIFFUS;
        public const string R_GEGENSTRAHLUNG = KlimaReihenkatalog.R_GEGENSTRAHLUNG;
        public const string R_BEDECKUNG = KlimaReihenkatalog.R_BEDECKUNG;
        public const string F_NORD = KlimaReihenkatalog.F_NORD;
        public const string F_OST = KlimaReihenkatalog.F_OST;
        public const string F_SUED = KlimaReihenkatalog.F_SUED;
        public const string F_WEST = KlimaReihenkatalog.F_WEST;
        public const string F_SONNENWINKEL = KlimaReihenkatalog.F_SONNENWINKEL;
        public const string A_TAUPUNKT = "A_TAUPUNKT";
        public const string ROH_PREFIX = KlimaReihenkatalog.ROH_PREFIX;

        /// <summary>Vorgabe: Temperatur, Feuchte, Global-, Direkt- und Diffusstrahlung (Konzept § 5).</summary>
        public static readonly IReadOnlyList<string> VORGABE_PARAMETER = new[]
        {
            R_TEMPERATUR, R_LUFTFEUCHTE, R_GLOBAL, R_DIREKT_NORMAL, R_DIFFUS
        };

        /// <summary>Das Jahr der ISO-Zeitstempel, wenn die Region kein Bezugsjahr im Gemeinjahr führt — das Referenzjahr des Hauses (<c>Init</c>).</summary>
        public const int ISO_JAHR_VORGABE = 2025;

        /// <summary>
        /// <b>Der Katalog der Größen</b> in Schreibreihenfolge: die Rechenspalten von <c>Tab_Solar(_STAMM)</c> und die
        /// Fassadenwerte samt Sonnenwinkel (Spalte und Einheit aus dem <see cref="KlimaReihenkatalog"/>), die 14 Rohspalten
        /// von <see cref="KlimaRohwerteSchema.ROHSPALTEN"/> und der abgeleitete Taupunkt.
        /// </summary>
        public static readonly IReadOnlyList<KlimaExportGroesse> Groessen = new List<KlimaExportGroesse>
        {
            Rechen(R_TEMPERATUR, () => R.KEXP_G_TEMPERATUR, KlimaGroessengruppe.Rechenwerte),
            Rechen(R_LUFTFEUCHTE, () => R.KEXP_G_LUFTFEUCHTE, KlimaGroessengruppe.Rechenwerte),
            Rechen(R_GLOBAL, () => R.KEXP_G_GLOBAL, KlimaGroessengruppe.Rechenwerte),
            Rechen(R_DIREKT_NORMAL, () => R.KEXP_G_DIREKT_NORMAL, KlimaGroessengruppe.Rechenwerte),
            Rechen(R_DIFFUS, () => R.KEXP_G_DIFFUS, KlimaGroessengruppe.Rechenwerte),
            Rechen(R_GEGENSTRAHLUNG, () => R.KEXP_G_GEGENSTRAHLUNG, KlimaGroessengruppe.Rechenwerte),
            Rechen(R_BEDECKUNG, () => R.KEXP_G_BEDECKUNG, KlimaGroessengruppe.Rechenwerte),
            Rechen(F_NORD, () => R.KEXP_G_SOL_NORD, KlimaGroessengruppe.Fassadenwerte, true),
            Rechen(F_OST, () => R.KEXP_G_SOL_OST, KlimaGroessengruppe.Fassadenwerte, true),
            Rechen(F_SUED, () => R.KEXP_G_SOL_SUED, KlimaGroessengruppe.Fassadenwerte, true),
            Rechen(F_WEST, () => R.KEXP_G_SOL_WEST, KlimaGroessengruppe.Fassadenwerte, true),
            Rechen(F_SONNENWINKEL, () => R.KEXP_G_SONNENWINKEL, KlimaGroessengruppe.Fassadenwerte, true),
            Roh(KlimaRohwerteSchema.SPALTE_TEMPERATUR, () => R.KEXP_G_TEMPERATUR, "°C"),
            Roh(KlimaRohwerteSchema.SPALTE_LUFTFEUCHTE, () => R.KEXP_G_LUFTFEUCHTE, "%"),
            Roh(KlimaRohwerteSchema.SPALTE_LUFTDRUCK, () => R.KEXP_G_LUFTDRUCK, "hPa"),
            Roh(KlimaRohwerteSchema.SPALTE_WINDGESCHWINDIGKEIT, () => R.KEXP_G_WINDGESCHWINDIGKEIT, "m/s"),
            Roh(KlimaRohwerteSchema.SPALTE_WINDRICHTUNG, () => R.KEXP_G_WINDRICHTUNG, "°"),
            Roh(KlimaRohwerteSchema.SPALTE_BEDECKUNGSGRAD, () => R.KEXP_G_BEDECKUNG, "Achtel"),
            Roh(KlimaRohwerteSchema.SPALTE_WASSERDAMPF, () => R.KEXP_G_WASSERDAMPF, "g/kg"),
            Roh(KlimaRohwerteSchema.SPALTE_GLOBAL, () => R.KEXP_G_GLOBAL, "W/m²"),
            Roh(KlimaRohwerteSchema.SPALTE_DIREKT_HORIZONTAL, () => R.KEXP_G_DIREKT_HORIZONTAL, "W/m²"),
            Roh(KlimaRohwerteSchema.SPALTE_DIREKT_NORMAL, () => R.KEXP_G_DIREKT_NORMAL, "W/m²"),
            Roh(KlimaRohwerteSchema.SPALTE_DIFFUS, () => R.KEXP_G_DIFFUS, "W/m²"),
            Roh(KlimaRohwerteSchema.SPALTE_GEGENSTRAHLUNG, () => R.KEXP_G_GEGENSTRAHLUNG, "W/m²"),
            Roh(KlimaRohwerteSchema.SPALTE_AUSSTRAHLUNG, () => R.KEXP_G_AUSSTRAHLUNG, "W/m²"),
            Roh(KlimaRohwerteSchema.SPALTE_QUALITAET, () => R.KEXP_G_QUALITAET, "", ganzzahl: true),
            new(A_TAUPUNKT, () => R.KEXP_G_TAUPUNKT, "°C", KlimaWertart.Abgeleitet, KlimaGroessengruppe.Abgeleitet, "", true)
        };

        private static KlimaExportGroesse Roh(string spalte, Func<string> name, string einheit, bool ganzzahl = false)
            => new(ROH_PREFIX + spalte, name, einheit, KlimaWertart.Rohwert, KlimaGroessengruppe.Rohwerte, spalte, false, ganzzahl);

        /// <summary>Eine Rechenspalte: Spalte und Einheit aus dem Reihenkatalog (eine Quelle), der Name aus dem Export.</summary>
        private static KlimaExportGroesse Rechen(string schluessel, Func<string> name, KlimaGroessengruppe gruppe, bool gerechnet = false)
        {
            KlimaReihe r = KlimaReihenkatalog.Reihe(schluessel)
                ?? throw new InvalidOperationException("Der Reihenkatalog kennt den Schlüssel " + schluessel + " nicht.");
            return new(schluessel, name, r.Einheit, KlimaWertart.Rechenwert, gruppe, r.Spalte, gerechnet);
        }

        /// <summary>Die Größe zu einem Schlüssel; <c>null</c> = unbekannt.</summary>
        public static KlimaExportGroesse Groesse(string schluessel)
            => Groessen.FirstOrDefault(g => string.Equals(g.Schluessel, schluessel, StringComparison.Ordinal));

        // ---------------------------------------------------------------------------------------------------

        /// <summary>Das Trennzeichen als Text.</summary>
        public string TrennzeichenText => Trennzeichen switch
        {
            KlimaTrennzeichen.Komma => ",",
            KlimaTrennzeichen.Tabulator => "\t",
            _ => ";"
        };

        /// <summary>Die Zahlkultur: Dezimalkomma wie das Haus (de-DE) oder Punkt (invariant).</summary>
        public CultureInfo Zahlkultur => Dezimalzeichen == KlimaDezimalzeichen.Komma
            ? CultureInfo.GetCultureInfo("de-DE")
            : CultureInfo.InvariantCulture;

        /// <summary>Komma als Trenn- UND Dezimalzeichen ergibt keine lesbare Datei — der Dialog sperrt das weich, der Schreiber verweigert es.</summary>
        public bool Widerspruechlich => Trennzeichen == KlimaTrennzeichen.Komma && Dezimalzeichen == KlimaDezimalzeichen.Komma;

        /// <summary>Vorlage „Haus (wie CSV am Bild)“: Semikolon, Komma, laufende Stunde, eine Kopfzeile.</summary>
        public void HausSetzen()
        {
            Trennzeichen = KlimaTrennzeichen.Semikolon;
            Dezimalzeichen = KlimaDezimalzeichen.Komma;
            Zeitspalte = KlimaZeitspalte.Stunde;
            Kopfart = KlimaKopfart.EineZeile;
        }

        /// <summary>Vorlage „International“: Komma, Punkt, ISO 8601, eine Kopfzeile.</summary>
        public void InternationalSetzen()
        {
            Trennzeichen = KlimaTrennzeichen.Komma;
            Dezimalzeichen = KlimaDezimalzeichen.Punkt;
            Zeitspalte = KlimaZeitspalte.Iso;
            Kopfart = KlimaKopfart.EineZeile;
        }

        /// <summary>Eine unabhängige Kopie.</summary>
        public KlimaExportOptionen Kopie()
        {
            var k = (KlimaExportOptionen)MemberwiseClone();
            k.Parameter = new List<string>(Parameter);
            return k;
        }
    }
}
