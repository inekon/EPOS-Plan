using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Wo der Wert einer Matrixzelle steht</b> — die EINE Stelle, an der „ein Ort je Zelle"
    /// (Konzept Konditionierungsprofile 5.6) als Tabelle steht, statt an jedem Schreib- und
    /// Leseweg neu behauptet zu werden.
    ///
    /// <para><b>Die Regel.</b> Je Größe und Zeile gibt es entweder eine <b>Bestandsspalte</b> am
    /// Eigentümer — dann trägt sie den Zahlenwert, und die Vorgabezeile trägt <b>nur</b> „aus",
    /// die Zeiten und <c>Bedingt_K</c> — oder keine; dann steht der Wert in der Vorgabezeile.
    /// Eine Zeile mit <c>Aus = 1</c> schlägt den Zahlenwert der Bestandsspalte.</para>
    ///
    /// <para><b>Je Eigentümerart.</b> Gebäude (<c>Tab_Gebaeude</c>), Zone (<c>Tab_Zone</c>) und
    /// Katalogbau (<c>Tab_Gebaeude_STAMM</c>) führen dieselben neun Bestandsspalten; eine
    /// <b>Vorlage</b> führt keine — sie trägt alle Zellen ihrer einen Größe in der Vorgabetabelle
    /// (Konzept 5.7). Die Nachtzeiten (<c>Nachtabsenkung_Beginn</c>/<c>_Ende</c>) sind
    /// <em>keine</em> Bestandsspalte dieser Tabelle: Zeiten gehören nach 5.6 in die Vorgabezeile,
    /// und die Zone führt sie ohnehin nicht.</para>
    ///
    /// <para><b>Ohne Datenbank</b> — eine Tabelle aus Namen; wer schreibt, holt sich Tabelle und
    /// Spalte hier und setzt beides als Bezeichner in seinen SQL-Text (Werte immer als <c>?</c>).</para>
    /// </summary>
    public static class Matrixzellenort
    {
        /// <summary>
        /// Der Ort einer Zelle: <see cref="Tabelle"/> und <see cref="Spalte"/> der Bestandsspalte.
        /// <see cref="Keiner"/> heißt „keine Bestandsspalte" — der Wert steht in der Vorgabezeile.
        /// </summary>
        public sealed record Ort(string Tabelle, string Spalte)
        {
            /// <summary>Die Zelle hat keine Bestandsspalte.</summary>
            public static readonly Ort Keiner = new Ort(null, null);

            /// <summary>Trägt die Zelle ihren Wert in einer Bestandsspalte?</summary>
            public bool IstBestandsspalte => !string.IsNullOrEmpty(Tabelle) && !string.IsNullOrEmpty(Spalte);
        }

        /// <summary>Die Tabelle eines Projektgebäudes.</summary>
        public const string TAB_GEBAEUDE = "Tab_Gebaeude";

        /// <summary>Die Tabelle eines Katalogbaus (P3 (b)).</summary>
        public const string TAB_KATALOGBAU = "Tab_Gebaeude_STAMM";

        /// <summary>Die Tabelle einer Zone.</summary>
        public const string TAB_ZONE = SchemaKatalog.TAB_ZONE;

        /// <summary>
        /// Die Bestandsspalten je Größe und Zeile — der Schlüssel ist
        /// „<c>&lt;Größe&gt;|&lt;Zeile&gt;</c>" mit den Kennwörtern aus <see cref="DbWerte"/>.
        /// Was hier fehlt, hat keine Bestandsspalte (Personen ganz, Saison überall,
        /// Kühlen/Wochenende und -/Ferien, Lüftung/Nacht … — Konzept 3.3).
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> SPALTEN =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Heizen: die vier Sollwerte sind Bestand, die Saison ist neu.
                { Schluessel(DbWerte.KOND_GROESSE_HEIZSOLL, DbWerte.KOND_ZEILE_TAG), "Raumsolltemperatur_Tag" },
                { Schluessel(DbWerte.KOND_GROESSE_HEIZSOLL, DbWerte.KOND_ZEILE_NACHT), "Raumsolltemperatur_Nachtabsenkung" },
                { Schluessel(DbWerte.KOND_GROESSE_HEIZSOLL, DbWerte.KOND_ZEILE_WOCHENENDE), "Raumsolltemperatur_Wochenende" },
                { Schluessel(DbWerte.KOND_GROESSE_HEIZSOLL, DbWerte.KOND_ZEILE_FERIEN), "Raumsolltemperatur_Ferien" },

                // Kühlen: Tag und Nacht sind Bestand (P13), alles Übrige ist neu.
                { Schluessel(DbWerte.KOND_GROESSE_KUEHLSOLL, DbWerte.KOND_ZEILE_TAG), "Kuehl_Sollwert" },
                { Schluessel(DbWerte.KOND_GROESSE_KUEHLSOLL, DbWerte.KOND_ZEILE_NACHT), "Kuehl_Sollwert_Nacht" },

                // Lüftung: Infiltration ist der Nennwert, Nutzerlüftung der Tagwert (F15).
                { Schluessel(DbWerte.KOND_GROESSE_LUEFTUNG, DbWerte.KOND_ZEILE_NENNWERT), "Luftwechsel_Infiltration" },
                { Schluessel(DbWerte.KOND_GROESSE_LUEFTUNG, DbWerte.KOND_ZEILE_TAG), "Luftwechsel_Nutzer" },

                // Geräte: der Nennwert [W]; die Anteilszeilen sind neu.
                { Schluessel(DbWerte.KOND_GROESSE_GERAETE, DbWerte.KOND_ZEILE_NENNWERT), "Interne_Waermegewinne" },

                // Personen: alles neu (Konzept 3.1).
            };

        /// <summary>Der Ort einer Zelle je Eigentümerart, Größe und Zeile.</summary>
        /// <param name="art">Wem die Zelle gehört.</param>
        /// <param name="groesse">Die Größe (Spalte der Matrix).</param>
        /// <param name="zeile">Das Zeilenkennwort (<see cref="DbWerte.KOND_ZEILEN"/>).</param>
        public static Ort Fuer(Kalendereigentuemer art, Konditionierungsgroesse groesse, string zeile)
        {
            string tabelle = Tabelle(art);
            if (tabelle == null) return Ort.Keiner;          // Vorlage: alles in der Vorgabezeile
            string spalte = Bestandsspalte(groesse, zeile);
            return spalte == null ? Ort.Keiner : new Ort(tabelle, spalte);
        }

        /// <summary>
        /// Trägt die Zelle ihren Zahlenwert in einer Bestandsspalte? Dann schreibt die
        /// Vorgabezeile ihn nicht, und der Leser nimmt ihn aus der Spalte (Konzept 5.6).
        /// </summary>
        public static bool HatBestandsspalte(Kalendereigentuemer art, Konditionierungsgroesse groesse, string zeile)
            => Fuer(art, groesse, zeile).IstBestandsspalte;

        /// <summary>
        /// Die Tabelle einer Eigentümerart; <c>null</c> für <see cref="Kalendereigentuemer.Vorlage"/>
        /// — eine Vorlage hat keine Bestandsspalten.
        /// </summary>
        public static string Tabelle(Kalendereigentuemer art)
        {
            switch (art)
            {
                case Kalendereigentuemer.Gebaeude: return TAB_GEBAEUDE;
                case Kalendereigentuemer.Zone: return TAB_ZONE;
                case Kalendereigentuemer.Katalogbau: return TAB_KATALOGBAU;
                default: return null;
            }
        }

        /// <summary>
        /// Die Bestandsspalte einer Zelle ohne Rücksicht auf den Eigentümer; <c>null</c>, wenn die
        /// Zelle keine hat. Die drei Tabellen führen dieselben Namen.
        /// </summary>
        public static string Bestandsspalte(Konditionierungsgroesse groesse, string zeile)
        {
            if (zeile == null) return null;
            return SPALTEN.TryGetValue(Schluessel(Konditionierungsgroessen.Kennwort(groesse), zeile), out string s)
                ? s : null;
        }

        private static string Schluessel(string groesse, string zeile) => groesse + "|" + zeile;
    }
}
