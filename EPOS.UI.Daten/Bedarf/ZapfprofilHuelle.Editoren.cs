using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Editoren der Stufe Experte</b> (Umsetzungskonzept Zapfprofilgenerator 5.1
    /// „Tagesgang bearbeiten", 5.3 „Zapfkategorien / Streuung"; Stufe Z4, Gruppe 2b) und der
    /// Ladeleistungs-Vorschlag des Zapfprofil-Dialogs.
    ///
    /// <para><b>Die Editoren gehören zum Katalog, nicht zum Arbeitsstand</b> (5.1): Ihr OK schreibt
    /// in EINER Transaktion über <see cref="TwwNutzungsartCtrl.TagesgangSpeichern"/> bzw.
    /// <see cref="TwwNutzungsartCtrl.KategorienSpeichern"/>. Ist die Nutzungsart gesperrt
    /// (Auslieferung, benutzt), entsteht eine neue Katalogversion (Status eigen); der
    /// Zapfprofil-Dialog stellt die Zone auf sie um und liest den Katalog neu
    /// (<see cref="Katalogstand"/>). Jede Ablehnung kommt benannt zurück — der Grund steht in der
    /// Oberflächensprache, der Ausgang des Kerns als Kennung.</para>
    ///
    /// <para><b>Normiert wird im Kern</b> (<see cref="TwwNutzungsartCtrl.AnteileNormiert"/>): Der
    /// Editor gibt Prozente, die Hülle teilt durch 100 und lässt je Reihe auf Σ 1 normieren;
    /// eine Reihe ohne Summe oder mit negativem Wert lehnt sie benannt ab
    /// (<see cref="TwwKatalogAusgang.RasterUngueltig"/>).</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Der Hilfeschlüssel des Tagesgang-Editors (5.8, Anker der Bedienseite).</summary>
        internal const string HILFE_TAGESGANG = "Form_Zapfprofil.grp_Tagesgang";

        /// <summary>Der Hilfeschlüssel des Kategorien-Editors (5.8, Anker der Bedienseite).</summary>
        internal const string HILFE_KATEGORIEN = "Form_Zapfprofil.grp_Kategorien";

        // =================================================================================
        // Tagesgang-Editor
        // =================================================================================

        /// <summary>
        /// Der Parametersatz der Komponente <c>TagesgangEditor.razor</c> zur Nutzungsart
        /// <paramref name="idNutzungsart"/> und dem Satz der Zone (<paramref name="idSatz"/>; <c>null</c> =
        /// der Satz der Nutzungsart): <c>Daten</c>, <c>Texte</c>, <c>Speichern</c>,
        /// <c>HilfeSchluessel</c>.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> TagesgangGaben(int idNutzungsart, int? idSatz)
            => new Dictionary<string, object>
            {
                ["Daten"] = TagesgangLaden(idNutzungsart, idSatz),
                ["Texte"] = Texte(),
                ["Speichern"] = new Func<ZapfprofilTagesgangEingabeDaten, ZapfprofilTagesgangErgebnis>(TagesgangSpeichern),
                ["HilfeSchluessel"] = HILFE_TAGESGANG
            };

        /// <summary>
        /// Der Stand des Editors: die Werte des Satzes der Zone, die Wochenfaktoren der Nutzungsart,
        /// die Vorlagen des Katalogs, ob „OK" eine neue Katalogversion anlegt (Sperre des Kerns,
        /// <see cref="TwwNutzungsartCtrl.TagesgangSperre"/>) samt Vorschlag der Version. Ohne Zeile,
        /// Satz oder Tabellen: <see cref="ZapfprofilTagesgangDaten.Verfuegbar"/> = false mit Grund.
        /// </summary>
        internal static ZapfprofilTagesgangDaten TagesgangLaden(int idNutzungsart, int? idSatz)
        {
            var d = new ZapfprofilTagesgangDaten { IdNutzungsart = idNutzungsart };
            IReadOnlyList<Tagesgangsatz> saetze = ZapfprofilCtrl.Tagesgangsaetze();
            d.Vorlagen = saetze.Select(AlsTagesgangsatzMitWerten).ToList();

            TwwKatalogAusgang sperre = TwwNutzungsartCtrl.TagesgangSperre(idNutzungsart);
            Nutzungsart n = sperre == TwwKatalogAusgang.TabellenFehlen ? null : TwwNutzungsartCtrl.Lies(idNutzungsart);
            if (n == null || sperre == TwwKatalogAusgang.NichtGefunden || sperre == TwwKatalogAusgang.TabellenFehlen
                || sperre == TwwKatalogAusgang.TagesgangsatzFehlt)
            {
                d.Verfuegbar = false;
                d.Grund = Tagesganggrund(n == null && sperre != TwwKatalogAusgang.TabellenFehlen ? TwwKatalogAusgang.NichtGefunden : sperre);
                if (n != null) d.Nutzungsart = Nutzungsartname(n);
                return d;
            }

            d.Nutzungsart = Nutzungsartname(n);
            d.Wochenfaktoren = (double[])(n.Wochenfaktoren ?? new double[ZapfprofilTagesgangDaten.WOCHENTAGE]).Clone();
            d.WochengangHerkunft = Herkunft(n.Herkunft?.Wochengang);

            // Der Satz der Zone: ihre Expertenwahl, sonst der Satz der Nutzungsart.
            Tagesgangsatz satz = idSatz.HasValue ? saetze.FirstOrDefault(s => s.Id == idSatz.Value) : null;
            satz ??= n.Tagesgaenge;
            d.Satz = satz != null ? AlsTagesgangsatzMitWerten(satz) : new ZapfprofilTagesgangsatzDaten();

            // Eine Expertenwahl, die nicht der Satz der Nutzungsart ist, geht als geänderter Tagesgang
            // an die Nutzungsart — die Sperre gilt dann wie bei jeder Änderung.
            d.Kopie = sperre != TwwKatalogAusgang.Ausgefuehrt;
            d.Sperrgrund = d.Kopie ? Tagesganggrund(sperre) : "";
            d.KatalogversionVorschlag = d.Kopie ? TwwNutzungsartCtrl.FreieKopieversion(idNutzungsart) : "";
            return d;
        }

        /// <summary>
        /// „OK" des Tagesgang-Editors: je Reihe Prozent → Anteil, auf Σ 1 normiert (Kern), dann
        /// <see cref="TwwNutzungsartCtrl.TagesgangSpeichern"/> in EINER Transaktion. Eine Reihe ohne
        /// Summe, mit negativem oder fehlendem Wert oder mit falscher Länge wird benannt abgelehnt;
        /// eine fehlende Katalogversion einer Kopie, ein belegter Name, eine gesperrte Zeile ebenso.
        /// </summary>
        internal static ZapfprofilTagesgangErgebnis TagesgangSpeichern(ZapfprofilTagesgangEingabeDaten e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            var gaenge = new List<double[]>();
            double[] woche = null;
            bool gueltig = e.TagesgaengeProzent != null && e.TagesgaengeProzent.Length == Tagesgangsatz.TAGTYPEN
                           && e.WochenfaktorenProzent != null && e.WochenfaktorenProzent.Length == NutzungsartRaster.WOCHENTAGE;
            if (gueltig)
            {
                foreach (double[] reihe in e.TagesgaengeProzent)
                {
                    double[] normiert = reihe != null && reihe.Length == Tagesgangsatz.STUNDEN
                        ? TwwNutzungsartCtrl.AnteileNormiert(reihe.Select(p => p / 100.0).ToArray()) : null;
                    if (normiert == null) { gueltig = false; break; }
                    gaenge.Add(normiert);
                }
                if (gueltig)
                {
                    woche = TwwNutzungsartCtrl.AnteileNormiert(e.WochenfaktorenProzent.Select(p => p / 100.0).ToArray());
                    gueltig = woche != null;
                }
            }
            if (!gueltig) return TagesgangAbgelehnt(e.IdNutzungsart, TwwKatalogAusgang.RasterUngueltig);

            TwwTagesgangErgebnis erg = TwwNutzungsartCtrl.TagesgangSpeichern(e.IdNutzungsart, gaenge, woche, e.Katalogversion);
            if (!erg.Ok) return TagesgangAbgelehnt(e.IdNutzungsart, erg.Ausgang);
            return new ZapfprofilTagesgangErgebnis(true, erg.IdNutzungsart, erg.IdTagesgangsatz, erg.IdNutzungsart != e.IdNutzungsart, null);
        }

        private static ZapfprofilTagesgangErgebnis TagesgangAbgelehnt(int idNutzungsart, TwwKatalogAusgang ausgang)
        {
            string kennung = TagesgangSchluessel(ausgang);
            string text = Format(Text_("ZPG_TGE_MSG_NICHT_GESPEICHERT", "Der Tagesgang wurde nicht gespeichert — {0}"),
                                 Tagesganggrund(ausgang));
            return new ZapfprofilTagesgangErgebnis(false, idNutzungsart, 0, false,
                new ZapfprofilMeldung(kennung, "", text, ZapfprofilMeldungsart.Fehler, ausgang.ToString()));
        }

        /// <summary>Der Ressourcenschlüssel des Grundes einer Pflegeaktion am Tagesgang: <c>ZPG_TGE_GRUND_…</c>.</summary>
        internal static string TagesgangSchluessel(TwwKatalogAusgang a) => "ZPG_TGE_GRUND_" + Gross(a.ToString());

        private static string Tagesganggrund(TwwKatalogAusgang a) => Text_(TagesgangSchluessel(a), a.ToString());

        /// <summary>Ein Tagesgangsatz samt Werten: 4 × 24 Anteile, Herkunft je Tagtyp; unvollständig = als Vorlage gesperrt.</summary>
        internal static ZapfprofilTagesgangsatzDaten AlsTagesgangsatzMitWerten(Tagesgangsatz s)
        {
            var d = new ZapfprofilTagesgangsatzDaten
            {
                Id = s.Id,
                Name = string.IsNullOrEmpty(s.Katalogversion) ? s.Bezeichner ?? "" : (s.Bezeichner ?? "") + TRENNER + s.Katalogversion,
                Status = Status(s.Status),
                Waehlbar = s.Vollstaendig,
                Sperrgrund = s.Vollstaendig ? "" : Text_("ZPG_TGE_GRUND_SATZ_UNVOLLSTAENDIG", "Der Tagesgangsatz führt nicht alle vier Tagtypen.")
            };
            for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
            {
                for (int h = 0; h < Tagesgangsatz.STUNDEN; h++)
                    d.Anteile[t][h] = s.Anteile != null ? s.Anteile[t, h] : 0.0;
                Provenienz p = s.JeTagtyp != null && t < s.JeTagtyp.Length ? s.JeTagtyp[t] : null;
                d.Herkunft[t] = p == null ? "" : Herkunft(p);
            }
            return d;
        }

        private static string Nutzungsartname(Nutzungsart n)
            => string.IsNullOrEmpty(n.Katalogversion) ? n.Name ?? "" : (n.Name ?? "") + TRENNER + n.Katalogversion;

        // =================================================================================
        // Kategorien-Editor
        // =================================================================================

        /// <summary>
        /// Der Parametersatz der Komponente <c>ZapfkategorienEditor.razor</c> zur Nutzungsart
        /// <paramref name="idNutzungsart"/>: <c>Daten</c>, <c>Texte</c>, <c>Pruefen</c> (die Regeln des
        /// Kerns, ohne Datenbank), <c>Speichern</c>, <c>HilfeSchluessel</c>.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KategorienGaben(int idNutzungsart)
            => new Dictionary<string, object>
            {
                ["Daten"] = KategorienLaden(idNutzungsart),
                ["Texte"] = Texte(),
                ["Pruefen"] = new Func<IReadOnlyList<ZapfprofilKategorieDaten>, ZapfprofilKategorienPruefungDaten>(KategorienPruefen),
                ["Speichern"] = new Func<IReadOnlyList<ZapfprofilKategorieDaten>, string, ZapfprofilKategorienErgebnis>(
                    (k, version) => KategorienSpeichern(idNutzungsart, k, version)),
                ["HilfeSchluessel"] = HILFE_KATEGORIEN
            };

        /// <summary>
        /// Der Stand des Kategorien-Editors: der Stand der Hülle (<see cref="Kategorien"/>), die
        /// Nutzungsart als Kurztext und — ist sie gesperrt — der Vorschlag einer freien
        /// Katalogversion. Ohne Tabellen oder Zeile: nicht verfügbar mit Grund.
        /// </summary>
        internal static ZapfprofilKategorienEditorDaten KategorienLaden(int idNutzungsart)
        {
            ZapfprofilKategorienDaten stand = Kategorien(idNutzungsart);
            var d = new ZapfprofilKategorienEditorDaten { Stand = stand };
            Nutzungsart n = TwwNutzungsartCtrl.Lies(idNutzungsart);
            if (n != null) d.Nutzungsart = Nutzungsartname(n);

            TwwKategorienStand kern = TwwNutzungsartCtrl.KategorienLesen(idNutzungsart);
            if (kern.Sperre == TwwKatalogAusgang.TabellenFehlen || kern.Sperre == TwwKatalogAusgang.NichtGefunden)
            {
                d.Verfuegbar = false;
                d.Grund = stand.Sperrgrund;
                return d;
            }
            d.KatalogversionVorschlag = stand.Frei ? "" : TwwNutzungsartCtrl.FreieKopieversion(idNutzungsart);
            return d;
        }

        /// <summary>
        /// Die Prüfung der Kategorien des Editors — die Regeln des Kerns
        /// (<see cref="Zapfkategoriensatz.Pruefen"/>, <see cref="Zapfkategoriensatz.Summenhinweis"/>),
        /// ohne Datenbank: die Summe der Anteile, der Hinweis bei Σ ≠ 1 und der erste Verstoß als
        /// benannte Ablehnung. Ein leeres Feld ist ein Verstoß, kein stiller Ersatzwert.
        /// </summary>
        internal static ZapfprofilKategorienPruefungDaten KategorienPruefen(IReadOnlyList<ZapfprofilKategorieDaten> kategorien)
        {
            List<Zapfkategorie> kern = AlsKernkategorien(0, kategorien);
            ZapfSatz verstoss = Zapfkategoriensatz.Pruefen(kern);
            var d = new ZapfprofilKategorienPruefungDaten
            {
                SummeAnteil = Zapfkategoriensatz.SummeAnteil(kern),
                Hinweis = Satztext(Zapfkategoriensatz.Summenhinweis(kern))
            };
            if (verstoss != null)
                d.Ablehnung = new ZapfprofilMeldung(verstoss.Schluessel, "", Satztext(verstoss), ZapfprofilMeldungsart.Ablehnung,
                                                    verstoss.Klartext ?? "");
            return d;
        }

        /// <summary>Die Kategorien des Editors als Kategorien des Kerns — ein leeres Feld wird NaN bzw. 0, nie ein Ersatzwert.</summary>
        private static List<Zapfkategorie> AlsKernkategorien(int idNutzungsart, IReadOnlyList<ZapfprofilKategorieDaten> kategorien)
            => (kategorien ?? new ZapfprofilKategorieDaten[0])
                .Select(k => new Zapfkategorie(idNutzungsart, (k?.Name ?? "").Trim(), k?.VolumenstromLJeMin ?? double.NaN,
                                               k?.StreuungLJeMin ?? double.NaN, k?.DauerMin ?? 0, k?.Anteil ?? double.NaN, null)
                {
                    KappungLJeMin = k?.KappungLJeMin
                }).ToList();

        // =================================================================================
        // Katalog nach einem Schreibweg, Ladeleistungs-Vorschlag
        // =================================================================================

        /// <summary>Der Katalog nach einem Schreibweg der Editoren: Nutzungsarten und Tagesgangsätze, wie der Dialog sie zeigt.</summary>
        internal static ZapfprofilKatalogstandDaten Katalogstand()
            => new ZapfprofilKatalogstandDaten
            {
                Katalog = Katalog(),
                Tagesgangsaetze = ZapfprofilCtrl.Tagesgangsaetze().Select(AlsTagesgangsatz).ToList()
            };

        /// <summary>
        /// <b>Der Vorschlag der Ladeleistung</b> für die Schätzhilfe des Zapfprofil-Dialogs (5.3, 4.7):
        /// die Schätzhilfe der ersten Speichergruppe aus dem Verfahrensvergleich der deterministischen
        /// Auslegung zum Arbeitsstand — derselbe Weg wie die Überlagerung „Auslegung", ohne
        /// Ensemble. <c>null</c> ohne Zone, ohne Speichergruppe oder wenn die Auslegung nicht rechnet.
        /// </summary>
        internal static ZapfprofilSchaetzhilfeDaten Ladevorschlag(int idProjekt, ZapfprofilEingabeDaten eingabe, ZapfprofilStand basis)
        {
            if (eingabe == null || eingabe.Zonen.Count == 0) return null;
            ZapfprofilAuslegungEingabeDaten a = eingabe.Auslegung?.Kopie() ?? AuslegungAusStand(basis);
            eingabe.Gebaeude?.InAuslegung(a);
            a.Stochastisch = false;
            ZapfprofilAuslegungDaten d = Auslegung(idProjekt, eingabe, a, basis, eingabe.Stufe);
            if (d.Zustand != ZapfprofilAuslegungZustand.Gerechnet) return null;
            return d.Gruppen.FirstOrDefault(g => g.Speicher && g.Vergleich?.Ladeleistung != null)?.Vergleich.Ladeleistung;
        }
    }
}
