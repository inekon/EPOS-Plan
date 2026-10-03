using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE KATALOGEMPFEHLUNG DER HILFSENERGIE AUF WEG B - Auftrag P671 (Register E30-Q12, EZ-24;
    // Anwenderentscheid 03.10.2026 "Setze jetzt fort").
    //
    // WOZU. Die Pflichtzeilen "Hilfsenergiekosten" (BHKW) und "Hilfsenergiekosten (Strom)"
    // (Heizkessel) der Betriebsvorlage "Standard" rechnen seit Schritt 94 als Anteil des
    // ENDENERGIEBEDARFS (Weg B): Brennstoffmenge der Anlage x Satz / 100 = Strommenge, bewertet mit
    // dem Strombezugspreis. Ihre Empfehlungsspannen stammen aber aus Weg A (Anteil der
    // Brennstoffkosten, DbWerte "ERFAHRUNGSWERTE gelten fuer Weg A"): BHKW 2-4 %, Heizkessel 4-8 %.
    // In Weg B sind sie um das Preisverhaeltnis Strom zu Brennstoff zu hoch - am Kessel von 1030
    // ergaeben sie 54 000 bis 108 000 EUR/a.
    //
    // DIE NEUEN SPANNEN (Weg B). Heizkessel 1,0-2,0 % (Brenner, Geblaese, Regelung, Pumpen des
    // Kesselkreises; nach VDI 2067 Blatt 1 und ueblichen Herstellerangaben rund 1 % des
    // Brennstoffeinsatzes bei Geblaesekesseln, bis 2 % bei Festbrennstoff), BHKW 0,5-1,5 %
    // (Eigenbedarf 1-3 % der elektrischen Arbeit, auf den Brennstoffeinsatz bezogen 0,4-1 %,
    // zuzueglich Pumpen). Gegenprobe als Umrechnung der alten Spanne: Kostenanteil x Brennstoffpreis
    // / Strompreis (rund 8 ct / 30 ct) ergibt 1,1-2,1 % am Kessel und 0,5-1,1 % am BHKW - die neuen
    // Spannen decken das. Die Werte stehen EINMAL, in der Saat (SchemaKatalog.Schritt39_Vorlagen);
    // dieser Schritt liest sie dort.
    //
    // WAS DER SCHRITT TUT. Er setzt Empfehlung_von/Empfehlung_bis der beiden Positionen in den
    // AUSLIEFERUNGSVORLAGEN (Tab_KostenVorlage.ReadOnly = 1, Kategorie Betrieb, Komponente BHKW
    // bzw. Heizkessel), die noch die alte Spanne tragen und mit Weg B rechnen. Reines DML, keine
    // Spalte, keine Tabelle; die STRICT-Zahl bleibt.
    //
    // NUR KATALOGZEILEN. Tab_ProjektWerte fuehrt keine Empfehlung und wird nicht beruehrt; der
    // gepflegte Satz einer Projektzeile bleibt, wie er ist. Eine eigene Vorlage des Anwenders
    // (ReadOnly = 0) bleibt ebenso, wie er sie angelegt hat - auch wenn sie die alte Spanne traegt.
    // Eine Auslieferungszeile auf Weg A behielte ihre Spanne zu Recht und wird nicht getroffen.
    //
    // ERGEBNISNEUTRAL. Die Empfehlung ist Hinweis am Satzfeld, kein Rechenwert; die Saat laesst den
    // Satz leer. Kein Referenzlauf und kein Anker rechnet anders, keine Einfrierregel greift.
    //
    // WIEDERHOLBAR. Getroffen wird nur, was die ALTE Spanne traegt; ein zweiter Lauf findet nichts
    // mehr (Offen() = 0).
    //
    // VIER LESER: der Schemaschritt der Schale (WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs), die Paketanhebung (Stufe Katalog - ein Paket fuehrt keine Vorlagen), das
    // Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung samt Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// <b>Der Schemaschritt „Katalogempfehlung der Hilfsenergie auf Weg B“</b> (Auftrag P671,
    /// Register E30‑Q12, EZ‑24) — EINE Quelle für Migration, Paketanhebung, Werkzeug und
    /// Testvorrichtung. Anlass, Spannen und Herleitung stehen im Kopf der Datei.
    /// </summary>
    public static class HilfsenergieEmpfehlungNachzug
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Sie folgt
        /// lückenlos auf <see cref="StromViertelstundenSchema.SCHRITT"/> (168, Welle M5, zeitgleich
        /// gebaut und zuerst auf dem Arbeitszweig; davor 167 die Teillastfelder der Welle M4); wird der
        /// Schritt beim Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = StromViertelstundenSchema.SCHRITT + 1;

        /// <summary>Die Positionen der Kostenvorlagen — nur sie fasst der Schritt an.</summary>
        public const string TABELLE = SchemaKatalog.TAB_KOSTENVORLAGEPOSITION;

        /// <summary>Weg B — die Bemessung, an der die neue Spanne gilt.</summary>
        public const string BEMESSUNG = DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF;

        // Die Anweisungen AUSGESCHRIEBEN, damit Werkzeuge/SqlDialektPruefer jede als Ganzes sieht.
        // Kein Alias und keine Verbundtabelle im UPDATE (BETRIEB_SQLITE § 6.2): Position -> Vorlage ->
        // Gewerk laeuft ueber zwei geschachtelte IN-Mengen, wie in Schritt 94.

        /// <summary>Setzt die neue Spanne — nur an Zeilen, die noch die alte tragen.</summary>
        internal const string SQL_NACHZIEHEN =
            "UPDATE Tab_KostenVorlagePosition SET Empfehlung_von = ?, Empfehlung_bis = ? " +
            "WHERE Bezeichnung = ? AND Bemessung = ? AND Empfehlung_von = ? AND Empfehlung_bis = ? " +
            "AND VorlageID IN (SELECT ID FROM Tab_KostenVorlage WHERE ReadOnly = 1 AND KategorieID = ? " +
            "AND KomponentenID IN (SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?))";

        /// <summary>Zählt die Zeilen einer Spanne — dieselbe Bedingung wie das Nachziehen.</summary>
        internal const string SQL_ZAEHLEN =
            "SELECT COUNT(*) FROM Tab_KostenVorlagePosition " +
            "WHERE Bezeichnung = ? AND Bemessung = ? AND Empfehlung_von = ? AND Empfehlung_bis = ? " +
            "AND VorlageID IN (SELECT ID FROM Tab_KostenVorlage WHERE ReadOnly = 1 AND KategorieID = ? " +
            "AND KomponentenID IN (SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?))";

        /// <summary>Eine Position, deren Empfehlung der Schritt nachzieht.</summary>
        public sealed class Ziel
        {
            internal Ziel(string komponente, string position, double altVon, double altBis)
            {
                Komponente = komponente;
                Position = position;
                AltVon = altVon;
                AltBis = altBis;
                SchemaKatalog.VorlagenPositionSeed saat = Saatposition(komponente, position);
                NeuVon = saat.EmpfehlungVon.Value;
                NeuBis = saat.EmpfehlungBis.Value;
            }

            /// <summary><c>Tab_KostenKomponente.Komponente</c>.</summary>
            public string Komponente { get; }

            /// <summary><c>Tab_KostenVorlagePosition.Bezeichnung</c>.</summary>
            public string Position { get; }

            /// <summary>Die Spanne aus Weg A [%], die der Schritt verlässt.</summary>
            public double AltVon { get; }

            /// <inheritdoc cref="AltVon"/>
            public double AltBis { get; }

            /// <summary>Die Spanne in Weg B [%] — aus der Saat.</summary>
            public double NeuVon { get; }

            /// <inheritdoc cref="NeuVon"/>
            public double NeuBis { get; }

            /// <summary>Für Protokoll und Bericht.</summary>
            public override string ToString()
                => Komponente + "/" + Position + " " + Prozent(AltVon) + "-" + Prozent(AltBis) + " % -> " +
                   Prozent(NeuVon) + "-" + Prozent(NeuBis) + " %";
        }

        /// <summary>Die zwei Positionen: BHKW und Heizkessel, mit der alten Spanne aus Weg A.</summary>
        public static IReadOnlyList<Ziel> Ziele { get; } = new[]
        {
            new Ziel(DbWerte.KOSTEN_KOMPONENTE_BHKW, "Hilfsenergiekosten", 2.0, 4.0),
            new Ziel(DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL, "Hilfsenergiekosten (Strom)", 4.0, 8.0),
        };

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Nachgezogene Zeilen je Ziel, in der Reihenfolge von <see cref="Ziele"/>.</summary>
            public List<int> JeZiel { get; } = new List<int>();

            /// <summary>Zahl der nachgezogenen Zeilen.</summary>
            public int Nachgezogen
            {
                get
                {
                    int n = 0;
                    foreach (int z in JeZiel) n += z;
                    return n;
                }
            }

            /// <summary>Die Zeilen für Protokoll und Werkzeug.</summary>
            public IEnumerable<string> Zeilen()
            {
                yield return Nachgezogen.ToString(CultureInfo.InvariantCulture) +
                             " Vorlagenposition(en) der Auslieferung auf die Empfehlung in Weg B gestellt";
                for (int i = 0; i < JeZiel.Count && i < Ziele.Count; i++)
                    yield return Ziele[i] + ": " + JeZiel[i].ToString(CultureInfo.InvariantCulture) + " Zeile(n)";
            }
        }

        /// <summary>Stehen die drei Tabellen samt der gelesenen Spalten? Ohne sie tut der Schritt nichts.</summary>
        public static bool Vorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON)
                && DataRepository.SpalteVorhanden(TABELLE, SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS)
                && DataRepository.SpalteVorhanden(TABELLE, SchemaKatalog.SPALTE_KVP_BEMESSUNG)
                && DataRepository.SpalteVorhanden(SchemaKatalog.TAB_KOSTENVORLAGE, SchemaKatalog.SPALTE_KV_READONLY)
                && DataRepository.SpalteVorhanden(SchemaKatalog.TAB_KOSTENKOMPONENTE, SchemaKatalog.SPALTE_KK_KOMPONENTE);
        }

        /// <summary>Wie viele Zeilen tragen noch die alte Spanne? 0 = der Schritt steht.</summary>
        public static int Offen()
        {
            if (!Vorhanden()) return 0;
            int n = 0;
            foreach (Ziel z in Ziele) n += Zaehlen(z, z.AltVon, z.AltBis);
            return n;
        }

        /// <summary>Wie viele Zeilen tragen die neue Spanne? Für Protokoll und Nachweis.</summary>
        public static int Nachgezogen()
        {
            if (!Vorhanden()) return 0;
            int n = 0;
            foreach (Ziel z in Ziele) n += Zaehlen(z, z.NeuVon, z.NeuBis);
            return n;
        }

        /// <summary>Steht der Schritt? Keine Auslieferungszeile trägt mehr die alte Spanne.</summary>
        public static bool Vollstaendig() => Vorhanden() && Offen() == 0;

        /// <summary>
        /// Zieht die Spannen in der Datenbank des Anwenders nach (Migration, Werkzeug, Testvorrichtung).
        /// <paramref name="bericht"/> nimmt die Zeilen auf und darf <c>null</c> sein. Ohne Kostenkatalog
        /// tut der Schritt nichts. Fehler werfen — der Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren(IList<string> bericht)
        {
            var b = new Bericht();
            if (!Vorhanden())
            {
                bericht?.Add("Kostenkatalog ohne Vorlagenpositionen - nichts nachzuziehen");
                return b;
            }
            foreach (Ziel z in Ziele)
                b.JeZiel.Add(DataRepository.ExecuteNonQuery(SQL_NACHZIEHEN, ParameterNachziehen(z)));
            if (bericht != null)
                foreach (string zeile in b.Zeilen()) bericht.Add(zeile);
            return b;
        }

        /// <summary>Die Parameter des Nachziehens: neue Spanne, dann die Bedingung mit der alten.</summary>
        internal static DbParam[] ParameterNachziehen(Ziel z)
        {
            return new[]
            {
                new DbParam("@nv", z.NeuVon),
                new DbParam("@nb", z.NeuBis),
                new DbParam("@pos", z.Position),
                new DbParam("@bem", BEMESSUNG),
                new DbParam("@av", z.AltVon),
                new DbParam("@ab", z.AltBis),
                new DbParam("@kat", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@komp", z.Komponente),
            };
        }

        /// <summary>Die Parameter der Zählung einer Spanne.</summary>
        internal static DbParam[] ParameterZaehlen(Ziel z, double von, double bis)
        {
            return new[]
            {
                new DbParam("@pos", z.Position),
                new DbParam("@bem", BEMESSUNG),
                new DbParam("@v", von),
                new DbParam("@b", bis),
                new DbParam("@kat", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@komp", z.Komponente),
            };
        }

        private static int Zaehlen(Ziel z, double von, double bis)
        {
            object o = DataRepository.ExecuteScalar(SQL_ZAEHLEN, ParameterZaehlen(z, von, bis));
            if (o == null || o == DBNull.Value) return 0;
            return Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Saatposition der Betriebsvorlage eines Gewerks — die eine Quelle der Spanne.</summary>
        internal static SchemaKatalog.VorlagenPositionSeed Saatposition(string komponente, string position)
        {
            foreach (SchemaKatalog.KostenVorlagenSeed v in SchemaKatalog.Schritt39_Vorlagen)
            {
                if (v.KategorieId != DbWerte.KOSTEN_KATEGORIE_BETRIEB ||
                    !string.Equals(v.Komponente, komponente, StringComparison.Ordinal)) continue;
                foreach (SchemaKatalog.VorlagenPositionSeed p in v.Positionen)
                    if (string.Equals(p.Bezeichnung, position, StringComparison.Ordinal) &&
                        p.EmpfehlungVon.HasValue && p.EmpfehlungBis.HasValue)
                        return p;
            }
            throw new InvalidOperationException("Die Saat fuehrt keine Empfehlung fuer " + komponente + "/" + position + ".");
        }

        private static string Prozent(double wert) => wert.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
