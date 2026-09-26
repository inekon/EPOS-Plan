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

        private readonly List<string> _ohneMengen = new List<string>();
        private readonly List<string> _seiteUnbestimmt = new List<string>();
        private readonly List<string> _ohneGebaeude = new List<string>();
        private readonly SortedDictionary<string, int> _platzierungsart = new SortedDictionary<string, int>(StringComparer.Ordinal);
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
            Kontext(projekt);

            List<IIfcBuilding> gebaeude = Sortiert<IIfcBuilding>().ToList();
            if (gebaeude.Count == 0)
            {
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "KEIN_GEBAEUDE"));
                return;
            }
            for (int i = 0; i < gebaeude.Count; i++) Gebaeude(gebaeude[i], i);
            Melden(0.1);

            Raumgrenzen();
            Untergeschosse();
            Zonen();
            for (int i = 0; i < gebaeude.Count; i++) Flaechenart(i);
            Melden(0.2);

            Bauteile();
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
            Struktur(b, index, null, besucht);

            List<AbbildRaum> raeume = g.Raeume;
            g.ZahlGeschosseMitRaeumen = raeume.Select(r => r.GeschossKennung).Where(s => s != null).Distinct(StringComparer.Ordinal).Count();
            if (g.ZahlGeschosseMitRaeumen > 1) g.Zonenvorschlag = IfcImportProfil.ZONENREGEL_Z4;
        }

        private void Baujahr(IIfcBuilding b, AbbildGebaeude g)
        {
            IfcFund f = IfcEigenschaften.Finden(_bezuege, b, "Pset_BuildingCommon", "YearOfConstruction");
            string text = f == null ? null : Textwert(f);
            if (string.IsNullOrWhiteSpace(text)) return;
            g.BaujahrText = text.Trim();
            g.Baujahr = Baujahrregel.Jahr(text);
            if (g.Baujahr.HasValue)
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUJAHR_TEXT", g.BaujahrText,
                    g.Baujahr.Value.ToString(CultureInfo.InvariantCulture)));
            else
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BAUJAHR_UNLESBAR", g.BaujahrText));
        }

        /// <summary>
        /// Läuft die räumliche Struktur eines Gebäudes ab: Zerlegung (<c>IsDecomposedBy</c>) und Enthaltensein
        /// (<c>ContainsElements</c>). Ein eingeschachteltes <c>IfcBuilding</c> ist ein eigenes Gebäude und wird
        /// hier nicht betreten; Zonen hängen nicht an der Zerlegung und bleiben ohnehin draußen.
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

            if (knoten is IIfcSpatialElement raeumlich)
                foreach (IIfcRelContainedInSpatialStructure rel in _bezuege.Enthaelt(raeumlich))
                    foreach (IIfcElement e in rel.RelatedElements.OfType<IIfcElement>().OrderBy(x => x.EntityLabel))
                        ElementZuordnen(e, gi, geschoss, besucht);

            foreach (IIfcRelAggregates rel in _bezuege.ZerlegtDurch(knoten))
                foreach (IIfcObjectDefinition kind in rel.RelatedObjects.OrderBy(x => x.EntityLabel))
                {
                    if (kind is IIfcBuilding) continue;
                    if (kind is IIfcSpatialElement) Struktur(kind, gi, geschoss, besucht);
                    else if (kind is IIfcElement e) ElementZuordnen(e, gi, geschoss, besucht);
                }
        }

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
            _raumflaechen[s.EntityLabel] = new[]
            {
                IfcEigenschaften.Menge(_bezuege, s, "Space", "NetFloorArea", _einheiten),
                IfcEigenschaften.Menge(_bezuege, s, "Space", "GrossFloorArea", _einheiten),
            };
            r.HoeheM = Positiv(IfcEigenschaften.Menge(_bezuege, s, "Space", "Height", _einheiten));
            r.VolumenM3 = Positiv(IfcEigenschaften.Menge(_bezuege, s, "Space", "NetVolume", _einheiten)
                                  ?? IfcEigenschaften.Menge(_bezuege, s, "Space", "GrossVolume", _einheiten));

            r.SollHeizenC = Sollwert(s);
            Beheizung(s, r, langname, name, g);
            r.Klassifikation = Klassifikation(s);

            g.Raeume.Add(r);
            _raum[s.EntityLabel] = r;
            _raumGebaeude[s.EntityLabel] = gi;
            _raumLage[s.EntityLabel] = Lage(geschoss);
            IfcRahmen? rahmen = IfcPlatzierung.Weltrahmen(s.ObjectPlacement, _wurzel, out _);
            if (rahmen.HasValue)
            {
                _raumPunkt[s.EntityLabel] = rahmen.Value.Ursprung;
                _raumRahmen[s.EntityLabel] = rahmen.Value;
            }
        }

        /// <summary>Die Temperatur [°C], oberhalb derer ein Raum nach Regel B3 beheizt ist.</summary>
        internal const double B3_GRENZE_C = 12.0;

        /// <summary>
        /// <b>Beheizt oder unbeheizt — die Regeln B1 bis B4 und B6</b> (Mehrzonenkonzept 6.1; B5 folgt
        /// nach den Raumgrenzen, <see cref="Untergeschosse"/>): B1 <c>PredefinedType = EXTERNAL</c>, B2
        /// <c>Pset_SpaceCommon.IsExternal = TRUE</c>, B3 der Heizsollwert aus
        /// <c>Pset_SpaceThermalRequirements</c> — nur mit auflösbarer Temperatureinheit — über 12 °C
        /// beheizt, sonst unbeheizt, B4 die Namensregel, B6 sonst beheizt. Die erste Regel, die trägt,
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
            if (aussen == true)
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
            string treffer = Raumnamenregel.Treffer(langname) ?? Raumnamenregel.Treffer(name);
            if (treffer != null)
            {
                Setzen(r, false, BeheiztQuelle.Name, "B4", null);
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UNBEHEIZT_NAME", r.Kennung, r.Name ?? "", treffer));
                return;
            }
            Setzen(r, true, BeheiztQuelle.Annahme, "B6", null);
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
                kv.Value.FlaecheM2 = kv.Value.Beheizt ? Positiv(f[quelle]) : Positiv(f[0] ?? f[1]);
            }

            if (raeume.Count == 0 || raeume.All(kv => !(_raumflaechen[kv.Key][0] > 0.0) && !(_raumflaechen[kv.Key][1] > 0.0)))
                g.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "KEINE_RAEUME", g.Anzeigename, Ganz(raeume.Count)));
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

        /// <summary>Die Raumgrenzen eines Bauteils — die der 2. Ebene, wenn es welche gibt, sonst alle.</summary>
        private List<IIfcRelSpaceBoundary> GrenzenVon(IIfcElement e)
        {
            if (!_grenzen.TryGetValue(e.EntityLabel, out List<IIfcRelSpaceBoundary> alle)) return new List<IIfcRelSpaceBoundary>();
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
            foreach (IIfcRoof dach in Sortiert<IIfcRoof>())
            {
                List<IIfcElement> teile = Teile(dach);
                if (teile.Count == 0) continue;
                if (IfcEigenschaften.Menge(_bezuege, dach, "Roof", "GrossArea", _einheiten).HasValue)
                    foreach (IIfcElement t in teile) uebersprungen.Add(t.EntityLabel);
                else
                    uebersprungen.Add(dach.EntityLabel);
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
            Randbedingung rand = Rand(istAussen, grenzen, dach, bodenplatte);

            int gi = GebaeudeVon(e, grenzen);
            var b = new AbbildBauteil
            {
                Kennung = e.GlobalId.ToString(),
                Quelltyp = e.ExpressType.ExpressName,
                Name = IfcEigenschaften.Text(e.Name),
                Quellart = plattenart?.ToString(),
                Randbedingung = rand,
            };
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
            if (!b.BruttoflaecheM2.HasValue) _ohneMengen.Add(b.Kennung);

            UWert(e, satz, b);
            b.Aufbau = Aufbau(e, out IIfcMaterialLayerSetUsage nutzung);

            Nachbarn(b, rand, grenzen, gi, platte != null || e is IIfcRoof);
            b.HuelleOhneNachbar = b.Nachbarn.Count == 0 && (rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich);

            if (senkrecht) Azimut(e, b, grenzen, gi);
            if (b.Aufbau != null) Schichtfolge(e, b, nutzung, grenzen, gi);

            // Stufe G6c: die Raumgrenzen je Seite, Dicke und Geschoss — der Eingang der Zonierung.
            GrenzenUebernehmen(b, grenzen);
            b.DickeM = Positiv(IfcEigenschaften.Menge(_bezuege, e, klasse, "Width", _einheiten))
                       ?? Positiv(IfcEigenschaften.Menge(_bezuege, e, klasse, "Depth", _einheiten))
                       ?? (b.Aufbau != null && b.Aufbau.Schichten.Count > 0 && b.Aufbau.Schichten.All(x => x.DickeM > 0.0)
                           ? b.Aufbau.Schichten.Sum(x => x.DickeM.Value) : (double?)null);
            b.GeschossKennung = _elementGeschoss.TryGetValue(e.EntityLabel, out string geschoss) ? geschoss : null;

            foreach (IIfcRelVoidsElement rel in _bezuege.Oeffnungen(e))
            {
                if (!(rel.RelatedOpeningElement is IIfcOpeningElement oeffnung)) continue;
                foreach (IIfcRelFillsElement fuellung in _bezuege.Fuellungen(oeffnung))
                {
                    IIfcElement f = fuellung.RelatedBuildingElement;
                    if (f is IIfcWindow fenster) b.Oeffnungen.Add(Oeffnung(fenster, "Window", Bauteilart.Fenster, b));
                    else if (f is IIfcDoor tuer) b.Oeffnungen.Add(Oeffnung(tuer, "Door", Bauteilart.Tuer, b));
                    if (f != null) _gefuellt.Add(f.EntityLabel);
                }
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
        /// <c>EXTERNAL*</c> = außen, <c>INTERNAL</c> = innen), bei <c>NOTDEFINED</c> die Zählregel: außen, wenn
        /// genau eine physische Raumgrenze auf das Bauteil zeigt. Ein Dach ohne jede Angabe gilt als außen,
        /// eine Bodenplatte als erdberührt — beides ist ihre Definition.
        /// </summary>
        internal static Randbedingung Rand(bool? istAussen, IReadOnlyCollection<IIfcRelSpaceBoundary> grenzen, bool dach, bool bodenplatte)
        {
            bool erde = grenzen.Any(g => g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL_EARTH);
            if (istAussen == true) return erde || bodenplatte ? Randbedingung.Erdreich : Randbedingung.Aussenluft;
            if (istAussen == false) return Randbedingung.Innen;
            if (grenzen.Count > 0)
            {
                if (erde) return Randbedingung.Erdreich;
                if (grenzen.Any(g => g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL
                                     || g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL_WATER
                                     || g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.EXTERNAL_FIRE))
                    return Randbedingung.Aussenluft;
                if (grenzen.All(g => g.InternalOrExternalBoundary == IfcInternalOrExternalEnum.INTERNAL))
                    return Randbedingung.Innen;
                if (grenzen.Count(g => g.PhysicalOrVirtualBoundary == IfcPhysicalOrVirtualEnum.PHYSICAL) == 1)
                    return Randbedingung.Aussenluft;
                return Randbedingung.Innen;
            }
            if (dach) return Randbedingung.Aussenluft;
            if (bodenplatte) return Randbedingung.Erdreich;
            return Randbedingung.Unbekannt;
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
            if (!rahmen.HasValue) { _seiteUnbestimmt.Add(b.Kennung); return; }

            List<double[]> punkte = Raeume(grenzen, gi).Where(r => _raumPunkt.ContainsKey(r)).Select(r => _raumPunkt[r]).ToList();
            int? seite = IfcPlatzierung.Aussenseite(rahmen.Value, punkte);
            if (!seite.HasValue) { _seiteUnbestimmt.Add(b.Kennung); return; }

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

        private AbbildBauteil Oeffnung(IIfcElement o, string klasse, Bauteilart art, AbbildBauteil wirt)
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
            b.BruttoflaecheM2 = flaeche;
            if (!flaeche.HasValue) _ohneMengen.Add(b.Kennung);

            UWert(o, "Pset_" + klasse + "Common", b);
            IfcFund g = IfcEigenschaften.Finden(_bezuege, o, "Pset_DoorWindowGlazingType", "SolarHeatGainTransmittance");
            b.GWert = g == null ? null : Zahl(g);
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

        private void UWert(IIfcElement e, string satz, AbbildBauteil b)
        {
            IfcFund f = IfcEigenschaften.Finden(_bezuege, e, satz, "ThermalTransmittance");
            if (f == null) return;
            double? u = Zahl(f);
            if (!u.HasValue) return;
            _abbild.ZahlUWerte++;
            b.UWertWm2K = u;
            b.UWertQuelle = f.Satz + (f.Quelle == IfcEigenschaftsquelle.Typ ? " (Typ)" : "");
        }

        // ==================================================================
        //  Schichten (IfcMaterialLayerSet, Pset_MaterialThermal / Pset_MaterialCommon)
        // ==================================================================

        /// <summary>Der Eigenschaftssatz der thermischen Stoffwerte (λ, c) — IFC4/IFC4X3.</summary>
        internal const string PSET_STOFF_THERMISCH = "Pset_MaterialThermal";

        /// <summary>Der allgemeine Eigenschaftssatz des Baustoffs (ρ) — IFC4/IFC4X3.</summary>
        internal const string PSET_STOFF_ALLGEMEIN = "Pset_MaterialCommon";

        /// <summary>
        /// Der Aufbau eines Bauteils aus seinem <c>IfcMaterialLayerSet</c>: Dicke je Schicht und die
        /// Stoffwerte λ, ρ, c ihres Baustoffs (<see cref="Stoffwerte"/>). Die Schichtfolge steht, wie die
        /// Datei sie zählt; ob die erste Schicht außen oder innen liegt, entscheidet erst
        /// <see cref="Schichtfolge(IIfcElement, AbbildBauteil, IIfcMaterialLayerSetUsage, List{IIfcRelSpaceBoundary}, int)"/>
        /// — bis dahin gilt die Annahme „erste Schicht außen".
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
            foreach (IIfcMaterialLayer schicht in satz.MaterialLayers)
            {
                IIfcMaterial stoff = schicht?.Material;
                double dicke = IfcEigenschaften.Wert(schicht?.LayerThickness);
                (double? lambda, double? rho, double? cp) = Stoffwerte(stoff);
                a.Schichten.Add(new AbbildSchicht
                {
                    BaustoffKennung = stoff?.Name.ToString() ?? "",
                    Name = stoff?.Name.ToString(),
                    DickeM = dicke > 0.0 ? dicke * _einheiten.Laenge : (double?)null,
                    LambdaWmK = lambda,
                    RhoKgM3 = rho,
                    CpJkgK = cp,
                });
            }
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
            if (_seiteUnbestimmt.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "SEITE_UNBESTIMMT", Ganz(_seiteUnbestimmt.Count), Beispiele(_seiteUnbestimmt)));
            if (_ohneGebaeude.Count > 0)
                _abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "OHNE_GEBAEUDE", Ganz(_ohneGebaeude.Count), Beispiele(_ohneGebaeude)));

            Mehrschalig();
            KeinUWert();
            Schichtmeldungen();

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
