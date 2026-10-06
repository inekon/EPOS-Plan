using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Dateiprofil der Projektdatei</b> (E87, F4): die Größengrenze, losgelöst von der IFC-Grenze
    /// (<see cref="IfcImportProfil"/>). Der Leser liest nur die Profil-, Zonen- und Raumtabellen; die Größe der Projektdateien
    /// stammt aus eingebetteten Bildern, die nie gelesen werden. Die Hülle wählt die Grenze je Plattform
    /// (<see cref="GrenzeFuerPlattform"/>) und belegt damit <see cref="GebaeudeImportAblauf.ProjektdateiMaxBytes"/>.
    /// </summary>
    internal static class SqprojProfil
    {
        /// <summary>Die Grenze unter Windows: 250 MB.</summary>
        internal const long MAX_BYTES = 250L * 1024 * 1024;

        /// <summary>Die Grenze auf iOS: 100 MB (bis zur Messung nach dem Muster G4-8 benannt abgelehnt darüber).</summary>
        internal const long MAX_BYTES_IOS = 100L * 1024 * 1024;

        /// <summary>Die Grenze einer Plattform.</summary>
        internal static long GrenzeFuerPlattform(bool ios) => ios ? MAX_BYTES_IOS : MAX_BYTES;
    }

    /// <summary>
    /// <b>Die Profilklassen der Projektdatei</b> (<c>PdProfile.ProfileType</c>, Befund 3.3): gelesen werden nur die fünf
    /// Klassen mit einer Größe in EPOS-Plan; alle anderen werden gezählt und benannt übersprungen.
    /// </summary>
    internal static class SqprojProfilklasse
    {
        /// <summary>DIN-V-18599-Nutzungsprofil (<c>PdProfileUsage</c>), ohne Zeitkurve.</summary>
        internal const int NUTZUNG = 2;
        /// <summary>Geräte (<c>PdProfileDevice</c>), Zeitkurve <c>Ratio</c>.</summary>
        internal const int GERAETE = 4;
        /// <summary>Heizen (<c>PdProfileHeating</c>), Zeitkurve <c>Temperature</c>.</summary>
        internal const int HEIZEN = 6;
        /// <summary>Kühlen (<c>PdProfileCooling</c>), Zeitkurve <c>Temperature</c>.</summary>
        internal const int KUEHLEN = 7;
        /// <summary>Personen (<c>PdProfilePerson</c>), Zeitkurve <c>Ratio</c>.</summary>
        internal const int PERSONEN = 8;
        /// <summary>Lüftung (<c>PdProfileVentilation</c>), Zeitkurve <c>SpecificRatedAirChange</c>.</summary>
        internal const int LUEFTUNG = 10;

        /// <summary>Die gelesenen Zeitprofilklassen in fester Reihenfolge (Regel 16.6 Nr. 5: Profile nach Klasse).</summary>
        internal static readonly int[] GELESEN = { HEIZEN, KUEHLEN, LUEFTUNG, GERAETE, PERSONEN };

        /// <summary>Die Größe einer gelesenen Klasse.</summary>
        internal static Konditionierungsgroesse Groesse(int klasse)
        {
            switch (klasse)
            {
                case HEIZEN: return Konditionierungsgroesse.Heizsoll;
                case KUEHLEN: return Konditionierungsgroesse.Kuehlsoll;
                case LUEFTUNG: return Konditionierungsgroesse.Lueftung;
                case GERAETE: return Konditionierungsgroesse.Geraete;
                case PERSONEN: return Konditionierungsgroesse.Personen;
                default: throw new ArgumentOutOfRangeException(nameof(klasse));
            }
        }

        /// <summary>Die Klasse einer Größe.</summary>
        internal static int Klasse(Konditionierungsgroesse g)
            => GELESEN.First(k => Groesse(k) == g);

        /// <summary>Die Spalte der Zeitkurve, die die Klasse trägt (für den Beleg).</summary>
        internal static string Kurvenspalte(int klasse)
            => klasse == HEIZEN || klasse == KUEHLEN ? "Temperature" : klasse == LUEFTUNG ? "SpecificRatedAirChange" : "Ratio";
    }

    /// <summary>
    /// <b>Die Tagesart einer Profilgruppe</b> (<c>PdProfileGroup.ProfileUsageDayType</c>). Belegt sind 4, 5 und 6; ihre
    /// Bedeutung ist im Befund offen. <b>Festlegung als Annahme</b> (benannt mit <c>IMP_SQ_PROT_TAGESART_ANNAHME</c> und im
    /// Beleg): 4 → Montag–Freitag, 5 → Montag–Samstag, 6 → alle Tage; jeder andere Code gilt als „alle Tage“ und wird benannt
    /// (<c>IMP_SQ_PROT_TAGESART_UNBEKANNT</c>, Regel 16.6 Nr. 2).
    /// </summary>
    internal enum SqprojTagesart
    {
        /// <summary>Die Kurve gilt an allen sieben Tagen (Code 6, angenommen).</summary>
        AlleTage,
        /// <summary>Montag bis Freitag (Code 4, angenommen); Samstag und Sonntag tragen „aus“ bzw. den Nachtwert.</summary>
        Werktage,
        /// <summary>Montag bis Samstag (Code 5, angenommen); der Sonntag trägt „aus“ bzw. den Nachtwert.</summary>
        WerktageSamstag,
        /// <summary>Ein ungedeuteter Code — behandelt wie <see cref="AlleTage"/>, benannt.</summary>
        Unbekannt,
    }

    /// <summary>
    /// <b>Das DIN-V-18599-10-Nutzungsprofil</b> einer Zone (<c>PdProfile</c> der Klasse 2 mit <c>PdProfileUsage</c>, Befund
    /// 3.3): die Profilnummer und die Spalten, die Kapitel 16.3 auf die Vorgabe-Matrix abbildet. <c>null</c> = keine Angabe.
    /// Zeiten sind Stunden 0 … 23 (die Delphi-Nullzeit heißt „keine Angabe“).
    /// </summary>
    internal sealed class SqprojNutzungsprofil
    {
        internal string Uuid { get; init; } = "";
        internal string Name { get; init; } = "";
        internal int? Profilnummer { get; init; }
        internal int? BetriebVon { get; init; }
        internal int? BetriebBis { get; init; }
        internal int? HeizVon { get; init; }
        internal int? HeizBis { get; init; }
        internal double? Personenzahl { get; init; }
        internal double? Raumtemperatur { get; init; }
        internal double? Absenkung { get; init; }
        internal double? Zuluftwechsel { get; init; }
        internal double? AussenluftJePerson { get; init; }
        internal double? AussenluftJeFlaeche { get; init; }
        internal double? VollnutzungPersonenH { get; init; }
        internal double? VollnutzungGeraeteH { get; init; }
        internal double? PersonenWm2 { get; init; }
        internal double? GeraeteWm2 { get; init; }
        /// <summary>Der Wartungswert der Beleuchtungsstärke [lx] (<c>MaintenanceIllumination</c>; NP4b, nur zur Beschreibung).</summary>
        internal double? Beleuchtungsstaerke { get; init; }

        /// <summary>Die Nutzungsstunden des Tages aus der Betriebszeit (über Mitternacht gerechnet); <c>null</c> ohne Zeiten.</summary>
        internal int? Betriebsstunden
            => BetriebVon is int v && BetriebBis is int b && v != b ? ((b - v) % 24 + 24) % 24 : (int?)null;
    }

    /// <summary>
    /// <b>Ein Gültigkeitsabschnitt eines Zeitprofils</b> (<c>PdProfileTaskSerial</c>, Befund 3.4): Tag 1 … 365 von bis (Start
    /// nach Ende = über den Jahreswechsel), die sieben Wochentagsschalter Montag … Sonntag und der Code
    /// <c>TaskPeriodType</c> (belegt: 1).
    /// </summary>
    internal sealed class SqprojAbschnitt
    {
        internal string Uuid { get; init; } = "";
        internal int? Abschnittsart { get; init; }
        internal int Beginn { get; init; } = 1;
        internal int Ende { get; init; } = 365;
        internal bool[] Wochentage { get; init; } = new bool[7];

        /// <summary>Ist ein Wochentagsschalter gesetzt?</summary>
        internal bool MitWochentagen => Wochentage.Any(w => w);

        /// <summary>Gilt der Abschnitt das ganze Jahr?</summary>
        internal bool Ganzjahr => Beginn == 1 && Ende == 365;
    }

    /// <summary>
    /// <b>Ein Zeitprofil einer Klasse</b> (<c>PdProfile</c> mit <c>PdProfileTimeCurve</c>, 24 Stunden, Stunde 1 … 24 →
    /// Index 0 … 23; <c>null</c> = Stunde ohne Wert), mit dem Nennwert der Zusatztabelle, der Betriebsart
    /// (<c>OperatingModeType</c>, nur gezählt und genannt) und den Abschnitten in fester Reihenfolge.
    /// </summary>
    internal sealed class SqprojZeitprofil
    {
        internal string Uuid { get; init; } = "";
        internal string Name { get; init; } = "";
        internal int Klasse { get; init; }
        internal double?[] Stunden { get; } = new double?[24];

        /// <summary>Die Betriebsart je Stunde (<c>OperatingModeType</c>: 1 Betriebsstunde, 2 außerhalb der Nutzungszeit); <c>null</c> = keine.</summary>
        internal int?[] Betriebsarten { get; } = new int?[24];
        internal int? Betriebsart { get; set; }

        /// <summary>Personen: Belegung aus <c>RatedPersonOccupancyRate</c> (gelesen als Personenzahl).</summary>
        internal double? Personen { get; init; }

        /// <summary>Personen: trockene Wärmeabgabe je Person [W] aus <c>SpecificRatedDryHeatEmission</c>.</summary>
        internal double? WattJePerson { get; init; }

        /// <summary>Geräte: spezifische Leistung [W/m²] aus der Zusatztabelle (erste vorhandene der Kandidatenspalten).</summary>
        internal double? GeraeteWm2 { get; init; }

        internal List<SqprojAbschnitt> Abschnitte { get; } = new List<SqprojAbschnitt>();

        /// <summary>Trägt die Kurve mindestens einen Stundenwert?</summary>
        internal bool HatKurve => Stunden.Any(s => s.HasValue);
    }

    /// <summary>
    /// <b>Eine Profilgruppe</b> (<c>PdProfileGroup</c>) einer Simulationszone: die Tagesart und je gelesener Klasse das
    /// erste Zeitprofil (nach Name, dann Kennung).
    /// </summary>
    internal sealed class SqprojProfilgruppe
    {
        internal string Uuid { get; init; } = "";
        internal string Name { get; init; } = "";
        internal int? TagesartCode { get; init; }

        /// <summary>Die Profilnummer der Gruppe (<c>PdProfileGroup.ProfileUsageType</c>); <c>null</c> = keine.</summary>
        internal int? Profilnummer { get; init; }

        /// <summary>Die Art der Gruppe (<c>ProfileGroupType</c>: 4 Gebäudegruppe, 5 Zonengruppe).</summary>
        internal int? Gruppenart { get; init; }
        internal SqprojTagesart Tagesart { get; init; }
        internal SortedDictionary<int, SqprojZeitprofil> Profile { get; } = new SortedDictionary<int, SqprojZeitprofil>();

        /// <summary>Das Zeitprofil einer Klasse; <c>null</c> = keines.</summary>
        internal SqprojZeitprofil Profil(int klasse) => Profile.TryGetValue(klasse, out SqprojZeitprofil p) ? p : null;
    }
}
