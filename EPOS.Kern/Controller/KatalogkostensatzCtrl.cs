using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Kostensatz EINES Katalog- oder Projektsatzes</b> — die Leseschicht der
    /// Katalogdialoge und Stammblätter seit Welle KB-4 (Anwenderentscheid 13.10.2026
    /// „eine Kostenbasis, keine Doppelhaltung"). Die Kostenspalten der Gerätetabellen
    /// sind mit Schemaschritt <see cref="GeraetekostenEntfallSchema.SCHRITT"/> entfallen;
    /// gepflegt wird der Satz in der Kostenverwaltung, und der Gerätesatz verweist über
    /// <c>ID_KostenVorlageInvestition</c> auf seine Satzvorlage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Dieselbe Zahl, die die Kostenverwaltung führt.</b> Die Masken zeigen den Satz
    /// der Satzvorlage, nicht einen zweiten gepflegten Wert — wer ihn ändern will, geht in
    /// die Kostenverwaltung. Der Planwert einer ANLAGE rechnet denselben Satz mit Baugröße
    /// und Stückzahl hoch (<see cref="TechnikPlanwertCtrl"/>); hier steht der Satz selbst,
    /// also die Zahl, die der Anwender dort eingetragen hat.
    /// </para>
    /// <para>
    /// <b>Die Einheit nennt das Gewerk, nicht die Maske.</b> Welche Bemessung ein Gewerk
    /// führt, steht in seiner Feldliste (<see cref="Katalogkostenfeld.Bemessung"/>) — der
    /// EINEN Liste, aus der auch Saat und Planwert rechnen; das Einheitenzeichen kommt aus
    /// <see cref="BemessungKatalog"/>. Ein Gewerk, das die Liste nicht führt, bekommt
    /// keinen Satz und keine Einheit: lieber keine Zahl als eine geratene.
    /// </para>
    /// <para>
    /// <b>Tabellennamen nur aus der Landkarte.</b> Der Name der Tabelle wird gegen die
    /// Gewerkliste gehalten und nie aus einer Eingabe übernommen; die ID bleibt Parameter
    /// (Hausregel SQL-Dialekt, <c>?</c>-Parameter über <see cref="DataRepository"/>).
    /// </para>
    /// </remarks>
    public static class KatalogkostensatzCtrl
    {
        /// <summary>Das Gewerk zu einer Katalog- oder Projekttabelle; <c>null</c> = keines.</summary>
        private static Katalogkostengewerk Gewerk(string tabelle)
        {
            if (string.IsNullOrEmpty(tabelle)) return null;
            foreach (Katalogkostengewerk g in GeraetekostenEntfallSchema.GEWERKE)
                if (string.Equals(g.Katalog, tabelle, StringComparison.Ordinal) ||
                    string.Equals(g.Kopie, tabelle, StringComparison.Ordinal)) return g;
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
    }
}
