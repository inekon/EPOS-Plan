using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Woher die Zeilen einer Größe kommen (Konzept Nutzungsprofile 4.3).</summary>
    public enum Raumnutzungsweg
    {
        /// <summary>Die Größe ist nicht belegt (NP-F6): kein Kalender, das Ziel behält seinen.</summary>
        Keiner = 0,

        /// <summary>Das Zeilenbild gilt wörtlich (NP-F7).</summary>
        Zeilenbild = 1,

        /// <summary>Die Stundenprofile ergeben eine Standardwoche (NP-F9).</summary>
        Stundenprofil = 2,

        /// <summary>Die Kennwerte ergeben Tag, Nacht, Wochenende und Ferien (Tabelle 4.3).</summary>
        Kennwerte = 3,
    }

    /// <summary>Was der Generator an einer Größe benennt, ohne abzulehnen.</summary>
    public enum Raumnutzungshinweis
    {
        /// <summary>Nichts.</summary>
        Keiner = 0,

        /// <summary>Außenluft in m³/(h·m²) ohne lichte Höhe des Ziels: die Lüftung wird nicht gesetzt (NP-F10).</summary>
        LueftungOhneHoehe = 1,

        /// <summary>
        /// Ein freier Einzeltag (nicht Sa und So zusammen) bräuchte eine Standardwoche, die Größe trägt aber keinen
        /// Tageswert: die Matrix kennt nur das Wochenende, der freie Tag fällt weg.
        /// </summary>
        FreierTagOhneTageswert = 2,

        /// <summary>Ein Stundenprofil ist nicht lesbar (nicht 24 Werte oder außerhalb der Grenzen); die Kennwerte gelten.</summary>
        StundenprofilUnlesbar = 3,

        /// <summary>Profil ohne einen einzigen Wert (NP-F13): nur der Name geht ans Ziel.</summary>
        ProfilOhneWerte = 4,
    }

    /// <summary>
    /// <b>Das Ergebnis des Generators je Größe</b>: der Weg, die erzeugte Vorlage (Vorgabezeilen und, wo nötig, ein
    /// Kalender mit Standardwoche oder Feiertagsregeln) als Eingabe von
    /// <see cref="Konditionierungsarbeit.VorlageUebernehmen"/>, der Nennwert nach Q39/Q40 samt Herleitung und ein Hinweis.
    /// </summary>
    /// <param name="Groesse">Die Größe.</param>
    /// <param name="Weg">Woher die Zeilen kommen; <see cref="Raumnutzungsweg.Keiner"/> heißt <paramref name="Vorlage"/> = <c>null</c>.</param>
    /// <param name="Vorlage">Die erzeugte Vorlage (Name = Profilname) oder <c>null</c>.</param>
    /// <param name="Nennwert">Der Nennwert [W] (nur Geräte und Personen, nur mit Fläche); sonst <c>null</c>.</param>
    /// <param name="Nennwertherleitung">Die Herleitung des Nennwerts als Formel (<c>8 W/m² × 120 m² = 960 W</c>); sonst <c>null</c>.</param>
    /// <param name="Hinweis">Was benannt wird.</param>
    public sealed record Raumnutzungsgroesse(Konditionierungsgroesse Groesse, Raumnutzungsweg Weg,
                                             Konditionierungsvorlage Vorlage, double? Nennwert,
                                             string Nennwertherleitung, Raumnutzungshinweis Hinweis);

    /// <summary>
    /// <b>Die abgeleiteten Nutzungstage eines Profils</b> (E93) im festen Raster von 365 Tagen — keine Eingabe:
    /// <see cref="Raumnutzungsgenerator.Nutzungstage"/> zählt sie aus Wochenmuster, Feiertagen und Ferien des Ziels.
    /// </summary>
    /// <param name="Wochenmuster">Die Tage des Wochenmusters (<c>Nutzungstage_Woche</c>, leer = alle sieben).</param>
    /// <param name="Feiertage">Die Nutzungstage, die die Feiertage „wie Sonntag" nehmen (Saldo; 0 ohne die Vorgabe).</param>
    /// <param name="Ferientage">Die übrigen Nutzungstage in den Ferienzeiträumen des Ziels; 0 ohne Ziel.</param>
    public readonly record struct Raumnutzungstage(int Wochenmuster, int Feiertage, int Ferientage)
    {
        /// <summary>Die Nutzungstage aus Wochenmuster und Feiertagen — die Zahl des Profils selbst.</summary>
        public int OhneFerien => Wochenmuster - Feiertage;

        /// <summary>Die Nutzungstage am Ziel: ohne Feiertage und ohne Ferientage.</summary>
        public int Tage => OhneFerien - Ferientage;
    }

    /// <summary>
    /// <b>Der Generator der Nutzungsprofile</b> (Konzept Nutzungsprofile 4.3, NP-F1, NP-F6, NP-F7, NP-F9, NP-F10, Q38–Q40):
    /// macht aus einem <see cref="Raumnutzungsprofil"/> je Größe eine <see cref="Konditionierungsvorlage"/> — dieselbe
    /// Form, die eine ausgelieferte Vorlage hat —, die <see cref="Konditionierungsarbeit.VorlageUebernehmen"/> wie jede
    /// Vorlage an Gebäude oder Zone schreibt (Ferien des Ziels, P12, Herkunft). Rechnet nichts, liest keine Datenbank.
    ///
    /// <para><b>Reihenfolge je Größe:</b> Zeilenbild → Stundenprofil → Kennwerte → nichts. Ein Zeilenbild gilt wörtlich;
    /// ein Stundenprofil ergibt eine Standardwoche (Werktag an den Nutzungstagen, sonst der freie Tag); die Kennwerte
    /// ergeben Tag (im Fenster), Nacht (außerhalb, mit Zeiten), Wochenende (Sa und So zugleich frei) und Ferien, jeweils
    /// mit dem Wert „außerhalb"; ist er gleich dem Tageswert, bleibt es bei der Zeile Tag. Ein anderer freier Tag
    /// erzwingt eine Standardwoche. <c>Feiertage_Wie_Sonntag</c> legt — wie die ausgelieferten Vorlagen Büro und
    /// Schule — einen Kalender mit den neun Feiertagsregeln „wie Sonntag" an, sobald die Größe mehr als die Zeile Tag
    /// trägt; seine Grundangabe ist der Wert außerhalb.</para>
    ///
    /// <para><b>Nennwerte</b> nur mit Fläche des Ziels und nur, wenn das Profil den Kennwert trägt: Geräte
    /// (<c>Geraete_Leistung</c> + <c>Beleuchtung_Leistung × Beleuchtung_Anteil</c>) × Fläche (Q38, Q39), Personen
    /// Fläche ÷ <c>Personen_Flaeche</c> × <c>Personen_Waerme</c> (leer = <see cref="Matrixeingang.PERSON_W"/>, Q40). Ohne
    /// Fläche bleibt der Nennwert des Ziels; die EPOS-Muster tragen keinen.</para>
    /// </summary>
    public static class Raumnutzungsgenerator
    {
        private const int STUNDEN = 24;
        private const int TAGE = 7;
        private const int NACHKOMMA = 4;

        /// <summary>Alle fünf Größen in der Ordnung von <see cref="Konditionierungsgroessen.Alle"/>.</summary>
        /// <param name="profil">Das Profil.</param>
        /// <param name="flaeche">Die Fläche des Ziels [m²]; <c>null</c> = ohne (der Nennwert des Ziels bleibt).</param>
        /// <param name="lichteHoehe">Die lichte Höhe des Ziels [m] für Außenluft in m³/(h·m²); <c>null</c> = ohne.</param>
        public static IReadOnlyList<Raumnutzungsgroesse> Erzeugen(Raumnutzungsprofil profil, double? flaeche,
                                                                   double? lichteHoehe = null)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            return Konditionierungsgroessen.Alle.Select(g => Erzeugen(profil, g, flaeche, lichteHoehe)).ToList();
        }

        /// <summary>Eine Größe (siehe <see cref="Erzeugen(Raumnutzungsprofil, double?, double?)"/>).</summary>
        public static Raumnutzungsgroesse Erzeugen(Raumnutzungsprofil profil, Konditionierungsgroesse groesse,
                                                   double? flaeche, double? lichteHoehe = null)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            if (profil.IstLeer)
                return new Raumnutzungsgroesse(groesse, Raumnutzungsweg.Keiner, null, null, null, Raumnutzungshinweis.ProfilOhneWerte);

            double? nennwert = Nennwert(profil, groesse, flaeche, out string herleitung);
            Raumnutzungshinweis hinweis = Raumnutzungshinweis.Keiner;
            string kennwort = Konditionierungsgroessen.Kennwort(groesse);

            // 1. Zeilenbild (NP-F7)
            List<Vorgabezeile> bild = (profil.Zeilen ?? new List<Vorgabezeile>())
                .Where(z => string.Equals(z.Groesse, kennwort, StringComparison.Ordinal)).ToList();
            if (bild.Count > 0)
            {
                Konditionierungsstand s = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
                foreach (Vorgabezeile z in bild)
                    s = s.MitVorgabe(groesse, z.Zeile, Konditionierungsstand.Zelle(z));
                s = MitFeiertagen(s, profil, groesse, null);
                return Ergebnis(profil, groesse, Raumnutzungsweg.Zeilenbild, s, nennwert, herleitung, hinweis);
            }

            // 2. Stundenprofil (NP-F9)
            Raumnutzungsstunden werktag = Stunden(profil, kennwort, RaumnutzungSchema.TAGESART_WERKTAG);
            Raumnutzungsstunden frei = Stunden(profil, kennwort, RaumnutzungSchema.TAGESART_FREI);
            if (werktag != null || frei != null)
            {
                double[] woche = Stundenwoche(profil, groesse, werktag ?? frei, frei ?? werktag, lichteHoehe, out hinweis);
                if (woche != null)
                {
                    Konditionierungsstand s = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
                    s = MitFeiertagen(s, profil, groesse, Kalenderangabe.AusWoche(woche), immer: true);
                    return Ergebnis(profil, groesse, Raumnutzungsweg.Stundenprofil, s, nennwert, herleitung, hinweis);
                }
            }

            // 3. Kennwerte (Tabelle 4.3)
            Konditionierungsstand k = Kennwertstand(profil, groesse, lichteHoehe, ref hinweis);
            if (k != null)
                return Ergebnis(profil, groesse, Raumnutzungsweg.Kennwerte, k, nennwert, herleitung, hinweis);

            // 4. nichts — ein Nennwert allein trägt noch keinen Kalender
            return new Raumnutzungsgroesse(groesse, Raumnutzungsweg.Keiner, null, null, null, hinweis);
        }

        private static Raumnutzungsgroesse Ergebnis(Raumnutzungsprofil p, Konditionierungsgroesse g, Raumnutzungsweg weg,
                                                    Konditionierungsstand inhalt, double? nennwert, string herleitung,
                                                    Raumnutzungshinweis hinweis)
        {
            if (nennwert.HasValue)
                inhalt = inhalt.MitVorgabe(g, DbWerte.KOND_ZEILE_NENNWERT, Matrixzelle.AusWert(nennwert.Value));
            return new Raumnutzungsgroesse(g, weg, new Konditionierungsvorlage(p.Id, p.Bezeichner, g, inhalt, AusProfil: true),
                                           nennwert, herleitung, hinweis);
        }

        // =================================================================
        //  Kennwerte
        // =================================================================

        /// <summary>Tageswert und Wert außerhalb einer Größe; <c>Aus</c> heißt „aus", <c>Wert = null</c> „wie das Ziel".</summary>
        private readonly record struct Angabe(double? Wert, bool Aus)
        {
            public bool Traegt => Aus || Wert.HasValue;
            public Matrixzelle Zelle(int? von = null, int? bis = null)
                => Aus ? Matrixzelle.Abgeschaltet(von, bis) : Matrixzelle.AusWert(Wert.Value, von, bis);
            public double Wochenwert => Aus ? double.NaN : Wert.Value;
            public bool Gleich(Angabe b) => Aus == b.Aus && (Aus || Nullable.Equals(Wert, b.Wert));
        }

        private static Konditionierungsstand Kennwertstand(Raumnutzungsprofil p, Konditionierungsgroesse g, double? hoehe,
                                                           ref Raumnutzungshinweis hinweis)
        {
            Angabe tag, ausser;
            int? von, bis;
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll:
                    if (p.Heiz_Soll == null && p.Heiz_Soll_Ausserhalb == null && p.Heiz_Aus_Ausserhalb != true) return null;
                    tag = new Angabe(p.Heiz_Soll, false);
                    ausser = p.Heiz_Aus_Ausserhalb == true ? new Angabe(null, true) : new Angabe(p.Heiz_Soll_Ausserhalb ?? p.Heiz_Soll, false);
                    (von, bis) = Betriebsfenster(p);
                    break;
                case Konditionierungsgroesse.Kuehlsoll:
                    if (p.Kuehl_Soll == null && p.Kuehl_Soll_Ausserhalb == null && p.Kuehl_Aus_Ausserhalb != true) return null;
                    tag = new Angabe(p.Kuehl_Soll, false);
                    ausser = p.Kuehl_Aus_Ausserhalb == true ? new Angabe(null, true) : new Angabe(p.Kuehl_Soll_Ausserhalb ?? p.Kuehl_Soll, false);
                    (von, bis) = Betriebsfenster(p);
                    break;
                case Konditionierungsgroesse.Lueftung:
                    if (p.Aussenluft == null && p.Aussenluft_Ausserhalb == null) return null;
                    double faktor = Lueftungsfaktor(p, hoehe);
                    if (double.IsNaN(faktor))
                    {
                        hinweis = Raumnutzungshinweis.LueftungOhneHoehe;
                        return null;
                    }
                    tag = new Angabe(Gerundet(p.Aussenluft * faktor), false);
                    ausser = new Angabe(Gerundet((p.Aussenluft_Ausserhalb ?? p.Aussenluft) * faktor), false);
                    (von, bis) = Betriebsfenster(p);
                    break;
                case Konditionierungsgroesse.Geraete:
                    if (p.Geraete_Anteil == null && p.Geraete_Anteil_Ausserhalb == null && p.Geraete_Leistung == null &&
                        p.Beleuchtung_Leistung == null) return null;
                    tag = new Angabe(p.Geraete_Anteil ?? 1.0, false);
                    ausser = new Angabe(p.Geraete_Anteil_Ausserhalb ?? tag.Wert, false);
                    (von, bis) = (p.Nutzung_Von, p.Nutzung_Bis);
                    break;
                case Konditionierungsgroesse.Personen:
                    if (p.Personen_Anteil == null && p.Personen_Anteil_Ausserhalb == null && p.Personen_Flaeche == null) return null;
                    tag = new Angabe(p.Personen_Anteil ?? 1.0, false);
                    ausser = new Angabe(p.Personen_Anteil_Ausserhalb ?? 0.0, false);
                    (von, bis) = (p.Nutzung_Von, p.Nutzung_Bis);
                    break;
                default:
                    return null;
            }

            Konditionierungsstand s = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
            if (tag.Traegt) s = s.MitVorgabe(g, DbWerte.KOND_ZEILE_TAG, tag.Zelle());
            if (!ausser.Traegt || (tag.Traegt && ausser.Gleich(tag))) return s;     // durchgehend: nur die Zeile Tag

            bool[] genutzt = Wochenmuster(p);
            bool ganztags = Ganztags(von, bis);
            if (!ganztags)
                s = s.MitVorgabe(g, DbWerte.KOND_ZEILE_NACHT, ausser.Zelle(Stunde(bis), Stunde(von)));
            if (!genutzt[5] && !genutzt[6])
                s = s.MitVorgabe(g, DbWerte.KOND_ZEILE_WOCHENENDE, ausser.Zelle());
            s = s.MitVorgabe(g, DbWerte.KOND_ZEILE_FERIEN, ausser.Zelle());

            // Ein freier Tag, den die Matrix nicht kennt (ein Werktag, ein Samstag oder Sonntag allein), braucht eine Woche.
            bool einzeltag = Enumerable.Range(0, 5).Any(d => !genutzt[d]) || genutzt[5] != genutzt[6];
            Kalenderangabe woche = null;
            if (einzeltag)
            {
                if (!tag.Traegt) hinweis = Raumnutzungshinweis.FreierTagOhneTageswert;
                else
                {
                    var w = new double[Kalenderwoche.WOCHENWERTE];
                    for (int d = 0; d < TAGE; d++)
                        for (int h = 0; h < STUNDEN; h++)
                            w[Kalenderwoche.Stelle(d, h)] = genutzt[d] && ImFenster(h, von, bis) ? tag.Wochenwert : ausser.Wochenwert;
                    woche = Kalenderangabe.AusWoche(w);
                }
            }
            return MitFeiertagen(s, p, g, woche, immer: woche != null);
        }

        /// <summary>Das Betriebsfenster: Betrieb, wo das Profil eins trägt, sonst die Nutzungszeit.</summary>
        private static (int?, int?) Betriebsfenster(Raumnutzungsprofil p)
            => p.Betrieb_Von.HasValue || p.Betrieb_Bis.HasValue ? (p.Betrieb_Von, p.Betrieb_Bis) : (p.Nutzung_Von, p.Nutzung_Bis);

        /// <summary>Faktor der Außenluft auf 1/h: 1 bei <c>1/h</c>, 1/Höhe bei <c>m3/hm2</c>; NaN ohne Höhe.</summary>
        private static double Lueftungsfaktor(Raumnutzungsprofil p, double? hoehe)
        {
            if (!string.Equals(p.Aussenluft_Einheit, RaumnutzungSchema.EINHEIT_JE_FLAECHE, StringComparison.Ordinal)) return 1.0;
            return hoehe.HasValue && hoehe.Value > 0.0 && double.IsFinite(hoehe.Value) ? 1.0 / hoehe.Value : double.NaN;
        }

        private static double? Gerundet(double? w)
            => w.HasValue ? Math.Round(w.Value, NACHKOMMA, MidpointRounding.AwayFromZero) : (double?)null;

        /// <summary>Die Nutzungstage Mo … So; ohne Angabe alle sieben.</summary>
        private static bool[] Wochenmuster(Raumnutzungsprofil p)
        {
            var t = new bool[TAGE];
            string w = p.Nutzungstage_Woche;
            for (int d = 0; d < TAGE; d++) t[d] = w == null || w.Length != TAGE || w[d] != '0';
            return t;
        }

        /// <summary>Ein Fenster ohne Grenzen oder von 0 bis 24 (bzw. Beginn = Ende) ist der ganze Tag.</summary>
        private static bool Ganztags(int? von, int? bis)
            => !von.HasValue || !bis.HasValue || Stunde(von) == Stunde(bis);

        private static int Stunde(int? s) => (s ?? 0) % STUNDEN;

        /// <summary>Liegt die Stunde im Fenster [von, bis) — auch über Mitternacht?</summary>
        private static bool ImFenster(int h, int? von, int? bis)
        {
            if (Ganztags(von, bis)) return true;
            int a = Stunde(von), b = Stunde(bis);
            return a < b ? h >= a && h < b : h >= a || h < b;
        }

        // =================================================================
        //  Stundenprofil
        // =================================================================

        private static Raumnutzungsstunden Stunden(Raumnutzungsprofil p, string groesse, string tagesart)
            => (p.Stunden ?? new List<Raumnutzungsstunden>()).FirstOrDefault(s =>
                   string.Equals(s.Groesse, groesse, StringComparison.Ordinal) &&
                   string.Equals(s.Tagesart, tagesart, StringComparison.Ordinal));

        private static double[] Stundenwoche(Raumnutzungsprofil p, Konditionierungsgroesse g, Raumnutzungsstunden werktag,
                                             Raumnutzungsstunden frei, double? hoehe, out Raumnutzungshinweis hinweis)
        {
            hinweis = Raumnutzungshinweis.Keiner;
            double faktor = 1.0;
            if (g == Konditionierungsgroesse.Lueftung && p.Aussenluft.HasValue)
            {
                faktor = Lueftungsfaktor(p, hoehe);
                if (double.IsNaN(faktor))
                {
                    hinweis = Raumnutzungshinweis.LueftungOhneHoehe;
                    return null;
                }
                faktor *= p.Aussenluft.Value;
            }
            double[] w = Stundenwerte(werktag.Werte, g, faktor), f = Stundenwerte(frei.Werte, g, faktor);
            if (w == null || f == null)
            {
                hinweis = Raumnutzungshinweis.StundenprofilUnlesbar;
                return null;
            }
            bool[] genutzt = Wochenmuster(p);
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int d = 0; d < TAGE; d++)
                for (int h = 0; h < STUNDEN; h++)
                    woche[Kalenderwoche.Stelle(d, h)] = genutzt[d] ? w[h] : f[h];
            return woche;
        }

        /// <summary>
        /// Die 24 Werte eines Stundenprofils (Semikolon, invariant; <c>aus</c> nur bei Heizen und Kühlen), mit
        /// <paramref name="faktor"/> und auf vier Nachkommastellen; <c>null</c>, wenn nicht lesbar oder außerhalb der Grenzen.
        /// </summary>
        public static double[] Stundenwerte(string text, Konditionierungsgroesse g, double faktor = 1.0)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string[] teile = text.Split(';');
            if (teile.Length != STUNDEN) return null;
            var werte = new double[STUNDEN];
            bool ausErlaubt = g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll;
            for (int h = 0; h < STUNDEN; h++)
            {
                string t = teile[h].Trim();
                if (ausErlaubt && string.Equals(t, DbWerte.KOND_WOCHE_AUS, StringComparison.OrdinalIgnoreCase))
                {
                    werte[h] = double.NaN;
                    continue;
                }
                if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) || !double.IsFinite(v))
                    return null;
                v = Math.Round(v * faktor, NACHKOMMA, MidpointRounding.AwayFromZero);
                if (!Konditionierungsgroessen.ImBereich(g, v)) return null;
                werte[h] = v;
            }
            return werte;
        }

        // =================================================================
        //  Feiertage und Kalenderkopf
        // =================================================================

        /// <summary>
        /// Legt den Kalender der Vorlage an — mit <paramref name="woche"/> als Grundangabe, sonst dem Wert außerhalb
        /// (Wochenende, sonst Nacht, sonst Tag; wie <c>KonditionierungsvorlagenSaat.Kalendergrund</c>) — und, bei
        /// <c>Feiertage_Wie_Sonntag</c>, die neun Feiertagsregeln „wie Sonntag" im Feiertagsband. Ohne Woche nur, wenn
        /// die Größe mehr als die Zeile Tag trägt und das Profil die Feiertage will (<paramref name="immer"/> erzwingt
        /// den Kopf für eine Woche).
        /// </summary>
        private static Konditionierungsstand MitFeiertagen(Konditionierungsstand s, Raumnutzungsprofil p,
                                                           Konditionierungsgroesse g, Kalenderangabe woche, bool immer = false)
        {
            bool feiertage = p.Feiertage_Wie_Sonntag == true;
            bool struktur = Konditionierungsstand.Traegt(s.Vorgabe(g, DbWerte.KOND_ZEILE_NACHT))
                            || Konditionierungsstand.Traegt(s.Vorgabe(g, DbWerte.KOND_ZEILE_WOCHENENDE))
                            || Konditionierungsstand.Traegt(s.Vorgabe(g, DbWerte.KOND_ZEILE_FERIEN));
            if (!immer && !(feiertage && struktur)) return s;

            Kalenderangabe grund = woche ?? Grundangabe(s, g);
            if (grund == null) return s;
            var perioden = new List<Kalenderregel>();
            if (feiertage)
            {
                Kalenderangabe wieSonntag = Kalenderangabe.AlsWochentag(KonditionierungsvorlagenSaattabelle.WIE_WOCHENTAG);
                for (int k = 0; k < DbWerte.KOND_FEIERTAGE.Count; k++)
                    perioden.Add(Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG + k,
                                                        KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN[k],
                                                        DbWerte.KOND_FEIERTAGE[k], wieSonntag));
            }
            return s.MitKalender(g, new Konditionierungskalender(g, grund, null, perioden), Kalenderherkunft.Keine);
        }

        private static Kalenderangabe Grundangabe(Konditionierungsstand s, Konditionierungsgroesse g)
        {
            foreach (string zeile in new[] { DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_TAG })
            {
                Matrixzelle z = s.Vorgabe(g, zeile);
                if (z == null || !z.Belegt) continue;
                return z.Aus ? Kalenderangabe.Abgeschaltet : Kalenderangabe.AusWert(z.Wert);
            }
            return null;
        }

        // =================================================================
        //  Nutzungstage im Jahr (E93)
        // =================================================================

        private const int TAGE_IM_JAHR = 365;
        private const int SONNTAG = 6;

        /// <summary>
        /// <b>Die Nutzungstage im Jahr, abgeleitet</b> (E93) — im festen Raster von 365 Tagen, ohne Datenbank: die Tage des
        /// Wochenmusters (Wochentag des 1. Januar aus <paramref name="referenzjahr"/>), bei <c>Feiertage_Wie_Sonntag</c> die
        /// neun Feiertage in der Lage von <see cref="Kalenderregel.Feiertag"/> (<see cref="Feiertage.Jahrestag"/>) wie ein
        /// Sonntag, und die Nutzungstage, die in einen Ferienzeitraum des Ziels fallen (Grenze 0 oder 366 = „aus", Beginn nach
        /// Ende = über den Jahreswechsel; ein ungültiger Zeitraum zählt nicht). Der Generator liest die Zahl nicht.
        /// </summary>
        /// <param name="profil">Das Profil.</param>
        /// <param name="ferien">Die Ferienzeiträume des Ziels (die des Gebäudes); <c>null</c> = ohne Ziel.</param>
        /// <param name="raster">Das Wochentagsraster des Ziels (E115, <see cref="Konditionierungdatenweg.Raster(int)"/>);
        /// <c>null</c> = ohne Projekt <see cref="Konditionierungdatenweg.Rueckfallraster"/>.</param>
        public static Raumnutzungstage Nutzungstage(Raumnutzungsprofil profil, Matrixeingang ferien = null,
                                                    Gemeinjahrkalender? raster = null)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            bool[] genutzt = Wochenmuster(profil);
            Gemeinjahrkalender kalender = raster ?? Konditionierungdatenweg.Rueckfallraster;
            int w0 = kalender.W0;
            var feiertag = new bool[TAGE_IM_JAHR + 1];
            if (profil.Feiertage_Wie_Sonntag == true)
                foreach (string regel in DbWerte.KOND_FEIERTAGE)
                {
                    int t = Feiertage.Jahrestag(regel, kalender);
                    if (t >= 1 && t <= TAGE_IM_JAHR) feiertag[t] = true;
                }
            bool[] frei = Ferientage(ferien);

            int muster = 0, mitFeiertagen = 0, ferientage = 0;
            for (int t = 1; t <= TAGE_IM_JAHR; t++)
            {
                int wochentag = (w0 + t - 1) % TAGE;
                if (genutzt[wochentag]) muster++;
                if (!(feiertag[t] ? genutzt[SONNTAG] : genutzt[wochentag])) continue;
                mitFeiertagen++;
                if (frei[t]) ferientage++;
            }
            return new Raumnutzungstage(muster, muster - mitFeiertagen, ferientage);
        }

        /// <summary>Die Tage 1 … 365 in einem Ferienzeitraum des Ziels (Index = Jahrestag).</summary>
        private static bool[] Ferientage(Matrixeingang b)
        {
            var frei = new bool[TAGE_IM_JAHR + 1];
            if (b == null) return frei;
            for (int k = 0; k < Matrixeingang.FERIENZEITRAEUME; k++)
            {
                double von = b.Ferienbeginn[k], bis = b.Ferienende[k];
                if (!Ferientag(von) || !Ferientag(bis)) continue;
                for (int t = (int)von; ; t = t % TAGE_IM_JAHR + 1)
                {
                    frei[t] = true;
                    if (t == (int)bis) break;
                }
            }
            foreach (Ferienzeile f in b.WeitereFerien ?? System.Array.Empty<Ferienzeile>())
            {
                if (f == null || !Ferientag(f.Beginn) || !Ferientag(f.Ende)) continue;
                for (int t = f.Beginn; ; t = t % TAGE_IM_JAHR + 1)
                {
                    frei[t] = true;
                    if (t == f.Ende) break;
                }
            }
            return frei;
        }

        /// <summary>Ein ganzer Jahrestag 1 … 365 — 0 und 366 heißen „aus".</summary>
        private static bool Ferientag(double tag)
            => double.IsFinite(tag) && tag >= 1.0 && tag <= TAGE_IM_JAHR && tag == Math.Floor(tag);

        // =================================================================
        //  Nennwerte (Q38–Q40)
        // =================================================================

        /// <summary>
        /// Der Nennwert einer Größe nach Q39 (Geräte samt Beleuchtung, Q38) bzw. Q40 (Personen) — nur mit Fläche und
        /// nur, wenn das Profil den Kennwert trägt; auf vier Nachkommastellen. <paramref name="herleitung"/> nennt die
        /// Formel invariant.
        /// </summary>
        public static double? Nennwert(Raumnutzungsprofil p, Konditionierungsgroesse g, double? flaeche, out string herleitung)
        {
            herleitung = null;
            if (p == null || !flaeche.HasValue || !(flaeche.Value > 0.0) || !double.IsFinite(flaeche.Value)) return null;
            double a = flaeche.Value;
            if (g == Konditionierungsgroesse.Geraete && (p.Geraete_Leistung.HasValue || p.Beleuchtung_Leistung.HasValue))
            {
                double geraete = p.Geraete_Leistung ?? 0.0;
                double licht = (p.Beleuchtung_Leistung ?? 0.0) * (p.Beleuchtung_Anteil ?? 1.0);
                double n = Math.Round((geraete + licht) * a, NACHKOMMA, MidpointRounding.AwayFromZero);
                string spez = p.Beleuchtung_Leistung.HasValue
                    ? "(" + Z(geraete) + " W/m² + " + Z(p.Beleuchtung_Leistung.Value) + " W/m² × " + Z(p.Beleuchtung_Anteil ?? 1.0) + ")"
                    : Z(geraete) + " W/m²";
                herleitung = spez + " × " + Z(a) + " m² = " + Z(n) + " W";
                return n;
            }
            if (g == Konditionierungsgroesse.Personen && p.Personen_Flaeche.HasValue && p.Personen_Flaeche.Value > 0.0)
            {
                double w = p.Personen_Waerme ?? Matrixeingang.PERSON_W;
                double n = Math.Round(a / p.Personen_Flaeche.Value * w, NACHKOMMA, MidpointRounding.AwayFromZero);
                herleitung = Z(a) + " m² ÷ " + Z(p.Personen_Flaeche.Value) + " m²/Person × " + Z(w) + " W/Person = " + Z(n) + " W";
                return n;
            }
            return null;
        }

        private static string Z(double w) => w.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
