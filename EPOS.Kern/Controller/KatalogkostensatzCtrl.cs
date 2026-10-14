using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kosten EINES Katalog- oder Projektsatzes als Lesewert</b> — die eine Leseschicht
    /// der Katalogdialoge, Stammblätter und Aufklapper (Anwenderentscheid 13.10.2026 „eine
    /// Kostenbasis, keine Doppelhaltung"). Die Gerätetabellen tragen keine Kostenspalten
    /// (<see cref="GeraetekostenEntfallSchema"/>); gepflegt werden die Kosten in der
    /// Kostenverwaltung, und der Gerätesatz verweist über <c>ID_KostenVorlageInvestition</c> auf
    /// seine Investitions-Satzvorlage. Zwei Lesarten derselben Vorlage:
    /// <list type="bullet">
    /// <item><see cref="Satz(string,int)"/> — der Satz je Bemessungsgröße, so wie der Anwender ihn
    /// eingetragen hat (Wärmepumpe €/kW, Photovoltaik €/kWp, Wechselrichter);</item>
    /// <item><see cref="Betrag(string,DataRow)"/> — der Betrag für EIN Stück: absolute Positionen
    /// wie eingetragen, spezifische mal Baugröße des Satzes (Heizkessel, BHKW, Pufferspeicher,
    /// Solarkollektor, Kältemaschine; Lesewert „Gerätekosten").</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Dieselbe Zahl, die die Kostenverwaltung führt.</b> Die Masken zeigen die Vorlage,
    /// nicht einen zweiten gepflegten Wert — wer ihn ändern will, geht in die Kostenverwaltung.
    /// </para>
    /// <para>
    /// <b>Warum nicht <see cref="TechnikPlanwertCtrl"/>.</b> Der Planwert dort gehört einer
    /// ANLAGE: Er nimmt die Stückzahl der Anlagenzeile und trennt die vier BHKW-Nebenposten ab,
    /// weil die Kostenverwaltung sie als eigene Zeilen führt. Der Katalogsatz hat keine Anlage —
    /// sein Betrag rechnet mit einem Stück und zeigt die Vorlage als EINE Summe. Die
    /// Bezugsgröße nennt beide Male dieselbe Liste (<see cref="Katalogkostenfeld.Bezugsspalte"/>).
    /// </para>
    /// <para>
    /// <b>Die Einheit nennt das Gewerk, nicht die Maske.</b> Welche Bemessung ein Gewerk
    /// führt, steht in seiner Feldliste (<see cref="Katalogkostenfeld.Bemessung"/>) — der
    /// EINEN Liste, aus der auch Saat und Planwert rechnen; das Einheitenzeichen kommt aus
    /// <see cref="BemessungKatalog"/>. Ein Gewerk, das die Liste nicht führt, bekommt
    /// keinen Satz, keinen Betrag und keine Einheit: lieber keine Zahl als eine geratene.
    /// </para>
    /// <para>
    /// <b>Tabellennamen nur aus der Landkarte.</b> Der Name der Tabelle wird gegen die
    /// Gewerkliste gehalten und nie aus einer Eingabe übernommen; die ID bleibt Parameter
    /// (Hausregel SQL-Dialekt, <c>?</c>-Parameter über <see cref="DataRepository"/>).
    /// </para>
    /// </remarks>
    public static class KatalogkostensatzCtrl
    {
        /// <summary>
        /// Das Gewerk zu einer Katalog- oder Projekttabelle; <c>null</c> = keines. Der Satz
        /// vergleicht den Namen genau (er geht in eine Abfrage), der Betrag der gelesenen Zeile
        /// ohne Rücksicht auf Groß- und Kleinschreibung.
        /// </summary>
        private static Katalogkostengewerk Gewerk(string tabelle,
            StringComparison vergleich = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(tabelle)) return null;
            foreach (Katalogkostengewerk g in GeraetekostenEntfallSchema.GEWERKE)
                if (string.Equals(g.Katalog, tabelle, vergleich) ||
                    string.Equals(g.Kopie, tabelle, vergleich)) return g;
            return null;
        }

        /// <summary>Das Investitionsfeld des Gewerks — es nennt die Bemessung des Satzes.</summary>
        private static Katalogkostenfeld Investitionsfeld(Katalogkostengewerk g)
        {
            if (g == null) return null;
            foreach (Katalogkostenfeld f in g.Felder)
                if (f.Kategorie == DbWerte.KOSTEN_KATEGORIE_INVESTITION) return f;
            return null;
        }

        /// <summary>
        /// <b>Das Einheitenzeichen des Satzes</b> dieses Gewerks („€", „€/kWp",
        /// „€/kW"); leere Zeichenkette, wenn das Gewerk keinen Satz führt.
        /// </summary>
        public static string Einheit(string tabelle)
        {
            Katalogkostenfeld f = Investitionsfeld(Gewerk(tabelle));
            if (f == null) return "";
            BemessungKatalog.Info info = BemessungKatalog.Finde(f.Bemessung);
            return info == null ? "" : (info.Einheit ?? "");
        }

        /// <summary>
        /// <b>Der Satz der Investitions-Satzvorlage</b> eines Satzes dieser Tabelle:
        /// die Summe der Positionssätze ihrer Vorlage ohne die Ertragspositionen.
        /// <c>null</c> = kein Verweis, keine Vorlage oder kein gepflegter Satz.
        /// </summary>
        public static double? Satz(string tabelle, int id)
        {
            if (Gewerk(tabelle) == null || id <= 0) return null;
            return SatzDerVorlage(Vorlagenverweis(tabelle, id));
        }

        /// <summary>
        /// Der Satz als TEXT in der Kultur des Anwenders — die Form, in der ihn ein
        /// Lesewert einer Maske zeigt. Leere Zeichenkette = kein gepflegter Satz.
        /// </summary>
        public static string SatzText(string tabelle, int id)
        {
            double? satz = Satz(tabelle, id);
            return satz.HasValue ? satz.Value.ToString(CultureInfo.CurrentCulture) : "";
        }

        /// <summary>
        /// <b>Der Satz EINER Satzvorlage</b> — derselbe Weg wie <see cref="Satz(string,int)"/>, nur dass
        /// der Verweis schon vorliegt (ein Satz, der mit <c>SELECT *</c> gelesen wurde, trägt ihn).
        /// <c>null</c> = keine Vorlage oder kein gepflegter Satz.
        /// </summary>
        public static double? SatzDerVorlage(int idVorlage)
        {
            if (idVorlage <= 0) return null;
            double summe = 0;
            bool gepflegt = false;
            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(idVorlage))
            {
                if (p == null || p.IstErloes || !p.Satz.HasValue) continue;
                summe += p.Satz.Value;
                gepflegt = true;
            }
            return gepflegt ? (double?)summe : null;
        }

        /// <summary>Der Satz einer Satzvorlage als TEXT in der Kultur des Anwenders; leer = keiner.</summary>
        public static string SatzTextDerVorlage(int idVorlage)
        {
            double? satz = SatzDerVorlage(idVorlage);
            return satz.HasValue ? satz.Value.ToString(CultureInfo.CurrentCulture) : "";
        }

        /// <summary>
        /// <b>Der Betrag der Investitions-Satzvorlage</b> dieser Zeile als Anzeigetext in der Kultur des
        /// Anwenders — ohne Vorlage, ohne Betrag oder ohne Bezugsgröße ein LEERER Text: Lieber
        /// keine Zahl als eine geratene (Vermerk der Oberfläche nennt die Kostenverwaltung).
        /// </summary>
        public static string BetragText(string tabelle, DataRow r)
        {
            double betrag = Betrag(tabelle, r);
            return betrag > 0 ? betrag.ToString("0.##", CultureInfo.CurrentCulture) : "";
        }

        /// <summary>
        /// Die Investitionssumme der Satzvorlage dieser Zeile [€] für EIN Stück; 0 heißt „keine
        /// Vorlage oder nichts darin, was sich an diesem Gewerk bemessen lässt".
        /// </summary>
        public static double Betrag(string tabelle, DataRow r)
        {
            if (r == null || string.IsNullOrEmpty(tabelle)) return 0;

            Katalogkostengewerk gewerk = Gewerk(tabelle, StringComparison.OrdinalIgnoreCase);
            if (gewerk == null) return 0;

            int idVorlage = Ganz(r, KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION);
            if (idVorlage <= 0) return 0;

            double summe = 0;
            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(idVorlage))
            {
                if (p == null || p.IstErloes || !p.Satz.HasValue) continue;
                double? betrag = Positionsbetrag(gewerk, r, p);
                if (betrag.HasValue && betrag.Value > 0) summe += betrag.Value;
            }
            return summe;
        }

        /// <summary>
        /// Der Betrag EINER Vorlagenposition an diesem Satz: ein absoluter Satz ist der Betrag,
        /// ein spezifischer wird mit der Baugröße eines Stücks multipliziert. <c>null</c> = die
        /// Position trägt an diesem Gewerk nichts bei (unbekannte Bemessung, fehlende oder leere
        /// Bezugsgröße) — dieselbe Regel wie beim Planwert der Anlage.
        /// </summary>
        private static double? Positionsbetrag(Katalogkostengewerk gewerk, DataRow r,
                                               KostenVorlagenPosition p)
        {
            if (Katalogkostenabgleich.Absolut(p.Bemessung)) return p.Satz.Value;

            Katalogkostenfeld feld = null;
            foreach (Katalogkostenfeld kandidat in gewerk.Felder)
                if (string.Equals(kandidat.Bemessung, p.Bemessung, StringComparison.Ordinal) &&
                    !string.IsNullOrEmpty(kandidat.Bezugsspalte)) { feld = kandidat; break; }
            if (feld == null) return null;

            double groesse = Zahl(r, feld.Bezugsspalte) /
                             (feld.Bezugsteiler == 0 ? 1.0 : feld.Bezugsteiler);
            return groesse > 0 ? (double?)(p.Satz.Value * groesse) : null;
        }

        /// <summary>Der Verweis <c>ID_KostenVorlageInvestition</c> dieses Satzes; 0 = keiner.</summary>
        private static int Vorlagenverweis(string tabelle, int id)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT \"" + KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION +
                    "\" FROM \"" + tabelle + "\" WHERE \"ID\" = ?",
                    new DbParam("@id", id));
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        private static double Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static int Ganz(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }
}
