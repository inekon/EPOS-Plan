using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Wem ein Kalender oder eine Vorgabezeile gehört</b> (Konzept Konditionierungsprofile 5.1,
    /// Eigentümerregel): genau ein Gebäude (mit oder ohne Zone), ein Katalogbau (P3 (b)) oder eine
    /// Vorlage (P11, KP1b).
    /// </summary>
    public enum Kalendereigentuemer
    {
        /// <summary>Ein Projektgebäude ohne Zone.</summary>
        Gebaeude,

        /// <summary>Eine Zone eines Projektgebäudes; <c>ID_Gebaeude</c> ist das Gebäude der Zone.</summary>
        Zone,

        /// <summary>Ein Katalogbau (<c>Tab_Gebaeude_STAMM</c>), Entscheid P3 (b).</summary>
        Katalogbau,

        /// <summary>Eine Vorlage (<c>Tab_Konditionierungsvorlage_STAMM</c>) — die Tabelle entsteht mit KP1b.</summary>
        Vorlage,

        /// <summary>
        /// Eine Zone eines Katalogbaus (<c>Tab_Zone_STAMM</c>, Schritt ZK); <c>ID_Gebaeude_Stamm</c> ist der Katalogbau
        /// der Zone.
        /// </summary>
        Katalogzone,
    }

    /// <summary>
    /// <b>Eine Zeile von <c>Tab_Konditionierungskalender</c></b>, wie sie in der Datenbank steht —
    /// ungeprüft, mit den Nullwerten der Spalten. Der Datenweg füllt sie, <see cref="Kalenderleser"/>
    /// macht daraus ein <see cref="Konditionierungskalender"/>. <b>Ohne Datenbankzugriff:</b> Die
    /// Klasse kennt kein <c>DataRepository</c> — so bleibt das Kalendermodell prüfbar ohne Datei.
    /// </summary>
    public sealed class Kalenderzeile
    {
        /// <summary>Die Id der Zeile.</summary>
        public long Id { get; set; }

        /// <summary>Das Gebäude (bei <see cref="Kalendereigentuemer.Zone"/> das Gebäude der Zone), sonst <c>null</c>.</summary>
        public long? IdGebaeude { get; set; }

        /// <summary>Die Zone, sonst <c>null</c>.</summary>
        public long? IdZone { get; set; }

        /// <summary>Der Katalogbau, sonst <c>null</c>.</summary>
        public long? IdGebaeudeStamm { get; set; }

        /// <summary>Die Vorlage, sonst <c>null</c> (KP1b).</summary>
        public long? IdVorlage { get; set; }

        /// <summary>Das Kennwort der Größe (<see cref="DbWerte.KOND_GROESSEN"/>).</summary>
        public string Groesse { get; set; }

        /// <summary>Die Grundangabe als Wert, sonst <c>null</c>.</summary>
        public double? Wert { get; set; }

        /// <summary>„aus“ als Grundangabe.</summary>
        public bool Aus { get; set; }

        /// <summary>Die Standardwoche als H8-Text, sonst <c>null</c>.</summary>
        public string Woche { get; set; }

        /// <summary>Der Nennwert [W] bei Geräten und Personen, sonst <c>null</c>.</summary>
        public double? Nennwert { get; set; }

        /// <summary>Der Vermerk eines Werkzeugs der Karte, sonst <c>null</c>.</summary>
        public string Bemerkung { get; set; }

        /// <summary>
        /// Der Eigentümer aus den vier Spalten — die Auswertung der Regel, die der <c>CHECK</c> in
        /// der Datenbank hält. <c>false</c>, wenn keine oder mehr als eine Spalte belegt ist.
        /// </summary>
        public bool Eigentuemer(out Kalendereigentuemer e)
            => Kalendereigentuemerregel.Bestimmen(IdGebaeude, IdZone, IdGebaeudeStamm, IdVorlage, out e);
    }

    /// <summary>
    /// <b>Eine Zeile von <c>Tab_Konditionierungsperiode</c></b>, wie sie in der Datenbank steht.
    /// Ohne Datenbankzugriff, wie <see cref="Kalenderzeile"/>.
    /// </summary>
    public sealed class Periodenzeile
    {
        /// <summary>Die Id der Zeile.</summary>
        public long Id { get; set; }

        /// <summary>Der Kalender, an dem die Periode hängt.</summary>
        public long IdKalender { get; set; }

        /// <summary>Der Rang 1 … 999, je Kalender eindeutig.</summary>
        public int Rang { get; set; }

        /// <summary>Die Art (<see cref="DbWerte.KOND_ARTEN"/>).</summary>
        public string Art { get; set; }

        /// <summary>Der Bezeichner.</summary>
        public string Bezeichner { get; set; }

        /// <summary>Der erste Tag 1 … 365, sonst <c>null</c> (Feiertagsregel).</summary>
        public int? Beginn { get; set; }

        /// <summary>Der letzte Tag 1 … 365, sonst <c>null</c> (Feiertagsregel).</summary>
        public int? Ende { get; set; }

        /// <summary>Die Feiertagsregel (<see cref="DbWerte.KOND_FEIERTAGE"/>), sonst <c>null</c>.</summary>
        public string Feiertagsregel { get; set; }

        /// <summary>Die Angabe als Wert, sonst <c>null</c>.</summary>
        public double? Wert { get; set; }

        /// <summary>„aus“ als Angabe.</summary>
        public bool Aus { get; set; }

        /// <summary>Die eigene Woche als H8-Text, sonst <c>null</c>.</summary>
        public string Woche { get; set; }

        /// <summary>„wie Wochentag X“ (1 … 7), sonst <c>null</c>.</summary>
        public int? WieWochentag { get; set; }

        /// <summary>
        /// Die Größenmaske einer Periode des gemeinsamen Kalenders (<c>Gilt_Fuer</c>, Bit k = k-te Größe in
        /// <see cref="DbWerte.KOND_GROESSEN"/>); <c>null</c> in einem Größenkalender.
        /// </summary>
        public int? GiltFuer { get; set; }

        /// <summary>Der Verweis auf eine benannte Woche (<c>ID_Woche</c>); die gelesene <see cref="Woche"/> ist dann deren Text.</summary>
        public long? IdWoche { get; set; }
    }

    /// <summary>
    /// <b>Eine Zeile von <c>Tab_Konditionierungsvorgabe</c></b> (Konzept 5.6, P10 (b)) — eine Zelle
    /// der Vorgabe-Matrix je Eigentümer, Größe und Zeile. Ohne Datenbankzugriff.
    /// </summary>
    public sealed class Vorgabezeile
    {
        /// <summary>Die Id der Zeile.</summary>
        public long Id { get; set; }

        /// <summary>Das Gebäude (bei einer Zonenzeile das Gebäude der Zone), sonst <c>null</c>.</summary>
        public long? IdGebaeude { get; set; }

        /// <summary>Die Zone, sonst <c>null</c>.</summary>
        public long? IdZone { get; set; }

        /// <summary>Der Katalogbau, sonst <c>null</c>.</summary>
        public long? IdGebaeudeStamm { get; set; }

        /// <summary>Die Vorlage, sonst <c>null</c> (KP1b).</summary>
        public long? IdVorlage { get; set; }

        /// <summary>Das Kennwort der Größe.</summary>
        public string Groesse { get; set; }

        /// <summary>Das Kennwort der Zeile (<see cref="DbWerte.KOND_ZEILEN"/>).</summary>
        public string Zeile { get; set; }

        /// <summary>Der Wert der Zelle, sonst <c>null</c>.</summary>
        public double? Wert { get; set; }

        /// <summary>„aus“ — hat Vorrang vor dem Zahlenwert der Bestandsspalte (Konzept 5.6).</summary>
        public bool Aus { get; set; }

        /// <summary>Stunde 0 … 23 in der Zeile <c>NACHT</c>, Tag 1 … 365 in der Zeile <c>SAISON</c>, sonst <c>null</c>.</summary>
        public int? Von { get; set; }

        /// <summary>Wie <see cref="Von"/>, das andere Ende.</summary>
        public int? Bis { get; set; }

        /// <summary>ΔT der Nachtauskühlung [K] an Lüftung/Nacht; <c>null</c> heißt 2 K (P9).</summary>
        public double? BedingtK { get; set; }

        /// <summary>Der Eigentümer aus den vier Spalten (wie <see cref="Kalenderzeile.Eigentuemer"/>).</summary>
        public bool Eigentuemer(out Kalendereigentuemer e)
            => Kalendereigentuemerregel.Bestimmen(IdGebaeude, IdZone, IdGebaeudeStamm, IdVorlage, out e);
    }

    /// <summary>
    /// <b>Die Eigentümerregel als Funktion</b> — die eine Stelle, an der sie im Kern steht, Wort für
    /// Wort wie der <c>CHECK</c> in beiden Tabellen (Konzept 5.1): genau eine der drei Spalten
    /// <c>ID_Gebaeude</c>, <c>ID_Gebaeude_Stamm</c>, <c>ID_Vorlage</c>, und eine Zone nur mit
    /// Gebäude.
    /// </summary>
    public static class Kalendereigentuemerregel
    {
        /// <summary>
        /// Bestimmt den Eigentümer; <c>false</c>, wenn die Regel verletzt ist (keine oder mehr als
        /// eine Spalte belegt, oder eine Zone ohne Gebäude) — dann ist es ein Datenfehler und keine
        /// stille Umdeutung.
        /// </summary>
        public static bool Bestimmen(long? idGebaeude, long? idZone, long? idGebaeudeStamm, long? idVorlage,
                                     out Kalendereigentuemer e)
        {
            e = Kalendereigentuemer.Gebaeude;
            int belegt = (idGebaeude.HasValue ? 1 : 0) + (idGebaeudeStamm.HasValue ? 1 : 0) +
                         (idVorlage.HasValue ? 1 : 0);
            if (belegt != 1) return false;
            if (idZone.HasValue && !idGebaeude.HasValue) return false;
            if (idGebaeude.HasValue) e = idZone.HasValue ? Kalendereigentuemer.Zone : Kalendereigentuemer.Gebaeude;
            else if (idGebaeudeStamm.HasValue) e = Kalendereigentuemer.Katalogbau;
            else e = Kalendereigentuemer.Vorlage;
            return true;
        }

        /// <summary>
        /// Trifft die Zeile diesen Eigentümer? Für die Kaskade Zone → Gebäude (F2): Eine
        /// Gebäudezeile trägt keine Zone, eine Zonenzeile genau diese.
        /// </summary>
        public static bool Trifft(long? zeileGebaeude, long? zeileZone, long? gesuchtGebaeude, long? gesuchtZone)
            => zeileGebaeude == gesuchtGebaeude && zeileZone == gesuchtZone;

        /// <summary>Die Zeilen eines Eigentümers aus einer Liste — ohne Datenbankabfrage, für Vorschau und Probe.</summary>
        public static List<T> Filtern<T>(IEnumerable<T> zeilen, System.Func<T, bool> trifft)
        {
            var treffer = new List<T>();
            if (zeilen == null) return treffer;
            foreach (T z in zeilen)
                if (trifft(z)) treffer.Add(z);
            return treffer;
        }
    }
}
