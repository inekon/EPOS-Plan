using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Konditionierung</b> (Stufe KP2, Welle K2, Teilschritt 5; Entwurf KP2 Abschnitt 2):
    /// Sie übersetzt zwischen der Datenseite des Reiters (<see cref="KonditionierungDaten"/>,
    /// <see cref="KonditionierungStand"/>, Anteile in Prozent) und dem reinen Arbeitsstand des Kerns
    /// (<see cref="Konditionierungsarbeitsstand"/>, Anteile 0 … 1), füllt die Konditionierung beim Lesen
    /// und trägt den <see cref="KonditionierungWeg"/> aus den reinen Schritten der
    /// <see cref="Konditionierungsarbeit"/>. Geschrieben wird allein im OK-Weg des Editors
    /// (<see cref="GebaeudeKatalogHuelle"/>); ausgenommen sind die Vorlagen (Festlegung 13).
    /// </summary>
    /// <remarks>
    /// <para><b>Eine Wahrheit.</b> Die neun Bestandszellen, die Nachtzeiten der Heizspalte, die Merker und
    /// die Ferienzeiträume stehen in den Feldern von <see cref="GebaeudeKatalogDaten"/> bzw.
    /// <see cref="ZoneDaten"/>; die Hülle bildet daraus den <see cref="Matrixeingang"/> und schreibt nach
    /// einem Schritt nur die Felder zurück, die er geändert hat. Die Zellen der Bestandsspalten tragen im
    /// gespeicherten Stand keinen Wert (Konzept 5.6).</para>
    /// <para><b>Die Fassung.</b> Jede Ebene, die ein Schritt ändert — auch eine Zone, deren Kalender
    /// „Anlegen" am Gebäude mit anlegt (F2 Regel 2) —, bekommt ihre <see cref="KonditionierungDaten.Fassung"/>
    /// um eins erhöht; der OK-Weg schreibt nur eine Ebene mit geänderter Fassung, und dort nur, was sich
    /// gegen die Datenbank geändert hat.</para>
    /// </remarks>
    internal static class KonditionierungHuelle
    {
        // =================================================================================
        // Größen, Zeilen, Prozent
        // =================================================================================

        /// <summary>Die Größe des Kerns zu einer Größe der Oberfläche — dieselbe Reihenfolge.</summary>
        internal static Konditionierungsgroesse Kern(KonditionierungGroesse g) => Konditionierungsgroessen.Alle[(int)g];

        /// <summary>Die Größe der Oberfläche zu einer Größe des Kerns.</summary>
        internal static KonditionierungGroesse Oberflaeche(Konditionierungsgroesse g)
            => (KonditionierungGroesse)Array.IndexOf(Konditionierungsgroessen.Alle, g);

        /// <summary>Das Zeilenkennwort einer Zeile der Oberfläche.</summary>
        internal static string Zeile(KonditionierungZeile z) => DbWerte.KOND_ZEILEN[(int)z];

        private static readonly KonditionierungZeile[] ZEILEN =
        {
            KonditionierungZeile.Nennwert, KonditionierungZeile.Tag, KonditionierungZeile.Nacht,
            KonditionierungZeile.Wochenende, KonditionierungZeile.Ferien, KonditionierungZeile.Saison,
        };

        /// <summary>Trägt die Zelle einen Anteil (Geräte und Personen außer dem Nennwert) — dann in Prozent an der Oberfläche?</summary>
        private static bool Anteil(Konditionierungsgroesse g, string zeile)
            => Konditionierungsgroessen.HatNennwert(g) && !string.Equals(zeile, DbWerte.KOND_ZEILE_NENNWERT, StringComparison.Ordinal);

        /// <summary>Ein Anteil 0 … 1 als Prozent — dezimal gerechnet, damit 0,1 genau 10 wird; „aus" (NaN) bleibt.</summary>
        internal static double Prozent(double anteil)
            => double.IsFinite(anteil) && Math.Abs(anteil) < 1e12 ? (double)((decimal)anteil * 100m) : anteil;

        /// <summary>Prozent als Anteil 0 … 1 — dezimal gerechnet; „aus" (NaN) bleibt.</summary>
        internal static double AusProzent(double prozent)
            => double.IsFinite(prozent) && Math.Abs(prozent) < 1e12 ? (double)((decimal)prozent / 100m) : prozent;

        private static double Skaliert(double wert, bool anteil, bool nachProzent)
            => !anteil ? wert : nachProzent ? Prozent(wert) : AusProzent(wert);

        // =================================================================================
        // Zellen und Kalender
        // =================================================================================

        /// <summary>Eine Zelle des Kerns an der Oberfläche.</summary>
        internal static KonditionierungZelle Zelle(Matrixzelle c, bool anteil)
            => new KonditionierungZelle
            {
                Wert = c != null && c.Belegt && !c.Aus ? Skaliert(c.Wert, anteil, true) : (double?)null,
                Aus = c != null && c.Aus,
                Von = c?.Von,
                Bis = c?.Bis,
                DeltaT = c?.BedingtK,
            };

        /// <summary>Eine Zelle der Oberfläche im Kern; leer = <see cref="Matrixzelle.Leer"/>.</summary>
        internal static Matrixzelle Zelle(KonditionierungZelle z, bool anteil)
        {
            if (z == null || z.Leer) return Matrixzelle.Leer;
            if (z.Aus) return Matrixzelle.Abgeschaltet(z.Von, z.Bis, z.DeltaT);
            return z.Wert.HasValue
                ? Matrixzelle.AusWert(Skaliert(z.Wert.Value, anteil, false), z.Von, z.Bis, z.DeltaT)
                : Matrixzelle.NurZeiten(z.Von, z.Bis, z.DeltaT);
        }

        private static void Setzen(KonditionierungSpalte s, KonditionierungZeile z, KonditionierungZelle zelle)
        {
            switch (z)
            {
                case KonditionierungZeile.Nennwert: s.Nennwert = zelle; break;
                case KonditionierungZeile.Tag: s.Tag = zelle; break;
                case KonditionierungZeile.Nacht: s.Nacht = zelle; break;
                case KonditionierungZeile.Wochenende: s.Wochenende = zelle; break;
                case KonditionierungZeile.Ferien: s.Ferien = zelle; break;
                default: s.Saison = zelle; break;
            }
        }

        /// <summary>Ein angelegter Kalender des Kerns an der Oberfläche, samt Herkunft und Matrixbereich.</summary>
        internal static KonditionierungKalender Kalender(Konditionierungskalender k, Kalenderherkunft h, bool anteil)
        {
            var d = new KonditionierungKalender
            {
                Zustand = KonditionierungZustand.Angelegt,
                Nennwert = k.Nennwert,
                Vorlage = h?.Vorlage,
                Vermerk = h?.Vermerk,
                HerkunftProfil = h?.IstProfil == true,
            };
            Angabe(k.Grundangabe, anteil, out KonditionierungAngabe art, out double? wert, out double[] woche, out _);
            d.Angabe = art;
            d.Wert = wert;
            d.Woche = woche;
            d.Perioden = k.Perioden.OrderByDescending(p => p.Rang).Select(p => Periode(p, anteil)).ToList();
            return d;
        }

        private static KonditionierungPeriode Periode(Kalenderregel r, bool anteil)
        {
            Angabe(r.Angabe, anteil, out KonditionierungAngabe art, out double? wert, out double[] woche, out int? tag);
            int index = -1;
            for (int i = 0; i < DbWerte.KOND_ARTEN.Count; i++)
                if (string.Equals(DbWerte.KOND_ARTEN[i], r.Art, StringComparison.Ordinal)) index = i;
            return new KonditionierungPeriode
            {
                Rang = r.Rang,
                Art = index < 0 ? KonditionierungPeriodenart.Zeitraum : (KonditionierungPeriodenart)index,
                Name = r.Bezeichner ?? "",
                Von = r.IstFeiertag ? (int?)null : r.Beginn,
                Bis = r.IstFeiertag ? (int?)null : r.Ende,
                Feiertagsregel = r.IstFeiertag ? r.Feiertagsregel : null,
                Angabe = art,
                Wert = wert,
                Woche = woche,
                WieWochentag = tag,
                Matrixbereich = Konditionierungsarbeit.IstMatrixbereich(r),
                Eigenband = Kalenderwerkzeuge.ImEigenband(r.Rang),
            };
        }

        private static void Angabe(Kalenderangabe a, bool anteil, out KonditionierungAngabe art, out double? wert,
                                   out double[] woche, out int? wieWochentag)
        {
            wert = null;
            woche = null;
            wieWochentag = null;
            switch (a.Art)
            {
                case Angabeart.Wert:
                    art = KonditionierungAngabe.Wert;
                    wert = Skaliert(a.Wert, anteil, true);
                    break;
                case Angabeart.Aus:
                    art = KonditionierungAngabe.Aus;
                    break;
                case Angabeart.Woche:
                    art = KonditionierungAngabe.Woche;
                    woche = a.Woche.Select(v => Skaliert(v, anteil, true)).ToArray();
                    break;
                default:
                    art = KonditionierungAngabe.WieWochentag;
                    wieWochentag = a.WieWochentag;
                    break;
            }
        }

        /// <summary>Die Angabe der Oberfläche im Kern — ein unvollständiger Satz ist eine <see cref="ArgumentException"/>.</summary>
        private static Kalenderangabe Angabe(KonditionierungAngabe art, double? wert, double[] woche, int? wieWochentag, bool anteil)
        {
            switch (art)
            {
                case KonditionierungAngabe.Wert:
                    if (!wert.HasValue) throw new ArgumentException("Eine Wertangabe ohne Wert.", nameof(wert));
                    return Kalenderangabe.AusWert(Skaliert(wert.Value, anteil, false));
                case KonditionierungAngabe.Aus:
                    return Kalenderangabe.Abgeschaltet;
                case KonditionierungAngabe.Woche:
                    if (woche == null) throw new ArgumentException("Eine Wochenangabe ohne Woche.", nameof(woche));
                    return Kalenderangabe.AusWoche(woche.Select(v => Skaliert(v, anteil, false)).ToArray());
                default:
                    if (!wieWochentag.HasValue) throw new ArgumentException("„wie Wochentag“ ohne Wochentag.", nameof(wieWochentag));
                    return Kalenderangabe.AlsWochentag(wieWochentag.Value);
            }
        }

        /// <summary>Ein angelegter Kalender der Oberfläche im Kern.</summary>
        internal static Konditionierungskalender Kalender(KonditionierungKalender d, Konditionierungsgroesse g)
        {
            bool anteil = Konditionierungsgroessen.HatNennwert(g);
            Kalenderangabe grund = Angabe(d.Angabe, d.Wert, d.Woche, null, anteil);
            var perioden = new List<Kalenderregel>();
            foreach (KonditionierungPeriode p in d.Perioden ?? new List<KonditionierungPeriode>())
            {
                Kalenderangabe a = Angabe(p.Angabe, p.Wert, p.Woche, p.WieWochentag, anteil);
                string art = DbWerte.KOND_ARTEN[(int)p.Art];
                perioden.Add(!string.IsNullOrEmpty(p.Feiertagsregel)
                    ? Kalenderregel.Feiertag(p.Rang, p.Name ?? "", p.Feiertagsregel, a)
                    : Kalenderregel.Zeitraum(p.Rang, art, p.Name ?? "", p.Von ?? 0, p.Bis ?? 0, a));
            }
            return new Konditionierungskalender(g, grund, d.Nennwert, perioden);
        }

        // =================================================================================
        // Ebenen
        // =================================================================================

        /// <summary>
        /// <b>Eine Ebene des Kerns an der Oberfläche</b> — die 30 Zellen, je Größe der angelegte Kalender;
        /// sonst „aus der Matrix", an einer Zone „vom Gebäude", wo das Gebäude einen angelegt hat.
        /// </summary>
        internal static KonditionierungDaten Daten(Konditionierungsstand e, Konditionierungsstand gebaeude, int fassung)
        {
            var d = new KonditionierungDaten { Fassung = fassung };
            if (e == null) return d;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                KonditionierungSpalte s = d.Spalte(Oberflaeche(g));
                foreach (KonditionierungZeile z in ZEILEN)
                    Setzen(s, z, Zelle(e.Vorgabe(g, Zeile(z)), Anteil(g, Zeile(z))));
                Konditionierungskalender k = e.Kalender(g);
                s.Kalender = k != null
                    ? Kalender(k, e.Herkunft(g), Konditionierungsgroessen.HatNennwert(g))
                    : new KonditionierungKalender
                    {
                        Zustand = gebaeude != null && gebaeude.Kalender(g) != null
                            ? KonditionierungZustand.VomGebaeude
                            : KonditionierungZustand.Abgeleitet
                    };
            }
            return d;
        }

        /// <summary>
        /// <b>Eine Ebene der Oberfläche im Kern</b> über dem gegebenen Bestand. Eine Zelle mit
        /// Bestandsspalte trägt keinen Wert (er steht im Feld), die Heiz-Nachtzeile an Gebäude und
        /// Katalogbau keine Zeiten (sie stehen in <c>NachtBeginn</c>/<c>NachtEnde</c>, B6).
        /// </summary>
        /// <exception cref="ArgumentException">Ein Kalender der Oberfläche ist unvollständig oder ungültig.</exception>
        internal static Konditionierungsstand Ebene(KonditionierungDaten d, Kalendereigentuemer art, Matrixeingang bestand)
        {
            Konditionierungsstand e = Konditionierungsstand.Leer(art, bestand);
            if (d == null) return e;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                KonditionierungSpalte s = d.Spalte(Oberflaeche(g));
                foreach (KonditionierungZeile z in ZEILEN)
                {
                    string zeile = Zeile(z);
                    Matrixzelle c = Zelle(s.Zelle(z), Anteil(g, zeile));
                    if (Matrixzellenort.HatBestandsspalte(art, g, zeile) && c.Belegt && !c.Aus)
                        c = Matrixzelle.NurZeiten(c.Von, c.Bis, c.BedingtK);
                    if (Konditionierungsarbeit.NachtzeitImBestand(art, g, zeile))
                        c = c.Aus ? Matrixzelle.Abgeschaltet(null, null, c.BedingtK) : Matrixzelle.NurZeiten(null, null, c.BedingtK);
                    e = e.MitVorgabe(g, zeile, c);
                }
                if (s.Kalender?.Zustand == KonditionierungZustand.Angelegt)
                    e = e.MitKalender(g, Kalender(s.Kalender, g), new Kalenderherkunft(s.Kalender.Vorlage, s.Kalender.Vermerk,
                                                                                         s.Kalender.HerkunftProfil));
            }
            return e;
        }

        // =================================================================================
        // Bestand
        // =================================================================================

        /// <summary>
        /// Die Bestandsfelder eines Feldsatzes — wie <see cref="GebaeudeKatalogHuelle.NachModell"/> sie
        /// schreibt: Nutzfläche ist die Wohnfläche, Bewohner = Wohnfläche ÷ Fläche je Nutzer (0 → 35 m²).
        /// </summary>
        internal static Matrixeingang Bestand(GebaeudeKatalogDaten g, bool kopplungWirksam, bool kuehlungWirksam)
        {
            var b = new Matrixeingang
            {
                SollTag = g.SollTag,
                SollNacht = g.NachtAbsenkung,
                SollWochenende = g.WochenendAbsenkung,
                SollFerien = g.SollFerien,
                NachtBeginn = g.NachtBeginn,
                NachtEnde = g.NachtEnde,
                Ferienmerker = g.Ferien,
                Wochenendmerker = g.Wochenende,
                Sollwertprofil = g.Sollwertprofil,
                KopplungWirksam = kopplungWirksam,
                KuehlSollwert = g.KuehlSollwert,
                KuehlSollwertNacht = g.KuehlSollwertNacht,
                KuehlungWirksam = kuehlungWirksam,
                LuftwechselInfiltration = g.LuftwechselInfiltration,
                LuftwechselNutzer = g.LuftwechselNutzer,
                Luftwechselrate = g.Luftwechselrate,
                InterneWaermegewinne = g.Waermegewinne,
                Bewohner = g.WohnflaecheGesamt is double w && w > 0.0
                    ? Gebaeudevorgaben.BewohnerAusFlaeche(w, g.FlaecheNutzer ?? 0.0)
                    : (double?)null,
                Maximaleraumtemperatur = g.MaxTemperatur,
            };
            for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
            {
                b.Ferienbeginn[i] = g.Ferienbeginn != null && i < g.Ferienbeginn.Length ? g.Ferienbeginn[i] : 0;
                b.Ferienende[i] = g.Ferienende != null && i < g.Ferienende.Length ? g.Ferienende[i] : 0;
            }
            Konditionierungsarbeit.HerkunftDesLuftwechsels(b);
            return b;
        }

        /// <summary>Die eigenen Bestandsfelder einer Zone; leer heißt „wie das Gebäude".</summary>
        internal static Matrixeingang Bestand(ZoneDaten z)
            => new Matrixeingang
            {
                SollTag = z.SollTag,
                SollNacht = z.SollNacht,
                SollWochenende = z.SollWochenende,
                SollFerien = z.SollFerien,
                Maximaleraumtemperatur = z.Maximaleraumtemperatur,
                // KU3-3: die Kühlzelle der Zone (Bestandszellen Kuehl_Sollwert, Kuehl_Sollwert_Nacht).
                KuehlSollwert = z.KuehlSollwert,
                KuehlSollwertNacht = z.KuehlSollwertNacht,
                LuftwechselInfiltration = z.LuftwechselInfiltration,
                LuftwechselNutzer = z.LuftwechselNutzer,
                InterneWaermegewinne = z.InterneWaermegewinne,
                Bewohner = z.Bewohner,
            };

        /// <summary>Schreibt in den Feldsatz nur, was sich zwischen den zwei Beständen geändert hat.</summary>
        private static void Zurueck(GebaeudeKatalogDaten g, Matrixeingang alt, Matrixeingang neu)
        {
            static bool Anders(double? a, double? b) => !Kalendervergleich.Gleich(a, b);
            if (Anders(alt.SollTag, neu.SollTag)) g.SollTag = neu.SollTag;
            if (Anders(alt.SollNacht, neu.SollNacht)) g.NachtAbsenkung = neu.SollNacht;
            if (Anders(alt.SollWochenende, neu.SollWochenende)) g.WochenendAbsenkung = neu.SollWochenende;
            if (Anders(alt.SollFerien, neu.SollFerien)) g.SollFerien = neu.SollFerien;
            if (Anders(alt.KuehlSollwert, neu.KuehlSollwert)) g.KuehlSollwert = neu.KuehlSollwert;
            if (Anders(alt.KuehlSollwertNacht, neu.KuehlSollwertNacht)) g.KuehlSollwertNacht = neu.KuehlSollwertNacht;
            if (Anders(alt.LuftwechselInfiltration, neu.LuftwechselInfiltration)) g.LuftwechselInfiltration = neu.LuftwechselInfiltration;
            if (Anders(alt.LuftwechselNutzer, neu.LuftwechselNutzer)) g.LuftwechselNutzer = neu.LuftwechselNutzer;
            if (Anders(alt.Luftwechselrate, neu.Luftwechselrate)) g.Luftwechselrate = neu.Luftwechselrate;
            if (Anders(alt.InterneWaermegewinne, neu.InterneWaermegewinne)) g.Waermegewinne = neu.InterneWaermegewinne;
            if (alt.NachtBeginn != neu.NachtBeginn) g.NachtBeginn = neu.NachtBeginn;
            if (alt.NachtEnde != neu.NachtEnde) g.NachtEnde = neu.NachtEnde;
            if (Anders(alt.Ferienmerker, neu.Ferienmerker)) g.Ferien = neu.Ferienmerker;
            if (Anders(alt.Wochenendmerker, neu.Wochenendmerker)) g.Wochenende = neu.Wochenendmerker;
            for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME && g.Ferienbeginn != null && g.Ferienende != null
                            && i < g.Ferienbeginn.Length && i < g.Ferienende.Length; i++)
            {
                if (Anders(alt.Ferienbeginn[i], neu.Ferienbeginn[i])) g.Ferienbeginn[i] = (int)neu.Ferienbeginn[i];
                if (Anders(alt.Ferienende[i], neu.Ferienende[i])) g.Ferienende[i] = (int)neu.Ferienende[i];
            }
        }

        /// <summary>Schreibt in die Zone nur, was sich geändert hat.</summary>
        private static void Zurueck(ZoneDaten z, Matrixeingang alt, Matrixeingang neu)
        {
            static bool Anders(double? a, double? b) => !Kalendervergleich.Gleich(a, b);
            if (Anders(alt.SollTag, neu.SollTag)) z.SollTag = neu.SollTag;
            if (Anders(alt.SollNacht, neu.SollNacht)) z.SollNacht = neu.SollNacht;
            if (Anders(alt.SollWochenende, neu.SollWochenende)) z.SollWochenende = neu.SollWochenende;
            if (Anders(alt.SollFerien, neu.SollFerien)) z.SollFerien = neu.SollFerien;
            if (Anders(alt.KuehlSollwert, neu.KuehlSollwert)) z.KuehlSollwert = neu.KuehlSollwert;
            if (Anders(alt.KuehlSollwertNacht, neu.KuehlSollwertNacht)) z.KuehlSollwertNacht = neu.KuehlSollwertNacht;
            if (Anders(alt.LuftwechselInfiltration, neu.LuftwechselInfiltration)) z.LuftwechselInfiltration = neu.LuftwechselInfiltration;
            if (Anders(alt.LuftwechselNutzer, neu.LuftwechselNutzer)) z.LuftwechselNutzer = neu.LuftwechselNutzer;
            if (Anders(alt.InterneWaermegewinne, neu.InterneWaermegewinne)) z.InterneWaermegewinne = neu.InterneWaermegewinne;
        }

        // =================================================================================
        // Der Arbeitsstand
        // =================================================================================

        /// <summary>
        /// <b>Der Bezug des Wegs</b> (Stufe KP2, Welle U1, Teilschritt 4 (c)) — was der Lauf aus dem
        /// PROJEKT nimmt und der Arbeitsstand deshalb auch: die Stufe der Anlagenkopplung
        /// (<c>Tab_Einstellungen.Anlagenkopplung</c>), das Referenzjahr des Laufs und den Projektschalter
        /// „Kühlung rechnen". Ein Katalogbau kennt kein Projekt: keine Kopplung, das Bezugsjahr
        /// <see cref="Konditionierungsarbeitsstand.BEZUGSJAHR_VORGABE"/>, die Kühlung allein nach dem
        /// Gebäude.
        /// </summary>
        /// <param name="Stufe">Die Kopplungsstufe des Projekts; <c>null</c> = keine.</param>
        /// <param name="Referenzjahr">Das Referenzjahr; <c>null</c> = das Bezugsjahr der Vorgabe.</param>
        /// <param name="Kuehlbetrieb">Rechnet das Projekt die Kühlung? Im Katalog <c>true</c> (unbekannt).</param>
        internal sealed record Bezug(string Stufe, int? Referenzjahr, bool Kuehlbetrieb = true)
        {
            /// <summary>Der Bezug eines Katalogbaus.</summary>
            internal static Bezug Katalog { get; } = new Bezug(null, null);
        }

        /// <summary>
        /// Der Bezug eines Projekts — dieselben Quellen wie der Lauf: <see cref="KonfigurationCtrl.AnlagenkopplungLesen"/>,
        /// <see cref="Konditionierungdatenweg.Bezugsjahr"/> (<c>SolardatenCtrl.Referenzjahr</c>) und
        /// <see cref="KonfigurationCtrl.KuehlbetriebLesen"/>; ohne Projekt (<paramref name="idProjekt"/> ≤ 0)
        /// der des Katalogs.
        /// </summary>
        internal static Bezug Projektbezug(int idProjekt)
            => idProjekt > 0
                ? new Bezug(KonfigurationCtrl.AnlagenkopplungLesen(idProjekt), Konditionierungdatenweg.Bezugsjahr(idProjekt),
                            KonfigurationCtrl.KuehlbetriebLesen(idProjekt))
                : Bezug.Katalog;

        /// <summary>
        /// Der reine Arbeitsstand zu einem Stand der Oberfläche (<paramref name="art"/>: Gebäude oder
        /// Katalogbau) mit dem Bezug des Projekts: Kopplung und Kühlung nach denselben Regeln wie der Lauf
        /// (<c>Vdi6007Rechenweg</c>), das Referenzjahr des Laufs.
        /// </summary>
        /// <exception cref="ArgumentException">Ein Kalender der Oberfläche ist unvollständig oder ungültig.</exception>
        internal static Konditionierungsarbeitsstand Arbeitsstand(KonditionierungStand s, Kalendereigentuemer art,
                                                                  Bezug bezug = null)
        {
            bezug ??= Bezug.Katalog;
            GebaeudeKatalogDaten g = s.Gebaeude;
            bool kuehlung = bezug.Kuehlbetrieb && g.KuehlungAktiv && g.KuehlSollwert.HasValue;
            bool kopplung = Waermeuebergabe.KopplungWirksamFuer(g.HeizkreisAktiv, g.UebergabeArt, bezug.Stufe);
            Konditionierungsstand gebaeude = Ebene(g.Konditionierung, art, Bestand(g, kopplung, kuehlung));
            var zonen = new List<Konditionierungszone>();
            foreach (ZoneDaten z in s.Zonen ?? Array.Empty<ZoneDaten>())
                zonen.Add(new Konditionierungszone(z.Id, z.Bezeichner ?? "", z.Nutzflaeche, z.IstBeheizt,
                                                   Ebene(z.Konditionierung, Kalendereigentuemer.Zone, Bestand(z))));
            return new Konditionierungsarbeitsstand(gebaeude, zonen, g.WohnflaecheGesamt, bezug.Referenzjahr);
        }

        /// <summary>
        /// <b>Der neue Stand der Oberfläche</b> nach einem Schritt: eine Kopie des alten, in der jede
        /// geänderte Ebene ihre Felder, ihre Konditionierung und eine um eins erhöhte Fassung trägt; die
        /// übrigen Zonen bekommen nur den Zustand „vom Gebäude" neu.
        /// </summary>
        internal static KonditionierungStand Stand(KonditionierungStand alt, Konditionierungsarbeitsstand vor,
                                                   Konditionierungsarbeitsstand neu)
        {
            KonditionierungStand s = alt.Kopie();
            if (!vor.Gebaeude.Gleich(neu.Gebaeude, mitBestand: true))
            {
                Zurueck(s.Gebaeude, vor.Gebaeude.Bestand, neu.Gebaeude.Bestand);
                s.Gebaeude.Konditionierung = Daten(neu.Gebaeude, null, (alt.Gebaeude.Konditionierung?.Fassung ?? 0) + 1);
            }
            foreach (ZoneDaten z in s.Zonen)
            {
                Konditionierungszone kv = vor.Zone(z.Id), kn = neu.Zone(z.Id);
                if (kv == null || kn == null) continue;
                bool geaendert = !kv.Stand.Gleich(kn.Stand, mitBestand: true);
                if (geaendert) Zurueck(z, kv.Stand.Bestand, kn.Stand.Bestand);
                int fassung = z.Konditionierung?.Fassung ?? 0;
                if (geaendert || z.Konditionierung != null)
                    z.Konditionierung = Daten(kn.Stand, neu.Gebaeude, geaendert ? fassung + 1 : fassung);
            }
            return s;
        }

        /// <summary>Ein Befund des Kerns an der Oberfläche.</summary>
        internal static KonditionierungRueckfrage Rueckfrage(Konditionierungsbilanz b)
            => b == null
                ? null
                : new KonditionierungRueckfrage(
                    b.Ersetzt.Select(p => new KonditionierungPosten((KonditionierungPostenart)(int)p.Art, p.Anzahl)).ToList(),
                    b.Bleibt.Select(p => new KonditionierungPosten((KonditionierungPostenart)(int)p.Art, p.Anzahl)).ToList(),
                    b.Zonen.ToList());

        private static Konditionierungsort Ort(KonditionierungOrt o) => new Konditionierungsort(Kern(o.Groesse), o.Zone);

        /// <summary>
        /// Ein reiner Schritt am Stand der Oberfläche: übersetzen, rechnen, zurück übersetzen. Eine
        /// Rückfrage des Kerns (F5) kommt als <see cref="KonditionierungErgebnis.Rueckfrage"/>, ein
        /// ungültiger Stand als benannte Ablehnung.
        /// </summary>
        internal static KonditionierungErgebnis Schritt(KonditionierungStand stand, Kalendereigentuemer art,
                                                        Func<Konditionierungsarbeitsstand, Konditionierungsschritt> schritt)
            => Schritt(stand, art, null, schritt);

        /// <summary>Derselbe Schritt mit dem Bezug des Projekts (<see cref="Projektbezug"/>).</summary>
        internal static KonditionierungErgebnis Schritt(KonditionierungStand stand, Kalendereigentuemer art, Bezug bezug,
                                                        Func<Konditionierungsarbeitsstand, Konditionierungsschritt> schritt)
        {
            if (stand?.Gebaeude == null)
                return KonditionierungErgebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ARBEITSSTAND_UNGUELTIG, "—"));
            try
            {
                Konditionierungsarbeitsstand a = Arbeitsstand(stand, art, bezug);
                Konditionierungsschritt s = schritt(a);
                if (s.Rueckfrage) return KonditionierungErgebnis.Frage(Rueckfrage(s.Bilanz));
                if (!s.Ok) return KonditionierungErgebnis.Fehler(s.Meldung);
                return KonditionierungErgebnis.Gut(Stand(stand, a, s.Stand));
            }
            catch (ArgumentException ex)
            {
                return KonditionierungErgebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ARBEITSSTAND_UNGUELTIG, ex.Message));
            }
        }

        /// <summary>
        /// <b>„Nutzungsprofil übernehmen…" über dem Arbeitsstand</b> (Stufe NP3b; Konzept Nutzungsprofile 6.2): übersetzen,
        /// <see cref="RaumnutzungCtrl.ProfilAnwenden"/> rechnen, zurück übersetzen; an einer Zone trägt der neue Stand den
        /// Profilnamen (<see cref="ZoneDaten.Nutzungsprofil"/>, NP-F14) — auch ohne einen Kalender (NP-F13). Geschrieben
        /// wird nichts.
        /// </summary>
        internal static KonditionierungProfilergebnis Profilschritt(KonditionierungStand stand, Kalendereigentuemer art, Bezug bezug,
                                                                   KonditionierungProfilanfrage anfrage,
                                                                   Func<long, Raumnutzungsprofil> profile)
        {
            if (stand?.Gebaeude == null || anfrage == null)
                return KonditionierungProfilergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ARBEITSSTAND_UNGUELTIG, "—"));
            Raumnutzungsprofil p = profile(anfrage.IdProfil);
            if (p == null)
                return KonditionierungProfilergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.RAUMNUTZUNG_MSG_PROFIL_FEHLT, anfrage.IdProfil.ToString(CultureInfo.InvariantCulture)));
            try
            {
                Konditionierungsarbeitsstand a = Arbeitsstand(stand, art, bezug);
                RaumnutzungCtrl.Anwendung an = RaumnutzungCtrl.ProfilAnwenden(a, p, anfrage.Zone, anfrage.Flaeche, anfrage.LichteHoehe);
                if (!an.Ok) return KonditionierungProfilergebnis.Fehler(an.Meldung);
                KonditionierungStand neu = Stand(stand, a, an.Stand);
                string name = KonditionierungNutzungSchema.Nutzungstext(p.Bezeichner);
                if (anfrage.Zone is int zone && neu.Zonen.FirstOrDefault(z => z.Id == zone) is ZoneDaten ziel)
                    ziel.Nutzungsprofil = name;
                RaumnutzungTexte t = RaumnutzungHuelle.Texte();
                List<KonditionierungProfilposten> posten = an.Posten.Select(x => new KonditionierungProfilposten(
                    Oberflaeche(x.Groesse), x.Weg != Raumnutzungsweg.Keiner, x.Uebernommen, x.Ersetzt, x.Unbeheizt,
                    x.Nennwert.HasValue ? x.Nennwertherleitung ?? "" : "", RaumnutzungHuelle.Hinweistext(x.Hinweis, t))).ToList();
                return new KonditionierungProfilergebnis(true, "", neu, name ?? p.Bezeichner ?? "", posten, an.Aufgeteilt, p.IstLeer);
            }
            catch (ArgumentException ex)
            {
                return KonditionierungProfilergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ARBEITSSTAND_UNGUELTIG, ex.Message));
            }
        }

        // =================================================================================
        // Lesen: die Konditionierung beim Öffnen
        // =================================================================================

        /// <summary>
        /// <b>Die Konditionierung eines Eigentümers beim Öffnen</b> (Fassung 0); <c>null</c> ohne die
        /// Tabellen der Konditionierung — dann nennt der Reiter seinen Grund.
        /// </summary>
        internal static KonditionierungDaten Lesen(KonditionierungCtrl.Eigner eigner, Konditionierungsstand gebaeude = null)
        {
            if (eigner == null || !KonditionierungSchema.Lesbar()) return null;
            Konditionierungsstand e = new KonditionierungCtrl().StandLesen(eigner, out _);
            return Daten(e, gebaeude, 0);
        }

        /// <summary>Die Konditionierung eines neuen Satzes: leer, alle Kalender aus der Matrix; <c>null</c> ohne Tabellen.</summary>
        internal static KonditionierungDaten Leer() => KonditionierungSchema.Lesbar() ? new KonditionierungDaten() : null;

        // =================================================================================
        // Schreiben: die Ebene für den OK-Weg
        // =================================================================================

        /// <summary>
        /// <b>Die Ebene, die der OK-Weg schreibt</b> — nur bei geänderter Fassung (&gt; 0); sonst <c>null</c>,
        /// und die Tabellen bleiben, wie sie sind („ohne Änderung schreibt der OK-Weg wie heute").
        /// <paramref name="immer"/>: auch ohne Änderung — „Speichern unter" nimmt die Konditionierung des
        /// Arbeitsstands mit (Festlegung 2).
        /// </summary>
        internal static Konditionierungsstand Schreibstand(GebaeudeKatalogDaten g, Kalendereigentuemer art, bool immer = false)
        {
            if (g?.Konditionierung == null || (!immer && g.Konditionierung.Fassung <= 0)) return null;
            return Ebene(g.Konditionierung, art, Bestand(g, false, g.KuehlungAktiv && g.KuehlSollwert.HasValue));
        }

        /// <summary>Die Ebenen der Zonen für den Schritt 3 des OK-Wegs — nur die mit geänderter Fassung.</summary>
        internal static IReadOnlyDictionary<int, Konditionierungsstand> Zonenstaende(IReadOnlyList<ZoneDaten> zonen)
        {
            var d = new Dictionary<int, Konditionierungsstand>();
            foreach (ZoneDaten z in zonen ?? Array.Empty<ZoneDaten>())
                if (z?.Konditionierung != null && z.Konditionierung.Fassung > 0)
                    d[z.Id] = Ebene(z.Konditionierung, Kalendereigentuemer.Zone, Bestand(z));
            return d;
        }

        // =================================================================================
        // Der Weg
        // =================================================================================

        /// <summary>
        /// <b>Der Weg der Konditionierung</b> für den Editor — je Handlung ein Delegat über den reinen
        /// Schritt. <paramref name="art"/> ist <see cref="Kalendereigentuemer.Gebaeude"/> im Projekt, sonst
        /// <see cref="Kalendereigentuemer.Katalogbau"/>; <paramref name="idGebaeude"/> &gt; 0 nur im Projekt
        /// — dann gibt es „aus dem Katalog erneut übernehmen" und die Rückfrage von „Speichern unter".
        /// Ohne die Tabellen der Konditionierung steht der Weg gesperrt da (<see cref="KonditionierungWeg.Sperre"/>).
        /// <paramref name="idProjekt"/> gibt im Projekt den Bezug (<see cref="Projektbezug"/>): Kopplung,
        /// Referenzjahr und Kühlbetrieb wie im Lauf (Stufe KP2, Welle U1, Teilschritt 4 (c)).
        /// </summary>
        internal static KonditionierungWeg Weg(Kalendereigentuemer art, int idGebaeude, int idProjekt = 0)
        {
            if (!KonditionierungSchema.Lesbar())
                return new KonditionierungWeg { Sperre = MyResource.Resource.KOND_TXT_GRUND_OHNE_TABELLEN };
            bool projekt = art == Kalendereigentuemer.Gebaeude && idGebaeude > 0;
            Bezug bezug = Projektbezug(projekt ? idProjekt : 0);
            Func<KonditionierungStand, KonditionierungErgebnis> katalog = projekt
                ? s => Schritt(s, art, bezug, a => Konditionierungsarbeit.KatalogErneut(
                    a, GebaeudeStammCtrl.KatalogebeneDerKopie(idGebaeude, out _)))
                : null;
            return Bauen(art, bezug, projekt, projekt ? idGebaeude : 0, new KonditionierungsvorlageCtrl(), katalog,
                         RaumnutzungCtrl.Lesbar() ? id => new RaumnutzungCtrl().ProfilLesen(id) : null);
        }

        /// <summary>
        /// <b>Der Weg ohne Datenbank</b> — allein die reinen Schritte über dem Arbeitsstand: Zellen,
        /// Kalender, Werkzeuge der Karte, Rückfragebefunde, Vorschau, Lasten und Prüfung. „Aus dem Katalog
        /// erneut übernehmen…" fehlt (es liest die Datenbank), die Vorlagen ebenso — es sei denn, der
        /// Prüfstand reicht eine Ablage ohne Datenbank (<see cref="Konditionierungsvorlagenablage"/>, Stufe
        /// KP2, Welle U2) — „kein Delegat, kein Knopf". Für Prüfstände ohne Datenbank (die
        /// Konditionierungsprobe des Wirts, die bunit-Proben); <paramref name="katalogErneut"/> setzt ein
        /// Prüfstand, der den Knopf zeigen will.
        /// </summary>
        /// <param name="art">Gebäude (Projekt) oder Katalogbau.</param>
        /// <param name="projekt">Steht der Editor im Projekt? Dann fragt „Speichern unter" nach den Zonen.</param>
        /// <param name="bezug">Der Bezug des Projekts; <c>null</c> = der des Katalogs.</param>
        /// <param name="katalogErneut">„Aus dem Katalog erneut übernehmen…" des Prüfstands; <c>null</c> = keiner.</param>
        /// <param name="vorlagen">Die Vorlagen des Prüfstands (etwa <see cref="Konditionierungsvorlagenablage.AusSaat"/>); <c>null</c> = keine.</param>
        /// <param name="profile">Die Nutzungsprofile des Prüfstands samt Kategorie (Stufe NP3b); <c>null</c> = ohne „Nutzungsprofil übernehmen…".</param>
        internal static KonditionierungWeg ReinerWeg(Kalendereigentuemer art, bool projekt, Bezug bezug = null,
                                                     Func<KonditionierungStand, KonditionierungErgebnis> katalogErneut = null,
                                                     IKonditionierungsvorlagen vorlagen = null,
                                                     IReadOnlyList<(string Kategorie, Raumnutzungsprofil Profil)> profile = null)
            => Bauen(art, bezug ?? Bezug.Katalog, projekt, 0, vorlagen, katalogErneut,
                     profile == null ? null : id => profile.Select(x => x.Profil).FirstOrDefault(p => p.Id == id),
                     profile == null ? null : () => profile.Select(x => RaumnutzungHuelle.Wahl(x.Kategorie, x.Profil, RaumnutzungHuelle.Texte())).ToList());

        /// <summary>
        /// Baut das Bündel: je Handlung ein Delegat über den reinen Schritt. <paramref name="vorlagen"/>
        /// <c>null</c> = ohne die Wege der Vorlagen (ohne Datenbank).
        /// </summary>
        /// <param name="profile">Der Leseweg der Nutzungsprofile (Stufe NP3b); <c>null</c> = ohne „Nutzungsprofil übernehmen…".</param>
        private static KonditionierungWeg Bauen(Kalendereigentuemer art, Bezug bezug, bool projekt, int idGebaeude,
                                                IKonditionierungsvorlagen vorlagen,
                                                Func<KonditionierungStand, KonditionierungErgebnis> katalogErneut,
                                                Func<long, Raumnutzungsprofil> profile = null,
                                                Func<IReadOnlyList<KonditionierungProfilwahl>> profilliste = null)
        {
            return new KonditionierungWeg
            {
                ZelleSetzen = (s, o, z, c) => Schritt(s, art, bezug, a => Konditionierungsarbeit.ZelleSetzen(
                    a, Ort(o), Zeile(z), Zelle(c, Anteil(Kern(o.Groesse), Zeile(z))))),
                Anlegen = (s, o) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Anlegen(a, Ort(o))),
                Verwerfen = (s, o) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Verwerfen(a, Ort(o))),
                MatrixErneut = (s, o) => Schritt(s, art, bezug, a => Konditionierungsarbeit.MatrixErneut(a, Ort(o))),
                KatalogErneut = katalogErneut,
                LuftwechselAufteilen = s => Schritt(s, art, bezug, Konditionierungsarbeit.LuftwechselAufteilen),
                // Stufe KP2, Welle U4: die Zonenmatrix - „vom Gebäude übernehmen und anpassen" und die
                // Platzhalter der geerbten Zellen.
                VomGebaeude = (s, o) => Schritt(s, art, bezug, a => Konditionierungsarbeit.VomGebaeudeUebernehmen(a, Ort(o))),
                Geerbt = (s, zone) => Geerbt(s, art, bezug, zone),

                // Stufe NP3b (Konzept Nutzungsprofile 6.2): „Nutzungsprofil übernehmen…" in den Arbeitsstand.
                Nutzungsprofile = profile == null ? null : profilliste ?? RaumnutzungHuelle.Profilwahl,
                ProfilUebernehmen = profile == null ? null : (s, anfrage) => Profilschritt(s, art, bezug, anfrage, profile),

                Vorlagen = vorlagen == null ? null : g => vorlagen.Liste(Kern(g)).Select(VorlageDaten).ToList(),
                VorlageUebernehmen = vorlagen == null ? null : (s, o, id) => Schritt(s, art, bezug, a =>
                {
                    Konditionierungsvorlage v = vorlagen.Inhalt(id, out string m);
                    return v == null ? Konditionierungsschritt.Fehler(m) : Konditionierungsarbeit.VorlageUebernehmen(a, Ort(o), v);
                }),
                AlsVorlageSpeichern = vorlagen == null ? null : (s, o, e) => AlsVorlage(vorlagen, s, art, bezug, o, e),
                VorlageUmbenennen = vorlagen == null ? null : (id, name) =>
                {
                    // Die Namensregel zuerst: Ihre Ablehnung nennt der Dialog AM FELD (Teilkonzept 7.4).
                    KonditionierungsvorlageCtrl.Vorlage alt = vorlagen.Lesen(id);
                    string regel = alt == null || alt.Ausgeliefert ? null : vorlagen.NamePruefen(alt.Groesse, name, id);
                    if (regel != null) return new KonditionierungVorlageErgebnis(false, regel, null) { AmNamen = true };
                    KonditionierungCtrl.Ergebnis e = vorlagen.Umbenennen(id, name);
                    return new KonditionierungVorlageErgebnis(e.Ok, e.Meldung, e.Ok ? VorlageDaten(vorlagen.Lesen(id)) : null);
                },
                VorlageLoeschen = vorlagen == null ? null : id =>
                {
                    KonditionierungCtrl.Ergebnis e = vorlagen.Loeschen(id);
                    return new KonditionierungVorlageErgebnis(e.Ok, e.Meldung, null);
                },
                VorlageDuplizieren = vorlagen == null ? null : (id, name) =>
                {
                    KonditionierungCtrl.Ergebnis e = vorlagen.Duplizieren(id, name, out long neu);
                    return new KonditionierungVorlageErgebnis(e.Ok, e.Meldung, e.Ok ? VorlageDaten(vorlagen.Lesen(neu)) : null);
                },
                // „Kopieren nach …" (Teilkonzept 3.5, 7.4): die Ziele und die Kopie nach Vorlagenkopierregel.
                Kopierziele = vorlagen == null ? null : g => Kopierzielliste(g),
                VorlageKopieren = vorlagen == null ? null : (id, k) => Kopieren(vorlagen, id, k),

                Grundangabe = (s, o, w) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Grundangabe(
                    a, Ort(o), w.HasValue ? Skaliert(w.Value, Konditionierungsgroessen.HatNennwert(Kern(o.Groesse)), false) : (double?)null)),
                Standardwoche = (s, o, w) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Standardwoche(
                    a, Ort(o), w == null ? null : w.Select(v => Skaliert(v, Konditionierungsgroessen.HatNennwert(Kern(o.Groesse)), false)).ToArray())),
                PeriodeSetzen = (s, o, rang, p) => Schritt(s, art, bezug, a => Konditionierungsarbeit.PeriodeSetzen(
                    a, Ort(o), rang, DbWerte.KOND_ARTEN[(int)p.Art], p.Name, p.Von ?? 0, p.Bis ?? 0, p.Feiertagsregel,
                    Angabe(p.Angabe, p.Wert, p.Woche, p.WieWochentag, Konditionierungsgroessen.HatNennwert(Kern(o.Groesse))))),
                RangVerschieben = (s, o, rang, hoeher) => Schritt(s, art, bezug, a => Konditionierungsarbeit.RangVerschieben(a, Ort(o), rang, hoeher)),
                PeriodeLoeschen = (s, o, rang) => Schritt(s, art, bezug, a => Konditionierungsarbeit.PeriodeLoeschen(a, Ort(o), rang)),
                Feiertagsregeln = Feiertagsregeln(),
                SollwertprofilUebernehmen = s => Schritt(s, art, bezug, Konditionierungsarbeit.SollwertprofilUebernehmen),
                Zeitfenster = (s, o, f) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Zeitfenster(
                    a, Ort(o), f.Tage, f.Von, f.Bis,
                    f.Wert.HasValue ? Skaliert(f.Wert.Value, Konditionierungsgroessen.HatNennwert(Kern(o.Groesse)), false) : (double?)null)),
                Feiertage = (s, o, w) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Feiertage(a, Ort(o), w)),
                Zeitstruktur = (s, o, q) => Schritt(s, art, bezug, a => Konditionierungsarbeit.Zeitstruktur(
                    a, Ort(o), q == KonditionierungZeitstruktur.WieHeizung ? Zeitstrukturquelle.WieHeizung : Zeitstrukturquelle.WieAnwesenheit)),

                Rueckfrage = (s, o, h) => Befund(s, art, bezug, o, h, idGebaeude),
                SpeichernUnterRueckfrage = projekt ? s => SpeichernUnterBefund(s, art, bezug) : null,
                WochenVorschau = Vorschau,
                Teppichbild = (s, o) => Teppich(s, art, bezug, o),
                Bezugsjahr = bezug.Referenzjahr ?? Konditionierungsarbeitsstand.BEZUGSJAHR_VORGABE,
                Lasten = (s, zone) => Lasten(s, art, bezug, zone),
                Pruefen = s => Pruefen(s, art, bezug),
            };
        }

        /// <summary>Die neun Feiertagsregeln mit ihren Anzeigenamen in der Sprache der Oberfläche (F11).</summary>
        private static IReadOnlyList<KonditionierungFeiertag> Feiertagsregeln()
        {
            IReadOnlyList<string> namen = Kalenderwerkzeuge.Feiertagsnamen();
            var liste = new List<KonditionierungFeiertag>();
            for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                liste.Add(new KonditionierungFeiertag(DbWerte.KOND_FEIERTAGE[k], k < namen.Count ? namen[k] : DbWerte.KOND_FEIERTAGE[k]));
            return liste;
        }

        private static KonditionierungVorlageDaten VorlageDaten(KonditionierungsvorlageCtrl.Vorlage v)
            => v == null
                ? null
                : new KonditionierungVorlageDaten(v.Id, Oberflaeche(v.Groesse), v.Bezeichner ?? "", v.Beschreibung ?? "",
                                                  Nutzung(v.Nutzung), v.Ausgeliefert);

        private static string Nutzung(string wert) => KonditionierungNutzungSchema.Nutzungstext(wert);

        /// <summary>
        /// „Als Vorlage speichern…" — der Inhalt aus dem Arbeitsstand (E54), geschrieben sofort (Festlegung
        /// 13). Die Namensregel läuft zuerst: Ihre Ablehnung (etwa ein Doppelname in der Liste) nennt der
        /// Dialog am Feld (<see cref="KonditionierungVorlageErgebnis.AmNamen"/>).
        /// </summary>
        private static KonditionierungVorlageErgebnis AlsVorlage(IKonditionierungsvorlagen vorlagen, KonditionierungStand s,
                                                                 Kalendereigentuemer art, Bezug bezug, KonditionierungOrt o,
                                                                 KonditionierungVorlageEingabe eingabe)
        {
            string regel = vorlagen.NamePruefen(Kern(o.Groesse), eingabe?.Name, 0);
            if (regel != null) return new KonditionierungVorlageErgebnis(false, regel, null) { AmNamen = true };
            try
            {
                Ebenenergebnis inhalt = Konditionierungsarbeit.AlsVorlage(Arbeitsstand(s, art, bezug), Ort(o));
                if (!inhalt.Ok) return new KonditionierungVorlageErgebnis(false, inhalt.Meldung, null);
                KonditionierungCtrl.Ergebnis e = vorlagen.SpeichernAus(inhalt.Stand, Kern(o.Groesse), eingabe?.Name,
                                                                       eingabe?.Beschreibung, Nutzung(eingabe?.Nutzung),
                                                                       out long id);
                return new KonditionierungVorlageErgebnis(e.Ok, e.Meldung, e.Ok ? VorlageDaten(vorlagen.Lesen(id)) : null);
            }
            catch (ArgumentException ex)
            {
                return new KonditionierungVorlageErgebnis(false, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ARBEITSSTAND_UNGUELTIG, ex.Message), null);
            }
        }

        /// <summary>
        /// Die erlaubten Ziele von „Kopieren nach …" für Vorlagen der Größe <paramref name="quelle"/>
        /// (<see cref="Vorlagenkopierregel.Ziele"/>) — bei Heizen → Kühlen mit den Vorgaben des Komfort- und des
        /// Absenksollwerts und den Grenzen der Kühlspalte.
        /// </summary>
        private static IReadOnlyList<KonditionierungKopierziel> Kopierzielliste(KonditionierungGroesse quelle)
        {
            Konditionierungsgroesse von = Kern(quelle);
            return Vorlagenkopierregel.Ziele(von)
                .Select(z => new KonditionierungKopierziel(
                    Oberflaeche(z),
                    Vorlagenkopierregel.MitKomfortsollwert(von, z) ? Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE : (double?)null,
                    Vorlagenkopierregel.KomfortsollwertMin, Vorlagenkopierregel.KomfortsollwertMax)
                {
                    Absenksollwert = Vorlagenkopierregel.MitKomfortsollwert(von, z)
                        ? Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE : (double?)null,
                })
                .ToList();
        }

        /// <summary>
        /// „Kopieren nach …" — die Kopie entsteht sofort (Festlegung 13). Die Richtung, die Namensregel in der
        /// Zielliste, der Komfort- und der Absenksollwert laufen zuerst: Eine Ablehnung des Namens nennt der Dialog
        /// am Namensfeld (<see cref="KonditionierungVorlageErgebnis.AmNamen"/>), eine des Komfortsollwerts am
        /// Sollwertfeld (<see cref="KonditionierungVorlageErgebnis.AmSollwert"/>), eine des Absenksollwerts — auch
        /// „unter dem Komfortsollwert" — am Absenkfeld (<see cref="KonditionierungVorlageErgebnis.AmAbsenkwert"/>).
        /// </summary>
        private static KonditionierungVorlageErgebnis Kopieren(IKonditionierungsvorlagen vorlagen, long id,
                                                               KonditionierungVorlageKopie kopie)
        {
            KonditionierungsvorlageCtrl.Vorlage quelle = vorlagen.Lesen(id);
            if (quelle == null || kopie == null)
                return new KonditionierungVorlageErgebnis(false, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_FEHLT, id.ToString(CultureInfo.InvariantCulture)), null);
            Konditionierungsgroesse ziel = Kern(kopie.Ziel);
            string richtung = Vorlagenkopierregel.Richtungspruefung(quelle.Groesse, ziel);
            if (richtung != null) return new KonditionierungVorlageErgebnis(false, richtung, null);
            string name = vorlagen.NamePruefen(ziel, kopie.Name, 0);
            if (name != null) return new KonditionierungVorlageErgebnis(false, name, null) { AmNamen = true };
            if (Vorlagenkopierregel.MitKomfortsollwert(quelle.Groesse, ziel))
            {
                if (Vorlagenkopierregel.KomfortsollwertPruefen(kopie.Komfortsollwert) is string soll)
                    return new KonditionierungVorlageErgebnis(false, soll, null) { AmSollwert = true };
                if (Vorlagenkopierregel.AbsenksollwertPruefen(kopie.Absenksollwert, kopie.Komfortsollwert) is string absenk)
                    return new KonditionierungVorlageErgebnis(false, absenk, null) { AmAbsenkwert = true };
            }

            KonditionierungCtrl.Ergebnis e = vorlagen.KopierenNach(id, ziel, kopie.Name, kopie.Komfortsollwert,
                                                                   kopie.Absenksollwert, out long neu);
            return new KonditionierungVorlageErgebnis(e.Ok, e.Meldung, e.Ok ? VorlageDaten(vorlagen.Lesen(neu)) : null);
        }

        /// <summary>
        /// <b>Was eine Zone vom Gebäude erbt</b> (Stufe KP2, Welle U4; Teilkonzept 3.4, 7.3) — die Zellen
        /// der Erbmatrix (<see cref="Konditionierungsarbeitsstand.Erbmatrix"/>), JEDE mit ihrem Wert, auch
        /// eine Bestandszelle, Anteile in Prozent; je Größe „vom Gebäude", wo das Gebäude einen Kalender
        /// angelegt hat, sonst „aus der Matrix". <c>null</c>, wenn es die Zone nicht gibt oder der Stand
        /// ungültig ist.
        /// </summary>
        internal static KonditionierungDaten Geerbt(KonditionierungStand s, Kalendereigentuemer art, Bezug bezug, int zone)
        {
            if (s?.Gebaeude == null) return null;
            try
            {
                Konditionierungsarbeitsstand a = Arbeitsstand(s, art, bezug);
                if (a.Zone(zone) == null) return null;
                Vorgabematrix m = a.Erbmatrix(zone);
                var d = new KonditionierungDaten();
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    KonditionierungSpalte spalte = d.Spalte(Oberflaeche(g));
                    foreach (KonditionierungZeile z in ZEILEN)
                        Setzen(spalte, z, Zelle(m.Spalte(g).Zeile(Zeile(z)), Anteil(g, Zeile(z))));
                    spalte.Kalender = new KonditionierungKalender
                    {
                        Zustand = a.Gebaeude.Kalender(g) != null ? KonditionierungZustand.VomGebaeude : KonditionierungZustand.Abgeleitet
                    };
                }
                return d;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Der Rückfragebefund VOR einer Handlung (Festlegung 3); <c>null</c> = keine Rückfrage nötig.</summary>
        private static KonditionierungRueckfrage Befund(KonditionierungStand s, Kalendereigentuemer art, Bezug bezug,
                                                        KonditionierungOrt o,
                                                        KonditionierungHandlung h, int idGebaeude)
        {
            Konditionierungshandlung? k = h switch
            {
                KonditionierungHandlung.MatrixErneut => Konditionierungshandlung.MatrixErneut,
                KonditionierungHandlung.VorlageUebernehmen => Konditionierungshandlung.VorlageUebernehmen,
                KonditionierungHandlung.Verwerfen => Konditionierungshandlung.Verwerfen,
                KonditionierungHandlung.KatalogErneut => Konditionierungshandlung.KatalogErneut,
                _ => null,
            };
            if (!k.HasValue || s?.Gebaeude == null) return null;
            if (k == Konditionierungshandlung.KatalogErneut && idGebaeude <= 0) return null;
            try
            {
                Konditionierungsstand katalog = k == Konditionierungshandlung.KatalogErneut
                    ? GebaeudeStammCtrl.KatalogebeneDerKopie(idGebaeude, out _)
                    : null;
                return Rueckfrage(Konditionierungsarbeit.Rueckfrage(Arbeitsstand(s, art, bezug), o == null ? null : Ort(o), k.Value, katalog));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Die Rückfrage von „Speichern unter" im Projekt: Zonen, Bauteile, Konditionierung zusammen.</summary>
        private static KonditionierungRueckfrage SpeichernUnterBefund(KonditionierungStand s, Kalendereigentuemer art,
                                                                      Bezug bezug)
        {
            if (s?.Gebaeude == null) return null;
            try
            {
                int bauteile = (s.Zonen ?? Array.Empty<ZoneDaten>()).Sum(z => z.Bauteile?.Count ?? 0);
                return Rueckfrage(Konditionierungsarbeit.RueckfrageSpeichernUnter(Arbeitsstand(s, art, bezug), bauteile));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Die Vorschau einer Woche (168 Werte in der Einheit der Spalte, NaN = „aus") — mit dem Titel
        /// ihrer Größe („Vorschau: Woche · Kühlen", Stufe KP2, Welle U1, Teilschritt 4 (d)); die Achse
        /// der Stunden teilt sie mit dem Zeitprogramm der Wärmeübergabe.
        /// </summary>
        private static WindowsFormsApplication1.Zeichnung.Zeichenmodell Vorschau(KonditionierungGroesse g, double[] werte)
        {
            if (werte == null || werte.Length != Kalenderwoche.WOCHENWERTE) return null;
            WaermeuebergabeTexte u = GebaeudeKatalogHuelle.UebergabeTexte();
            string einheit = g switch
            {
                KonditionierungGroesse.Lueftung => "1/h",
                KonditionierungGroesse.Geraete or KonditionierungGroesse.Personen => "%",
                _ => "°C",
            };
            string groesse = g switch
            {
                KonditionierungGroesse.Heizen => MyResource.Resource.KOND_LBL_GROESSE_HEIZEN,
                KonditionierungGroesse.Kuehlen => MyResource.Resource.KOND_LBL_GROESSE_KUEHLEN,
                KonditionierungGroesse.Lueftung => MyResource.Resource.KOND_LBL_GROESSE_LUEFTUNG,
                KonditionierungGroesse.Geraete => MyResource.Resource.KOND_LBL_GROESSE_GERAETE,
                _ => MyResource.Resource.KOND_LBL_GROESSE_PERSONEN,
            };
            string titel = MyResource.Resource.KOND_LBL_VORSCHAU_WOCHE + " · " + groesse;
            return ChartRenderer.StundenprofilModell(titel, werte, 24, u.Raster.BildAchseX, einheit);
        }

        /// <summary>
        /// <b>Das Teppichbild</b> einer Karte (Teilkonzept 7.5, Entwurf KP2 Festlegung 8; Welle U3): der
        /// Kalender, den der Dialog für die Größe am Ort zeigt (angelegt, sonst der Generator aus der
        /// wirksamen Matrix, <see cref="Konditionierungsarbeitsstand.Ansichtskalender"/>), als
        /// <see cref="Kalenderteppich"/> gegen das Bezugsjahr des Wegs — im Projekt das des Laufs, im
        /// Katalog 2025 — und über den Renderer aus K4 (<see cref="ChartRenderer.KalenderteppichModell"/>)
        /// mit den Texten der Oberflächensprache. Das Bezugsjahr steht im Titel, der <c>data-wert</c> jedes
        /// Felds nennt Zeitraum, Stunden, Wert und Quelle. <c>null</c> = kein Kalender (etwa Personen ohne Anteil).
        /// </summary>
        private static WindowsFormsApplication1.Zeichnung.Zeichenmodell Teppich(KonditionierungStand s, Kalendereigentuemer art,
                                                                                Bezug bezug, KonditionierungOrt o)
        {
            if (s?.Gebaeude == null || o == null) return null;
            try
            {
                Konditionierungsarbeitsstand a = Arbeitsstand(s, art, bezug);
                Konditionierungskalender k = a.Ansichtskalender(Kern(o.Groesse), o.Zone);
                if (k == null) return null;
                return ChartRenderer.KalenderteppichModell(Kalenderteppich.Bilden(k, a.Referenzjahr), null,
                                                           ChartRenderer.KalenderteppichTexte.AusRessourcen());
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Die Herleitung der Lasten (P1) am Gebäude bzw. an einer Zone.</summary>
        private static KonditionierungLasten Lasten(KonditionierungStand s, Kalendereigentuemer art, Bezug bezug, int? zone)
        {
            if (s?.Gebaeude == null) return null;
            try
            {
                Konditionierungsarbeitsstand a = Arbeitsstand(s, art, bezug);
                long? ort = zone;
                Konditionierungszone z = ort.HasValue ? a.Zone(ort.Value) : null;
                if (ort.HasValue && z == null) return null;
                Matrixeingang b = z == null ? a.Gebaeude.Bestand : a.AufgeloesterBestand(z);
                Vorgabematrix m = a.Matrix(ort);
                Matrixzelle pn = m.Personen.Nennwert;
                double personenNenn = pn.Belegt && !pn.Aus
                    ? pn.Wert
                    : Konditionierungsarbeit.PersonenNennwertVorschlag(b.Bewohner, 0.0, null);
                double personenMittel = Konditionierungsarbeit.PersonenJahresmittelW(
                    a.GeltenderKalender(Konditionierungsgroesse.Personen, ort), a.W0, a.Referenzjahr);
                double geraeteNenn = b.InterneWaermegewinne ?? 0.0;
                Konditionierungskalender gk = a.GeltenderKalender(Konditionierungsgroesse.Geraete, ort);
                double geraeteMittel = geraeteNenn;
                if (gk != null)
                {
                    double[] anteil = gk.Auswerten(a.W0, a.Referenzjahr);
                    double summe = 0.0;
                    foreach (double v in anteil) summe += v;
                    geraeteMittel = summe * (gk.Nennwert ?? geraeteNenn) / anteil.Length;
                }
                return new KonditionierungLasten(b.Bewohner, Matrixeingang.PERSON_W, personenNenn, personenMittel,
                                                 geraeteNenn, geraeteMittel, z == null ? a.Nutzflaeche : z.Nutzflaeche);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// <b>Die Prüfregeln des Kerns</b> über die Konditionierung des Stands — Grenzen je Zelle, Rundlauf
        /// jedes Kalenders, eindeutiger Rang, höchstens <see cref="Kalenderregel.PERIODEN_MAX"/> Perioden;
        /// leer = gültig. Dieselben Regeln wie im Schreibweg.
        /// </summary>
        internal static string Pruefen(KonditionierungStand s, Kalendereigentuemer art, Bezug bezug = null)
        {
            if (s?.Gebaeude == null) return "";
            Konditionierungsarbeitsstand a;
            try
            {
                a = Arbeitsstand(s, art, bezug);
            }
            catch (ArgumentException ex)
            {
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_ARBEITSSTAND_UNGUELTIG, ex.Message);
            }
            var ebenen = new List<Konditionierungsstand> { a.Gebaeude };
            ebenen.AddRange(a.Zonen.Select(z => z.Stand));
            foreach (Konditionierungsstand e in ebenen)
            {
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                    foreach (string zeile in DbWerte.KOND_ZEILEN)
                    {
                        Matrixzelle c = e.Vorgabe(g, zeile);
                        if (!Konditionierungsstand.Traegt(c)) continue;
                        string f = Konditionierungsarbeit.Zellenpruefung(g, zeile, c);
                        if (f != null) return f;
                    }
                foreach (Konditionierungskalender k in e.Angelegt().Values)
                {
                    if (!Kalenderleser.Rundlaeuft(k, out int rang, out int stelle))
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                                             Fahrplanbefund.RundlaufVerletzt.ToString(),
                                             "Rang " + rang.ToString(CultureInfo.InvariantCulture) + ", Stelle " +
                                             stelle.ToString(CultureInfo.InvariantCulture));
                    string r = Kalenderwerkzeuge.Rangpruefung(k.Perioden.ToList());
                    if (r != null) return r;
                    if (k.Perioden.Count > Kalenderregel.PERIODEN_MAX)
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_PERIODEN_ZU_VIELE,
                                             k.Perioden.Count.ToString(CultureInfo.InvariantCulture),
                                             Kalenderregel.PERIODEN_MAX.ToString(CultureInfo.InvariantCulture));
                }
            }
            return "";
        }
    }
}
