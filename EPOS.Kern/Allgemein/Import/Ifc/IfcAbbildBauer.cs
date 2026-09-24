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
    /// <c>Xbim.Ifc4.Interfaces.IIfc*</c>, ein Weg für IFC2X3, IFC4 und IFC4X3. Verzweigt wird nur an
    /// den zwei benannten Stellen (3.5 Nr. 7): der Erkennung der Raumgrenzen 2. Ebene
    /// (<see cref="IstZweiteEbene"/>) und dem in IFC4X3 entfallenen <c>Pset_SpaceThermalRequirements</c>
    /// (<see cref="Sollwert"/>).
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
        private readonly Dictionary<int, List<IIfcRelSpaceBoundary>> _grenzen = new Dictionary<int, List<IIfcRelSpaceBoundary>>();
        private readonly HashSet<int> _grenzenZweiteEbene = new HashSet<int>();
        private readonly HashSet<string> _gemeldeteArten = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> _gefuellt = new HashSet<int>();

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
            IfcFund f = IfcEigenschaften.Finden(b, "Pset_BuildingCommon", "YearOfConstruction");
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
            if (knoten is IIfcBuildingStorey s) geschoss = s;
            if (knoten is IIfcSpace raum) RaumAnlegen(raum, gi, geschoss);

            if (knoten is IIfcSpatialElement raeumlich && raeumlich.ContainsElements != null)
                foreach (IIfcRelContainedInSpatialStructure rel in raeumlich.ContainsElements)
                    foreach (IIfcElement e in rel.RelatedElements.OfType<IIfcElement>().OrderBy(x => x.EntityLabel))
                        ElementZuordnen(e, gi, geschoss, besucht);

            if (knoten.IsDecomposedBy == null) return;
            foreach (IIfcRelAggregates rel in knoten.IsDecomposedBy)
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
            }
            if (e.IsDecomposedBy == null || !besucht.Add(-e.EntityLabel)) return;
            foreach (IIfcRelAggregates rel in e.IsDecomposedBy)
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
                IfcEigenschaften.Menge(s, "Space", "NetFloorArea", _einheiten),
                IfcEigenschaften.Menge(s, "Space", "GrossFloorArea", _einheiten),
            };
            r.HoeheM = Positiv(IfcEigenschaften.Menge(s, "Space", "Height", _einheiten));
            r.VolumenM3 = Positiv(IfcEigenschaften.Menge(s, "Space", "NetVolume", _einheiten)
                                  ?? IfcEigenschaften.Menge(s, "Space", "GrossVolume", _einheiten));

            // Beheizt (3.5 Nr. 3): IsExternal = true schließt aus, sonst die Namensregel.
            bool? aussen = Wahrheit(IfcEigenschaften.Finden(s, "Pset_SpaceCommon", "IsExternal"));
            if (aussen == true)
            {
                r.Beheizt = false;
                r.BeheiztQuelle = BeheiztQuelle.Attribut;
                r.Zustandsangabe = "IsExternal";
            }
            else
            {
                string treffer = Raumnamenregel.Treffer(langname) ?? Raumnamenregel.Treffer(name);
                r.Beheizt = treffer == null;
                r.BeheiztQuelle = treffer == null ? BeheiztQuelle.Annahme : BeheiztQuelle.Name;
                if (treffer != null)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UNBEHEIZT_NAME", r.Kennung, r.Name ?? "", treffer));
            }

            r.SollHeizenC = Sollwert(s);

            g.Raeume.Add(r);
            _raum[s.EntityLabel] = r;
            _raumGebaeude[s.EntityLabel] = gi;
            _raumLage[s.EntityLabel] = Lage(geschoss);
            IfcRahmen? rahmen = IfcPlatzierung.Weltrahmen(s.ObjectPlacement, _wurzel, out _);
            if (rahmen.HasValue) _raumPunkt[s.EntityLabel] = rahmen.Value.Ursprung;
        }

        /// <summary>
        /// Der Heizsollwert eines Raums aus <c>Pset_SpaceThermalRequirements</c> — Verzweigung 2 (3.5 Nr. 7):
        /// In IFC4X3 ist der Satz entfallen und wird nicht gelesen. Gelesen wird die untere Wintergrenze,
        /// sonst die untere Grenze, sonst der Sollwert des Bands.
        /// </summary>
        private double? Sollwert(IIfcSpace s)
        {
            if (_abbild.SchemaStand == IfcSchemaStand.Ifc4x3) return null;
            bool vorhanden = IfcEigenschaften.HatSatz(s, PSET_SOLLWERTE);
            if (vorhanden) _abbild.ZahlSollwertsaetze++;
            foreach (string name in new[] { "SpaceTemperatureWinter", "SpaceTemperatureWinterMin", "SpaceTemperatureMin", "SpaceTemperature" })
            {
                IfcFund f = IfcEigenschaften.Finden(s, PSET_SOLLWERTE, name);
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
                if (IfcEigenschaften.Menge(dach, "Roof", "GrossArea", _einheiten).HasValue)
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

        private static List<IIfcElement> Teile(IIfcElement e)
            => e.IsDecomposedBy == null ? new List<IIfcElement>()
               : e.IsDecomposedBy.SelectMany(r => r.RelatedObjects.OfType<IIfcElement>()).ToList();

        private void Bauteil(IIfcElement e, string klasse)
        {
            string satz = "Pset_" + klasse + "Common";
            IIfcSlab platte = e as IIfcSlab;
            IfcSlabTypeEnum? plattenart = platte?.PredefinedType;
            bool dach = e is IIfcRoof || plattenart == IfcSlabTypeEnum.ROOF;
            bool bodenplatte = plattenart == IfcSlabTypeEnum.BASESLAB;
            bool senkrecht = e is IIfcWall || e is IIfcCurtainWall || e is IIfcPlate;

            // Plattenwerk außen (IfcPlate) nur, wenn es als außen erklärt ist.
            bool? istAussen = Wahrheit(IfcEigenschaften.Finden(e, satz, "IsExternal"));
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
                b.BruttoflaecheM2 = Positiv(IfcEigenschaften.Menge(e, klasse, "GrossSideArea", _einheiten))
                                    ?? Produkt(IfcEigenschaften.Menge(e, klasse, "Length", _einheiten),
                                               IfcEigenschaften.Menge(e, klasse, "Height", _einheiten));
                b.NettoflaecheM2 = NichtNegativ(IfcEigenschaften.Menge(e, klasse, "NetSideArea", _einheiten));
            }
            else
            {
                b.BruttoflaecheM2 = Positiv(IfcEigenschaften.Menge(e, klasse, "GrossArea", _einheiten));
                b.NettoflaecheM2 = NichtNegativ(IfcEigenschaften.Menge(e, klasse, "NetArea", _einheiten));
            }
            if (!b.BruttoflaecheM2.HasValue) _ohneMengen.Add(b.Kennung);

            UWert(e, satz, b);
            b.Aufbau = Aufbau(e);

            Nachbarn(b, rand, grenzen, gi, platte != null || e is IIfcRoof);
            b.HuelleOhneNachbar = b.Nachbarn.Count == 0 && (rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich);

            if (senkrecht) Azimut(e, b, grenzen, gi);

            foreach (IIfcRelVoidsElement rel in e.HasOpenings ?? Enumerable.Empty<IIfcRelVoidsElement>())
            {
                if (!(rel.RelatedOpeningElement is IIfcOpeningElement oeffnung) || oeffnung.HasFillings == null) continue;
                foreach (IIfcRelFillsElement fuellung in oeffnung.HasFillings)
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
            double? flaeche = Positiv(IfcEigenschaften.Menge(o, klasse, "Area", _einheiten))
                              ?? Produkt(IfcEigenschaften.Menge(o, klasse, "Width", _einheiten),
                                         IfcEigenschaften.Menge(o, klasse, "Height", _einheiten));
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
            IfcFund g = IfcEigenschaften.Finden(o, "Pset_DoorWindowGlazingType", "SolarHeatGainTransmittance");
            b.GWert = g == null ? null : Zahl(g);
            return b;
        }

        private void UWert(IIfcElement e, string satz, AbbildBauteil b)
        {
            IfcFund f = IfcEigenschaften.Finden(e, satz, "ThermalTransmittance");
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

        private AbbildAufbau Aufbau(IIfcElement e)
        {
            IIfcMaterialLayerSet satz = Schichtsatz(e);
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
                a.Schichten.Add(new AbbildSchicht
                {
                    BaustoffKennung = stoff?.Name.ToString() ?? "",
                    Name = stoff?.Name.ToString(),
                    DickeM = dicke > 0.0 ? dicke * _einheiten.Laenge : (double?)null,
                    LambdaWmK = Positiv(Stoffwert(stoff, "ThermalConductivity")),
                    RhoKgM3 = Positiv(Stoffwert(stoff, "MassDensity")),
                    CpJkgK = Positiv(Stoffwert(stoff, "SpecificHeatCapacity")),
                });
            }
            a.Status = a.Schichten.Count == 0 ? Aufbaustatus.OhneAufbau
                     : a.Schichten.All(s => s.Vollstaendig) ? Aufbaustatus.Vollstaendig
                     : a.Schichten.All(s => s.HatWiderstand) ? Aufbaustatus.Masselos
                     : Aufbaustatus.Unvollstaendig;
            return a;
        }

        /// <summary>Der Schichtsatz eines Bauteils: am Vorkommnis, sonst am Typ.</summary>
        private static IIfcMaterialLayerSet Schichtsatz(IIfcElement e)
        {
            IIfcMaterialLayerSet s = Schichtsatz(e.HasAssociations);
            if (s != null) return s;
            foreach (IIfcRelDefinesByType rel in e.IsTypedBy ?? Enumerable.Empty<IIfcRelDefinesByType>())
            {
                s = Schichtsatz(rel?.RelatingType?.HasAssociations);
                if (s != null) return s;
            }
            return null;
        }

        private static IIfcMaterialLayerSet Schichtsatz(IEnumerable<IIfcRelAssociates> zuordnungen)
        {
            if (zuordnungen == null) return null;
            foreach (IIfcRelAssociatesMaterial rel in zuordnungen.OfType<IIfcRelAssociatesMaterial>())
            {
                if (rel.RelatingMaterial is IIfcMaterialLayerSetUsage nutzung) return nutzung.ForLayerSet;
                if (rel.RelatingMaterial is IIfcMaterialLayerSet satz) return satz;
            }
            return null;
        }

        /// <summary>Ein Stoffwert aus einem beliebigen Eigenschaftssatz des Baustoffs; <c>null</c> = keiner.</summary>
        private static double? Stoffwert(IIfcMaterial stoff, string name)
        {
            if (stoff == null) return null;
            try
            {
                foreach (IIfcMaterialProperties satz in stoff.HasProperties ?? Enumerable.Empty<IIfcMaterialProperties>())
                    foreach (IIfcPropertySingleValue p in satz.Properties.OfType<IIfcPropertySingleValue>())
                        if (IfcEigenschaften.Gleich(p.Name.ToString(), name)) return IfcEigenschaften.Zahl(p.NominalValue);
            }
            catch (NotSupportedException)
            {
                // IFC2X3 führt Stoffwerte in eigenen Entitäten, die die IFC4-Schnittstelle nicht abbildet.
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
            if (!b.UWertWm2K.HasValue && b.Aufbau == null) w[1] += b.BruttoflaecheM2.Value;
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
