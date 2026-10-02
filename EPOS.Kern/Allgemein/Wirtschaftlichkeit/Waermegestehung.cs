using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wärmegestehungskosten „nur Wärmeerzeuger"</b> (Anwenderentscheid 30.09.2026:
    /// „Die Gestehungskosten sollten nur den Bedarf und die Kosten der den Anlagen zugeordneten
    /// enthalten.") — die Regel an EINER Stelle.
    ///
    /// <para><b>Die Kennzahl.</b> Kosten der Wärmeerzeugung, annuisiert, je kWh Jahreswärmebedarf:
    /// <c>(−Kapitalwert der Wärmeerzeugung × a(i, T)) ÷ (Wärmebedarf × 1.000)</c>. Der Kapitalwert
    /// der Wärmeerzeugung entsteht mit demselben Rechenkern wie der des Projekts
    /// (<see cref="KapitalwertRechner"/>) — nur aus einem Zahlungsgerüst, das allein die
    /// Wärmeerzeugung trägt:</para>
    /// <list type="bullet">
    ///   <item><description><b>Anlagen</b>: Investition nach Zuschuss, Betriebskosten,
    ///     Ersatzbeschaffungen und Restwert der Positionen, die <see cref="PositionZaehlt"/>
    ///     der Wärme zuordnet — Wärmepumpe, Heizkessel, BHKW, Solarthermie, Pufferspeicher und die
    ///     allgemeinen Positionen, die keiner Stromanlage zugeordnet sind.</description></item>
    ///   <item><description><b>Energie</b>: die Energiekosten der Wärmeerzeuger
    ///     (<see cref="Energiekosten"/>) — Brennstoff der Kessel und BHKW samt CO₂-Abgabe, der Strom
    ///     von Wärmepumpe, Heizstab und Elektrokessel zum Arbeitspreis des Stromträgers ohne
    ///     Anrechnung von PV-Eigenverbrauch, Grund- und Leistungspreis eines Trägers nach
    ///     <see cref="Anteil"/> —, abzüglich der Stromgutschrift für den im Projekt verbrauchten
    ///     BHKW-Strom (<see cref="StromgutschriftEur"/>).</description></item>
    ///   <item><description><b>Erlöse</b>: die Erlöse der Wärmeerzeuger — eingespeister BHKW-Strom
    ///     und die Reihen aus <see cref="ErloesReiheZaehlt"/>.</description></item>
    /// </list>
    /// <para><b>Draußen</b> bleiben Haushaltsstrom und Stromverbraucher, Kältestrom und Kühlung,
    /// Photovoltaik (Investition, Betrieb, Erlöse, Eigenverbrauch) und Stromspeicher. Der
    /// <b>Kapitalwert</b> und alle übrigen Kennzahlen bleiben projektweit — diese Klasse ändert
    /// nur die eine Kennzahl.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche</b>: Die Mengen und Preise liefern
    /// <see cref="KostenEmissionRechner"/> (je Träger, <see cref="EnergieTraegerNachweis"/>) und
    /// die Wirtschaftlichkeit (Positionen, Erlöse); hier steht allein, was davon zählt.</para>
    /// </summary>
    internal static class Waermegestehung
    {
        // ------------------------------------------------------------------
        // Kostenkomponenten (Tab_KostenKomponente.ID, feste Nummern der Auslieferung)
        // ------------------------------------------------------------------
        internal const int KOMPONENTE_WAERMEPUMPE = 1;
        internal const int KOMPONENTE_HEIZKESSEL = 2;
        internal const int KOMPONENTE_PHOTOVOLTAIK = 3;
        internal const int KOMPONENTE_SOLARTHERMIE = 4;
        internal const int KOMPONENTE_STROMSPEICHER = 5;
        internal const int KOMPONENTE_PUFFERSPEICHER = 6;
        internal const int KOMPONENTE_BHKW = 7;
        internal const int KOMPONENTE_WAERMEZENTRALE = 8;
        internal const int KOMPONENTE_BAULICHE_ANLAGEN = 9;
        internal const int KOMPONENTE_STROMEINSPEISUNG = 10;

        // ------------------------------------------------------------------
        // Anlagentypen (Tab_Typ_Energieanlagen.ID)
        // ------------------------------------------------------------------
        internal const int TYP_WAERMEPUMPE = 1;
        internal const int TYP_SOLARTHERMIE = 2;
        internal const int TYP_PHOTOVOLTAIK = 3;
        internal const int TYP_BATTERIESPEICHER = 4;
        internal const int TYP_HEIZKESSEL = 10;
        internal const int TYP_BHKW = 11;
        internal const int TYP_PUFFERSPEICHER = 12;

        /// <summary>
        /// <b>Die Zuordnung einer Kostenposition</b> (Investition, Zuschuss, Betriebskosten):
        /// Zählt sie zur Wärmeerzeugung?
        /// <list type="number">
        ///   <item><description>Photovoltaik und Stromspeicher: nie.</description></item>
        ///   <item><description>Wärmepumpe, Heizkessel, Solarthermie, Pufferspeicher, BHKW:
        ///     immer.</description></item>
        ///   <item><description>Allgemeine Positionen (Wärmezentrale, bauliche Anlagen,
        ///     Stromeinspeisung, Positionen ohne oder mit unbekannter Komponente): Ist die Position
        ///     einer Anlage zugeordnet, entscheidet deren Typ (<see cref="AnlagentypZaehlt"/>);
        ///     sonst zählt sie — außer der Stromeinspeisung, die nur zählt, wenn das Projekt ein
        ///     BHKW führt (dessen Einspeisung gutgeschrieben wird).</description></item>
        /// </list>
        /// </summary>
        /// <param name="komponente"><c>Tab_ProjektWerte.KomponentenID</c>; 0 = keine.</param>
        /// <param name="anlagentyp">Typ der zugeordneten Anlage (<c>Tab_Energieanlagen.ID_Type</c>);
        /// 0 = keine Anlage zugeordnet oder Typ unbekannt.</param>
        /// <param name="bhkwImProjekt">Führt das Projekt ein BHKW?</param>
        internal static bool PositionZaehlt(int komponente, int anlagentyp, bool bhkwImProjekt)
        {
            switch (komponente)
            {
                case KOMPONENTE_PHOTOVOLTAIK:
                case KOMPONENTE_STROMSPEICHER:
                    return false;
                case KOMPONENTE_WAERMEPUMPE:
                case KOMPONENTE_HEIZKESSEL:
                case KOMPONENTE_SOLARTHERMIE:
                case KOMPONENTE_PUFFERSPEICHER:
                case KOMPONENTE_BHKW:
                    return true;
            }
            if (anlagentyp > 0) return AnlagentypZaehlt(anlagentyp);
            if (komponente == KOMPONENTE_STROMEINSPEISUNG) return bhkwImProjekt;
            return true;
        }

        /// <summary>Ist der Anlagentyp ein Wärmeerzeuger oder Wärmespeicher? Photovoltaik und
        /// Batteriespeicher nicht; ein unbekannter Typ zählt wie eine allgemeine Position.</summary>
        internal static bool AnlagentypZaehlt(int anlagentyp)
        {
            return anlagentyp != TYP_PHOTOVOLTAIK && anlagentyp != TYP_BATTERIESPEICHER;
        }

        /// <summary>
        /// <b>Die Erlösreihen der Wärmeerzeuger</b>: KWK-Zuschlag und seine Pauschale,
        /// Energiesteuer-Entlastung (§ 53/§ 53a/§ 54 EnergieStG — Brennstoff von BHKW und Kessel)
        /// und die Stromsteuer-Befreiung des BHKW-Stroms (§ 9 Abs. 1 Nr. 3 StromStG). Nicht dabei:
        /// die PV-Vergütung und die Stromsteuer-Entlastung nach § 9b StromStG, die am Netzbezug des
        /// ganzen Anschlusses hängt.
        /// </summary>
        internal static bool ErloesReiheZaehlt(string name)
        {
            return string.Equals(name, KapitalwertRechner.ErloesReihe.KWKG, StringComparison.Ordinal) ||
                   string.Equals(name, KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, StringComparison.Ordinal) ||
                   string.Equals(name, KapitalwertRechner.ErloesReihe.ENERGIESTEUER, StringComparison.Ordinal) ||
                   string.Equals(name, KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Der Anteil der Wärmeerzeuger an Grund- und Leistungspreis eines Trägers</b> —
        /// nach Jahresmenge: Gehört der Träger nur Wärmeerzeugern, ganz (1); teilen sich
        /// Wärmeerzeuger und andere Verbraucher den Träger (typisch Strom), ihr Mengenanteil.
        /// Ohne Wärmemenge 0; ohne bekannte Gesamtmenge ganz.
        /// </summary>
        internal static double Anteil(double waermeMWh, double gesamtMWh)
        {
            if (!(waermeMWh > 0)) return 0.0;
            if (!(gesamtMWh > waermeMWh)) return 1.0;
            return waermeMWh / gesamtMWh;
        }

        /// <summary>
        /// <b>Die Energiekosten der Wärmeerzeuger [€/a]</b> aus der Aufstellung je Träger:
        /// je Träger die Arbeitskosten der Wärmemenge plus Grund- und Leistungspreis nach
        /// <see cref="Anteil"/>. Die CO₂-Abgabe steht nicht hier (sie ist eine eigene Reihe und
        /// hängt ganz am Brennstoff der Wärmeerzeuger). <c>null</c> ohne Aufstellung.
        /// </summary>
        internal static double? Energiekosten(IEnumerable<EnergieTraegerNachweis> traeger)
        {
            if (traeger == null) return null;
            double summe = 0.0;
            bool irgendeiner = false;
            foreach (EnergieTraegerNachweis t in traeger)
            {
                if (t == null) continue;
                irgendeiner = true;
                double fix = t.GrundpreisEur + t.LeistungEur;
                summe += t.WaermeArbeitEur;
                if (fix != 0.0) summe += fix * Anteil(t.WaermeMengeMWh, t.VerbrauchGesamtMWh);
            }
            return irgendeiner ? (double?)summe : null;
        }

        /// <summary>
        /// Der Strom der Wärmeerzeuger [MWh/a]: Wärmepumpe, Heizstab und Elektrokessel — die
        /// Mengen, die der Lauf je Erzeuger getrennt vom Grundstrombedarf führt
        /// (<see cref="ErgebnisWaermepumpeModel.Stromverbrauch_WP"/>,
        /// <see cref="ErgebnisWaermepumpeModel.Stromverbrauch_Heizstab"/>,
        /// <see cref="ErgebnisHeizkesselModel.Stromverbrauch"/>). Der Kältestrom der Wärmepumpe
        /// gehört nicht dazu.
        /// </summary>
        internal static double WaermestromMWh(ErgebnisModel m)
        {
            if (m == null) return 0.0;
            double s = 0.0;
            if (m.Waermepumpe != null)
                s += Math.Max(0.0, m.Waermepumpe.Stromverbrauch_WP) +
                     Math.Max(0.0, m.Waermepumpe.Stromverbrauch_Heizstab);
            if (m.Heizkessel != null) s += Math.Max(0.0, m.Heizkessel.Stromverbrauch);
            return s;
        }

        /// <summary>
        /// Der Stromverbrauch ALLER Verbraucher des Anschlusses [MWh/a] — die Bezugsgröße des
        /// Anteils am Grund- und Leistungspreis des Stromträgers: Grundstrombedarf (Stromverbraucher,
        /// Stromganglinien), Strom der Wärmeerzeuger und Kältestrom.
        /// </summary>
        internal static double StromverbrauchGesamtMWh(ErgebnisModel m)
        {
            if (m == null) return 0.0;
            double s = WaermestromMWh(m);
            if (m.Energiebedarf != null) s += Math.Max(0.0, m.Energiebedarf.Strombedarf_Gesamt);
            if (m.Waermepumpe != null && m.Waermepumpe.Stromverbrauch_Kuehlung.HasValue)
                s += Math.Max(0.0, m.Waermepumpe.Stromverbrauch_Kuehlung.Value);
            return s;
        }

        /// <summary>
        /// Der im Projekt verbrauchte BHKW-Strom [MWh/a]: aus der Strommatrix des Laufs
        /// (<see cref="StromMatrix.KwkEigenGesamtMWh"/>, stundenweise min-Regel), ohne Stundenreihen
        /// der Teil des Strombedarfs an der BHKW-Stufe, den das BHKW gedeckt hat (Strombedarf −
        /// Reststrombedarf, höchstens die Erzeugung). 0 ohne BHKW.
        /// </summary>
        internal static double BhkwEigenstromMWh(StromMatrix matrix, ErgebnisBHKWModel bhkw)
        {
            if (bhkw == null || !(bhkw.Stromproduktion > 0)) return 0.0;
            if (matrix != null && !matrix.StrombedarfFehlt)
                return Math.Max(0.0, matrix.KwkEigenGesamtMWh);
            double gedeckt = Math.Max(0.0, bhkw.Strombedarf - bhkw.Reststrombedarf);
            return Math.Min(bhkw.Stromproduktion, gedeckt);
        }

        /// <summary>
        /// <b>Die Stromgutschrift [€/a]</b>: der im Projekt verbrauchte BHKW-Strom als vermiedener
        /// Bezug zum Arbeitspreis des Stromträgers — damit der Wärmepreis eines BHKW nicht um seinen
        /// Eigenstrom verzerrt wird. 0 ohne Menge oder ohne Arbeitspreis.
        /// </summary>
        internal static double StromgutschriftEur(double eigenstromMWh, double? arbeitspreisJeKwh)
        {
            if (!(eigenstromMWh > 0) || !arbeitspreisJeKwh.HasValue) return 0.0;
            return eigenstromMWh * 1000.0 * arbeitspreisJeKwh.Value;
        }

        /// <summary>
        /// <b>Die Kennzahl [€/kWh]</b>: <c>(−Kapitalwert × a(i, T)) ÷ (Wärmebedarf × 1.000)</c>;
        /// <c>null</c> ohne Wärmebedarf.
        /// </summary>
        internal static double? Kennzahl(double kapitalwert, double zinsProzent, int jahre, double waermeMWh)
        {
            if (!(waermeMWh > 0)) return null;
            double a = KapitalwertRechner.Annuitaet(zinsProzent / 100.0, jahre);
            return (-kapitalwert * a) / (waermeMWh * 1000.0);
        }

        /// <summary>
        /// Die <b>Zerlegung des Zählers</b> [€/a, annuisiert]: Anlagen (Investition, Betrieb,
        /// Ersatz, Restwert), Energie (samt CO₂-Abgabe und abzüglich der Stromgutschrift) und
        /// Erlöse — Kosten positiv, Erlöse positiv als Abzug. Reine Auskunft aus dem Zahlungsbild
        /// der Wärmeerzeugung; die Kennzahl rechnet mit dem Kapitalwert.
        /// </summary>
        internal sealed class Zerlegung
        {
            /// <summary>Annuität der Anlagen [€/a] (Investition nach Zuschuss, Betrieb, Ersatz, Restwert).</summary>
            public double AnlagenEurJahr;
            /// <summary>Annuität der Energiekosten der Wärmeerzeuger [€/a], CO₂-Abgabe eingeschlossen,
            /// Stromgutschrift abgezogen.</summary>
            public double EnergieEurJahr;
            /// <summary>Darin: die Stromgutschrift des ersten Jahres [€/a] (nicht annuisiert).</summary>
            public double StromgutschriftJahr1;
            /// <summary>Annuität der Erlöse der Wärmeerzeuger [€/a].</summary>
            public double ErloeseEurJahr;
            /// <summary>Der Jahreswärmebedarf [kWh/a].</summary>
            public double WaermeKwh;

            /// <summary>Zähler [€/a] = Anlagen + Energie − Erlöse.</summary>
            public double ZaehlerEurJahr { get { return AnlagenEurJahr + EnergieEurJahr - ErloeseEurJahr; } }

            /// <summary>Aus der Gliederung des Zahlungsbilds der Wärmeerzeugung; <c>null</c> ohne sie.</summary>
            internal static Zerlegung Aus(Zahlungsgliederung g, double waermeMWh, double stromgutschriftJahr1)
            {
                if (g == null || !(waermeMWh > 0)) return null;
                double a = KapitalwertRechner.Annuitaet(g.ZinsProzent / 100.0, g.Jahre);
                double anlagen = 0, energie = 0, erloese = 0;
                foreach (Zahlungsbestandteil b in g.Bestandteile)
                {
                    if (b == null) continue;
                    if (string.Equals(b.Schluessel, Zahlungsgliederung.ENERGIE, StringComparison.Ordinal))
                        energie -= b.Barwert;
                    else if (string.Equals(b.Schluessel, Zahlungsgliederung.ERLOESE, StringComparison.Ordinal))
                        erloese += b.Barwert;
                    else anlagen -= b.Barwert;
                }
                return new Zerlegung
                {
                    AnlagenEurJahr = anlagen * a,
                    EnergieEurJahr = energie * a,
                    ErloeseEurJahr = erloese * a,
                    StromgutschriftJahr1 = stromgutschriftJahr1,
                    WaermeKwh = waermeMWh * 1000.0
                };
            }
        }
    }
}
