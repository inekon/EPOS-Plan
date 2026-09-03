using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die KWKG-Angaben <b>einer</b> BHKW-Anlage, wie der Dialog sie zeigt und speichert
    /// (Etappe E6). Jedes Feld ist NULL-fähig; <c>null</c> heißt durchgehend „kein
    /// eigener Wert — es gilt der Projektwert".
    /// </summary>
    public class KwkgAnlagenAngabe
    {
        /// <summary><c>Tab_Energieanlagen.ID</c> — die Zeile, in die gespeichert wird.</summary>
        public int IdAnlage;

        /// <summary>Projekt der Anlage (Stamm oder Variante der Vergleichsgruppe).</summary>
        public int IdProjekt;

        /// <summary>Anzeigename des Projekts (Stamm- bzw. Variantenname).</summary>
        public string Projektname = "";

        /// <summary>Bezeichner der Anlagenzeile — zugleich der Schlüssel, über den die
        /// Rechnung sie ihrer Ergebnis-Modulzeile zuordnet.</summary>
        public string Bezeichner = "";

        /// <summary>Elektrische Nennleistung [kW] aus der Gerätezeile <c>Tab_BHKW.Pel</c>.</summary>
        public double PelKW;

        /// <summary>Bestell-/Genehmigungsdatum dieser Anlage (§ 6 KWKG 2025).</summary>
        public DateTime? Stichtag;

        /// <summary>Inbetriebnahmedatum dieser Anlage.</summary>
        public DateTime? Inbetriebnahme;

        /// <summary>Anlagenart, Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c> (leer = nicht erfasst).</summary>
        public string Anlagenart = "";

        /// <summary>Tatbestand des § 6 Abs. 3, Steuerwert <c>DbWerte.KWKG_EIGENFALL_*</c>.</summary>
        public string Eigenfall = "";

        /// <summary>Überschreibwert des Einspeisesatzes [ct/kWh].</summary>
        public double? SatzEinspCt;

        /// <summary>Überschreibwert des Eigenstromsatzes [ct/kWh].</summary>
        public double? SatzEigenCt;

        /// <summary>Vbh-Kontingent dieser Anlage [h].</summary>
        public double? VbhKontingent;

        /// <summary>Jahresdeckel-Override dieser Anlage [h/a].</summary>
        public double? VbhDeckel;
    }

    /// <summary>
    /// Lese- und Schreibweg der KWKG-Angaben <b>je BHKW-Anlage</b>
    /// (<c>Tab_Energieanlagen</c>, Migrationsschritt 22, Etappe E6).
    ///
    /// <para><b>Warum ein eigener Controller.</b> <c>WirtschaftlichkeitCtrl</c> liest
    /// dieselben acht Spalten für die RECHNUNG — dort zusammen mit der Brennstoff- und
    /// Trägerauflösung, gecacht je Berechne-Lauf und über eine private Klasse. Der Dialog
    /// braucht dagegen einen Schreibweg, die Projektzugehörigkeit und keine Caches. Beide
    /// Wege teilen sich die Spaltennamen aus <see cref="SchemaKatalog"/>; eine zweite
    /// Wahrheit über die Spalten gibt es damit nicht.</para>
    ///
    /// <para><b>Der Dialog schreibt nur diese acht Spalten.</b> Ein <c>UPDATE</c> je
    /// Anlagenzeile mit namentlich aufgezählten Feldern — nie <c>SELECT *</c> und
    /// Rückschreiben, weil <c>Tab_Energieanlagen</c> 65 Spalten des Rechenkerns führt, die
    /// dieser Dialog nicht kennt und nicht anfassen darf.</para>
    /// </summary>
    public class KwkgAnlagenCtrl
    {
        /// <summary>
        /// Alle BHKW-Anlagen der Vergleichsgruppe (Stammprojekt und seine Varianten) mit
        /// ihren KWKG-Angaben, in Projekt- und Lesereihenfolge.
        /// </summary>
        public List<KwkgAnlagenAngabe> LadeGruppe(int idStamm, string stammName)
        {
            var liste = new List<KwkgAnlagenAngabe>();
            if (idStamm <= 0) return liste;

            // Die Tabellen und Spalten sicherstellen — dieselbe tolerante Vorsorge, die
            // auch der Rechenweg fährt (StelleTabellenSicher legt die acht Spalten an,
            // falls die Migration nie lief).
            new WirtschaftlichkeitCtrl().StelleTabellenSicher();

            if (string.IsNullOrEmpty(stammName)) stammName = Projektname(idStamm);

            var varianten = new VariantenCtrl();
            varianten.StelleVariantentabelleSicher();
            foreach (VariantenCtrl.VarianteInfo vi in varianten.LadeGruppe(idStamm, stammName))
                Lade(vi.IdProjekt, string.IsNullOrEmpty(vi.Variantenname)
                                   ? vi.Projektname : vi.Variantenname, liste);
            return liste;
        }

        /// <summary>Die BHKW-Anlagen EINES Projekts anhängen.</summary>
        private static void Lade(int idProjekt, string projektname, List<KwkgAnlagenAngabe> liste)
        {
            try
            {
                DataTable dt;
                using (DataRepository.EngineModus())
                    dt = DataRepository.GetDataTable(
                        "SELECT a.ID, a.Bezeichner, b.Pel" + Spaltenliste("a") + " " +
                        "FROM Tab_Energieanlagen AS a " +
                        "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID " +
                        "WHERE a.ID_Projekt = ? AND a.ID_Type = " + WizardItemClass.BHKW_TYP,
                        new DbParam("@p", idProjekt));
                if (dt == null || !dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG)) return;

                foreach (DataRow r in dt.Rows)
                {
                    var g = new KwkgAnlagenAngabe
                    {
                        IdAnlage = Ganzzahl(r, "ID"),
                        IdProjekt = idProjekt,
                        Projektname = projektname ?? "",
                        Bezeichner = Text(r, "Bezeichner"),
                        PelKW = D(r, "Pel") ?? 0,
                        Stichtag = Datum(r, SchemaKatalog.SPALTE_EA_KWKG_STICHTAG),
                        Inbetriebnahme = Datum(r, SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME),
                        Anlagenart = Text(r, SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART),
                        Eigenfall = Text(r, SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL),
                        SatzEinspCt = D(r, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP),
                        SatzEigenCt = D(r, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN),
                        VbhKontingent = D(r, SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT),
                        VbhDeckel = D(r, SchemaKatalog.SPALTE_EA_KWKG_DECKEL)
                    };
                    liste.Add(g);
                }
            }
            catch { }
        }

        /// <summary>
        /// Speichert die acht Angaben einer Anlagenzeile. Liefert false, wenn das UPDATE
        /// scheitert oder keine Zeile trifft.
        /// </summary>
        public bool Speichere(KwkgAnlagenAngabe g)
        {
            if (g == null || g.IdAnlage <= 0) return false;
            try
            {
                return DataRepository.ExecuteSQL(
                    "UPDATE Tab_Energieanlagen SET " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_STICHTAG + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_EA_KWKG_DECKEL + "] = ? " +
                    "WHERE ID = ?",
                    Datumswert(g.Stichtag),
                    Datumswert(g.Inbetriebnahme),
                    Textwert(g.Anlagenart, 24),
                    Textwert(g.Eigenfall, 24),
                    Zahlwert(g.SatzEinspCt),
                    Zahlwert(g.SatzEigenCt),
                    Zahlwert(g.VbhKontingent),
                    Zahlwert(g.VbhDeckel),
                    new DbParam("@id", DbParamTyp.Integer) { Wert = g.IdAnlage });
            }
            catch { return false; }
        }

        // ------------------------------------------------------------- Hilfsmittel

        /// <summary>Projektname eines Projekts; leer, wenn er sich nicht lesen lässt.</summary>
        private static string Projektname(int idProjekt)
        {
            try
            {
                using (DataRepository.EngineModus())
                {
                    object o = DataRepository.ExecuteScalar(
                        "SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                        new DbParam("@p", idProjekt));
                    if (o != null && o != DBNull.Value) return Convert.ToString(o).Trim();
                }
            }
            catch { }
            return "";
        }

        /// <summary>Die acht E6-Spalten als SELECT-Anhang, mit Tabellenpräfix.</summary>
        private static string Spaltenliste(string praefix)
        {
            string s = "";
            foreach (SchemaSpalte sp in SchemaKatalog.Schritt22_KwkgJeAnlage)
                s += ", " + praefix + ".[" + sp.Name + "]";
            return s;
        }

        /// <summary>NULL statt 0 — „kein eigener Wert" ist etwas anderes als der Wert 0.</summary>
        private static DbParam Zahlwert(double? v)
        {
            return new DbParam("@d", DbParamTyp.Double)
            { Wert = v.HasValue ? (object)v.Value : DBNull.Value };
        }

        private static DbParam Datumswert(DateTime? v)
        {
            return new DbParam("@t", DbParamTyp.Date)
            { Wert = v.HasValue ? (object)v.Value.Date : DBNull.Value };
        }

        /// <summary>Steuerwert gekürzt auf die Spaltenbreite — ein zu langer Wert ließe das
        /// UPDATE STILL scheitern (die Lehre aus Etappe E3, Probe C2).</summary>
        private static DbParam Textwert(string s, int laenge)
        {
            object wert = DBNull.Value;
            if (!string.IsNullOrEmpty(s))
            {
                string t = s.Trim();
                if (t.Length > 0) wert = t.Length > laenge ? t.Substring(0, laenge) : t;
            }
            return new DbParam("@s", DbParamTyp.VarWChar, laenge) { Wert = wert };
        }

        private static int Ganzzahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        private static string Text(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            try { return Convert.ToString(r[spalte]).Trim(); } catch { return ""; }
        }

        private static DateTime? Datum(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDateTime(r[spalte]); } catch { return null; }
        }
    }
}
