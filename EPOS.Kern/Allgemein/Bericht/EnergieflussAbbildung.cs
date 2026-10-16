﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Abbildung der Energieflussbilanz auf die Bilddaten des Renderers</b> (Konzept Energieflussbild, Abschnitte
    /// 2.2, 3.1 und 3.4, Schritte SK-12 und SK-2c): <see cref="EnergieflussBilanz"/> → <see cref="EnergieflussBildDaten"/>
    /// nach Variante A „Gestraffte Kette“. Sie rechnet keine Fachgröße neu (jede Menge kommt aus der Bilanz) und zeichnet
    /// nichts (Spalte, Lage, Maßstab und Beschriftung sind Sache von <c>ChartRenderer.EnergieflussModell</c>); die
    /// Hinweiszeilen bleiben an der Bilanz. Die Bilanz selbst bleibt unverändert — Tabelle, KI-Text und Textalternative
    /// führen Speicher und Kleinstverluste weiterhin als eigene Zeilen.
    ///
    /// <para><b>Was die Abbildung tut:</b></para>
    /// <list type="bullet">
    /// <item><b>Speicher falten (SK-Q13):</b> Ein Speicherknoten verschwindet aus dem Bild. Seine Entladung fließt als Band
    /// des Busses in die Senken (ein schon vorhandenes direktes Band wird um den Speicheranteil breiter, der Zeigertext
    /// nennt „davon x MWh/a über Speicher“), Verlust und Füllstandsänderung werden Abzweige am Bus, Ladung und Entladung
    /// stehen im Zeigertext des Busses, dessen Name den Zusatz „· Speicher“ trägt.</item>
    /// <item><b>Kleinstverluste (SK-Q15):</b> Ein Verlust unter <see cref="KLEINVERLUST_ANTEIL"/> seines Erzeugers bzw.
    /// Busses wird nicht gezeichnet; er steht im Zeigertext des Knotens.</item>
    /// <item><b>Nullbänder:</b> Ein Band unter <see cref="MINDEST_BAND_MWH"/> (es stünde als „0,0“ im Bild) entfällt; ein
    /// Knoten ohne Band entfällt.</item>
    /// <item><b>Spur (3.1):</b> Busse nach ihrer Kennung; Wandler nach den Formen ihrer Abgabe (Strom und Wärme = Gemischt);
    /// Senken nach der Spur ihres größten Zuflusses; Quellen nach der Spur ihres größten Abflusses; Verlustknoten nach
    /// ihrem Eigentümer.</item>
    /// <item><b>Farbrolle = Energieform des Bandes (3.4, SK-Q14):</b> <see cref="Farbrolle.STROM"/>, <see cref="Farbrolle.WAERME"/>,
    /// <see cref="Farbrolle.KAELTE"/>, <see cref="Farbrolle.BRENNSTOFF"/>, <see cref="Farbrolle.UMWELTWAERME"/>,
    /// <see cref="Farbrolle.VERLUST"/>, <see cref="Farbrolle.REST"/> (gestrichelt); Knoten <see cref="Farbrolle.KNOTEN"/>.</item>
    /// <item><b>Legende:</b> die vorkommenden Energieformen in fester Reihenfolge, Texte in der Sprache der Bilanz.</item>
    /// </list>
    /// </summary>
    public static class EnergieflussAbbildung
    {
        /// <summary>Ein Verlust unter diesem Anteil am Zufluss bzw. Abfluss seines Knotens wird nicht gezeichnet (nur Zeigertext).</summary>
        public const double KLEINVERLUST_ANTEIL = 0.01;

        /// <summary>Ein Band unter dieser Menge [MWh/a] entfällt im Bild — es stünde als „0,0“ da.</summary>
        public const double MINDEST_BAND_MWH = 0.05;

        /// <summary>Die Reihenfolge der Energieformen in der Legende.</summary>
        private static readonly EnergieflussForm[] LEGENDENFOLGE =
        {
            EnergieflussForm.Strom, EnergieflussForm.Waerme, EnergieflussForm.Kaelte, EnergieflussForm.Brennstoff,
            EnergieflussForm.Umwelt, EnergieflussForm.Verlust, EnergieflussForm.Rest
        };

        // ---------------------------------------------------------------- Arbeitskopien

        private sealed class AKnoten
        {
            public EnergieflussKnoten Q;
            public string Id, Name, Kurzname, Zeigertext;
            public EnergieflussKnotenart Art;
            public double Zu, Ab;
            public int Rang;
            public EnergieflussSpur? Spur;
        }

        private sealed class ABand
        {
            public EnergieflussFluss Q;
            public string Von, Nach, Zeigertext;
            public double Mwh;
            public EnergieflussForm Form;
            public double UeberSpeicher;
            public readonly List<string> Speicher = new List<string>();
        }

        /// <summary>
        /// Die Bilddaten zur Bilanz nach den Regeln der Klassenbeschreibung. Eine leere oder fehlende Bilanz ergibt leere
        /// Bilddaten (der Renderer zeichnet dann ein leeres Bild).
        /// </summary>
        public static EnergieflussBildDaten Bild(EnergieflussBilanz bilanz)
        {
            var daten = new EnergieflussBildDaten();
            if (bilanz == null) return daten;
            bool en = bilanz.Englisch;
            CultureInfo kultur = Energiefluss.Kultur(en);

            var knoten = new Dictionary<string, AKnoten>(StringComparer.Ordinal);
            var folge = new List<AKnoten>();
            int rang = 0;
            foreach (EnergieflussKnoten k in bilanz.Knoten)
            {
                if (k == null || string.IsNullOrEmpty(k.Id) || knoten.ContainsKey(k.Id)) continue;
                var a = new AKnoten
                {
                    Q = k, Id = k.Id, Name = k.Name ?? "", Kurzname = k.Kurzname ?? "", Art = k.Art,
                    Zu = k.ZuflussMwh, Ab = k.AbflussMwh, Rang = rang++,
                    Zeigertext = string.IsNullOrEmpty(k.Zeigertext) ? null : k.Zeigertext,
                };
                knoten[a.Id] = a;
                folge.Add(a);
            }
            var baender = new List<ABand>();
            foreach (EnergieflussFluss f in bilanz.Fluesse)
            {
                if (f == null || !double.IsFinite(f.Mwh) || f.Mwh <= 0.0) continue;
                if (string.IsNullOrEmpty(f.Von) || string.IsNullOrEmpty(f.Nach) || f.Von == f.Nach) continue;
                if (!knoten.ContainsKey(f.Von) || !knoten.ContainsKey(f.Nach)) continue;
                baender.Add(new ABand
                {
                    Q = f, Von = f.Von, Nach = f.Nach, Mwh = f.Mwh, Form = f.Form,
                    Zeigertext = string.IsNullOrEmpty(f.Zeigertext) ? null : f.Zeigertext,
                });
            }

            SpeicherFalten(knoten, folge, baender, en, kultur);
            baender.RemoveAll(b => b.Mwh < MINDEST_BAND_MWH);
            Kleinverluste(knoten, baender, en, kultur);
            // Ein Knoten ohne Band entfällt.
            var mitBand = new HashSet<string>(baender.Select(b => b.Von).Concat(baender.Select(b => b.Nach)), StringComparer.Ordinal);
            folge.RemoveAll(k => !mitBand.Contains(k.Id));
            foreach (string id in knoten.Keys.Where(id => !mitBand.Contains(id)).ToList()) knoten.Remove(id);
            Spuren(knoten, folge, baender);

            daten.Knoten = folge.Select(Bildknoten).ToList();
            daten.Baender = baender.Select(Bildband).ToList();
            daten.Legende = Legende(baender, en);
            return daten;
        }

        // ---------------------------------------------------------------- Speicher falten

        private static void SpeicherFalten(Dictionary<string, AKnoten> knoten, List<AKnoten> folge, List<ABand> baender,
                                           bool en, CultureInfo kultur)
        {
            foreach (AKnoten s in folge.Where(k => k.Art == EnergieflussKnotenart.Speicher).ToList())
            {
                List<ABand> ein = baender.Where(b => b.Nach == s.Id).ToList();
                if (ein.Count == 0) continue;
                // Der Bus des Speichers: die größte Ladung aus einem Bus, sonst die größte Ladung überhaupt.
                ABand ladung = ein.Where(b => knoten[b.Von].Art == EnergieflussKnotenart.Bus).OrderByDescending(b => b.Mwh).FirstOrDefault()
                               ?? ein.OrderByDescending(b => b.Mwh).First();
                AKnoten bus = knoten[ladung.Von];
                double ladungMwh = ein.Sum(b => b.Mwh), entladung = 0.0;

                foreach (ABand b in ein)
                {
                    baender.Remove(b);
                    if (b.Von == bus.Id) continue;
                    // Ein Zufluss aus einem anderen Knoten (etwa Solarwärme) geht in den Bus.
                    Verschmelzen(baender, b.Von, bus.Id, b, null);
                }
                foreach (ABand b in baender.Where(b => b.Von == s.Id).ToList())
                {
                    AKnoten ziel = knoten[b.Nach];
                    if (ziel.Art == EnergieflussKnotenart.Verlust)
                    {
                        b.Von = bus.Id;   // Verlust und Füllstandsänderung: Abzweig am Bus, Zeigertext der Bilanz bleibt
                        continue;
                    }
                    entladung += b.Mwh;
                    baender.Remove(b);
                    Verschmelzen(baender, bus.Id, b.Nach, b, s.Name);
                }
                // Die verschmolzenen Bänder bekommen ihren Zeigertext aus Summe und Speicheranteil.
                foreach (ABand b in baender.Where(b => b.Von == bus.Id && b.UeberSpeicher > 0.0))
                {
                    AKnoten ziel = knoten[b.Nach];
                    double bezug = ziel.Zu > 0.0 ? ziel.Zu : b.Mwh;
                    string z = string.Format(kultur, Energiefluss.T(nameof(MyResource.Resource.EF_ZEIGER_FLUSS), en), bus.Name, ziel.Name,
                                             Energiefluss.Zahl(b.Mwh, kultur), Energiefluss.Prozent(100.0 * b.Mwh / bezug, kultur));
                    z += " · " + string.Format(kultur, Energiefluss.T(nameof(MyResource.Resource.EF_ZEIGER_UEBER_SPEICHER), en),
                                               Energiefluss.Zahl(b.UeberSpeicher, kultur), string.Join(", ", b.Speicher));
                    b.Zeigertext = z;
                }
                string speicher = string.Format(kultur, Energiefluss.T(nameof(MyResource.Resource.EF_ZEIGER_SPEICHER_AM_BUS), en),
                                                s.Name, Energiefluss.Zahl(ladungMwh, kultur), Energiefluss.Zahl(entladung, kultur));
                if (!string.IsNullOrEmpty(s.Q?.Zusatz)) speicher += " · " + s.Q.Zusatz;
                bus.Zeigertext = (bus.Zeigertext ?? Knotentext(bus, en, kultur)) + " · " + speicher;
                string zusatz = " · " + Energiefluss.T(nameof(MyResource.Resource.EF_SP_SPEICHER), en);
                if (!bus.Name.EndsWith(zusatz, StringComparison.Ordinal)) bus.Name += zusatz;
                folge.Remove(s);
                knoten.Remove(s.Id);
            }
        }

        /// <summary>Das Band <paramref name="quelle"/> auf die Kante von → nach legen: vorhandenes Band verbreitern, sonst neu.</summary>
        private static void Verschmelzen(List<ABand> baender, string von, string nach, ABand quelle, string speicherName)
        {
            ABand ziel = baender.FirstOrDefault(b => b.Von == von && b.Nach == nach);
            if (ziel == null)
            {
                ziel = new ABand { Q = quelle.Q, Von = von, Nach = nach, Mwh = 0.0, Form = quelle.Form, Zeigertext = null };
                baender.Add(ziel);
            }
            ziel.Mwh += quelle.Mwh;
            if (speicherName != null)
            {
                ziel.UeberSpeicher += quelle.Mwh;
                if (!ziel.Speicher.Contains(speicherName)) ziel.Speicher.Add(speicherName);
            }
        }

        private static string Knotentext(AKnoten k, bool en, CultureInfo kultur)
            => string.Format(kultur, Energiefluss.T(nameof(MyResource.Resource.EF_ZEIGER_KNOTEN), en), k.Name,
                             Energiefluss.Zahl(k.Zu, kultur), Energiefluss.Zahl(k.Ab, kultur));

        // ---------------------------------------------------------------- Kleinstverluste

        private static void Kleinverluste(Dictionary<string, AKnoten> knoten, List<ABand> baender, bool en, CultureInfo kultur)
        {
            foreach (ABand b in baender.Where(b => knoten[b.Nach].Art == EnergieflussKnotenart.Verlust).ToList())
            {
                AKnoten o = knoten[b.Von], v = knoten[b.Nach];
                if (b.Mwh >= KLEINVERLUST_ANTEIL * Math.Max(o.Zu, o.Ab)) continue;
                baender.Remove(b);
                o.Zeigertext = (o.Zeigertext ?? Knotentext(o, en, kultur)) + " · " +
                               string.Format(kultur, Energiefluss.T(nameof(MyResource.Resource.EF_ZEIGER_KLEINVERLUST), en), v.Name,
                                             Energiefluss.Zahl(b.Mwh, kultur));
            }
        }

        // ---------------------------------------------------------------- Spuren

        /// <summary>Die Spur zu einer Energieform (Rückfall, wenn kein Nachbar die Spur vorgibt).</summary>
        public static EnergieflussSpur SpurDerForm(EnergieflussForm form)
            => form == EnergieflussForm.Strom ? EnergieflussSpur.Strom
             : form == EnergieflussForm.Kaelte ? EnergieflussSpur.Kaelte
             : EnergieflussSpur.Waerme;

        private static void Spuren(Dictionary<string, AKnoten> knoten, List<AKnoten> folge, List<ABand> baender)
        {
            // 1. Busse nach Kennung, Wandler nach den Formen ihrer Abgabe.
            foreach (AKnoten k in folge)
            {
                if (k.Art == EnergieflussKnotenart.Bus)
                    k.Spur = k.Id == Energiefluss.STROM ? EnergieflussSpur.Strom
                           : k.Id == Energiefluss.KAELTE ? EnergieflussSpur.Kaelte
                           : k.Id == Energiefluss.WAERME ? EnergieflussSpur.Waerme
                           : SpurDerForm(GroessteForm(baender.Where(b => b.Nach == k.Id)));
                else if (k.Art == EnergieflussKnotenart.Wandler || k.Art == EnergieflussKnotenart.Speicher)
                {
                    List<ABand> ab = baender.Where(b => b.Von == k.Id && knoten[b.Nach].Art != EnergieflussKnotenart.Verlust).ToList();
                    bool strom = ab.Any(b => b.Form == EnergieflussForm.Strom), waerme = ab.Any(b => b.Form == EnergieflussForm.Waerme),
                         kaelte = ab.Any(b => b.Form == EnergieflussForm.Kaelte);
                    k.Spur = strom && (waerme || kaelte) ? EnergieflussSpur.Gemischt
                           : kaelte && !waerme ? EnergieflussSpur.Kaelte
                           : strom ? EnergieflussSpur.Strom
                           : EnergieflussSpur.Waerme;
                }
            }
            // 2. Senken nach ihrem größten Zufluss, 3. Quellen nach ihrem größten Abfluss, 4. Verluste nach dem Eigentümer.
            foreach (AKnoten k in folge.Where(k => k.Art == EnergieflussKnotenart.Senke))
            {
                ABand b = baender.Where(x => x.Nach == k.Id).OrderByDescending(x => x.Mwh).FirstOrDefault();
                k.Spur = b != null && knoten[b.Von].Spur.HasValue ? knoten[b.Von].Spur : (EnergieflussSpur?)SpurDerForm(b?.Form ?? EnergieflussForm.Waerme);
            }
            foreach (AKnoten k in folge.Where(k => k.Art == EnergieflussKnotenart.Quelle))
            {
                ABand b = baender.Where(x => x.Von == k.Id).OrderByDescending(x => x.Mwh).FirstOrDefault();
                k.Spur = b != null && knoten[b.Nach].Spur.HasValue ? knoten[b.Nach].Spur : (EnergieflussSpur?)SpurDerForm(b?.Form ?? EnergieflussForm.Waerme);
            }
            foreach (AKnoten k in folge.Where(k => k.Art == EnergieflussKnotenart.Verlust))
            {
                ABand b = baender.FirstOrDefault(x => x.Nach == k.Id);
                k.Spur = b != null && knoten[b.Von].Spur.HasValue ? knoten[b.Von].Spur : (EnergieflussSpur?)EnergieflussSpur.Waerme;
            }
            foreach (AKnoten k in folge.Where(k => !k.Spur.HasValue))
                k.Spur = SpurDerForm(GroessteForm(baender.Where(b => b.Von == k.Id || b.Nach == k.Id)));
        }

        private static EnergieflussForm GroessteForm(IEnumerable<ABand> baender)
        {
            ABand b = baender.OrderByDescending(x => x.Mwh).FirstOrDefault();
            return b?.Form ?? EnergieflussForm.Waerme;
        }

        // ---------------------------------------------------------------- Knoten

        private static EnergieflussBildKnoten Bildknoten(AKnoten k)
            => new EnergieflussBildKnoten
            {
                Kennung = k.Id,
                Spur = k.Spur ?? EnergieflussSpur.Waerme,
                Name = k.Name,
                Kurzname = k.Kurzname,
                Mwh = k.Art == EnergieflussKnotenart.Quelle ? k.Ab : k.Zu,
                Rolle = k.Art == EnergieflussKnotenart.Verlust ? Farbrolle.VERLUST : Farbrolle.KNOTEN,
                Art = k.Art,
                Zeigertext = k.Zeigertext,
            };

        /// <summary>Die Jahressumme, die der Bericht am Knoten nennt: Quellen ihren Abfluss, alle anderen ihren Zufluss.</summary>
        public static double Jahressumme(EnergieflussKnoten k)
            => k.Art == EnergieflussKnotenart.Quelle ? k.AbflussMwh : k.ZuflussMwh;

        // ---------------------------------------------------------------- Bänder

        private static EnergieflussBildBand Bildband(ABand b)
            => new EnergieflussBildBand
            {
                Von = b.Von,
                Nach = b.Nach,
                Mwh = b.Mwh,
                Rolle = Rolle(b.Form),
                Art = Bandart(b.Form),
                Zeigertext = b.Zeigertext,
            };

        /// <summary>Verlustbänder sind Verlust, „ungedeckt“ und „nicht zugeordnet“ gestrichelt; alles andere ein gewöhnliches Band.</summary>
        public static EnergieflussBandart Bandart(EnergieflussForm form)
            => form == EnergieflussForm.Verlust ? EnergieflussBandart.Verlust
             : form == EnergieflussForm.Rest ? EnergieflussBandart.NichtZugeordnet
             : EnergieflussBandart.Normal;

        /// <summary>Die Farbrolle eines Bandes: seine Energieform (Konzept 3.4, SK-Q14).</summary>
        public static Farbrolle Rolle(EnergieflussForm form)
        {
            switch (form)
            {
                case EnergieflussForm.Strom: return Farbrolle.STROM;
                case EnergieflussForm.Waerme: return Farbrolle.WAERME;
                case EnergieflussForm.Kaelte: return Farbrolle.KAELTE;
                case EnergieflussForm.Brennstoff: return Farbrolle.BRENNSTOFF;
                case EnergieflussForm.Umwelt: return Farbrolle.UMWELTWAERME;
                case EnergieflussForm.Verlust: return Farbrolle.VERLUST;
                default: return Farbrolle.REST;
            }
        }

        // ---------------------------------------------------------------- Legende

        /// <summary>Der Legendentext einer Energieform in der Sprache des Bildes.</summary>
        public static string Legendentext(EnergieflussForm form, bool englisch)
        {
            string schluessel;
            switch (form)
            {
                case EnergieflussForm.Strom: schluessel = nameof(MyResource.Resource.EF_LEG_STROM); break;
                case EnergieflussForm.Waerme: schluessel = nameof(MyResource.Resource.EF_LEG_WAERME); break;
                case EnergieflussForm.Kaelte: schluessel = nameof(MyResource.Resource.EF_LEG_KAELTE); break;
                case EnergieflussForm.Brennstoff: schluessel = nameof(MyResource.Resource.EF_LEG_BRENNSTOFF); break;
                case EnergieflussForm.Umwelt: schluessel = nameof(MyResource.Resource.EF_LEG_UMWELT); break;
                case EnergieflussForm.Verlust: schluessel = nameof(MyResource.Resource.EF_LEG_VERLUST); break;
                default: schluessel = nameof(MyResource.Resource.EF_LEG_REST); break;
            }
            return Energiefluss.T(schluessel, englisch);
        }

        private static IReadOnlyList<EnergieflussBildLegende> Legende(List<ABand> baender, bool en)
        {
            var formen = new HashSet<EnergieflussForm>(baender.Select(b => b.Form));
            return LEGENDENFOLGE.Where(formen.Contains)
                                .Select(f => new EnergieflussBildLegende { Rolle = Rolle(f), Text = Legendentext(f, en), Gestrichelt = f == EnergieflussForm.Rest })
                                .ToList();
        }
    }
}
