using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Editors „Zeitverlauf je Größe"</b> im Blatt „Nutzungsprofile" (Stufe NP4c; Konzept Nutzungsprofile
    /// 6.1, 4.3, NP-F7, NP-F9) — sie baut <see cref="RaumnutzungBildWeg"/> aus <see cref="RaumnutzungCtrl"/>: Einheit und
    /// Grenzen je Größe aus <see cref="Konditionierungsgroessen"/>, die Vorschau (Woche und Teppichbild) und die Vorschläge
    /// beim Umschalten. Plattformfrei, ohne Datenbank: Geschrieben wird über „Speichern" des Blatts
    /// (<see cref="RaumnutzungCtrl.ProfilAendern"/>), die Übersetzung des Feldsatzes steht in <see cref="RaumnutzungHuelle.Kern(RaumnutzungProfilDaten)"/>.
    /// </summary>
    internal static class RaumnutzungBildHuelle
    {
        /// <summary>Das Bündel des Editors.</summary>
        internal static RaumnutzungBildWeg Weg()
        {
            RaumnutzungBildTexte t = Texte();
            return new RaumnutzungBildWeg
            {
                Texte = t,
                Grenzen = Grenzen,
                Vorschau = (d, g) => Vorschau(d, g, t),
                Zeilenbildvorschlag = Zeilenbildvorschlag,
                Stundenvorschlag = Stundenvorschlag,
            };
        }

        /// <summary>Einheit und Grenzen einer Größe in der Anzeige — die Grenzen des Kerns (NP-F11), Anteile in Prozent.</summary>
        internal static RaumnutzungBildgrenzen Grenzen(KonditionierungGroesse g)
        {
            Konditionierungsgroesse k = KonditionierungHuelle.Kern(g);
            double f = Kalenderteppich.Anzeigefaktor(k);
            return new RaumnutzungBildgrenzen(g, Kalenderteppich.Einheit(k), f, Konditionierungsgroessen.Min(k) * f,
                                              Konditionierungsgroessen.Max(k) * f,
                                              k == Konditionierungsgroesse.Heizsoll || k == Konditionierungsgroesse.Kuehlsoll);
        }

        /// <summary>
        /// Die Vorschau einer Größe über dem Entwurf (<see cref="RaumnutzungCtrl.Vorschau"/>): die typische Woche in der
        /// Einheit der Anzeige und das Teppichbild des Kalenders, den ein leeres Ziel an der neutralen Ferienlage bekäme.
        /// </summary>
        internal static RaumnutzungBildvorschau Vorschau(RaumnutzungProfilDaten d, KonditionierungGroesse g, RaumnutzungBildTexte t)
        {
            if (d == null) return null;
            try
            {
                Konditionierungsgroesse k = KonditionierungHuelle.Kern(g);
                RaumnutzungCtrl.Profilvorschau v = RaumnutzungCtrl.Vorschau(RaumnutzungHuelle.Kern(d), k);
                string hinweis = RaumnutzungHuelle.Hinweistext(v.Hinweis, RaumnutzungHuelle.Texte());
                string bezug = string.Format(CultureInfo.CurrentCulture, t.TextBezug, v.Referenzjahr.ToString(CultureInfo.InvariantCulture));
                // Ohne Kalender: nicht belegt — oder der Schritt hat am Ziel benannt abgelehnt; dann steht sein Grund da.
                if (v.Kalender == null)
                    return new RaumnutzungBildvorschau(false, Weg(v.Weg), null, null,
                                                       string.IsNullOrEmpty(v.Meldung) ? hinweis : v.Meldung, bezug);
                double f = Kalenderteppich.Anzeigefaktor(k);
                double[] woche = v.Woche?.Select(w => double.IsNaN(w) ? w : Math.Round(w * f, 4, MidpointRounding.AwayFromZero)).ToArray();
                var teppich = ChartRenderer.KalenderteppichModell(Kalenderteppich.ImGemeinjahr(v.Kalender, v.Referenzjahr, v.Feiertagsjahr), null,
                                                                  ChartRenderer.KalenderteppichTexte.AusRessourcen());
                return new RaumnutzungBildvorschau(true, Weg(v.Weg), woche, teppich, hinweis, bezug);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Das Zeilenbild, das die Kennwerte der Größe ergäben (<see cref="RaumnutzungCtrl.Zeilenbildvorschlag"/>).</summary>
        internal static IReadOnlyList<RaumnutzungZeilenbildDaten> Zeilenbildvorschlag(RaumnutzungProfilDaten d, KonditionierungGroesse g)
        {
            if (d == null) return Array.Empty<RaumnutzungZeilenbildDaten>();
            return RaumnutzungCtrl.Zeilenbildvorschlag(RaumnutzungHuelle.Kern(d), KonditionierungHuelle.Kern(g))
                .Select(z => new RaumnutzungZeilenbildDaten(g, z.Zeile, z.Wert, z.Aus, z.Von, z.Bis, z.BedingtK))
                .ToList();
        }

        /// <summary>Die Stundenprofile, die der bisherige Weg der Größe ergäbe (<see cref="RaumnutzungCtrl.Stundenvorschlag"/>).</summary>
        internal static IReadOnlyList<RaumnutzungStundenDaten> Stundenvorschlag(RaumnutzungProfilDaten d, KonditionierungGroesse g)
        {
            if (d == null) return Array.Empty<RaumnutzungStundenDaten>();
            Konditionierungsgroesse k = KonditionierungHuelle.Kern(g);
            List<Raumnutzungsstunden> s = RaumnutzungCtrl.Stundenvorschlag(RaumnutzungHuelle.Kern(d), k);
            if (s == null) return Array.Empty<RaumnutzungStundenDaten>();
            return s.Select(x => (x, Raumnutzungsgenerator.Stundenwerte(x.Werte, k)))
                    .Where(x => x.Item2 != null)
                    .Select(x => new RaumnutzungStundenDaten(g, x.x.Tagesart == RaumnutzungSchema.TAGESART_FREI
                                                                    ? RaumnutzungTagesart.Frei : RaumnutzungTagesart.Werktag,
                                                             x.Item2))
                    .ToList();
        }

        /// <summary>Der Weg des Generators als Umschalterstellung (ohne Weg: aus Kennwerten).</summary>
        private static RaumnutzungBildweg Weg(Raumnutzungsweg w) => w switch
        {
            Raumnutzungsweg.Zeilenbild => RaumnutzungBildweg.Zeilenbild,
            Raumnutzungsweg.Stundenprofil => RaumnutzungBildweg.Stundenprofil,
            _ => RaumnutzungBildweg.Kennwerte,
        };

        /// <summary>Das Textbündel aus <c>MyResource</c> — Rückfall ist der deutsche Vorgabewert.</summary>
        internal static RaumnutzungBildTexte Texte()
        {
            var t = new RaumnutzungBildTexte();
            t.Titel = Text_("RNP_ED_TITEL", t.Titel);
            t.LabelWeg = Text_("RNP_ED_LBL_WEG", t.LabelWeg);
            t.WegKennwerte = Text_("RNP_ED_WEG_KENNWERTE", t.WegKennwerte);
            t.WegZeilenbild = Text_("RNP_ED_WEG_ZEILENBILD", t.WegZeilenbild);
            t.WegStunden = Text_("RNP_ED_WEG_STUNDEN", t.WegStunden);
            t.HinweisKennwerte = Text_("RNP_ED_HINWEIS_KENNWERTE", t.HinweisKennwerte);
            t.HinweisZeilenbild = Text_("RNP_ED_HINWEIS_ZEILENBILD", t.HinweisZeilenbild);
            t.HinweisStunden = Text_("RNP_ED_HINWEIS_STUNDEN", t.HinweisStunden);
            t.HinweisLuft = Text_("RNP_ED_HINWEIS_LUFT", t.HinweisLuft);
            t.HinweisLeer = Text_("RNP_ED_HINWEIS_LEER", t.HinweisLeer);
            t.TitelFrage = Text_("RNP_ED_TITEL_FRAGE", t.TitelFrage);
            t.FrageKennwerte = Text_("RNP_ED_FRAGE_KENNWERTE", t.FrageKennwerte);
            t.FrageStunden = Text_("RNP_ED_FRAGE_STUNDEN", t.FrageStunden);
            t.FrageZeilenbild = Text_("RNP_ED_FRAGE_ZEILENBILD", t.FrageZeilenbild);
            t.LabelStunde = Text_("RNP_ED_LBL_STUNDE", t.LabelStunde);
            t.TagesartWerktag = Text_("RNP_LBL_TAGESART_WERKTAG", t.TagesartWerktag);
            t.TagesartFrei = Text_("RNP_LBL_TAGESART_FREI", t.TagesartFrei);
            t.LabelEinfuegen = Text_("RNP_ED_LBL_EINFUEGEN", t.LabelEinfuegen);
            t.PlatzhalterEinfuegen = Text_("RNP_ED_PH_EINFUEGEN", t.PlatzhalterEinfuegen);
            t.KnopfEinfuegen = Text_("RNP_ED_BTN_EINFUEGEN", t.KnopfEinfuegen);
            t.GrundEinfuegen = Text_("RNP_ED_GRUND_EINFUEGEN", t.GrundEinfuegen);
            t.MeldungAnzahl = Text_("RNP_ED_MSG_ANZAHL", t.MeldungAnzahl);
            t.MeldungWert = Text_("RNP_ED_MSG_WERT", t.MeldungWert);
            t.LabelVorschau = Text_("RNP_ED_LBL_VORSCHAU", t.LabelVorschau);
            t.LabelWoche = Text_("RNP_ED_LBL_WOCHE", t.LabelWoche);
            t.LabelTeppich = Text_("RNP_ED_LBL_TEPPICH", t.LabelTeppich);
            t.TextBezug = Text_("RNP_ED_TXT_BEZUG", t.TextBezug);
            t.TextNichtBelegt = Text_("RNP_ED_TXT_NICHT_BELEGT", t.TextNichtBelegt);
            t.GrundNurLesen = Text_("RNP_TXT_AUSGELIEFERT", t.GrundNurLesen);

            WochenrasterTexte r = t.Raster;
            r.Wochentage = Text_("WRASTER_TAGE", r.Wochentage);
            r.KopfTag = Text_("WRASTER_KOPF_TAG", r.KopfTag);
            r.Zelle = Text_("WRASTER_ZELLE", r.Zelle);
            r.ZeileVorgabe = Text_("WRASTER_ZEILE_VORGABE", r.ZeileVorgabe);
            return t;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
