using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Was die Editoren der Stufe Experte vor dem Schreiben wissen müssen</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 5.1 „Tagesgang bearbeiten", 5.3 „Zapfkategorien /
    /// Streuung — als Katalogkopie bearbeitbar"; Stufe Z4, Gruppe 2b): ob
    /// <see cref="TagesgangSpeichern"/> an Ort und Stelle schriebe oder eine neue Katalogversion
    /// anlegte (<see cref="TagesgangSperre"/>), eine freie Katalogversion für die Kopie
    /// (<see cref="FreieKopieversion"/>) und die Normierung eines Anteilsrasters auf Σ 1
    /// (<see cref="AnteileNormiert"/>) — dieselbe Regel für die Vorschau des Editors und für den
    /// Schreibweg. Nur lesend; geschrieben wird allein über <see cref="TagesgangSpeichern"/> und
    /// <see cref="KategorienSpeichern"/>.
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        /// <summary>
        /// Das Zeichen zwischen der Katalogversion der Vorlage und der laufenden Nummer einer
        /// Anwenderkopie („TEST-1-E1") — neutral, ohne Sprache.
        /// </summary>
        internal const string KOPIEVERSION_ZUSATZ = "-E";

        /// <summary>
        /// Die Sperre des Tagesgangs der Nutzungsart <paramref name="idNutzungsart"/> — dieselbe
        /// Regel wie in <see cref="TagesgangSpeichern"/>: <see cref="TwwKatalogAusgang.Ausgefuehrt"/>
        /// heißt „ein geänderter Tagesgang wird an Ort und Stelle geschrieben"; sonst
        /// <see cref="TwwKatalogAusgang.ReadOnlyGesperrt"/> (Nutzungsart, Kategorie oder Satz der
        /// Auslieferung) bzw. <see cref="TwwKatalogAusgang.BenutztGesperrt"/> (eine Zone benutzt die
        /// Nutzungsart oder den Satz, oder eine ANDERE Nutzungsart trägt den Satz) — dann entsteht
        /// eine neue Katalogversion. Ohne Zeile, Satz oder Tabellen der benannte Ausgang.
        /// </summary>
        internal static TwwKatalogAusgang TagesgangSperre(int idNutzungsart)
        {
            if (!TabellenVorhanden()) return TwwKatalogAusgang.TabellenFehlen;
            Nutzungsart n = Lies(idNutzungsart);
            if (n == null) return TwwKatalogAusgang.NichtGefunden;
            if (IstReadOnly(idNutzungsart)) return TwwKatalogAusgang.ReadOnlyGesperrt;
            if (IstBenutzt(idNutzungsart)) return TwwKatalogAusgang.BenutztGesperrt;

            int satz = n.Tagesgaenge?.Id ?? 0;
            if (satz <= 0) return TwwKatalogAusgang.TagesgangsatzFehlt;
            if (TagesgangsatzIstReadOnly(satz)) return TwwKatalogAusgang.ReadOnlyGesperrt;
            object zonen = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_ZONE + " WHERE ID_Tagesgangsatz = ?",
                new DbParam("@satz", satz));
            object andere = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID_Tagesgangsatz = ? AND ID <> ?",
                new DbParam("@satz", satz), new DbParam("@id", idNutzungsart));
            return Zahl(zonen) > 0 || Zahl(andere) > 0 ? TwwKatalogAusgang.BenutztGesperrt : TwwKatalogAusgang.Ausgefuehrt;
        }

        /// <summary>
        /// <b>Eine freie Katalogversion für eine Anwenderkopie</b> der Nutzungsart
        /// <paramref name="idNutzungsart"/>: ihre Katalogversion mit <see cref="KOPIEVERSION_ZUSATZ"/>
        /// und der kleinsten Nummer ab 1, unter der weder die Nutzungsart noch ihr Tagesgangsatz
        /// (Bezeichner, Katalogversion) schon steht — nur ein Vorschlag; der Schreibweg prüft den
        /// natürlichen Schlüssel erneut (<see cref="TwwKatalogAusgang.NameBelegt"/>). <c>""</c>, wenn es
        /// die Nutzungsart (oder die Tabellen) nicht gibt.
        /// </summary>
        internal static string FreieKopieversion(int idNutzungsart)
        {
            if (!TabellenVorhanden()) return "";
            Nutzungsart n = Lies(idNutzungsart);
            if (n == null) return "";

            string basis = (n.Katalogversion ?? "").Trim() + KOPIEVERSION_ZUSATZ;
            HashSet<string> belegt = Versionen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, n.Name);
            if (n.Tagesgaenge != null) belegt.UnionWith(Versionen(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, n.Tagesgaenge.Bezeichner));
            for (int k = 1; ; k++)
            {
                string kandidat = basis + k.ToString(CultureInfo.InvariantCulture);
                if (!belegt.Contains(kandidat)) return kandidat;
            }
        }

        /// <summary>
        /// <b>Ein Anteilsraster auf Σ 1 normiert</b> (Tagesgang je Tagtyp, Wochenfaktoren): jeder
        /// Wert durch die Summe. <c>null</c>, wenn ein Wert fehlt (NaN), negativ oder nicht endlich
        /// ist oder die Summe nicht größer 0 — die Regeln des Schreibwegs
        /// (<see cref="TagesgangSpeichern"/>: endlich, nicht negativ, Σ 1).
        /// </summary>
        internal static double[] AnteileNormiert(IReadOnlyList<double> werte)
        {
            if (werte == null || werte.Count == 0) return null;
            double summe = 0.0;
            foreach (double w in werte)
            {
                if (!NichtNegativ(w)) return null;
                summe += w;
            }
            if (!(summe > 0) || double.IsInfinity(summe)) return null;
            var normiert = new double[werte.Count];
            for (int i = 0; i < normiert.Length; i++) normiert[i] = werte[i] / summe;
            return normiert;
        }

        /// <summary>Die Katalogversionen, unter denen <paramref name="bezeichner"/> in <paramref name="tabelle"/> steht.</summary>
        private static HashSet<string> Versionen(string tabelle, string bezeichner)
        {
            var menge = new HashSet<string>(StringComparer.Ordinal);
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT Katalogversion FROM " + tabelle + " WHERE Bezeichner = ?",
                new DbParam("@b", (bezeichner ?? "").Trim()));
            if (dt != null)
                foreach (System.Data.DataRow r in dt.Rows) menge.Add(ZapfprofilCtrl.Text(r, "Katalogversion"));
            return menge;
        }

        private static long Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
    }
}
