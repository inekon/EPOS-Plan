using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Zapfkategorien</b> (Stufe Experte; Umsetzungskonzept Zapfprofilgenerator 4.4,
    /// 5.3 „Zapfkategorien / Streuung — als Katalogkopie bearbeitbar"; Stufe Z4, Gruppe 1): liest die
    /// Kategorien einer Nutzungsart samt Summe, Hinweis, Sperre und Vorgabesatz als DTO und speichert
    /// den Editor über <see cref="TwwNutzungsartCtrl.KategorienSpeichern"/> — an Ort und Stelle oder,
    /// bei einer gesperrten Nutzungsart, als neue Katalogversion. Die Herkunft ist nur Anzeige: Die
    /// Provenienz führt der Kern nach. Geschrieben wird sofort (der Editor gehört zum Katalog, nicht
    /// zum Arbeitsstand, 5.1); die Zone wechselt der Dialog auf die neue Nutzungsart.
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Die Kategorien der Nutzungsart für den Editor; ohne Tabellen oder Zeile mit Sperrgrund.</summary>
        internal static ZapfprofilKategorienDaten Kategorien(int idNutzungsart)
        {
            TwwKategorienStand s = TwwNutzungsartCtrl.KategorienLesen(idNutzungsart);
            return new ZapfprofilKategorienDaten
            {
                IdNutzungsart = idNutzungsart,
                Kategorien = s.Kategorien.Select(AlsKategorie).ToList(),
                SummeAnteil = s.SummeAnteil,
                Hinweis = Satztext(s.Hinweis),
                Frei = s.Frei,
                Sperrgrund = s.Frei ? "" : Kategoriengrund(s.Sperre),
                Vorgabe = TwwNutzungsartCtrl.KategorienVorgabe().Select(AlsKategorie).ToList()
            };
        }

        /// <summary>
        /// Speichert die Kategorien des Editors (Reihenfolge = Position). Ein leeres Feld wird nie still
        /// ersetzt: Es verletzt die Regeln und kommt als benannte Ablehnung zurück, ebenso eine
        /// gesperrte Nutzungsart ohne Katalogversion der Kopie.
        /// </summary>
        internal static ZapfprofilKategorienErgebnis KategorienSpeichern(int idNutzungsart,
                                                                        IReadOnlyList<ZapfprofilKategorieDaten> kategorien,
                                                                        string katalogversion)
        {
            List<Zapfkategorie> kern = AlsKernkategorien(idNutzungsart, kategorien);
            TwwKategorienErgebnis e = TwwNutzungsartCtrl.KategorienSpeichern(idNutzungsart, kern, katalogversion);
            if (e.Ok) return new ZapfprofilKategorienErgebnis(true, e.IdNutzungsart, e.NeueZeile, null);

            string grund = e.Grund != null ? Satztext(e.Grund) : Kategoriengrund(e.Ausgang);
            string kennung = e.Grund?.Schluessel ?? KategorienSchluessel(e.Ausgang);
            string text = Format(Text_("ZPG_KATEG_MSG_NICHT_GESPEICHERT", "Die Zapfkategorien wurden nicht gespeichert — {0}"), grund);
            return new ZapfprofilKategorienErgebnis(false, idNutzungsart, false,
                new ZapfprofilMeldung(kennung, "", text, ZapfprofilMeldungsart.Fehler, e.Grund?.Klartext ?? ""));
        }

        /// <summary>Der Ressourcenschlüssel des Grundes einer Pflegeaktion: <c>ZPG_KATEG_GRUND_…</c>.</summary>
        internal static string KategorienSchluessel(TwwKatalogAusgang a) => "ZPG_KATEG_GRUND_" + Gross(a.ToString());

        private static string Kategoriengrund(TwwKatalogAusgang a) => Text_(KategorienSchluessel(a), a.ToString());

        private static ZapfprofilKategorieDaten AlsKategorie(Zapfkategorie k) => new ZapfprofilKategorieDaten
        {
            Name = k.Name ?? "",
            VolumenstromLJeMin = k.VolumenstromLJeMin,
            StreuungLJeMin = k.StreuungLJeMin,
            DauerMin = k.DauerMin,
            Anteil = k.Anteil,
            KappungLJeMin = k.KappungLJeMin,
            Herkunft = Herkunft(k.Herkunft)
        };
    }
}
