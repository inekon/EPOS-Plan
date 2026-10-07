using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Meldungsschlüssel der Projektdatei</b> (Präfix <see cref="PRAEFIX"/>, Texte in beiden Sprachen in
    /// <c>Resource.resx</c>) und die festen Code-Tabellen des Lesers (Regel 16.6 Nr. 2: nur die belegten Werte des Befunds
    /// werden gedeutet, jeder andere fällt benannt zurück).
    /// </summary>
    internal static class SqprojProtokoll
    {
        /// <summary>Das Präfix aller Meldungen der Projektdatei.</summary>
        internal const string PRAEFIX = "IMP_SQ_PROT_";

        internal const string KEINE_DATEI = PRAEFIX + "KEINE_DATEI";
        internal const string LESEFEHLER = PRAEFIX + "LESEFEHLER";
        internal const string TABELLE_FEHLT = PRAEFIX + "TABELLE_FEHLT";
        internal const string FASSUNG = PRAEFIX + "FASSUNG";
        internal const string FASSUNG_UNBEKANNT = PRAEFIX + "FASSUNG_UNBEKANNT";
        internal const string ZU_GROSS = PRAEFIX + "ZU_GROSS";
        internal const string KEIN_HOTTCAD = PRAEFIX + "KEIN_HOTTCAD";
        internal const string NICHT_GELESEN = PRAEFIX + "NICHT_GELESEN";
        internal const string ZONENTYP_UEBERSPRUNGEN = PRAEFIX + "ZONENTYP_UEBERSPRUNGEN";
        internal const string KLASSE_UEBERSPRUNGEN = PRAEFIX + "KLASSE_UEBERSPRUNGEN";
        internal const string BETRIEBSART = PRAEFIX + "BETRIEBSART";
        internal const string TAGESART_UNBEKANNT = PRAEFIX + "TAGESART_UNBEKANNT";
        internal const string TAGESART_ANNAHME = PRAEFIX + "TAGESART_ANNAHME";
        internal const string RAUMART_ABWEICHUNG = PRAEFIX + "RAUMART_ABWEICHUNG";
        internal const string ABSCHNITTSART_UNBEKANNT = PRAEFIX + "ABSCHNITTSART_UNBEKANNT";
        internal const string RAUM_OHNE_TREFFER = PRAEFIX + "RAUM_OHNE_TREFFER";
        internal const string RAUM_HERKUNFT = PRAEFIX + "RAUM_HERKUNFT";
        internal const string GUID_MEHRDEUTIG = PRAEFIX + "GUID_MEHRDEUTIG";
        internal const string IFC_RAUM_OHNE_GEGENSTUECK = PRAEFIX + "IFC_RAUM_OHNE_GEGENSTUECK";
        internal const string ZONE_LEER = PRAEFIX + "ZONE_LEER";
        internal const string ZONE_ABGELEHNT = PRAEFIX + "ZONE_ABGELEHNT";
        internal const string RAUM_BEHEIZUNG = PRAEFIX + "RAUM_BEHEIZUNG";
        internal const string NUTZUNG_OHNE_ABBILDUNG = PRAEFIX + "NUTZUNG_OHNE_ABBILDUNG";
        internal const string WERT_BEGRENZT = PRAEFIX + "WERT_BEGRENZT";
        internal const string BILANZ = PRAEFIX + "BILANZ";
        /// <summary>Die wirksame Zonierung und die andere vorhandene (E87): Zonierung, Zahl, andere, Zahl.</summary>
        internal const string ZONIERUNG = PRAEFIX + "ZONIERUNG";
        /// <summary>Die wirksame Zonierung, nur eine vorhanden (E87): Zonierung, Zahl.</summary>
        internal const string ZONIERUNG_EINE = PRAEFIX + "ZONIERUNG_EINE";

        /// <summary>Die Bauteiltabellen fehlen (BA-4b): Tabellen — es bleibt beim Stand ohne Aufbauten.</summary>
        internal const string BAUTEILE_FEHLEN = PRAEFIX + "BAUTEILE_FEHLEN";
        /// <summary>Die Bauteiltabellen sind nicht lesbar (BA-4b): Grund.</summary>
        internal const string BAUTEILE_UNLESBAR = PRAEFIX + "BAUTEILE_UNLESBAR";
        /// <summary>Gelesene Bauteile (BA-4b): Hüllflächen, Aufbauten, davon mit Schichten, Schichten.</summary>
        internal const string BAUTEILE = PRAEFIX + "BAUTEILE";
        /// <summary>Lage, Öffnungen oder Standort der Hüllflächen sind nicht lesbar: Grund — Aufbauten und Nettoflächen bleiben.</summary>
        internal const string LAGE_UNLESBAR = PRAEFIX + "LAGE_UNLESBAR";

        /// <summary>Der Belegtext je Zelle: Tabelle, Spalte, Profilnummer bzw. Profil.</summary>
        internal const string BELEG = "GIMP_BELEG_SQPROJ";

        /// <summary>Höchstens so viele Namen nennt eine Meldung (wie der Zonenplan).</summary>
        internal const int NAMEN_MAX = 5;

        /// <summary>Der Belegzusatz der Tagesart-Annahme (<c>GIMP_BELEG_SQPROJ_TAGESART</c>).</summary>
        internal const string BELEG_TAGESART = "GIMP_BELEG_SQPROJ_TAGESART";

        /// <summary>Die belegten Tagesarten als Annahme: 4 Montag–Freitag, 5 Montag–Samstag, 6 alle Tage; sonst unbekannt.</summary>
        internal static SqprojTagesart Tagesart(int? code)
            => code == 4 ? SqprojTagesart.Werktage : code == 5 ? SqprojTagesart.WerktageSamstag
             : code == 6 ? SqprojTagesart.AlleTage : SqprojTagesart.Unbekannt;

        /// <summary>Die Zahl der Tage je Woche einer angenommenen Tagesart (5, 6, 7); <c>null</c> bei unbekannt.</summary>
        internal static int? Wochentage(SqprojTagesart t)
            => t == SqprojTagesart.Werktage ? 5 : t == SqprojTagesart.WerktageSamstag ? 6 : t == SqprojTagesart.AlleTage ? 7 : (int?)null;

        /// <summary>
        /// Die Raumartcodes <c>BmRoom.RoomType</c> und ihr <c>mrt…</c>-Text (derselbe wie <c>HSETU_RaumAllgemein.RoomType</c>
        /// im IFC, Befund Kapitel 6) — nur ein Abgleichsbeleg, kein Schlüssel.
        /// </summary>
        internal static readonly IReadOnlyDictionary<int, string> RAUMARTEN = new SortedDictionary<int, string>
        {
            [0] = "mrtNone", [1] = "mrtLiving", [2] = "mrtSleeping", [3] = "mrtChild", [4] = "mrtKitchen", [5] = "mrtEating",
            [6] = "mrtHall", [7] = "mrtGuests", [9] = "mrtAdjoiningRoom", [10] = "mrtStorageRoom", [11] = "mrtBath", [12] = "mrtWC",
            [14] = "mrtOffice", [15] = "mrtConference", [23] = "mrtBasement", [24] = "mrtCentralHeating", [25] = "mrtRoof",
            [26] = "mrtStairway", [30] = "mrtSauna", [31] = "mrtFitness", [33] = "mrtLocker", [34] = "mrtStore", [35] = "mrtStorage",
            [36] = "mrtConnection", [41] = "mrtWorkshop", [42] = "mrtGarage", [44] = "mrtWintergarden", [52] = "mrtHallWay", [53] = "mrtShaft",
        };

        /// <summary>Der belegte Abschnittscode (<c>TaskPeriodType</c>): 1.</summary>
        internal const int ABSCHNITTSART_BELEGT = 1;

        /// <summary>Die Zahl als Text, invariant.</summary>
        internal static string Z(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Byte als Megabyte (1 MB = 1024 · 1024 Byte), invariant, bis eine Nachkommastelle — für <see cref="ZU_GROSS"/>.</summary>
        internal static string Mb(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("0.#", CultureInfo.InvariantCulture);

        /// <summary>Die Zahl als Text, invariant, bis vier Nachkommastellen.</summary>
        internal static string Z(double w) => w.ToString("0.####", CultureInfo.InvariantCulture);

        /// <summary>Bis zu <see cref="NAMEN_MAX"/> Namen, mit „…“, wenn es mehr sind.</summary>
        internal static string Namen(IEnumerable<string> namen)
        {
            List<string> l = (namen ?? Enumerable.Empty<string>()).ToList();
            string s = string.Join(", ", l.Take(NAMEN_MAX));
            return l.Count > NAMEN_MAX ? s + ", …" : s;
        }

        /// <summary>
        /// <b>Der Belegtext</b> einer Zelle (<c>GIMP_BELEG_SQPROJ</c>, ≤ 200 Zeichen): Tabelle, Spalte und Profilnummer
        /// (bzw. der Profilname, wenn es keine Nummer gibt) — in der Sprache der Oberfläche.
        /// </summary>
        internal static string Beleg(Zellbeleg b)
        {
            if (b == null) return null;
            string profil = b.Profilnummer.HasValue ? Z(b.Profilnummer.Value) : (b.Profil ?? "");
            string text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.GIMP_BELEG_SQPROJ, b.Tabelle, b.Spalte, profil);
            if (b.TagesartCode is int code && b.Tage is int tage)
                text += "; " + string.Format(CultureInfo.CurrentCulture, MyResource.Resource.GIMP_BELEG_SQPROJ_TAGESART, Z(code), Z(tage));
            return text.Length <= KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN ? text : text.Substring(0, KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN);
        }
    }
}
