using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Vom geladenen IFC-Modell zum normierten Abbild</b> (Umsetzungskonzept 3.4 und 3.5): Einheiten,
    /// Nordrichtung, Gebäude, Geschosse, Räume, Raumgrenzen und Bauteile — alles über
    /// <c>Xbim.Ifc4.Interfaces.IIfc*</c>, ein Weg für IFC2X3, IFC4 und IFC4X3. Verzweigt wird an
    /// den zwei benannten Stellen (3.5 Nr. 7): der Erkennung der Raumgrenzen 2. Ebene
    /// (<see cref="IstZweiteEbene"/>) und dem in IFC4X3 entfallenen <c>Pset_SpaceThermalRequirements</c>
    /// (<see cref="Sollwert"/>) — und an einer dritten, gemessenen: Die Stoffwerte der Baustoffe liest
    /// die Schnittstelle in IFC2X3 nicht verlässlich; dort werden sie benannt NICHT gelesen
    /// (<see cref="Stoffwerte"/>).
    ///
    /// <para><b>Die Übersetzung in die gemeinsame Zuordnung.</b> Ein IFC-Bauteil grenzt oft an mehrere
    /// Räume derselben Seite (eine Außenwand an Wohnen und Küche); <see cref="GebaeudeAggregation"/>
    /// erwartet dagegen je Bauteil höchstens zwei Nachbarn, einen je Seite. Der Leser bildet deshalb die
    /// Nachbarn so, dass die gemeinsamen Regeln greifen: Ein Außenbauteil bekommt den ersten beheizten
    /// Raum als einzigen Nachbarn (die Außenseite ist die Randbedingung); ein Innenbauteil den ersten
    /// beheizten und den ersten unbeheizten Raum, und bei einer Decke sagt die Geschosslage, wer oben
    /// liegt (<see cref="GebaeudeAggregation.SICHT_BODEN"/>/<see cref="GebaeudeAggregation.SICHT_DECKE"/>).
    /// Ein Außenbauteil ohne Raumgrenze zählt über <see cref="AbbildBauteil.HuelleOhneNachbar"/>.</para>
    ///
    /// <para><b>Linear in der Dateigröße:</b> Jede Rückbeziehung (<c>IsDefinedBy</c>, <c>IsTypedBy</c>,
    /// <c>HasAssociations</c>, <c>IsDecomposedBy</c>, <c>ContainsElements</c>, <c>HasOpenings</c>,
    /// <c>HasFillings</c>, <c>HasProperties</c>) kommt aus <see cref="IfcRueckbezuege"/>, das jede
    /// Beziehungsart einmal je Modell durchläuft — nie aus der Eigenschaft der Bibliothek, die je Frage
    /// das ganze Modell durchsucht (O(n²)); dort steht auch, warum nicht <c>BeginInverseCaching</c>.</para>
    ///
    /// <para><b>Keine Geometrieableitung</b> (3.1): Fehlen die Mengen, bleibt die Fläche leer
    /// (<c>IMP_IFC_PROT_KEINE_MENGEN</c>); die Ableitung aus Körpern ist Stufe G5. <c>IfcZone</c> wird
    /// nicht gelesen (3.5 Nr. 8), <c>Pset_SpaceThermalLoad.AirExchangeRate</c> nicht benutzt (3.4).</para>
    /// </summary>
    internal sealed class IfcAbbildBauer
    {
        private const string P = IfcImportProfil.MELDUNGSPRAEFIX;
        private const int BEISPIELE = 5;

        /// <summary>Der Eigenschaftssatz der Raumsollwerte — in IFC4X3 entfallen (3.5 Nr. 7).</summary>
        internal const string PSET_SOLLWERTE = "Pset_SpaceThermalRequirements";

        /// <summary>Größter Abstand zweier Wandachsen gleicher Richtung und gleicher Räume [m], den die Mehrschalenprobe noch als eine Wand liest.</summary>
        internal const double SCHALENABSTAND_MAX_M = 0.6;

        /// <summary>Kleinster Achsabstand [m], ab dem zwei solche Wände zwei Schalen sind und nicht zwei Abschnitte derselben Flucht.</summary>
        internal const double SCHALENABSTAND_MIN_M = 0.01;

        private readonly IModel _modell;
        private readonly IfcRueckbezuege _bezuege;
        private readonly IfcGebaeudeAbbild _abbild;
        private readonly IProgress<ImportFortschritt> _melder;
        private readonly CancellationToken _abbruch;
        private readonly double _anteilStart;

        private IfcEinheiten _einheiten = new IfcEinheiten();
        private double _drehung;
        private IfcRahmen _wurzel = IfcRahmen.Welt;

        private readonly Dictionary<int, AbbildRaum> _raum = new Dictionary<int, AbbildRaum>();
        private readonly Dictionary<int, int> _raumGebaeude = new Dictionary<int, int>();
        private readonly Dictionary<int, double?> _raumLage = new Dictionary<int, double?>();
        private readonly Dictionary<int, double[]> _raumPunkt = new Dictionary<int, double[]>();
        private readonly Dictionary<int, int> _elementGebaeude = new Dictionary<int, int>();
        private readonly Dictionary<int, double?> _elementLage = new Dictionary<int, double?>();
        private readonly Dictionary<int, string> _elementGeschoss = new Dictionary<int, string>();
        private readonly Dictionary<int, IfcRahmen> _raumRahmen = new Dictionary<int, IfcRahmen>();
        private readonly SortedDictionary<string, int> _flaecheUnbekannt = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<int, List<IIfcRelSpaceBoundary>> _grenzen = new Dictionary<int, List<IIfcRelSpaceBoundary>>();
        private readonly HashSet<int> _grenzenZweiteEbene = new HashSet<int>();
        private readonly HashSet<string> _gemeldeteArten = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> _gefuellt = new HashSet<int>();

        private readonly Dictionary<int, (double? Lambda, double? Rho, double? Cp)> _stoffe
            = new Dictionary<int, (double? Lambda, double? Rho, double? Cp)>();
        private readonly SortedSet<string> _stoffwertNull = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> _stoffwerteNichtGelesen = new SortedSet<string>(StringComparer.Ordinal);

        // Rückfall der Wärmekapazität (Mehrzonenkonzept 6.5): je Baustoff das Ergebnis, je Aufbau die Meldungswerte.
        private readonly Dictionary<int, (double CpJkgK, string Quelle)?> _cpRueckfall = new Dictionary<int, (double CpJkgK, string Quelle)?>();
        private readonly List<(string Aufbau, List<string> Schichten)> _cpRueckfallAufbauten = new List<(string Aufbau, List<string> Schichten)>();
        private Baustoffabgleich _abgleichSaat;
        private readonly HashSet<string> _cpRueckfallSignaturen = new HashSet<string>(StringComparer.Ordinal);

        private readonly List<string> _ohneMengen = new List<string>();
        private readonly List<string> _seiteUnbestimmt = new List<string>();
        private readonly List<string> _ohneGebaeude = new List<string>();
        private readonly SortedDictionary<string, int> _platzierungsart = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private double _winkel = 1.0;
        private readonly SortedDictionary<string, int> _koerperNichtLesbar = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<int> _gebaeudeMitDarstellung = new HashSet<int>();
        private readonly List<Schale> _schalen = new List<Schale>();

        private sealed class Schale
        {
            public int Gebaeude;
            public string Raeume;
            public double Azimut;
            public double AbstandM;
        }

        internal IfcAbbildBauer(IModel modell, IfcGebaeudeAbbild abbild, IProgress<ImportFortschritt> melder,
                                CancellationToken abbruch, double anteilStart)
        {
            _modell = modell;
            _bezuege = new IfcRueckbezuege(modell);
            _abbild = abbild;
            _melder = melder;
            _abbruch = abbruch;
            _anteilStart = anteilStart;
        }

        // ==================================================================
        //  Ablauf
        // ==================================================================

        internal void Bauen()
        {
            IIfcProject projekt = Sortiert<IIfcProject>().FirstOrDefault();
            _einheiten = IfcEinheiten.Lesen(projekt, _abbild.Meldungen);
            _abbild.LaengenFaktorNachMeter = _einheiten.Laenge;
            _abbild.FlaechenFaktorNachM2 = _einheiten.Flaeche;
            _abbild.VolumenFaktorNachM3 = _einheiten.Volumen;
            _winkel = IfcRaumkoerper.Winkelfaktor(projekt);
            Kontext(projekt);

            List<IIfcBuilding> gebaeude = Sortiert<IIfcBuilding>().ToList();
            if (gebaeude.Count == 0)
            {
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "KEIN_GEBAEUDE"));
                return;
            }
            for (int i = 0; i < gebaeude.Count; i++) Gebaeude(gebaeude[i], i);
            BeheizungsartMelden();
            Melden(0.1);

            Raumgrenzen();
            Untergeschosse();
            Zonen();
            for (int i = 0; i < gebaeude.Count; i++) Flaechenart(i);
            Melden(0.2);

            Raumbezuege();
            Bauteile();
            Koerpertrennflaechen();
            GrundrissTrenndecken();
            // Die Flächen der Körper nach Randbedingung (Konzept HottCAD-Verbund 4.2) — Anzeige und Gegenprobe.
            foreach (AbbildGebaeude g in _abbild.Gebaeude) Flaechenklassifikation.Klassifizieren(g, _drehung);
            ReferenzenMelden();
            Melden(0.9);

            Abschluss();
            Melden(1.0);
        }

        private void Melden(double anteilAbbild)
        {
            _abbruch.ThrowIfCancellationRequested();
            _melder?.Report(new ImportFortschritt(_anteilStart + (1.0 - _anteilStart) * anteilAbbild,
                P + "ABBILD"));
        }

        private IEnumerable<T> Sortiert<T>() where T : IPersistEntity
            => _modell.Instances.OfType<T>().OrderBy(e => e.EntityLabel);

        // ==================================================================
        //  Nordrichtung (3.4, 3.5 Nr. 6)
        // ==================================================================

        private void Kontext(IIfcProject projekt)
        {
            List<IIfcGeometricRepresentationContext> kontexte = projekt?.RepresentationContexts?
                .OfType<IIfcGeometricRepresentationContext>()
                .Where(k => !(k is IIfcGeometricRepresentationSubContext))
                .ToList() ?? new List<IIfcGeometricRepresentationContext>();
            IIfcGeometricRepresentationContext kontext =
                kontexte.FirstOrDefault(k => IfcEigenschaften.Gleich(IfcEigenschaften.Text(k.ContextType), "Model"))
                ?? kontexte.FirstOrDefault();

            if (kontext?.WorldCoordinateSystem != null)
                _wurzel = IfcPlatzierung.Lokal(kontext.WorldCoordinateSystem);

            double[] nord = IfcPlatzierung.Richtung(kontext?.TrueNorth);
            if (nord != null && Math.Abs(nord[0]) + Math.Abs(nord[1]) > IfcPlatzierung.WAAGERECHT_MIN)
                _abbild.TrueNorthGrad = IfcPlatzierung.DrehungAusTrueNorth(nord[0], nord[1]);

            IIfcMapConversion karte = null;
            // Einmal je Datei gefragt — hier genügt die Suche der Bibliothek (kein Index nötig).
            try { karte = kontext?.HasCoordinateOperation?.OfType<IIfcMapConversion>().FirstOrDefault(); }
            catch (Exception) { karte = null; }   // IFC2X3 kennt die Umrechnung nicht
            if (karte != null)
            {
                _abbild.MapConversionVorhanden = true;
                double abszisse = IfcEigenschaften.Wert(karte.XAxisAbscissa);
                double ordinate = IfcEigenschaften.Wert(karte.XAxisOrdinate);
                if (double.IsNaN(abszisse) && double.IsNaN(ordinate)) { abszisse = 1.0; ordinate = 0.0; }
                if (double.IsNaN(abszisse)) abszisse = 0.0;
                if (double.IsNaN(ordinate)) ordinate = 0.0;
                _abbild.MapConversionGrad = IfcPlatzierung.DrehungAusMapConversion(abszisse, ordinate);
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "MAPCONVERSION"));
            }

            _drehung = IfcPlatzierung.Drehung(_abbild.TrueNorthGrad, _abbild.MapConversionGrad);
            if (!_abbild.TrueNorthGrad.HasValue && !_abbild.MapConversionVorhanden)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "KEIN_NORDEN"));
            else
            {
                _abbild.NordwinkelGrad = _drehung;
                if (Math.Abs(_drehung) > 1e-9)
                    _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "NORDDREHUNG", Zahl(Math.Round(_drehung, 3))));
            }
        }

        // ==================================================================
        //  Gebäude, Geschosse, Räume
        // ==================================================================

        private void Gebaeude(IIfcBuilding b, int index)
        {
            var g = new AbbildGebaeude
            {
                Kennung = b.GlobalId.ToString(),
                Name = IfcEigenschaften.Text(b.Name) ?? IfcEigenschaften.Text(b.LongName),
                Art = IfcEigenschaften.Text(b.ObjectType),
                Quelltyp = b.ExpressType.ExpressName,
                Zonenvorschlag = IfcImportProfil.ZONENREGEL_Z5,
            };
            _abbild.Gebaeude.Add(g);

            Baujahr(b, g);

            var besucht = new HashSet<int>();
            _enthalteneGeschosse = _enthalteneRaeume = 0;
            Struktur(b, index, null, besucht);
            if (_enthalteneGeschosse + _enthalteneRaeume > 0)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "STRUKTUR_ENTHALTEN", g.Anzeigename,
                    Ganz(_enthalteneGeschosse), Ganz(_enthalteneRaeume)));

            List<AbbildRaum> raeume = g.Raeume;
            g.ZahlGeschosseMitRaeumen = raeume.Select(r => r.GeschossKennung).Where(s => s != null).Distinct(StringComparer.Ordinal).Count();
            if (g.ZahlGeschosseMitRaeumen > 1) g.Zonenvorschlag = IfcImportProfil.ZONENREGEL_Z4;
        }

        /// <summary>
        /// Die Namen des Baujahr-Rückfalls in ihrer Rangfolge (Mehrzonenkonzept 6.5): erst der Standardname
        /// in einem beliebigen Satz — auch mit angehängter Einheit, etwa <c>YearOfConstruction (Datum)</c> —,
        /// dann <c>Constructed</c> („Erstellungsjahr des Gebäudes" eines CAD-Exports ohne Standardsatz).
        /// </summary>
        internal static readonly IReadOnlyList<string> BAUJAHR_NAMEN = new[] { "YearOfConstruction", "Constructed" };

        /// <summary>
        /// <b>Das Baujahr des Gebäudes</b>: <c>Pset_BuildingCommon.YearOfConstruction</c> (Vorkommnis vor Typ).
        /// Fehlt es dort oder ist es leer, fällt der Leser auf <see cref="BAUJAHR_NAMEN"/> in einem beliebigen
        /// Satz zurück (Name ohne angehängte Einheit, Groß-/Kleinschreibung egal) und nimmt den ersten Wert, aus
        /// dem sich ein Jahr lesen lässt (<see cref="Baujahrregel.Jahr"/>); der Rückfall wird mit Satz, Name und
        /// Text benannt (<c>IMP_IFC_PROT_BAUJAHR_RUECKFALL</c>, I). Ein unlesbarer Standardwert bleibt, was er ist
        /// (<c>BAUJAHR_UNLESBAR</c>) — zurückgefallen wird nur, wenn der Standard fehlt.
        /// </summary>
        private void Baujahr(IIfcBuilding b, AbbildGebaeude g)
        {
            IfcFund f = IfcEigenschaften.Finden(_bezuege, b, "Pset_BuildingCommon", "YearOfConstruction");
            string text = f == null ? null : Textwert(f);
            if (string.IsNullOrWhiteSpace(text))
            {
                IfcFund r = BaujahrRueckfall(b, out string rtext);
                if (r == null) return;
                g.BaujahrText = rtext.Trim();
                g.Baujahr = Baujahrregel.Jahr(rtext);
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUJAHR_RUECKFALL", r.Satz, r.Eigenschaft.Name.ToString(),
                    g.BaujahrText, g.Baujahr.Value.ToString(CultureInfo.InvariantCulture)));
                return;
            }
            g.BaujahrText = text.Trim();
            g.Baujahr = Baujahrregel.Jahr(text);
            if (g.Baujahr.HasValue)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUJAHR_TEXT", g.BaujahrText,
                    g.Baujahr.Value.ToString(CultureInfo.InvariantCulture)));
            else
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUJAHR_UNLESBAR", g.BaujahrText));
        }

        /// <summary>Der erste Fund aus <see cref="BAUJAHR_NAMEN"/>, aus dem sich ein Jahr lesen lässt; <c>null</c> = keiner.</summary>
        private IfcFund BaujahrRueckfall(IIfcBuilding b, out string text)
        {
            foreach (string name in BAUJAHR_NAMEN)
                foreach (IfcFund f in IfcEigenschaften.AlleMitNamen(_bezuege, b, new[] { name }))
                {
                    string t = f.Eigenschaft is IIfcPropertySingleValue einzel ? IfcEigenschaften.Textwert(einzel.NominalValue) : null;
                    if (Baujahrregel.Jahr(t).HasValue) { text = t; return f; }
                }
            text = null;
            return null;
        }

        /// <summary>
        /// Läuft die räumliche Struktur eines Gebäudes ab: Zerlegung (<c>IsDecomposedBy</c>) und Enthaltensein
        /// (<c>ContainsElements</c>). Ein eingeschachteltes <c>IfcBuilding</c> ist ein eigenes Gebäude und wird
        /// hier nicht betreten; Zonen hängen nicht an der Zerlegung und bleiben ohnehin draußen.
        ///
        /// <para><b>Räumliche Elemente über das Enthaltensein</b> (Mehrzonenkonzept 6.5): Manche CAD-Exporte
        /// hängen Geschosse und Räume nicht über <c>IfcRelAggregates</c>, sondern über
        /// <c>IfcRelContainedInSpatialStructure</c> an — das Schema lässt jedes <c>IfcProduct</c> zu. Solche
        /// Kinder werden wie zerlegte betreten, aber erst NACH der Zerlegung: Was beide Wege erreichen, nimmt
        /// den Weg der Zerlegung (Geschoss), und jedes räumliche Element zählt einmal
        /// (<paramref name="besucht"/>). Gezählt wird für <c>IMP_IFC_PROT_STRUKTUR_ENTHALTEN</c>.</para>
        /// </summary>
        private void Struktur(IIfcObjectDefinition knoten, int gi, IIfcBuildingStorey geschoss, HashSet<int> besucht)
        {
            if (!besucht.Add(knoten.EntityLabel)) return;
            if (knoten is IIfcBuildingStorey s)
            {
                geschoss = s;
                GeschossAnlegen(s, gi);
            }
            if (knoten is IIfcSpace raum) RaumAnlegen(raum, gi, geschoss);

            List<IIfcSpatialElement> enthalten = null;
            if (knoten is IIfcSpatialElement raeumlich)
                foreach (IIfcRelContainedInSpatialStructure rel in _bezuege.Enthaelt(raeumlich))
                    foreach (IIfcProduct p in rel.RelatedElements.OrderBy(x => x.EntityLabel))
                    {
                        if (p is IIfcElement e) ElementZuordnen(e, gi, geschoss, besucht);
                        else if (p is IIfcSpatialElement kindRaum && !(p is IIfcBuilding))
                            (enthalten ??= new List<IIfcSpatialElement>()).Add(kindRaum);
                    }

            foreach (IIfcRelAggregates rel in _bezuege.ZerlegtDurch(knoten))
                foreach (IIfcObjectDefinition kind in rel.RelatedObjects.OrderBy(x => x.EntityLabel))
                {
                    if (kind is IIfcBuilding) continue;
                    if (kind is IIfcSpatialElement) Struktur(kind, gi, geschoss, besucht);
                    else if (kind is IIfcElement e) ElementZuordnen(e, gi, geschoss, besucht);
                }

            if (enthalten == null) return;
            foreach (IIfcSpatialElement kind in enthalten)
            {
                if (besucht.Contains(kind.EntityLabel)) continue;
                if (kind is IIfcBuildingStorey) _enthalteneGeschosse++;
                else if (kind is IIfcSpace) _enthalteneRaeume++;
                Struktur(kind, gi, geschoss, besucht);
            }
        }

        /// <summary>Geschosse und Räume des laufenden Gebäudes, die allein über das Enthaltensein hängen.</summary>
        private int _enthalteneGeschosse, _enthalteneRaeume;

        private void ElementZuordnen(IIfcElement e, int gi, IIfcBuildingStorey geschoss, HashSet<int> besucht)
        {
            if (!_elementGebaeude.ContainsKey(e.EntityLabel))
            {
                _elementGebaeude[e.EntityLabel] = gi;
                _elementLage[e.EntityLabel] = Lage(geschoss);
                _elementGeschoss[e.EntityLabel] = geschoss?.GlobalId.ToString();
            }
            if (!besucht.Add(-e.EntityLabel)) return;
            foreach (IIfcRelAggregates rel in _bezuege.ZerlegtDurch(e))
                foreach (IIfcElement teil in rel.RelatedObjects.OfType<IIfcElement>())
                    ElementZuordnen(teil, gi, geschoss, besucht);
        }

        /// <summary>
        /// Die Höhenlage eines Geschosses — nur für die Reihenfolge, nie als absoluter Wert (3.5 Nr. 2):
        /// <c>Elevation</c>, sonst die z-Koordinate der Platzierung.
        /// </summary>
        private double? Lage(IIfcBuildingStorey geschoss)
        {
            if (geschoss == null) return null;
            double h = IfcEigenschaften.Wert(geschoss.Elevation);
            if (!double.IsNaN(h)) return h * _einheiten.Laenge;
            IfcRahmen? r = IfcPlatzierung.Weltrahmen(geschoss.ObjectPlacement, _wurzel, out _);
            return r.HasValue ? r.Value.Ursprung[2] * _einheiten.Laenge : (double?)null;
        }

        /// <summary>
        /// Ein Geschoss des Gebäudes — Lage für die Reihenfolge und die Bruttogrundfläche
        /// (<c>Qto_BuildingStoreyBaseQuantities.GrossFloorArea</c>) für den Rückfall der Dachfläche (3.4).
        /// </summary>
        private void GeschossAnlegen(IIfcBuildingStorey s, int gi)
        {
            _abbild.Gebaeude[gi].Geschosse.Add(new AbbildGeschoss
            {
                Kennung = s.GlobalId.ToString(),
                Name = IfcEigenschaften.Text(s.Name) ?? IfcEigenschaften.Text(s.LongName),
                LageM = Lage(s),
                GrundflaecheM2 = Positiv(IfcEigenschaften.Menge(_bezuege, s, "BuildingStorey", "GrossFloorArea", _einheiten)),
            });
        }

        private readonly Dictionary<int, double?[]> _raumflaechen = new Dictionary<int, double?[]>();

        // ------------------------------------------------------------------
        //  Mengenrückfall der Räume (Mehrzonenkonzept 6.5)
        // ------------------------------------------------------------------

        /// <summary>Rückfall der Nettofläche, wenn weder <c>NetFloorArea</c> noch <c>GrossFloorArea</c> im Qto steht.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_NETTOFLAECHE = new[] { "NetFloorArea", "Area", "NetArea" };

        /// <summary>Rückfall der Bruttofläche — nur gelesen, wenn auch die Nettofläche fehlt.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_BRUTTOFLAECHE = new[] { "GrossFloorArea", "GrossArea" };

        /// <summary>Rückfall des Volumens, wenn weder <c>NetVolume</c> noch <c>GrossVolume</c> im Qto steht.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_VOLUMEN = new[] { "NetVolume", "GrossVolume", "Volume" };

        /// <summary>Rückfall der Raumhöhe, wenn <c>Height</c> nicht im Qto steht.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_HOEHE = new[] { "Height", "FinishCeilingHeight" };

        /// <summary>Raum → Herkunft der zurückgefallenen Fläche je Platz (0 netto, 1 brutto).</summary>
        private readonly Dictionary<int, (string Satz, string Name)?[]> _flaechenRueckfall = new Dictionary<int, (string Satz, string Name)?[]>();

        /// <summary>Gebäude → genutzte Rückfälle (Zielmenge, Satz, Menge) in Lesereihenfolge.</summary>
        private readonly Dictionary<int, List<(string Ziel, string Satz, string Name)>> _rueckfaelle
            = new Dictionary<int, List<(string Ziel, string Satz, string Name)>>();

        private double? Rueckfall(IIfcSpace s, IReadOnlyList<string> namen, out (string Satz, string Name)? herkunft)
        {
            double? w = IfcEigenschaften.MengeRueckfall(_bezuege, s, namen, _einheiten, out string satz, out string name);
            herkunft = w.HasValue ? (satz, name) : ((string, string)?)null;
            return w;
        }

        private void RueckfallMerken(int gi, string ziel, (string Satz, string Name) herkunft)
        {
            if (!_rueckfaelle.TryGetValue(gi, out List<(string Ziel, string Satz, string Name)> liste))
                _rueckfaelle[gi] = liste = new List<(string Ziel, string Satz, string Name)>();
            liste.Add((ziel, herkunft.Satz, herkunft.Name));
        }

        /// <summary>
        /// Benennt die genutzten Rückfälle eines Gebäudes — eine Warnung je Zielmenge, Satz und Menge
        /// (<c>IMP_IFC_PROT_MENGE_RUECKFALL</c>): Der Anwender sieht, welcher Mengenname galt.
        /// </summary>
        private void RueckfaelleMelden(int gi)
        {
            if (!_rueckfaelle.TryGetValue(gi, out List<(string Ziel, string Satz, string Name)> liste)) return;
            AbbildGebaeude g = _abbild.Gebaeude[gi];
            string[] reihenfolge = { "NetFloorArea", "GrossFloorArea", "NetVolume", "Height" };
            foreach (var gruppe in liste.GroupBy(x => x)
                                        .OrderBy(x => Array.IndexOf(reihenfolge, x.Key.Ziel))
                                        .ThenBy(x => x.Key.Satz, StringComparer.Ordinal)
                                        .ThenBy(x => x.Key.Name, StringComparer.Ordinal))
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "MENGE_RUECKFALL", g.Anzeigename,
                    Ganz(gruppe.Count()), gruppe.Key.Ziel, gruppe.Key.Satz, gruppe.Key.Name));
        }

        private void RaumAnlegen(IIfcSpace s, int gi, IIfcBuildingStorey geschoss)
        {
            if (_raum.ContainsKey(s.EntityLabel)) return;
            AbbildGebaeude g = _abbild.Gebaeude[gi];
            string langname = IfcEigenschaften.Text(s.LongName);
            string name = IfcEigenschaften.Text(s.Name);
            var r = new AbbildRaum
            {
                Kennung = s.GlobalId.ToString(),
                Quelltyp = s.ExpressType.ExpressName,
                Name = langname ?? name,
                GeschossKennung = geschoss?.GlobalId.ToString(),
            };
            double?[] flaechen =
            {
                IfcEigenschaften.Menge(_bezuege, s, "Space", "NetFloorArea", _einheiten),
                IfcEigenschaften.Menge(_bezuege, s, "Space", "GrossFloorArea", _einheiten),
            };
            if (!(flaechen[0] > 0.0) && !(flaechen[1] > 0.0))
            {
                var herkunft = new (string Satz, string Name)?[2];
                flaechen[0] = Rueckfall(s, RUECKFALL_NETTOFLAECHE, out herkunft[0]);
                flaechen[1] = Rueckfall(s, RUECKFALL_BRUTTOFLAECHE, out herkunft[1]);
                if (herkunft[0].HasValue || herkunft[1].HasValue) _flaechenRueckfall[s.EntityLabel] = herkunft;
            }
            _raumflaechen[s.EntityLabel] = flaechen;
            r.HoeheM = Positiv(IfcEigenschaften.Menge(_bezuege, s, "Space", "Height", _einheiten));
            if (!r.HoeheM.HasValue)
            {
                r.HoeheM = Rueckfall(s, RUECKFALL_HOEHE, out (string Satz, string Name)? h);
                if (h.HasValue) RueckfallMerken(gi, "Height", h.Value);
            }
            r.VolumenM3 = Positiv(IfcEigenschaften.Menge(_bezuege, s, "Space", "NetVolume", _einheiten)
                                  ?? IfcEigenschaften.Menge(_bezuege, s, "Space", "GrossVolume", _einheiten));
            if (!r.VolumenM3.HasValue)
            {
                r.VolumenM3 = Rueckfall(s, RUECKFALL_VOLUMEN, out (string Satz, string Name)? v);
                if (v.HasValue) RueckfallMerken(gi, "NetVolume", v.Value);
            }

            r.SollHeizenC = Sollwert(s);
            Beheizung(s, r, langname, name, g);
            r.Klassifikation = Klassifikation(s);
            RaumtypLesen(s, r);
            r.Konditionierung = KonditionierungLesen(s, r, g);

            g.Raeume.Add(r);
            _raum[s.EntityLabel] = r;
            _raumGebaeude[s.EntityLabel] = gi;
            _raumLage[s.EntityLabel] = Lage(geschoss);
            IfcRahmen? rahmen = IfcPlatzierung.Weltrahmen(s.ObjectPlacement, _wurzel, out _);
            if (rahmen.HasValue)
            {
                _raumPunkt[s.EntityLabel] = rahmen.Value.Ursprung;
                _raumRahmen[s.EntityLabel] = rahmen.Value;
                // Der Grundriss aus der Körperdarstellung — trägt ohne Raumgrenzen die Trenndecke (Mehrzonenkonzept 6.5).
                try { r.GrundrissM = IfcRaumgrundriss.Lesen(s, rahmen.Value, _einheiten.Laenge); }
                catch (Exception) { r.GrundrissM = null; }   // eine unlesbare Darstellung ist kein Grundriss
            }
            Raumkoerper(s, r, gi, rahmen);
        }

        /// <summary>
        /// <b>Der Körper des Raums aus der Datei</b> (Datenaustauschkonzept 15.2, Stufe G7f-1): nur Anzeige, nie
        /// Rechengrundlage. Ohne Weltrahmen (<c>IfcGridPlacement</c>, <c>IfcLinearPlacement</c>) kein Körper; nicht
        /// lesbare Arten werden je Art gezählt (<c>KOERPER_ART</c>).
        /// </summary>
        private void Raumkoerper(IIfcSpace s, AbbildRaum r, int gi, IfcRahmen? rahmen)
        {
            if (s.Representation == null) return;
            _gebaeudeMitDarstellung.Add(gi);
            if (!rahmen.HasValue) return;
            var nichtLesbar = new List<string>();
            r.Koerper = IfcRaumkoerper.Lesen(s, rahmen.Value, _einheiten.Laenge, _winkel, nichtLesbar);
            foreach (string art in nichtLesbar)
                _koerperNichtLesbar[art] = _koerperNichtLesbar.TryGetValue(art, out int z) ? z + 1 : 1;
        }

        /// <summary>
        /// Der Anteil der <see cref="Dateikoerper.DREIECKSGRENZE"/>, unter dem die Raumkörper eines Gebäudes bleiben müssen,
        /// damit auch die Körper der Hüllbauteile geladen werden (Konzept HottCAD-Verbund 3.2).
        /// </summary>
        internal const double BAUTEILKOERPER_RAUMANTEIL = 2.0 / 3.0;

        /// <summary>Je Gebäude: Dreiecke der Raumkörper, Bauteile mit Körper, deren Dreiecke, ausgelassen wegen der Grenze.</summary>
        private readonly Dictionary<int, (int Raum, int Bauteile, int Dreiecke, int Ausgelassen)> _bauteilkoerper
            = new Dictionary<int, (int, int, int, int)>();

        /// <summary>Die nicht lesbaren Träger der Bauteilkörper je Art (getrennt von denen der Räume).</summary>
        private readonly SortedDictionary<string, int> _bauteilkoerperNichtLesbar = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// <b>Der Körper eines Hüllbauteils aus der Datei</b> (Konzept HottCAD-Verbund 3.2): derselbe Leser wie für Räume
        /// (<see cref="IfcRaumkoerper"/>), nur Anzeige. Ohne Darstellung oder Weltrahmen kein Körper, kein Fehler. Die
        /// Dreiecksgrenze gilt für Räume und Bauteile zusammen: Bauteilkörper nur, wenn die Räume unter
        /// <see cref="BAUTEILKOERPER_RAUMANTEIL"/> der Grenze bleiben und die Summe die Grenze nicht überschreitet.
        /// </summary>
        private void Bauteilkoerper(IIfcElement e, AbbildBauteil b, int gi)
        {
            if (gi < 0 || !(e is IIfcWall || e is IIfcSlab || e is IIfcRoof || e is IIfcWindow || e is IIfcDoor)) return;
            if (e.Representation == null) return;
            if (!_bauteilkoerper.TryGetValue(gi, out (int Raum, int Bauteile, int Dreiecke, int Ausgelassen) stand))
                stand = (_abbild.Gebaeude[gi].Raeume.Sum(r => r.Koerper?.DreieckZahl ?? 0), 0, 0, 0);
            if (stand.Raum >= Dateikoerper.DREIECKSGRENZE * BAUTEILKOERPER_RAUMANTEIL)
            {
                _bauteilkoerper[gi] = (stand.Raum, stand.Bauteile, stand.Dreiecke, stand.Ausgelassen + 1);
                return;
            }
            IfcRahmen? rahmen = IfcPlatzierung.Weltrahmen(e.ObjectPlacement, _wurzel, out _);
            if (!rahmen.HasValue) { _bauteilkoerper[gi] = stand; return; }
            var nichtLesbar = new List<string>();
            Dateikoerper k = IfcRaumkoerper.Lesen(e, rahmen.Value, _einheiten.Laenge, _winkel, nichtLesbar);
            foreach (string art in nichtLesbar) Zaehlen(_bauteilkoerperNichtLesbar, art);
            if (k == null) { _bauteilkoerper[gi] = stand; return; }
            if (stand.Raum + stand.Dreiecke + k.DreieckZahl > Dateikoerper.DREIECKSGRENZE)
            {
                _bauteilkoerper[gi] = (stand.Raum, stand.Bauteile, stand.Dreiecke, stand.Ausgelassen + 1);
                return;
            }
            b.Koerper = k;
            _bauteilkoerper[gi] = (stand.Raum, stand.Bauteile + 1, stand.Dreiecke + k.DreieckZahl, stand.Ausgelassen);
        }

        /// <summary>Je Gebäude die Räume mit <c>PredefinedType = INTERNAL</c> und <c>IsExternal = TRUE</c> (Regel B2 ohne Wirkung).</summary>
        private readonly Dictionary<AbbildGebaeude, List<string>> _aussenWiderspruch = new Dictionary<AbbildGebaeude, List<string>>();

        private static void Zaehlen(Dictionary<AbbildGebaeude, List<string>> ziel, AbbildGebaeude g, string raum)
        {
            if (!ziel.TryGetValue(g, out List<string> liste)) ziel[g] = liste = new List<string>();
            liste.Add(raum);
        }

        /// <summary>Die Temperatur [°C], oberhalb derer ein Raum nach Regel B3 beheizt ist.</summary>
        internal const double B3_GRENZE_C = 12.0;

        /// <summary>
        /// <b>Beheizt oder unbeheizt — die Regeln B1 bis B4 und B6</b> (Mehrzonenkonzept 6.1; B5 folgt
        /// nach den Raumgrenzen, <see cref="Untergeschosse"/>): B1 <c>PredefinedType = EXTERNAL</c>, B2
        /// <c>Pset_SpaceCommon.IsExternal = TRUE</c> (nicht gegen ein ausdrückliches <c>PredefinedType = INTERNAL</c>: dann
        /// gilt das Attribut, benannt <c>IMP_IFC_PROT_AUSSEN_WIDERSPRUCH</c>), B3 der Heizsollwert aus
        /// <c>Pset_SpaceThermalRequirements</c> — nur mit auflösbarer Temperatureinheit — über 12 °C
        /// beheizt, sonst unbeheizt, ersatzweise die Beheizungsart eines CAD-Exports
        /// (<see cref="Beheizungsart"/>), B4 die Namensregel, B6 sonst beheizt. Die erste Regel, die trägt,
        /// entscheidet.
        /// </summary>
        private void Beheizung(IIfcSpace s, AbbildRaum r, string langname, string name, AbbildGebaeude g)
        {
            IfcSpaceTypeEnum? art = null;
            try { art = s.PredefinedType; } catch (Exception) { art = null; }   // IFC2X3 kennt die Art nicht
            if (art == IfcSpaceTypeEnum.EXTERNAL)
            {
                Setzen(r, false, BeheiztQuelle.Attribut, "B1", "PredefinedType=EXTERNAL");
                return;
            }
            bool? aussen = Wahrheit(IfcEigenschaften.Finden(_bezuege, s, "Pset_SpaceCommon", "IsExternal"));
            // B2 nur ohne Widerspruch: Erklärt das Attribut den Raum ausdrücklich als Innenraum (INTERNAL), geht es dem
            // Satz vor — IsExternal = TRUE bleibt dann ohne Wirkung, benannt je Gebäude (AUSSEN_WIDERSPRUCH).
            if (aussen == true && art == IfcSpaceTypeEnum.INTERNAL)
                Zaehlen(_aussenWiderspruch, g, r.Name ?? r.Kennung);
            else if (aussen == true)
            {
                Setzen(r, false, BeheiztQuelle.Attribut, "B2", "IsExternal");
                return;
            }
            if (r.SollHeizenC.HasValue && _einheiten.TemperaturInKelvin.HasValue)
            {
                bool warm = r.SollHeizenC.Value > B3_GRENZE_C;
                Setzen(r, warm, BeheiztQuelle.Attribut, "B3",
                       PSET_SOLLWERTE + " " + Zahl(Math.Round(r.SollHeizenC.Value, 2)) + " °C");
                return;
            }
            // B3, Rückfall eines CAD-Exports: die Beheizungsart des Raums aus einem beliebigen Satz.
            bool? erklaert = Beheizungsart(s, g, langname ?? name);
            if (erklaert.HasValue)
            {
                Setzen(r, erklaert.Value, BeheiztQuelle.Attribut, "B3", _beheizungsartBeleg);
                return;
            }
            string treffer = Raumnamenregel.Treffer(langname) ?? Raumnamenregel.Treffer(name);
            if (treffer != null)
            {
                Setzen(r, false, BeheiztQuelle.Name, "B4", null);
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UNBEHEIZT_NAME", r.Kennung, r.Name ?? "", treffer));
                return;
            }
            Setzen(r, true, BeheiztQuelle.Annahme, "B6", null);
        }

        /// <summary>Die Beheizungsart eines Raums, wenn der Standard fehlt (CAD-Export, <c>HSETU_RaumAllgemein</c>).</summary>
        internal const string BEHEIZUNGSART = "HeatingType";

        /// <summary>
        /// <b>Die Abbildung der Beheizungsart</b> (Mehrzonenkonzept 6.5): Wert ohne das Präfix <c>bht</c>,
        /// Groß-/Kleinschreibung egal → beheizt. Nur die eindeutigen Werte entscheiden; <c>SeparatelyHeated</c>
        /// (getrennt beheizt) entscheidet nach der Raumtemperatur der Datei (<see cref="GETRENNT_BEHEIZT"/>), jeder
        /// andere Wert — und <c>SeparatelyHeated</c> ohne Raumtemperatur — lässt die Entscheidung den Regeln B4 bis B6
        /// (benannt).
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, bool> BEHEIZUNGSART_ABBILDUNG
            = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["Heated"] = true,
                ["UnHeated"] = false,
            };

        /// <summary>Der Wert der Beheizungsart „getrennt beheizt" (ohne Präfix <c>bht</c>).</summary>
        internal const string GETRENNT_BEHEIZT = "SeparatelyHeated";

        /// <summary>Die Raumtemperatur eines CAD-Exports (<c>HSETU_RaumAllgemein</c>, Einheit im Namen: <c>InsideTemperature (°C)</c>).</summary>
        internal const string RAUMTEMPERATUR = "InsideTemperature";

        /// <summary>Das Band einer lesbaren Raumtemperatur [°C]; außerhalb gilt sie als nicht angegeben.</summary>
        internal const double RAUMTEMPERATUR_MIN_C = -50.0, RAUMTEMPERATUR_MAX_C = 60.0;

        /// <summary>Je Gebäude und Beleg „Satz.Name = Wert": die getrennt beheizten Räume mit Name, Temperatur und Einstufung.</summary>
        private readonly Dictionary<AbbildGebaeude, SortedDictionary<string, List<(string Raum, double TemperaturC, bool Beheizt)>>> _beheizungsartTemperatur
            = new Dictionary<AbbildGebaeude, SortedDictionary<string, List<(string Raum, double TemperaturC, bool Beheizt)>>>();

        /// <summary>Der Beleg der zuletzt gelesenen Beheizungsart („Satz.Name = Wert").</summary>
        private string _beheizungsartBeleg;

        /// <summary>Je Gebäude und Beleg „Satz.Name": Räume beheizt bzw. unbeheizt nach der Beheizungsart der Datei.</summary>
        private readonly Dictionary<AbbildGebaeude, SortedDictionary<string, int[]>> _beheizungsart
            = new Dictionary<AbbildGebaeude, SortedDictionary<string, int[]>>();

        /// <summary>Je Gebäude und „Satz.Name\u0001Wert": Räume, deren Beheizungsart nicht entscheidet.</summary>
        private readonly Dictionary<AbbildGebaeude, SortedDictionary<string, int>> _beheizungsartOffen
            = new Dictionary<AbbildGebaeude, SortedDictionary<string, int>>();

        /// <summary>
        /// <b>Die Beheizungsart eines CAD-Exports</b> (Rückfall zu B3, Mehrzonenkonzept 6.5): die Eigenschaft
        /// <see cref="BEHEIZUNGSART"/> aus einem beliebigen Satz des Raums (Aufzählung oder Text, Präfix
        /// <c>bht</c> ohne Belang), abgebildet nach <see cref="BEHEIZUNGSART_ABBILDUNG"/>; <c>null</c> = keine
        /// Angabe oder ein Wert, der nicht entscheidet (gezählt, wenn eine Angabe da ist).
        /// <para><b>„Getrennt beheizt"</b> (<see cref="GETRENNT_BEHEIZT"/>) entscheidet nach der Raumtemperatur der Datei
        /// (<see cref="Raumtemperatur"/>) wie Regel B3: über <see cref="B3_GRENZE_C"/> beheizt, sonst unbeheizt — benannt je
        /// Gebäude (<c>IMP_IFC_PROT_BEHEIZUNGSART_TEMPERATUR</c>). Die Temperatur stuft nur ein; als Sollwert wird sie
        /// nicht übernommen. Ohne Raumtemperatur bleibt der Raum offen.</para>
        /// </summary>
        private bool? Beheizungsart(IIfcSpace s, AbbildGebaeude g, string raumname)
        {
            IfcFund f = IfcEigenschaften.AlleMitNamen(_bezuege, s, new[] { BEHEIZUNGSART }).FirstOrDefault();
            if (f == null) return null;
            string wert = f.Eigenschaft is IIfcPropertyEnumeratedValue aufz
                ? aufz.EnumerationValues?.Select(IfcEigenschaften.Textwert).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                : f.Eigenschaft is IIfcPropertySingleValue einzel ? IfcEigenschaften.Textwert(einzel.NominalValue) : null;
            wert = (wert ?? "").Trim();
            string kern = wert.StartsWith("bht", StringComparison.OrdinalIgnoreCase) ? wert.Substring(3) : wert;
            string ort = f.Satz + "." + f.Eigenschaft.Name;
            if (BEHEIZUNGSART_ABBILDUNG.TryGetValue(kern, out bool warm))
            {
                if (!_beheizungsart.TryGetValue(g, out SortedDictionary<string, int[]> z))
                    _beheizungsart[g] = z = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
                if (!z.TryGetValue(ort, out int[] n)) z[ort] = n = new int[2];
                n[warm ? 0 : 1]++;
                _beheizungsartBeleg = ort + " = " + wert;
                return warm;
            }
            if (string.Equals(kern, GETRENNT_BEHEIZT, StringComparison.OrdinalIgnoreCase))
            {
                (double TemperaturC, string Ort)? t = Raumtemperatur(s);
                if (t.HasValue)
                {
                    bool beheizt = t.Value.TemperaturC > B3_GRENZE_C;
                    string beleg = ort + " = " + wert;
                    if (!_beheizungsartTemperatur.TryGetValue(g, out SortedDictionary<string, List<(string, double, bool)>> z))
                        _beheizungsartTemperatur[g] = z = new SortedDictionary<string, List<(string, double, bool)>>(StringComparer.Ordinal);
                    if (!z.TryGetValue(beleg, out List<(string, double, bool)> liste)) z[beleg] = liste = new List<(string, double, bool)>();
                    liste.Add((string.IsNullOrWhiteSpace(raumname) ? s.GlobalId.ToString() : raumname.Trim(), t.Value.TemperaturC, beheizt));
                    _beheizungsartBeleg = beleg + ", " + t.Value.Ort + " = " + Zahl(Math.Round(t.Value.TemperaturC, 2)) + " °C";
                    return beheizt;
                }
            }
            if (!_beheizungsartOffen.TryGetValue(g, out SortedDictionary<string, int> offen))
                _beheizungsartOffen[g] = offen = new SortedDictionary<string, int>(StringComparer.Ordinal);
            Zaehlen(offen, ort + "\u0001" + (wert.Length == 0 ? "—" : wert));
            return null;
        }

        /// <summary>Der Raumtyp eines CAD-Exports (<c>HSETU_RaumAllgemein</c>, Aufzählung mit Präfix <c>mrt</c>).</summary>
        internal const string RAUMTYP = "RoomType";

        /// <summary>
        /// <b>Raumtyp und Raumtemperatur</b> für die Zonenregel Z6 (Mehrzonenkonzept 6.1): der Raumtyp aus
        /// <see cref="RAUMTYP"/> eines beliebigen Satzes ohne Präfix <c>mrt</c> (<c>mrtOffice</c> → <c>Office</c>), sonst
        /// <c>Pset_SpaceCommon.Category</c>, sonst <c>ObjectType</c>, sonst <c>null</c> — sprachneutral, wie in der Datei;
        /// dazu die Raumtemperatur der Datei (<see cref="Raumtemperatur"/>), die nie als Sollwert gilt.
        /// </summary>
        private void RaumtypLesen(IIfcSpace s, AbbildRaum r)
        {
            r.RaumtemperaturC = Raumtemperatur(s)?.TemperaturC;
            string typ = null;
            IfcFund f = IfcEigenschaften.AlleMitNamen(_bezuege, s, new[] { RAUMTYP }).FirstOrDefault();
            if (f != null)
                typ = f.Eigenschaft is IIfcPropertyEnumeratedValue aufz
                    ? aufz.EnumerationValues?.Select(IfcEigenschaften.Textwert).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                    : f.Eigenschaft is IIfcPropertySingleValue einzel ? IfcEigenschaften.Textwert(einzel.NominalValue) : null;
            typ = typ?.Trim();
            if (typ != null && typ.Length > 3 && typ.StartsWith("mrt", StringComparison.Ordinal)) typ = typ.Substring(3);
            if (string.IsNullOrWhiteSpace(typ))
            {
                IfcFund kategorie = IfcEigenschaften.Finden(_bezuege, s, "Pset_SpaceCommon", "Category");
                typ = kategorie?.Eigenschaft is IIfcPropertySingleValue k ? IfcEigenschaften.Textwert(k.NominalValue) : null;
            }
            if (string.IsNullOrWhiteSpace(typ)) typ = IfcEigenschaften.Text(s.ObjectType);
            r.Raumtyp = string.IsNullOrWhiteSpace(typ) ? null : typ.Trim();
        }

        /// <summary>
        /// <b>Die Raumtemperatur eines CAD-Exports</b>: die Eigenschaft <see cref="RAUMTEMPERATUR"/> aus einem beliebigen
        /// Satz des Raums, nur mit der Einheit °C im Namen (<c>InsideTemperature (°C)</c>) und im Band
        /// [<see cref="RAUMTEMPERATUR_MIN_C"/>; <see cref="RAUMTEMPERATUR_MAX_C"/>]; sonst <c>null</c> — eine Zahl ohne
        /// Einheit könnte Kelvin sein.
        /// </summary>
        private (double TemperaturC, string Ort)? Raumtemperatur(IIfcSpace s)
        {
            foreach (IfcFund f in IfcEigenschaften.AlleMitNamen(_bezuege, s, new[] { RAUMTEMPERATUR }))
            {
                if (!(f.Eigenschaft is IIfcPropertySingleValue einzel)) continue;
                IfcEigenschaften.NameOhneEinheit(einzel.Name.ToString(), out string einheit);
                string e = (einheit ?? "").Replace(" ", "");
                if (!string.Equals(e, "°C", StringComparison.OrdinalIgnoreCase) && !string.Equals(e, "degC", StringComparison.OrdinalIgnoreCase))
                    continue;
                double? t = IfcEigenschaften.Zahl(einzel.NominalValue);
                if (t.HasValue && t.Value >= RAUMTEMPERATUR_MIN_C && t.Value <= RAUMTEMPERATUR_MAX_C)
                    return (t.Value, f.Satz + "." + einzel.Name);
            }
            return null;
        }

        /// <summary>
        /// Die Sammelmeldungen der Beheizungsart je Gebäude: Räume beheizt und unbeheizt nach der Datei (I),
        /// getrennt beheizte Räume nach ihrer Raumtemperatur (I, je Beleg, mit Raum und Temperatur) und Räume,
        /// deren Beheizungsart nicht entscheidet (I, je Wert).
        /// </summary>
        private void BeheizungsartMelden()
        {
            foreach (KeyValuePair<AbbildGebaeude, List<string>> g in _aussenWiderspruch)
                g.Key.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "AUSSEN_WIDERSPRUCH", Ganz(g.Value.Count), Beispiele(g.Value)));
            foreach (KeyValuePair<AbbildGebaeude, SortedDictionary<string, int[]>> g in _beheizungsart)
                foreach (KeyValuePair<string, int[]> m in g.Value)
                    g.Key.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BEHEIZUNGSART", Ganz(m.Value[0]), Ganz(m.Value[1]), m.Key));
            foreach (KeyValuePair<AbbildGebaeude, SortedDictionary<string, List<(string Raum, double TemperaturC, bool Beheizt)>>> g in _beheizungsartTemperatur)
                foreach (KeyValuePair<string, List<(string Raum, double TemperaturC, bool Beheizt)>> m in g.Value)
                    g.Key.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BEHEIZUNGSART_TEMPERATUR",
                        Ganz(m.Value.Count(x => x.Beheizt)), Ganz(m.Value.Count(x => !x.Beheizt)), m.Key,
                        string.Join(", ", m.Value.Select(x => x.Raum + " " + Zahl(Math.Round(x.TemperaturC, 2)) + " °C"))));
            foreach (KeyValuePair<AbbildGebaeude, SortedDictionary<string, int>> g in _beheizungsartOffen)
                foreach (KeyValuePair<string, int> m in g.Value)
                {
                    string[] t = m.Key.Split('\u0001');
                    g.Key.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BEHEIZUNGSART_OFFEN", Ganz(m.Value), t[0], t[1]));
                }
        }

        private static void Setzen(AbbildRaum r, bool beheizt, BeheiztQuelle quelle, string regel, string angabe)
        {
            r.Beheizt = beheizt;
            r.BeheiztQuelle = quelle;
            r.Beheizungsregel = regel;
            r.Zustandsangabe = angabe;
        }

        /// <summary>Die Klassifikation eines Raums (Regel Z2) als „Quelle|Kennung"; <c>null</c> = keine.</summary>
        private string Klassifikation(IIfcSpace s)
        {
            foreach (IIfcRelAssociatesClassification k in _bezuege.Zuordnungen(s).OfType<IIfcRelAssociatesClassification>())
            {
                if (!(k.RelatingClassification is IIfcClassificationReference bezug)) continue;
                string kennung = IfcEigenschaften.Text(bezug.Identification) ?? IfcEigenschaften.Text(bezug.Name);
                if (string.IsNullOrWhiteSpace(kennung)) continue;
                string quelle = bezug.ReferencedSource is IIfcClassification c ? c.Name.ToString()
                              : bezug.ReferencedSource is IIfcClassificationReference oben ? IfcEigenschaften.Text(oben.Identification) : null;
                return (quelle ?? "").Trim() + "|" + kennung.Trim();
            }
            return null;
        }

        /// <summary>
        /// <b>Regel B5</b> (Mehrzonenkonzept 6.1): Ein Raum im Untergeschoss ohne Grenze <c>EXTERNAL</c>
        /// ist unbeheizt, wenn keine der Regeln B1 bis B4 trägt. Untergeschoss ist jedes Geschoss unter
        /// dem niedrigsten, dessen Räume Grenzen mit <c>EXTERNAL</c> tragen, ersatzweise unter dem
        /// niedrigsten Geschoss mit einer Höhenlage ab −0,5 m — nie „Höhenlage unter null".
        /// </summary>
        private void Untergeschosse()
        {
            var aussen = new HashSet<int>();
            foreach (List<IIfcRelSpaceBoundary> liste in _grenzen.Values)
                foreach (IIfcRelSpaceBoundary g in liste)
                    if (g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL && g.RelatingSpace is IIfcSpace s)
                        aussen.Add(s.EntityLabel);

            for (int gi = 0; gi < _abbild.Gebaeude.Count; gi++)
            {
                AbbildGebaeude geb = _abbild.Gebaeude[gi];
                List<int> raeume = _raum.Keys.Where(l => _raumGebaeude[l] == gi).OrderBy(l => l).ToList();
                double? grenze = raeume.Where(l => aussen.Contains(l) && _raumLage[l].HasValue).Select(l => _raumLage[l]).Min();
                if (!grenze.HasValue)
                    grenze = raeume.Select(l => _raumLage[l]).Where(h => h.HasValue && h.Value >= UNTERGESCHOSS_ERSATZ_M).Min();
                if (!grenze.HasValue) continue;
                foreach (int l in raeume)
                {
                    AbbildRaum r = _raum[l];
                    if (r.BeheiztQuelle != BeheiztQuelle.Annahme || aussen.Contains(l)) continue;
                    if (!(_raumLage[l] < grenze.Value - 1e-9)) continue;
                    Setzen(r, false, BeheiztQuelle.Lage, "B5", null);
                    string geschoss = geb.Geschosse.FirstOrDefault(x => x.Kennung == r.GeschossKennung)?.Anzeigename ?? "";
                    geb.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UNBEHEIZT_LAGE", r.Kennung, r.Name ?? "", geschoss));
                }
            }
        }

        /// <summary>Die Höhenlage [m], ab der ein Geschoss ersatzweise als Erdgeschoss gilt (B5).</summary>
        internal const double UNTERGESCHOSS_ERSATZ_M = -0.5;

        /// <summary>
        /// <b>Die Zonen der Datei</b> (Regel Z1, Mehrzonenkonzept 6.1): <c>IfcSpatialZone</c> mit
        /// <c>THERMAL</c> (Räume über <c>IfcRelReferencedInSpatialStructure</c>), sonst <c>IfcZone</c>
        /// (Räume über <c>IfcRelAssignsToGroup</c>). Zonen werden entschachtelt — es zählen die obersten,
        /// die Räume über den transitiven Abschluss; ein Raum in mehreren obersten Zonen gehört in keine
        /// (<c>IMP_IFC_PROT_RAUM_MEHRFACH</c>).
        /// </summary>
        private void Zonen()
        {
            var zonen = new List<(string Kennung, string Name, HashSet<int> Raeume)>();
            List<IIfcSpatialZone> thermisch;
            try
            {
                thermisch = Sortiert<IIfcSpatialZone>().Where(z => z.PredefinedType == IfcSpatialZoneTypeEnum.THERMAL).ToList();
            }
            catch (Exception) { thermisch = new List<IIfcSpatialZone>(); }
            if (thermisch.Count > 0)
            {
                var bezug = new Dictionary<int, HashSet<int>>();
                foreach (IIfcRelReferencedInSpatialStructure rel in Sortiert<IIfcRelReferencedInSpatialStructure>())
                {
                    if (rel.RelatingStructure == null) continue;
                    if (!bezug.TryGetValue(rel.RelatingStructure.EntityLabel, out HashSet<int> m))
                        bezug[rel.RelatingStructure.EntityLabel] = m = new HashSet<int>();
                    foreach (IIfcSpace s in rel.RelatedElements.OfType<IIfcSpace>()) m.Add(s.EntityLabel);
                }
                foreach (IIfcSpatialZone z in thermisch)
                    zonen.Add((z.GlobalId.ToString(), IfcEigenschaften.Text(z.LongName) ?? IfcEigenschaften.Text(z.Name),
                               bezug.TryGetValue(z.EntityLabel, out HashSet<int> r) ? r : new HashSet<int>()));
            }
            else
            {
                var glieder = new Dictionary<int, List<IIfcObjectDefinition>>();
                foreach (IIfcRelAssignsToGroup rel in Sortiert<IIfcRelAssignsToGroup>())
                {
                    if (!(rel.RelatingGroup is IIfcZone z)) continue;
                    if (!glieder.TryGetValue(z.EntityLabel, out List<IIfcObjectDefinition> l)) glieder[z.EntityLabel] = l = new List<IIfcObjectDefinition>();
                    l.AddRange(rel.RelatedObjects);
                }
                var unter = new HashSet<int>(glieder.Values.SelectMany(l => l.OfType<IIfcZone>()).Select(z => z.EntityLabel));
                foreach (IIfcZone z in Sortiert<IIfcZone>().Where(z => !unter.Contains(z.EntityLabel)))
                {
                    var raeume = new HashSet<int>();
                    var besucht = new HashSet<int>();
                    var offen = new Stack<int>();
                    offen.Push(z.EntityLabel);
                    while (offen.Count > 0)
                    {
                        int k = offen.Pop();
                        if (!besucht.Add(k) || !glieder.TryGetValue(k, out List<IIfcObjectDefinition> l)) continue;
                        foreach (IIfcObjectDefinition o in l)
                        {
                            if (o is IIfcSpace s) raeume.Add(s.EntityLabel);
                            else if (o is IIfcZone u) offen.Push(u.EntityLabel);
                        }
                    }
                    zonen.Add((z.GlobalId.ToString(), IfcEigenschaften.Text(z.LongName) ?? IfcEigenschaften.Text(z.Name), raeume));
                }
            }
            if (zonen.Count == 0) return;

            var mehrfach = new List<string>();
            foreach (KeyValuePair<int, AbbildRaum> kv in _raum.OrderBy(x => x.Key))
            {
                var treffer = zonen.Where(z => z.Raeume.Contains(kv.Key)).ToList();
                if (treffer.Count == 1)
                {
                    kv.Value.ZonenKennung = treffer[0].Kennung;
                    kv.Value.ZonenName = treffer[0].Name;
                }
                else if (treffer.Count > 1)
                {
                    kv.Value.ZoneMehrfach = true;
                    mehrfach.Add(kv.Value.Kennung);
                }
            }
            foreach (AbbildGebaeude g in _abbild.Gebaeude)
                g.ZahlZonen = g.Raeume.Select(r => r.ZonenKennung).Where(k => k != null).Distinct(StringComparer.Ordinal).Count();
            if (mehrfach.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "RAUM_MEHRFACH", Ganz(mehrfach.Count), Beispiele(mehrfach)));
        }

        /// <summary>
        /// Der Heizsollwert eines Raums aus <c>Pset_SpaceThermalRequirements</c> — Verzweigung 2 (3.5 Nr. 7):
        /// In IFC4X3 ist der Satz entfallen und wird nicht gelesen. Gelesen wird die untere Wintergrenze,
        /// sonst die untere Grenze, sonst der Sollwert des Bands.
        /// </summary>
        /// <summary>
        /// <b>Die Konditionierung der Zone aus den eigenen Sätzen von EPOS-Plan</b> (Datenaustauschkonzept 6.3, 16.3):
        /// <c>EPOS_Zone</c> (Nutzung, Heiz- und Kühlsollwert, Luftwechsel der Nutzer) und je Größe
        /// <c>EPOS_Kalender_&lt;Größe&gt;</c> (<see cref="IfcKonditionierungssatz"/>). Eine Datei ohne diese Werte ergibt
        /// <c>null</c>; ein nicht lesbarer Satz oder Periodentext wird benannt übersprungen (<c>KOND_UEBERSPRUNGEN</c>).
        /// </summary>
        private AbbildKonditionierung KonditionierungLesen(IIfcSpace s, AbbildRaum r, AbbildGebaeude g)
        {
            Dictionary<string, List<IfcSatzwert>> saetze = null;
            // HasProperties ist hier das Vorwärtsattribut von IfcPropertySet (Wache: Bezeichner „ps“).
            foreach (IIfcPropertySet ps in IfcEigenschaften.Saetze(_bezuege, s).OfType<IIfcPropertySet>())
            {
                string name = IfcEigenschaften.Text(ps.Name);
                if (name == null || !(name == IfcSchreiber.EPOS_ZONE || name.StartsWith(IfcKonditionierungssatz.PRAEFIX_KALENDER, StringComparison.Ordinal)))
                    continue;
                saetze ??= new Dictionary<string, List<IfcSatzwert>>(StringComparer.Ordinal);
                if (!saetze.TryGetValue(name, out List<IfcSatzwert> werte)) saetze[name] = werte = new List<IfcSatzwert>();
                foreach (IIfcPropertySingleValue e in ps.HasProperties.OfType<IIfcPropertySingleValue>())
                    werte.Add(Satzwert(e));
            }
            if (saetze == null) return null;
            var k = new AbbildKonditionierung();
            if (saetze.TryGetValue(IfcSchreiber.EPOS_ZONE, out List<IfcSatzwert> zone))
            {
                IfcSatzwert W(string n) => zone.FirstOrDefault(w => w.Name == n);
                string nutzung = W(IfcKonditionierungssatz.NUTZUNG)?.Text?.Trim();
                k.Nutzung = Zonenplan.NUTZUNGEN.Contains(nutzung) ? nutzung : null;
                k.HeizsollTagC = W(IfcKonditionierungssatz.HEIZSOLL_TAG)?.Zahl;
                k.HeizsollNachtC = W(IfcKonditionierungssatz.HEIZSOLL_NACHT)?.Zahl;
                k.KuehlsollC = W(IfcKonditionierungssatz.KUEHLSOLL)?.Zahl;
                k.LuftwechselNutzerJeH = W(IfcKonditionierungssatz.LUFTWECHSEL_NUTZER)?.Zahl;
            }
            var uebersprungen = new List<string>();
            foreach (Konditionierungsgroesse groesse in Konditionierungsgroessen.Alle)
                if (saetze.TryGetValue(IfcKonditionierungssatz.Satzname(groesse), out List<IfcSatzwert> werte)
                    && IfcKonditionierungssatz.KalenderLesen(groesse, werte, uebersprungen) is AbbildKalender kalender)
                    k.Kalender.Add(kalender);
            if (uebersprungen.Count > 0)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "KOND_UEBERSPRUNGEN", r.Name ?? r.Kennung,
                    Ganz(uebersprungen.Count), string.Join(", ", uebersprungen)));
            return k.Traegt ? k : null;
        }

        /// <summary>Ein Wert eines eigenen Satzes: Temperaturen nach °C (Einheit der Eigenschaft vor der des Projekts), auf vier Stellen.</summary>
        private IfcSatzwert Satzwert(IIfcPropertySingleValue e)
        {
            string name = e.Name.ToString() ?? "";
            string beschreibung = IfcEigenschaften.Text(e.Description);
            IIfcValue v = e.NominalValue;
            // Der Typname gilt für IFC4 und IFC2X3 gleich (die Werttypen sind je Schema eigene Strukturen).
            switch (v?.GetType().Name)
            {
                case "IfcThermodynamicTemperatureMeasure":
                {
                    double? t = IfcEigenschaften.Zahl(v);
                    if (t.HasValue)
                        t = e.Unit is IIfcSIUnit si && si.Name == Xbim.Ifc4.Interfaces.IfcSIUnitName.KELVIN ? t.Value - 273.15
                            : e.Unit is IIfcSIUnit c && c.Name == Xbim.Ifc4.Interfaces.IfcSIUnitName.DEGREE_CELSIUS ? t.Value
                            : _einheiten.NachCelsius(t.Value);
                    return new IfcSatzwert(name, IfcSatzwertart.Temperatur, t.HasValue ? Math.Round(t.Value, Kalenderwoche.NACHKOMMASTELLEN) : null);
                }
                case "IfcBoolean":
                case "IfcLogical":
                    return new IfcSatzwert(name, IfcSatzwertart.Wahrheit, Wahr: IfcEigenschaften.Wahrheit(v), BeschreibungWoertlich: beschreibung);
                case "IfcText":
                case "IfcLabel":
                case "IfcIdentifier":
                    return new IfcSatzwert(name, IfcSatzwertart.Text, Text: IfcEigenschaften.Textwert(v), BeschreibungWoertlich: beschreibung);
                default:
                    double? z = IfcEigenschaften.Zahl(v);
                    return new IfcSatzwert(name, IfcSatzwertart.Zahl, z.HasValue ? Math.Round(z.Value, Kalenderwoche.NACHKOMMASTELLEN) : null,
                                           BeschreibungWoertlich: beschreibung);
            }
        }

        private double? Sollwert(IIfcSpace s)
        {
            if (_abbild.SchemaStand == IfcSchemaStand.Ifc4x3) return null;
            bool vorhanden = IfcEigenschaften.HatSatz(_bezuege, s, PSET_SOLLWERTE);
            if (vorhanden) _abbild.ZahlSollwertsaetze++;
            foreach (string name in new[] { "SpaceTemperatureWinter", "SpaceTemperatureWinterMin", "SpaceTemperatureMin", "SpaceTemperature" })
            {
                IfcFund f = IfcEigenschaften.Finden(_bezuege, s, PSET_SOLLWERTE, name);
                double? w = f == null ? null : Zahl(f, untereGrenzeZuerst: true);
                if (w.HasValue) return _einheiten.NachCelsius(w.Value);
            }
            return null;
        }

        /// <summary>
        /// Nutzfläche je Gebäude (3.4): <c>NetFloorArea</c> für alle beheizten Räume oder
        /// <c>GrossFloorArea</c> für alle — eine gemischte Summe wird nicht gebildet, sondern gemeldet
        /// (<c>IMP_IFC_PROT_FLAECHENART_GEMISCHT</c>); dann bleiben die Räume ohne Nettofläche leer.
        /// </summary>
        private void Flaechenart(int gi)
        {
            AbbildGebaeude g = _abbild.Gebaeude[gi];
            List<KeyValuePair<int, AbbildRaum>> raeume = _raum.Where(kv => _raumGebaeude[kv.Key] == gi).OrderBy(kv => kv.Key).ToList();
            List<KeyValuePair<int, AbbildRaum>> beheizt = raeume.Where(kv => kv.Value.Beheizt).ToList();
            int netto = beheizt.Count(kv => _raumflaechen[kv.Key][0] > 0.0);
            int brutto = beheizt.Count(kv => _raumflaechen[kv.Key][1] > 0.0);
            int nurBrutto = beheizt.Count(kv => !(_raumflaechen[kv.Key][0] > 0.0) && _raumflaechen[kv.Key][1] > 0.0);

            int quelle;   // 0 = netto, 1 = brutto
            if (beheizt.Count > 0 && netto == beheizt.Count) quelle = 0;
            else if (beheizt.Count > 0 && brutto == beheizt.Count) quelle = 1;
            else
            {
                quelle = netto > 0 ? 0 : 1;
                if (netto > 0 && nurBrutto > 0)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "FLAECHENART_GEMISCHT",
                        Ganz(netto), Ganz(nurBrutto), Ganz(beheizt.Count - netto - nurBrutto)));
            }
            foreach (KeyValuePair<int, AbbildRaum> kv in raeume)
            {
                double?[] f = _raumflaechen[kv.Key];
                int platz = kv.Value.Beheizt ? quelle : f[0].HasValue ? 0 : 1;
                kv.Value.FlaecheM2 = Positiv(f[platz]);
                if (kv.Value.FlaecheM2.HasValue && _flaechenRueckfall.TryGetValue(kv.Key, out (string Satz, string Name)?[] herkunft)
                    && herkunft[platz].HasValue)
                    RueckfallMerken(gi, platz == 0 ? "NetFloorArea" : "GrossFloorArea", herkunft[platz].Value);
            }
            RueckfaelleMelden(gi);
            FlaecheAusGrundriss(g, raeume);

            if (raeume.Count == 0 || raeume.All(kv => !(_raumflaechen[kv.Key][0] > 0.0) && !(_raumflaechen[kv.Key][1] > 0.0)
                                                      && !kv.Value.FlaecheAusGrundriss))
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "KEINE_RAEUME", g.Anzeigename, Ganz(raeume.Count)));
        }

        /// <summary>
        /// <b>Die Raumfläche aus dem Grundriss</b> (Mehrzonenkonzept 6.5): Führt die Datei für einen Raum keine Flächenmenge —
        /// weder Netto noch Brutto, auch nicht über die Rückfallnamen —, gilt die Fläche seines Grundrisses
        /// (<see cref="AbbildRaum.GrundrissM"/>, Gaußsche Trapezformel) als Raumfläche, gekennzeichnet über
        /// <see cref="AbbildRaum.FlaecheAusGrundriss"/> und benannt je Gebäude (<c>IMP_IFC_PROT_FLAECHE_GRUNDRISS</c>, I).
        /// Ein Raum mit einer Flächenmenge bleibt unberührt.
        /// </summary>
        private void FlaecheAusGrundriss(AbbildGebaeude g, List<KeyValuePair<int, AbbildRaum>> raeume)
        {
            var namen = new List<string>();
            double summe = 0.0;
            foreach (KeyValuePair<int, AbbildRaum> kv in raeume)
            {
                AbbildRaum r = kv.Value;
                if (r.FlaecheM2.HasValue || _raumflaechen[kv.Key][0] > 0.0 || _raumflaechen[kv.Key][1] > 0.0) continue;
                if (r.GrundrissM == null || r.GrundrissM.Count < 3) continue;
                double f = Math.Abs(Grundrissueberlappung.Flaeche(r.GrundrissM));
                if (!(f > 0.0)) continue;
                r.FlaecheM2 = f;
                r.FlaecheAusGrundriss = true;
                summe += f;
                namen.Add(r.Name ?? r.Kennung);
            }
            if (namen.Count > 0)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "FLAECHE_GRUNDRISS", Ganz(namen.Count),
                    Zahl(Math.Round(summe, 1)), Beispiele(namen)));
        }

        // ==================================================================
        //  Raumgrenzen (3.5 Nr. 4)
        // ==================================================================

        private void Raumgrenzen()
        {
            foreach (IIfcRelSpaceBoundary rsb in Sortiert<IIfcRelSpaceBoundary>())
            {
                _abbild.ZahlRaumgrenzen++;
                bool zweite = IstZweiteEbene(rsb, _abbild.SchemaStand, out bool nurNachName);
                if (zweite)
                {
                    _abbild.ZahlRaumgrenzenZweiteEbene++;
                    if (nurNachName) _abbild.ZahlRaumgrenzenNachName++;
                }
                if (rsb.RelatingSpace is IIfcSpace raum && _raumGebaeude.TryGetValue(raum.EntityLabel, out int gi))
                {
                    _abbild.Gebaeude[gi].ZahlGrenzen++;
                    if (zweite) _abbild.Gebaeude[gi].ZahlGrenzenZweiteEbene++;
                }
                IIfcElement e = rsb.RelatedBuildingElement;
                if (e == null) continue;
                if (!_grenzen.TryGetValue(e.EntityLabel, out List<IIfcRelSpaceBoundary> liste))
                    _grenzen[e.EntityLabel] = liste = new List<IIfcRelSpaceBoundary>();
                liste.Add(rsb);
                if (zweite) _grenzenZweiteEbene.Add(rsb.EntityLabel);
            }
        }

        /// <summary>
        /// Ist eine Raumgrenze eine der 2. Ebene? — Verzweigung 1 (3.5 Nr. 7). Archicad schreibt trotz IFC4
        /// die BASISKLASSE und trägt das Merkmal nur in <c>Name='2ndLevel'</c> bzw. <c>Description='2a'</c>
        /// (oder <c>'2b'</c>); geprüft wird beides. Den Untertyp <c>IfcRelSpaceBoundary2ndLevel</c> gibt es
        /// erst ab IFC4 — in IFC2X3 zählen allein Name und Beschreibung.
        /// </summary>
        internal static bool IstZweiteEbene(IIfcRelSpaceBoundary rsb, IfcSchemaStand schema, out bool nurNachName)
        {
            string name = IfcEigenschaften.Text(rsb.Name);
            string beschreibung = IfcEigenschaften.Text(rsb.Description);
            bool nachName = IfcEigenschaften.Gleich(name, "2ndLevel")
                            || IfcEigenschaften.Gleich(beschreibung, "2a") || IfcEigenschaften.Gleich(beschreibung, "2b");
            bool nachTyp = schema != IfcSchemaStand.Ifc2x3 && rsb is IIfcRelSpaceBoundary2ndLevel;
            nurNachName = nachName && !nachTyp;
            return nachName || nachTyp;
        }

        /// <summary>Bauteile, deren äußere Raumgrenzen als Splitter nicht entschieden (<see cref="IstAussenSplitter"/>).</summary>
        private readonly List<string> _aussenSplitter = new List<string>();

        /// <summary>Je Platte eines zerlegten Dachs das Dach, dessen Raumgrenzen sie trägt (<see cref="Bauteile"/>).</summary>
        private readonly Dictionary<int, int> _grenzenUebertrag = new Dictionary<int, int>();

        /// <summary>Zerlegte Dächer, deren Raumgrenzen auf ihre Platte übergingen, und die Zahl dieser Grenzen.</summary>
        private int _daecherUebertragen, _dachgrenzenUebertragen;

        /// <summary>Die Raumgrenzen, die ein Bauteil oder eine Öffnung des Abbilds übernommen hat (Kennung der Grenze).</summary>
        private readonly HashSet<int> _grenzenUebernommen = new HashSet<int>();

        /// <summary>
        /// <b>Raumgrenzen ohne Bauteilfläche</b> (Mehrzonenkonzept 6.2): Jede Grenze der gewählten Ebene (die 2. Ebene, wenn die
        /// Datei welche führt), die kein Bauteil und keine Öffnung des Abbilds übernommen hat, wird je Art gezählt — die Klasse
        /// ihres Bauteils (Stütze, Träger, mehrteiliges Dach, virtuelles Element …), ohne Bauteil „—“ — und benannt
        /// (<c>IMP_IFC_PROT_GRENZEN_OHNE_BAUTEIL</c>, I, mit Anzahl und Fläche je Art). Ihre Geometrie liest der Leser; sie
        /// gehört nur zu keinem Bauteil der Hülle.
        /// </summary>
        private void GrenzenOhneBauteilMelden()
        {
            if (_aussenSplitter.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "AUSSEN_SPLITTER", Ganz(_aussenSplitter.Count), Beispiele(_aussenSplitter)));
            if (_daecherUebertragen > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "GRENZEN_DACHPLATTE", Ganz(_daecherUebertragen), Ganz(_dachgrenzenUebertragen)));
            bool nurZweite = _abbild.ZahlRaumgrenzenZweiteEbene > 0;
            var arten = new SortedDictionary<string, (int Zahl, double FlaecheM2)>(StringComparer.Ordinal);
            foreach (IIfcRelSpaceBoundary rsb in Sortiert<IIfcRelSpaceBoundary>())
            {
                if (_grenzenUebernommen.Contains(rsb.EntityLabel)) continue;
                if (nurZweite && !IstZweiteEbene(rsb, _abbild.SchemaStand, out _)) continue;
                IIfcElement e = rsb.RelatedBuildingElement;
                string art = (e == null ? "—" : e.ExpressType.ExpressName)
                             + (rsb.PhysicalOrVirtualBoundary == IfcPhysicalOrVirtualEnum.VIRTUAL ? " (VIRTUAL)" : "");
                IfcRahmen? rahmen = rsb.RelatingSpace is IIfcSpace raum && _raumRahmen.TryGetValue(raum.EntityLabel, out IfcRahmen r) ? r : (IfcRahmen?)null;
                IfcGrenzgeometrie.Flaeche f = IfcGrenzgeometrie.Lesen(rsb, rahmen, _einheiten.Laenge);
                arten.TryGetValue(art, out (int Zahl, double FlaecheM2) n);
                arten[art] = (n.Zahl + 1, n.FlaecheM2 + (f?.FlaecheM2 ?? 0.0));
            }
            foreach (KeyValuePair<string, (int Zahl, double FlaecheM2)> a in arten.OrderByDescending(x => x.Value.FlaecheM2).ThenBy(x => x.Key, StringComparer.Ordinal))
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "GRENZEN_OHNE_BAUTEIL", Ganz(a.Value.Zahl), a.Key,
                    Zahl(Math.Round(a.Value.FlaecheM2, 1))));
        }

        /// <summary>Die Raumgrenzen eines Bauteils — die der 2. Ebene, wenn es welche gibt, sonst alle.</summary>
        private List<IIfcRelSpaceBoundary> GrenzenVon(IIfcElement e)
        {
            if (!_grenzen.TryGetValue(e.EntityLabel, out List<IIfcRelSpaceBoundary> alle)
                && !(_grenzenUebertrag.TryGetValue(e.EntityLabel, out int dach) && _grenzen.TryGetValue(dach, out alle)))
                return new List<IIfcRelSpaceBoundary>();
            List<IIfcRelSpaceBoundary> zweite = alle.Where(g => _grenzenZweiteEbene.Contains(g.EntityLabel)).ToList();
            return zweite.Count > 0 ? zweite : alle;
        }

        // ==================================================================
        //  Bauteile
        // ==================================================================

        private void Bauteile()
        {
            var uebersprungen = new HashSet<int>();
            // Dach aus Platten (3.4, Zeile Dach): ENTWEDER das Dach mit eigener Fläche ODER seine Platten.
            // Fenster und Türen als Teile des Dachs (Dachfenster über IfcRelAggregates, Mehrzonenkonzept 6.5)
            // sind keine Platten: Sie werden Öffnungen des Dachs.
            foreach (IIfcRoof dach in Sortiert<IIfcRoof>())
            {
                List<IIfcElement> teile = Teile(dach).Where(t => !(t is IIfcWindow) && !(t is IIfcDoor)).ToList();
                if (teile.Count == 0) continue;
                if (IfcEigenschaften.Menge(_bezuege, dach, "Roof", "GrossArea", _einheiten).HasValue
                    || IfcEigenschaften.MengeRueckfall(_bezuege, dach, RUECKFALL_BAUTEIL_BRUTTO, _einheiten, out _, out _).HasValue)
                    foreach (IIfcElement t in teile) uebersprungen.Add(t.EntityLabel);
                else
                {
                    uebersprungen.Add(dach.EntityLabel);
                    // Die Raumgrenzen eines zerlegten Dachs: Hängen sie am Dach und trägt seine einzige Platte keine
                    // eigenen, gelten sie für die Platte (Mehrzonenkonzept 6.5) — sonst gingen sie verloren.
                    if (teile.Count == 1 && _grenzen.ContainsKey(dach.EntityLabel) && !_grenzen.ContainsKey(teile[0].EntityLabel))
                    {
                        _grenzenUebertrag[teile[0].EntityLabel] = dach.EntityLabel;
                        _dachgrenzenUebertragen += GrenzenVon(dach).Count;
                        _daecherUebertragen++;
                    }
                }
            }
            // Eine Vorhangfassade zählt als Ganzes; ihre Platten und Pfosten nicht noch einmal.
            foreach (IIfcCurtainWall fassade in Sortiert<IIfcCurtainWall>())
                foreach (IIfcElement t in Teile(fassade)) uebersprungen.Add(t.EntityLabel);

            var elemente = new List<(IIfcElement Element, string Klasse)>();
            elemente.AddRange(Sortiert<IIfcWall>().Select(e => ((IIfcElement)e, "Wall")));
            elemente.AddRange(Sortiert<IIfcSlab>().Select(e => ((IIfcElement)e, "Slab")));
            elemente.AddRange(Sortiert<IIfcRoof>().Select(e => ((IIfcElement)e, "Roof")));
            elemente.AddRange(Sortiert<IIfcCurtainWall>().Select(e => ((IIfcElement)e, "CurtainWall")));
            elemente.AddRange(Sortiert<IIfcPlate>().Select(e => ((IIfcElement)e, "Plate")));

            int n = 0;
            foreach ((IIfcElement e, string klasse) in elemente.OrderBy(x => x.Element.EntityLabel))
            {
                if (++n % 50 == 0) Melden(0.2 + 0.7 * n / elemente.Count);
                if (uebersprungen.Contains(e.EntityLabel)) continue;
                Bauteil(e, klasse);
            }

            // Fenster und Türen, die keine Öffnung füllen: kein Wirt, keine Himmelsrichtung, nicht gezählt.
            List<string> ohneWirt = Sortiert<IIfcWindow>().Cast<IIfcElement>().Concat(Sortiert<IIfcDoor>())
                .Where(e => !_gefuellt.Contains(e.EntityLabel)).Select(e => e.GlobalId.ToString()).ToList();
            if (ohneWirt.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "OHNE_WIRT", Ganz(ohneWirt.Count), Beispiele(ohneWirt)));
            if (_dachteilOeffnungen > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "OEFFNUNG_TEIL", Ganz(_dachteilOeffnungen)));
            BauteilRueckfaelleMelden();
            SchichtdickenMelden();
        }

        // ==================================================================
        //  Rückfälle der Bauteile ohne Standardsätze (Mehrzonenkonzept 6.5)
        // ==================================================================

        /// <summary>Bruttofläche ohne Standardmenge, in dieser Reihenfolge aus allen Mengensätzen.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_BAUTEIL_BRUTTO = new[] { "GrossSideArea", "GrossArea", "Area" };

        /// <summary>Nettofläche der Datei ohne Standardmenge, in dieser Reihenfolge aus allen Mengensätzen.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_BAUTEIL_NETTO = new[] { "NetSideArea", "NetArea" };

        /// <summary>Fläche einer Öffnung ohne <c>Area</c>, Breite × Höhe und <c>OverallWidth × OverallHeight</c>.</summary>
        internal static readonly IReadOnlyList<string> RUECKFALL_OEFFNUNG = new[] { "Area", "GrossArea" };

        /// <summary>Die Namen des U-Werts: der Standardname und das Synonym fremder Sätze.</summary>
        internal static readonly IReadOnlyList<string> UWERT_NAMEN = new[] { "ThermalTransmittance", "UValue" };

        /// <summary>Die Eigenschaft der Angrenzung, wenn <c>IsExternal</c> fehlt (CAD-Export, <c>HSETU_BauteilAllgemein</c>).</summary>
        internal const string ANGRENZUNG = "AdjacentType";

        /// <summary>Die Hüllkennung eines CAD-Exports (<c>HSETU_BauteilEnergetischeBewertung</c>): zählt das Bauteil zur Hüllfläche?</summary>
        internal static readonly IReadOnlyList<string> HUELLKENNUNG = new[] { "ElementEnergyConsultingProperties.CladdingSurface", "CladdingSurface" };

        /// <summary>
        /// <b>Die Abbildung der Angrenzung</b> (Mehrzonenkonzept 6.5): Wert ohne das Präfix <c>bta</c>,
        /// Groß-/Kleinschreibung egal → Randbedingung und, gegen unbeheizt, ob das Bauteil für die Zone
        /// Boden (<c>true</c>) oder Decke (<c>false</c>) ist. <c>None</c> und jeder andere Wert bleiben
        /// unbestimmt (die bisherige Vorgabe, benannt).
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, (Randbedingung Rand, bool? Zonenboden)> ANGRENZUNG_ABBILDUNG
            = new Dictionary<string, (Randbedingung, bool?)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Outside"] = (Randbedingung.Aussenluft, null),
                ["Ground"] = (Randbedingung.Erdreich, null),
                ["Heated"] = (Randbedingung.Innen, null),
                ["UnHeated"] = (Randbedingung.Unbeheizt, null),
                ["CellarCeiling"] = (Randbedingung.Unbeheizt, true),
                ["UppermostStorey"] = (Randbedingung.Unbeheizt, false),
            };

        private int _dachteilOeffnungen;
        private readonly SortedDictionary<string, int> _bauteilMengenRueckfall = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int> _uRueckfall = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int[]> _uEinheit = new SortedDictionary<string, int[]>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int> _uNichtPositiv = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int> _angrenzung = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int> _nichtHuelle = new SortedDictionary<string, int>(StringComparer.Ordinal);

        private static void Zaehlen(SortedDictionary<string, int> zaehler, string schluessel)
            => zaehler[schluessel] = zaehler.TryGetValue(schluessel, out int n) ? n + 1 : 1;

        /// <summary>
        /// Die Fläche aus allen Mengensätzen, wenn die Standardmenge fehlt — gezählt je Ziel, Satz und
        /// Menge für <c>IMP_IFC_PROT_BAUTEIL_MENGE_RUECKFALL</c>. Nur Flächennamen: Längen fremder Sätze
        /// tragen eine andere Bedeutung (im gemessenen CAD-Export ist <c>Width</c> die Wandlänge).
        /// </summary>
        private double? FlaecheRueckfall(IIfcElement e, IReadOnlyList<string> namen, string ziel)
        {
            double? w = IfcEigenschaften.MengeRueckfall(_bezuege, e, namen, _einheiten, out string satz, out string name);
            if (w.HasValue) Zaehlen(_bauteilMengenRueckfall, ziel + "\u0001" + satz + "\u0001" + name);
            return w;
        }

        /// <summary>
        /// <b>Die Angrenzung ohne <c>IsExternal</c></b>: die Eigenschaft <see cref="ANGRENZUNG"/> aus einem
        /// beliebigen Satz (Vorkommnis vor Typ), abgebildet nach <see cref="ANGRENZUNG_ABBILDUNG"/>;
        /// <c>null</c> = keine Angabe oder ein Wert ohne Abbildung (beides gezählt, wenn eine Angabe da ist).
        /// </summary>
        private (Randbedingung Rand, bool? Zonenboden)? Angrenzung(IIfcElement e)
        {
            IfcFund f = IfcEigenschaften.AlleMitNamen(_bezuege, e, new[] { ANGRENZUNG }).FirstOrDefault();
            if (f == null) return null;
            string wert = f.Eigenschaft is IIfcPropertyEnumeratedValue aufz
                ? aufz.EnumerationValues?.Select(IfcEigenschaften.Textwert).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                : f.Eigenschaft is IIfcPropertySingleValue einzel ? IfcEigenschaften.Textwert(einzel.NominalValue) : null;
            wert = (wert ?? "").Trim();
            string kern = wert.StartsWith("bta", StringComparison.OrdinalIgnoreCase) ? wert.Substring(3) : wert;
            string ort = f.Satz + "." + f.Eigenschaft.Name;
            if (ANGRENZUNG_ABBILDUNG.TryGetValue(kern, out (Randbedingung Rand, bool? Zonenboden) a))
            {
                Zaehlen(_angrenzung, Ziel(a.Rand) + "\u0001" + ort + "\u0001" + wert);
                return a;
            }
            Zaehlen(_angrenzung, P + "ANGRENZUNG_UNBESTIMMT\u0001" + ort + "\u0001" + (wert.Length == 0 ? "—" : wert));
            return null;
        }

        /// <summary>Der Rahmenanteil eines CAD-Exports (<c>HSETU_EcoCad.FractionOfFrame</c>, Konzept HottCAD-Verbund 3.3).</summary>
        internal const string RAHMENANTEIL = "FractionOfFrame";

        /// <summary>
        /// <b>Der Rahmenanteil [0–1] aus dem Wert der Datei</b>: Über 1 ist er in Prozent (der CAD-Export schreibt <c>30</c>
        /// für 30 %), bis 1 ein Anteil; Text mit Dezimalkomma wird gelesen. Negativ, über 100 % oder unlesbar → <c>null</c>.
        /// </summary>
        internal static double? RahmenanteilLesen(IIfcProperty p, out bool prozent)
        {
            prozent = false;
            if (!(p is IIfcPropertySingleValue einzel) || einzel.NominalValue == null) return null;
            double? w = IfcEigenschaften.Zahl(einzel.NominalValue);
            if (!w.HasValue && double.TryParse((IfcEigenschaften.Textwert(einzel.NominalValue) ?? "").Trim().Replace(',', '.'),
                                               NumberStyles.Float, CultureInfo.InvariantCulture, out double t)) w = t;
            if (!w.HasValue || double.IsNaN(w.Value) || w.Value < 0.0 || w.Value > 100.0) return null;
            prozent = w.Value > 1.0;
            return prozent ? w.Value / 100.0 : w.Value;
        }

        /// <summary>Der Rahmenanteil einer Öffnung mit Herkunft IFC und Beleg; ohne Angabe bleibt er leer (Vorgabe des Gebäudes).</summary>
        private void Rahmenanteil(IIfcElement o, AbbildBauteil b)
        {
            IfcFund f = IfcEigenschaften.AlleMitNamen(_bezuege, o, new[] { RAHMENANTEIL }).FirstOrDefault();
            if (f == null) return;
            double? anteil = RahmenanteilLesen(f.Eigenschaft, out bool prozent);
            if (!anteil.HasValue) return;
            b.Rahmenanteil = anteil;
            b.RahmenanteilHerkunft = Importherkunft.Ifc;
            b.RahmenanteilBeleg = f.Satz + "." + f.Eigenschaft.Name + (prozent ? " [%]" : " [–]");
        }

        /// <summary>Die Namen der Angrenzung je Seite (<c>HSETU_Bauteilreferenzen</c>, Konzept HottCAD-Verbund 3.1).</summary>
        internal static readonly IReadOnlyList<string> SEITE_ANGRENZUNG = new[] { "ElementReferences[0].AdjacentType", "ElementReferences[1].AdjacentType" };

        /// <summary>Die Namen der Orientierung je Seite; die Datei hängt die Einheit an (<c>Orientation (°)</c>).</summary>
        internal static readonly IReadOnlyList<string> SEITE_ORIENTIERUNG = new[] { "ElementReferences[0].Orientation", "ElementReferences[1].Orientation" };

        /// <summary>Der Platzhalter des CAD-Exports für „keine Orientierung“ (<c>botNone (-987654321,99)</c>): alles darunter ist keine.</summary>
        internal const double ORIENTIERUNG_PLATZHALTER = -1.0e6;

        /// <summary>Die beiden Seiten eines Bauteils, wie die Datei sie führt, und die wirksame Seite daraus.</summary>
        internal sealed class Seitenangabe
        {
            public (Randbedingung Rand, bool? Zonenboden, string Beleg)?[] Seite { get; } = new (Randbedingung, bool?, string)?[2];
            public string[] Code { get; } = new string[2];
            public string[] Ort { get; } = new string[2];
            public double?[] Orientierung { get; } = new double?[2];
            public bool Gefunden { get; set; }
            public int WirksamIndex { get; set; } = -1;
            public (Randbedingung Rand, bool? Zonenboden, string Beleg)? Wirksam { get; set; }

            public void Uebertragen(AbbildBauteil b)
            {
                if (!Gefunden) return;
                b.RandbedingungSeiteA = Seite[0]?.Rand;
                b.RandbedingungSeiteB = Seite[1]?.Rand;
                b.OrientierungSeiteA = Orientierung[0];
                b.OrientierungSeiteB = Orientierung[1];
                b.RandbedingungWirksam = Wirksam?.Rand;
                b.RandbedingungBeleg = Wirksam?.Beleg;
            }
        }

        /// <summary>
        /// <b>Ein Code der Seite</b> (<c>bta…</c>, Präfix und Schreibweise egal) → Randbedingung, Zonenboden und Beleg;
        /// <c>btaNone</c>, leer und jeder unbekannte Wert → <c>null</c> (keine Angabe).
        /// </summary>
        internal static (Randbedingung Rand, bool? Zonenboden, string Beleg)? SeiteAbbilden(string code)
        {
            string wert = (code ?? "").Trim();
            string kern = wert.StartsWith("bta", StringComparison.OrdinalIgnoreCase) ? wert.Substring(3) : wert;
            if (!ANGRENZUNG_ABBILDUNG.TryGetValue(kern, out (Randbedingung Rand, bool? Zonenboden) a)) return null;
            string beleg = string.Equals(kern, "CellarCeiling", StringComparison.OrdinalIgnoreCase) ? AbbildBauteil.BELEG_KELLERDECKE
                         : string.Equals(kern, "UppermostStorey", StringComparison.OrdinalIgnoreCase) ? AbbildBauteil.BELEG_OBERSTE_DECKE
                         : null;
            return (a.Rand, a.Zonenboden, beleg);
        }

        /// <summary>
        /// <b>Die wirksame Seite</b> (Konzept HottCAD-Verbund 3.1): die erste nicht beheizte Seite; tragen alle Seiten mit
        /// Angabe <c>btaHeated</c>, ist das Bauteil innen; ohne jede Angabe <c>-1</c>.
        /// </summary>
        internal static int WirksameSeite(IReadOnlyList<(Randbedingung Rand, bool? Zonenboden, string Beleg)?> seiten)
        {
            for (int i = 0; i < seiten.Count; i++)
                if (seiten[i].HasValue && seiten[i].Value.Rand != Randbedingung.Innen) return i;
            for (int i = 0; i < seiten.Count; i++)
                if (seiten[i].HasValue) return i;
            return -1;
        }

        /// <summary>
        /// <b>Die Orientierung einer Seite</b> [°]: eine Zahl, oder der Text des CAD-Exports <c>botS (180)</c> mit der Zahl in
        /// Klammern (Dezimalkomma); der Platzhalter <c>-987654321,99</c> und alles Unlesbare → <c>null</c>.
        /// </summary>
        internal static double? OrientierungLesen(IIfcProperty p)
        {
            if (!(p is IIfcPropertySingleValue einzel) || einzel.NominalValue == null) return null;
            double? zahl = IfcEigenschaften.Zahl(einzel.NominalValue);
            if (!zahl.HasValue)
            {
                string t = IfcEigenschaften.Textwert(einzel.NominalValue) ?? "";
                int auf = t.LastIndexOf('('), zu = t.LastIndexOf(')');
                string kern = auf >= 0 && zu > auf ? t.Substring(auf + 1, zu - auf - 1) : t;
                if (double.TryParse(kern.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double w)) zahl = w;
            }
            if (!zahl.HasValue || double.IsNaN(zahl.Value) || double.IsInfinity(zahl.Value) || zahl.Value < ORIENTIERUNG_PLATZHALTER) return null;
            double grad = zahl.Value % 360.0;
            return grad < 0.0 ? grad + 360.0 : grad;
        }

        /// <summary>Die beiden Seiten eines Bauteils aus beliebigen Sätzen (Vorkommnis vor Typ).</summary>
        private Seitenangabe Seiten(IIfcElement e)
        {
            var s = new Seitenangabe();
            for (int i = 0; i < 2; i++)
            {
                IfcFund f = IfcEigenschaften.AlleMitNamen(_bezuege, e, new[] { SEITE_ANGRENZUNG[i] }).FirstOrDefault();
                if (f != null)
                {
                    s.Gefunden = true;
                    string wert = f.Eigenschaft is IIfcPropertyEnumeratedValue aufz
                        ? aufz.EnumerationValues?.Select(IfcEigenschaften.Textwert).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
                        : f.Eigenschaft is IIfcPropertySingleValue einzel ? IfcEigenschaften.Textwert(einzel.NominalValue) : null;
                    s.Code[i] = (wert ?? "").Trim();
                    s.Ort[i] = f.Satz + "." + f.Eigenschaft.Name;
                    s.Seite[i] = SeiteAbbilden(s.Code[i]);
                }
                IfcFund o = IfcEigenschaften.AlleMitNamen(_bezuege, e, new[] { SEITE_ORIENTIERUNG[i] }).FirstOrDefault();
                if (o != null) s.Orientierung[i] = OrientierungLesen(o.Eigenschaft);
            }
            s.WirksamIndex = WirksameSeite(s.Seite);
            s.Wirksam = s.WirksamIndex < 0 ? null : s.Seite[s.WirksamIndex];
            return s;
        }

        /// <summary>Die wirksame Seite als Angrenzung, gezählt wie die des allgemeinen Satzes.</summary>
        private (Randbedingung Rand, bool? Zonenboden)? WirksamZaehlen(Seitenangabe s)
        {
            (Randbedingung Rand, bool? Zonenboden, string Beleg) w = s.Wirksam.Value;
            Zaehlen(_angrenzung, Ziel(w.Rand) + "\u0001" + s.Ort[s.WirksamIndex] + "\u0001" + s.Code[s.WirksamIndex]);
            return (w.Rand, w.Zonenboden);
        }

        private static string Ziel(Randbedingung r)
        {
            switch (r)
            {
                case Randbedingung.Aussenluft: return P + "ANGRENZUNG_AUSSEN";
                case Randbedingung.Erdreich: return P + "ANGRENZUNG_ERDREICH";
                case Randbedingung.Innen: return P + "ANGRENZUNG_INNEN";
                default: return P + "ANGRENZUNG_UNBEHEIZT";
            }
        }

        /// <summary>Die Hüllkennung des Bauteils (<see cref="HUELLKENNUNG"/>, Vorkommnis vor Typ); <c>null</c> = keine.</summary>
        private bool? Huellkennung(IIfcElement e, out string ort)
        {
            ort = null;
            foreach (IfcFund f in IfcEigenschaften.AlleMitNamen(_bezuege, e, HUELLKENNUNG))
            {
                if (!(f.Eigenschaft is IIfcPropertySingleValue einzel)) continue;
                bool? w = IfcEigenschaften.Wahrheit(einzel.NominalValue);
                if (!w.HasValue) continue;
                ort = f.Satz + "." + f.Eigenschaft.Name;
                return w;
            }
            return null;
        }

        /// <summary>
        /// Die Sammelmeldungen der Bauteil-Rückfälle: Mengen aus fremden Sätzen (W), U-Werte unter fremdem
        /// Namen (I), U-Werte mit abweichender Einheit (W, wenn ein Bauteil dadurch ohne U-Wert bleibt, sonst
        /// I), U-Werte null oder kleiner (I), die Angrenzung ohne <c>IsExternal</c> (I, unbestimmt W) und Bauteile, die nach der Hüllkennung
        /// nicht zur Hülle zählen (I).
        /// </summary>
        private void BauteilRueckfaelleMelden()
        {
            foreach (KeyValuePair<string, int> m in _bauteilMengenRueckfall)
            {
                string[] t = m.Key.Split('\u0001');
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "BAUTEIL_MENGE_RUECKFALL", Ganz(m.Value), t[0], t[1], t[2]));
            }
            foreach (KeyValuePair<string, int> m in _uRueckfall)
            {
                string[] t = m.Key.Split('\u0001');
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UWERT_RUECKFALL", Ganz(m.Value), t[0], t[1]));
            }
            foreach (KeyValuePair<string, int[]> m in _uEinheit)
            {
                string[] t = m.Key.Split('\u0001');
                _abbild.Meldungen.Add(new PruefMeldung(m.Value[1] > 0 ? PruefStufe.Warnung : PruefStufe.Info, P + "UWERT_EINHEIT",
                    Ganz(m.Value[0]), t[0], t[1], t[2], Ganz(m.Value[1])));
            }
            foreach (KeyValuePair<string, int> m in _uNichtPositiv)
            {
                string[] t = m.Key.Split('\u0001');
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UWERT_NICHT_POSITIV", Ganz(m.Value), t[0], t[1], t[2]));
            }
            foreach (KeyValuePair<string, int> m in _angrenzung)
            {
                string[] t = m.Key.Split('\u0001');
                _abbild.Meldungen.Add(new PruefMeldung(t[0] == P + "ANGRENZUNG_UNBESTIMMT" ? PruefStufe.Warnung : PruefStufe.Info,
                    t[0], Ganz(m.Value), t[1], t[2]));
            }
            foreach (KeyValuePair<string, int> m in _azimutRueckfall)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "AZIMUT_RUECKFALL", Ganz(m.Value), m.Key));
            foreach (KeyValuePair<string, int> m in _nichtHuelle)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "NICHT_HUELLE", Ganz(m.Value), m.Key));
        }

        private List<IIfcElement> Teile(IIfcElement e)
            => _bezuege.ZerlegtDurch(e).SelectMany(r => r.RelatedObjects.OfType<IIfcElement>()).ToList();

        private void Bauteil(IIfcElement e, string klasse)
        {
            string satz = "Pset_" + klasse + "Common";
            IIfcSlab platte = e as IIfcSlab;
            IfcSlabTypeEnum? plattenart = platte?.PredefinedType;
            bool dach = e is IIfcRoof || plattenart == IfcSlabTypeEnum.ROOF;
            bool bodenplatte = plattenart == IfcSlabTypeEnum.BASESLAB;
            bool senkrecht = e is IIfcWall || e is IIfcCurtainWall || e is IIfcPlate;

            // Plattenwerk außen (IfcPlate) nur, wenn es als außen erklärt ist.
            bool? istAussen = Wahrheit(IfcEigenschaften.Finden(_bezuege, e, satz, "IsExternal"));
            if (e is IIfcPlate && istAussen != true) return;

            List<IIfcRelSpaceBoundary> grenzen = GrenzenVon(e);
            // Die beiden Seiten eines CAD-Exports (HSETU_Bauteilreferenzen, Konzept HottCAD-Verbund 3.1).
            Seitenangabe seiten = Seiten(e);
            // Ohne IsExternal und ohne Raumgrenze: die Angrenzung eines CAD-Exports (Mehrzonenkonzept 6.5) — die wirksame
            // Seite vor der Angrenzung des allgemeinen Satzes, diese als Rückfall für Dateien ohne Seiten.
            (Randbedingung Rand, bool? Zonenboden)? angrenzung = !istAussen.HasValue && grenzen.Count == 0
                ? (seiten.Wirksam.HasValue ? WirksamZaehlen(seiten) : Angrenzung(e)) : null;
            Randbedingung rand = Rand(istAussen, grenzen, dach, bodenplatte, angrenzung?.Rand, out bool splitter);
            if (splitter) _aussenSplitter.Add(IfcEigenschaften.Text(e.Name) ?? e.GlobalId.ToString());

            int gi = GebaeudeVon(e, grenzen);
            var b = new AbbildBauteil
            {
                Kennung = e.GlobalId.ToString(),
                Quelltyp = e.ExpressType.ExpressName,
                Name = IfcEigenschaften.Text(e.Name),
                Quellart = plattenart?.ToString(),
                Randbedingung = rand,
            };
            seiten.Uebertragen(b);
            b.Art = Art(e, rand, dach, bodenplatte, plattenart, gi, grenzen);
            b.NeigungGrad = senkrecht ? 90.0 : dach ? 0.0 : b.Art == Bauteilart.Bodenplatte ? 180.0 : (double?)null;

            // Flächen: GrossSideArea bzw. GrossArea — nie NetSideArea als Bruttomaß (3.4).
            if (senkrecht && !(e is IIfcPlate))
            {
                b.BruttoflaecheM2 = Positiv(IfcEigenschaften.Menge(_bezuege, e, klasse, "GrossSideArea", _einheiten))
                                    ?? Produkt(IfcEigenschaften.Menge(_bezuege, e, klasse, "Length", _einheiten),
                                               IfcEigenschaften.Menge(_bezuege, e, klasse, "Height", _einheiten));
                b.NettoflaecheM2 = NichtNegativ(IfcEigenschaften.Menge(_bezuege, e, klasse, "NetSideArea", _einheiten));
            }
            else
            {
                b.BruttoflaecheM2 = Positiv(IfcEigenschaften.Menge(_bezuege, e, klasse, "GrossArea", _einheiten));
                b.NettoflaecheM2 = NichtNegativ(IfcEigenschaften.Menge(_bezuege, e, klasse, "NetArea", _einheiten));
            }
            // Ohne Standardmenge: die Flächennamen aus allen Mengensätzen (Mehrzonenkonzept 6.5).
            if (!b.BruttoflaecheM2.HasValue) b.BruttoflaecheM2 = FlaecheRueckfall(e, RUECKFALL_BAUTEIL_BRUTTO, "GrossArea");
            if (!b.NettoflaecheM2.HasValue && b.BruttoflaecheM2.HasValue && !IfcEigenschaften.HatMengensatz(_bezuege, e, klasse))
                b.NettoflaecheM2 = FlaecheRueckfall(e, RUECKFALL_BAUTEIL_NETTO, "NetArea");
            if (!b.BruttoflaecheM2.HasValue) _ohneMengen.Add(b.Kennung);

            bool uNichtPositiv = UWert(e, satz, b);
            b.Aufbau = Aufbau(e, out IIfcMaterialLayerSetUsage nutzung);

            // Ohne Raumgrenzen im ganzen Gebäude: die Raumbezüge (IfcRelReferencedInSpatialStructure, Mehrzonenkonzept 6.5).
            // Ein Bauteil mit U-Wert null oder kleiner und ohne Aufbau bewertet die Datei nicht (eine Bodenöffnung, ein
            // Hilfsbauteil): Es bekommt keine Nachbarn aus den Bezügen und trennt keine Geschosse.
            bool ohneGrenzen = grenzen.Count == 0 && gi >= 0 && _abbild.Gebaeude[gi].ZahlGrenzen == 0;
            bool unbewertet = uNichtPositiv && b.Aufbau == null;
            if (!ohneGrenzen || unbewertet || !ReferenzNachbarn(e, b, rand, gi, platte != null || e is IIfcRoof, angrenzung?.Zonenboden))
                Nachbarn(b, rand, grenzen, gi, platte != null || e is IIfcRoof);
            bool huelle = rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich
                          || (rand == Randbedingung.Unbeheizt && angrenzung.HasValue);
            if (b.Nachbarn.Count == 0 && huelle && Huellkennung(e, out string kennung) == false)
            {
                // Die Datei erklärt das Bauteil ausdrücklich als nicht zur Hüllfläche gehörig.
                Zaehlen(_nichtHuelle, kennung);
                huelle = false;
            }
            b.HuelleOhneNachbar = b.Nachbarn.Count == 0 && huelle;
            if (b.HuelleOhneNachbar && rand == Randbedingung.Unbeheizt) b.ZonenbodenOhneNachbar = angrenzung?.Zonenboden;
            // Eine Innenwand ohne Nachbarraum: einseitig innere Masse statt übergangen (Mehrzonenkonzept 6.5).
            b.InnenEinseitig = ohneGrenzen && e is IIfcWall && rand == Randbedingung.Innen && b.Nachbarn.Count == 0;

            if (senkrecht) Azimut(e, b, grenzen, gi);
            if (b.Aufbau != null) Schichtfolge(e, b, nutzung, grenzen, gi);

            // Stufe G6c: die Raumgrenzen je Seite, Dicke und Geschoss — der Eingang der Zonierung.
            GrenzenUebernehmen(b, grenzen);
            b.DickeM = Positiv(IfcEigenschaften.Menge(_bezuege, e, klasse, "Width", _einheiten))
                       ?? Positiv(IfcEigenschaften.Menge(_bezuege, e, klasse, "Depth", _einheiten))
                       ?? (b.Aufbau != null && b.Aufbau.Schichten.Count > 0 && b.Aufbau.Schichten.All(x => x.DickeM > 0.0)
                           ? b.Aufbau.Schichten.Sum(x => x.DickeM.Value) : (double?)null);
            b.GeschossKennung = _elementGeschoss.TryGetValue(e.EntityLabel, out string geschoss) ? geschoss : null;
            Bauteilkoerper(e, b, gi);

            foreach (IIfcRelVoidsElement rel in _bezuege.Oeffnungen(e))
            {
                if (!(rel.RelatedOpeningElement is IIfcOpeningElement oeffnung)) continue;
                foreach (IIfcRelFillsElement fuellung in _bezuege.Fuellungen(oeffnung))
                {
                    IIfcElement f = fuellung.RelatedBuildingElement;
                    if (f is IIfcWindow fenster) b.Oeffnungen.Add(Oeffnung(fenster, "Window", Bauteilart.Fenster, b, gi));
                    else if (f is IIfcDoor tuer) b.Oeffnungen.Add(Oeffnung(tuer, "Door", Bauteilart.Tuer, b, gi));
                    if (f != null) _gefuellt.Add(f.EntityLabel);
                }
            }
            // Fenster und Türen als Teile des Bauteils (IfcRelAggregates — Dachfenster eines CAD-Exports):
            // Öffnungen wie die einer Füllung, wenn keine Füllung sie schon trägt (Mehrzonenkonzept 6.5).
            foreach (IIfcElement t in Teile(e))
            {
                if (_gefuellt.Contains(t.EntityLabel)) continue;
                if (t is IIfcWindow fenster) b.Oeffnungen.Add(Oeffnung(fenster, "Window", Bauteilart.Fenster, b, gi));
                else if (t is IIfcDoor tuer) b.Oeffnungen.Add(Oeffnung(tuer, "Door", Bauteilart.Tuer, b, gi));
                else continue;
                _gefuellt.Add(t.EntityLabel);
                _dachteilOeffnungen++;
            }

            if (b.InnenEinseitig)
            {
                _innenEinseitig++;
                _innenEinseitigM2 += b.BruttoflaecheM2 ?? 0.0;
            }

            if (gi < 0)
            {
                _ohneGebaeude.Add(b.Kennung);
                _abbild.BauteileOhneGebaeude.Add(b);
            }
            else
                _abbild.Gebaeude[gi].Bauteile.Add(b);
        }

        /// <summary>
        /// Die Randbedingung (3.5 Nr. 4): <c>IsExternal</c> (Vorkommnis vor Typ) entscheidet; fehlt es,
        /// entscheidet <c>InternalOrExternalBoundary</c> der Raumgrenzen (<c>EXTERNAL_EARTH</c> = Erdreich,
        /// <c>EXTERNAL*</c> = außen, an einer Bodenplatte erdberührt wie unter <c>IsExternal</c>, <c>INTERNAL</c> = innen;
        /// äußere Grenzen, die nur ein Splitter sind, entscheiden nicht, <see cref="IstAussenSplitter"/>, gemeldet über
        /// <paramref name="splitter"/>), bei <c>NOTDEFINED</c> die Zählregel: außen, wenn
        /// genau eine physische Raumgrenze auf das Bauteil zeigt. Ein Dach ohne jede Angabe gilt als außen,
        /// eine Bodenplatte als erdberührt — beides ist ihre Definition. Ohne beides gilt die
        /// <paramref name="angrenzung"/> eines CAD-Exports (<see cref="ANGRENZUNG"/>, Mehrzonenkonzept 6.5)
        /// vor diesen Vorgaben.
        /// </summary>
        internal static Randbedingung Rand(bool? istAussen, IReadOnlyCollection<IIfcRelSpaceBoundary> grenzen, bool dach, bool bodenplatte,
                                           Randbedingung? angrenzung = null)
            => Rand(istAussen, grenzen, dach, bodenplatte, angrenzung, out _);

        /// <summary>Die Randbedingung wie oben; <paramref name="splitter"/> sagt, ob äußere Grenzen als Splitter nicht entschieden.</summary>
        internal static Randbedingung Rand(bool? istAussen, IReadOnlyCollection<IIfcRelSpaceBoundary> grenzen, bool dach, bool bodenplatte,
                                           Randbedingung? angrenzung, out bool splitter)
        {
            splitter = false;
            bool erde = grenzen.Any(g => g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL_EARTH);
            if (istAussen == true) return erde || bodenplatte ? Randbedingung.Erdreich : Randbedingung.Aussenluft;
            if (istAussen == false) return Randbedingung.Innen;
            if (grenzen.Count > 0)
            {
                if (erde) return Randbedingung.Erdreich;
                List<IIfcRelSpaceBoundary> aussen = grenzen.Where(g => g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL
                                                                       || g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL_WATER
                                                                       || g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL_FIRE).ToList();
                if (aussen.Count > 0 && IstAussenSplitter(aussen, grenzen))
                {
                    splitter = true;
                    grenzen = grenzen.Except(aussen).ToList();
                }
                else if (aussen.Count > 0)
                    return bodenplatte ? Randbedingung.Erdreich : Randbedingung.Aussenluft;
                if (grenzen.All(g => g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.INTERNAL))
                    return Randbedingung.Innen;
                if (grenzen.Count(g => g.PhysicalOrVirtualBoundary == IfcPhysicalOrVirtualEnum.PHYSICAL) == 1)
                    return Randbedingung.Aussenluft;
                return Randbedingung.Innen;
            }
            if (angrenzung.HasValue) return angrenzung.Value;
            if (dach) return Randbedingung.Aussenluft;
            if (bodenplatte) return Randbedingung.Erdreich;
            return Randbedingung.Unbekannt;
        }

        /// <summary>Der Flächenanteil, unter dem die äußeren Raumgrenzen eines Bauteils mit inneren Grenzen nicht entscheiden.</summary>
        internal const double AUSSEN_SPLITTER_ANTEIL = 0.05;

        /// <summary>
        /// <b>Ein äußerer Splitter</b> (Mehrzonenkonzept 6.2): Trägt ein Bauteil auch innere Raumgrenzen und haben seine äußeren
        /// Grenzen alle ein lesbares Polygon, zusammen aber weniger als <see cref="AUSSEN_SPLITTER_ANTEIL"/> der gelesenen
        /// Grenzfläche des Bauteils, entscheiden sie die Randbedingung nicht — ein Randstreifen (FZK-Haus: 0,82 m² an einer
        /// Geschossdecke von 170,6 m² Grenzfläche) macht keine Decke zum Dach.
        /// </summary>
        internal static bool IstAussenSplitter(IReadOnlyCollection<IIfcRelSpaceBoundary> aussen, IReadOnlyCollection<IIfcRelSpaceBoundary> alle)
        {
            if (aussen.Count == alle.Count) return false;
            double flaecheAussen = 0.0, flaecheAlle = 0.0;
            foreach (IIfcRelSpaceBoundary g in alle)
            {
                IfcGrenzgeometrie.Flaeche f = IfcGrenzgeometrie.Lesen(g, null, 1.0);
                bool gelesen = f != null && f.Fehler == null;
                if (aussen.Contains(g))
                {
                    if (!gelesen) return false;
                    flaecheAussen += f.FlaecheM2;
                }
                if (gelesen) flaecheAlle += f.FlaecheM2;
            }
            return flaecheAlle > 0.0 && flaecheAussen < AUSSEN_SPLITTER_ANTEIL * flaecheAlle;
        }

        /// <summary>
        /// Die Bauteilart aus Klasse, <c>PredefinedType</c> und Lage: Wand außen → Außenwand, innen →
        /// Innenwand; Platte <c>ROOF</c> und <c>IfcRoof</c> → Dach; <c>BASESLAB</c> → Bodenplatte; eine
        /// Außenplatte sonst nach der Lage des beheizten Raums (liegt er darüber, ist sie sein Boden);
        /// Vorhangfassade; Plattenwerk → Sonstiges.
        /// </summary>
        private Bauteilart Art(IIfcElement e, Randbedingung rand, bool dach, bool bodenplatte, IfcSlabTypeEnum? plattenart,
                               int gi, List<IIfcRelSpaceBoundary> grenzen)
        {
            bool aussen = rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich;
            if (e is IIfcWall) return aussen ? Bauteilart.Aussenwand : Bauteilart.Innenwand;
            if (e is IIfcCurtainWall) return Bauteilart.Vorhangfassade;
            if (e is IIfcPlate) return Bauteilart.Sonstiges;
            if (dach) return Bauteilart.Dach;
            if (bodenplatte) return Bauteilart.Bodenplatte;
            if (!aussen) return Bauteilart.Decke;
            if (rand == Randbedingung.Erdreich) return Bauteilart.Bodenplatte;
            // Außenplatte FLOOR o. Ä.: Liegt ein beheizter Nachbarraum im Geschoss der Platte oder darüber,
            // ist sie dessen Boden (über Außenluft), sonst seine Decke.
            double? lage = _elementLage.TryGetValue(e.EntityLabel, out double? l) ? l : null;
            foreach (int raum in Raeume(grenzen, gi).Where(r => _raum[r].Beheizt))
                if (lage.HasValue && _raumLage[raum].HasValue)
                    return _raumLage[raum].Value >= lage.Value ? Bauteilart.Bodenplatte : Bauteilart.Decke;
            return Bauteilart.Decke;
        }

        /// <summary>Das Gebäude eines Bauteils: über die räumliche Struktur, sonst über seine Räume; −1 = keines.</summary>
        private int GebaeudeVon(IIfcElement e, List<IIfcRelSpaceBoundary> grenzen)
        {
            if (_elementGebaeude.TryGetValue(e.EntityLabel, out int gi)) return gi;
            foreach (IIfcRelSpaceBoundary g in grenzen)
                if (g.RelatingSpace is IIfcSpace s && _raumGebaeude.TryGetValue(s.EntityLabel, out gi)) return gi;
            return -1;
        }

        /// <summary>Die bekannten Räume der Raumgrenzen (nur <c>IfcSpace</c>, kein Außenraum), nach Kennzahl geordnet, im Gebäude <paramref name="gi"/> zuerst.</summary>
        private List<int> Raeume(List<IIfcRelSpaceBoundary> grenzen, int gi)
            => grenzen.Select(g => g.RelatingSpace).OfType<IIfcSpace>().Select(s => s.EntityLabel)
                      .Where(l => _raum.ContainsKey(l)).Distinct()
                      .OrderBy(l => _raumGebaeude[l] == gi ? 0 : 1).ThenBy(l => l).ToList();

        /// <summary>Die Nachbarn in der Form der gemeinsamen Zuordnung (Klassenkopf).</summary>
        private void Nachbarn(AbbildBauteil b, Randbedingung rand, List<IIfcRelSpaceBoundary> grenzen, int gi, bool waagerecht)
        {
            List<int> raeume = Raeume(grenzen, gi);
            List<int> beheizt = raeume.Where(r => _raum[r].Beheizt).ToList();
            List<int> unbeheizt = raeume.Where(r => !_raum[r].Beheizt).ToList();

            if (rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich)
            {
                // Die Außenseite ist die Randbedingung; innen genügt ein Raum.
                int? innen = beheizt.Count > 0 ? beheizt[0] : unbeheizt.Count > 0 ? unbeheizt[0] : (int?)null;
                if (innen.HasValue) b.Nachbarn.Add(new AbbildNachbar(_raum[innen.Value].Kennung, null));
                return;
            }

            if (beheizt.Count > 0 && unbeheizt.Count > 0)
            {
                int h = beheizt[0], u = unbeheizt[0];
                string sichtH = null, sichtU = null;
                if (waagerecht && _raumLage[h].HasValue && _raumLage[u].HasValue && _raumLage[h].Value != _raumLage[u].Value)
                {
                    bool hOben = _raumLage[h].Value > _raumLage[u].Value;
                    sichtH = hOben ? GebaeudeAggregation.SICHT_BODEN : GebaeudeAggregation.SICHT_DECKE;
                    sichtU = hOben ? GebaeudeAggregation.SICHT_DECKE : GebaeudeAggregation.SICHT_BODEN;
                }
                b.Nachbarn.Add(new AbbildNachbar(_raum[h].Kennung, sichtH));
                b.Nachbarn.Add(new AbbildNachbar(_raum[u].Kennung, sichtU));
            }
            else if (beheizt.Count >= 2)
            {
                b.Nachbarn.Add(new AbbildNachbar(_raum[beheizt[0]].Kennung, null));
                b.Nachbarn.Add(new AbbildNachbar(_raum[beheizt[1]].Kennung, null));
            }
            else
                foreach (int r in beheizt.Concat(unbeheizt).Take(2))
                    b.Nachbarn.Add(new AbbildNachbar(_raum[r].Kennung, null));
        }

        // ==================================================================
        //  Raumbezüge ohne Raumgrenzen (Mehrzonenkonzept 6.5)
        // ==================================================================

        /// <summary>Kleinste und größte Zahl der Räume eines Geschosses, die eine Wand referenzieren müssen, damit sie innere Masse ist.</summary>
        internal const int WAND_BEZUG_MIN = 2, WAND_BEZUG_MAX = 4;

        /// <summary>Bauteil → die Räume (bekannt, nach Kennzahl), die es über <c>IfcRelReferencedInSpatialStructure</c> referenzieren.</summary>
        private readonly Dictionary<int, List<int>> _raumbezug = new Dictionary<int, List<int>>();

        /// <summary>
        /// Kleinster Anteil der Trenndeckenfläche eines Geschosspaars an der beheizten Grundfläche des kleineren der
        /// beiden Geschosse, ab dem das Paar als gekoppelt gilt (<see cref="AbbildGebaeude.GeschosseGekoppelt"/>); darunter
        /// nennen die Raumbezüge nicht alle Deckenteile, und das Paar ist benannt schwach gekoppelt.
        /// </summary>
        internal const double TRENNDECKE_ANTEIL_MIN = 0.5;

        /// <summary>Gebäude → Trenndecken je Geschosspaar (Geschosskennungen unten, oben): die referenzierten Deckenteile.</summary>
        private readonly Dictionary<int, SortedDictionary<(string Unten, string Oben), List<AbbildBauteil>>> _trenndecken
            = new Dictionary<int, SortedDictionary<(string Unten, string Oben), List<AbbildBauteil>>>();

        /// <summary>Gebäude → Platten, deren Erklärung gegen unbeheizt vor dem Raumbezug gilt, obwohl der Raum dieser Seite beheizt ist: Bauteil, Raum.</summary>
        private readonly Dictionary<int, List<(string Bauteil, string Raum)>> _erklaerungVorBezug = new Dictionary<int, List<(string, string)>>();

        /// <summary>Gebäude → Zahl der Innenwände, die über die Raumbezüge zwei Nachbarn bekommen.</summary>
        private readonly Dictionary<int, int> _bezugswaende = new Dictionary<int, int>();

        private int _innenEinseitig;
        private double _innenEinseitigM2;

        /// <summary>
        /// <b>Die Raumbezüge</b> eines CAD-Exports ohne Raumgrenzen (HottCAD-Muster): Je Raum nennt ein
        /// <c>IfcRelReferencedInSpatialStructure</c> die angrenzenden Bauteile. Gesammelt wird je Bauteil, welche
        /// bekannten Räume es referenzieren — einmal je Modell, linear.
        /// </summary>
        private void Raumbezuege()
        {
            foreach (IIfcRelReferencedInSpatialStructure rel in Sortiert<IIfcRelReferencedInSpatialStructure>())
            {
                if (!(rel.RelatingStructure is IIfcSpace raum) || !_raum.ContainsKey(raum.EntityLabel)) continue;
                foreach (IIfcProduct p in rel.RelatedElements)
                {
                    if (!(p is IIfcElement)) continue;
                    if (!_raumbezug.TryGetValue(p.EntityLabel, out List<int> liste)) _raumbezug[p.EntityLabel] = liste = new List<int>();
                    if (!liste.Contains(raum.EntityLabel)) liste.Add(raum.EntityLabel);
                    string kennung = p.GlobalId.ToString();
                    if (!_raum[raum.EntityLabel].Bezugsbauteile.Contains(kennung)) _raum[raum.EntityLabel].Bezugsbauteile.Add(kennung);
                }
            }
            foreach (List<int> liste in _raumbezug.Values) liste.Sort();
        }

        /// <summary>
        /// <b>Die Nachbarn aus den Raumbezügen</b> eines Bauteils ohne Raumgrenzen (Mehrzonenkonzept 6.5); <c>false</c> =
        /// die Regel greift nicht, es gilt der bisherige Weg. Flächen werden nicht je Raumpaar gerechnet (ADR-003) —
        /// das Bauteil trägt seine Menge einmal, zwischen genau zwei Räumen:
        /// <list type="bullet">
        /// <item><b>Trenndecke:</b> Eine Platte oder ein Dach, nicht außen, das Räume genau zweier Geschosse
        /// referenzieren, trennt diese Geschosse — Nachbarn sind je Geschoss der erste beheizte Raum (sonst der erste),
        /// mit der Sicht aus der Geschosslage (oben Boden, unten Decke). Erklärt die Datei die Platte gegen unbeheizt
        /// (<c>btaCellarCeiling</c>, <c>btaUppermostStorey</c>), liegen aber beiderseits beheizte Räume, gilt die
        /// Erklärung der Datei, nicht der Bezug.</item>
        /// <item><b>Innenwand:</b> Eine Wand innen oder ohne Angabe, die <see cref="WAND_BEZUG_MIN"/> bis
        /// <see cref="WAND_BEZUG_MAX"/> Räume desselben Geschosses referenzieren, liegt zwischen zwei von ihnen — zwei
        /// beheizten, wenn es sie gibt (innere Masse), sonst einem beheizten und einem unbeheizten.</item>
        /// </list>
        /// </summary>
        private bool ReferenzNachbarn(IIfcElement e, AbbildBauteil b, Randbedingung rand, int gi, bool waagerecht, bool? zonenboden)
        {
            if (rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich) return false;
            if (!_raumbezug.TryGetValue(e.EntityLabel, out List<int> alle)) return false;
            List<int> raeume = alle.Where(r => _raumGebaeude[r] == gi).ToList();
            if (raeume.Count < 2) return false;
            List<IGrouping<string, int>> geschosse = raeume.GroupBy(r => _raum[r].GeschossKennung ?? "", StringComparer.Ordinal).ToList();

            if (waagerecht)
            {
                if (geschosse.Count != 2 || geschosse.Any(x => x.Key.Length == 0)) return false;
                int a = Erster(geschosse[0]), c = Erster(geschosse[1]);
                if (!_raumLage[a].HasValue || !_raumLage[c].HasValue || _raumLage[a].Value == _raumLage[c].Value) return false;
                (int unten, int oben) = _raumLage[a].Value < _raumLage[c].Value ? (a, c) : (c, a);
                if (rand == Randbedingung.Unbeheizt && _raum[a].Beheizt && _raum[c].Beheizt)
                {
                    // Die Erklärung der Datei gilt vor dem Bezug; der Widerspruch zur Beheizung des Raums wird benannt —
                    // der Raum der Seite, die die Datei unbeheizt nennt (Kellerdecke: unten, oberste Decke: oben).
                    string raum = zonenboden == true ? Anzeige(unten) : zonenboden == false ? Anzeige(oben) : Anzeige(unten) + " / " + Anzeige(oben);
                    if (!_erklaerungVorBezug.TryGetValue(gi, out List<(string, string)> liste)) _erklaerungVorBezug[gi] = liste = new List<(string, string)>();
                    liste.Add((string.IsNullOrWhiteSpace(b.Name) ? b.Kennung : b.Name.Trim(), raum));
                    return false;
                }
                // Der beheizte Raum zuerst (die gemeinsame Zuordnung liest die Sicht des ersten), sonst der untere.
                bool obenZuerst = _raum[oben].Beheizt && !_raum[unten].Beheizt;
                AbbildNachbar nOben = new AbbildNachbar(_raum[oben].Kennung, GebaeudeAggregation.SICHT_BODEN);
                AbbildNachbar nUnten = new AbbildNachbar(_raum[unten].Kennung, GebaeudeAggregation.SICHT_DECKE);
                b.Nachbarn.Add(obenZuerst ? nOben : nUnten);
                b.Nachbarn.Add(obenZuerst ? nUnten : nOben);
                b.Trenndeckenherkunft = AbbildBauteil.TRENNDECKE_BEZUG;
                if (!_trenndecken.TryGetValue(gi, out SortedDictionary<(string, string), List<AbbildBauteil>> paare))
                    _trenndecken[gi] = paare = new SortedDictionary<(string, string), List<AbbildBauteil>>();
                (string, string) paar = (_raum[unten].GeschossKennung, _raum[oben].GeschossKennung);
                if (!paare.TryGetValue(paar, out List<AbbildBauteil> teile)) paare[paar] = teile = new List<AbbildBauteil>();
                teile.Add(b);
                return true;
            }

            if (!(e is IIfcWall) || rand == Randbedingung.Unbeheizt) return false;
            if (geschosse.Count != 1 || raeume.Count > WAND_BEZUG_MAX || raeume.Count < WAND_BEZUG_MIN) return false;
            List<int> beheizt = raeume.Where(r => _raum[r].Beheizt).ToList();
            List<int> unbeheizt = raeume.Where(r => !_raum[r].Beheizt).ToList();
            List<int> wahl = beheizt.Count >= 2 ? beheizt.Take(2).ToList()
                           : beheizt.Count == 1 ? new List<int> { beheizt[0], unbeheizt[0] }
                           : unbeheizt.Take(2).ToList();
            foreach (int r in wahl) b.Nachbarn.Add(new AbbildNachbar(_raum[r].Kennung, null));
            _bezugswaende[gi] = _bezugswaende.TryGetValue(gi, out int w) ? w + 1 : 1;
            return true;
        }

        /// <summary>Höchstzahl der Raumnamen in <c>IMP_IFC_PROT_KOERPER_OHNE_PAAR</c>.</summary>
        internal const int KOERPER_OHNE_PAAR_NAMEN = 5;

        /// <summary>
        /// <b>Die Trennflächen aus den Raumkörpern</b> (Mehrzonenkonzept 6.2, „Trennflächen aus Raumkörpern“): Je Gebäude
        /// die Flächenpaare der Körper (<see cref="Koerpernachbarschaft"/>). Rangfolge Raumgrenzen vor Körpern vor
        /// Raumbezügen:
        /// <list type="bullet">
        /// <item>Mit Raumgrenzen werden die Paare nur gezählt (<c>IMP_IFC_PROT_KOERPERPAARE_GEZAEHLT</c>, I).</item>
        /// <item>Ohne Raumgrenzen wird je Paar ein Trennbauteil mit zwei Grenzen der Herkunft <see cref="Grenzherkunft.Koerper"/>
        /// gebildet (Fläche = Schnittfläche, Gegenstücke wechselseitig). U-Wert, Aufbau und Dicke stammen vom Bauteil der
        /// gleichen Art, das beide Räume referenzieren (Richtung passend, Fläche am nächsten), sonst von einer Innenwand
        /// bzw. Decke, die einer der Räume referenziert, bei einer Decke sonst von der größten freien Decke des
        /// Geschosspaars (<see cref="FreieDecke"/>), sonst bleibt der U-Wert offen (Vorgabe). Ein so abgedecktes Bauteil
        /// geht in den Paaren auf, wenn jeder Raum, der es referenziert, an einem dieser Paare liegt (seine Öffnungen gehen
        /// an das größte Paar); sonst gibt es die Fläche seiner Paare ab und trägt nur den Rest weiter. Die Trenndecken aus den Raumbezügen eines Geschosspaars weichen den Körperdecken, wo ein
        /// Körperpaar das Geschosspaar verbindet; sonst bleiben sie als Rückfall. Außenflächen bleiben unberührt.</item>
        /// </list>
        /// Meldungen: <c>IMP_IFC_PROT_GRENZEN_AUS_KOERPER</c> (I), <c>IMP_IFC_PROT_KOERPER_OHNE_PAAR</c> (I); den
        /// Kopplungswächter der Körperdecken (<c>IMP_IFC_PROT_KOERPERPAAR_SCHWACH</c>, W) führt <see cref="ReferenzenMelden"/>.
        /// </summary>
        private void Koerpertrennflaechen()
        {
            for (int gi = 0; gi < _abbild.Gebaeude.Count; gi++)
            {
                AbbildGebaeude g = _abbild.Gebaeude[gi];
                if (g.Raeume.Count(r => r.Koerper != null) < 2) continue;
                List<Koerperpaar> paare = Koerpernachbarschaft.Paare(g.Raeume);
                g.ZahlKoerperpaare = paare.Count;
                g.KoerperTrennwandM2 = Math.Round(paare.Where(p => !p.Decke).Sum(p => p.FlaecheM2), 6);
                g.KoerperTrenndeckeM2 = Math.Round(paare.Where(p => p.Decke).Sum(p => p.FlaecheM2), 6);
                if (g.ZahlGrenzen > 0)
                {
                    if (paare.Count > 0)
                        g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPERPAARE_GEZAEHLT", g.Anzeigename, Ganz(paare.Count),
                            Zahl(Math.Round(g.KoerperTrennwandM2, 2)), Zahl(Math.Round(g.KoerperTrenndeckeM2, 2))));
                    continue;
                }
                var mitPaar = new HashSet<int>(paare.SelectMany(p => new[] { p.RaumA, p.RaumB }));
                List<string> ohne = Enumerable.Range(0, g.Raeume.Count).Where(i => g.Raeume[i].Koerper != null && !mitPaar.Contains(i))
                                              .Select(i => string.IsNullOrWhiteSpace(g.Raeume[i].Name) ? g.Raeume[i].Kennung : g.Raeume[i].Name.Trim()).ToList();
                if (ohne.Count > 0)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_OHNE_PAAR", Ganz(ohne.Count),
                        string.Join(", ", ohne.Take(KOERPER_OHNE_PAAR_NAMEN)) + (ohne.Count > KOERPER_OHNE_PAAR_NAMEN ? ", …" : "")));
                if (paare.Count == 0) continue;
                g.KoerperpaareGebildet = true;

                // Bauteil → die Räume dieses Gebäudes, die es über die Raumbezüge referenzieren (Raumkennungen).
                var bezug = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
                foreach (KeyValuePair<int, List<int>> e in _raumbezug.OrderBy(x => x.Key))
                {
                    if (!(_modell.Instances[e.Key] is IIfcRoot wurzel)) continue;
                    var raeume = new HashSet<string>(e.Value.Where(r => _raumGebaeude.TryGetValue(r, out int x) && x == gi).Select(r => _raum[r].Kennung),
                                                     StringComparer.Ordinal);
                    if (raeume.Count > 0) bezug[wurzel.GlobalId.ToString()] = raeume;
                }
                HashSet<string> Bezug(AbbildBauteil b) => bezug.TryGetValue(b.Kennung, out HashSet<string> r) ? r : null;
                bool Innen(AbbildBauteil b) => b.Randbedingung != Randbedingung.Aussenluft && b.Randbedingung != Randbedingung.Erdreich;
                bool IstWand(AbbildBauteil b) => b.Quelltyp != null && b.Quelltyp.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0
                                                 && !b.Quelltyp.StartsWith("IfcCurtainWall", StringComparison.OrdinalIgnoreCase);
                bool IstDecke(AbbildBauteil b) => string.Equals(b.Quelltyp, "IfcSlab", StringComparison.OrdinalIgnoreCase)
                                                  && b.Quellart != nameof(IfcSlabTypeEnum.ROOF) && b.Quellart != nameof(IfcSlabTypeEnum.BASESLAB);
                bool Richtung(AbbildBauteil b, Koerperpaar p)
                {
                    if (p.Decke || !b.AzimutGrad.HasValue) return true;
                    double az = Math.Atan2(p.NormaleA[0], p.NormaleA[1]) * 180.0 / Math.PI;
                    double d = Math.Abs(((b.AzimutGrad.Value - az) % 180.0 + 180.0) % 180.0);
                    return Math.Min(d, 180.0 - d) <= Koerpernachbarschaft.WAND_NEIGUNG_GRAD;
                }

                List<AbbildBauteil> bestand = g.Bauteile.ToList();
                var neu = new List<(AbbildBauteil Teil, Koerperpaar Paar, List<AbbildBauteil> Abgedeckt)>();
                var kennungen = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (Koerperpaar p in paare)
                {
                    AbbildRaum ra = g.Raeume[p.RaumA], rb = g.Raeume[p.RaumB];
                    Func<AbbildBauteil, bool> art = p.Decke ? (Func<AbbildBauteil, bool>)IstDecke : IstWand;
                    bool Beide(AbbildBauteil b) { HashSet<string> r = Bezug(b); return r != null && r.Contains(ra.Kennung) && r.Contains(rb.Kennung); }
                    bool Einer(AbbildBauteil b) { HashSet<string> r = Bezug(b); return r != null && (r.Contains(ra.Kennung) || r.Contains(rb.Kennung)); }
                    List<AbbildBauteil> beide = bestand.Where(b => Innen(b) && art(b) && Beide(b) && Richtung(b, p)).ToList();
                    AbbildBauteil vorlage = beide.OrderBy(b => Math.Abs((b.BruttoflaecheM2 ?? 0.0) - p.FlaecheM2)).FirstOrDefault()
                        ?? bestand.Where(b => Innen(b) && art(b) && Einer(b) && Richtung(b, p)
                                              && (p.Decke ? b.Art == Bauteilart.Decke : b.Art == Bauteilart.Innenwand))
                                  .OrderBy(b => Math.Abs((b.BruttoflaecheM2 ?? 0.0) - p.FlaecheM2)).FirstOrDefault();
                    // Eine Decke ohne Bezug: die größte freie Decke des Geschosspaars, im oberen Geschoss vor dem unteren
                    // (wie die Trenndecke aus dem Grundriss).
                    if (vorlage == null && p.Decke)
                    {
                        string o = (p.Oben == p.RaumA ? ra : rb).GeschossKennung, u = (p.Oben == p.RaumA ? rb : ra).GeschossKennung;
                        vorlage = bestand.Where(b => FreieDecke(b) && b.GeschossKennung != null && (b.GeschossKennung == o || b.GeschossKennung == u))
                                         .OrderByDescending(b => b.GeschossKennung == o).ThenByDescending(b => b.BruttoflaecheM2 ?? 0.0).FirstOrDefault();
                    }
                    var abgedeckt = new List<AbbildBauteil>(beide);
                    if (vorlage != null && !abgedeckt.Contains(vorlage)) abgedeckt.Add(vorlage);

                    string kennung = (vorlage?.Kennung ?? "KOERPER") + "|" + ra.Kennung + "|" + rb.Kennung;
                    int n = kennungen.TryGetValue(kennung, out int k) ? k + 1 : 1;
                    kennungen[kennung] = n;
                    if (n > 1) kennung += "|" + Ganz(n);
                    AbbildRaum oben = p.Decke ? (p.Oben == p.RaumA ? ra : rb) : null;
                    var t = new AbbildBauteil
                    {
                        Kennung = kennung, Quelltyp = vorlage?.Quelltyp ?? (p.Decke ? "IfcSlab" : "IfcWall"), Name = vorlage?.Name,
                        Quellart = vorlage?.Quellart, Art = p.Decke ? Bauteilart.Decke : Bauteilart.Innenwand,
                        Randbedingung = Randbedingung.Innen, BruttoflaecheM2 = p.FlaecheM2,
                        UWertWm2K = vorlage?.UWertWm2K, UWertQuelle = vorlage?.UWertQuelle, Aufbau = vorlage?.Aufbau,
                        DickeM = vorlage?.DickeM ?? (p.AbstandM > 0.0 ? p.AbstandM : (double?)null),
                        NeigungGrad = p.Decke ? (double?)null : 90.0,
                        GeschossKennung = oben?.GeschossKennung ?? ra.GeschossKennung,
                        Trenndeckenherkunft = p.Decke ? AbbildBauteil.TRENNDECKE_KOERPER : null,
                    };
                    if (p.Decke) Paar(t, oben == ra ? rb : ra, oben);
                    else { t.Nachbarn.Add(new AbbildNachbar(ra.Kennung, null)); t.Nachbarn.Add(new AbbildNachbar(rb.Kennung, null)); }
                    double[] normaleB = p.NormaleA.Select(x => -x).ToArray();
                    t.Grenzen.Add(new AbbildGrenze
                    {
                        Kennung = kennung + "|A", RaumKennung = ra.Kennung, Lage = Randbedingung.Innen, FlaecheM2 = p.FlaecheM2,
                        SchwerpunktM = p.SchwerpunktA, Normale = p.NormaleA, RandpunkteM = p.RandA, GegenstueckKennung = kennung + "|B",
                        Herkunft = Grenzherkunft.Koerper,
                    });
                    t.Grenzen.Add(new AbbildGrenze
                    {
                        Kennung = kennung + "|B", RaumKennung = rb.Kennung, Lage = Randbedingung.Innen, FlaecheM2 = p.FlaecheM2,
                        SchwerpunktM = p.SchwerpunktB, Normale = normaleB, RandpunkteM = p.RandB, GegenstueckKennung = kennung + "|A",
                        Herkunft = Grenzherkunft.Koerper,
                    });
                    neu.Add((t, p, abgedeckt));
                }

                // Abgedeckte Bauteile gehen auf, wenn jeder Raum, der sie referenziert, an einem ihrer Paare liegt.
                var verbraucht = new HashSet<AbbildBauteil>();
                foreach (AbbildBauteil b in neu.SelectMany(x => x.Abgedeckt).Distinct())
                {
                    var anPaaren = new HashSet<string>(neu.Where(x => x.Abgedeckt.Contains(b))
                        .SelectMany(x => new[] { g.Raeume[x.Paar.RaumA].Kennung, g.Raeume[x.Paar.RaumB].Kennung }), StringComparer.Ordinal);
                    HashSet<string> r = Bezug(b);
                    if (r == null || r.All(anPaaren.Contains)) { verbraucht.Add(b); continue; }
                    // Teilweise abgedeckt: Das Bauteil gibt die Fläche seiner Paare ab; was bleibt, trägt es weiter.
                    if (!b.BruttoflaecheM2.HasValue) continue;
                    double rest = b.BruttoflaecheM2.Value - neu.Where(x => x.Abgedeckt.Contains(b)).Sum(x => x.Paar.FlaecheM2);
                    if (rest < Koerpernachbarschaft.FLAECHE_MIN_M2) { verbraucht.Add(b); continue; }
                    if (b.NettoflaecheM2.HasValue) b.NettoflaecheM2 = b.NettoflaecheM2.Value * rest / b.BruttoflaecheM2.Value;
                    b.BruttoflaecheM2 = Math.Round(rest, 6);
                }

                // Die Trenndecken aus den Raumbezügen eines Geschosspaars weichen den Körperdecken dieses Paars.
                _trenndecken.TryGetValue(gi, out SortedDictionary<(string Unten, string Oben), List<AbbildBauteil>> geschosspaare);
                var koerperdecken = new SortedDictionary<(string Unten, string Oben), List<AbbildBauteil>>();
                foreach ((AbbildBauteil t, Koerperpaar p, _) in neu.Where(x => x.Paar.Decke))
                {
                    AbbildRaum o = g.Raeume[p.Oben], u = g.Raeume[p.Oben == p.RaumA ? p.RaumB : p.RaumA];
                    if (u.GeschossKennung == null || o.GeschossKennung == null || u.GeschossKennung == o.GeschossKennung) continue;
                    (string, string) schluessel = (u.GeschossKennung, o.GeschossKennung);
                    if (!koerperdecken.TryGetValue(schluessel, out List<AbbildBauteil> l)) koerperdecken[schluessel] = l = new List<AbbildBauteil>();
                    l.Add(t);
                }
                foreach (KeyValuePair<(string Unten, string Oben), List<AbbildBauteil>> kd in koerperdecken)
                {
                    if (geschosspaare != null && geschosspaare.TryGetValue(kd.Key, out List<AbbildBauteil> alt))
                        foreach (AbbildBauteil b in alt.Where(b => b.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_BEZUG)) verbraucht.Add(b);
                    if (geschosspaare == null) _trenndecken[gi] = geschosspaare = new SortedDictionary<(string, string), List<AbbildBauteil>>();
                    geschosspaare[kd.Key] = kd.Value;
                }

                // Die Öffnungen eines aufgegangenen Bauteils gehen an sein größtes Paar.
                foreach (AbbildBauteil b in bestand.Where(verbraucht.Contains))
                {
                    if (b.Oeffnungen.Count == 0) continue;
                    AbbildBauteil ziel = neu.Where(x => x.Abgedeckt.Contains(b)).OrderByDescending(x => x.Paar.FlaecheM2).Select(x => x.Teil).FirstOrDefault();
                    ziel?.Oeffnungen.AddRange(b.Oeffnungen);
                }
                g.Bauteile.RemoveAll(verbraucht.Contains);
                g.Bauteile.AddRange(neu.Select(x => x.Teil));
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "GRENZEN_AUS_KOERPER", g.Anzeigename, Ganz(paare.Count),
                    Zahl(Math.Round(g.KoerperTrennwandM2, 2)), Zahl(Math.Round(g.KoerperTrenndeckeM2, 2))));
            }
        }

        /// <summary>Kleinste Überlappung zweier Grundrisse übereinanderliegender Räume, ab der sie eine Trenndecke teilen [m²].</summary>
        internal const double UEBERLAPPUNG_MIN_M2 = 1.0;

        /// <summary>
        /// <b>Trenndecken ohne Raumgrenzen und ohne Raumbezug</b> (Mehrzonenkonzept 6.5, Rest der Stufe G6c): Für je zwei
        /// übereinanderliegende Geschosse mit Räumen, die kein Raumbezug schon verbindet:
        /// <list type="bullet">
        /// <item><b>Grundriss:</b> Tragen alle Räume beider Geschosse einen Grundriss (<see cref="AbbildRaum.GrundrissM"/>),
        /// bekommt jedes Raumpaar, dessen Grundrisse sich um mindestens <see cref="UEBERLAPPUNG_MIN_M2"/> überdecken, eine
        /// Trenndecke als Paar mit der Überlappung als Fläche (Herkunft <see cref="AbbildBauteil.TRENNDECKE_GRUNDRISS"/>).
        /// U-Wert, Aufbau und Dicke stammen von der größten freien Decke des Geschosspaars (eine Platte innen ohne Grenze,
        /// Nachbarn und Hüllerklärung, im oberen Geschoss vor dem unteren); sie geht in den Paaren auf. Ohne solche Decke
        /// bleibt das Paar ohne U-Wert (Vorgabe der Baualtersklasse).</item>
        /// <item><b>Geschoss:</b> Fehlt einem Raum der Grundriss und führt die Datei keine Raumbezüge (sonst nennen
        /// diese die Nachbarn selbst), trennt jede freie Decke des Geschosspaars den ersten
        /// beheizten Raum je Geschoss (sonst den ersten), mit ihrer Fläche (Herkunft
        /// <see cref="AbbildBauteil.TRENNDECKE_GESCHOSS"/>) — wie der Raumbezug, auch mit dessen Schätzung.</item>
        /// </list>
        /// Die Paare gehen in die Kopplung der Raumbezüge ein (<see cref="ReferenzenMelden"/>); <c>GRENZEN_ENTKOPPELT</c>
        /// bleibt nur, wo auch das scheitert. Meldung je Gebäude <c>IMP_IFC_PROT_TRENNDECKE_GRUNDRISS</c> (I).
        /// </summary>
        private void GrundrissTrenndecken()
        {
            for (int gi = 0; gi < _abbild.Gebaeude.Count; gi++)
            {
                AbbildGebaeude g = _abbild.Gebaeude[gi];
                if (g.ZahlGrenzen > 0) continue;
                var geschosse = g.Raeume.Where(r => r.GeschossKennung != null)
                    .GroupBy(r => r.GeschossKennung, StringComparer.Ordinal)
                    .Select(x => (Kennung: x.Key, Lage: g.Geschosse.FirstOrDefault(k => k.Kennung == x.Key)?.LageM ?? x.First().GeschossLageM,
                                  Raeume: x.ToList()))
                    .Where(x => x.Lage.HasValue).OrderBy(x => x.Lage.Value).ToList();
                if (geschosse.Count < 2) continue;
                _trenndecken.TryGetValue(gi, out SortedDictionary<(string Unten, string Oben), List<AbbildBauteil>> paare);
                // Führt die Datei Raumbezüge, nennt sie die Nachbarn selbst: Eine Decke ohne Bezug trennt dann keine Räume.
                bool bezuege = _raumbezug.Values.Any(l => l.Any(r => _raumGebaeude.TryGetValue(r, out int x) && x == gi));
                var verbraucht = new HashSet<AbbildBauteil>();
                var neu = new List<AbbildBauteil>();
                int zahl = 0;
                double flaeche = 0.0;
                var geschosspaare = new List<string>();
                for (int i = 1; i < geschosse.Count; i++)
                {
                    var u = geschosse[i - 1];
                    var o = geschosse[i];
                    if (u.Lage.Value == o.Lage.Value) continue;
                    if (paare != null && paare.ContainsKey((u.Kennung, o.Kennung))) continue;
                    List<AbbildBauteil> vorlagen = g.Bauteile.Where(b => FreieDecke(b) && !verbraucht.Contains(b)
                                                                         && (b.GeschossKennung == o.Kennung || b.GeschossKennung == u.Kennung))
                        .OrderByDescending(b => b.GeschossKennung == o.Kennung).ThenByDescending(b => b.BruttoflaecheM2 ?? 0.0).ToList();
                    var stuecke = new List<AbbildBauteil>();
                    if (u.Raeume.All(r => r.GrundrissM != null) && o.Raeume.All(r => r.GrundrissM != null))
                    {
                        AbbildBauteil v = vorlagen.FirstOrDefault();
                        foreach (AbbildRaum ru in u.Raeume)
                            foreach (AbbildRaum ro in o.Raeume)
                            {
                                double f = Grundrissueberlappung.Ueberlappung(ru.GrundrissM, ro.GrundrissM);
                                if (f < UEBERLAPPUNG_MIN_M2) continue;
                                var t = new AbbildBauteil
                                {
                                    Kennung = (v?.Kennung ?? "TRENNDECKE") + "|" + ru.Kennung + "|" + ro.Kennung,
                                    Quelltyp = v?.Quelltyp ?? "IfcSlab", Name = v?.Name, Quellart = v?.Quellart,
                                    Art = Bauteilart.Decke, Randbedingung = Randbedingung.Innen,
                                    BruttoflaecheM2 = Math.Round(f, 4), UWertWm2K = v?.UWertWm2K, UWertQuelle = v?.UWertQuelle,
                                    Aufbau = v?.Aufbau, DickeM = v?.DickeM, GeschossKennung = o.Kennung,
                                    Trenndeckenherkunft = AbbildBauteil.TRENNDECKE_GRUNDRISS,
                                };
                                Paar(t, ru, ro);
                                stuecke.Add(t);
                                neu.Add(t);
                            }
                        if (stuecke.Count > 0 && v != null) verbraucht.Add(v);
                    }
                    else if (!bezuege)
                    {
                        AbbildRaum ru = u.Raeume.FirstOrDefault(r => r.Beheizt) ?? u.Raeume[0];
                        AbbildRaum ro = o.Raeume.FirstOrDefault(r => r.Beheizt) ?? o.Raeume[0];
                        foreach (AbbildBauteil v in vorlagen)
                        {
                            Paar(v, ru, ro);
                            v.Trenndeckenherkunft = AbbildBauteil.TRENNDECKE_GESCHOSS;
                            stuecke.Add(v);
                        }
                    }
                    if (stuecke.Count == 0) continue;
                    if (paare == null) _trenndecken[gi] = paare = new SortedDictionary<(string, string), List<AbbildBauteil>>();
                    paare[(u.Kennung, o.Kennung)] = stuecke;
                    zahl += stuecke.Count;
                    flaeche += stuecke.Sum(t => t.BruttoflaecheM2 ?? 0.0);
                    geschosspaare.Add((g.Geschosse.FirstOrDefault(k => k.Kennung == u.Kennung)?.Anzeigename ?? u.Kennung) + "/"
                                      + (g.Geschosse.FirstOrDefault(k => k.Kennung == o.Kennung)?.Anzeigename ?? o.Kennung));
                }
                if (zahl == 0) continue;
                g.Bauteile.RemoveAll(verbraucht.Contains);
                g.Bauteile.AddRange(neu);
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "TRENNDECKE_GRUNDRISS", g.Anzeigename, Ganz(zahl),
                    Zahl(Math.Round(flaeche, 2)), string.Join(", ", geschosspaare)));
            }
        }

        /// <summary>Eine Platte innen ohne Grenze, Nachbarn und Hüllerklärung, bewertet — Vorlage einer Trenndecke ohne Raumbezug.</summary>
        private static bool FreieDecke(AbbildBauteil b)
            => string.Equals(b.Quelltyp, "IfcSlab", StringComparison.OrdinalIgnoreCase)
               && b.Quellart != nameof(IfcSlabTypeEnum.ROOF) && b.Quellart != nameof(IfcSlabTypeEnum.BASESLAB)
               && b.Nachbarn.Count == 0 && b.Grenzen.Count == 0 && !b.HuelleOhneNachbar
               && (b.Randbedingung == Randbedingung.Innen || b.Randbedingung == Randbedingung.Unbekannt)
               && (b.UWertWm2K > 0.0 || b.Aufbau != null || !b.UWertWm2K.HasValue);

        /// <summary>Die Nachbarn einer Trenndecke: der beheizte Raum zuerst, Sicht aus der Lage (oben Boden, unten Decke).</summary>
        private static void Paar(AbbildBauteil t, AbbildRaum unten, AbbildRaum oben)
        {
            var nOben = new AbbildNachbar(oben.Kennung, GebaeudeAggregation.SICHT_BODEN);
            var nUnten = new AbbildNachbar(unten.Kennung, GebaeudeAggregation.SICHT_DECKE);
            bool obenZuerst = oben.Beheizt && !unten.Beheizt;
            t.Nachbarn.Add(obenZuerst ? nOben : nUnten);
            t.Nachbarn.Add(obenZuerst ? nUnten : nOben);
        }

        /// <summary>Der Name eines Raums, sonst seine Kennung.</summary>
        private string Anzeige(int raum) => string.IsNullOrWhiteSpace(_raum[raum].Name) ? _raum[raum].Kennung : _raum[raum].Name.Trim();

        /// <summary>Der erste beheizte Raum einer Gruppe, sonst der erste.</summary>
        private int Erster(IEnumerable<int> raeume)
        {
            List<int> liste = raeume.ToList();
            foreach (int r in liste) if (_raum[r].Beheizt) return r;
            return liste[0];
        }

        /// <summary>
        /// Die Meldungen der Raumbezüge und die <b>Schätzung der Trenndeckenfläche</b> (Anwenderentscheid 03.10.2026):
        /// <list type="bullet">
        /// <item>Je Gebäude die Trenndecken je Geschosspaar und die Innenwände (<c>IMP_IFC_PROT_TRENNDECKE_REFERENZ</c>, I).</item>
        /// <item>Bleibt die Summe der referenzierten Deckenteile eines Geschosspaars unter der kleineren beheizten
        /// Grundfläche der beiden Geschosse (aus den Raummengen), referenziert die Datei nicht alle Deckenteile: Die
        /// Trenndeckenfläche ist dann diese Grundfläche, auf die referenzierten Teile im Verhältnis ihrer Flächen verteilt
        /// (ohne Flächen zu gleichen Teilen) — U-Wert und Aufbau bleiben die der Teile, über die Fläche gewichtet
        /// (<c>IMP_IFC_PROT_TRENNDECKE_GESCHAETZT</c>, I, mit geschätzter und referenzierter Fläche). Ohne beheizten Raum
        /// in einem der Geschosse wird nicht geschätzt.</item>
        /// <item>Der Kopplungswächter: Ein Paar unter <see cref="TRENNDECKE_ANTEIL_MIN"/> der Grundfläche des kleineren
        /// Geschosses koppelt nicht (<c>IMP_IFC_PROT_TRENNDECKE_KLEIN</c>, W); nach der Schätzung greift er nur, wenn ein
        /// Paar keine Grundfläche zum Schätzen hat. Grenzt jedes Deckenteil eines Paars an einen unbeheizten Raum eines
        /// Geschosses, das auch beheizte Räume hat, verbindet das Paar diese nicht: Es wird weder geschätzt noch trägt es.</item>
        /// <item>Je Platte, deren Erklärung gegen unbeheizt vor dem Bezug gilt, obwohl der Raum der unbeheizten Seite als
        /// beheizt gilt, ein Hinweis (<c>IMP_IFC_PROT_ERKLAERUNG_VOR_BEZUG</c>, W).</item>
        /// <item>Dateiweit die Innenwände, die einseitig als innere Masse zählen (<c>IMP_IFC_PROT_INNEN_EINSEITIG</c>, I).</item>
        /// </list>
        /// </summary>
        private void ReferenzenMelden()
        {
            for (int gi = 0; gi < _abbild.Gebaeude.Count; gi++)
            {
                AbbildGebaeude g = _abbild.Gebaeude[gi];
                if (_erklaerungVorBezug.TryGetValue(gi, out List<(string Bauteil, string Raum)> erklaert))
                    foreach ((string bauteil, string raum) in erklaert)
                        g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "ERKLAERUNG_VOR_BEZUG", bauteil, raum));
                _trenndecken.TryGetValue(gi, out SortedDictionary<(string Unten, string Oben), List<AbbildBauteil>> paare);
                _bezugswaende.TryGetValue(gi, out int waende);
                // Die Meldung der Raumbezüge zählt nur deren Decken; die aus Geschoss und Grundriss meldet GrundrissTrenndecken.
                int decken = paare?.Values.Sum(p => p.Count(t => t.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_BEZUG)) ?? 0;
                if ((paare == null || paare.Count == 0) && waende == 0) continue;
                g.ZahlTrenndeckenReferenz = decken;
                string Name(string kennung) => g.Geschosse.FirstOrDefault(x => x.Kennung == kennung)?.Anzeigename ?? kennung;
                double Hoehe(string kennung) => g.Geschosse.FirstOrDefault(x => x.Kennung == kennung)?.LageM ?? 0.0;
                List<KeyValuePair<(string Unten, string Oben), List<AbbildBauteil>>> geordnet = paare == null
                    ? new List<KeyValuePair<(string Unten, string Oben), List<AbbildBauteil>>>()
                    : paare.OrderBy(p => Hoehe(p.Key.Unten)).ThenBy(p => Hoehe(p.Key.Oben)).ToList();
                int Bezug(List<AbbildBauteil> teile) => teile.Count(t => t.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_BEZUG);
                List<KeyValuePair<(string Unten, string Oben), List<AbbildBauteil>>> ausBezug = geordnet.Where(p => Bezug(p.Value) > 0).ToList();
                string liste = ausBezug.Count == 0 ? "—"
                    : string.Join(", ", ausBezug.Select(p => Name(p.Key.Unten) + "/" + Name(p.Key.Oben) + (Bezug(p.Value) > 1 ? " (" + Ganz(Bezug(p.Value)) + ")" : "")));
                if (decken > 0 || waende > 0)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "TRENNDECKE_REFERENZ", g.Anzeigename, Ganz(decken), liste, Ganz(waende)));
                if (geordnet.Count == 0) continue;

                // Die beheizte Grundfläche je Geschoss aus den Raummengen — Maßstab der Schätzung und der Kopplung.
                var warm = g.Raeume.Where(r => r.Beheizt && r.GeschossKennung != null)
                                   .GroupBy(r => r.GeschossKennung, StringComparer.Ordinal)
                                   .ToDictionary(x => x.Key, x => x.Sum(r => r.FlaecheM2 ?? 0.0), StringComparer.Ordinal);
                // Grenzt jedes Deckenteil eines Paars auf einer Seite an einen unbeheizten Raum eines Geschosses MIT beheizten
                // Räumen, liegt die Decke unter Z4 an der unbeheizten Gruppe dieses Geschosses, und seine beheizten Räume
                // bleiben ohne Verbindung: Das Paar wird weder geschätzt noch trägt es. Ein Geschoss ganz ohne beheizten Raum
                // verbindet weiter (eine Zone).
                var raumGeschoss = g.Raeume.GroupBy(r => r.Kennung, StringComparer.Ordinal)
                                           .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
                bool Verbindet(AbbildNachbar n) => raumGeschoss.TryGetValue(n.Kennung, out AbbildRaum r)
                                                   && (r.Beheizt || r.GeschossKennung == null || !warm.ContainsKey(r.GeschossKennung));
                var tragend = new List<(string Unten, string Oben)>();
                foreach (KeyValuePair<(string Unten, string Oben), List<AbbildBauteil>> p in geordnet)
                {
                    if (!p.Value.Any(t => t.Nachbarn.Count == 2 && t.Nachbarn.All(Verbindet))) continue;
                    double kleiner = Math.Min(warm.TryGetValue(p.Key.Unten, out double u) ? u : 0.0, warm.TryGetValue(p.Key.Oben, out double o) ? o : 0.0);
                    double referenziert = p.Value.Sum(t => t.BruttoflaecheM2 ?? 0.0);
                    double flaeche = referenziert;
                    // Die Überlappung der Grundrisse ist gemessen, keine Teilmenge: keine Schätzung.
                    bool gemessen = p.Value.All(t => t.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_GRUNDRISS
                                                     || t.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_KOERPER);
                    bool koerper = p.Value.All(t => t.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_KOERPER);
                    if (kleiner > 0.0 && referenziert < kleiner && !gemessen)
                    {
                        Schaetzen(p.Value, referenziert, kleiner);
                        flaeche = kleiner;
                        g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "TRENNDECKE_GESCHAETZT", Name(p.Key.Unten), Name(p.Key.Oben),
                            Zahl(Math.Round(kleiner, 2)), Zahl(Math.Round(referenziert, 2))));
                    }
                    if (flaeche >= TRENNDECKE_ANTEIL_MIN * kleiner) { tragend.Add(p.Key); continue; }
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + (koerper ? "KOERPERPAAR_SCHWACH" : "TRENNDECKE_KLEIN"), Name(p.Key.Unten), Name(p.Key.Oben),
                        Zahl(Math.Round(flaeche, 2)), Zahl(Math.Round(kleiner, 2)), Ganz((int)Math.Round(100.0 * flaeche / kleiner))));
                }
                g.GeschosseGekoppelt = Gekoppelt(warm.Keys, tragend);
            }
            if (_innenEinseitig > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "INNEN_EINSEITIG", Ganz(_innenEinseitig),
                    Zahl(Math.Round(_innenEinseitigM2, 2))));
        }

        /// <summary>
        /// Verteilt die geschätzte Trenndeckenfläche auf die referenzierten Teile: im Verhältnis ihrer Bruttoflächen
        /// (die flächengewichteten U-Werte und Aufbauten bleiben), ohne Flächen zu gleichen Teilen; eine Nettofläche der
        /// Datei wird im selben Verhältnis mitgeführt.
        /// </summary>
        private static void Schaetzen(List<AbbildBauteil> teile, double referenziert, double ziel)
        {
            foreach (AbbildBauteil t in teile)
            {
                double faktor = referenziert > 0.0 ? ziel / referenziert : 0.0;
                if (referenziert > 0.0 && !t.BruttoflaecheM2.HasValue) continue;
                double alt = t.BruttoflaecheM2 ?? 0.0;
                t.BruttoflaecheM2 = referenziert > 0.0 ? alt * faktor : ziel / teile.Count;
                if (t.NettoflaecheM2.HasValue && alt > 0.0) t.NettoflaecheM2 = t.NettoflaecheM2.Value * t.BruttoflaecheM2.Value / alt;
            }
        }

        /// <summary>Liegen alle Geschosse mit beheizten Räumen über die Geschosspaare in einem Verbund (auch über ein unbeheiztes Geschoss)?</summary>
        private static bool Gekoppelt(IEnumerable<string> beheizteGeschosse, List<(string Unten, string Oben)> paare)
        {
            var warm = new HashSet<string>(beheizteGeschosse, StringComparer.Ordinal);
            if (warm.Count < 2) return false;
            var erreicht = new HashSet<string>(StringComparer.Ordinal) { warm.First() };
            bool weiter = true;
            while (weiter)
            {
                weiter = false;
                foreach ((string u, string o) in paare)
                    if (erreicht.Contains(u) != erreicht.Contains(o)) { erreicht.Add(u); erreicht.Add(o); weiter = true; }
            }
            return warm.All(erreicht.Contains);
        }

        /// <summary>
        /// Der Azimut eines senkrechten Außenbauteils aus der Platzierungskette und der Seite seiner Räume
        /// (3.4); ohne Raumgrenze bleibt er unbestimmt (<c>IMP_IFC_PROT_SEITE_UNBESTIMMT</c>).
        /// </summary>
        private void Azimut(IIfcElement e, AbbildBauteil b, List<IIfcRelSpaceBoundary> grenzen, int gi)
        {
            if (b.Randbedingung != Randbedingung.Aussenluft && b.Randbedingung != Randbedingung.Erdreich) return;
            IfcRahmen? rahmen = IfcPlatzierung.Weltrahmen(e.ObjectPlacement, _wurzel, out string fremd);
            if (fremd != null)
            {
                _platzierungsart[fremd] = _platzierungsart.TryGetValue(fremd, out int z) ? z + 1 : 1;
                return;
            }
            if (!rahmen.HasValue) { Unbestimmt(e, b, grenzen); return; }

            List<double[]> punkte = Raeume(grenzen, gi).Where(r => _raumPunkt.ContainsKey(r)).Select(r => _raumPunkt[r]).ToList();
            int? seite = IfcPlatzierung.Aussenseite(rahmen.Value, punkte);
            if (!seite.HasValue) { Unbestimmt(e, b, grenzen); return; }

            double nx = seite.Value * rahmen.Value.Y[0], ny = seite.Value * rahmen.Value.Y[1];
            b.AzimutGrad = IfcPlatzierung.Azimut(nx, ny, _drehung);

            // Für die Mehrschalenprobe (3.5 Nr. 9): Achslage längs der Außennormalen.
            double[] n = IfcPlatzierung.Normiert(new[] { nx, ny, 0.0 });
            if (n != null && gi >= 0)
                _schalen.Add(new Schale
                {
                    Gebaeude = gi,
                    Raeume = string.Join(",", Raeume(grenzen, gi)),
                    Azimut = b.AzimutGrad.Value,
                    AbstandM = (rahmen.Value.Ursprung[0] * n[0] + rahmen.Value.Ursprung[1] * n[1]) * _einheiten.Laenge,
                });
        }

        /// <summary>Die Namen der Himmelsrichtung, die ein CAD-Export am Bauteil selbst nennt (<c>Orientation (°)</c>).</summary>
        internal static readonly IReadOnlyList<string> ORIENTIERUNG = new[] { "Orientation" };

        private readonly SortedDictionary<string, int> _azimutRueckfall = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Die Seite ist aus Platzierung und Räumen nicht zu bestimmen. <b>Ohne Raumgrenze</b> gilt die
        /// Himmelsrichtung, die die Datei am Bauteil selbst nennt (<see cref="ORIENTIERUNG"/>, Mehrzonenkonzept
        /// 6.5): eine Zahl in [0°, 360°], 0° = Nord, im Uhrzeigersinn, auch als Text mit Dezimalkomma; als Einheit
        /// im Namen nur Grad. Sie wird als geografische Richtung übernommen — ohne den Nordwinkel des Modells —
        /// und benannt (<c>IMP_IFC_PROT_AZIMUT_RUECKFALL</c>); sonst bleibt die Seite unbestimmt.
        /// </summary>
        private void Unbestimmt(IIfcElement e, AbbildBauteil b, List<IIfcRelSpaceBoundary> grenzen)
        {
            if (grenzen.Count == 0)
                foreach (IfcFund f in IfcEigenschaften.AlleMitNamen(_bezuege, e, ORIENTIERUNG))
                {
                    IfcEigenschaften.NameOhneEinheit(f.Eigenschaft.Name.ToString(), out string einheit);
                    if (einheit != null && einheit != "°" && !einheit.Equals("deg", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!(f.Eigenschaft is IIfcPropertySingleValue einzel)) continue;
                    double? w = IfcEigenschaften.Zahl(einzel.NominalValue);
                    if (!w.HasValue && IfcEigenschaften.Textwert(einzel.NominalValue) is string t
                        && double.TryParse(t.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                        w = z;
                    if (!(w >= 0.0 && w <= 360.0)) continue;
                    b.AzimutGrad = IfcPlatzierung.Normieren(w.Value);
                    Zaehlen(_azimutRueckfall, f.Satz + "." + f.Eigenschaft.Name);
                    return;
                }
            _seiteUnbestimmt.Add(b.Kennung);
        }

        private AbbildBauteil Oeffnung(IIfcElement o, string klasse, Bauteilart art, AbbildBauteil wirt, int gi = -1)
        {
            var b = new AbbildBauteil
            {
                Kennung = o.GlobalId.ToString(),
                Quelltyp = o.ExpressType.ExpressName,
                Name = IfcEigenschaften.Text(o.Name),
                Art = art,
                Randbedingung = wirt.Randbedingung,
                NeigungGrad = wirt.NeigungGrad,
            };
            double? flaeche = Positiv(IfcEigenschaften.Menge(_bezuege, o, klasse, "Area", _einheiten))
                              ?? Produkt(IfcEigenschaften.Menge(_bezuege, o, klasse, "Width", _einheiten),
                                         IfcEigenschaften.Menge(_bezuege, o, klasse, "Height", _einheiten));
            if (!flaeche.HasValue)
            {
                double breite = double.NaN, hoehe = double.NaN;
                if (o is IIfcWindow f)
                {
                    breite = IfcEigenschaften.Wert(f.OverallWidth);
                    hoehe = IfcEigenschaften.Wert(f.OverallHeight);
                }
                else if (o is IIfcDoor t)
                {
                    breite = IfcEigenschaften.Wert(t.OverallWidth);
                    hoehe = IfcEigenschaften.Wert(t.OverallHeight);
                }
                if (breite > 0.0 && hoehe > 0.0) flaeche = breite * hoehe * _einheiten.Laenge * _einheiten.Laenge;
            }
            // Zuletzt ein Flächenname aus einem beliebigen Mengensatz — nie Breite × Höhe eines fremden Satzes.
            if (!flaeche.HasValue) flaeche = FlaecheRueckfall(o, RUECKFALL_OEFFNUNG, "Area");
            b.BruttoflaecheM2 = flaeche;
            if (!flaeche.HasValue) _ohneMengen.Add(b.Kennung);

            Seiten(o).Uebertragen(b);
            UWert(o, "Pset_" + klasse + "Common", b);
            IfcFund g = IfcEigenschaften.Finden(_bezuege, o, "Pset_DoorWindowGlazingType", "SolarHeatGainTransmittance");
            b.GWert = g == null ? null : Zahl(g);
            Rahmenanteil(o, b);
            Bauteilkoerper(o, b, gi);
            GrenzenUebernehmen(b, GrenzenVon(o));
            b.GeschossKennung = _elementGeschoss.TryGetValue(o.EntityLabel, out string geschoss) ? geschoss : wirt.GeschossKennung;
            return b;
        }

        /// <summary>
        /// Die Raumgrenzen eines Bauteils in das Abbild (Stufe G6c): je Grenze Raum, Lage, Art, das
        /// Gegenstück der Datei und — soweit auswertbar — Fläche, Schwerpunkt und Normale in
        /// Weltkoordinaten (<see cref="IfcGrenzgeometrie"/>). Eine Geometrie, die sich nicht auswerten
        /// lässt, wird je Typ gezählt (<c>IMP_IFC_PROT_FLAECHE_UNBEKANNT</c>).
        /// </summary>
        private void GrenzenUebernehmen(AbbildBauteil b, List<IIfcRelSpaceBoundary> grenzen)
        {
            foreach (IIfcRelSpaceBoundary g in grenzen.OrderBy(x => x.EntityLabel))
            {
                _grenzenUebernommen.Add(g.EntityLabel);
                IIfcSpace raum = g.RelatingSpace as IIfcSpace;
                var a = new AbbildGrenze
                {
                    Kennung = g.GlobalId.ToString(),
                    RaumKennung = raum?.GlobalId.ToString(),
                    Lage = Grenzlage(g.InternalOrExternalBoundary),
                    Virtuell = g.PhysicalOrVirtualBoundary == IfcPhysicalOrVirtualEnum.VIRTUAL,
                };
                if (_abbild.SchemaStand != IfcSchemaStand.Ifc2x3 && g is IIfcRelSpaceBoundary2ndLevel zweite)
                    a.GegenstueckKennung = zweite.CorrespondingBoundary?.GlobalId.ToString();
                bool rahmen = raum != null && _raumRahmen.ContainsKey(raum.EntityLabel);
                IfcGrenzgeometrie.Flaeche f = IfcGrenzgeometrie.Lesen(g, rahmen ? _raumRahmen[raum.EntityLabel] : (IfcRahmen?)null, _einheiten.Laenge);
                if (f != null && f.Fehler != null)
                {
                    a.Geometriefehler = f.Fehler;
                    _flaecheUnbekannt[f.Fehler] = _flaecheUnbekannt.TryGetValue(f.Fehler, out int n) ? n + 1 : 1;
                }
                else if (f != null)
                {
                    a.FlaecheM2 = f.FlaecheM2;
                    a.AusschnittM2 = f.AusschnittM2;
                    // Ohne Platzierung des Raums bleibt die Lage im Raum unbekannt; der Inhalt gilt.
                    a.SchwerpunktM = rahmen ? f.SchwerpunktM : null;
                    a.Normale = rahmen ? f.Normale : null;
                    a.RandpunkteM = rahmen ? f.RandpunkteM : null;
                }
                b.Grenzen.Add(a);
            }
        }

        private static Randbedingung Grenzlage(IfcInternalOrExternalEnum lage)
        {
            switch (lage)
            {
                case IfcInternalOrExternalEnum.EXTERNAL: return Randbedingung.Aussenluft;
                case IfcInternalOrExternalEnum.EXTERNAL_EARTH: return Randbedingung.Erdreich;
                case IfcInternalOrExternalEnum.INTERNAL: return Randbedingung.Innen;
                default: return Randbedingung.Unbekannt;
            }
        }

        /// <summary>
        /// <b>Der U-Wert eines Bauteils</b>: <c>Pset_&lt;Klasse&gt;Common.ThermalTransmittance</c> (Vorkommnis vor
        /// Typ). Fehlt er, gilt jede Eigenschaft <see cref="UWERT_NAMEN"/> eines beliebigen Satzes
        /// (Mehrzonenkonzept 6.5) — benannt (<c>IMP_IFC_PROT_UWERT_RUECKFALL</c>). Verglichen wird der Name ohne
        /// angehängte Einheit; nennt der Name eine andere Einheit als W/(m²K) (etwa <c>W/(m K)</c>), gilt der
        /// Wert nicht als U-Wert, sondern wird benannt übergangen (<c>IMP_IFC_PROT_UWERT_EINHEIT</c>). Ein Wert null
        /// oder kleiner ist kein U-Wert (ein CAD-Export schreibt 0 oder −1 an Bauteile ohne energetische Bewertung):
        /// übergangen; bleibt das Bauteil dadurch ohne U-Wert, benannt (<c>IMP_IFC_PROT_UWERT_NICHT_POSITIV</c>).
        /// </summary>
        /// <returns>Bleibt das Bauteil ohne U-Wert, weil die Datei ihn null oder kleiner angibt?</returns>
        private bool UWert(IIfcElement e, string satz, AbbildBauteil b)
        {
            IfcFund gewaehlt = null;
            double? u = null;
            string nichtPositiv = null;
            var abweichend = new List<string>();
            List<IfcFund> kandidaten = IfcEigenschaften.AlleMitNamen(_bezuege, e, UWERT_NAMEN).ToList();
            // Der Standardsatz zuerst (Vorkommnis vor Typ), dann alle übrigen in Dateireihenfolge.
            IEnumerable<IfcFund> reihe = kandidaten.Where(f => IfcEigenschaften.Gleich(f.Satz, satz) && IstStandardname(f))
                                                   .OrderBy(f => f.Quelle)
                                                   .Concat(kandidaten.Where(f => !(IfcEigenschaften.Gleich(f.Satz, satz) && IstStandardname(f))));
            foreach (IfcFund f in reihe)
            {
                IfcEigenschaften.NameOhneEinheit(f.Eigenschaft.Name.ToString(), out string einheit);
                if (einheit != null && !IfcEigenschaften.IstUWertEinheit(einheit))
                {
                    abweichend.Add(f.Satz + "\u0001" + f.Eigenschaft.Name + "\u0001" + einheit);
                    continue;
                }
                u = Zahl(f);
                if (!u.HasValue) continue;
                if (!(u.Value > 0.0))
                {
                    // Ein CAD-Export schreibt 0 oder −1 an Bauteile ohne energetische Bewertung: kein U-Wert.
                    nichtPositiv = nichtPositiv ?? f.Satz + "\u0001" + f.Eigenschaft.Name + "\u0001" + Zahl(u.Value);
                    u = null;
                    continue;
                }
                gewaehlt = f;
                break;
            }
            foreach (string a in abweichend.Distinct())
            {
                if (!_uEinheit.TryGetValue(a, out int[] z)) _uEinheit[a] = z = new int[2];
                z[0]++;
                if (gewaehlt == null) z[1]++;
            }
            if (gewaehlt == null && nichtPositiv != null) Zaehlen(_uNichtPositiv, nichtPositiv);
            if (gewaehlt == null) return nichtPositiv != null;
            if (!(IfcEigenschaften.Gleich(gewaehlt.Satz, satz) && gewaehlt.Eigenschaft.Name.ToString().Trim()
                      .Equals("ThermalTransmittance", StringComparison.OrdinalIgnoreCase)))
                Zaehlen(_uRueckfall, gewaehlt.Satz + "\u0001" + gewaehlt.Eigenschaft.Name);
            _abbild.ZahlUWerte++;
            b.UWertWm2K = u;
            b.UWertQuelle = gewaehlt.Satz + (gewaehlt.Quelle == IfcEigenschaftsquelle.Typ ? " (Typ)" : "");
            return false;
        }

        private static bool IstStandardname(IfcFund f)
            => IfcEigenschaften.Gleich(IfcEigenschaften.NameOhneEinheit(f.Eigenschaft.Name.ToString(), out _), "ThermalTransmittance");

        // ==================================================================
        //  Schichten (IfcMaterialLayerSet, Pset_MaterialThermal / Pset_MaterialCommon)
        // ==================================================================

        /// <summary>Der Eigenschaftssatz der thermischen Stoffwerte (λ, c) — IFC4/IFC4X3.</summary>
        internal const string PSET_STOFF_THERMISCH = "Pset_MaterialThermal";

        /// <summary>Der allgemeine Eigenschaftssatz des Baustoffs (ρ) — IFC4/IFC4X3.</summary>
        internal const string PSET_STOFF_ALLGEMEIN = "Pset_MaterialCommon";

        /// <summary>Schichtsatz → Kennung, Name und größte Dicke nach der Dateieinheit [m]: als Millimeter gelesen.</summary>
        private readonly SortedDictionary<int, (string Kennung, string Name, double Groesste)> _schichtMillimeter
            = new SortedDictionary<int, (string Kennung, string Name, double Groesste)>();

        /// <summary>Die schon gezählten Schichtsätze — ein Satz an mehreren Bauteilen zählt seine Folien einmal.</summary>
        private readonly HashSet<int> _schichtsatzGezaehlt = new HashSet<int>();

        /// <summary>Übergangene Schichten unter der kleinsten Schichtdicke: „Name (d mm)" → Zahl der Schichten.</summary>
        private readonly SortedDictionary<string, int> _schichtDuenn = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Die Sammelhinweise der Schichtdicken, je Datei einer: Sätze als Millimeter gelesen (W, mit der
        /// größten Dicke und ihrem Satz) und Schichten unter <see cref="GebaeudeFestwerte.SCHICHT_DICKE_MIN_M"/>,
        /// die übergangen sind (I, nach Name und Dicke zusammengefasst).
        /// </summary>
        private void SchichtdickenMelden()
        {
            if (_schichtMillimeter.Count > 0)
            {
                // Die größte Dicke; bei Gleichstand der Satz mit der kleinsten Kennung.
                (string kennung, string name, double groesste) = _schichtMillimeter.Values
                    .Aggregate((x, y) => y.Groesste > x.Groesste ? y : x);
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "SCHICHTDICKE_MM", Ganz(_schichtMillimeter.Count),
                    Zahl(Math.Round(groesste, 3)), kennung, name ?? "—"));
            }
            if (_schichtDuenn.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "SCHICHT_DUENN", Ganz(_schichtDuenn.Values.Sum()),
                    string.Join(", ", _schichtDuenn.Select(kv => kv.Key + " ×" + Ganz(kv.Value)))));
        }

        /// <summary>
        /// Der Aufbau eines Bauteils aus seinem <c>IfcMaterialLayerSet</c>: Dicke je Schicht und die
        /// Stoffwerte λ, ρ, c ihres Baustoffs (<see cref="Stoffwerte"/>). Die Schichtfolge steht, wie die
        /// Datei sie zählt; ob die erste Schicht außen oder innen liegt, entscheidet erst
        /// <see cref="Schichtfolge(IIfcElement, AbbildBauteil, IIfcMaterialLayerSetUsage, List{IIfcRelSpaceBoundary}, int)"/>
        /// — bis dahin gilt die Annahme „erste Schicht außen".
        /// <para><b>Rückfall „Schichtdicke in Millimetern"</b>: Liegt nach der Längeneinheit der Datei
        /// mindestens eine Dicke des Satzes über <see cref="GebaeudeFestwerte.SCHICHT_DICKE_MAX_M"/>, gilt
        /// der ganze Satz als in Millimetern geschrieben (CAD-Exporte mit <c>METRE</c> im Kopf): alle Dicken
        /// durch 1000, ein Sammelhinweis je Datei. Schichten unter <see cref="GebaeudeFestwerte.SCHICHT_DICKE_MIN_M"/>
        /// (0,5 mm: Folien, Anstriche) tragen keine Wärmewirkung und werden mit Sammelhinweis übergangen;
        /// Bleche ab 0,5 mm bleiben als Schicht mit ihrer Masse erhalten.</para>
        /// </summary>
        private AbbildAufbau Aufbau(IIfcElement e, out IIfcMaterialLayerSetUsage nutzung)
        {
            IIfcMaterialLayerSet satz = Schichtsatz(e, out nutzung);
            if (satz == null) return null;
            var a = new AbbildAufbau
            {
                Kennung = "IfcMaterialLayerSet #" + satz.EntityLabel.ToString(CultureInfo.InvariantCulture),
                Name = IfcEigenschaften.Text(satz.LayerSetName),
                Richtung = Schichtrichtung.AussenNachInnen,
                RichtungAngenommen = true,
            };
            // Die Dicken nach der Längeneinheit der Datei; liegt eine über dem Band, ist der ganze Satz
            // in Millimetern geschrieben (Rückfall „Schichtdicke in mm", je Schichtsatz).
            List<IIfcMaterialLayer> schichten = satz.MaterialLayers.ToList();
            double[] dicken = schichten.Select(x => IfcEigenschaften.Wert(x?.LayerThickness))
                                       .Select(d => d > 0.0 && !double.IsInfinity(d) ? d * _einheiten.Laenge : 0.0).ToArray();
            double groesste = dicken.Length == 0 ? 0.0 : dicken.Max();
            bool millimeter = groesste > GebaeudeFestwerte.SCHICHT_DICKE_MAX_M;
            bool zaehlen = _schichtsatzGezaehlt.Add(satz.EntityLabel);
            if (millimeter)
            {
                for (int i = 0; i < dicken.Length; i++) dicken[i] /= 1000.0;
                _schichtMillimeter[satz.EntityLabel] = (a.Kennung, a.Name, groesste);
            }
            List<string> rueckfall = null;
            for (int i = 0; i < schichten.Count; i++)
            {
                IIfcMaterial stoff = schichten[i]?.Material;
                if (dicken[i] > 0.0 && dicken[i] < GebaeudeFestwerte.SCHICHT_DICKE_MIN_M)
                {
                    // Folie, Anstrich unter 0,5 mm: ohne Wärmewirkung — übergangen statt abgelehnt.
                    if (zaehlen) Zaehlen(_schichtDuenn, (stoff?.Name.ToString() ?? "—") + " (" + Zahl(Math.Round(dicken[i] * 1000.0, 3)) + " mm)");
                    continue;
                }
                (double? lambda, double? rho, double? cp) = Stoffwerte(stoff);
                if (!cp.HasValue && lambda > 0.0 && rho > 0.0)
                {
                    (double CpJkgK, string Quelle)? r = CpRueckfall(stoff);
                    if (r.HasValue)
                    {
                        cp = r.Value.CpJkgK;
                        if (zaehlen) (rueckfall ??= new List<string>()).Add(stoff.Name.ToString() + " = " + Zahl(r.Value.CpJkgK) + " (" + r.Value.Quelle + ")");
                    }
                }
                a.Schichten.Add(new AbbildSchicht
                {
                    BaustoffKennung = stoff?.Name.ToString() ?? "",
                    Name = stoff?.Name.ToString(),
                    DickeM = dicken[i] > 0.0 ? dicken[i] : (double?)null,
                    LambdaWmK = lambda,
                    RhoKgM3 = rho,
                    CpJkgK = cp,
                });
            }
            // Eine Meldung je Aufbau: Autorensysteme schreiben je Bauteil einen eigenen Schichtsatz gleichen Namens und Inhalts.
            if (rueckfall != null && _cpRueckfallSignaturen.Add((a.Name ?? a.Kennung) + "\u0001" + string.Join("\u0001", rueckfall)))
                _cpRueckfallAufbauten.Add((a.Name ?? a.Kennung, rueckfall));
            a.Status = a.Schichten.Count == 0 ? Aufbaustatus.OhneAufbau
                     : a.Schichten.All(s => s.Vollstaendig) ? Aufbaustatus.Vollstaendig
                     : a.Schichten.All(s => s.HatWiderstand) ? Aufbaustatus.Masselos
                     : Aufbaustatus.Unvollstaendig;
            return a;
        }

        /// <summary>Der Schichtsatz eines Bauteils samt seiner Nutzung: am Vorkommnis, sonst am Typ (dort ohne Nutzung).</summary>
        private IIfcMaterialLayerSet Schichtsatz(IIfcElement e, out IIfcMaterialLayerSetUsage nutzung)
        {
            IIfcMaterialLayerSet s = Schichtsatz(_bezuege.Zuordnungen(e), out nutzung);
            if (s != null) return s;
            foreach (IIfcRelDefinesByType rel in _bezuege.TypisiertDurch(e))
            {
                IIfcTypeObject typ = rel?.RelatingType;
                s = Schichtsatz(typ == null ? null : _bezuege.Zuordnungen(typ), out nutzung);
                if (s != null) return s;
            }
            nutzung = null;
            return null;
        }

        private static IIfcMaterialLayerSet Schichtsatz(IEnumerable<IIfcRelAssociates> zuordnungen, out IIfcMaterialLayerSetUsage nutzung)
        {
            nutzung = null;
            if (zuordnungen == null) return null;
            foreach (IIfcRelAssociatesMaterial rel in zuordnungen.OfType<IIfcRelAssociatesMaterial>())
            {
                if (rel.RelatingMaterial is IIfcMaterialLayerSetUsage n && n.ForLayerSet != null)
                {
                    nutzung = n;
                    return n.ForLayerSet;
                }
                if (rel.RelatingMaterial is IIfcMaterialLayerSet satz) return satz;
            }
            return null;
        }

        /// <summary>
        /// <b>Die Stoffwerte eines Baustoffs</b> — λ [W/(mK)] und c [J/(kgK)] aus
        /// <c>Pset_MaterialThermal</c>, ρ [kg/m³] aus <c>Pset_MaterialCommon</c> (Umsetzungskonzept 3.4,
        /// Zeile Bauweise); steht eine Größe im jeweils anderen dieser zwei Sätze, gilt auch sie, jeder
        /// andere Satz bleibt ungelesen. Die Werte gelten in SI, wie IFC sie vorgibt.
        ///
        /// <para><b>Ein Stoffwert ≤ 0 ist eine Fehlstelle</b> (Befund P: Autorensysteme füllen die Sätze
        /// oft mit Nullen) — er zählt als fehlend, der Aufbau wird damit masselos bzw. unvollständig, die
        /// Zuordnung nimmt die Vorgabe; der Baustoff wird benannt (<c>IMP_IFC_PROT_STOFFWERT_NULL</c>).</para>
        ///
        /// <para><b>IFC2X3 — die dritte Verzweigung.</b> Dort stehen die Stoffwerte als ATTRIBUTE in
        /// <c>IfcThermalMaterialProperties</c> und <c>IfcGeneralMaterialProperties</c> oder in
        /// <c>IfcExtendedMaterialProperties</c>. Gemessen an xBIM 6.1.605: Über
        /// <c>IIfcMaterialProperties.Properties</c> liefern die ersten beiden nichts (ihre Attribute
        /// bildet die Schnittstelle nicht ab), die dritte wirft <c>MissingMethodException</c>. Verlässlich
        /// lesbar ist über die Schnittstellen also nichts; die Stoffwerte werden deshalb nicht gelesen und
        /// der Baustoff benannt (<c>IMP_IFC_PROT_STOFFWERTE_NICHT_GELESEN</c>) — nicht geraten, und ohne
        /// den Lauf an einer Bibliotheksausnahme scheitern zu lassen.</para>
        /// </summary>
        private (double? Lambda, double? Rho, double? Cp) Stoffwerte(IIfcMaterial stoff)
        {
            if (stoff == null) return (null, null, null);
            if (_stoffe.TryGetValue(stoff.EntityLabel, out (double? Lambda, double? Rho, double? Cp) bekannt)) return bekannt;

            string name = stoff.Name.ToString();
            if (string.IsNullOrWhiteSpace(name)) name = "#" + stoff.EntityLabel.ToString(CultureInfo.InvariantCulture);
            (double? Lambda, double? Rho, double? Cp) werte = (null, null, null);
            try
            {
                List<IIfcMaterialProperties> saetze = _bezuege.Stoffsaetze(stoff).ToList();
                if (_abbild.SchemaStand == IfcSchemaStand.Ifc2x3)
                {
                    if (saetze.Count > 0) _stoffwerteNichtGelesen.Add(name);
                }
                else
                {
                    bool nullwert = false;
                    werte = (Stoffwert(saetze, "ThermalConductivity", PSET_STOFF_THERMISCH, PSET_STOFF_ALLGEMEIN, ref nullwert),
                             Stoffwert(saetze, "MassDensity", PSET_STOFF_ALLGEMEIN, PSET_STOFF_THERMISCH, ref nullwert),
                             Stoffwert(saetze, "SpecificHeatCapacity", PSET_STOFF_THERMISCH, PSET_STOFF_ALLGEMEIN, ref nullwert));
                    if (nullwert) _stoffwertNull.Add(name);
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is MissingMethodException || ex is InvalidCastException)
            {
                // Die Schnittstelle bildet den Satz nicht ab — nicht gelesen, benannt; der Lauf geht weiter.
                werte = (null, null, null);
                _stoffwerteNichtGelesen.Add(name);
            }
            _stoffe[stoff.EntityLabel] = werte;
            return werte;
        }

        /// <summary>
        /// <b>Die Wärmekapazität eines Baustoffs ohne <c>SpecificHeatCapacity</c></b>, aber mit Dichte und
        /// Wärmeleitfähigkeit (Mehrzonenkonzept 6.5): Katalog der Auslieferung über den Namensabgleich, sonst die
        /// Stofftabelle (<see cref="Waermekapazitaetsrueckfall"/>); einmal je Baustoff, benannt je Aufbau
        /// (<c>IMP_IFC_PROT_WAERMEKAPAZITAET_RUECKFALL</c>). <c>null</c> = kein Treffer, die Schicht bleibt masselos.
        /// </summary>
        private (double CpJkgK, string Quelle)? CpRueckfall(IIfcMaterial stoff)
        {
            if (_cpRueckfall.TryGetValue(stoff.EntityLabel, out (double CpJkgK, string Quelle)? bekannt)) return bekannt;
            _abgleichSaat ??= new Baustoffabgleich(BaustoffabgleichDaten.AusSaat());
            (double CpJkgK, string Quelle)? r = Waermekapazitaetsrueckfall.Bestimmen(stoff.Name.ToString(), _abgleichSaat);
            _cpRueckfall[stoff.EntityLabel] = r;
            return r;
        }

        /// <summary>
        /// Ein Stoffwert aus dem Satz <paramref name="satz"/>, sonst aus <paramref name="andererSatz"/>;
        /// <c>null</c> = keiner. Ein Wert ≤ 0 setzt <paramref name="nullwert"/> und zählt als fehlend.
        /// </summary>
        private static double? Stoffwert(List<IIfcMaterialProperties> saetze, string eigenschaft, string satz, string andererSatz,
                                         ref bool nullwert)
        {
            foreach (string gesucht in new[] { satz, andererSatz })
                foreach (IIfcMaterialProperties s in saetze)
                {
                    if (!IfcEigenschaften.Gleich(IfcEigenschaften.Text(s.Name), gesucht)) continue;
                    foreach (IIfcPropertySingleValue p in s.Properties.OfType<IIfcPropertySingleValue>())
                    {
                        if (!IfcEigenschaften.Gleich(p.Name.ToString(), eigenschaft)) continue;
                        double? w = IfcEigenschaften.Zahl(p.NominalValue);
                        if (!w.HasValue) continue;
                        if (w.Value > 0.0 && !double.IsInfinity(w.Value)) return w;
                        nullwert = true;
                        return null;
                    }
                }
            return null;
        }

        // ==================================================================
        //  Schichtfolge (IfcMaterialLayerSetUsage und die Raumseite)
        // ==================================================================

        /// <summary>Kleinster lotrechter Anteil der z-Achse einer Platte, ab dem „oben" feststeht.</summary>
        internal const double LOTRECHT_MIN = 0.5;

        /// <summary>
        /// <b>Welche Schicht liegt raumseitig?</b> Die Spezifikation legt das nicht im Schichtsatz fest,
        /// sondern in seiner Nutzung (<c>IfcMaterialLayerSetUsage</c>): Die Schichten werden ab der
        /// Bezugslinie in Richtung <c>DirectionSense</c> längs der Achse <c>LayerSetDirection</c>
        /// geschichtet — bei der Wand die lokale y-Achse (<c>AXIS2</c>), bei Platte und Dach die lokale
        /// z-Achse (<c>AXIS3</c>). Die erste Schicht liegt also auf der Seite GEGEN die
        /// Schichtungsrichtung. Wo außen ist, sagt bei der Wand die Raumgrenze (dieselbe Regel wie beim
        /// Azimut; bei einer Wand gegen einen unbeheizten Raum die Seite des beheizten), bei der Platte
        /// ihre Art: Dach und Decke über beheiztem Raum außen oben, Bodenplatte und Boden über
        /// unbeheiztem Raum außen unten.
        ///
        /// <para>Ohne Nutzung (Schichtsatz nur am Typ), ohne bestimmbare Seite oder bei einer anderen
        /// Achse bleibt die Annahme „erste Schicht außen" stehen und wird für jedes vollständige
        /// Hüllbauteil benannt (<c>IMP_IFC_PROT_SCHICHTFOLGE_ANGENOMMEN</c>) — sie entscheidet, welche
        /// Schichten als raumseitige Speichermasse zählen.</para>
        /// </summary>
        private void Schichtfolge(IIfcElement e, AbbildBauteil b, IIfcMaterialLayerSetUsage nutzung,
                                  List<IIfcRelSpaceBoundary> grenzen, int gi)
        {
            int? aussen = AussenLaengsSchichtachse(e, b, nutzung, grenzen, gi);
            if (!aussen.HasValue) return;
            int stapel = nutzung.DirectionSense == IfcDirectionSenseEnum.NEGATIVE ? -1 : +1;
            b.Aufbau.Richtung = Schichtfolge(stapel, aussen.Value);
            b.Aufbau.RichtungAngenommen = false;
        }

        /// <summary>
        /// Die Zählrichtung der Schichten aus der Schichtungsrichtung (+1 = längs der Achse, −1 = gegen sie)
        /// und der Lage der Außenseite auf derselben Achse (+1 / −1): Zeigen beide in dieselbe Richtung,
        /// liegt die erste Schicht innen.
        /// </summary>
        internal static Schichtrichtung Schichtfolge(int schichtungsrichtung, int aussenseite)
            => schichtungsrichtung * aussenseite > 0 ? Schichtrichtung.InnenNachAussen : Schichtrichtung.AussenNachInnen;

        /// <summary>Die Außenseite längs der Schichtachse: +1, −1, oder <c>null</c> = nicht bestimmbar.</summary>
        private int? AussenLaengsSchichtachse(IIfcElement e, AbbildBauteil b, IIfcMaterialLayerSetUsage nutzung,
                                              List<IIfcRelSpaceBoundary> grenzen, int gi)
        {
            if (nutzung == null) return null;
            IfcRahmen? rahmen = IfcPlatzierung.Weltrahmen(e.ObjectPlacement, _wurzel, out _);
            if (!rahmen.HasValue) return null;

            if (nutzung.LayerSetDirection == IfcLayerSetDirectionEnum.AXIS2)
            {
                if (!(b.NeigungGrad == 90.0)) return null;
                List<int> raeume = Raeume(grenzen, gi);
                bool aussenbauteil = b.Randbedingung == Randbedingung.Aussenluft || b.Randbedingung == Randbedingung.Erdreich;
                IEnumerable<int> innen = raeume;
                if (!aussenbauteil)
                {
                    // Zwischen beheiztem und unbeheiztem Raum: innen ist die Seite des beheizten.
                    if (!raeume.Any(r => _raum[r].Beheizt) || !raeume.Any(r => !_raum[r].Beheizt)) return null;
                    innen = raeume.Where(r => _raum[r].Beheizt);
                }
                List<double[]> punkte = innen.Where(r => _raumPunkt.ContainsKey(r)).Select(r => _raumPunkt[r]).ToList();
                return IfcPlatzierung.Aussenseite(rahmen.Value, punkte);
            }

            if (nutzung.LayerSetDirection == IfcLayerSetDirectionEnum.AXIS3)
            {
                bool? obenAussen = ObenAussen(b);
                double lotrecht = rahmen.Value.Z[2];
                if (!obenAussen.HasValue || Math.Abs(lotrecht) < LOTRECHT_MIN) return null;
                return (obenAussen.Value ? 1 : -1) * (lotrecht > 0.0 ? 1 : -1);
            }
            return null;
        }

        /// <summary>
        /// Liegt die Außenseite einer Platte oben? Dach und Decke über Außenluft ja, Bodenplatte nein;
        /// eine Decke zwischen beheiztem und unbeheiztem Raum nach der Sicht des beheizten (der Leser
        /// setzt sie aus den Geschosslagen, <see cref="Nachbarn"/>); sonst <c>null</c>.
        /// </summary>
        private bool? ObenAussen(AbbildBauteil b)
        {
            if (b.Art == Bauteilart.Dach) return true;
            if (b.Art == Bauteilart.Bodenplatte) return false;
            if (b.Art != Bauteilart.Decke) return null;
            if (b.Randbedingung == Randbedingung.Aussenluft) return true;
            if (b.Nachbarn.Count == 2)
            {
                bool? boden = GebaeudeAggregation.SichtIstBoden(b.Nachbarn[0].Sicht);
                if (boden.HasValue) return !boden.Value;   // der Boden des beheizten Raums hat außen unten
            }
            return null;
        }

        // ==================================================================
        //  Abschluss: Sammelmeldungen
        // ==================================================================

        private void Abschluss()
        {
            if (_ohneMengen.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "KEINE_MENGEN", Ganz(_ohneMengen.Count), Beispiele(_ohneMengen)));
            foreach (KeyValuePair<string, int> art in _platzierungsart)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "PLATZIERUNGSART", Ganz(art.Value), art.Key));
            foreach (KeyValuePair<string, int> art in _koerperNichtLesbar)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_ART", Ganz(art.Value), art.Key));
            foreach (KeyValuePair<string, int> art in _bauteilkoerperNichtLesbar)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUTEILKOERPER_ART", Ganz(art.Value), art.Key));
            foreach (KeyValuePair<int, (int Raum, int Bauteile, int Dreiecke, int Ausgelassen)> bk in _bauteilkoerper.OrderBy(x => x.Key))
            {
                AbbildGebaeude g = _abbild.Gebaeude[bk.Key];
                if (bk.Value.Bauteile > 0)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUTEILKOERPER_GELESEN", Ganz(bk.Value.Bauteile), Ganz(bk.Value.Dreiecke)));
                if (bk.Value.Ausgelassen > 0)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUTEILKOERPER_GRENZE", Ganz(bk.Value.Ausgelassen),
                        Ganz(Dateikoerper.DREIECKSGRENZE), Ganz(bk.Value.Raum)));
            }
            for (int gi = 0; gi < _abbild.Gebaeude.Count; gi++)
            {
                if (!_gebaeudeMitDarstellung.Contains(gi)) continue;
                AbbildGebaeude g = _abbild.Gebaeude[gi];
                int mit = g.Raeume.Count(x => x.Koerper != null);
                _abbild.Gebaeude[gi].Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "KOERPER_GELESEN", Ganz(mit),
                    Ganz(g.Raeume.Count - mit), Ganz(g.Raeume.Sum(x => x.Koerper?.DreieckZahl ?? 0))));
            }
            if (_seiteUnbestimmt.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "SEITE_UNBESTIMMT", Ganz(_seiteUnbestimmt.Count), Beispiele(_seiteUnbestimmt)));
            if (_ohneGebaeude.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "OHNE_GEBAEUDE", Ganz(_ohneGebaeude.Count), Beispiele(_ohneGebaeude)));

            Mehrschalig();
            KeinUWert();
            Schichtmeldungen();

            GrenzenOhneBauteilMelden();
            foreach (KeyValuePair<string, int> art in _flaecheUnbekannt)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "FLAECHE_UNBEKANNT", Ganz(art.Value), art.Key));

            int zonen = _modell.Instances.OfType<IIfcZone>().Count();
            if (zonen > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "ZONEN_UEBERGANGEN", Ganz(zonen)));

            int bauteile = _abbild.Gebaeude.Sum(g => g.Bauteile.Count + g.Bauteile.Sum(b => b.Oeffnungen.Count))
                           + _abbild.BauteileOhneGebaeude.Count;
            _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "GELESEN", Ganz(_abbild.Gebaeude.Count),
                Ganz(_raum.Count), Ganz(bauteile)));
        }

        /// <summary>
        /// Mehrschalige Wände (3.5 Nr. 9): Außenwände desselben Gebäudes mit denselben Räumen und derselben
        /// Richtung, deren Achsen um 1 cm bis 0,6 m auseinanderliegen, sind zwei Schalen EINER Wand. Ohne
        /// Schichtauswertung werden sie nicht zusammengefasst, sondern gemeldet — die Außenwandfläche kann
        /// zu groß sein. Abschnitte derselben Flucht (gleiche Achslage) sind keine Schalen.
        /// </summary>
        private void Mehrschalig()
        {
            int zahl = 0;
            foreach (var gruppe in _schalen.GroupBy(s => s.Gebaeude + "|" + s.Raeume + "|"
                                                          + Math.Round(s.Azimut).ToString(CultureInfo.InvariantCulture)))
            {
                List<Schale> liste = gruppe.ToList();
                var betroffen = new HashSet<Schale>();
                for (int i = 0; i < liste.Count; i++)
                    for (int j = i + 1; j < liste.Count; j++)
                    {
                        double d = Math.Abs(liste[i].AbstandM - liste[j].AbstandM);
                        if (d >= SCHALENABSTAND_MIN_M && d <= SCHALENABSTAND_MAX_M) { betroffen.Add(liste[i]); betroffen.Add(liste[j]); }
                    }
                zahl += betroffen.Count;
            }
            if (zahl > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "MEHRSCHALIG", Ganz(zahl)));
        }

        /// <summary>
        /// Je Gruppe der Hülle ein Hinweis, wenn für mehr als 30 % der Fläche der U-Wert fehlt — die
        /// gemeinsame Zuordnung nimmt dann die Vorgabe der Klasse (E2). Gezählt werden die Bauteile mit
        /// einem beheizten Nachbarraum oder ohne Raumbezug an der Außenluft.
        /// </summary>
        private void KeinUWert()
        {
            var summe = new SortedDictionary<string, double[]>(StringComparer.Ordinal);   // Zielfeld → {Fläche, ohne U}
            foreach (AbbildGebaeude g in _abbild.Gebaeude)
            {
                var beheizt = new HashSet<string>(g.Raeume.Where(r => r.Beheizt).Select(r => r.Kennung), StringComparer.Ordinal);
                foreach (AbbildBauteil b in g.Bauteile)
                {
                    bool aussen = b.Randbedingung == Randbedingung.Aussenluft || b.Randbedingung == Randbedingung.Erdreich;
                    if (!aussen || !(b.HuelleOhneNachbar || b.Nachbarn.Any(n => beheizt.Contains(n.Kennung)))) continue;
                    Addieren(summe, UFeld(b), b);
                    foreach (AbbildBauteil o in b.Oeffnungen)
                        Addieren(summe, o.Art == Bauteilart.Fenster ? GebaeudeZielfelder.U_FENSTER : GebaeudeZielfelder.U_SONSTIGE, o);
                }
            }
            foreach (KeyValuePair<string, double[]> e in summe)
                if (e.Value[0] > 0.0 && e.Value[1] > GebaeudeAggregation.ANTEIL_OHNE_U_GRENZE * e.Value[0])
                    _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "KEIN_UWERT", e.Key,
                        Zahl(Math.Round(100.0 * e.Value[1] / e.Value[0], 1))));
        }

        /// <summary>
        /// Die Sammelmeldungen der Schichten: Baustoffe mit Stoffwert ≤ 0, nicht gelesene Stoffwerte
        /// (IFC2X3) und vollständige Aufbauten von Hüllbauteilen, deren Schichtfolge nur angenommen ist.
        /// </summary>
        private void Schichtmeldungen()
        {
            if (_stoffwertNull.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "STOFFWERT_NULL",
                    Ganz(_stoffwertNull.Count), Beispiele(_stoffwertNull.ToList())));
            if (_stoffwerteNichtGelesen.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "STOFFWERTE_NICHT_GELESEN",
                    _abbild.Schemastand ?? _abbild.SchemaStand.ToString(), Ganz(_stoffwerteNichtGelesen.Count),
                    Beispiele(_stoffwerteNichtGelesen.ToList())));
            foreach ((string aufbau, List<string> schichten) in _cpRueckfallAufbauten)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "WAERMEKAPAZITAET_RUECKFALL",
                    aufbau, Ganz(schichten.Count), string.Join(", ", schichten)));

            var angenommen = new List<string>();
            foreach (AbbildGebaeude g in _abbild.Gebaeude)
            {
                var beheizt = new HashSet<string>(g.Raeume.Where(r => r.Beheizt).Select(r => r.Kennung), StringComparer.Ordinal);
                foreach (AbbildBauteil b in g.Bauteile)
                {
                    if (b.Aufbau == null || b.Aufbau.Status != Aufbaustatus.Vollstaendig || !b.Aufbau.RichtungAngenommen) continue;
                    bool aussen = b.Randbedingung == Randbedingung.Aussenluft || b.Randbedingung == Randbedingung.Erdreich;
                    int zahlBeheizt = b.Nachbarn.Count(n => beheizt.Contains(n.Kennung));
                    bool huelle = aussen ? b.HuelleOhneNachbar || zahlBeheizt > 0 : zahlBeheizt == 1;
                    if (huelle) angenommen.Add(b.Kennung);
                }
            }
            if (angenommen.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "SCHICHTFOLGE_ANGENOMMEN",
                    Ganz(angenommen.Count), Beispiele(angenommen)));
        }

        private static string UFeld(AbbildBauteil b)
        {
            if (b.Randbedingung == Randbedingung.Erdreich) return GebaeudeZielfelder.U_GRUND;
            switch (b.Art)
            {
                case Bauteilart.Aussenwand: return GebaeudeZielfelder.U_AUSSENWAND;
                case Bauteilart.Dach: return GebaeudeZielfelder.U_DACH;
                default: return GebaeudeZielfelder.U_SONSTIGE;
            }
        }

        private static void Addieren(SortedDictionary<string, double[]> summe, string feld, AbbildBauteil b)
        {
            if (!(b.BruttoflaecheM2 > 0.0)) return;
            if (!summe.TryGetValue(feld, out double[] w)) summe[feld] = w = new double[2];
            w[0] += b.BruttoflaecheM2.Value;
            // Ohne U-Wert zählt auch ein Aufbau, aus dem keiner zu rechnen ist (unvollständig, IFC2X3).
            bool ausSchichten = b.Aufbau != null
                                && (b.Aufbau.Status == Aufbaustatus.Vollstaendig || b.Aufbau.Status == Aufbaustatus.Masselos);
            if (!b.UWertWm2K.HasValue && !ausSchichten) w[1] += b.BruttoflaecheM2.Value;
        }

        // ==================================================================
        //  Werte
        // ==================================================================

        /// <summary>
        /// Der Zahlenwert einer Eigenschaft: Einzelwert über <c>NominalValue</c>, Bereich über
        /// <c>SetPointValue</c>, sonst die Grenzen (obere zuerst, beim Sollwert die untere); jede andere
        /// Eigenschaftsart wird benannt übergangen (<c>IMP_IFC_PROT_EIGENSCHAFTSART</c>, einmal je Art und Name).
        /// </summary>
        private double? Zahl(IfcFund f, bool untereGrenzeZuerst = false)
        {
            switch (f.Eigenschaft)
            {
                case IIfcPropertySingleValue einzel:
                    return IfcEigenschaften.Zahl(einzel.NominalValue);
                case IIfcPropertyBoundedValue bereich:
                {
                    double? soll = IfcEigenschaften.Zahl(bereich.SetPointValue);
                    if (soll.HasValue) return soll;
                    double? oben = IfcEigenschaften.Zahl(bereich.UpperBoundValue);
                    double? unten = IfcEigenschaften.Zahl(bereich.LowerBoundValue);
                    return untereGrenzeZuerst ? unten ?? oben : oben ?? unten;
                }
                default:
                    Eigenschaftsart(f);
                    return null;
            }
        }

        private bool? Wahrheit(IfcFund f)
        {
            if (f == null) return null;
            if (f.Eigenschaft is IIfcPropertySingleValue einzel) return IfcEigenschaften.Wahrheit(einzel.NominalValue);
            Eigenschaftsart(f);
            return null;
        }

        private string Textwert(IfcFund f)
        {
            if (f.Eigenschaft is IIfcPropertySingleValue einzel) return IfcEigenschaften.Textwert(einzel.NominalValue);
            Eigenschaftsart(f);
            return null;
        }

        private void Eigenschaftsart(IfcFund f)
        {
            string art = f.Eigenschaft?.ExpressType?.ExpressName ?? f.Eigenschaft?.GetType().Name ?? "";
            string name = f.Satz + "." + f.Eigenschaft?.Name.ToString();
            if (_gemeldeteArten.Add(art + "|" + name))
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "EIGENSCHAFTSART", art, name));
        }

        private static double? Positiv(double? w) => w.HasValue && w.Value > 0.0 && !double.IsInfinity(w.Value) ? w : null;

        private static double? NichtNegativ(double? w) => w.HasValue && w.Value >= 0.0 && !double.IsInfinity(w.Value) ? w : null;

        private static double? Produkt(double? a, double? b) => a > 0.0 && b > 0.0 ? a.Value * b.Value : (double?)null;

        private static string Beispiele(List<string> kennungen)
            => kennungen.Count <= BEISPIELE ? string.Join(", ", kennungen) : string.Join(", ", kennungen.Take(BEISPIELE)) + ", …";

        private static string Zahl(double w) => w.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
