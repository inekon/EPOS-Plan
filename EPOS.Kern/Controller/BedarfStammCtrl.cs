using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Verteiler der drei Bedarfs-STAMMKOEPFE (iU9-W8.0b) — Datenseite von
    /// <c>EPOS.UI/Dialoge/Bedarf/TypStammDialog.razor</c>, das
    /// <c>Form_EingDBStromverbraucher</c>, <c>Form_EingDBProzess</c> und
    /// <c>Form_EingDBBrauchwasser</c> abloest.
    ///
    /// <para><b>Er rechnet nicht, er verteilt.</b> Hinter jeder Methode steht der bereits
    /// vorhandene Stammcontroller der Auspraegung (<see cref="StromverbraucherStammCtrl"/>,
    /// <see cref="ProzesswaermeStammCtrl"/> seit W8.0a, <see cref="BrauchwasserStammCtrl"/>).
    /// Die Komponente kennt nur diese eine Schnittstelle, die Anweisungen bleiben dort, wo
    /// sie schon standen — es entsteht keine vierte Fassung derselben SQL.</para>
    ///
    /// <para>Die einzige EIGENE Abfrage ist die Typliste: Der Stromverbrauchertyp fuehrt
    /// seinen Namen in <c>Typname</c>, die beiden anderen in <c>Bezeichner</c>, und beide
    /// Listen sind nach genau dieser Spalte sortiert (Konstruktoren der drei Masken).</para>
    /// </summary>
    internal static class BedarfStammCtrl
    {
        /// <summary>Der Typkatalog einer Auspraegung: Tabelle und Namensspalte.</summary>
        internal static (string Tabelle, string Spalte) TypKatalog(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    return (StromverbraucherStammCtrl.TYP_STAMM, "Typname");
                case BedarfsArt.Prozesswaerme:
                    return (ProzesswaermeStammCtrl.TYP_STAMM, "Bezeichner");
                default:
                    return (BrauchwasserStammCtrl.TYP_STAMM, "Bezeichner");
            }
        }

        /// <summary>Die Kopftabelle einer Auspraegung (<c>Tab_*_STAMM</c>).</summary>
        internal static string KopfTabelle(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return StromverbraucherStammCtrl.TABLE;
                case BedarfsArt.Prozesswaerme:    return ProzesswaermeStammCtrl.TABLE;
                default:                          return BrauchwasserStammCtrl.TABLE;
            }
        }

        /// <summary>
        /// Die Typliste der Klappliste, in derselben Reihenfolge wie im Vorlaeufer:
        /// <c>ORDER BY Typname</c> beim Stromverbraucher, sonst <c>ORDER BY Bezeichner</c>.
        /// </summary>
        internal static IReadOnlyList<string> Typen(BedarfsArt art)
        {
            (string tabelle, string spalte) = TypKatalog(art);
            var liste = new List<string>();

            // Spaltennamen sind Bezeichner und koennen nicht parametrisiert werden; beide
            // Werte stammen aus TypKatalog und nicht aus einer Eingabe.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + spalte + " FROM " + tabelle + " ORDER BY " + spalte);
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
                if (row[0] != DBNull.Value) liste.Add(row[0].ToString());
            return liste;
        }

        /// <summary>
        /// Die zwoelf Monatswerte eines Kopfsatzes, oder <c>null</c>, wenn es ihn nicht gibt
        /// (Modus „Neu"): Der Vorlaeufer liess die Felder dann leer stehen.
        /// </summary>
        internal static double[] Monatswerte(BedarfsArt art, string bezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + KopfTabelle(art) + " WHERE Bezeichner = ?",
                new DbParam("@bez", bezeichner ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            var monat = new double[12];
            for (int i = 0; i < 12; i++)
            {
                string spalte = "Monat_" + (i + 1);
                monat[i] = (dt.Columns.Contains(spalte) && row[spalte] != DBNull.Value)
                    ? Convert.ToDouble(row[spalte]) : 0.0;
            }
            return monat;
        }

        /// <summary>Gibt es den Bezeichner schon? („Name existiert bereits!")</summary>
        internal static bool Exists(BedarfsArt art, string bezeichner)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return new StromverbraucherStammCtrl().Exists(bezeichner);
                case BedarfsArt.Prozesswaerme:    return new ProzesswaermeStammCtrl().Exists(bezeichner);
                default:                          return new BrauchwasserStammCtrl().Exists(bezeichner);
            }
        }

        /// <summary>Ist der Kopfsatz Auslieferungsbestand (<c>ReadOnly</c>)?</summary>
        internal static bool IstReadOnly(BedarfsArt art, string bezeichner)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return new StromverbraucherStammCtrl().IsReadOnly(bezeichner);
                case BedarfsArt.Prozesswaerme:    return new ProzesswaermeStammCtrl().IsReadOnly(bezeichner);
                default:                          return new BrauchwasserStammCtrl().IsReadOnly(bezeichner);
            }
        }

        /// <summary>
        /// Schreibt den Kopf. <paramref name="isNew"/> legt an, sonst wird ueberschrieben.
        ///
        /// <para>Die ReadOnly-Sperre prueft der Aufrufer VORHER ueber
        /// <see cref="IstReadOnly"/> — die <c>SaveHead</c> der drei Controller melden sie
        /// sonst ueber <c>Meldung.Hinweis</c>, und das waere in einer WebView ein modaler
        /// Kasten ueber dem Dialog statt eines Warnbanners darin.</para>
        /// </summary>
        internal static bool SaveHead(BedarfsArt art, string bez, string typ, string beschr,
                                      double[] monat, bool isNew)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    return new StromverbraucherStammCtrl().SaveHead(bez, typ, beschr, monat, isNew);
                case BedarfsArt.Prozesswaerme:
                    return new ProzesswaermeStammCtrl().SaveHead(bez, typ, beschr, monat, isNew);
                default:
                    return new BrauchwasserStammCtrl().SaveHead(bez, typ, beschr, monat, isNew);
            }
        }
    }
}
