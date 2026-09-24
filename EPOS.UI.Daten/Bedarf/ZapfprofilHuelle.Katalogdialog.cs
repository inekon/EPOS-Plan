using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Katalogdialogs „Brauchwasser-Nutzungsarten"</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.4, 5.5 <c>KatalogGaben()</c>; Stufe Z4, Gruppe 3): baut aus
    /// <see cref="TwwNutzungsartCtrl"/> die DTO des <c>TwwNutzungsartAdminDialog</c> — Katalogliste,
    /// Stammblatt der gewählten Nutzungsart samt Vorschau, Editor für „Neu…", „Ändern…" und
    /// „Speichern unter…", Löschen, die Editoren „Tagesgang…" und „Kategorien…" (dieselben Wege wie
    /// im Zapfprofil-Dialog) und den Katalogimport.
    ///
    /// <para><b>Katalog ohne Arbeitsstand</b> (5.4): Jede Aktion schreibt sofort in EINER
    /// Transaktion des Kerns; der Dialog liest danach neu. Jede Ablehnung kommt benannt zurück —
    /// der Grund in der Oberflächensprache (<c>ZPGK_GRUND_…</c> je Ausgang des Kerns), die Sätze
    /// des Imports als <see cref="ZapfSatz"/> (<c>ZPG_SATZ_KATALOGIMPORT_…</c>).</para>
    ///
    /// <para><b>Plattformfrei.</b> Die Dateiwahl des Imports kommt über
    /// <see cref="Dienste.Datei"/> (wartbarer Zwilling, HINTER dem Blazor-Ereignis); die Windows-Schale
    /// zeigt die Komponente in einem Fenster (<c>TwwNutzungsartAdminHuelle</c>), auf iOS bleibt der
    /// Katalog geschlossen (5.5).</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Der Hilfeschlüssel des Katalogdialogs (5.8).</summary>
        internal const string HILFE_KATALOG = "Form_Brauchwasser_Nutzungsarten.btn_Help";

        /// <summary>Der Hilfeschlüssel des Editors der Nutzungsart (Anker der Bedienseite).</summary>
        internal const string HILFE_KATALOG_EDITOR = "Form_Brauchwasser_Nutzungsarten.grp_Editor";

        /// <summary>Der Hilfeschlüssel des Katalogimports (Anker der Bedienseite).</summary>
        internal const string HILFE_KATALOG_IMPORT = "Form_Brauchwasser_Nutzungsarten.grp_Import";

        /// <summary>Die Einstellung, die den Ordner der letzten Paketwahl hält.</summary>
        internal const string EINSTELLUNG_KATALOGORDNER = "Zapfprofil.Katalogordner";

        /// <summary>
        /// Der Parametersatz der Komponente <c>TwwNutzungsartAdminDialog.razor</c> — ohne
        /// <c>Geschlossen</c>, damit ihn auch ein Wirt ohne Fenster nehmen kann.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben()
            => new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(KatalogZeilen),
                ["Katalogprofil"] = Katalogfilterprofil.FuerTwwNutzungsart(Katalogtexte.Fuer),
                ["Detail"] = new Func<int, TwwNutzungsartDetailDaten>(KatalogDetail),
                ["EditorLaden"] = new Func<int, TwwEditorModus, TwwNutzungsartEditorDaten>(EditorLaden),
                ["Speichern"] = new Func<TwwEditorModus, TwwNutzungsartEntwurfDaten, TwwNutzungsartSpeicherErgebnis>(EntwurfSpeichern),
                ["Loeschen"] = new Func<int, TwwNutzungsartSpeicherErgebnis>(KatalogLoeschen),
                ["TagesgangGaben"] = new Func<int, IReadOnlyDictionary<string, object>>(id => TagesgangGaben(id, null)),
                ["KategorienGaben"] = new Func<int, IReadOnlyDictionary<string, object>>(KategorienGaben),
                ["PaketWaehlen"] = new Func<string, Task<string>>(PaketWaehlen),
                ["Importieren"] = new Func<string, TwwImportberichtDaten>(KatalogImportieren),
                ["Texte"] = KatalogTexte(),
                ["HilfeSchluessel"] = HILFE_KATALOG
            };

        // =================================================================================
        // Liste und Stammblatt
        // =================================================================================

        /// <summary>Die Zeilen der Katalogliste (EINE Abfrage); die Wertwörter in der Oberflächensprache.</summary>
        internal static IReadOnlyList<Katalogfilterzeile> KatalogZeilen()
            => TwwNutzungsartCtrl.Katalogfilterzeilen(Katalogwort);

        /// <summary>
        /// Übersetzt die Wertwörter der Katalogliste: Der Kern fragt <c>ZPG_BEZUGSART_</c>,
        /// <c>ZPG_KALENDER_</c>, <c>ZPG_HERKUNFT_</c> bzw. <c>ZPG_STATUS_</c> plus den Namen des
        /// Aufzählungswerts; die Oberfläche führt sie als <c>ZPG_BEZUG_…</c> und die übrigen in
        /// Großschreibung. <c>null</c> = der Name selbst.
        /// </summary>
        internal static string Katalogwort(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            foreach ((string frage, string antwort) in new[]
                     {
                         ("ZPG_BEZUGSART_", "ZPG_BEZUG_"), ("ZPG_KALENDER_", "ZPG_KALENDER_"),
                         ("ZPG_HERKUNFT_", "ZPG_HERKUNFT_"), ("ZPG_STATUS_", "ZPG_STATUS_")
                     })
                if (schluessel.StartsWith(frage, StringComparison.Ordinal))
                    return Text_(antwort + Gross(schluessel.Substring(frage.Length)), null);
            return null;
        }

        /// <summary>
        /// <b>Das Stammblatt einer Nutzungsart</b>: Kopf, lesende Gruppen, Vorschau und die
        /// Sperrgründe. <c>null</c>, wenn es die Zeile (oder die Tabellen) nicht gibt.
        /// </summary>
        internal static TwwNutzungsartDetailDaten KatalogDetail(int id)
        {
            Nutzungsart n = TwwNutzungsartCtrl.Lies(id);
            if (n == null) return null;
            TwwNutzungsartAdminTexte t = KatalogTexte();

            bool readOnly = TwwNutzungsartCtrl.IstReadOnly(id);
            bool benutzt = TwwNutzungsartCtrl.IstBenutzt(id);
            IReadOnlyList<string> projekte = TwwNutzungsartCtrl.Projektverwendung().TryGetValue(id, out IReadOnlyList<string> p)
                ? p : Array.Empty<string>();
            string projekttext = projekte.Count > 0 ? string.Join(", ", projekte) : t.WertNirgends;

            var d = new TwwNutzungsartDetailDaten
            {
                Id = id,
                Name = n.Name ?? "",
                Katalogversion = n.Katalogversion ?? "",
                Unterzeile = string.Join(TRENNER, new[] { n.Katalogversion ?? "", Status(n.Status), Bezugsgroesse(n.Bezug) }
                                                   .Where(s => s.Length > 0)),
                Auslieferung = readOnly,
                Benutzt = benutzt,
                Projekte = projekte,
                AendernGrund = readOnly ? t.GrundAuslieferung : benutzt ? Format(t.GrundBenutzt, projekttext) : "",
                LoeschGrund = readOnly ? t.GrundLoeschenAuslieferung : benutzt ? Format(t.GrundLoeschenBenutzt, projekttext) : ""
            };

            double[] bedarf = n.BedarfJeNiveauKwhJeEinheitTag ?? new double[NutzungsartRaster.NIVEAUS];
            d.Kennzahlen = new[] { new Stammblattkennzahl(KZahl(bedarf[1]) + " " + t.EinheitBedarf, t.KennzahlBedarf) };

            string[] niveaus = { t.LabelBedarfNiedrig, t.LabelBedarfMittel, t.LabelBedarfHoch };
            var kennwerte = new List<Stammblattwert> { new(t.LabelBezugsart, Bezugsgroesse(n.Bezug)) };
            for (int i = 0; i < NutzungsartRaster.NIVEAUS; i++)
            {
                double? min = n.Herkunft?.Bandbreite?.Min is { } mi && i < mi.Length ? mi[i] : null;
                double? max = n.Herkunft?.Bandbreite?.Max is { } ma && i < ma.Length ? ma[i] : null;
                string wert = min.HasValue || max.HasValue
                    ? Format(t.WertBandbreite, KZahl(bedarf[i]), min.HasValue ? KZahl(min.Value) : "–", max.HasValue ? KZahl(max.Value) : "–")
                    : KZahl(bedarf[i]);
                kennwerte.Add(new(niveaus[i], wert, t.EinheitBedarf));
            }
            kennwerte.Add(new(t.LabelZapftemperatur, n.Bezugstemperaturen == null ? "" : KZahl(n.Bezugstemperaturen.ZapftemperaturC), t.EinheitGrad));
            kennwerte.Add(new(t.LabelKaltwasser, n.Bezugstemperaturen == null ? "" : KZahl(n.Bezugstemperaturen.KaltwasserC), t.EinheitGrad));
            kennwerte.Add(new(t.LabelBilanzgrenze, Grenzname(n.Grenze)));
            kennwerte.Add(new(t.LabelKalender, Kalendername(n.Kalender)));
            kennwerte.Add(new(t.LabelFerienfaktor, n.Ferienfaktor.HasValue ? KZahl(n.Ferienfaktor.Value) : ""));
            d.Kennwerte = kennwerte;

            var gaenge = new List<Stammblattwert> { Stammblattwert.Abschnitt(t.LabelMonatsfaktoren) };
            for (int m = 0; m < NutzungsartRaster.MONATE; m++)
                gaenge.Add(new(Monatsname(m + 1), n.Monatsfaktoren != null && m < n.Monatsfaktoren.Length ? KZahl(n.Monatsfaktoren[m]) : ""));
            gaenge.Add(Stammblattwert.Abschnitt(t.LabelWochenfaktoren));
            for (int w = 0; w < NutzungsartRaster.WOCHENTAGE; w++)
                gaenge.Add(new(Wochentagsname(w), n.Wochenfaktoren != null && w < n.Wochenfaktoren.Length ? KZahl(n.Wochenfaktoren[w]) : ""));
            d.Gaenge = gaenge;

            Tagesgangsatz satz = n.Tagesgaenge;
            d.Tagesgang = new[]
            {
                new Stammblattwert(t.LabelTagesgangsatz, satz == null ? "" : Satzname(satz)),
                new Stammblattwert(t.LabelStatus, satz == null ? "" : satz.Vollstaendig ? t.WertSatzVollstaendig : t.WertSatzUnvollstaendig)
            };

            TwwKategorienStand kat = TwwNutzungsartCtrl.KategorienLesen(id);
            var kategorien = new List<Stammblattwert>();
            foreach (Zapfkategorie k in kat.Kategorien)
            {
                string wert = Format(t.WertKategorie, KZahl(k.VolumenstromLJeMin), KZahl(k.StreuungLJeMin),
                                     k.DauerMin.ToString(CultureInfo.CurrentCulture), KZahl(k.Anteil));
                if (k.KappungLJeMin.HasValue) wert = Format(t.WertKappung, wert, KZahl(k.KappungLJeMin.Value));
                kategorien.Add(new(k.Name ?? "", wert));
            }
            if (kategorien.Count == 0) kategorien.Add(new(t.GruppeKategorien, t.WertKeineKategorien));
            else if (kat.Hinweis != null) kategorien.Add(new("", Satztext(kat.Hinweis)));
            d.Kategorien = kategorien;

            var herkunft = new List<Stammblattwert>
            {
                new(t.LabelStatus, Status(n.Status)),
                new(t.LabelKatalogversion, n.Katalogversion ?? ""),
                new(t.LabelQuelleBedarf, Herkunft(n.Herkunft?.Bedarf)),
                new(t.LabelQuelleJahresgang, Herkunft(n.Herkunft?.Jahresgang)),
                new(t.LabelQuelleWochengang, Herkunft(n.Herkunft?.Wochengang))
            };
            string[] tagtypen = { Text_("ZPG_TAGTYP_WERKTAG", "Werktag"), Text_("ZPG_TAGTYP_SAMSTAG", "Samstag"),
                                  Text_("ZPG_TAGTYP_SONNTAG", "Sonn-/Feiertag"), Text_("ZPG_TAGTYP_RUHETAG", "Ruhetag") };
            for (int tt = 0; tt < Tagesgangsatz.TAGTYPEN; tt++)
                if (satz?.JeTagtyp != null && tt < satz.JeTagtyp.Length && satz.JeTagtyp[tt] != null)
                    herkunft.Add(new(Format(t.LabelQuelleTagesgang, tagtypen[tt]), Herkunft(satz.JeTagtyp[tt])));
            herkunft.Add(new(t.LabelVorlage, n.IdVorlage.HasValue ? Vorlagenname(n.IdVorlage.Value, t) : t.WertOhneVorlage));
            herkunft.Add(new(t.LabelVerwendet, projekttext));
            d.Herkunft = herkunft;

            TwwNutzungsartVorschau v = TwwNutzungsartCtrl.Vorschau(n);
            if (v == null) d.VorschauGrund = t.KeinBild;
            else
            {
                ZapfprofilBildtexte bild = Bildtexte();
                bild.TitelTagesgang = t.BildTagesgang;
                bild.TitelJahresgang = t.BildJahresgang;
                d.TagesgangModell = ZapfprofilBilder.TagesgangModell("", v.WerktagKw, v.SamstagKw, v.SonnFeiertagKw, null, bild);
                d.JahresgangModell = ZapfprofilBilder.JahresgangModell(v.MonateKwh, null, Energieeinheit.KWh.Text, bild);
            }
            return d;
        }

        private static string Satzname(Tagesgangsatz s)
            => string.IsNullOrEmpty(s.Katalogversion) ? s.Bezeichner ?? "" : (s.Bezeichner ?? "") + TRENNER + s.Katalogversion;

        private static string Vorlagenname(int id, TwwNutzungsartAdminTexte t)
        {
            Nutzungsart v = TwwNutzungsartCtrl.Lies(id);
            return v == null ? "#" + id.ToString(CultureInfo.InvariantCulture) : Nutzungsartname(v);
        }

        /// <summary>Die Bilanzgrenze als Text — Schlüssel <c>ZPG_GRENZE_…</c> wie im Zapfprofil-Dialog.</summary>
        internal static string Grenzname(ZapfBilanzgrenze g)
        {
            switch (g)
            {
                case ZapfBilanzgrenze.Zapfstelle: return Text_("ZPG_GRENZE_ZAPFSTELLE", "an der Zapfstelle");
                case ZapfBilanzgrenze.MitVerteilung: return Text_("ZPG_GRENZE_VERTEILUNG", "mit Verteil- und Zirkulationsverlust");
                case ZapfBilanzgrenze.MitSpeicher: return Text_("ZPG_GRENZE_SPEICHER", "zusätzlich mit Speicherverlust");
                default: return g.ToString();
            }
        }

        /// <summary>Eine Zahl in der Oberflächenkultur, höchstens drei Nachkommastellen, ohne Tausendertrennung.</summary>
        private static string KZahl(double w)
            => double.IsNaN(w) || double.IsInfinity(w) ? "" : w.ToString("0.###", CultureInfo.CurrentCulture);

        // =================================================================================
        // Editor: Neu, Ändern, Speichern unter
        // =================================================================================

        /// <summary>
        /// <b>Der Stand des Editors</b> zur Zeile <paramref name="id"/> (bei „Neu" die Vorlage der
        /// Werte; 0 = ohne Vorlage) im Modus <paramref name="modus"/>. „Ändern" an einer gesperrten
        /// Zeile (Auslieferung, benutzt) wird „Speichern unter" mit dem Sperrgrund als Hinweis und
        /// einer freien Katalogversion als Vorschlag.
        /// </summary>
        internal static TwwNutzungsartEditorDaten EditorLaden(int id, TwwEditorModus modus)
        {
            TwwNutzungsartAdminTexte t = KatalogTexte();
            var d = new TwwNutzungsartEditorDaten
            {
                Modus = modus,
                Bezugsarten = Enum.GetValues(typeof(ZapfBezugsart)).Cast<ZapfBezugsart>().Select(b => ((int)b, Bezugsgroesse(b))).ToList(),
                Bilanzgrenzen = Enum.GetValues(typeof(ZapfBilanzgrenze)).Cast<ZapfBilanzgrenze>().Select(g => ((int)g, Grenzname(g))).ToList(),
                Kalenderarten = Enum.GetValues(typeof(ZapfKalenderart)).Cast<ZapfKalenderart>().Select(k => ((int)k, Kalendername(k))).ToList(),
                Tagesgangsaetze = ZapfprofilCtrl.Tagesgangsaetze().Where(s => s.Vollstaendig).Select(s => (s.Id, Satzname(s))).ToList(),
                Monatsnamen = Enumerable.Range(1, NutzungsartRaster.MONATE).Select(Monatsname).ToList()
            };

            Nutzungsart n = id > 0 ? TwwNutzungsartCtrl.Lies(id) : null;
            if (id > 0 && n == null)
            {
                d.Verfuegbar = false;
                d.Grund = Text_(KatalogSchluessel(TwwKatalogAusgang.NichtGefunden), "");
                return d;
            }

            if (modus == TwwEditorModus.Aendern && n != null
                && (TwwNutzungsartCtrl.IstReadOnly(id) || TwwNutzungsartCtrl.IstBenutzt(id)))
            {
                d.Modus = TwwEditorModus.SpeichernUnter;
                d.Hinweis = KatalogDetail(id)?.AendernGrund ?? "";
            }
            else
                d.Hinweis = modus switch
                {
                    TwwEditorModus.Neu => t.EditorHinweisNeu,
                    TwwEditorModus.Aendern => t.EditorHinweisAendern,
                    _ => t.EditorHinweisSpeichernUnter
                };

            d.Entwurf = n == null ? new TwwNutzungsartEntwurfDaten
            {
                Bezugsart = (int)ZapfBezugsart.Personen,
                Bilanzgrenze = (int)ZapfBilanzgrenze.Zapfstelle,
                Kalenderart = (int)ZapfKalenderart.Wohnen,
                Monatsfaktoren = Enumerable.Repeat((double?)1.0, NutzungsartRaster.MONATE).ToArray(),
                IdTagesgangsatz = d.Tagesgangsaetze.Count > 0 ? d.Tagesgangsaetze[0].Id : 0
            } : AlsEntwurf(n);
            d.Entwurf.IdBezug = n?.Id ?? 0;
            if (d.Modus == TwwEditorModus.Neu) d.Entwurf.Bezeichner = "";
            if (d.Modus == TwwEditorModus.SpeichernUnter && n != null)
                d.Entwurf.Katalogversion = TwwNutzungsartCtrl.FreieKopieversion(n.Id);

            double[] woche = n?.Wochenfaktoren ?? GleicheWoche();
            d.Wochenfaktoren = string.Join(TRENNER, Enumerable.Range(0, NutzungsartRaster.WOCHENTAGE)
                .Select(w => Wochentagsname(w) + " " + (woche[w] * 100.0).ToString("0.#", CultureInfo.CurrentCulture) + " %"));
            return d;
        }

        private static double[] GleicheWoche() => Enumerable.Repeat(1.0 / NutzungsartRaster.WOCHENTAGE, NutzungsartRaster.WOCHENTAGE).ToArray();

        /// <summary>Die Werte einer Katalogzeile als Eingaben des Editors.</summary>
        private static TwwNutzungsartEntwurfDaten AlsEntwurf(Nutzungsart n) => new TwwNutzungsartEntwurfDaten
        {
            Bezeichner = n.Name ?? "",
            Katalogversion = n.Katalogversion ?? "",
            Bezugsart = (int)n.Bezug,
            Bedarf = (n.BedarfJeNiveauKwhJeEinheitTag ?? new double[NutzungsartRaster.NIVEAUS]).Select(w => (double?)w).ToArray(),
            BedarfMin = (double?[])(n.Herkunft?.Bandbreite?.Min ?? new double?[NutzungsartRaster.NIVEAUS]).Clone(),
            BedarfMax = (double?[])(n.Herkunft?.Bandbreite?.Max ?? new double?[NutzungsartRaster.NIVEAUS]).Clone(),
            Zapftemperatur = n.Bezugstemperaturen?.ZapftemperaturC,
            Kaltwasser = n.Bezugstemperaturen?.KaltwasserC,
            Bilanzgrenze = (int)n.Grenze,
            Kalenderart = (int)n.Kalender,
            Ferienfaktor = n.Ferienfaktor,
            Monatsfaktoren = (n.Monatsfaktoren ?? new double[NutzungsartRaster.MONATE]).Select(w => (double?)w).ToArray(),
            IdTagesgangsatz = n.Tagesgaenge?.Id ?? 0
        };

        /// <summary>
        /// Die fehlenden Pflichtangaben des Entwurfs — die Beschriftungen der leeren Felder; leer =
        /// vollständig. Dieselbe Prüfung im Editor (je Eingabe, ohne Datenbank) und vor dem Schreiben.
        /// </summary>
        internal static IReadOnlyList<string> EntwurfLuecken(TwwNutzungsartEntwurfDaten e, TwwNutzungsartAdminTexte t)
        {
            var luecken = new List<string>();
            if (e == null) return new[] { t.LabelBezeichner };
            if (string.IsNullOrWhiteSpace(e.Bezeichner)) luecken.Add(t.LabelBezeichner);
            if (string.IsNullOrWhiteSpace(e.Katalogversion)) luecken.Add(t.LabelKatalogversion);
            if (!Enum.IsDefined(typeof(ZapfBezugsart), e.Bezugsart)) luecken.Add(t.LabelBezugsart);
            string[] niveaus = { t.LabelBedarfNiedrig, t.LabelBedarfMittel, t.LabelBedarfHoch };
            for (int i = 0; i < NutzungsartRaster.NIVEAUS; i++)
                if (e.Bedarf == null || i >= e.Bedarf.Length || !e.Bedarf[i].HasValue) luecken.Add(niveaus[i]);
            if (!e.Zapftemperatur.HasValue) luecken.Add(t.LabelZapftemperatur);
            if (!e.Kaltwasser.HasValue) luecken.Add(t.LabelKaltwasser);
            if (!Enum.IsDefined(typeof(ZapfBilanzgrenze), e.Bilanzgrenze)) luecken.Add(t.LabelBilanzgrenze);
            if (!Enum.IsDefined(typeof(ZapfKalenderart), e.Kalenderart)) luecken.Add(t.LabelKalender);
            if (e.Monatsfaktoren == null || e.Monatsfaktoren.Length != NutzungsartRaster.MONATE || e.Monatsfaktoren.Any(m => !m.HasValue))
                luecken.Add(t.LabelMonatsfaktoren);
            if (e.IdTagesgangsatz <= 0) luecken.Add(t.LabelTagesgangsatz);
            return luecken;
        }

        /// <summary>
        /// <b>„Speichern" des Editors</b> in EINER Transaktion des Kerns: „Neu" legt eine eigene Zeile
        /// an (Eigenkonstruktion, neutrale Quelle), „Ändern" schreibt an Ort und Stelle (die Provenienz
        /// je Wertgruppe führt der Kern nach), „Speichern unter" legt eine neue Zeile mit der Vorlage
        /// an. Die Monatsfaktoren normiert der Kern auf das Mittel 1; die Wochenfaktoren kommen aus
        /// der Bezugszeile. Jede Ablehnung ist benannt.
        /// </summary>
        internal static TwwNutzungsartSpeicherErgebnis EntwurfSpeichern(TwwEditorModus modus, TwwNutzungsartEntwurfDaten e)
        {
            TwwNutzungsartAdminTexte t = KatalogTexte();
            IReadOnlyList<string> luecken = EntwurfLuecken(e, t);
            if (luecken.Count > 0)
                return Abgelehnt(t, e?.IdBezug ?? 0, TwwKatalogAusgang.EntwurfUnvollstaendig, Format(t.EditorPflicht, string.Join(", ", luecken)));

            double[] monate = TwwNutzungsartCtrl.AnteileNormiert(e.Monatsfaktoren.Select(m => m.Value).ToArray());
            if (monate == null) return Abgelehnt(t, e.IdBezug, TwwKatalogAusgang.RasterUngueltig, null);
            for (int m = 0; m < monate.Length; m++) monate[m] *= NutzungsartRaster.MONATE;

            Nutzungsart bezug = e.IdBezug > 0 ? TwwNutzungsartCtrl.Lies(e.IdBezug) : null;
            if (modus != TwwEditorModus.Neu && bezug == null)
                return Abgelehnt(t, e.IdBezug, TwwKatalogAusgang.NichtGefunden, null);

            string version = e.Katalogversion.Trim();
            TwwNutzungsartEntwurf basis = bezug != null && modus != TwwEditorModus.Neu
                ? TwwNutzungsartEntwurf.Aus(bezug)
                : new TwwNutzungsartEntwurf
                {
                    Wochenfaktoren = (double[])(bezug?.Wochenfaktoren ?? GleicheWoche()).Clone(),
                    BedarfHerkunft = Eigen(version),
                    JahresgangHerkunft = Eigen(version),
                    WochengangHerkunft = Eigen(version)
                };
            TwwNutzungsartEntwurf entwurf = basis with
            {
                Bezeichner = e.Bezeichner.Trim(),
                Katalogversion = version,
                Bezug = (ZapfBezugsart)e.Bezugsart,
                Bedarf = e.Bedarf.Select(b => b.Value).ToArray(),
                BedarfMin = (double?[])e.BedarfMin.Clone(),
                BedarfMax = (double?[])e.BedarfMax.Clone(),
                Bezugstemperaturen = new Temperaturbezug(e.Zapftemperatur.Value, e.Kaltwasser.Value),
                Grenze = (ZapfBilanzgrenze)e.Bilanzgrenze,
                Kalender = (ZapfKalenderart)e.Kalenderart,
                Ferienfaktor = e.Ferienfaktor,
                Monatsfaktoren = monate,
                IdTagesgangsatz = e.IdTagesgangsatz
            };

            TwwKatalogErgebnis erg = modus switch
            {
                TwwEditorModus.Aendern => TwwNutzungsartCtrl.Aendern(e.IdBezug, entwurf),
                TwwEditorModus.SpeichernUnter => TwwNutzungsartCtrl.SpeichernUnter(e.IdBezug, entwurf),
                _ => TwwNutzungsartCtrl.Neu(entwurf)
            };
            if (!erg.Ok) return Abgelehnt(t, e.IdBezug, erg.Ausgang, null);
            int id = modus == TwwEditorModus.Aendern ? e.IdBezug : erg.Id;
            return new TwwNutzungsartSpeicherErgebnis(true, id, Format(t.MeldungGespeichert, entwurf.Bezeichner + TRENNER + version), "");
        }

        private static Provenienz Eigen(string version)
            => new Provenienz(TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION, null, version, WindowsFormsApplication1.Herkunftsart.Eigenkonstruktion);

        /// <summary>„Löschen" — der Kern sperrt Auslieferung und benutzte Zeilen benannt.</summary>
        internal static TwwNutzungsartSpeicherErgebnis KatalogLoeschen(int id)
        {
            TwwNutzungsartAdminTexte t = KatalogTexte();
            Nutzungsart n = TwwNutzungsartCtrl.Lies(id);
            TwwKatalogErgebnis erg = TwwNutzungsartCtrl.Loeschen(id);
            if (!erg.Ok)
            {
                string grund = Text_(KatalogSchluessel(erg.Ausgang), erg.Ausgang.ToString());
                return new TwwNutzungsartSpeicherErgebnis(false, id, Format(t.EditorNichtGeloescht, grund), KatalogSchluessel(erg.Ausgang));
            }
            return new TwwNutzungsartSpeicherErgebnis(true, id, Format(t.MeldungGeloescht, n == null ? "" : Nutzungsartname(n)), "");
        }

        /// <summary>Der Ressourcenschlüssel des Grundes einer Pflegeaktion am Katalog: <c>ZPGK_GRUND_…</c>.</summary>
        internal static string KatalogSchluessel(TwwKatalogAusgang a) => "ZPGK_GRUND_" + Gross(a.ToString());

        private static TwwNutzungsartSpeicherErgebnis Abgelehnt(TwwNutzungsartAdminTexte t, int id, TwwKatalogAusgang a, string grund)
        {
            string text = grund ?? Text_(KatalogSchluessel(a), a.ToString());
            return new TwwNutzungsartSpeicherErgebnis(false, id, Format(t.EditorNichtGespeichert, text), KatalogSchluessel(a));
        }

        // =================================================================================
        // Katalogimport
        // =================================================================================

        /// <summary>
        /// Die Paketwahl über <see cref="Dienste.Datei"/> — HINTER dem Blazor-Ereignis (wartbarer
        /// Zwilling), Startordner aus der Einstellung der letzten Wahl. <c>""</c> = abgebrochen.
        /// </summary>
        internal static Task<string> PaketWaehlen(string filter)
        {
            TwwNutzungsartAdminTexte t = KatalogTexte();
            string ordner = "";
            try { ordner = Dienste.Einstellungen?.Lies(EINSTELLUNG_KATALOGORDNER, "") ?? ""; } catch { }
            return Dienste.Datei.DateiOeffnenAsync(t.ImportTitel, string.IsNullOrEmpty(filter) ? t.ImportDateifilter : filter, ordner);
        }

        /// <summary>
        /// <b>Der Katalogimport</b>: Paket lesen (ZIP, Ordner, eine Datei des Paketordners), im Kern
        /// einspielen (<see cref="TwwNutzungsartCtrl.Importieren"/>) und den Bericht in der
        /// Oberflächensprache zurückgeben; der Ordner wird für die nächste Wahl gemerkt.
        /// </summary>
        internal static TwwImportberichtDaten KatalogImportieren(string pfad)
        {
            TwwNutzungsartAdminTexte t = KatalogTexte();
            var d = new TwwImportberichtDaten();
            if (string.IsNullOrWhiteSpace(pfad))
            {
                d.Abgebrochen = true;
                d.Abbruch = t.ImportKeinPaket;
                return d;
            }

            IReadOnlyList<TwwPaketdatei> dateien = TwwNutzungsartCtrl.PaketLesen(pfad, out ZapfSatz fehler);
            if (fehler != null)
            {
                d.Abgebrochen = true;
                d.Abbruch = Format(t.ImportAbbruch, Satztext(fehler));
                return d;
            }
            try
            {
                string ordner = System.IO.Directory.Exists(pfad) ? pfad : System.IO.Path.GetDirectoryName(pfad) ?? "";
                Dienste.Einstellungen?.Schreib(EINSTELLUNG_KATALOGORDNER, ordner);
            }
            catch { /* ein nicht gemerkter Ordner ist kein Importfehler */ }

            TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(dateien);
            return AlsBericht(b, t);
        }

        /// <summary>Der Bericht des Kerns in der Oberflächensprache.</summary>
        internal static TwwImportberichtDaten AlsBericht(TwwKatalogimportBericht b, TwwNutzungsartAdminTexte t)
        {
            var d = new TwwImportberichtDaten();
            d.Hinweise.AddRange(b.Hinweise.Select(Satztext));
            if (b.Abbruch != null)
            {
                d.Abgebrochen = true;
                d.Abbruch = Format(t.ImportAbbruch, Satztext(b.Abbruch));
                return d;
            }
            foreach (TwwImportzeile z in b.Zeilen)
            {
                string name = z.Nutzungsart.Length == 0 ? Format(t.ImportZeile, z.Zeile.ToString(CultureInfo.CurrentCulture))
                            : z.Katalogversion.Length == 0 ? z.Nutzungsart : z.Nutzungsart + TRENNER + z.Katalogversion;
                (TwwImportausgangDaten a, string text) = z.Ausgang switch
                {
                    TwwImportausgang.Angelegt => (TwwImportausgangDaten.Angelegt, t.ImportAngelegt),
                    TwwImportausgang.Uebersprungen => (TwwImportausgangDaten.Uebersprungen, t.ImportUebersprungen),
                    _ => (TwwImportausgangDaten.Abgelehnt, t.ImportAbgelehnt)
                };
                d.Zeilen.Add(new TwwImportzeileDaten(name, a, text, Satztext(z.Grund), z.IdNeu));
            }
            d.Zusammenfassung = Format(t.ImportZusammenfassung, b.Angelegt, b.Uebersprungen, b.Abgelehnt);
            return d;
        }

        // =================================================================================
        // Texte
        // =================================================================================

        /// <summary>Das Textbündel des Katalogdialogs in der Oberflächensprache (<c>ZPGK_…</c>); Rückfall der deutsche Vorgabewert.</summary>
        internal static TwwNutzungsartAdminTexte KatalogTexte()
        {
            var t = new TwwNutzungsartAdminTexte();
            t.Titel = Text_("ZPGK_TITEL", t.Titel);
            t.LeerKatalog = Text_("ZPGK_LEER", t.LeerKatalog);
            t.KennzahlBedarf = Text_("ZPGK_KZ_BEDARF_MITTEL", t.KennzahlBedarf);
            t.EinheitBedarf = Text_("ZPGK_EINHEIT_BEDARF", t.EinheitBedarf);
            t.EinheitGrad = Text_("ZPGK_EINHEIT_GRAD", t.EinheitGrad);
            t.GruppeKennwerte = Text_("ZPGK_GRP_KENNWERTE", t.GruppeKennwerte);
            t.GruppeGaenge = Text_("ZPGK_GRP_GAENGE", t.GruppeGaenge);
            t.GruppeTagesgang = Text_("ZPGK_GRP_TAGESGANG", t.GruppeTagesgang);
            t.GruppeKategorien = Text_("ZPGK_GRP_KATEGORIEN", t.GruppeKategorien);
            t.GruppeHerkunft = Text_("ZPGK_GRP_HERKUNFT", t.GruppeHerkunft);
            t.LabelBezeichner = Text_("ZPGK_LBL_BEZEICHNER", t.LabelBezeichner);
            t.LabelKatalogversion = Text_("ZPGK_LBL_KATALOGVERSION", t.LabelKatalogversion);
            t.LabelStatus = Text_("ZPGK_LBL_STATUS", t.LabelStatus);
            t.LabelBezugsart = Text_("ZPGK_LBL_BEZUGSART", t.LabelBezugsart);
            t.LabelBedarfNiedrig = Text_("ZPGK_LBL_BEDARF_NIEDRIG", t.LabelBedarfNiedrig);
            t.LabelBedarfMittel = Text_("ZPGK_LBL_BEDARF_MITTEL", t.LabelBedarfMittel);
            t.LabelBedarfHoch = Text_("ZPGK_LBL_BEDARF_HOCH", t.LabelBedarfHoch);
            t.LabelUntereGrenze = Text_("ZPGK_LBL_UNTERE_GRENZE", t.LabelUntereGrenze);
            t.LabelObereGrenze = Text_("ZPGK_LBL_OBERE_GRENZE", t.LabelObereGrenze);
            t.LabelZapftemperatur = Text_("ZPGK_LBL_ZAPFTEMPERATUR", t.LabelZapftemperatur);
            t.LabelKaltwasser = Text_("ZPGK_LBL_KALTWASSER", t.LabelKaltwasser);
            t.LabelBilanzgrenze = Text_("ZPGK_LBL_BILANZGRENZE", t.LabelBilanzgrenze);
            t.LabelKalender = Text_("ZPGK_LBL_KALENDER", t.LabelKalender);
            t.LabelFerienfaktor = Text_("ZPGK_LBL_FERIENFAKTOR", t.LabelFerienfaktor);
            t.LabelMonatsfaktoren = Text_("ZPGK_LBL_MONATSFAKTOREN", t.LabelMonatsfaktoren);
            t.LabelWochenfaktoren = Text_("ZPGK_LBL_WOCHENFAKTOREN", t.LabelWochenfaktoren);
            t.LabelTagesgangsatz = Text_("ZPGK_LBL_TAGESGANGSATZ", t.LabelTagesgangsatz);
            t.LabelVorlage = Text_("ZPGK_LBL_VORLAGE", t.LabelVorlage);
            t.LabelVerwendet = Text_("ZPGK_LBL_VERWENDET", t.LabelVerwendet);
            t.LabelQuelleBedarf = Text_("ZPGK_LBL_QUELLE_BEDARF", t.LabelQuelleBedarf);
            t.LabelQuelleJahresgang = Text_("ZPGK_LBL_QUELLE_JAHRESGANG", t.LabelQuelleJahresgang);
            t.LabelQuelleWochengang = Text_("ZPGK_LBL_QUELLE_WOCHENGANG", t.LabelQuelleWochengang);
            t.LabelQuelleTagesgang = Text_("ZPGK_LBL_QUELLE_TAGESGANG", t.LabelQuelleTagesgang);
            t.WertBandbreite = Text_("ZPGK_WERT_BANDBREITE", t.WertBandbreite);
            t.WertKategorie = Text_("ZPGK_WERT_KATEGORIE", t.WertKategorie);
            t.WertKappung = Text_("ZPGK_WERT_KAPPUNG", t.WertKappung);
            t.WertKeineKategorien = Text_("ZPGK_WERT_KEINE_KATEGORIEN", t.WertKeineKategorien);
            t.WertSatzVollstaendig = Text_("ZPGK_WERT_SATZ_VOLLSTAENDIG", t.WertSatzVollstaendig);
            t.WertSatzUnvollstaendig = Text_("ZPGK_WERT_SATZ_UNVOLLSTAENDIG", t.WertSatzUnvollstaendig);
            t.WertNirgends = Text_("ZPGK_WERT_NIRGENDS", t.WertNirgends);
            t.WertOhneVorlage = Text_("ZPGK_WERT_OHNE_VORLAGE", t.WertOhneVorlage);
            t.KnopfAendern = Text_("ZPGK_BTN_AENDERN", t.KnopfAendern);
            t.KnopfTagesgang = Text_("ZPGK_BTN_TAGESGANG", t.KnopfTagesgang);
            t.KnopfGrafik = Text_("ZPGK_BTN_GRAFIK", t.KnopfGrafik);
            t.KnopfKategorien = Text_("ZPGK_BTN_KATEGORIEN", t.KnopfKategorien);
            t.KnopfNeu = Text_("ZPGK_BTN_NEU", t.KnopfNeu);
            t.KnopfSpeichernUnter = Text_("ZPGK_BTN_SPEICHERN_UNTER", t.KnopfSpeichernUnter);
            t.KnopfLoeschen = Text_("ZPGK_BTN_LOESCHEN", t.KnopfLoeschen);
            t.KnopfImport = Text_("ZPGK_BTN_IMPORT", t.KnopfImport);
            t.KnopfBeenden = Text_("ZPGK_BTN_BEENDEN", t.KnopfBeenden);
            t.GrundAuslieferung = Text_("ZPGK_GRUND_AUSLIEFERUNG", t.GrundAuslieferung);
            t.GrundBenutzt = Text_("ZPGK_GRUND_BENUTZT", t.GrundBenutzt);
            t.GrundLoeschenAuslieferung = Text_("ZPGK_GRUND_LOESCHEN_AUSLIEFERUNG", t.GrundLoeschenAuslieferung);
            t.GrundLoeschenBenutzt = Text_("ZPGK_GRUND_LOESCHEN_BENUTZT", t.GrundLoeschenBenutzt);
            t.GrundKeineWahl = Text_("ZPGK_GRUND_KEINE_WAHL", t.GrundKeineWahl);
            t.GrundOhneWeg = Text_("ZPGK_GRUND_OHNE_WEG", t.GrundOhneWeg);
            t.FrageLoeschen = Text_("ZPGK_FRAGE_LOESCHEN", t.FrageLoeschen);
            t.MeldungGeloescht = Text_("ZPGK_MSG_GELOESCHT", t.MeldungGeloescht);
            t.MeldungGespeichert = Text_("ZPGK_MSG_GESPEICHERT", t.MeldungGespeichert);
            t.MeldungTagesgang = Text_("ZPGK_MSG_TAGESGANG", t.MeldungTagesgang);
            t.MeldungKategorien = Text_("ZPGK_MSG_KATEGORIEN", t.MeldungKategorien);
            t.MeldungImportiert = Text_("ZPGK_MSG_IMPORTIERT", t.MeldungImportiert);
            t.GrafikTitel = Text_("ZPGK_GRAFIK_TITEL", t.GrafikTitel);
            t.GrafikHinweis = Text_("ZPGK_GRAFIK_HINWEIS", t.GrafikHinweis);
            t.KeinBild = Text_("ZPGK_KEIN_BILD", t.KeinBild);
            t.BildTagesgang = Text_("ZPGK_BILD_TAGESGANG", t.BildTagesgang);
            t.BildJahresgang = Text_("ZPGK_BILD_JAHRESGANG", t.BildJahresgang);
            t.ImportTitel = Text_("ZPGK_IMPORT_TITEL", t.ImportTitel);
            t.ImportHinweis = Text_("ZPGK_IMPORT_HINWEIS", t.ImportHinweis);
            t.ImportHerkunft = Text_("ZPGK_IMPORT_HERKUNFT", t.ImportHerkunft);
            t.ImportDatei = Text_("ZPGK_IMPORT_DATEI", t.ImportDatei);
            t.ImportDateifilter = Text_("ZPGK_IMPORT_DATEIFILTER", t.ImportDateifilter);
            t.ImportStarten = Text_("ZPGK_IMPORT_STARTEN", t.ImportStarten);
            t.ImportKeinPaket = Text_("ZPGK_IMPORT_KEIN_PAKET", t.ImportKeinPaket);
            t.ImportZusammenfassung = Text_("ZPGK_IMPORT_ZUSAMMENFASSUNG", t.ImportZusammenfassung);
            t.ImportAngelegt = Text_("ZPGK_IMPORT_ANGELEGT", t.ImportAngelegt);
            t.ImportUebersprungen = Text_("ZPGK_IMPORT_UEBERSPRUNGEN", t.ImportUebersprungen);
            t.ImportAbgelehnt = Text_("ZPGK_IMPORT_ABGELEHNT", t.ImportAbgelehnt);
            t.ImportZeile = Text_("ZPGK_IMPORT_ZEILE", t.ImportZeile);
            t.ImportSpalteNutzungsart = Text_("ZPGK_IMPORT_SP_NUTZUNGSART", t.ImportSpalteNutzungsart);
            t.ImportSpalteErgebnis = Text_("ZPGK_IMPORT_SP_ERGEBNIS", t.ImportSpalteErgebnis);
            t.ImportSpalteGrund = Text_("ZPGK_IMPORT_SP_GRUND", t.ImportSpalteGrund);
            t.ImportAbbruch = Text_("ZPGK_IMPORT_ABBRUCH", t.ImportAbbruch);
            t.ImportHinweise = Text_("ZPGK_IMPORT_HINWEISE", t.ImportHinweise);
            t.EditorTitelNeu = Text_("ZPGK_ED_TITEL_NEU", t.EditorTitelNeu);
            t.EditorTitelAendern = Text_("ZPGK_ED_TITEL_AENDERN", t.EditorTitelAendern);
            t.EditorTitelSpeichernUnter = Text_("ZPGK_ED_TITEL_SPEICHERN_UNTER", t.EditorTitelSpeichernUnter);
            t.EditorHinweisNeu = Text_("ZPGK_ED_HINWEIS_NEU", t.EditorHinweisNeu);
            t.EditorHinweisAendern = Text_("ZPGK_ED_HINWEIS_AENDERN", t.EditorHinweisAendern);
            t.EditorHinweisSpeichernUnter = Text_("ZPGK_ED_HINWEIS_SPEICHERN_UNTER", t.EditorHinweisSpeichernUnter);
            t.EditorGruppeKennung = Text_("ZPGK_ED_GRP_KENNUNG", t.EditorGruppeKennung);
            t.EditorGruppeBedarf = Text_("ZPGK_ED_GRP_BEDARF", t.EditorGruppeBedarf);
            t.EditorGruppeZeit = Text_("ZPGK_ED_GRP_ZEIT", t.EditorGruppeZeit);
            t.EditorWoche = Text_("ZPGK_ED_WOCHE", t.EditorWoche);
            t.EditorMonateMittel = Text_("ZPGK_ED_MONATE_MITTEL", t.EditorMonateMittel);
            t.EditorMonateEins = Text_("ZPGK_ED_MONATE_EINS", t.EditorMonateEins);
            t.EditorPflicht = Text_("ZPGK_ED_PFLICHT", t.EditorPflicht);
            t.EditorFehleingabe = Text_("ZPGK_ED_FEHLEINGABE", t.EditorFehleingabe);
            t.EditorKeineWahl = Text_("ZPGK_ED_KEINE_WAHL", t.EditorKeineWahl);
            t.EditorNichtGespeichert = Text_("ZPGK_ED_NICHT_GESPEICHERT", t.EditorNichtGespeichert);
            t.EditorNichtGeloescht = Text_("ZPGK_ED_NICHT_GELOESCHT", t.EditorNichtGeloescht);
            return t;
        }
    }
}
