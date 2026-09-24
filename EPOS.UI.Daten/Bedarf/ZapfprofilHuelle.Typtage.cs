using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Dialogs „VDI-4655-Typtage"</b> (Umsetzungskonzept Zapfprofilgenerator 4.2,
    /// 5.8, Kapitel 6; Stufe Z4b, Gruppe 2): baut aus <see cref="TwwTyptagCtrl"/> die DTO des
    /// <c>TwwTyptagImportDialog</c> — Stand, Paketwahl, Prüfung ohne Schreibzugriff, Einspielen und
    /// Löschen.
    ///
    /// <para><b>Kein Arbeitsstand</b>: Einspielen und Löschen schreiben sofort in EINER Transaktion
    /// des Kerns; der Dialog liest den Stand danach neu. Jede Ablehnung kommt benannt zurück — die
    /// Sätze des Kerns als <see cref="ZapfSatz"/> (<c>ZPG_SATZ_…</c>), die Beschriftungen als
    /// <c>ZPGT_…</c>.</para>
    ///
    /// <para><b>Plattformfrei</b>: Die Paketwahl kommt über <see cref="Dienste.Datei"/> (wartbarer
    /// Zwilling, HINTER dem Blazor-Ereignis), der Startordner aus der Einstellung
    /// <see cref="EINSTELLUNG_IMPORTORDNER"/> — Muster der Katalogpaketwahl.</para>
    ///
    /// <para><b>Kein Wert der Richtlinie</b> verlässt diese Hülle: Stand und Prüfbericht nennen
    /// Quelle, Ausgabe, Zonen, Gebäudearten, Typtagcodes, Zeitraster und Zeilenzahlen — nie einen
    /// Faktor oder eine Kalendertagzahl (Konzept Kapitel 6).</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Der Hilfeschlüssel des Dialogs „VDI-4655-Typtage" (5.8).</summary>
        internal const string HILFE_TYPTAGE = "Form_Brauchwasser_Typtage.btn_Help";

        /// <summary>Die Einstellung, die den Ordner der letzten Paketwahl hält.</summary>
        internal const string EINSTELLUNG_IMPORTORDNER = "Zapfprofil.Importordner";

        /// <summary>
        /// Der Parametersatz der Komponente <c>TwwTyptagImportDialog.razor</c> — ohne
        /// <c>Geschlossen</c>, damit ihn auch ein Wirt ohne Fenster nehmen kann.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> TyptagGaben()
            => new Dictionary<string, object>
            {
                ["Stand"] = new Func<TwwTyptagStandDaten>(TyptagStand),
                ["PaketWaehlen"] = new Func<string, Task<string>>(TyptagPaketWaehlen),
                ["Pruefen"] = new Func<string, TwwTyptagPruefberichtDaten>(TyptagPruefen),
                ["Einspielen"] = new Func<string, TwwTyptagErgebnisDaten>(TyptagEinspielen),
                ["Loeschen"] = new Func<TwwTyptagErgebnisDaten>(TyptagLoeschen),
                ["Texte"] = TyptagTexte(),
                ["HilfeSchluessel"] = HILFE_TYPTAGE
            };

        // =================================================================================
        // Stand
        // =================================================================================

        /// <summary>Der eingespielte Stand; ohne Tabelle oder ohne Zeile mit benanntem Grund.</summary>
        internal static TwwTyptagStandDaten TyptagStand() => AlsStandDaten(TwwTyptagCtrl.Stand());

        private static TwwTyptagStandDaten AlsStandDaten(TwwTyptagstand s)
        {
            var d = new TwwTyptagStandDaten();
            if (s == null || !s.Vorhanden)
            {
                d.Grund = TwwTyptagCtrl.TabelleVorhanden()
                    ? Text_("ZPGT_LEER", new TwwTyptagImportTexte().Leer)
                    : Satztext(ZapfSatz.Neu("TYPTAGIMPORT_TABELLE_FEHLT", TwwSchema.TAB_TWW_TYPTAG_IMPORT));
                return d;
            }
            d.Vorhanden = true;
            d.Zeilen = s.Zeilen;
            d.Quelle = s.Quelle ?? "";
            d.Ausgabe = s.Ausgabe ?? "";
            d.DatumImport = s.DatumImport ?? "";
            d.Klimazonen = s.Klimazonen.ToList();
            d.Gebaeudearten = s.Gebaeudearten.ToList();
            d.Typtage = s.Typtage.ToList();
            d.AufloesungenMin = s.AufloesungenMin.ToList();
            d.MitTagesgaenge = s.MitTagesgaengen;
            return d;
        }

        // =================================================================================
        // Paketwahl und Prüfung
        // =================================================================================

        /// <summary>
        /// Die Paketwahl über <see cref="Dienste.Datei"/> — HINTER dem Blazor-Ereignis (wartbarer
        /// Zwilling), Startordner aus der Einstellung der letzten Wahl. <c>""</c> = abgebrochen.
        /// </summary>
        internal static Task<string> TyptagPaketWaehlen(string filter)
        {
            TwwTyptagImportTexte t = TyptagTexte();
            string ordner = "";
            try { ordner = Dienste.Einstellungen?.Lies(EINSTELLUNG_IMPORTORDNER, "") ?? ""; } catch { }
            return Dienste.Datei.DateiOeffnenAsync(t.WahlTitel, string.IsNullOrEmpty(filter) ? t.Dateifilter : filter, ordner);
        }

        /// <summary>
        /// <b>Die Prüfung eines Pakets</b> — Paket lesen und mit demselben Leser prüfen wie das
        /// Einspielen, aber OHNE Schreibzugriff: Der eingespielte Stand bleibt, bis „Einspielen"
        /// gedrückt ist. Der Ordner wird für die nächste Wahl gemerkt.
        /// </summary>
        internal static TwwTyptagPruefberichtDaten TyptagPruefen(string pfad)
        {
            TwwTyptagImportTexte t = TyptagTexte();
            var d = new TwwTyptagPruefberichtDaten();
            if (string.IsNullOrWhiteSpace(pfad))
            {
                d.Abgebrochen = true;
                d.Abbruch = t.KeinPaket;
                return d;
            }

            IReadOnlyList<TwwPaketdatei> dateien = TwwTyptagCtrl.PaketLesen(pfad, out ZapfSatz fehler);
            if (fehler != null)
            {
                d.Abgebrochen = true;
                d.Abbruch = Format(t.Abbruch, Satztext(fehler));
                return d;
            }
            OrdnerMerken(pfad);

            TwwTyptagpruefung p = TwwTyptagCtrl.Pruefen(dateien);
            d.Hinweise.AddRange(p.Hinweise.Select(Satztext));
            if (!p.Ok)
            {
                d.Abgebrochen = true;
                d.Abbruch = Format(t.Abbruch, Satztext(p.Abbruch));
                return d;
            }
            d.Zusammenfassung = Format(t.Zusammenfassung, p.Klimazonen.Count, p.Gebaeudearten.Count,
                                       p.Typtage.Count, p.Zeilen);
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelQuelle, p.Quelle.Length > 0 ? p.Quelle : t.WertOhne));
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelAusgabe, p.Ausgabe.Length > 0 ? p.Ausgabe : t.WertOhne));
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelZonen, Aufzaehlung(p.Klimazonen.Select(Zahl), t)));
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelGebaeudearten, Aufzaehlung(p.Gebaeudearten, t)));
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelTyptage, Aufzaehlung(p.Typtage, t)));
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelAufloesung, p.AufloesungenMin.Count == 0
                ? t.WertOhneGaenge : Aufzaehlung(p.AufloesungenMin.Select(Zahl), t)));
            d.Angaben.Add(new TwwTyptagAngabeDaten(t.LabelZeilen, Zahl(p.Zeilen)));
            return d;
        }

        // =================================================================================
        // Einspielen und Löschen
        // =================================================================================

        /// <summary>
        /// <b>Das Einspielen</b> (<see cref="TwwTyptagCtrl.Importieren(IReadOnlyList{TwwPaketdatei}, string)"/>):
        /// EIN Vorgang, ein Paket ersetzt das andere. Bei einem Abbruch ist nichts geändert, und der
        /// Grund kommt benannt zurück.
        /// </summary>
        internal static TwwTyptagErgebnisDaten TyptagEinspielen(string pfad)
        {
            TwwTyptagImportTexte t = TyptagTexte();
            var e = new TwwTyptagErgebnisDaten { Stand = TyptagStand() };
            if (string.IsNullOrWhiteSpace(pfad))
            {
                e.Meldung = t.KeinPaket;
                return e;
            }

            IReadOnlyList<TwwPaketdatei> dateien = TwwTyptagCtrl.PaketLesen(pfad, out ZapfSatz fehler);
            if (fehler != null)
            {
                e.Meldung = Format(t.Abbruch, Satztext(fehler));
                return e;
            }
            OrdnerMerken(pfad);

            TwwTyptagimportBericht b = TwwTyptagCtrl.Importieren(dateien);
            e.Hinweise.AddRange(b.Hinweise.Select(Satztext));
            e.Stand = AlsStandDaten(b.Stand);
            if (b.Abbruch != null)
            {
                e.Meldung = Format(t.Abbruch, Satztext(b.Abbruch));
                return e;
            }
            e.Ok = true;
            e.Meldung = Format(t.MeldungEingespielt, b.Zeilen, b.Ersetzt);
            return e;
        }

        /// <summary>
        /// <b>Das Löschen</b> (<see cref="TwwTyptagCtrl.Loeschen"/>): Danach ist der Typtagweg
        /// benannt nicht verfügbar. War nichts eingespielt, sagt die Meldung genau das.
        /// </summary>
        internal static TwwTyptagErgebnisDaten TyptagLoeschen()
        {
            TwwTyptagImportTexte t = TyptagTexte();
            int weg = TwwTyptagCtrl.Loeschen();
            return new TwwTyptagErgebnisDaten
            {
                Ok = weg > 0,
                Meldung = weg > 0 ? Format(t.MeldungGeloescht, weg) : t.MeldungNichts,
                Stand = TyptagStand()
            };
        }

        /// <summary>Merkt den Ordner der Paketwahl; ein nicht gemerkter Ordner ist kein Importfehler.</summary>
        private static void OrdnerMerken(string pfad)
        {
            try
            {
                string ordner = System.IO.Directory.Exists(pfad) ? pfad : System.IO.Path.GetDirectoryName(pfad) ?? "";
                Dienste.Einstellungen?.Schreib(EINSTELLUNG_IMPORTORDNER, ordner);
            }
            catch { /* ein nicht gemerkter Ordner ist kein Importfehler */ }
        }

        private static string Zahl(int i) => i.ToString(CultureInfo.CurrentCulture);

        private static string Aufzaehlung(IEnumerable<string> werte, TwwTyptagImportTexte t)
        {
            string text = string.Join(", ", werte ?? Enumerable.Empty<string>());
            return text.Length > 0 ? text : t.WertOhne;
        }

        // =================================================================================
        // Texte
        // =================================================================================

        /// <summary>Das Textbündel des Dialogs in der Oberflächensprache (<c>ZPGT_…</c>); Rückfall der deutsche Vorgabewert.</summary>
        internal static TwwTyptagImportTexte TyptagTexte()
        {
            var t = new TwwTyptagImportTexte();
            t.Titel = Text_("ZPGT_TITEL", t.Titel);
            t.HinweisLizenz = Text_("ZPGT_HINWEIS_LIZENZ", t.HinweisLizenz);
            t.HinweisFormat = Text_("ZPGT_HINWEIS_FORMAT", t.HinweisFormat);
            t.GruppeStand = Text_("ZPGT_GRP_STAND", t.GruppeStand);
            t.GruppePruefung = Text_("ZPGT_GRP_PRUEFUNG", t.GruppePruefung);
            t.Leer = Text_("ZPGT_LEER", t.Leer);
            t.LabelQuelle = Text_("ZPGT_LBL_QUELLE", t.LabelQuelle);
            t.LabelAusgabe = Text_("ZPGT_LBL_AUSGABE", t.LabelAusgabe);
            t.LabelDatum = Text_("ZPGT_LBL_DATUM", t.LabelDatum);
            t.LabelZonen = Text_("ZPGT_LBL_ZONEN", t.LabelZonen);
            t.LabelGebaeudearten = Text_("ZPGT_LBL_GEBAEUDEARTEN", t.LabelGebaeudearten);
            t.LabelTyptage = Text_("ZPGT_LBL_TYPTAGE", t.LabelTyptage);
            t.LabelAufloesung = Text_("ZPGT_LBL_AUFLOESUNG", t.LabelAufloesung);
            t.LabelZeilen = Text_("ZPGT_LBL_ZEILEN", t.LabelZeilen);
            t.SpalteAngabe = Text_("ZPGT_SP_ANGABE", t.SpalteAngabe);
            t.SpalteWert = Text_("ZPGT_SP_WERT", t.SpalteWert);
            t.WertOhne = Text_("ZPGT_WERT_OHNE", t.WertOhne);
            t.WertOhneGaenge = Text_("ZPGT_WERT_OHNE_GAENGE", t.WertOhneGaenge);
            t.KnopfPaket = Text_("ZPGT_BTN_PAKET", t.KnopfPaket);
            t.KnopfEinspielen = Text_("ZPGT_BTN_EINSPIELEN", t.KnopfEinspielen);
            t.KnopfLoeschen = Text_("ZPGT_BTN_LOESCHEN", t.KnopfLoeschen);
            t.KnopfBeenden = Text_("ZPGT_BTN_BEENDEN", t.KnopfBeenden);
            t.Dateifilter = Text_("ZPGT_DATEIFILTER", t.Dateifilter);
            t.WahlTitel = Text_("ZPGT_WAHL_TITEL", t.WahlTitel);
            t.KeinPaket = Text_("ZPGT_KEIN_PAKET", t.KeinPaket);
            t.Abbruch = Text_("ZPGT_ABBRUCH", t.Abbruch);
            t.Zusammenfassung = Text_("ZPGT_ZUSAMMENFASSUNG", t.Zusammenfassung);
            t.Hinweise = Text_("ZPGT_HINWEISE", t.Hinweise);
            t.FrageErsetzen = Text_("ZPGT_FRAGE_ERSETZEN", t.FrageErsetzen);
            t.FrageLoeschen = Text_("ZPGT_FRAGE_LOESCHEN", t.FrageLoeschen);
            t.MeldungEingespielt = Text_("ZPGT_MSG_EINGESPIELT", t.MeldungEingespielt);
            t.MeldungGeloescht = Text_("ZPGT_MSG_GELOESCHT", t.MeldungGeloescht);
            t.MeldungNichts = Text_("ZPGT_MSG_NICHTS", t.MeldungNichts);
            return t;
        }
    }
}
