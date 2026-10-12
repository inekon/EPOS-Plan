using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Feld im Kopf des Reiters „Gebäude und Hülle“ (Entwurf GD, Feldkarte 1.1).</summary>
    public enum Gebaeudefeld
    {
        /// <summary>Nutzfläche (<c>Wohnflaeche_gesamt</c>, <c>Nutzflaeche</c>).</summary>
        Nutzflaeche,
        /// <summary>Fläche je Nutzer (<c>Flaeche_Nutzer</c> → <c>Bewohner</c>).</summary>
        FlaecheJeNutzer,
        /// <summary>Bauart → Bauweise (<c>Bauweise</c>).</summary>
        Bauweise,
        /// <summary>Fensterdurchlassgrad g.</summary>
        Fensterdurchlassgrad,
        /// <summary>Raumhöhe des Gebäudes.</summary>
        Raumhoehe,
        /// <summary>Luftwechselrate des Gebäudes.</summary>
        Luftwechselrate,
        /// <summary>Gebäudeart (nur Einordnung).</summary>
        Gebaeudeart,
        /// <summary>Verwendung Wohn-/Nichtwohngebäude (nur Einordnung).</summary>
        Verwendung,
        /// <summary>Baualtersklasse (nur Einordnung).</summary>
        Baualtersklasse,
        /// <summary>Baujahr (nur Einordnung).</summary>
        Baujahr,
        /// <summary>Energiestandard (nur Einordnung).</summary>
        Energiestandard,
    }

    /// <summary>Woher der wirksame Wert eines Gebäudefelds stammt — die Marke neben dem Feld.</summary>
    public enum Feldherkunft
    {
        /// <summary>Aus der Importdatei gelesen.</summary>
        Datei,
        /// <summary>Das Feld ist leer bzw. null, es gilt die Vorgabe des Kerns.</summary>
        Vorgabe,
        /// <summary>Von Hand eingetragen; der Rechenweg liest den Wert unmittelbar.</summary>
        Hand,
        /// <summary>Der Wert wirkt nur als geerbter Wert bzw. Rückfall in Zonen oder Bauteilen.</summary>
        Geerbt,
        /// <summary>Der Rechenweg liest das Feld nicht.</summary>
        Wirkungslos,
    }

    /// <summary>Wie der Dialog ein Gebäudefeld zeigt.</summary>
    public enum Feldanzeige
    {
        /// <summary>Als Eingabefeld.</summary>
        Feld,
        /// <summary>Als Eingabefeld mit dem Hinweis „Rückfall“ (Bauteilweg).</summary>
        Rueckfall,
        /// <summary>Ausgeblendet: Jede Zone trägt den Wert selbst.</summary>
        JeZone,
    }

    /// <summary>Was die Auskunft von einem Bauteil einer Zone wissen muss.</summary>
    /// <param name="Bauteilart">Persistenzwert der Bauteilart (<c>DbWerte.BAUTEILART_*</c>).</param>
    /// <param name="Randbedingung">Persistenzwert der Randbedingung; <c>null</c> = Vorgabe der Art.</param>
    /// <param name="TrennflaecheZuordnung">Zuordnung einer Trennfläche (<c>DbWerte.TRENNFLAECHE_*</c>).</param>
    /// <param name="MitAufbau">Trägt das Bauteil einen Schichtaufbau?</param>
    /// <param name="MitGWert">Trägt ein Fenster einen eigenen g-Wert?</param>
    public sealed record Bauteilmerkmal(string Bauteilart, string Randbedingung, string TrennflaecheZuordnung,
                                         bool MitAufbau, bool MitGWert);

    /// <summary>Die Eingaben einer Zone samt den Merkmalen ihrer Bauteile.</summary>
    public sealed record Zonenmerkmale(Zoneneingaben Eingaben, IReadOnlyList<Bauteilmerkmal> Bauteile);

    /// <summary>
    /// Die Auskunft zu einem Gebäudefeld. <paramref name="Anzahl"/> zählt bei <see cref="Feldherkunft.Geerbt"/> und
    /// <see cref="Feldherkunft.Vorgabe"/> mit Zonen die Zonen, Fenster oder Gruppen, in denen der Wert wirkt.
    /// </summary>
    public sealed record Feldauskunft(Gebaeudefeld Feld, Feldherkunft Herkunft, Feldanzeige Anzeige, int Anzahl, int Zonenzahl)
    {
        /// <summary>Gehört das Feld in den Block „Beschreibung und Einordnung“?</summary>
        public bool IstBeschreibend => Gebaeudefeldherkunft.IstBeschreibend(Feld);
    }

    /// <summary>
    /// <b>Die wirksame Herkunft je Gebäudefeld</b> (Entwurf GD 2.1 und 4.1, Welle GD-1): Datei, Vorgabe, Hand, geerbt oder
    /// wirkungslos — und ob der Dialog das Feld zeigt.
    ///
    /// <para><b>Die Auskunft ruft, sie rechnet nicht nach:</b> Ob eine Zone die Raumhöhe des Gebäudes erbt, entscheidet
    /// <see cref="Zonenvorgaben.Bilden(Zoneneingaben, Gebaeudevorgaben, int)"/> (Volumen „abgeleitet“ und Raumhöhe „Gebäude“).
    /// Ob die Luftwechselrate des Gebäudes wirkt, entscheidet
    /// <see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?, out Luftwechselherkunft)"/> mit
    /// Infiltration und Nutzerlüftung aus derselben Kaskade. Das ist derselbe Weg wie im Eingangsbauer des Laufs
    /// (<c>GebaeudeModellEingang.Zonenwerte</c>).</para>
    ///
    /// <para><b>Rückfälle des Bauteilwegs:</b> Der Fensterdurchlassgrad gilt für jedes transparente Bauteil ohne eigenes g.
    /// Die Bauweise gilt je Zone für die Außengruppe, wenn sie opake Außenbauteile, aber keines mit Aufbau trägt, und für
    /// die Innengruppe, wenn keines ihrer Bauteile einen Aufbau trägt. Gezählt wird nach Aufbau; ob ein Aufbau Masse trägt,
    /// entscheidet erst der Lauf.</para>
    ///
    /// <para>Gebäudeart, Verwendung, Baualtersklasse, Baujahr und Energiestandard liest kein Rechenweg
    /// (Feldkarte 1.1). Auf dem Tagesbilanz-Weg gibt es keine Zonen, dort wirken alle Rechengrößen unmittelbar.
    /// „Datei“ setzt nur der Aufrufer, solange die Herkunft der Gebäudefelder nicht gespeichert ist.</para>
    /// </summary>
    public static class Gebaeudefeldherkunft
    {
        /// <summary>Die Felder des Blocks „Rechengrößen“ in Anzeigefolge.</summary>
        public static readonly IReadOnlyList<Gebaeudefeld> Rechengroessen = new[]
        {
            Gebaeudefeld.Bauweise, Gebaeudefeld.Nutzflaeche, Gebaeudefeld.FlaecheJeNutzer,
            Gebaeudefeld.Fensterdurchlassgrad, Gebaeudefeld.Raumhoehe, Gebaeudefeld.Luftwechselrate,
        };

        /// <summary>Die Felder des Blocks „Beschreibung und Einordnung“ in Anzeigefolge.</summary>
        public static readonly IReadOnlyList<Gebaeudefeld> Einordnung = new[]
        {
            Gebaeudefeld.Gebaeudeart, Gebaeudefeld.Baualtersklasse, Gebaeudefeld.Baujahr,
            Gebaeudefeld.Verwendung, Gebaeudefeld.Energiestandard,
        };

        /// <summary>Liest kein Rechenweg das Feld (Block „Beschreibung und Einordnung“)?</summary>
        public static bool IstBeschreibend(Gebaeudefeld feld)
            => feld is Gebaeudefeld.Gebaeudeart or Gebaeudefeld.Verwendung or Gebaeudefeld.Baualtersklasse
                    or Gebaeudefeld.Baujahr or Gebaeudefeld.Energiestandard;

        /// <summary>
        /// Bildet die Auskunft für jedes Feld.
        /// </summary>
        /// <param name="gebaeude">Die Vorgaben des Gebäudes, aus denen Zonen erben.</param>
        /// <param name="istVdi6007">Rechnet das Gebäude auf dem VDI-Weg (sonst Tagesbilanz)?</param>
        /// <param name="zonen">Die Zonen; leer = Klassenweg ohne Zone.</param>
        /// <param name="flaecheJeNutzer">Fläche je Nutzer [m²]; leer oder 0 = Vorgabe.</param>
        /// <param name="ausDatei">Felder, deren Wert aus der Importdatei stammt.</param>
        /// <exception cref="ArgumentNullException">ohne Gebäudevorgaben.</exception>
        public static IReadOnlyDictionary<Gebaeudefeld, Feldauskunft> Bilden(
            Gebaeudevorgaben gebaeude, bool istVdi6007, IReadOnlyList<Zonenmerkmale> zonen,
            double? flaecheJeNutzer = null, IReadOnlyCollection<Gebaeudefeld> ausDatei = null)
        {
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));
            zonen ??= Array.Empty<Zonenmerkmale>();
            int zonenzahl = istVdi6007 ? zonen.Count : 0;
            var e = new Dictionary<Gebaeudefeld, Feldauskunft>();

            void Setzen(Gebaeudefeld f, Feldherkunft h, Feldanzeige a = Feldanzeige.Feld, int n = 0)
            {
                if (h == Feldherkunft.Hand && ausDatei != null && Enthaelt(ausDatei, f)) h = Feldherkunft.Datei;
                e[f] = new Feldauskunft(f, h, a, n, zonenzahl);
            }

            foreach (Gebaeudefeld f in Einordnung) Setzen(f, Feldherkunft.Wirkungslos);
            Setzen(Gebaeudefeld.Nutzflaeche, Feldherkunft.Hand);
            Setzen(Gebaeudefeld.FlaecheJeNutzer, flaecheJeNutzer is double fn && fn != 0 ? Feldherkunft.Hand : Feldherkunft.Vorgabe);

            if (!istVdi6007)
            {
                // Tagesbilanz-Weg: keine Zonen, keine Kaskade - jede Rechengröße wirkt unmittelbar.
                Setzen(Gebaeudefeld.Bauweise, Feldherkunft.Hand);
                Setzen(Gebaeudefeld.Fensterdurchlassgrad, Feldherkunft.Hand);
                Setzen(Gebaeudefeld.Raumhoehe, Feldherkunft.Hand);
                Setzen(Gebaeudefeld.Luftwechselrate, Feldherkunft.Hand);
                return e;
            }

            if (zonenzahl == 0)
            {
                // Klassenweg: Raumhöhe, g und Bauweise wirken unmittelbar; die Luftwechselrate nur ohne Lüftungswerte.
                Setzen(Gebaeudefeld.Bauweise, Feldherkunft.Hand);
                Setzen(Gebaeudefeld.Fensterdurchlassgrad, Feldherkunft.Hand);
                Setzen(Gebaeudefeld.Raumhoehe, Feldherkunft.Hand);
                Gebaeudemodellvorgaben.WirksamerLuftwechsel(gebaeude.Luftwechselrate, gebaeude.LuftwechselInfiltration,
                                                            gebaeude.LuftwechselNutzer, out Luftwechselherkunft h);
                Setzen(Gebaeudefeld.Luftwechselrate, h switch
                {
                    Luftwechselherkunft.Luftwechselrate => Feldherkunft.Hand,
                    Luftwechselherkunft.Vorgabe => Feldherkunft.Vorgabe,
                    _ => Feldherkunft.Wirkungslos,
                });
                return e;
            }

            // Bauteilweg mit Zonen: je Zone die Kaskade des Laufs rufen.
            int hoeheGeerbt = 0, lwRate = 0, lwVorgabe = 0, fensterOhneG = 0, gruppenOhneAufbau = 0;
            foreach (Zonenmerkmale z in zonen)
            {
                if (z == null) continue;
                Zonenvorgaben v = Zonenvorgaben.Bilden(z.Eingaben ?? new Zoneneingaben(), gebaeude, zonenzahl);
                if (v.Volumen.Herkunft == Vorgabeherkunft.Abgeleitet && v.Raumhoehe.Herkunft == Vorgabeherkunft.Gebaeude)
                    hoeheGeerbt++;
                Gebaeudemodellvorgaben.WirksamerLuftwechsel(gebaeude.Luftwechselrate, v.LuftwechselInfiltration.Wert,
                                                            v.LuftwechselNutzer.Wert, out Luftwechselherkunft h);
                if (h == Luftwechselherkunft.Luftwechselrate) lwRate++;
                else if (h == Luftwechselherkunft.Vorgabe) lwVorgabe++;

                bool aussenOpak = false, aussenMitAufbau = false, innenMitAufbau = false;
                foreach (Bauteilmerkmal b in z.Bauteile ?? Array.Empty<Bauteilmerkmal>())
                {
                    if (b == null) continue;
                    switch (Gruppe(b))
                    {
                        case Bauteilgruppe.Fenster:
                            if (!b.MitGWert) fensterOhneG++;
                            break;
                        case Bauteilgruppe.Innen:
                            innenMitAufbau |= b.MitAufbau;
                            break;
                        default:
                            aussenOpak = true;
                            aussenMitAufbau |= b.MitAufbau;
                            break;
                    }
                }
                if (aussenOpak && !aussenMitAufbau) gruppenOhneAufbau++;
                if (!innenMitAufbau) gruppenOhneAufbau++;
            }

            Setzen(Gebaeudefeld.Raumhoehe, hoeheGeerbt > 0 ? Feldherkunft.Geerbt : Feldherkunft.Wirkungslos,
                   hoeheGeerbt > 0 ? Feldanzeige.Feld : Feldanzeige.JeZone, hoeheGeerbt);
            int lw = lwRate + lwVorgabe;
            Setzen(Gebaeudefeld.Luftwechselrate,
                   lw == 0 ? Feldherkunft.Wirkungslos : lwVorgabe > 0 ? Feldherkunft.Vorgabe : Feldherkunft.Geerbt,
                   lw > 0 ? Feldanzeige.Feld : Feldanzeige.JeZone, lw);
            Setzen(Gebaeudefeld.Fensterdurchlassgrad, fensterOhneG > 0 ? Feldherkunft.Geerbt : Feldherkunft.Wirkungslos,
                   Feldanzeige.Rueckfall, fensterOhneG);
            Setzen(Gebaeudefeld.Bauweise, gruppenOhneAufbau > 0 ? Feldherkunft.Geerbt : Feldherkunft.Wirkungslos,
                   Feldanzeige.Rueckfall, gruppenOhneAufbau);
            return e;
        }

        /// <summary>Die Gruppe eines Bauteils wie <c>BauteilEingang.Gruppe</c>: transparent, innen (auch eine Trennfläche, die nicht ausdrücklich außen zählt), sonst außen.</summary>
        private static Bauteilgruppe Gruppe(Bauteilmerkmal b)
        {
            if (Bauteilzuordnung.IstTransparent(b.Bauteilart)) return Bauteilgruppe.Fenster;
            Bauteilrand? rand = GebaeudeZonenabbildung.RandAusZeile(b.Bauteilart, b.Randbedingung);
            if (rand == Bauteilrand.Innen) return Bauteilgruppe.Innen;
            if (rand == Bauteilrand.Zone && !string.Equals(b.TrennflaecheZuordnung, DbWerte.TRENNFLAECHE_AW, StringComparison.Ordinal))
                return Bauteilgruppe.Innen;
            return Bauteilgruppe.Aussen;
        }

        private static bool Enthaelt(IReadOnlyCollection<Gebaeudefeld> menge, Gebaeudefeld f)
        {
            foreach (Gebaeudefeld x in menge) if (x == f) return true;
            return false;
        }
    }
}
