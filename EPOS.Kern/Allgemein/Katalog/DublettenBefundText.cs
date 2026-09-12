using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Gegenüberstellung zweier Sätze einer Dublettengruppe (iU9-W14c.0f).
    /// </summary>
    public sealed class Gegenueberstellung
    {
        public Gegenueberstellung(int idA, string nameA, int idB, string nameB,
                                  IReadOnlyList<(string Spalte, string A, string B)> zeilen)
        {
            IdA = idA;
            NameA = nameA ?? "";
            IdB = idB;
            NameB = nameB ?? "";
            Zeilen = zeilen ?? Array.Empty<(string, string, string)>();
        }

        public int IdA { get; }
        public string NameA { get; }
        public int IdB { get; }
        public string NameB { get; }

        /// <summary>Die ABWEICHENDEN Spalten mit beiden Werten.</summary>
        public IReadOnlyList<(string Spalte, string A, string B)> Zeilen { get; }
    }

    /// <summary>
    /// Der Detailtext des Dublettenbefunds — <b>ohne <c>DataRow</c> nach außen</b>
    /// (iU9-W14c.0f, Befund W14c-B42).
    ///
    /// <para><b>Warum das hier steht.</b> <c>Form_KatalogDubletten.DetailText</c> und
    /// <c>Zelle</c> lasen unmittelbar aus <see cref="DataRow"/> und
    /// <see cref="DataTable"/>. Eine Razor-Komponente darf das nicht (Hausregel
    /// „Keine Datenbank" in <c>EPOS.UI/CLAUDE.md</c>) — ohne diesen Schritt zöge
    /// <c>EPOS.UI</c> <c>System.Data</c> herein. Hier steht die Umsetzung von einer
    /// Datenzeile zu Spalten- und Wertepaaren; die Anzeige setzt daraus ihren Text
    /// zusammen.</para>
    ///
    /// <para><b>Die Auswahl der Spalten ist unverändert:</b> beim Satz die
    /// Namensspalte plus <c>DublettenPruefung.VergleichsSpalten</c>, bei der Gruppe
    /// Satz 1 gegen jeden weiteren mit
    /// <c>DublettenPruefung.AbweichendeSpalten</c>.</para>
    /// </summary>
    public static class DublettenBefundText
    {
        /// <summary>
        /// Die Felder EINES Satzes: die Namensspalte zuerst, dann die
        /// Vergleichsspalten des Katalogs. Ohne Datenzeile eine leere Liste.
        /// </summary>
        public static IReadOnlyList<(string Spalte, string Wert)> Blatt(KatalogDefinition k, KatalogSatz satz)
        {
            var zeilen = new List<(string, string)>();
            if (k == null || satz == null || satz.Zeile == null) return zeilen;

            zeilen.Add((k.NamensSpalte, Zelle(satz.Zeile, k.NamensSpalte)));
            foreach (string sp in DublettenPruefung.VergleichsSpalten(k, satz.Zeile.Table))
                zeilen.Add((sp, Zelle(satz.Zeile, sp)));
            return zeilen;
        }

        /// <summary>
        /// Die Gegenüberstellungen EINER Gruppe: der erste Satz gegen jeden weiteren.
        /// Eine Gruppe mit weniger als zwei Sätzen liefert eine leere Liste.
        /// </summary>
        public static IReadOnlyList<Gegenueberstellung> Gruppe(KatalogDefinition k, DublettenGruppe g)
        {
            var liste = new List<Gegenueberstellung>();
            if (k == null || g == null || g.Saetze.Count < 2) return liste;

            KatalogSatz erster = g.Saetze[0];
            for (int i = 1; i < g.Saetze.Count; i++)
            {
                KatalogSatz zweiter = g.Saetze[i];
                var zeilen = new List<(string, string, string)>();
                foreach (string sp in DublettenPruefung.AbweichendeSpalten(k, erster, zweiter))
                    zeilen.Add((sp, Zelle(erster.Zeile, sp), Zelle(zweiter.Zeile, sp)));

                liste.Add(new Gegenueberstellung(erster.Id, erster.Name, zweiter.Id, zweiter.Name, zeilen));
            }
            return liste;
        }

        /// <summary>Zellwert als Anzeigetext; NULL und unbekannte Spalten bleiben leer.</summary>
        private static string Zelle(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            if (v == null || v is DBNull) return "";
            return Convert.ToString(v, CultureInfo.CurrentCulture);
        }
    }
}
