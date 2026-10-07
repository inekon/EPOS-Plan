using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using RR = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Aufheizoptimierung im Bericht</b> (Entwurf KP3, Welle O3a; E58 F4 (b), E60; Festlegungen 30, 39, 41): die
    /// Lüftungs- und Aufheizzeilen der Gebäudetafel, die benannten Hinweise W1–W5, die Auslegungsgröße Φ_HL + Φ_RH mit ihren
    /// Teilen und die Kurzform der Konditionierung je Gebäude (Teilkonzept 7.6, B19).
    ///
    /// <para><b>Gerechnet wird hier nichts.</b> Die Zahlen sind die der Ergebniszeile des Laufs (<c>Tab_ErgebnisGebaeude</c>,
    /// dieselben wie im Bedarfsdialog); die Kurzform liest den Konditionierungsstand, den der Sammler je Gebäude ablegt
    /// (<see cref="ProjektDetails.Konditionierung"/>), den Aufschlag die Projekteinstellung (<see cref="ProjektDetails.Einstellungen"/>).
    /// Eine Zeile steht nur mit Wert — ein Projekt ohne Aufheizoptimierung, Nachtauskühlung und Sommerlüftung behält seine
    /// Tafel, wie sie war. Ohne Datenbank; die Texte in der Kultur des Berichts.</para>
    /// </summary>
    public static class Aufheizbericht
    {
        /// <summary>Ein Text aus <c>MyResource</c> in der Kultur des Berichts.</summary>
        private static string T(string ressource, CultureInfo kultur) => Berichtstabellen.Grund(ressource, kultur);

        private static Tabellenzelle Zahl(double? w, int dez, string einheit, CultureInfo kultur) => new Tabellenzelle
        {
            Text = w.HasValue ? Tabellenformat.F(w.Value, dez, kultur) + (string.IsNullOrEmpty(einheit) ? "" : " " + einheit) : Tabellenzelle.STRICH,
            Zahl = w, Format = "N" + dez.ToString(CultureInfo.InvariantCulture), Einheit = einheit,
        };

        // =====================================================================
        //  Die Zeilen der Gebäudetafel
        // =====================================================================

        /// <summary>
        /// <b>Die Lüftungs-, Aufheiz- und Auslegungszeilen eines Gebäudes</b> — nach den Kühl- und Raumkennzahlen der
        /// Gebäudetafel, jede nur mit Wert (Festlegung 30): Nachtauskühl- und Sommerlüftungsstunden; mit Aufheizrechnung
        /// (Zustand gesetzt) Zustand, Art, t_auf,max mit T_a,B und Variante (bei Art „manuell“ die manuelle Aufheizzeit),
        /// P_auf mit Quelle, Rampentage, Σ, längste Rampe, die Tage W2 und W1, die Kappungsstunden und der Aufschlag der
        /// Projekteinstellung (nur bei Art täglich oder fest); mit Auslegungsheizlast die Auslegungsgröße Φ_HL + Φ_RH mit
        /// ihren Teilen (E60). Die ideale Spitze und das Tagesmittel stehen schon darüber in der Tafel.
        /// </summary>
        /// <param name="aufschlag">Der Aufschlag der Projekteinstellung (<see cref="Aufschlag"/>); <c>null</c> = keiner.</param>
        public static IEnumerable<(string Beschriftung, Tabellenzelle Wert)> Zeilen(ErgebnisGebaeudeModel g,
            (double? H, double? Prozent)? aufschlag, CultureInfo kultur)
        {
            if (g == null) yield break;
            if (g.NachtauskuehlstundenH is int nacht)
                yield return (T(nameof(RR.BV_AUFH_NACHTAUSKUEHLSTUNDEN), kultur), Zahl(nacht, 0, "h/a", kultur));
            if (g.SommerlueftungsstundenH is int sommer && g.IstVdi6007)
                yield return (T(nameof(RR.BV_AUFH_SOMMERLUEFTUNGSSTUNDEN), kultur), Zahl(sommer, 0, "h/a", kultur));

            if (g.AufheizZustand != null)
            {
                bool manuell = g.AufheizArt == DbWerte.AUFHEIZ_ART_MANUELL;
                yield return (T(nameof(RR.BV_AUFH_ZUSTAND), kultur), Zellen.Text(Zustandtext(g.AufheizZustand, kultur)));
                string art = Arttext(g, kultur);
                if (art.Length > 0) yield return (T(nameof(RR.BV_AUFH_ART), kultur), Zellen.Text(art));
                if (manuell)
                    yield return (T(nameof(RR.BV_AUFH_ZEIT_MANUELL), kultur), Zahl(g.AufheizzeitMaxH, 0, "h", kultur));
                else if (g.AufheizzeitMaxH.HasValue)
                    yield return (T(nameof(RR.BV_AUFH_ZEIT_MAX), kultur), Zellen.Text(Zeitzeile(g, kultur)));
                if (g.AufheizLeistungKw.HasValue)
                {
                    string quelle = Quellentext(g.AufheizLeistungsquelle, kultur);
                    Tabellenzelle z = Zahl(g.AufheizLeistungKw, 1, "kW", kultur);
                    yield return (T(nameof(RR.BV_AUFH_LEISTUNG), kultur), quelle.Length == 0 ? z : new Tabellenzelle
                    {
                        Text = z.Text + " (" + quelle + ")", Zahl = z.Zahl, Format = z.Format, Einheit = z.Einheit,
                    });
                }
                if (g.Aufheiztage.HasValue) yield return (T(nameof(RR.BV_AUFH_TAGE), kultur), Zahl(g.Aufheiztage, 0, null, kultur));
                if (g.AufheizstundenH.HasValue) yield return (T(nameof(RR.BV_AUFH_STUNDEN), kultur), Zahl(g.AufheizstundenH, 0, "h/a", kultur));
                if (g.AufheizzeitLaengsteH.HasValue) yield return (T(nameof(RR.BV_AUFH_LAENGSTE), kultur), Zahl(g.AufheizzeitLaengsteH, 0, "h", kultur));
                if (g.AufheiztageBegrenzt.HasValue) yield return (T(nameof(RR.BV_AUFH_BEGRENZT), kultur), Zahl(g.AufheiztageBegrenzt, 0, null, kultur));
                if (g.AufheiztageUnerreichbar.HasValue) yield return (T(nameof(RR.BV_AUFH_UNERREICHBAR), kultur), Zahl(g.AufheiztageUnerreichbar, 0, null, kultur));
                if (g.HeizleistungMaxStundenH.HasValue) yield return (T(nameof(RR.BV_AUFH_KAPPUNG), kultur), Zahl(g.HeizleistungMaxStundenH, 1, "h/a", kultur));
                // Festlegung 35: Der Aufschlag wirkt nur auf ermittelte Rampen (täglich, fest), nie auf den manuellen Wert.
                string a = Aufschlagtext(aufschlag, kultur);
                if (a != null && (g.AufheizArt == DbWerte.AUFHEIZ_ART_TAEGLICH || g.AufheizArt == DbWerte.AUFHEIZ_ART_FEST))
                    yield return (T(nameof(RR.BV_AUFH_AUFSCHLAG), kultur), Zellen.Text(a));
            }

            if (Auslegungsgroesse(g) is double auslegung)
            {
                yield return (T(nameof(RR.BV_AUFH_AUSLEGUNG), kultur), Zahl(auslegung, 1, "kW", kultur));
                yield return (T(nameof(RR.BV_AUFH_AUSLEGUNGSHEIZLAST), kultur), Zahl(g.AuslegungsheizlastKw, 1, "kW", kultur));
                yield return (T(nameof(RR.BV_AUFH_ZUSCHLAG), kultur), Zahl(g.AufheizzuschlagKw, 1, "kW", kultur));
            }
        }

        /// <summary>
        /// Die Auslegungsgröße Φ_HL + Φ_RH [kW] (E60, Festlegung 41) aus den Teilen der Ergebniszeile; ein gekoppeltes
        /// Gebäude ohne Zuschlag trägt Φ_HL allein. <c>null</c> ohne Auslegungsheizlast (Schalter aus, Tagesbilanz-Weg).
        /// </summary>
        public static double? Auslegungsgroesse(ErgebnisGebaeudeModel g)
            => g?.AuslegungsheizlastKw is double hl ? hl + (g.AufheizzuschlagKw ?? 0.0) : (double?)null;

        /// <summary>Hat ein Gebäude des Stands mit Aufheizrechnung gerechnet (Zustand gesetzt)?</summary>
        public static bool HatAufheizung(VariantenDaten v)
            => v?.Ergebnis?.Gebaeude != null && v.Ergebnis.Gebaeude.Any(g => g != null && g.AufheizZustand != null);

        /// <summary>
        /// <b>Die benannten Hinweise W1–W5</b> eines Gebäudes (Konzept 4.8) — dieselben Sätze wie im Bedarfsdialog, je
        /// Hinweis nur mit Anlass; leer ohne Aufheizrechnung.
        /// </summary>
        public static List<string> Hinweise(ErgebnisGebaeudeModel g, CultureInfo kultur)
        {
            var l = new List<string>();
            if (g?.AufheizZustand == null) return l;
            if (g.AufheizZustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR) l.Add(T(nameof(RR.GEBB_AUFH_W1_BEMESSUNG), kultur));
            if (g.AufheiztageUnerreichbar is int w1 && w1 > 0) l.Add(string.Format(kultur, T(nameof(RR.GEBB_AUFH_W1), kultur), w1));
            if (g.AufheiztageBegrenzt is int w2 && w2 > 0) l.Add(string.Format(kultur, T(nameof(RR.GEBB_AUFH_W2), kultur), w2));
            if (g.AufheiztageNachweisband is int w3 && w3 > 0) l.Add(string.Format(kultur, T(nameof(RR.GEBB_AUFH_W3), kultur), w3));
            if (g.AufheizspruengeAus is int w4 && w4 > 0) l.Add(string.Format(kultur, T(nameof(RR.GEBB_AUFH_W4), kultur), w4));
            if (g.AufheizZustand == DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT) l.Add(T(nameof(RR.GEBB_AUFH_W5), kultur));
            return l;
        }

        /// <summary>Der Hinweis zur idealen Spitze neben der Auslegungsgröße (Festlegung 41) — derselbe Satz wie im Bedarfsdialog.</summary>
        public static string HinweisSpitze(CultureInfo kultur) => T(nameof(RR.GEBB_AUFH_HRL_SPITZE), kultur);

        /// <summary>Der Zustand der Aufheizrechnung als Text; leer ohne Zustand.</summary>
        public static string Zustandtext(string zustand, CultureInfo kultur) => zustand switch
        {
            DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN => T(nameof(RR.GEBB_AUFH_ZUSTAND_BEMESSEN), kultur),
            DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR => T(nameof(RR.GEBB_AUFH_ZUSTAND_UNERREICHBAR), kultur),
            DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT => T(nameof(RR.GEBB_AUFH_ZUSTAND_GEKOPPELT), kultur),
            DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT => T(nameof(RR.GEBB_AUFH_ZUSTAND_UNBEHEIZT), kultur),
            _ => "",
        };

        /// <summary>Die wirksame Art als Text — bei „manuell“ mit dem Wert; leer ohne Art.</summary>
        public static string Arttext(ErgebnisGebaeudeModel g, CultureInfo kultur) => g?.AufheizArt switch
        {
            DbWerte.AUFHEIZ_ART_MANUELL => string.Format(kultur, T(nameof(RR.GEBB_AUFH_ART_MANUELL), kultur), g.AufheizzeitMaxH),
            DbWerte.AUFHEIZ_ART_FEST => T(nameof(RR.SIMKONF_AUFH_ART_FEST), kultur),
            DbWerte.AUFHEIZ_ART_TAEGLICH => T(nameof(RR.SIMKONF_AUFH_ART_TAEGLICH), kultur),
            _ => "",
        };

        /// <summary>Die Quelle von P_auf als Text; leer ohne Quelle.</summary>
        public static string Quellentext(string quelle, CultureInfo kultur) => quelle switch
        {
            DbWerte.AUFHEIZ_QUELLE_GRENZE => T(nameof(RR.SIMKONF_AUFH_QUELLE_GRENZE), kultur),
            DbWerte.AUFHEIZ_QUELLE_ZIEL => T(nameof(RR.SIMKONF_AUFH_QUELLE_ZIEL), kultur),
            DbWerte.AUFHEIZ_QUELLE_GEMISCHT => T(nameof(RR.SIMKONF_AUFH_QUELLE_GEMISCHT), kultur),
            _ => "",
        };

        /// <summary>„t h bei T_a,B °C (Variante)“ — die Zeile wie im Bedarfsdialog; ohne T_a,B nur die Zeit.</summary>
        private static string Zeitzeile(ErgebnisGebaeudeModel g, CultureInfo kultur)
        {
            string t = Tabellenformat.F(g.AufheizzeitMaxH.Value, 0, kultur);
            if (!g.AufheizAussenC.HasValue) return t + " h";
            string variante = g.AufheizBemessung == DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG
                ? T(nameof(RR.SIMKONF_AUFH_BEMESSUNG_ABZUG), kultur)
                : g.AufheizBemessung == null ? "" : T(nameof(RR.SIMKONF_AUFH_BEMESSUNG_STUNDE), kultur);
            return string.Format(kultur, T(nameof(RR.GEBB_AUFH_ZEIT_BEI), kultur), t, Tabellenformat.F(g.AufheizAussenC.Value, 1, kultur), variante);
        }

        // =====================================================================
        //  Aufschlag der Projekteinstellung
        // =====================================================================

        /// <summary>
        /// Der Aufschlag der Projekteinstellung (<c>Aufheiz_Aufschlag_H</c>, <c>Aufheiz_Aufschlag_Prozent</c>, E59) aus der
        /// geladenen Zeile von <c>Tab_Einstellungen</c>; <c>null</c>, wenn keiner gesetzt ist (0 und NULL heißen keiner,
        /// Festlegung 36). Die Ergebniszeile trägt den Aufschlag nicht — der Bericht nennt ihn deshalb als Projekteinstellung.
        /// </summary>
        public static (double? H, double? Prozent)? Aufschlag(ProjektDetails d)
        {
            DataTable t = d?.Einstellungen;
            if (t == null || t.Rows.Count == 0) return null;
            double? h = ProjektDetails.D(t.Rows[0], AufheizManuellSchema.SPALTE_AUFSCHLAG_H);
            double? p = ProjektDetails.D(t.Rows[0], AufheizManuellSchema.SPALTE_AUFSCHLAG_PROZENT);
            if (!(h > 0.0)) h = null;
            if (!(p > 0.0)) p = null;
            return h == null && p == null ? null : (h, p);
        }

        /// <summary>„2 h / 10 % (es gilt der größere Wert)“; <c>null</c> ohne Aufschlag.</summary>
        private static string Aufschlagtext((double? H, double? Prozent)? aufschlag, CultureInfo kultur)
        {
            if (aufschlag is not { } a || (a.H == null && a.Prozent == null)) return null;
            string h = a.H.HasValue ? Tabellenformat.F(a.H.Value, 0, kultur) : "0";
            string p = a.Prozent.HasValue ? a.Prozent.Value.ToString("0.#", kultur) : "0";
            return string.Format(kultur, T(nameof(RR.BV_AUFH_AUFSCHLAG_WERT), kultur), h, p);
        }

        // =====================================================================
        //  Kurzform der Konditionierung je Gebäude (Teilkonzept 7.6, B19)
        // =====================================================================

        /// <summary>
        /// <b>Die Kurzform der Konditionierung eines Gebäudes</b> — je Größe, die das Gebäude selbst trägt (Vorgabezeilen,
        /// ein angelegter Kalender oder eine Herkunft), eine Zeile Beschriftung · Text, etwa „Konditionierung – Heizen“ ·
        /// „20/18 °C, 22–6 Uhr, Saison 1.10.–30.4., Vorlage „Büro““. Die Werte sind die der Matrix dieser Ebene (Bestand und
        /// Vorgabezellen, gebildet wie im Lauf), der Vorlagenname die Herkunft aus <c>Bemerkung</c> (Festlegung 30). Leer ohne
        /// Stand oder ohne eigene Angabe.
        /// </summary>
        public static List<(string Beschriftung, string Text)> Konditionierung(Konditionierungsstand stand, CultureInfo kultur)
        {
            var l = new List<(string, string)>();
            if (stand == null || stand.TabellenLeer) return l;
            List<Vorgabezeile> zeilen = stand.Vorgabezeilen();
            IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> kalender = stand.Angelegt();
            Vorgabematrix m = stand.Matrix();
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                string kennwort = Konditionierungsgroessen.Kennwort(g);
                bool eigeneZeilen = zeilen.Any(z => z.Groesse == kennwort);
                bool eigenerKalender = kalender.ContainsKey(g);
                Kalenderherkunft herkunft = stand.Herkunft(g);
                string vorlage = herkunft?.Vorlage;
                if (!eigeneZeilen && !eigenerKalender && string.IsNullOrEmpty(vorlage)) continue;

                var teile = new List<string>();
                if (eigeneZeilen) teile.AddRange(Spaltenteile(m, g, kultur));
                else if (eigenerKalender) teile.Add(T(nameof(RR.BV_AUFH_KURZ_KALENDER), kultur));
                if (!string.IsNullOrEmpty(vorlage)) teile.Add(string.Format(kultur, T(nameof(RR.BV_AUFH_KURZ_VORLAGE), kultur), vorlage));
                if (teile.Count == 0) continue;
                l.Add((string.Format(kultur, T(nameof(RR.BV_AUFH_KURZ_ZEILE), kultur), Groessenname(g, kultur)), string.Join(", ", teile)));
            }
            return l;
        }

        /// <summary>Die Teile einer Spalte: Tag/Nacht mit Einheit, das Nachtfenster, Wochenende, Ferien und Saison.</summary>
        private static IEnumerable<string> Spaltenteile(Vorgabematrix m, Konditionierungsgroesse g, CultureInfo kultur)
        {
            Matrixspalte s = m.Spalte(g);
            string einheit = Einheit(g);
            string tag = Zellwert(s.Tag, g, kultur), nacht = Zellwert(s.Nacht, g, kultur);
            string aus = T(nameof(RR.BV_AUFH_KURZ_AUS), kultur);
            if (tag != null && nacht != null)
                yield return tag == aus && nacht == aus ? aus : tag + "/" + nacht + (einheit.Length > 0 ? " " + einheit : "");
            else if (tag != null)
                yield return tag + (tag != aus && einheit.Length > 0 ? " " + einheit : "");
            else if (nacht != null)
                yield return Tabellenzelle.STRICH + "/" + nacht + (nacht != aus && einheit.Length > 0 ? " " + einheit : "");
            m.Nachtfenster(g, out int? von, out int? bis);
            if (nacht != null && von.HasValue && bis.HasValue)
                yield return string.Format(kultur, T(nameof(RR.BV_AUFH_KURZ_NACHT), kultur), von.Value, bis.Value);
            string we = Zellwert(s.Wochenende, g, kultur);
            if (we != null) yield return string.Format(kultur, T(nameof(RR.BV_AUFH_KURZ_WOCHENENDE), kultur), Mit(we, aus, einheit));
            string ferien = Zellwert(s.Ferien, g, kultur);
            if (ferien != null) yield return string.Format(kultur, T(nameof(RR.BV_AUFH_KURZ_FERIEN), kultur), Mit(ferien, aus, einheit));
            if (s.Saison.Von is int beginn && s.Saison.Bis is int ende && beginn >= 1 && beginn <= 365 && ende >= 1 && ende <= 365)
                yield return string.Format(kultur, T(nameof(RR.BV_AUFH_KURZ_SAISON), kultur), Datum(beginn, kultur), Datum(ende, kultur));
        }

        private static string Mit(string wert, string aus, string einheit)
            => wert == aus || einheit.Length == 0 ? wert : wert + " " + einheit;

        /// <summary>Der Wert einer Zelle: „aus“, die Zahl (Geräte und Personen als Prozent des Nennwerts) oder <c>null</c>.</summary>
        private static string Zellwert(Matrixzelle c, Konditionierungsgroesse g, CultureInfo kultur)
        {
            if (c == null) return null;
            if (c.Aus) return T(nameof(RR.BV_AUFH_KURZ_AUS), kultur);
            if (!c.Belegt || !double.IsFinite(c.Wert)) return null;
            return g == Konditionierungsgroesse.Geraete || g == Konditionierungsgroesse.Personen
                ? (c.Wert * 100.0).ToString("0", kultur)
                : c.Wert.ToString("0.##", kultur);
        }

        private static string Einheit(Konditionierungsgroesse g) => g switch
        {
            Konditionierungsgroesse.Heizsoll or Konditionierungsgroesse.Kuehlsoll => "°C",
            Konditionierungsgroesse.Lueftung => "1/h",
            _ => "%",
        };

        /// <summary>Ein Tag 1 … 365 als Datum ohne Jahr (kein Schaltjahr), im Muster der Berichtssprache.</summary>
        private static string Datum(int tag, CultureInfo kultur)
            => new DateTime(2001, 1, 1).AddDays(tag - 1).ToString(T(nameof(RR.BV_AUFH_KURZ_DATUM), kultur), kultur);

        private static string Groessenname(Konditionierungsgroesse g, CultureInfo kultur) => g switch
        {
            Konditionierungsgroesse.Heizsoll => T(nameof(RR.KOND_LBL_GROESSE_HEIZEN), kultur),
            Konditionierungsgroesse.Kuehlsoll => T(nameof(RR.KOND_LBL_GROESSE_KUEHLEN), kultur),
            Konditionierungsgroesse.Lueftung => T(nameof(RR.KOND_LBL_GROESSE_LUEFTUNG), kultur),
            Konditionierungsgroesse.Geraete => T(nameof(RR.KOND_LBL_GROESSE_GERAETE), kultur),
            _ => T(nameof(RR.KOND_LBL_GROESSE_PERSONEN), kultur),
        };
    }
}
