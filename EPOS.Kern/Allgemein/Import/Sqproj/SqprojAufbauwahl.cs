using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Woher der Aufbau eines Bauteils stammt</b> — die Rangfolge nach Entscheid E97 (Konzept Bauteilaufbau 5.5, BA-4b); mit der
    /// Aufbauquelle „Projektdatei“ (Anwenderentscheid vom 08.10.2026) trägt Rang 1 den Aufbau der Projektdatei ohne U-Abgleich.
    /// </summary>
    internal enum Aufbaurang
    {
        /// <summary>Nicht bestimmt (ohne Projektdatei bzw. transparent).</summary>
        Keiner = 0,
        /// <summary>1: der Aufbau der Projektdatei an der Hüllfläche — sein U passt auf 1 % zum U der IFC (ohne IFC-U bzw. mit Aufbauquelle
        /// „Projektdatei“: direkt).</summary>
        Projektdatei = 1,
        /// <summary>2: ein Aufbau aus dem Aufbaukatalog der Projektdatei, dessen U eindeutig auf 1 % das U der IFC trifft.</summary>
        Projektkatalog = 2,
        /// <summary>3: die Schichten der IFC (BA-1, Relevanzregel).</summary>
        IfcSchichten = 3,
        /// <summary>4: kein vollständiger Aufbau — Ersatzaufbau (BA-2) bzw. ohne Aufbau.</summary>
        Ersatz = 4,
    }

    /// <summary>
    /// <b>Die Entscheidung je IFC-Bauteil</b>: die zugeordnete Hüllfläche, der gewählte Aufbau der Projektdatei (Rang 1 oder 2,
    /// sonst <c>null</c> mit <see cref="Aufbaurang.Keiner"/>), die beiden U-Werte und ob die Schichtfolge für den ersten
    /// Nachbarn des Bauteils umzudrehen ist.
    /// </summary>
    internal sealed class SqprojAufbauentscheid
    {
        internal SqprojHuellflaeche Flaeche { get; init; }
        internal Aufbaurang Rang { get; init; }
        internal SqprojAufbau Aufbau { get; init; }
        internal double? UProjektdatei { get; init; }
        internal double? UIfc { get; init; }
        internal bool Umgekehrt { get; init; }
        internal bool RichtungAngenommen { get; init; }
        internal bool KatalogMehrdeutig { get; init; }
        internal bool UeberGuid { get; init; }

        /// <summary>Weicht das U der Projektdatei an der Hüllfläche um mehr als 1 % vom U der IFC ab?</summary>
        internal bool UAbweichend => UProjektdatei > 0.0 && UIfc > 0.0 && !SqprojAufbauwahl.Passt(UProjektdatei.Value, UIfc.Value);
    }

    /// <summary>
    /// <b>Zuordnung und Rangfolge der Aufbauten aus der Projektdatei</b> (BA-4b; Konzept Bauteilaufbau 5.5, Befund Projektdatei
    /// N.6/N.7/N.10). Plattformfrei, ohne Datenbank, schreibt nichts.
    /// <para><b>Welcher Stand gilt</b> (Anwenderentscheid vom 08.10.2026, ersetzt E97 als feste Vorgabe): Beim Weg „IFC + Projektdatei“
    /// prüft der Import zuerst, ob beide Dateien zum selben Projektstand gehören (<see cref="Standpruefung"/>). Schlägt die Prüfung
    /// nicht an oder wählt der Anwender „IFC“ (<see cref="Aufbauquelle.Ifc"/>), gilt die Rangfolge E97 unten — der Stand der IFC geht
    /// vor, ein Aufbau der Projektdatei nur, wenn sein U zum U der IFC passt. Wählt er „Projektdatei“ (<see cref="Aufbauquelle.Projektdatei"/>),
    /// gilt je Bauteil der zugewiesene Aufbau der Projektdatei ohne U-Abgleich (Rang 1) und ihr U (<see cref="SqprojStand.UWirksam"/>);
    /// solange die Wahl offen ist, ist der Zuordnungsstand unvollständig.</para>
    /// <list type="number">
    /// <item><b>Zuordnen</b>: IFC-Bauteil → Eigenschaft <c>GUID</c> (<see cref="AbbildBauteil.HottcadGuid"/>) → Level-3-<c>GId</c> der
    /// Hüllfläche; Ausweich: die dekodierte <c>GlobalId</c> (<see cref="SqprojRaumabgleich.IfcKennung"/>). Trägt eine <c>GId</c>
    /// mehrere Hüllflächen mit verschiedenen Aufbauten, wird nicht geraten.</item>
    /// <item><b>Rang 1</b>: der Aufbau an der Hüllfläche, wenn sein U auf <see cref="U_TOLERANZ"/> zum U der IFC passt (ohne IFC-U
    /// direkt). <b>Rang 2</b>: sonst der Aufbau im Katalog der Datei (auch unbenutzte), dessen U eindeutig das U der IFC trifft
    /// (gleiche Schichtfolgen zählen als einer; liegen mehrere im Band, entscheidet ein eindeutig gleiches U
    /// (<see cref="U_GLEICH"/>); sonst nicht raten). Sonst bleibt das Bauteil auf dem Weg der IFC
    /// (Rang 3 bzw. 4, entschieden im <see cref="GebaeudeBauteilvorschlag"/>).</item>
    /// <item><b>Richtung</b>: die Schichten laufen innen → außen aus Sicht des Innenraums der Hüllfläche
    /// (<see cref="SqprojHuellflaeche.InnenRaum"/>); liegt dieser Raum im IFC-Bauteil nicht an erster Stelle, wird die Folge
    /// umgedreht. Ohne abgeglichenen Raum entscheidet bei einer Decke die Sicht „Boden“ (oberer Raum), sonst gilt die Folge für
    /// den ersten Nachbarn (Richtung angenommen, wenn es zwei sind).</item>
    /// </list>
    /// </summary>
    internal sealed class SqprojAufbauwahl
    {
        /// <summary>Die Toleranz des U-Abgleichs (E97): |U₁/U₂ − 1| ≤ 1 %.</summary>
        internal const double U_TOLERANZ = 0.01;

        /// <summary>Ein „gleiches“ U (Befund N.7): |U₁/U₂ − 1| ≤ 0,01 % — entscheidet unter mehreren Katalogtreffern im Band.</summary>
        internal const double U_GLEICH = 1e-4;

        private readonly Dictionary<AbbildBauteil, SqprojAufbauentscheid> _je = new Dictionary<AbbildBauteil, SqprojAufbauentscheid>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, AbbildAufbau> _abbilder = new Dictionary<string, AbbildAufbau>(StringComparer.Ordinal);

        private SqprojAufbauwahl() { }

        /// <summary>Die Entscheidungen je zugeordnetem IFC-Bauteil.</summary>
        internal IReadOnlyDictionary<AbbildBauteil, SqprojAufbauentscheid> Entscheide => _je;

        /// <summary>Opake IFC-Bauteile ohne Gegenstück in der Projektdatei.</summary>
        internal List<AbbildBauteil> OhneGegenstueck { get; } = new List<AbbildBauteil>();

        /// <summary>Opake IFC-Bauteile, deren Schlüssel mehrere Hüllflächen mit verschiedenen Aufbauten trifft (nicht geraten).</summary>
        internal List<AbbildBauteil> Mehrdeutig { get; } = new List<AbbildBauteil>();

        /// <summary>Zugeordnet über die Eigenschaft <c>GUID</c> bzw. über die dekodierte <c>GlobalId</c>.</summary>
        internal int UeberGuid { get; private set; }
        internal int UeberGlobalId { get; private set; }

        /// <summary>Passen zwei U-Werte auf <see cref="U_TOLERANZ"/>?</summary>
        internal static bool Passt(double u1, double u2) => u1 > 0.0 && u2 > 0.0 && Math.Abs(u1 / u2 - 1.0) <= U_TOLERANZ + 1e-12;

        /// <summary>Ist die Bauteilart opak mit Schichten (keine Öffnung)?</summary>
        internal static bool Opak(Bauteilart art) => art != Bauteilart.Fenster && art != Bauteilart.Tuer && art != Bauteilart.Vorhangfassade;

        /// <summary>Die Entscheidung für ein IFC-Bauteil; <c>null</c> = nicht zugeordnet.</summary>
        internal SqprojAufbauentscheid Entscheid(AbbildBauteil b) => b != null && _je.TryGetValue(b, out SqprojAufbauentscheid e) ? e : null;

        /// <summary>
        /// <b>Bildet die Zuordnung</b> der opaken Bauteile eines IFC-Gebäudes zur Projektdatei. Ohne gelesene Bauteiltabellen
        /// eine leere Wahl.
        /// </summary>
        /// <param name="projektdateiGilt">Aufbauquelle „Projektdatei“: der zugewiesene Aufbau der Projektdatei ohne U-Abgleich.</param>
        internal static SqprojAufbauwahl Bilden(SqprojAbbild projekt, SqprojRaumabgleich abgleich, AbbildGebaeude ifc, bool projektdateiGilt = false)
        {
            var w = new SqprojAufbauwahl();
            if (projekt == null || ifc == null || !projekt.BauteileGelesen) return w;

            var schluessel = new SqprojBauteilschluessel(projekt);
            List<SqprojAufbau> katalog = projekt.Aufbauten.Values.Where(a => a.HatSchichten).OrderBy(a => a.Kennung, StringComparer.Ordinal).ToList();

            foreach (AbbildBauteil b in ifc.Bauteile)
            {
                if (!Opak(b.Art)) continue;
                List<SqprojHuellflaeche> treffer = schluessel.Treffer(b, out bool ueberGuid);
                if (treffer == null)
                {
                    w.OhneGegenstueck.Add(b);
                    continue;
                }
                if (SqprojBauteilschluessel.Mehrdeutig(treffer))
                {
                    w.Mehrdeutig.Add(b);
                    continue;
                }
                if (ueberGuid) w.UeberGuid++; else w.UeberGlobalId++;
                w._je[b] = Entscheiden(b, treffer[0], projekt, katalog, abgleich, ueberGuid, projektdateiGilt);
            }
            return w;
        }

        private static SqprojAufbauentscheid Entscheiden(AbbildBauteil b, SqprojHuellflaeche h, SqprojAbbild projekt, List<SqprojAufbau> katalog,
                                                         SqprojRaumabgleich abgleich, bool ueberGuid, bool projektdateiGilt)
        {
            SqprojAufbau eigen = h.AufbauKennung != null && projekt.Aufbauten.TryGetValue(h.AufbauKennung, out SqprojAufbau x) ? x : null;
            double? uIfc = b.UWertWm2K > 0.0 ? b.UWertWm2K : null;
            double? uPd = eigen?.UWert ?? h.UWert;
            SqprojAufbau gewaehlt = null;
            Aufbaurang rang = Aufbaurang.Keiner;
            bool mehrdeutig = false;
            if (eigen != null && eigen.HatSchichten && (projektdateiGilt || !uIfc.HasValue || Passt(eigen.UWert.Value, uIfc.Value)))
            {
                gewaehlt = eigen;
                rang = Aufbaurang.Projektdatei;
            }
            else if (uIfc.HasValue && !projektdateiGilt)
            {
                List<List<SqprojAufbau>> gruppen = katalog.Where(a => Passt(a.UWert.Value, uIfc.Value))
                                                          .GroupBy(a => a.Signatur, StringComparer.Ordinal).Select(g => g.ToList()).ToList();
                // Liegen mehrere Schichtfolgen im Band, entscheidet ein gleiches U (Befund N.7: < 0,01 %), wenn es eindeutig ist.
                List<List<SqprojAufbau>> gleich = gruppen.Where(g => Math.Abs(g[0].UWert.Value / uIfc.Value - 1.0) <= U_GLEICH).ToList();
                if (gruppen.Count > 1 && gleich.Count == 1) gruppen = gleich;
                if (gruppen.Count == 1)
                {
                    gewaehlt = gruppen[0][0];
                    rang = Aufbaurang.Projektkatalog;
                }
                else mehrdeutig = gruppen.Count > 1;
            }

            // Richtung: innen → außen aus Sicht des Innenraums der Hüllfläche.
            bool umgekehrt = false, angenommen = false;
            string raum = abgleich?.IfcRaum(h.InnenRaum);
            int pos = raum == null ? -1 : b.Nachbarn.FindIndex(n => string.Equals(n.Kennung, raum, StringComparison.Ordinal));
            if (pos >= 0) umgekehrt = pos > 0;
            else if (b.Art == Bauteilart.Decke && b.Nachbarn.FindIndex(n => GebaeudeAggregation.SichtIstBoden(n.Sicht) == true) is int boden && boden >= 0)
                umgekehrt = boden > 0;
            else angenommen = b.Nachbarn.Count > 1;

            return new SqprojAufbauentscheid
            {
                Flaeche = h, Rang = rang, Aufbau = gewaehlt, UProjektdatei = uPd, UIfc = uIfc, Umgekehrt = umgekehrt,
                RichtungAngenommen = angenommen, KatalogMehrdeutig = mehrdeutig, UeberGuid = ueberGuid,
            };
        }

        /// <summary>
        /// <b>Der Aufbau eines Entscheids als Aufbau des Abbilds</b> — Schichten innen → außen für den ersten Nachbarn des Bauteils
        /// (umgedreht über <see cref="Schichtrichtung.AussenNachInnen"/>), Stoffwerte der Schicht, Kennzeichen
        /// <see cref="AbbildSchicht.AusProjektdatei"/>; je Aufbau und Richtung einmal. <c>null</c> ohne gewählten Aufbau.
        /// </summary>
        internal AbbildAufbau Abbild(SqprojAufbauentscheid e)
        {
            if (e?.Aufbau == null) return null;
            string schluessel = e.Aufbau.Kennung + "|" + (e.Umgekehrt ? "U" : "G") + (e.RichtungAngenommen ? "A" : "");
            if (_abbilder.TryGetValue(schluessel, out AbbildAufbau bekannt)) return bekannt;
            AbbildAufbau a = AbbildAus(e.Aufbau, e.Umgekehrt, e.RichtungAngenommen);
            _abbilder[schluessel] = a;
            return a;
        }

        /// <summary>
        /// <b>Ein Aufbau der Projektdatei als Aufbau des Abbilds</b> — Schichten innen → außen (umgedreht über
        /// <see cref="Schichtrichtung.AussenNachInnen"/>), Stoffwerte der Schicht, Kennzeichen <see cref="AbbildSchicht.AusProjektdatei"/>.
        /// Der eine Weg für den IFC-Weg mit Projektdatei (<see cref="Abbild"/>) und das Abbild allein aus der Projektdatei.
        /// </summary>
        internal static AbbildAufbau AbbildAus(SqprojAufbau aufbau, bool umgekehrt, bool richtungAngenommen)
        {
            var a = new AbbildAufbau
            {
                Kennung = aufbau.Kennung,
                Name = aufbau.Name ?? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_SQ_AUFBAU_NAME,
                                                    (aufbau.UWert ?? 0.0).ToString("0.###", CultureInfo.CurrentCulture)),
                UWertWm2K = aufbau.UWert,
                Richtung = umgekehrt ? Schichtrichtung.AussenNachInnen : Schichtrichtung.InnenNachAussen,
                RichtungAngenommen = richtungAngenommen,
            };
            foreach (SqprojSchicht s in aufbau.Schichten)
                a.Schichten.Add(new AbbildSchicht
                {
                    BaustoffKennung = s.Kennung ?? "",
                    Kennung = s.Kennung,
                    Name = s.Name,
                    DickeM = s.DickeM,
                    LambdaWmK = s.LambdaWmK,
                    RhoKgM3 = s.RhoKgM3,
                    CpJkgK = s.CpJkgK,
                    RWertM2KW = s.DickeM > 0.0 && s.LambdaWmK > 0.0 ? s.DickeM / s.LambdaWmK : null,
                    AusProjektdatei = true,
                });
            a.Status = a.Schichten.All(s => s.Vollstaendig) ? Aufbaustatus.Vollstaendig
                     : a.Schichten.All(s => s.HatWiderstand) ? Aufbaustatus.Masselos : Aufbaustatus.Unvollstaendig;
            return a;
        }
    }
}
