using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE H2 — der Bezugsgrößen-Auflöser der Endenergie-Bemessungen
    /// (<c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md</c> § 4.5, Festlegung
    /// 29.08.2026; Anschluss an Etappe H1, offener Punkt H1-1).
    ///
    /// <para><b>Was er liefert.</b> Für eine Betriebskostenposition mit
    /// <c>BEMESSUNG_PROZENT_ENDENERGIEKOSTEN</c> (Weg A) oder
    /// <c>BEMESSUNG_PROZENT_ENDENERGIEBEDARF</c> (Weg B) die Endenergie der
    /// betrachteten Anlage aus dem <b>jüngsten Simulationslauf</b>: den
    /// Jahresbedarf [kWh/a] und — soweit Preise gepflegt sind — die
    /// Arbeitskosten [€/a]. Die Menge ist ein ERGEBNISWERT, kein Eingabewert:
    /// Sie wird bei jedem Lesen frisch geholt, die Spalte
    /// <c>Tab_ProjektWerte.Menge</c> bleibt Ausweisgröße („Stand des Laufs").</para>
    ///
    /// <para><b>Endenergie je Komponente</b> (Tabelle § 4.5):
    /// BHKW und Heizkessel = Brennstoff (<c>Verbrauch</c> je Modul × Arbeitspreis
    /// seines <c>CarrierId</c>), Wärmepumpe = Strom (<c>Stromverbrauch</c> +
    /// <c>Heizstab</c> × Strombezugspreis). Solarthermie, Pufferspeicher,
    /// Stromspeicher und Photovoltaik haben KEINE Endenergie — dort ist nur der
    /// feste Jahresbetrag zulässig, der Auflöser liefert null.</para>
    ///
    /// <para><b>Anlagenscharf mit Komponentensumme als Rückfall.</b> Trägt die
    /// Position eine <c>ID_Anlage</c> (Schritt 45), zählen nur die Modulzeilen
    /// dieser Anlage — die Zuordnung läuft über den Bezeichner, denn die
    /// Ergebnis-Modulzeilen führen keinen Anlagenschlüssel
    /// (<c>ErgebnisBHKWModulModel.Modul</c> = <c>Tab_Energieanlagen.Bezeichner</c>,
    /// dasselbe Verfahren wie <c>ErdreichAuswertung</c>). Ist die Anlage im Lauf
    /// nicht vertreten, gibt es bewusst KEINE Bezugsgröße statt der Summe einer
    /// anderen Anlage. Ohne <c>ID_Anlage</c> gilt die Summe aller Module der
    /// Komponente.</para>
    ///
    /// <para><b>Preise aus der EINEN Wahrheit.</b> Arbeitspreis je Träger und
    /// Stromträger kommen aus <see cref="KostenEmissionRechner"/>
    /// (<c>ArbeitspreisJeKwh</c>/<c>StromTraegerId</c>) — keine zweite
    /// Preisverrechnung. Fehlt der Preis eines beteiligten Trägers, bleiben die
    /// KOSTEN null (Weg A ohne Basis), der BEDARF in kWh bleibt bestimmbar.</para>
    /// </summary>
    internal sealed class EndenergieAufloeser
    {
        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> der Wärmepumpe — dieselbe feste Nummer wie
        /// in <c>Form_Kosten.GetKomponentenID</c> („Wärmepumpe" = 1; Begründung für
        /// die festen Nummern 1…7 dort). BHKW und Heizkessel stehen bereits benannt
        /// in <see cref="BetriebskostenCtrl"/>.
        /// </summary>
        internal const int KOMPONENTE_WAERMEPUMPE = 1;

        /// <summary>Endenergie einer Position — das Ergebnis des Auflösers.</summary>
        internal sealed class Groesse
        {
            /// <summary>Jahresbedarf [kWh/a] — Basis von Weg B; immer &gt; 0.</summary>
            public double BedarfKwh;

            /// <summary>Arbeitskosten [€/a] — Basis von Weg A; null, wenn ein
            /// beteiligter Träger keinen Arbeitspreis führt.</summary>
            public double? KostenEuro;

            /// <summary>Klartext der Basis für Herleitungen, z. B.
            /// „BHKW ‚Modul 1'" oder „alle Heizkessel-Module".</summary>
            public string Basis = "";
        }

        private readonly int _idProjekt;
        private readonly ErgebnisModel _ergebnis;
        private readonly Dictionary<int, string> _anlagenName = new Dictionary<int, string>();
        private readonly Dictionary<int, double?> _preisJeTraeger = new Dictionary<int, double?>();
        private double? _strompreis;
        private bool _strompreisErmittelt;

        private EndenergieAufloeser(int idProjekt, ErgebnisModel ergebnis)
        {
            _idProjekt = idProjekt;
            _ergebnis = ergebnis;
        }

        /// <summary>
        /// Baut den Auflöser für ein Projekt; null, wenn es keinen Simulationslauf
        /// gibt — dann hat keine Endenergie-Position eine Bezugsgröße (Festlegung:
        /// „ohne Lauf keine Menge und damit kein Betrag").
        /// </summary>
        internal static EndenergieAufloeser FuerProjekt(int idProjekt)
        {
            try
            {
                ErgebnisModel erg = new ErgebnisCtrl().Load(idProjekt);
                if (erg == null) return null;

                var a = new EndenergieAufloeser(idProjekt, erg);
                a.AnlagenNamenLaden();
                return a;
            }
            catch { return null; }
        }

        /// <summary>Strombezugspreis [€/kWh] des Projekts (Arbeitspreis des
        /// Stromträgers); null = nicht gepflegt. Bewertet Weg B und die
        /// Wärmepumpen-Endenergie.</summary>
        internal double? StrompreisJeKwh
        {
            get
            {
                if (!_strompreisErmittelt)
                {
                    _strompreisErmittelt = true;
                    int carrier = KostenEmissionRechner.StromTraegerId(_idProjekt);
                    _strompreis = carrier > 0
                        ? KostenEmissionRechner.ArbeitspreisJeKwh(_idProjekt, carrier)
                        : null;
                }
                return _strompreis;
            }
        }

        /// <summary>
        /// Endenergie der Position — anlagenscharf, sofern <paramref name="idAnlage"/>
        /// gesetzt ist, sonst Komponentensumme. null = keine Bezugsgröße (Komponente
        /// ohne Endenergie, Anlage nicht im Lauf, oder Bedarf 0).
        /// </summary>
        internal Groesse FuerPosition(int komponentenID, int idAnlage)
        {
            string anlagenName = null;
            if (idAnlage > 0 && !_anlagenName.TryGetValue(idAnlage, out anlagenName))
                return null;   // Anlage unbekannt — keine fremde Summe unterstellen

            switch (komponentenID)
            {
                case BetriebskostenCtrl.KOMPONENTE_BHKW:
                    // Die Basis-Texte sind Herleitungsprosa; ihre Lokalisierung kommt
                    // mit der Herleitungstafel der Etappe B6 (bis dahin liest sie kein
                    // Anzeigepfad).
                    return Brennstoffsumme(BhkwModule(), anlagenName, "BHKW");
                case BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL:
                    return Brennstoffsumme(KesselModule(), anlagenName, "Heizkessel");
                case KOMPONENTE_WAERMEPUMPE:
                    return Waermepumpensumme(anlagenName);
                default:
                    return null;   // § 4.5: keine Endenergie — nur fester Jahresbetrag
            }
        }

        // ------------------------------------------------------------------ intern

        private void AnlagenNamenLaden()
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", _idProjekt));
                if (dt == null) return;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["ID"] == DBNull.Value) continue;
                    _anlagenName[Convert.ToInt32(r["ID"])] =
                        r["Bezeichner"] == DBNull.Value ? "" : Convert.ToString(r["Bezeichner"]);
                }
            }
            catch { }
        }

        private double? Preis(int carrierId)
        {
            double? p;
            if (_preisJeTraeger.TryGetValue(carrierId, out p)) return p;
            p = KostenEmissionRechner.ArbeitspreisJeKwh(_idProjekt, carrierId);
            _preisJeTraeger[carrierId] = p;
            return p;
        }

        private sealed class Brennstoffzeile
        {
            public string Modul;
            public double VerbrauchMWh;
            public int CarrierId;
        }

        private List<Brennstoffzeile> BhkwModule()
        {
            var liste = new List<Brennstoffzeile>();
            if (_ergebnis.BHKW != null && _ergebnis.BHKW.Module != null)
                foreach (ErgebnisBHKWModulModel m in _ergebnis.BHKW.Module)
                    liste.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Verbrauch, CarrierId = m.CarrierId });
            return liste;
        }

        private List<Brennstoffzeile> KesselModule()
        {
            var liste = new List<Brennstoffzeile>();
            if (_ergebnis.Heizkessel != null && _ergebnis.Heizkessel.Module != null)
                foreach (ErgebnisHeizkesselModulModel m in _ergebnis.Heizkessel.Module)
                    liste.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Verbrauch, CarrierId = m.CarrierId });
            return liste;
        }

        /// <summary>Brennstoff-Endenergie (BHKW/Heizkessel): Σ Verbrauch, Kosten je
        /// Modul zum Arbeitspreis seines Trägers.</summary>
        private Groesse Brennstoffsumme(List<Brennstoffzeile> module, string anlagenName, string komponentenWort)
        {
            double bedarfKwh = 0;
            double kosten = 0;
            bool kostenVollstaendig = true;
            int getroffen = 0;

            foreach (Brennstoffzeile m in module)
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;

                getroffen++;
                if (m.VerbrauchMWh <= 0) continue;
                bedarfKwh += m.VerbrauchMWh * 1000.0;

                double? preis = Preis(m.CarrierId);
                if (preis.HasValue) kosten += m.VerbrauchMWh * 1000.0 * preis.Value;
                else kostenVollstaendig = false;
            }

            if (anlagenName != null && getroffen == 0) return null;   // Anlage nicht im Lauf
            if (bedarfKwh <= 0) return null;

            return new Groesse
            {
                BedarfKwh = bedarfKwh,
                KostenEuro = kostenVollstaendig ? kosten : (double?)null,
                Basis = anlagenName != null
                    ? komponentenWort + " „" + anlagenName + "“"
                    : string.Format(CultureInfo.CurrentCulture, "alle {0}-Module", komponentenWort)
            };
        }

        /// <summary>Strom-Endenergie der Wärmepumpe: (Stromverbrauch + Heizstab) ×
        /// Strombezugspreis.</summary>
        private Groesse Waermepumpensumme(string anlagenName)
        {
            if (_ergebnis.Waermepumpe == null || _ergebnis.Waermepumpe.Module == null) return null;

            double bedarfKwh = 0;
            int getroffen = 0;
            foreach (ErgebnisWaermepumpeModulModel m in _ergebnis.Waermepumpe.Module)
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;
                getroffen++;
                bedarfKwh += (m.Stromverbrauch + m.Heizstab) * 1000.0;
            }

            if (anlagenName != null && getroffen == 0) return null;
            if (bedarfKwh <= 0) return null;

            double? preis = StrompreisJeKwh;
            return new Groesse
            {
                BedarfKwh = bedarfKwh,
                KostenEuro = preis.HasValue ? bedarfKwh * preis.Value : (double?)null,
                Basis = anlagenName != null
                    ? "Wärmepumpe „" + anlagenName + "“"
                    : "alle Wärmepumpen-Module"
            };
        }
    }
}
