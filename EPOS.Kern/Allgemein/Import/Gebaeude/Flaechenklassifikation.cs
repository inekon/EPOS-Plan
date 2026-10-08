using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Gruppe einer Fläche nach Randbedingung</b> (Konzept HottCAD-Verbund 4.1, E87 F5): jede Fläche eines Raumkörpers
    /// genau eine Gruppe. Anzeige und Gegenprobe, keine Rechengröße.
    /// </summary>
    internal enum Flaechengruppe
    {
        /// <summary>Innen, thermisch neutral (beheizt beidseitig).</summary>
        R0 = 0,
        /// <summary>Wand beheizt gegen außen.</summary>
        R1 = 1,
        /// <summary>Wand beheizt gegen unbeheizt.</summary>
        R2 = 2,
        /// <summary>Wand oder Boden gegen Erdreich.</summary>
        R3 = 3,
        /// <summary>Boden gegen unbeheizt oder außen.</summary>
        R4 = 4,
        /// <summary>Decke oder Dach gegen außen.</summary>
        R5 = 5,
        /// <summary>Decke gegen unbeheizt (Dachraum).</summary>
        R6 = 6,
        /// <summary>Fenster und Türen nach außen oder gegen unbeheizt.</summary>
        R7 = 7,
    }

    /// <summary>
    /// <b>Eine klassifizierte Fläche</b>: die Dreiecke eines Raumkörpers (oder, mit <see cref="Raumkennung"/> <c>null</c>, eines
    /// Bauteilkörpers), ihre Gruppe, das Bauteil, das sie bestimmt (<c>null</c> = keines), und der Beleg der Regel.
    /// </summary>
    internal sealed record Flaechengruppenzeile(string Raumkennung, IReadOnlyList<int> Dreiecksindizes, Flaechengruppe Gruppe,
                                                string Bauteilkennung, string Beleg);

    /// <summary>
    /// <b>Die Flächenklassifikation eines Gebäudes</b> (Konzept HottCAD-Verbund 4.2): Jede ebene Fläche eines Raumkörpers bekommt
    /// eine <see cref="Flaechengruppe"/>.
    /// <list type="number">
    /// <item><b>Quellfläche</b> (Datenaustauschkonzept 17.4, Beleg <see cref="BELEG_QUELLFLAECHE"/>): Trägt ein Dreieck eines aus
    /// Flächen gebildeten Körpers die Kennung einer Fläche, die einem Bauteil der Datei entspricht, gibt dessen wirksame
    /// Randbedingung die Gruppe — die Zuordnung ist durch die Bauweise bekannt und wird nicht gesucht. Ersatzkennungen
    /// (<c>Raum:Mantel…</c>, <c>Raum:Schale…</c>) treffen kein Bauteil und fallen auf die folgenden Regeln zurück.</item>
    /// <item><b>Raumgrenze</b> (Rangfolge wie E73 zuerst): Trägt ein Bauteil eine Raumgrenze des Raums in derselben Ebene, gibt
    /// deren Lage die Gruppe, bei innen die Beheizung des Nachbarn.</item>
    /// <item><b>Gepaart</b> (<see cref="Koerpernachbarschaft"/>): Liegt mindestens die Hälfte der Fläche einem anderen Raum gegenüber,
    /// entscheidet die Beheizung des Nachbarn — beheizt R0, unbeheizt R2 (senkrecht), R4 (Normale nach unten), R6 (nach oben).</item>
    /// <item><b>Ungepaart, Bauteil des Raumbezugs</b>: ein Bauteil, auf das der Raum verweist, mit passender Lage (Wand, Boden, Decke
    /// aus der Normale) und — bei Wänden — passender Orientierung (Toleranz <see cref="ORIENTIERUNG_TOLERANZ_GRAD"/>); dessen
    /// wirksame Randbedingung gibt R1, R2, R3, R4, R5 oder R6. Innere Bauteile zählen hier nicht.</item>
    /// <item><b>Rückfall</b> <see cref="BELEG_OHNE_BAUTEIL"/>: senkrecht R1, unten R3 im untersten Geschoss sonst R4, oben R5.</item>
    /// </list>
    /// Fenster und Türen sind R7: als eigene Bauteilkörper (Zeilen ohne Raum) und in der Bilanz mit ihrer Fläche aus dem
    /// Mengensatz. Die Bilanz je Gruppe summiert die Dreiecksflächen der Raumkörper; die Gegenprobe hält sie gegen die
    /// Bauteilflächen des Mengensatzes und meldet über <see cref="ABWEICHUNG_GRENZE"/> einen Hinweis. Nichts davon wird
    /// geschrieben oder gerechnet.
    /// </summary>
    internal static class Flaechenklassifikation
    {
        /// <summary>Der Name der Meldung zu Flächen ohne passendes Bauteil; davor steht das Meldungspräfix des Formats.</summary>
        internal const string NAME_OHNE_BAUTEIL = "FLAECHE_OHNE_BAUTEIL";

        /// <summary>Der Name der Meldung zur Gegenprobe über der Abweichungsgrenze; davor steht das Meldungspräfix des Formats.</summary>
        internal const string NAME_ABWEICHUNG = "FLAECHENGRUPPE_ABWEICHUNG";

        /// <summary>Die Meldung zu Flächen ohne passendes Bauteil im IFC-Weg.</summary>
        internal const string MELDUNG_OHNE_BAUTEIL = IfcImportProfil.MELDUNGSPRAEFIX + NAME_OHNE_BAUTEIL;

        /// <summary>Die Meldung zur Gegenprobe über der Abweichungsgrenze im IFC-Weg.</summary>
        internal const string MELDUNG_ABWEICHUNG = IfcImportProfil.MELDUNGSPRAEFIX + NAME_ABWEICHUNG;

        /// <summary>Das Meldungspräfix des Formats eines Abbilds: IFC, Projektdatei oder gbXML (Vorgabe).</summary>
        internal static string Praefix(string format)
            => string.Equals(format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal) ? IfcImportProfil.MELDUNGSPRAEFIX
             : string.Equals(format, GebaeudeQuelle.FORMAT_SQPROJ, StringComparison.Ordinal) ? SqprojImportProfil.MELDUNGSPRAEFIX
             : GbxmlImportProfil.MELDUNGSPRAEFIX;

        /// <summary>Die Toleranz der Orientierung einer Wand gegen die Flächennormale [°].</summary>
        internal const double ORIENTIERUNG_TOLERANZ_GRAD = 15.0;

        /// <summary>Der Anteil der Fläche, ab dem sie als gepaart gilt.</summary>
        internal const double PAAR_ANTEIL = 0.5;

        /// <summary>Die relative Abweichung der Gegenprobe, ab der ein Hinweis kommt.</summary>
        internal const double ABWEICHUNG_GRENZE = 0.05;

        /// <summary>Der größte Abstand einer Raumgrenze von der Ebene der Fläche [m].</summary>
        internal const double GRENZE_ABSTAND_M = 0.05;

        /// <summary>Beleg: die Quellfläche eines gebildeten Körpers (17.4).</summary>
        internal const string BELEG_QUELLFLAECHE = "QUELLFLAECHE";
        /// <summary>Beleg: Raumgrenze der Datei.</summary>
        internal const string BELEG_RAUMGRENZE = "RAUMGRENZE";
        /// <summary>Beleg: Flächenpaar der Körper.</summary>
        internal const string BELEG_PAAR = "PAAR";
        /// <summary>Beleg: Bauteil des Raumbezugs.</summary>
        internal const string BELEG_BAUTEIL = "BAUTEIL";
        /// <summary>Beleg: Rückfall aus der Normale, kein passendes Bauteil.</summary>
        internal const string BELEG_OHNE_BAUTEIL = "FLAECHE_OHNE_BAUTEIL";
        /// <summary>Beleg: Körper eines Bauteils (Zeile ohne Raum).</summary>
        internal const string BELEG_BAUTEILKOERPER = "BAUTEILKOERPER";

        private enum Lage { Wand, Boden, Decke }

        private sealed class Flaeche
        {
            internal double[] N;
            internal double S;
            internal string Quelle;
            internal readonly List<int> Dreiecke = new List<int>();
            internal double FlaecheM2;
            internal readonly Dictionary<int, double> Partner = new Dictionary<int, double>();
        }

        /// <summary>
        /// Die formatfreie Stelle nach dem Lesen (17.4): klassifiziert jedes Gebäude eines Abbilds mit Raumkörpern, gedreht um
        /// den wirksamen Nordwinkel (<see cref="GebaeudeAbbild.NordwinkelWirksamGrad"/>). Gerufen von den Lesern der
        /// Projektdatei und von gbXML; der IFC-Weg ruft <see cref="Klassifizieren"/> in seinem Ablauf selbst.
        /// </summary>
        internal static void KlassifizierenAlle(GebaeudeAbbild abbild)
        {
            if (abbild == null) return;
            string praefix = Praefix(abbild.Format);
            foreach (AbbildGebaeude g in abbild.Gebaeude) Klassifizieren(g, abbild.NordwinkelWirksamGrad ?? 0.0, praefix);
        }

        /// <summary>
        /// Klassifiziert die Flächen der Raumkörper eines Gebäudes und hängt Zeilen, Bilanz und Gegenprobe an
        /// <paramref name="g"/>; ohne einen Raumkörper bleibt alles leer. Meldungen gehen an das Gebäude.
        /// </summary>
        /// <param name="g">Das Gebäude.</param>
        /// <param name="nordwinkelGrad">Die Drehung, mit der der Leser die Azimute gedreht hat (<see cref="GebaeudeAbbild.NordwinkelGrad"/>).</param>
        /// <param name="praefix">Das Meldungspräfix des Formats (<see cref="Praefix"/>); ohne Angabe das des IFC-Wegs.</param>
        internal static void Klassifizieren(AbbildGebaeude g, double nordwinkelGrad = 0.0, string praefix = IfcImportProfil.MELDUNGSPRAEFIX)
        {
            if (g == null || g.Raeume.All(r => r.Koerper == null)) return;
            var zeilen = new List<Flaechengruppenzeile>();
            var bilanz = Enum.GetValues(typeof(Flaechengruppe)).Cast<Flaechengruppe>().ToDictionary(x => x, _ => 0.0);
            var jeKennung = new Dictionary<string, AbbildBauteil>(StringComparer.Ordinal);
            foreach (AbbildBauteil b in g.Bauteile)
                if (!jeKennung.ContainsKey(b.Kennung)) jeKennung[b.Kennung] = b;

            // Die Paare der Körper je Fläche (Rang 2).
            var flaechen = new List<Flaeche>[g.Raeume.Count];
            for (int i = 0; i < g.Raeume.Count; i++) flaechen[i] = g.Raeume[i].Koerper == null ? null : Flaechen(g.Raeume[i].Koerper);
            double cosPaar = Math.Cos(Koerpernachbarschaft.WINKEL_MAX_GRAD * Math.PI / 180.0);
            foreach (Koerperpaar p in Koerpernachbarschaft.Paare(g.Raeume))
            {
                double[] gegen = p.NormaleA.Select(x => -x).ToArray();
                Zuordnen(flaechen[p.RaumA], p.NormaleA, p.SchwerpunktA, p.RaumB, p.FlaecheM2, cosPaar);
                Zuordnen(flaechen[p.RaumB], gegen, p.SchwerpunktB, p.RaumA, p.FlaecheM2, cosPaar);
            }

            double? untersteLage = g.Raeume.Where(r => r.GeschossLageM.HasValue).Select(r => r.GeschossLageM.Value).DefaultIfEmpty().Min();
            bool mitLage = g.Raeume.Any(r => r.GeschossLageM.HasValue);
            double sinWand = Math.Sin(Koerpernachbarschaft.WAND_NEIGUNG_GRAD * Math.PI / 180.0);
            int ohneBauteil = 0;
            double ohneBauteilM2 = 0.0;

            for (int ri = 0; ri < g.Raeume.Count; ri++)
            {
                AbbildRaum raum = g.Raeume[ri];
                if (flaechen[ri] == null) continue;
                bool unterstes = !mitLage || !raum.GeschossLageM.HasValue || raum.GeschossLageM.Value <= untersteLage.Value + 0.01;
                List<AbbildBauteil> bezug = raum.Bezugsbauteile.Where(jeKennung.ContainsKey).Select(k => jeKennung[k])
                                                .Where(b => b.Art != Bauteilart.Fenster && b.Art != Bauteilart.Tuer).ToList();
                foreach (Flaeche f in flaechen[ri])
                {
                    Lage lage = f.N[2] > sinWand ? Lage.Decke : f.N[2] < -sinWand ? Lage.Boden : Lage.Wand;
                    Flaechengruppe? gruppe = null;
                    string bauteil = null, beleg = null;

                    // 0. Quellfläche eines gebildeten Körpers (17.4).
                    if (f.Quelle != null && jeKennung.TryGetValue(f.Quelle, out AbbildBauteil quelle)
                        && AusQuelle(g, raum, quelle, lage) is Flaechengruppe gq)
                    {
                        gruppe = gq; bauteil = quelle.Kennung; beleg = BELEG_QUELLFLAECHE;
                    }
                    // 1. Raumgrenze der Datei.
                    if (!gruppe.HasValue && g.ZahlGrenzen > 0 && Raumgrenze(g, raum, f, lage, out Flaechengruppe gg, out string gb))
                    {
                        gruppe = gg; bauteil = gb; beleg = BELEG_RAUMGRENZE;
                    }
                    // 2. Gepaart.
                    if (!gruppe.HasValue && f.Partner.Count > 0 && f.Partner.Values.Sum() >= PAAR_ANTEIL * f.FlaecheM2)
                    {
                        int nachbar = f.Partner.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First().Key;
                        gruppe = NachBeheizung(g.Raeume[nachbar].Beheizt, lage);
                        beleg = BELEG_PAAR;
                    }
                    // 3. Bauteil des Raumbezugs.
                    if (!gruppe.HasValue)
                    {
                        double az = IfcPlatzierung.Azimut(f.N[0], f.N[1], nordwinkelGrad);
                        AbbildBauteil b = Passend(bezug, lage, az);
                        Flaechengruppe? ausBauteil = b == null ? null : AusRand(Rand(b), lage);
                        if (ausBauteil.HasValue)
                        {
                            gruppe = ausBauteil; bauteil = b.Kennung; beleg = BELEG_BAUTEIL;
                        }
                    }
                    // 4. Rückfall aus der Normale.
                    if (!gruppe.HasValue)
                    {
                        gruppe = lage == Lage.Wand ? Flaechengruppe.R1 : lage == Lage.Decke ? Flaechengruppe.R5
                               : unterstes ? Flaechengruppe.R3 : Flaechengruppe.R4;
                        beleg = BELEG_OHNE_BAUTEIL;
                        ohneBauteil++;
                        ohneBauteilM2 += f.FlaecheM2;
                    }
                    zeilen.Add(new Flaechengruppenzeile(raum.Kennung, f.Dreiecke, gruppe.Value, bauteil, beleg));
                    bilanz[gruppe.Value] += f.FlaecheM2;
                }
            }

            // Die Körper der Bauteile und Öffnungen (Zeilen ohne Raum); die Öffnungen der Hülle zählen mit ihrer Fläche in R7.
            var menge = Enum.GetValues(typeof(Flaechengruppe)).Cast<Flaechengruppe>().ToDictionary(x => x, _ => 0.0);
            foreach (AbbildBauteil b in g.Bauteile)
            {
                Flaechengruppe? mg = Mengengruppe(b);
                if (mg.HasValue) menge[mg.Value] += b.BruttoflaecheM2 ?? 0.0;
                if (b.Koerper != null)
                    zeilen.Add(new Flaechengruppenzeile(null, Enumerable.Range(0, b.Koerper.DreieckZahl).ToList(), mg ?? Flaechengruppe.R0,
                                                        b.Kennung, BELEG_BAUTEILKOERPER));
                foreach (AbbildBauteil o in b.Oeffnungen)
                {
                    bool huelle = Rand(o) != Randbedingung.Innen && Rand(o) != Randbedingung.Unbekannt;
                    if (huelle)
                    {
                        menge[Flaechengruppe.R7] += o.BruttoflaecheM2 ?? 0.0;
                        bilanz[Flaechengruppe.R7] += o.BruttoflaecheM2 ?? 0.0;
                    }
                    if (o.Koerper != null)
                        zeilen.Add(new Flaechengruppenzeile(null, Enumerable.Range(0, o.Koerper.DreieckZahl).ToList(),
                                                            huelle ? Flaechengruppe.R7 : Flaechengruppe.R0, o.Kennung, BELEG_BAUTEILKOERPER));
                }
            }

            g.Flaechengruppen = zeilen;
            g.FlaechengruppenBilanzM2 = bilanz.ToDictionary(x => x.Key, x => Math.Round(x.Value, 6));
            g.FlaechengruppenMengeM2 = menge.ToDictionary(x => x.Key, x => Math.Round(x.Value, 6));

            if (ohneBauteil > 0)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, praefix + NAME_OHNE_BAUTEIL, g.Anzeigename, Ganz(ohneBauteil), Zahl(ohneBauteilM2)));
            foreach (Flaechengruppe x in Abweichungen(g))
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, praefix + NAME_ABWEICHUNG, g.Anzeigename, x.ToString(),
                    Zahl(g.FlaechengruppenBilanzM2[x]), Zahl(g.FlaechengruppenMengeM2[x])));
        }

        /// <summary>
        /// Die Gruppen, deren Dreiecksfläche um mehr als <see cref="ABWEICHUNG_GRENZE"/> von der Bauteilfläche des Mengensatzes
        /// abweicht (R0 und R7 nicht: R0 hat keine Menge, R7 kommt aus der Menge).
        /// </summary>
        internal static List<Flaechengruppe> Abweichungen(AbbildGebaeude g)
        {
            var liste = new List<Flaechengruppe>();
            if (g?.FlaechengruppenBilanzM2 == null) return liste;
            foreach (Flaechengruppe x in new[] { Flaechengruppe.R1, Flaechengruppe.R2, Flaechengruppe.R3, Flaechengruppe.R4, Flaechengruppe.R5, Flaechengruppe.R6 })
            {
                double k = g.FlaechengruppenBilanzM2[x], m = g.FlaechengruppenMengeM2[x];
                if (k <= 0.0 && m <= 0.0) continue;
                if (Math.Abs(k - m) > ABWEICHUNG_GRENZE * Math.Max(k, m)) liste.Add(x);
            }
            return liste;
        }

        // ------------------------------------------------------------------
        //  Regeln
        // ------------------------------------------------------------------

        /// <summary>Die wirksame Randbedingung eines Bauteils: die nicht beheizte Seite, sonst die aus Typ und Nachbarschaft.</summary>
        internal static Randbedingung Rand(AbbildBauteil b) => b.RandbedingungWirksam ?? b.Randbedingung;

        private static Flaechengruppe NachBeheizung(bool nachbarBeheizt, Lage lage)
            => nachbarBeheizt ? Flaechengruppe.R0 : lage == Lage.Wand ? Flaechengruppe.R2 : lage == Lage.Boden ? Flaechengruppe.R4 : Flaechengruppe.R6;

        private static Flaechengruppe? AusRand(Randbedingung rand, Lage lage)
        {
            switch (rand)
            {
                case Randbedingung.Aussenluft: return lage == Lage.Wand ? Flaechengruppe.R1 : lage == Lage.Boden ? Flaechengruppe.R4 : Flaechengruppe.R5;
                case Randbedingung.Erdreich: return lage == Lage.Decke ? Flaechengruppe.R5 : Flaechengruppe.R3;
                case Randbedingung.Unbeheizt: return lage == Lage.Wand ? Flaechengruppe.R2 : lage == Lage.Boden ? Flaechengruppe.R4 : Flaechengruppe.R6;
                case Randbedingung.Innen: return Flaechengruppe.R0;
                default: return null;
            }
        }

        /// <summary>
        /// Die Gruppe aus der Quellfläche: die wirksame Randbedingung des Bauteils wie in der Regel „Bauteil des Raumbezugs“;
        /// ein inneres Bauteil nach der Beheizung des Nachbarraums, ohne bekannten Nachbarn R0; unbekannt = <c>null</c>.
        /// </summary>
        private static Flaechengruppe? AusQuelle(AbbildGebaeude g, AbbildRaum raum, AbbildBauteil b, Lage lage)
        {
            Randbedingung rand = Rand(b);
            if (rand != Randbedingung.Innen) return AusRand(rand, lage);
            AbbildNachbar n = b.Nachbarn.FirstOrDefault(y => y.Kennung != raum.Kennung);
            AbbildRaum nachbar = n == null ? null : g.Raeume.FirstOrDefault(r => r.Kennung == n.Kennung);
            return nachbar == null ? Flaechengruppe.R0 : NachBeheizung(nachbar.Beheizt, lage);
        }

        /// <summary>Ist das Bauteil für den Raum Boden (<c>true</c>), Decke (<c>false</c>) oder offen (<c>null</c>)?</summary>
        private static bool? IstBoden(AbbildBauteil b)
        {
            if (b.RandbedingungBeleg == AbbildBauteil.BELEG_KELLERDECKE) return true;
            if (b.RandbedingungBeleg == AbbildBauteil.BELEG_OBERSTE_DECKE) return false;
            if (Rand(b) == Randbedingung.Erdreich || b.Art == Bauteilart.Bodenplatte) return true;
            if (b.Art == Bauteilart.Dach || string.Equals(b.Quelltyp, "IfcRoof", StringComparison.OrdinalIgnoreCase)) return false;
            return b.ZonenbodenOhneNachbar;
        }

        private static bool Senkrecht(AbbildBauteil b)
            => b.NeigungGrad.HasValue ? Math.Abs(b.NeigungGrad.Value - 90.0) < 45.0
                                      : b.Art == Bauteilart.Aussenwand || b.Art == Bauteilart.Innenwand || b.Art == Bauteilart.Vorhangfassade;

        /// <summary>Das Bauteil des Raumbezugs zur Fläche: Lage gleich, bei Wänden Orientierung in der Toleranz; Hülle vor innen.</summary>
        private static AbbildBauteil Passend(List<AbbildBauteil> bezug, Lage lage, double azimut)
        {
            IEnumerable<AbbildBauteil> kandidaten;
            if (lage == Lage.Wand)
                kandidaten = bezug.Where(b => Senkrecht(b) && Orientierungen(b).Any(o => Winkelabstand(o, azimut) <= ORIENTIERUNG_TOLERANZ_GRAD));
            else
            {
                bool boden = lage == Lage.Boden;
                List<AbbildBauteil> waagerecht = bezug.Where(b => !Senkrecht(b) && IstBoden(b) != !boden).ToList();
                kandidaten = waagerecht.Where(b => IstBoden(b) == boden).Concat(waagerecht.Where(b => !IstBoden(b).HasValue));
            }
            // Ein inneres Bauteil gibt einer ungepaarten Fläche keine Gruppe: Läge dort ein Raum mit Körper, wäre sie gepaart.
            return kandidaten.FirstOrDefault(b => Rand(b) != Randbedingung.Innen && Rand(b) != Randbedingung.Unbekannt);
        }

        private static IEnumerable<double> Orientierungen(AbbildBauteil b)
        {
            if (b.OrientierungSeiteA.HasValue) yield return b.OrientierungSeiteA.Value;
            if (b.OrientierungSeiteB.HasValue) yield return b.OrientierungSeiteB.Value;
            if (b.AzimutGrad.HasValue) yield return b.AzimutGrad.Value;
        }

        internal static double Winkelabstand(double a, double b)
        {
            double d = Math.Abs(((a - b) % 360.0 + 360.0) % 360.0);
            return Math.Min(d, 360.0 - d);
        }

        /// <summary>Die Gruppe eines Bauteils nach seiner Menge (Gegenprobe); <c>null</c> = innen oder unbestimmt.</summary>
        internal static Flaechengruppe? Mengengruppe(AbbildBauteil b)
        {
            Randbedingung rand = Rand(b);
            if (rand == Randbedingung.Innen || rand == Randbedingung.Unbekannt) return null;
            if (Senkrecht(b)) return AusRand(rand, Lage.Wand);
            bool? boden = IstBoden(b);
            return AusRand(rand, boden == true ? Lage.Boden : Lage.Decke);
        }

        /// <summary>Eine Raumgrenze eines Bauteils in der Ebene der Fläche (Rang 1).</summary>
        private static bool Raumgrenze(AbbildGebaeude g, AbbildRaum raum, Flaeche f, Lage lage, out Flaechengruppe gruppe, out string bauteil)
        {
            double cosMax = Math.Cos(ORIENTIERUNG_TOLERANZ_GRAD * Math.PI / 180.0);
            foreach (AbbildBauteil b in g.Bauteile)
                foreach (AbbildGrenze gr in b.Grenzen)
                {
                    if (gr.RaumKennung != raum.Kennung || gr.Normale == null || gr.SchwerpunktM == null) continue;
                    if (Math.Abs(Punkt(gr.Normale, f.N)) < cosMax || Math.Abs(Punkt(f.N, gr.SchwerpunktM) - f.S) > GRENZE_ABSTAND_M) continue;
                    Flaechengruppe? x = null;
                    if (gr.Lage == Randbedingung.Innen)
                    {
                        AbbildNachbar n = b.Nachbarn.FirstOrDefault(y => y.Kennung != raum.Kennung);
                        AbbildRaum nachbar = n == null ? null : g.Raeume.FirstOrDefault(r => r.Kennung == n.Kennung);
                        if (nachbar != null) x = NachBeheizung(nachbar.Beheizt, lage);
                    }
                    else x = AusRand(gr.Lage, lage);
                    if (!x.HasValue) continue;
                    gruppe = x.Value;
                    bauteil = b.Kennung;
                    return true;
                }
            gruppe = Flaechengruppe.R0;
            bauteil = null;
            return false;
        }

        // ------------------------------------------------------------------
        //  Geometrie
        // ------------------------------------------------------------------

        /// <summary>
        /// Die ebenen Flächen eines Körpers (Normale und Ebenenabstand auf 1/1000 gerundet) mit ihren Dreiecken; trägt der Körper
        /// Quellflächen (gebildet, 17.4), teilt die Quellfläche die Ebene zusätzlich — zwei Bauteile in einer Ebene bleiben getrennt.
        /// </summary>
        private static List<Flaeche> Flaechen(Dateikoerper k)
        {
            var liste = new List<Flaeche>();
            var jeSchluessel = new Dictionary<(long, long, long, long, string), Flaeche>();
            bool mitQuelle = k.Quellflaechen.Count == k.Dreiecke.Count && k.Quellflaechen.Count > 0;
            for (int t = 0; t < k.Dreiecke.Count; t++)
            {
                int[] d = k.Dreiecke[t];
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double[] kreuz = Kreuz(Minus(b, a), Minus(c, a));
                double laenge = Math.Sqrt(Punkt(kreuz, kreuz));
                if (laenge < 1e-12) continue;
                double[] n = kreuz.Select(x => x / laenge).ToArray();
                double s = Punkt(n, a);
                string quelle = mitQuelle ? k.Quellflaechen[t] : null;
                var schluessel = ((long)Math.Round(n[0] * 1e3), (long)Math.Round(n[1] * 1e3), (long)Math.Round(n[2] * 1e3), (long)Math.Round(s * 1e3), quelle);
                if (!jeSchluessel.TryGetValue(schluessel, out Flaeche f))
                {
                    f = new Flaeche { N = n, S = s, Quelle = quelle };
                    jeSchluessel[schluessel] = f;
                    liste.Add(f);
                }
                f.Dreiecke.Add(t);
                f.FlaecheM2 += laenge / 2.0;
            }
            return liste;
        }

        /// <summary>Ordnet die Paarfläche der Fläche des Raums zu, deren Ebene den Schwerpunkt trägt.</summary>
        private static void Zuordnen(List<Flaeche> flaechen, double[] n, double[] schwerpunkt, int partner, double flaecheM2, double cosMax)
        {
            if (flaechen == null || n == null || schwerpunkt == null) return;
            Flaeche beste = flaechen.Where(f => Punkt(f.N, n) >= cosMax)
                                    .OrderBy(f => Math.Abs(Punkt(f.N, schwerpunkt) - f.S)).FirstOrDefault();
            if (beste == null || Math.Abs(Punkt(beste.N, schwerpunkt) - beste.S) > GRENZE_ABSTAND_M) return;
            beste.Partner[partner] = (beste.Partner.TryGetValue(partner, out double v) ? v : 0.0) + flaecheM2;
        }

        private static double Punkt(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        private static double[] Minus(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        private static double[] Kreuz(double[] a, double[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        private static string Ganz(int n) => n.ToString(CultureInfo.CurrentCulture);
        private static string Zahl(double x) => Math.Round(x, 2).ToString("0.##", CultureInfo.CurrentCulture);
    }
}
