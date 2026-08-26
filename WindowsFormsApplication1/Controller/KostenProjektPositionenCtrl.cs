using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// PROJEKTMODUS der Kostenverwaltung (Etappe KD6a, Konzept Kostendialoge
    /// § 3.2/§ 5 — der in KD3 auf KD6 verschobene dritte Kontext, Nutzerabnahme
    /// 26.08.2026): Lesen und Pflegen der Tab_ProjektWerte-Positionen einer
    /// Komponente und Kategorie in DERSELBEN Rasterdarstellung wie die
    /// Stammvorlagen (<see cref="ucVorlagenZeile"/>).
    ///
    /// <para><b>Keine neue Wahrheit:</b> Gelesen wird über die Bestandsspalten
    /// samt <see cref="KostenPositionCtrl.LiesZusatz"/>; geschrieben wird
    /// ausschließlich über die Bestands-Schreibwege
    /// (<see cref="KostenPositionCtrl.SetzeBetragMitZusatz"/>,
    /// <see cref="KostenVorlagenUebernahmeCtrl.StammIdSicher"/> — dieselben Wege
    /// wie Übernahme-Mechanik und alter Kosteneditor). Der ANZEIGEbetrag
    /// satzbasierter Positionen kommt aus <see cref="BetriebskostenCtrl.Betrag"/>
    /// — dem einen Rechenweg, den auch der Rechenkern nutzt.</para>
    /// </summary>
    internal static class KostenProjektPositionenCtrl
    {
        /// <summary>Eine Projektzeile in Vorlagen-Rasterform; <c>Id</c> ist
        /// <c>Tab_ProjektWerte.ID</c>, <c>BetragNetto</c> der BERECHNETE Betrag.</summary>
        internal sealed class Zeile
        {
            public KostenVorlagenPosition Raster = new KostenVorlagenPosition();
            public double Eingegeben;
            public double? Menge;
            public double Best;
            public double Worst;
            public double BestNutzung;
            public double WorstNutzung;
            public int StartJahr;
        }

        // ------------------------------------------------------------- Lesen ---

        internal static List<Zeile> Lies(int projektId, int komponentenId, int kategorieId)
        {
            var liste = new List<Zeile>();
            Dictionary<int, KostenPositionCtrl.Zusatz> zusaetze =
                KostenPositionCtrl.LiesZusatz(projektId, kategorieId);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT w.ID, w.StammID, w.EingegebenerWert, w.Nutzungsdauer, " +
                "w.BestCase, w.WorstCase, w.BestCase_Nutzungsdauer, w.WorstCase_Nutzungsdauer, " +
                "k.Bezeichnung " +
                "FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS k " +
                "ON w.StammID = k.StammID " +
                "WHERE w.ProjektID = ? AND w.KomponentenID = ? AND w.KategorieID = ? " +
                "ORDER BY w.ID",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@k", komponentenId),
                new OleDbParameter("@g", kategorieId));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                var z = new Zeile();
                z.Raster.Id = Convert.ToInt32(r["ID"]);
                z.Raster.StammId = r["StammID"] == DBNull.Value
                    ? (int?)null : Convert.ToInt32(r["StammID"]);
                z.Raster.Bezeichnung = Convert.ToString(r["Bezeichnung"]);
                z.Eingegeben = W(r, "EingegebenerWert") ?? 0;
                z.Raster.Nutzungsdauer = W(r, "Nutzungsdauer");
                z.Best = W(r, "BestCase") ?? 0;
                z.Worst = W(r, "WorstCase") ?? 0;
                z.BestNutzung = W(r, "BestCase_Nutzungsdauer") ?? 0;
                z.WorstNutzung = W(r, "WorstCase_Nutzungsdauer") ?? 0;

                KostenPositionCtrl.Zusatz zu;
                if (!zusaetze.TryGetValue(z.Raster.Id, out zu)) zu = new KostenPositionCtrl.Zusatz();
                z.Raster.Kostenart = zu.Kostenart ?? "";
                z.Raster.Bemessung = string.IsNullOrEmpty(zu.Bemessung)
                    ? DbWerte.BEMESSUNG_BETRAG : zu.Bemessung;
                z.Raster.IstErloes = zu.IstErloes;
                z.Menge = zu.Menge;
                z.StartJahr = zu.StartJahr;

                // Feldsemantik der Materialisierung (KD3): absolut → der Satz IST
                // der eingegebene Wert; satzbasiert → Satz = Einheitpreis.
                BemessungKatalog.Info info = BemessungKatalog.Finde(z.Raster.Bemessung);
                bool absolut = info == null || info.Absolut;
                z.Raster.Satz = absolut ? (double?)z.Eingegeben : zu.Einheitpreis;

                // Anzeigebetrag aus dem EINEN Rechenweg (auch für Kategorie 1 —
                // die Bemessungsarten sind dieselben 15 des Katalogs).
                double betrag;
                try { betrag = BetriebskostenCtrl.Betrag(z.Eingegeben, zu); }
                catch { betrag = z.Eingegeben; }
                z.Raster.BetragNetto = betrag;

                liste.Add(z);
            }
            return liste;
        }

        // ---------------------------------------------------------- Schreiben ---

        /// <summary>Felder einer bestehenden Zeile sichern (Bezeichnung über das
        /// Positionslexikon, Bemessung/Satz über den Zusatz-Schreibweg, ND direkt).</summary>
        internal static bool Speichern(Zeile z)
        {
            if (z == null || z.Raster.Id <= 0) return false;

            // Bezeichnung = Lexikonverweis: Umbenennen heißt StammID wechseln.
            int stammId = KostenVorlagenUebernahmeCtrl.StammIdSicher(z.Raster.Bezeichnung);
            if (stammId > 0 && (!z.Raster.StammId.HasValue || z.Raster.StammId.Value != stammId))
            {
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET StammID = ? WHERE ID = ?",
                    new OleDbParameter("@s", stammId),
                    new OleDbParameter("@id", z.Raster.Id));
                z.Raster.StammId = stammId;
            }

            BemessungKatalog.Info info = BemessungKatalog.Finde(z.Raster.Bemessung);
            bool absolut = info == null || info.Absolut;
            double eingegeben = absolut ? (z.Raster.Satz ?? 0) : 0.0;

            var zusatz = new KostenPositionCtrl.Zusatz
            {
                Kostenart = z.Raster.Kostenart ?? "",
                Bemessung = z.Raster.Bemessung,
                IstErloes = z.Raster.IstErloes,
                Menge = z.Menge,
                Einheitpreis = absolut ? (double?)null : z.Raster.Satz,
                StartJahr = z.StartJahr
            };
            if (!KostenPositionCtrl.SetzeBetragMitZusatz(z.Raster.Id, eingegeben, zusatz))
                return false;
            z.Eingegeben = eingegeben;

            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Nutzungsdauer = ? WHERE ID = ?",
                Zahl("@n", z.Raster.Nutzungsdauer),
                new OleDbParameter("@id", z.Raster.Id));

            KostenPositionCtrl.Zusatz zu2 = zusatz;
            double betrag;
            try { betrag = BetriebskostenCtrl.Betrag(eingegeben, zu2); }
            catch { betrag = eingegeben; }
            z.Raster.BetragNetto = betrag;
            return true;
        }

        /// <summary>Neue Projektposition (Muster der Übernahme-Mechanik, § 8).</summary>
        internal static int Neu(int projektId, int komponentenId, int kategorieId,
                                string name, string kostenart, string bemessung)
        {
            int stammId = KostenVorlagenUebernahmeCtrl.StammIdSicher(name);
            if (stammId <= 0) return 0;

            string gruppe = kategorieId == Form_Kosten.KATEGORIE_BETRIEB
                ? DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI
                : DbWerte.KOSTEN_GRUPPE_ALLGEMEIN;
            int id = KostenPositionCtrl.SetzeBetrag(projektId, kategorieId,
                komponentenId, stammId, 0.0, gruppe, true);
            if (id <= 0) return 0;

            KostenPositionCtrl.SetzeBetragMitZusatz(id, 0.0, new KostenPositionCtrl.Zusatz
            {
                Kostenart = kostenart ?? "",
                Bemessung = string.IsNullOrEmpty(bemessung) ? DbWerte.BEMESSUNG_BETRAG : bemessung,
                IstErloes = false,
                Menge = null,
                Einheitpreis = null
            });
            return id;
        }

        internal static bool Loeschen(int id)
        {
            return id > 0 && DataRepository.ExecuteSQL(
                "DELETE FROM Tab_ProjektWerte WHERE ID = ?",
                new OleDbParameter("@id", id));
        }

        /// <summary>Worst/Best (Betrag + Nutzungsdauer) und Startjahr sichern
        /// (FK10; NULL bei Startjahr ≤ 1, dieselbe Regel wie der Kosteneditor).</summary>
        internal static bool CaseSichern(Zeile z)
        {
            if (z == null || z.Raster.Id <= 0) return false;
            bool ok = DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET BestCase = ?, WorstCase = ?, " +
                "BestCase_Nutzungsdauer = ?, WorstCase_Nutzungsdauer = ? WHERE ID = ?",
                new OleDbParameter("@b", z.Best),
                new OleDbParameter("@w", z.Worst),
                new OleDbParameter("@bn", z.BestNutzung),
                new OleDbParameter("@wn", z.WorstNutzung),
                new OleDbParameter("@id", z.Raster.Id));
            if (!ok) return false;

            if (!KostenPositionCtrl.StelleSpaltenSicher()) return true;
            return DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_STARTJAHR +
                "] = ? WHERE ID = ?",
                z.StartJahr > 1
                    ? new OleDbParameter("@j", z.StartJahr)
                    : new OleDbParameter("@j", DBNull.Value),
                new OleDbParameter("@id", z.Raster.Id));
        }

        /// <summary>Kategoriesumme des Projekts über die BERECHNETEN Beträge —
        /// Erlöse negativ (L7), dieselbe Konvention wie der Summenfuß.</summary>
        internal static double Summe(List<Zeile> zeilen)
        {
            double netto = 0;
            if (zeilen != null)
                foreach (Zeile z in zeilen)
                    if (z.Raster.BetragNetto.HasValue)
                        netto += z.Raster.IstErloes
                            ? -z.Raster.BetragNetto.Value : z.Raster.BetragNetto.Value;
            return netto;
        }

        // -------------------------------------------------------------- Helfer ---

        private static double? W(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        private static OleDbParameter Zahl(string name, double? wert)
        {
            var p = new OleDbParameter(name, OleDbType.Double);
            p.Value = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }
    }
}
