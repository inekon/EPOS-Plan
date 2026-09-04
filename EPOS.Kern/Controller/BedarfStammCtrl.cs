using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie das Loeschen eines Bedarfs-Kopfsatzes ausgegangen ist (iU9-W14b.0a).
    ///
    /// <para>Der Vorlaeufer kannte nur <c>true</c>/<c>false</c> und liess den
    /// Stammcontroller die Sperre selbst MELDEN — ein modaler Kasten mitten im
    /// Loeschweg. Hier ist der Grund ein Wert, und der Dialog entscheidet, ob daraus
    /// ein Warnbanner oder eine Erfolgsmeldung wird.</para>
    /// </summary>
    internal enum BedarfLoeschErgebnis
    {
        /// <summary>Der Satz ist weg.</summary>
        Geloescht = 0,

        /// <summary>Auslieferungsbestand (<c>ReadOnly</c>) — er bleibt stehen.</summary>
        Schreibgeschuetzt = 1,

        /// <summary>Das DELETE hat nicht gegriffen.</summary>
        Fehlgeschlagen = 2
    }

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

        /// <summary>
        /// Der JAHRESVERBRAUCH eines Katalogsatzes — die Summe seiner zwoelf Monatswerte
        /// (iU9-W9.0b). Der Vorlaeufer hiess in allen drei Bedarfsmasken
        /// <c>Prozesssumme</c> und stand dort dreimal wortgleich
        /// (<c>Form_Prozesswaerme</c>:212, <c>Form_Stromverbraucher</c>:99,
        /// <c>Form_Brauchwasser</c>:151).
        ///
        /// <para>Einen Satz, den es nicht gibt, wertet er wie der Vorlaeufer mit 0 —
        /// dort lief die Schleife bei <c>rows == 0</c> gar nicht erst.</para>
        /// </summary>
        internal static double Jahressumme(BedarfsArt art, string bezeichner)
        {
            double[] monat = Monatswerte(art, bezeichner);
            if (monat == null) return 0;

            double summe = 0;
            for (int i = 0; i < 12; i++) summe += monat[i];
            return summe;
        }

        /// <summary>
        /// Die LISTENSPALTE einer Auspraegung — was <c>SetControls</c> der drei
        /// Verwaltungsmasken in ihre Liste schrieb (iU9-W14b.0a).
        ///
        /// <para><b>Zwei Modellfelder, eine Spalte.</b> Die Prozesswaerme fuellte aus
        /// <c>m_szProzessname</c>, die beiden anderen aus <c>m_szBezeichner</c> — dahinter
        /// steht in allen drei Faellen dieselbe DB-Spalte <c>Bezeichner</c>
        /// (<c>ProzesswaermeStammCtrl.MapRow</c>). Die Reihenfolge ist die der
        /// <c>ReadAll</c>: <c>ORDER BY Bezeichner</c>.</para>
        /// </summary>
        internal static IReadOnlyList<string> Bezeichner(BedarfsArt art)
        {
            var liste = new List<string>();

            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    {
                        var ctrl = new StromverbraucherStammCtrl();
                        ctrl.ReadAll();
                        for (int i = 0; i < ctrl.rows; i++) liste.Add(ctrl.items[i].m_szBezeichner ?? "");
                        return liste;
                    }
                case BedarfsArt.Prozesswaerme:
                    {
                        var ctrl = new ProzesswaermeStammCtrl();
                        ctrl.ReadAll();
                        for (int i = 0; i < ctrl.rows; i++) liste.Add(ctrl.items[i].m_szProzessname ?? "");
                        return liste;
                    }
                default:
                    {
                        var ctrl = new BrauchwasserStammCtrl();
                        ctrl.ReadAll();
                        for (int i = 0; i < ctrl.rows; i++) liste.Add(ctrl.items[i].m_szBezeichner ?? "");
                        return liste;
                    }
            }
        }

        /// <summary>
        /// Beschreibung und Typ eines Kopfsatzes — <c>SetProzessInfo</c> der drei Masken
        /// (iU9-W14b.0a). <c>null</c>, wenn es den Satz nicht gibt: Der Vorlaeufer prueft
        /// <c>rows &gt; 0</c> und liess die drei Anzeigefelder dann UNVERAENDERT stehen.
        /// </summary>
        internal static (string Beschreibung, string Typ)? Kopf(BedarfsArt art, string bezeichner)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    {
                        var ctrl = new StromverbraucherStammCtrl();
                        ctrl.ReadSingle(bezeichner);
                        if (ctrl.rows == 0) return null;
                        return (ctrl.m_szBeschreibung ?? "", ctrl.m_szTyp ?? "");
                    }
                case BedarfsArt.Prozesswaerme:
                    {
                        var ctrl = new ProzesswaermeStammCtrl();
                        ctrl.ReadSingle(bezeichner);
                        if (ctrl.rows == 0) return null;
                        return (ctrl.m_szBeschreibung ?? "", ctrl.m_szTyp ?? "");
                    }
                default:
                    {
                        var ctrl = new BrauchwasserStammCtrl();
                        ctrl.ReadSingle(bezeichner);
                        if (ctrl.rows == 0) return null;
                        return (ctrl.m_szBeschreibung ?? "", ctrl.m_szTyp ?? "");
                    }
            }
        }

        /// <summary>
        /// Loescht einen Kopfsatz (iU9-W14b.0a).
        ///
        /// <para><b>Die ReadOnly-Sperre prueft diese Methode VORHER</b>, genau wie
        /// <see cref="SaveHead"/> es vom Aufrufer verlangt: Die drei <c>Delete</c> der
        /// Stammcontroller melden sie ueber <c>Meldung.Hinweis</c>, und das waere in einer
        /// WebView ein modaler Kasten ueber dem Dialog statt eines Warnbanners darin.</para>
        /// </summary>
        internal static BedarfLoeschErgebnis Loeschen(BedarfsArt art, string bezeichner)
        {
            if (string.IsNullOrEmpty(bezeichner)) return BedarfLoeschErgebnis.Fehlgeschlagen;
            if (IstReadOnly(art, bezeichner)) return BedarfLoeschErgebnis.Schreibgeschuetzt;

            bool weg;
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: weg = new StromverbraucherStammCtrl().Delete(bezeichner); break;
                case BedarfsArt.Prozesswaerme:    weg = new ProzesswaermeStammCtrl().Delete(bezeichner); break;
                default:                          weg = new BrauchwasserStammCtrl().Delete(bezeichner); break;
            }
            return weg ? BedarfLoeschErgebnis.Geloescht : BedarfLoeschErgebnis.Fehlgeschlagen;
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
