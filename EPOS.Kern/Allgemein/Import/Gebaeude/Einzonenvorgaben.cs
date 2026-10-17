using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Felder des Einzonenwegs, die „Vorgaben der Baualtersklasse übernehmen…“ füllt (MA-7).</summary>
    public enum Einzonenfeld
    {
        /// <summary>U-Wert Außenwand [W/(m²K)].</summary>
        UAussenwand,
        /// <summary>U-Wert Fenster [W/(m²K)].</summary>
        UFenster,
        /// <summary>U-Wert Dach [W/(m²K)].</summary>
        UDach,
        /// <summary>U-Wert Bodenplatte (Grundfläche) [W/(m²K)].</summary>
        UGrund,
        /// <summary>U-Wert sonstige Flächen [W/(m²K)].</summary>
        USonstige,
        /// <summary>ψ Anschluss Fenster–Wand [W/(mK)].</summary>
        PsiFensterWand,
        /// <summary>ψ Anschluss Außenwand–Kellerdecke [W/(mK)].</summary>
        PsiAussenwandKeller,
        /// <summary>ψ Anschluss Wand–Dach [W/(mK)].</summary>
        PsiWandDach,
        /// <summary>Fensterdurchlassgrad g [–].</summary>
        Fensterdurchlassgrad,
        /// <summary>Bauart (0 leicht, 1 schwer, 2 sehr schwer, <see cref="Gebaeudebauweise"/>) — sie bestimmt die Bauweise.</summary>
        Bauart,
    }

    /// <summary>
    /// Die Werte des Einzonenwegs, die eine Baualtersklasse vorgibt — je Feld <c>null</c> = leer bzw. „keine Vorgabe“.
    /// Die Bauart steht als Zahl im selben Raster (0, 1, 2).
    /// </summary>
    public sealed record Einzonenwerte(double? UAussenwand, double? UFenster, double? UDach, double? UGrund, double? USonstige,
                                       double? PsiFensterWand, double? PsiAussenwandKeller, double? PsiWandDach,
                                       double? Fensterdurchlassgrad, int? Bauart)
    {
        /// <summary>Der Wert eines Feldes (die Bauart als Zahl).</summary>
        public double? Wert(Einzonenfeld feld) => feld switch
        {
            Einzonenfeld.UAussenwand => UAussenwand,
            Einzonenfeld.UFenster => UFenster,
            Einzonenfeld.UDach => UDach,
            Einzonenfeld.UGrund => UGrund,
            Einzonenfeld.USonstige => USonstige,
            Einzonenfeld.PsiFensterWand => PsiFensterWand,
            Einzonenfeld.PsiAussenwandKeller => PsiAussenwandKeller,
            Einzonenfeld.PsiWandDach => PsiWandDach,
            Einzonenfeld.Fensterdurchlassgrad => Fensterdurchlassgrad,
            Einzonenfeld.Bauart => Bauart,
            _ => null
        };

        /// <summary>Dieselben Werte mit einem anderen Wert für <paramref name="feld"/>.</summary>
        public Einzonenwerte Mit(Einzonenfeld feld, double? wert) => feld switch
        {
            Einzonenfeld.UAussenwand => this with { UAussenwand = wert },
            Einzonenfeld.UFenster => this with { UFenster = wert },
            Einzonenfeld.UDach => this with { UDach = wert },
            Einzonenfeld.UGrund => this with { UGrund = wert },
            Einzonenfeld.USonstige => this with { USonstige = wert },
            Einzonenfeld.PsiFensterWand => this with { PsiFensterWand = wert },
            Einzonenfeld.PsiAussenwandKeller => this with { PsiAussenwandKeller = wert },
            Einzonenfeld.PsiWandDach => this with { PsiWandDach = wert },
            Einzonenfeld.Fensterdurchlassgrad => this with { Fensterdurchlassgrad = wert },
            Einzonenfeld.Bauart => this with { Bauart = wert is double b ? (int)Math.Round(b) : null },
            _ => this
        };
    }

    /// <summary>
    /// Die Vorgabe einer Baualtersklasse (bzw. eines Energiestandards mit Katalogsätzen) für den Einzonenweg.
    /// </summary>
    /// <param name="Kennung">Der Buchstabe der Klasse oder der Code des Standards, aus dem die Werte stammen.</param>
    /// <param name="AusStandard">Stammen die Werte vom Energiestandard (Vorrang E47) statt von der Klasse?</param>
    /// <param name="Frei">Der freie Wert nach Stein/Loga (E51) statt des Katalogmedians?</param>
    /// <param name="Katalogsaetze">Zahl der Katalogsätze hinter den Medianen; 0 beim freien Wert.</param>
    /// <param name="Werte">Die Werte; ein leeres Feld gibt die Vorgabe nicht vor.</param>
    public sealed record Einzonenvorgabe(string Kennung, bool AusStandard, bool Frei, int Katalogsaetze, Einzonenwerte Werte);

    /// <summary>Eine Zeile der Vorschau: das Feld, sein Wert jetzt und der Wert der Vorgabe.</summary>
    public sealed record Einzonenposten(Einzonenfeld Feld, double? Alt, double? Neu)
    {
        /// <summary>Ändert die Übernahme dieses Feld?</summary>
        public bool Aendert => !Einzonenvorgaben.Gleich(Alt, Neu);
    }

    /// <summary>Warum „Vorgaben der Baualtersklasse übernehmen…“ nicht geht.</summary>
    public enum Einzonensperre
    {
        /// <summary>Es geht.</summary>
        Keine,
        /// <summary>Das Gebäude trägt Zonen — dann gilt der Zonenweg (Summenregel).</summary>
        Zonen,
        /// <summary>Keine Baualtersklasse gesetzt (oder keine bekannte).</summary>
        OhneKlasse,
        /// <summary>Die Klasse gibt keinen Wert vor.</summary>
        OhneWerte,
    }

    /// <summary>
    /// <b>„Vorgaben der Baualtersklasse übernehmen…“ im Einzonenweg</b> (Entwurf Geführter Modellaufbau 2.1 Nr. 1, E137 F4 (a),
    /// Welle MA-7): Baualtersklasse — und, wenn gesetzt und mit Katalogsätzen, der Energiestandard (Vorrang E47) — geben die
    /// U-Werte der Hüllgruppen, die drei ψ, den Fensterdurchlassgrad und die Bauart vor. Die Werte kommen aus derselben Tafel,
    /// mit der der Gebäudeimport füllt und die Stufe C der Vorschlagsregeln kennt (<see cref="GebaeudeVorgaben"/>: Median der
    /// Katalogbauten der Klasse, ohne Katalogsatz der freie Wert nach Stein/Loga 2025); die Bauart aus
    /// <see cref="BAUART_JE_KLASSE"/>.
    ///
    /// <para>Rein und plattformfrei: Vorgabe, Vorschau (alt → neu je Feld), Anwenden und Sperre. Geschrieben wird nichts —
    /// der Gebäudeeditor trägt die Werte in seinen Arbeitsstand, das OK schreibt sie; bestehende Gebäude ändern sich nur auf
    /// Klick des Anwenders.</para>
    /// </summary>
    public static class Einzonenvorgaben
    {
        /// <summary>
        /// <b>Die Bauart je Baualtersklasse A…M</b>: die häufigste Bauart der Katalogsätze der Klasse in
        /// <c>Tab_Gebaeude_STAMM</c> der Testdatenbank (<c>Referenzlaeufe/Kenndaten_Test.sqlite</c>, ohne den
        /// Referenzkatalogbau von 1051), gelesen über den spezifischen Wert <c>Bauweise / Wohnfläche</c>
        /// (<see cref="Gebaeudebauweise.BauartAusBauweise"/>; ohne Wohnfläche die Nutzfläche). In allen dreizehn Klassen ist es
        /// „schwer“; bei Gleichstand gälte die schwerere. Der Test <c>EinzonenvorgabenTests</c> rechnet die Tafel nach — wer den
        /// Katalog ändert, zieht sie hier nach. Kein Normwert, kein Herstellerwert.
        /// </summary>
        public static readonly IReadOnlyList<int> BAUART_JE_KLASSE = new[]
        {
            Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER,
            Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER,
            Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER, Gebaeudebauweise.SCHWER,
            Gebaeudebauweise.SCHWER,
        };

        /// <summary>Die Felder in der Reihenfolge der Vorschau.</summary>
        public static readonly IReadOnlyList<Einzonenfeld> Felder = (Einzonenfeld[])Enum.GetValues(typeof(Einzonenfeld));

        /// <summary>
        /// <b>Die Vorgabe</b> der Klasse <paramref name="klassenindex"/> (0 = A … 12 = M) mit dem Vorrang des Energiestandards
        /// (<see cref="GebaeudeVorgaben.Fuer(char?, string)"/>); <c>null</c> außerhalb A…M.
        /// </summary>
        public static Einzonenvorgabe Fuer(int klassenindex, string energiestandard = null)
        {
            if (klassenindex < 0 || klassenindex >= BAUART_JE_KLASSE.Count) return null;
            char klasse = (char)('A' + klassenindex);
            Baualtersvorgabe v = GebaeudeVorgaben.Fuer(klasse, string.IsNullOrWhiteSpace(energiestandard) ? null : energiestandard);
            if (v == null) return null;
            var werte = new Einzonenwerte(v.UAussenwand, v.UFenster, v.UDach, v.UGrund, v.USonstige,
                                          v.PsiFensterWand, v.PsiAussenwandKeller, v.PsiWandDach, v.GWert,
                                          BAUART_JE_KLASSE[klassenindex]);
            return new Einzonenvorgabe(v.Kennung, v.Energiestandard != null, v.Frei, v.Katalogsaetze, werte);
        }

        /// <summary>
        /// <b>Die Sperre</b>: Zonen vor fehlender Klasse vor einer Vorgabe ohne Werte; <see cref="Einzonensperre.Keine"/> = es geht.
        /// </summary>
        /// <param name="zonenzahl">Zahl der Zonen des Gebäudes im Arbeitsstand.</param>
        /// <param name="klasseGesetzt">Trägt das Gebäude eine Baualtersklasse?</param>
        /// <param name="vorgabe">Die Vorgabe (<see cref="Fuer"/>); <c>null</c> = keine.</param>
        public static Einzonensperre Sperre(int zonenzahl, bool klasseGesetzt, Einzonenvorgabe vorgabe)
        {
            if (zonenzahl > 0) return Einzonensperre.Zonen;
            if (!klasseGesetzt || vorgabe == null) return Einzonensperre.OhneKlasse;
            foreach (Einzonenfeld f in Felder)
                if (vorgabe.Werte.Wert(f) != null) return Einzonensperre.Keine;
            return Einzonensperre.OhneWerte;
        }

        /// <summary>
        /// <b>Die Vorschau</b>: je Feld, für das die Vorgabe einen Wert hat, der Wert jetzt und der neue — auch Felder, die
        /// schon so stehen (<see cref="Einzonenposten.Aendert"/> = <c>false</c>). Ein Feld ohne Vorgabe fehlt, es bleibt.
        /// </summary>
        public static IReadOnlyList<Einzonenposten> Vorschau(Einzonenwerte ist, Einzonenvorgabe vorgabe)
        {
            var liste = new List<Einzonenposten>();
            if (ist == null || vorgabe == null) return liste;
            foreach (Einzonenfeld f in Felder)
                if (vorgabe.Werte.Wert(f) is double neu) liste.Add(new Einzonenposten(f, ist.Wert(f), neu));
            return liste;
        }

        /// <summary><b>Anwenden</b>: die Werte nach der Übernahme — jedes Feld mit Vorgabe bekommt sie, die übrigen bleiben.</summary>
        public static Einzonenwerte Anwenden(Einzonenwerte ist, Einzonenvorgabe vorgabe)
        {
            if (ist == null || vorgabe == null) return ist;
            Einzonenwerte w = ist;
            foreach (Einzonenposten p in Vorschau(ist, vorgabe)) w = w.Mit(p.Feld, p.Neu);
            return w;
        }

        /// <summary>
        /// <b>Die Rücknahme</b> (Muster MA-1a): Jedes Feld, das noch so steht, wie die Übernahme es hinterließ, bekommt seinen
        /// Wert von davor; ein Feld, das der Anwender danach geändert hat, bleibt.
        /// </summary>
        public static Einzonenwerte Zuruecknehmen(Einzonenwerte jetzt, Einzonenwerte vorher, Einzonenwerte nachher)
        {
            if (jetzt == null || vorher == null || nachher == null) return jetzt;
            Einzonenwerte w = jetzt;
            foreach (Einzonenfeld f in Felder)
                if (Gleich(jetzt.Wert(f), nachher.Wert(f))) w = w.Mit(f, vorher.Wert(f));
            return w;
        }

        /// <summary>Gleichheit zweier Feldwerte (beide leer oder bis 1e-9 gleich).</summary>
        public static bool Gleich(double? a, double? b)
            => a is null ? b is null : b is double y && Math.Abs(a.Value - y) < 1e-9;
    }
}
