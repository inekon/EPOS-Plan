using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Auf welcher Seite ein Hüllbauteil der einen Zone liegt (Einzonenweg X4).</summary>
    internal enum Huellseite
    {
        /// <summary>Außenluft.</summary>
        Aussen = 0,
        /// <summary>Erdreich.</summary>
        Erdreich = 1,
        /// <summary>Ein unbeheizter Raum, ein Raum, den es nicht gibt, oder eine Seite, die die Datei nicht nennt.</summary>
        Unbeheizt = 2,
    }

    /// <summary>
    /// <b>Ein Bauteil der Hülle</b> — was die Einzonen-Zuordnung (<see cref="GebaeudeAggregation"/>)
    /// je Bauteil entscheidet: Seite, Summenfeld des Gebäudeeditors, Brutto- und Nettofläche,
    /// Fenster und Türen, und ob die Gegenprobe der Trennflächen es verworfen hat.
    /// </summary>
    internal sealed class Huellposten
    {
        internal Huellposten(AbbildBauteil bauteil, int heizPos, int anderePos, Huellseite seite, bool? boden, string summenfeld)
        {
            Bauteil = bauteil;
            HeizPos = heizPos;
            AnderePos = anderePos;
            Seite = seite;
            Boden = boden;
            Summenfeld = summenfeld;
            BruttoM2 = bauteil.BruttoflaecheM2;
        }

        /// <summary>Das Bauteil des Abbilds.</summary>
        internal AbbildBauteil Bauteil { get; }

        /// <summary>Stelle des beheizten Nachbarraums in <see cref="AbbildBauteil.Nachbarn"/>; −1 = Außenbauteil ohne Nachbarraum.</summary>
        internal int HeizPos { get; }

        /// <summary>Stelle des anderen Nachbarn; −1 = keiner.</summary>
        internal int AnderePos { get; }

        /// <summary>Die Seite des Bauteils.</summary>
        internal Huellseite Seite { get; }

        /// <summary>Gegen einen unbeheizten Raum: Ist die Fläche für die Zone Boden (<c>true</c>) oder Decke (<c>false</c>)? <c>null</c> = unbestimmt oder keine waagerechte Art.</summary>
        internal bool? Boden { get; }

        /// <summary>Das Flächenfeld des Gebäudeeditors, in das die Nettofläche zählt (<see cref="GebaeudeZielfelder"/>).</summary>
        internal string Summenfeld { get; }

        /// <summary>Die Randbedingung der Seite.</summary>
        internal Randbedingung Rand => Seite == Huellseite.Aussen ? Randbedingung.Aussenluft
                                     : Seite == Huellseite.Erdreich ? Randbedingung.Erdreich : Randbedingung.Unbeheizt;

        /// <summary>Bruttofläche [m²]; <c>null</c> = Geometrie fehlt.</summary>
        internal double? BruttoM2 { get; }

        /// <summary>Summe der abgezogenen Öffnungen [m²].</summary>
        internal double AbzugM2 { get; set; }

        /// <summary>Nettofläche nach dem Fensterabzug (U14) [m²]; <c>null</c> = Geometrie fehlt.</summary>
        internal double? NettoM2 { get; set; }

        /// <summary>Wurde die Nettofläche der Datei genommen (IFC <c>NetSideArea</c>), weil Brutto − Öffnungen negativ war?</summary>
        internal bool NettoRueckfall { get; set; }

        /// <summary>Ist die Nettofläche negativ geworden und auf 0 gesetzt (Fehler <c>NETTOFLAECHE_NEGATIV</c>)?</summary>
        internal bool NettoNegativ { get; set; }

        /// <summary>Die Trennfläche zu einem bekannten Raum (Schlüssel der Gegenprobe); <c>null</c> = keine.</summary>
        internal string Paar { get; set; }

        /// <summary>Aus welcher Richtung die Datei die Trennfläche beschreibt (erster Nachbar).</summary>
        internal string Richtung { get; set; }

        /// <summary>Hat die Gegenprobe der Trennflächen diese Beschreibung als die kleinere verworfen?</summary>
        internal bool Verworfen { get; set; }

        /// <summary>Die Fenster des Bauteils.</summary>
        internal List<AbbildBauteil> Fenster { get; } = new List<AbbildBauteil>();

        /// <summary>Die Türen des Bauteils.</summary>
        internal List<AbbildBauteil> Tueren { get; } = new List<AbbildBauteil>();
    }

    /// <summary>
    /// <b>Eine Fläche innerer Masse</b> — beide Nachbarn beheizt. Die Einzonen-Zuordnung übergeht
    /// sie (keine Hülle); der Bauteilvorschlag führt sie als Innenbauteil (Gruppe IW).
    /// </summary>
    internal sealed class Innenposten
    {
        internal Innenposten(AbbildBauteil bauteil, int posA, int posB)
        {
            Bauteil = bauteil;
            PosA = posA;
            PosB = posB;
            BruttoM2 = bauteil.BruttoflaecheM2;
        }

        /// <summary>Das Bauteil des Abbilds.</summary>
        internal AbbildBauteil Bauteil { get; }

        /// <summary>Stelle des beheizten Raums DIESES Gebäudes in <see cref="AbbildBauteil.Nachbarn"/> — Seite A.</summary>
        internal int PosA { get; }

        /// <summary>Stelle des zweiten beheizten Raums — Seite B; −1, wenn er in einem anderen Gebäude liegt (dann zählt nur Seite A).</summary>
        internal int PosB { get; }

        /// <summary>Bruttofläche [m²]; <c>null</c> = Geometrie fehlt.</summary>
        internal double? BruttoM2 { get; }

        /// <summary>Summe der abgezogenen Öffnungen (Innentüren, Innenfenster) [m²].</summary>
        internal double AbzugM2 { get; set; }

        /// <summary>Nettofläche je Seite [m²]; <c>null</c> = Geometrie fehlt.</summary>
        internal double? NettoM2 { get; set; }
    }

    /// <summary>
    /// <b>Eine Trennfläche, die die Datei von beiden Seiten beschreibt</b> — das Ergebnis der
    /// Gegenprobe: Es zählt die größere Beschreibung, die kleinere ist verworfen.
    /// </summary>
    /// <param name="RaumA">Der eine Raum (der kleinere Schlüssel, ordinal).</param>
    /// <param name="RaumB">Der andere Raum.</param>
    /// <param name="GrossM2">Die Bruttofläche der größeren Beschreibung [m²].</param>
    /// <param name="KleinM2">Die Bruttofläche der nächstkleineren Beschreibung [m²].</param>
    internal sealed record Trennflaechenpaar(string RaumA, string RaumB, double GrossM2, double KleinM2);

    /// <summary>Das Ergebnis der Einordnung: Hülle und innere Masse, je in Dateireihenfolge.</summary>
    internal sealed class Huelleneinordnung
    {
        /// <summary>Die Hüllbauteile samt Fenster und Türen.</summary>
        internal List<Huellposten> Huelle { get; } = new List<Huellposten>();

        /// <summary>Die Flächen innerer Masse.</summary>
        internal List<Innenposten> Innen { get; } = new List<Innenposten>();

        /// <summary>Die Trennflächen, die die Datei von beiden Seiten beschreibt, in der Reihenfolge ihres ersten Auftretens.</summary>
        internal List<Trennflaechenpaar> Paare { get; } = new List<Trennflaechenpaar>();

        /// <summary>
        /// Die Flächen innerer Masse, die zählen — ohne die mit Nettofläche 0 (ganz Öffnung); eine
        /// ohne Geometrie zählt mit (sie trägt keine Fläche, aber sie fehlt der Datenlage).
        /// </summary>
        internal IReadOnlyList<Innenposten> Innenflaechen
            => Innen.Where(p => !(p.NettoM2.HasValue && p.NettoM2.Value <= 0.0)).ToList();

        /// <summary>
        /// <b>Die gemessene Innenfläche beider Seiten</b> [m²]: je Fläche innerer Masse die
        /// Nettofläche, zweifach, wenn beide Räume zu diesem Gebäude gehören, sonst einfach (nur die
        /// eigene Seite zählt) — die eine Messung für das Zielfeld Innenflächenfaktor der Zuordnung
        /// und den Innenweg des Bauteilvorschlags.
        /// </summary>
        internal double InnenflaecheM2
            => Innenflaechen.Where(p => p.NettoM2.HasValue).Sum(p => p.NettoM2.Value * (p.PosB >= 0 ? 2.0 : 1.0));
    }

    /// <summary>
    /// <b>Die Einordnung der Bauteile eines Gebäudes in Hülle und innere Masse</b> — die Regeln der
    /// Einzonen-Zuordnung (Zonenregel X4) je Bauteil, an EINER Stelle: Die Zuordnung
    /// (<see cref="GebaeudeAggregation"/>) bildet ihre Summenfelder aus dieser Einordnung, der
    /// Bauteilvorschlag (<see cref="GebaeudeBauteilvorschlag"/>) seine Zeilen — so legen beide
    /// dieselben Flächen in dieselben Summenfelder. Ohne Datenbank; meldet nichts (die Meldungen
    /// und Markierungen der Zuordnung bildet die Aggregation aus dem Ergebnis).
    ///
    /// <para>Der Vorschlag hält seine Zeilen trotzdem gegen die Summenfelder der Zuordnung: Jede
    /// Gruppe muss dieselbe Fläche summieren, sonst lehnt er benannt ab
    /// (<see cref="GebaeudeBauteilvorschlag.SUMME_ABWEICHUNG"/>).</para>
    ///
    /// <list type="bullet">
    /// <item><b>Hülle:</b> ein Bauteil mit einem beheizten Nachbarn DIESES Gebäudes; zwei beheizte
    /// Nachbarn = innere Masse. Ein Außenbauteil ohne Nachbarraum (<see cref="AbbildBauteil.HuelleOhneNachbar"/>)
    /// zählt nach seiner Randbedingung.</item>
    /// <item><b>Seite:</b> ohne zweiten Nachbarn nach der Randbedingung der Art (Außenluft, Erdreich,
    /// sonst unbeheizt); mit einem zweiten, nicht beheizten oder unbekannten Nachbarn unbeheizt.</item>
    /// <item><b>Summenfeld:</b> Außenluft — Außenwand → Außenwand, Dach und Decke → Dach, sonst
    /// Sonstige; Erdreich → Grundfläche; unbeheizt — Boden → Grundfläche, Decke → Dach, sonst
    /// Sonstige.</item>
    /// <item><b>Boden oder Decke</b> gegen unbeheizt: die Sicht des beheizten Raums, dann die des
    /// anderen, dann die Neigung (die Normale zeigt vom ersten Nachbarn weg), dann die Flächenart.</item>
    /// <item><b>Gegenprobe der Trennflächen:</b> beschreibt die Datei dieselbe Trennfläche von beiden
    /// Seiten, zählt nur die größere Beschreibung.</item>
    /// <item><b>Fensterabzug (U14):</b> Netto = Brutto − Σ Fenster − Σ Türen derselben Fläche; negativ →
    /// die Nettofläche der Datei, sonst 0 und Fehler.</item>
    /// </list>
    /// </summary>
    internal static class GebaeudeHuelleneinordnung
    {
        /// <summary>Ordnet die Bauteile des Gebäudes <paramref name="index"/> ein.</summary>
        /// <param name="istBeheizt">Der wirksame Zustand eines Raums (mit den Haken der Raumliste).</param>
        internal static Huelleneinordnung Einordnen(GebaeudeAbbild abbild, int index, Func<AbbildRaum, bool> istBeheizt)
        {
            var ergebnis = new Huelleneinordnung();
            AbbildGebaeude g = abbild.Gebaeude[index];

            // Alle Räume der Datei — ein Nachbar kann im Nachbargebäude liegen.
            var raeume = new Dictionary<string, AbbildRaum>(StringComparer.Ordinal);
            var raumGebaeude = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < abbild.Gebaeude.Count; i++)
                foreach (AbbildRaum r in abbild.Gebaeude[i].Raeume)
                    if (!raeume.ContainsKey(r.Kennung)) { raeume[r.Kennung] = r; raumGebaeude[r.Kennung] = i; }

            foreach (AbbildBauteil s in g.Bauteile)
                Einordnen(s, index, raeume, raumGebaeude, istBeheizt, ergebnis);

            Trennflaechen(ergebnis.Huelle, ergebnis.Paare);
            foreach (Huellposten p in ergebnis.Huelle) Oeffnungen(p);
            foreach (Innenposten p in ergebnis.Innen) Oeffnungen(p);
            return ergebnis;
        }

        private static void Einordnen(AbbildBauteil s, int index, Dictionary<string, AbbildRaum> raeume,
                                      Dictionary<string, int> raumGebaeude, Func<AbbildRaum, bool> istBeheizt,
                                      Huelleneinordnung ergebnis)
        {
            int hPos = -1;
            for (int i = 0; i < s.Nachbarn.Count; i++)
                if (raeume.TryGetValue(s.Nachbarn[i].Kennung, out AbbildRaum r) && istBeheizt(r)
                    && raumGebaeude[s.Nachbarn[i].Kennung] == index) { hPos = i; break; }
            bool ohneNachbar = hPos < 0 && s.HuelleOhneNachbar && s.Nachbarn.Count == 0
                               && (s.Randbedingung == Randbedingung.Aussenluft || s.Randbedingung == Randbedingung.Erdreich);
            if (hPos < 0 && !ohneNachbar) return;

            int aPos = -1;
            for (int i = 0; i < s.Nachbarn.Count; i++)
                if (i != hPos) { aPos = i; break; }

            Huellseite seite;
            AbbildRaum andererRaum = null;
            if (aPos < 0)
            {
                if (s.Randbedingung == Randbedingung.Aussenluft) seite = Huellseite.Aussen;
                else if (s.Randbedingung == Randbedingung.Erdreich) seite = Huellseite.Erdreich;
                else seite = Huellseite.Unbeheizt;
            }
            else
            {
                raeume.TryGetValue(s.Nachbarn[aPos].Kennung, out andererRaum);
                if (andererRaum != null && istBeheizt(andererRaum))
                {
                    // Innere Masse: Seite B nur, wenn der zweite Raum zu diesem Gebäude gehört.
                    bool gleichesGebaeude = raumGebaeude[s.Nachbarn[aPos].Kennung] == index;
                    ergebnis.Innen.Add(new Innenposten(s, hPos, gleichesGebaeude ? aPos : -1));
                    return;
                }
                seite = Huellseite.Unbeheizt;
            }

            bool? boden = null;
            string feld;
            switch (seite)
            {
                case Huellseite.Aussen:
                    feld = s.Art == Bauteilart.Aussenwand ? GebaeudeZielfelder.FLAECHE_AUSSENWAND
                         : s.Art == Bauteilart.Dach || s.Art == Bauteilart.Decke ? GebaeudeZielfelder.FLAECHE_DACH
                         : GebaeudeZielfelder.FLAECHE_SONSTIGE;
                    break;
                case Huellseite.Erdreich:
                    feld = GebaeudeZielfelder.FLAECHE_GRUND;
                    break;
                default:
                    boden = IstWaagerechteArt(s.Art) ? Boden(s, hPos, aPos) : null;
                    feld = boden == true ? GebaeudeZielfelder.FLAECHE_GRUND
                         : boden == false ? GebaeudeZielfelder.FLAECHE_DACH
                         : GebaeudeZielfelder.FLAECHE_SONSTIGE;
                    break;
            }

            var p = new Huellposten(s, hPos, aPos, seite, boden, feld);
            if (seite == Huellseite.Unbeheizt && aPos >= 0 && andererRaum != null)
            {
                string h = s.Nachbarn[hPos].Kennung, a = s.Nachbarn[aPos].Kennung;
                p.Paar = string.CompareOrdinal(h, a) < 0 ? h + "\u0001" + a : a + "\u0001" + h;
                p.Richtung = s.Nachbarn[0].Kennung;
            }
            ergebnis.Huelle.Add(p);
        }

        /// <summary>Ist die Fläche für den beheizten Nachbarn Boden (<c>true</c>) oder Decke (<c>false</c>)? <c>null</c> = unbestimmt.</summary>
        internal static bool? Boden(AbbildBauteil s, int hPos, int aPos)
        {
            // 1. Die Sicht des beheizten Raums (AdjacentSpaceId/@surfaceType), 2. die des anderen.
            bool? b = GebaeudeAggregation.SichtIstBoden(s.Nachbarn[hPos].Sicht);
            if (b.HasValue) return b;
            if (aPos >= 0)
            {
                b = GebaeudeAggregation.SichtIstBoden(s.Nachbarn[aPos].Sicht);
                if (b.HasValue) return !b.Value;
            }

            // 3. Die Neigung: Die Normale zeigt vom ERSTEN Nachbarn weg (gbXML-Hausannahme) —
            //    nach oben (Neigung < 90°) heißt: der erste liegt darunter, die Fläche ist seine Decke.
            bool hIstErster = hPos == 0;
            if (s.NeigungGrad is double t && Math.Abs(t - 90.0) > GebaeudeAggregation.WAAGERECHT_GRAD)
            {
                bool ersterUnten = t < 90.0;
                return hIstErster ? !ersterUnten : ersterUnten;
            }

            // 4. Die Art der Fläche aus Sicht des ersten Nachbarn (Ceiling, InteriorFloor …).
            b = GebaeudeAggregation.SichtIstBoden(s.Quellart);
            if (b.HasValue) return hIstErster ? b.Value : !b.Value;
            return null;
        }

        /// <summary>Decke, Bodenplatte und Dach liegen waagerecht — nur sie sind Boden oder Decke.</summary>
        internal static bool IstWaagerechteArt(Bauteilart art)
            => art == Bauteilart.Decke || art == Bauteilart.Bodenplatte || art == Bauteilart.Dach;

        /// <summary>
        /// Die Gegenprobe der Trennflächen (Datenaustauschkonzept 3.5): Beschreibt die Datei dieselbe
        /// Trennfläche von beiden Seiten (A→B und B→A), zählt nur die größere Beschreibung; jedes
        /// solche Paar steht mit beiden Flächen in <paramref name="paare"/>.
        /// </summary>
        private static void Trennflaechen(List<Huellposten> posten, List<Trennflaechenpaar> paare)
        {
            foreach (IGrouping<string, Huellposten> paar in posten.Where(p => p.Paar != null).GroupBy(p => p.Paar, StringComparer.Ordinal))
            {
                var richtungen = paar.GroupBy(p => p.Richtung, StringComparer.Ordinal).ToList();
                if (richtungen.Count < 2) continue;
                var summen = richtungen.Select(r => new { r.Key, Flaeche = r.Sum(p => p.BruttoM2 ?? 0.0), Posten = r.ToList() })
                                       .OrderByDescending(r => r.Flaeche).ThenBy(r => r.Key, StringComparer.Ordinal).ToList();
                foreach (var r in summen.Skip(1))
                    foreach (Huellposten p in r.Posten) p.Verworfen = true;
                string[] raeume = paar.Key.Split('\u0001');
                paare.Add(new Trennflaechenpaar(raeume[0], raeume[1], summen[0].Flaeche, summen[1].Flaeche));
            }
        }

        /// <summary>Fenster und Türen eines Hüllbauteils samt Fensterabzug (U14).</summary>
        private static void Oeffnungen(Huellposten p)
        {
            AbbildBauteil s = p.Bauteil;
            foreach (AbbildBauteil o in s.Oeffnungen)
            {
                if (o.Art == Bauteilart.Fenster) p.Fenster.Add(o);
                else if (o.Art == Bauteilart.Tuer) p.Tueren.Add(o);
                else continue;
                if (o.BruttoflaecheM2.HasValue) p.AbzugM2 += o.BruttoflaecheM2.Value;
            }
            if (!p.BruttoM2.HasValue) return;
            double netto = p.BruttoM2.Value - p.AbzugM2;
            if (netto < 0.0 && s.NettoflaecheM2 is double eigen && eigen >= 0.0)
            {
                netto = eigen;
                p.NettoRueckfall = true;
            }
            if (netto < 0.0)
            {
                netto = 0.0;
                p.NettoNegativ = true;
            }
            p.NettoM2 = netto;
        }

        /// <summary>Die Nettofläche einer Fläche innerer Masse: dieselbe Regel, ohne Fehler — eine Innenfläche 0 entfällt.</summary>
        private static void Oeffnungen(Innenposten p)
        {
            AbbildBauteil s = p.Bauteil;
            foreach (AbbildBauteil o in s.Oeffnungen)
                if ((o.Art == Bauteilart.Fenster || o.Art == Bauteilart.Tuer) && o.BruttoflaecheM2.HasValue)
                    p.AbzugM2 += o.BruttoflaecheM2.Value;
            if (!p.BruttoM2.HasValue) return;
            double netto = p.BruttoM2.Value - p.AbzugM2;
            if (netto < 0.0 && s.NettoflaecheM2 is double eigen && eigen >= 0.0) netto = eigen;
            p.NettoM2 = Math.Max(0.0, netto);
        }
    }
}
