using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E30/2 (#544, Befund B4 der Sichtprüfung 1030; Anwenderentscheid 26.09.2026
    /// „Wenn Hilfsenergie angegeben ist, müssen die Hilfsenergiekosten daraus ermittelt
    /// werden", Fragen E30‑Q1 a bis Q4 a) — <b>die Hilfsenergiekosten aus dem Anteil an der
    /// Anlage</b>.
    ///
    /// <para><b>Der Befund.</b> Der Hilfsenergieanteil einer Anlage
    /// (<c>Tab_Energieanlagen.Hilfsenergie_Anteil</c>, % des Endenergiebedarfs, gepflegt im
    /// BHKW-Wirtschaftlichkeitsdialog) ergab bis hierher eine Menge
    /// (<see cref="HilfsstromRechner.MengeMWh"/>), die allein den KWK-Zuschlag minderte.
    /// Gekostet hat sie nichts: Die Simulation führt diesen Strom nicht (die Strommatrix
    /// bleibt brutto), und die Hilfsenergie-Kostenposition rechnete nur mit einem eigenen
    /// Satz.</para>
    ///
    /// <para><b>Der Rechenweg (E30‑Q1 a).</b> Trägt eine BHKW- oder Brennstoffkessel-Anlage
    /// einen Anteil &gt; 0, rechnet ihre Hilfsenergie-Kostenposition nach Weg B
    /// („% des Endenergiebedarfs") mit dem Anteil als Satz:
    /// <c>Brennstoff der Anlage [kWh] × Arbeitspreis des Projekt-Stromträgers × Anteil / 100</c>
    /// — dieselbe Menge wie der KWKG-Abzug, bewertet wie jede Weg-B-Position
    /// (Szenariomengen und -preise über den Endenergie-Auflöser, Endenergie-Topf p_E). Die
    /// gespeicherte Bemessung der Zeile spielt dabei keine Rolle — ein Anteil ist eine
    /// Menge, kein Anteil der Brennstoffkosten (Weg A wäre um das Preisverhältnis falsch).
    /// Führt die Anlage keine Hilfsenergie-Kostenposition, entsteht eine abgeleitete
    /// Zeile.</para>
    ///
    /// <para><b>Vorrang (E30‑Q2 a).</b> Trägt eine Hilfsenergie-Kostenposition derselben
    /// Anlage selbst einen Satz oder Betrag (dieselbe Lesart „aktiv" wie
    /// <c>KohaerenzPruefung.HilfsenergieDoppelpflege</c>), gilt die Position; der Anteil
    /// mindert dann nur den KWK-Zuschlag und wird nicht zusätzlich bepreist. So wird
    /// derselbe Strom nie zweimal gebucht.</para>
    ///
    /// <para><b>Nicht erfasst (E30‑Q3 a):</b> Elektrokessel und Wärmepumpen — ihre Endenergie
    /// ist Netzstrom und steht schon in den Energiekosten; ebenso der Kälte-Hilfsstrom
    /// (<c>Kuehl_Hilfsstromanteil</c>), den die Simulation im Netzbezug führt. Emissionen und
    /// Stromsteuer des Hilfsstroms bleiben unberührt (E30‑Q5).</para>
    ///
    /// <para><b>Ohne Anteil bitgleich.</b> Trägt keine Anlage des Projekts einen Anteil, ist
    /// der Plan leer; die Leseschleifen rechnen dann Zeile für Zeile wie vorher.</para>
    /// </summary>
    internal static class HilfsenergieAusAnteil
    {
        /// <summary>Herkunft des Satzes einer Position, deren Satz aus dem Anteil der Anlage
        /// stammt (<see cref="KostenPositionNachweis.SatzHerkunft"/>).</summary>
        internal const string HERKUNFT_ANLAGENANTEIL = "ANLAGENANTEIL";

        /// <summary>Eine Anlage mit gepflegtem Anteil.</summary>
        internal sealed class Anlage
        {
            /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
            public int IdAnlage;

            /// <summary>Kostenkomponente der Anlage (<c>BetriebskostenCtrl.KOMPONENTE_*</c>) —
            /// sie wählt im Auflöser die Endenergie (BHKW-Brennstoff, Kesselbrennstoff).</summary>
            public int Komponente;

            /// <summary>Der Anteil [% des Endenergiebedarfs], &gt; 0.</summary>
            public double AnteilProzent;
        }

        /// <summary>Wer die Hilfsenergiekosten eines Projekts aus dem Anteil trägt.</summary>
        internal sealed class Plan
        {
            /// <summary><c>Tab_ProjektWerte.ID</c> der Position, die den Anteil als Satz
            /// trägt → die Anlage.</summary>
            public readonly Dictionary<int, Anlage> JeZeile = new Dictionary<int, Anlage>();

            /// <summary>Anlagen mit Anteil, aber ohne Hilfsenergie-Kostenposition — sie
            /// bekommen eine abgeleitete Zeile.</summary>
            public readonly List<Anlage> OhneZeile = new List<Anlage>();

            /// <summary><c>true</c> = nichts abzuleiten; die Schleifen rechnen wie vorher.</summary>
            public bool Leer
            {
                get { return JeZeile.Count == 0 && OhneZeile.Count == 0; }
            }
        }

        /// <summary>
        /// Plant die Hilfsenergiekosten eines Projekts: welche Position den Anteil ihrer Anlage
        /// als Satz trägt und welche Anlage eine abgeleitete Zeile braucht.
        ///
        /// <para>Die billigste Frage zuerst: Trägt keine BHKW-/Kesselanlage einen Anteil
        /// (der ganze Bestand der Testdatenbank), endet die Planung nach EINER Abfrage mit
        /// einem leeren Plan. Eine Datenbank ohne die Spalte (vor Schritt 61) liefert über
        /// <see cref="DataRepository"/> eine leere Tabelle — ebenfalls ein leerer Plan.</para>
        /// </summary>
        internal static Plan Plane(int idProjekt)
        {
            var plan = new Plan();
            if (idProjekt <= 0) return plan;

            List<Anlage> anlagen = AnlagenMitAnteil(idProjekt);
            if (anlagen.Count == 0) return plan;

            // Die Hilfsenergie-Kostenpositionen der Kategorie 2 je Anlage, in Lesereihenfolge.
            // Der Namensvergleich läuft in C# (dieselbe Begründung wie in
            // KohaerenzPruefung.AnlagenMitHilfsenergiePosition: kein LIKE-Dialekt).
            DataTable dt = DataRepository.GetDataTable(
                "SELECT w.ID, f.Bezeichnung, w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "], " +
                "w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], w.EingegebenerWert " +
                "FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                "ON w.StammID = f.StammID " +
                "WHERE w.ProjektID = ? AND w.KategorieID = 2",
                new DbParam("@p", idProjekt));

            var aktiv = new HashSet<int>();
            var erste = new Dictionary<int, int>();   // Anlage → erste nicht aktive Zeile
            if (dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_PW_ID_ANLAGE))
                foreach (DataRow r in dt.Rows)
                {
                    string name = r["Bezeichnung"] == DBNull.Value
                                ? "" : Convert.ToString(r["Bezeichnung"]).Trim();
                    if (!IstHilfsenergiePosition(name)) continue;

                    object ida = r[SchemaKatalog.SPALTE_PW_ID_ANLAGE];
                    if (ida == DBNull.Value) continue;
                    int idAnlage = Convert.ToInt32(ida, CultureInfo.InvariantCulture);
                    if (idAnlage <= 0) continue;

                    if (Gepflegt(r[SchemaKatalog.SPALTE_PW_EINHEITPREIS]) ||
                        Gepflegt(r["EingegebenerWert"]))
                    {
                        aktiv.Add(idAnlage);
                        continue;
                    }
                    int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                    if (!erste.ContainsKey(idAnlage)) erste[idAnlage] = id;
                }

            foreach (Anlage a in anlagen)
            {
                if (aktiv.Contains(a.IdAnlage)) continue;          // E30‑Q2 a: die Position gilt
                int zeile;
                if (erste.TryGetValue(a.IdAnlage, out zeile)) plan.JeZeile[zeile] = a;
                else plan.OhneZeile.Add(a);
            }
            return plan;
        }

        /// <summary>Eine Hilfsenergie-Kostenposition — „Hilfsenergiekosten" samt den
        /// Spielarten „… (Strom)", „… (Pumpen)" usw. (<see cref="DbWerte.VDI_POS_HILFSENERGIE"/>).</summary>
        internal static bool IstHilfsenergiePosition(string bezeichnung)
        {
            return !string.IsNullOrEmpty(bezeichnung) &&
                   bezeichnung.StartsWith(DbWerte.VDI_POS_HILFSENERGIE, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>„Gepflegt" heißt: weder NULL noch 0 — die Lesart der Kohärenzprüfung
        /// (eine Vorlagenzeile mit 0 ist vorbereitet, nicht gepflegt).</summary>
        private static bool Gepflegt(object o)
        {
            if (o == null || o == DBNull.Value) return false;
            try { return Math.Abs(Convert.ToDouble(o, CultureInfo.InvariantCulture)) > 1e-12; }
            catch (FormatException) { return false; }
            catch (InvalidCastException) { return false; }
        }

        /// <summary>BHKW- und Brennstoffkessel-Anlagen des Projekts mit einem Anteil &gt; 0
        /// (E30‑Q3 a; Elektrokessel ausgenommen).</summary>
        private static List<Anlage> AnlagenMitAnteil(int idProjekt)
        {
            var liste = new List<Anlage>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Type, [" + SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "] " +
                "FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND [" +
                SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "] > 0 AND ID_Type IN (" +
                WizardItemClass.BHKW_TYP.ToString(CultureInfo.InvariantCulture) + ", " +
                WizardItemClass.KESSEL_TYP.ToString(CultureInfo.InvariantCulture) + ")",
                new DbParam("@p", idProjekt));
            if (dt == null || !dt.Columns.Contains(SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL))
                return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                int typ = Convert.ToInt32(r["ID_Type"], CultureInfo.InvariantCulture);
                double anteil = Convert.ToDouble(r[SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL],
                                                 CultureInfo.InvariantCulture);
                if (!(anteil > 0)) continue;
                if (typ == WizardItemClass.KESSEL_TYP && WirtschaftlichkeitCtrl.IstElektrokesselAnlage(id))
                    continue;
                liste.Add(new Anlage
                {
                    IdAnlage = id,
                    Komponente = typ == WizardItemClass.BHKW_TYP
                               ? BetriebskostenCtrl.KOMPONENTE_BHKW
                               : BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL,
                    AnteilProzent = anteil
                });
            }
            return liste;
        }

        /// <summary>Der Name der abgeleiteten Zeile (Anzeige; beide Sprachen).</summary>
        internal static string NameAbgeleiteteZeile()
        {
            return Text("HILFS_ANTEIL_POSITION", "Hilfsenergiekosten (aus dem Anlagenanteil)");
        }

        /// <summary>Der Vermerk der Herleitung, wenn der Satz aus dem Anteil stammt.</summary>
        internal static string HerkunftKurz()
        {
            return Text("HILFS_ANTEIL_HERKUNFT", "Satz aus dem Hilfsenergieanteil der Anlage");
        }

        private static string Text(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch (System.Resources.MissingManifestResourceException) { return rueckfall; }
        }
    }
}
