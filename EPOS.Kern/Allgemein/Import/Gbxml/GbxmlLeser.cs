using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der gbXML-Leser</b> (Stufe G4c; Datenaustauschkonzept Kapitel 3, ADR-004): LINQ to XML
    /// gegen ein handgeschriebenes Lesemodell, reine BCL — kein Paket, keine Codeerzeugung, keine
    /// Reflexion, iOS-fest.
    ///
    /// <para><b>Zwei Regeln tragen ihn</b> (3.1): Die Datei wird als STROM gelesen, nie als Text —
    /// UTF-16LE mit BOM muss gehen, die Kodierung erkennt der XML-Leser (Probe 5). Und es wird
    /// NICHT validiert: Keine der vier offiziellen Beispieldateien ist schemagültig; der Leser
    /// entscheidet je Feld, ob ein Wert brauchbar ist (Probe 4 hält das als Quelltextwache). Eine
    /// DTD ist verboten und es gibt keinen Auflöser — keine externe Entität, kein Netzzugriff.</para>
    ///
    /// <para><b>Zweistufig</b> (3.1): erst die Wurzelkataloge (<c>Construction</c>, <c>Layer</c>,
    /// <c>Material</c>, <c>WindowType</c>, <c>Zone</c>), dann <c>Campus/Building/Space</c> und
    /// <c>Campus/Surface</c> — die Flächen hängen am Campus, nicht am Raum, und die Zuordnung läuft
    /// allein über <c>AdjacentSpaceId/@spaceIdRef</c>. Jeder Verweis ins Leere ist eine Warnung, der
    /// Rest wird gelesen.</para>
    ///
    /// <para><b>Elemente werden über ihren lokalen Namen gefunden</b>, mit oder ohne den Namensraum
    /// <see cref="NAMENSRAUM"/> — Dateien ohne Namensraumangabe laufen damit ebenso.</para>
    ///
    /// <para><b>Annahmen, die benannt sind:</b> Der Azimut gilt wie in EPOS (0° = Nord, im
    /// Uhrzeigersinn) und <c>CADModelAzimuth</c> wird NICHT aufaddiert, sondern gemeldet (3.2,
    /// Probe 21); im Polygon ist +y Norden und +x Osten, die Normale zeigt nach der Rechte-Hand-Regel
    /// vom ersten Nachbarraum weg; die erste Schicht eines Aufbaus liegt außen (3.7).</para>
    /// </summary>
    internal sealed class GbxmlLeser : IGebaeudeLeser
    {
        /// <summary>Der Namensraum von gbXML.</summary>
        public const string NAMENSRAUM = "http://www.gbxml.org/schema";

        /// <summary>
        /// Die Werte von <c>versionEnum</c> (Befund R 1.1). Er endet bei 6.01 — <c>8.01</c> ist nach
        /// dem eigenen Schema ungültig (8.01 ist byteweise 7.04) und wird nur vermerkt, nicht
        /// abgelehnt: Der Versionswert steuert nichts am Lesen.
        /// </summary>
        public static readonly IReadOnlyList<string> BekannteVersionen = new[]
        {
            "0.35", "0.36", "0.37", "5.00", "5.01", "5.10", "5.11", "5.12", "6.00", "6.01",
        };

        private const string P = GbxmlImportProfil.MELDUNGSPRAEFIX;

        // Die Tabellen der Leserichtung (Flächenart, übergangene Flächenarten, Öffnungsart) stehen im
        // gemeinsamen Vokabular, damit Leser und Schreiber (G7a) dieselben Werte führen.
        private static IReadOnlyDictionary<string, (Bauteilart Art, Randbedingung Rand)> Flaechenarten => GbxmlVokabular.Flaechenarten;

        private static IReadOnlySet<string> Uebergangen => GbxmlVokabular.UebergangeneFlaechenarten;

        private static IReadOnlyDictionary<string, Bauteilart> Oeffnungsarten => GbxmlVokabular.Oeffnungsarten;

        /// <inheritdoc />
        public GebaeudeAbbild Lesen(Stream quelle, GebaeudeImportProfil profil,
                                    IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            var abbild = new GbxmlAbbild();
            abbruch.ThrowIfCancellationRequested();

            XDocument dokument;
            try
            {
                dokument = Laden(quelle);
            }
            catch (XmlException ex)
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "LESEFEHLER", ex.Message));
                return abbild;
            }

            new Lesung(abbild, melder, abbruch).Lesen(dokument);
            return abbild;
        }

        /// <summary>
        /// Lädt das Dokument aus dem STROM — Kodierung aus BOM und XML-Deklaration, DTD verboten,
        /// kein Auflöser, keine Schemaprüfung.
        /// </summary>
        internal static XDocument Laden(Stream quelle)
        {
            var einstellungen = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                CloseInput = false,
            };
            using (XmlReader leser = XmlReader.Create(quelle, einstellungen))
                return XDocument.Load(leser, LoadOptions.None);
        }

        // ==================================================================
        //  Ein Lesedurchgang — der Zustand eines Laufs
        // ==================================================================

        private sealed class Lesung
        {
            private readonly GbxmlAbbild _abbild;
            private readonly IProgress<ImportFortschritt> _melder;
            private readonly CancellationToken _abbruch;

            private readonly HashSet<string> _gemeldet = new HashSet<string>(StringComparer.Ordinal);
            private readonly Dictionary<string, XElement> _material = new Dictionary<string, XElement>(StringComparer.Ordinal);
            private readonly Dictionary<string, XElement> _schicht = new Dictionary<string, XElement>(StringComparer.Ordinal);
            private readonly Dictionary<string, XElement> _konstruktion = new Dictionary<string, XElement>(StringComparer.Ordinal);
            private readonly Dictionary<string, XElement> _fenstertyp = new Dictionary<string, XElement>(StringComparer.Ordinal);
            private readonly Dictionary<string, XElement> _zone = new Dictionary<string, XElement>(StringComparer.Ordinal);
            private readonly Dictionary<string, AbbildAufbau> _aufbauten = new Dictionary<string, AbbildAufbau>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _raumGebaeude = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly SortedDictionary<string, int> _uebergangen = new SortedDictionary<string, int>(StringComparer.Ordinal);
            private readonly List<string> _ohneAufbau = new List<string>();

            private string _laenge, _flaeche, _volumen, _temperatur;
            private bool _laengeGilt, _flaecheGilt, _volumenGilt, _temperaturGilt;

            internal Lesung(GbxmlAbbild abbild, IProgress<ImportFortschritt> melder, CancellationToken abbruch)
            {
                _abbild = abbild;
                _melder = melder;
                _abbruch = abbruch;
            }

            internal void Lesen(XDocument dokument)
            {
                XElement wurzel = dokument.Root;
                if (wurzel == null || wurzel.Name.LocalName != "gbXML")
                {
                    Datei(PruefStufe.Fehler, "KEIN_CAMPUS", wurzel?.Name.LocalName ?? "");
                    return;
                }

                Version(wurzel);
                Einheiten(wurzel);
                Kataloge(wurzel);

                List<XElement> campusse = Kinder(wurzel, "Campus").ToList();
                if (!campusse.SelectMany(c => Kinder(c, "Building")).Any())
                {
                    Datei(PruefStufe.Fehler, "KEIN_CAMPUS", wurzel.Name.LocalName);
                    return;
                }
                if (_konstruktion.Count == 0) Datei(PruefStufe.Warnung, "KEINE_KONSTRUKTIONEN");

                Ort(campusse);
                foreach (XElement campus in campusse)
                    foreach (XElement building in Kinder(campus, "Building"))
                        Gebaeude(building);

                List<XElement> flaechen = campusse.SelectMany(c => Kinder(c, "Surface")).ToList();
                for (int i = 0; i < flaechen.Count; i++)
                {
                    _abbruch.ThrowIfCancellationRequested();
                    if (_melder != null && i % 100 == 0)
                        _melder.Report(new ImportFortschritt(flaechen.Count == 0 ? 1.0 : (double)i / flaechen.Count,
                            GebaeudeImportAblauf.MELDUNG + "FLAECHEN", Ganz(i), Ganz(flaechen.Count)));
                    Flaeche(flaechen[i]);
                }

                if (_ohneAufbau.Count > 0)
                    Datei(PruefStufe.Warnung, "OHNE_AUFBAU", Ganz(_ohneAufbau.Count), Beispiele(_ohneAufbau));
                foreach (KeyValuePair<string, int> u in _uebergangen)
                    Datei(PruefStufe.Info, "UEBERGANGEN", u.Key, Ganz(u.Value));
            }

            // --------------------------------------------------------------
            //  Wurzel: Version, Einheiten, Kataloge, Ort
            // --------------------------------------------------------------

            private void Version(XElement wurzel)
            {
                string version = Attr(wurzel, "version");
                _abbild.Version = version;
                _abbild.Schemastand = version;
                if (version == null || !BekannteVersionen.Contains(version))
                    Datei(PruefStufe.Info, "VERSION_UNBEKANNT", version ?? "");
            }

            private void Einheiten(XElement wurzel)
            {
                _abbild.Laengeneinheit = _laenge = Attr(wurzel, "lengthUnit");
                _abbild.Flaecheneinheit = _flaeche = Attr(wurzel, "areaUnit");
                _abbild.Volumeneinheit = _volumen = Attr(wurzel, "volumeUnit");
                _abbild.Temperatureinheit = _temperatur = Attr(wurzel, "temperatureUnit");
                _laengeGilt = Global("lengthUnit", _laenge, GbxmlEinheiten.Laenge);
                _flaecheGilt = Global("areaUnit", _flaeche, GbxmlEinheiten.Flaeche);
                _volumenGilt = Global("volumeUnit", _volumen, GbxmlEinheiten.Volumen);
                _temperaturGilt = Global("temperatureUnit", _temperatur, GbxmlEinheiten.Temperatur);
            }

            private bool Global(string attribut, string einheit, Func<double, string, double?> umrechnen)
            {
                if (einheit != null && umrechnen(1.0, einheit).HasValue) return true;
                EinheitUnbekannt(attribut, einheit ?? "");
                return false;
            }

            private void Kataloge(XElement wurzel)
            {
                foreach (XElement e in wurzel.Elements())
                {
                    string id = Attr(e, "id");
                    if (id == null) continue;
                    switch (e.Name.LocalName)
                    {
                        case "Material": Merken(_material, id, e); break;
                        case "Layer": Merken(_schicht, id, e); break;
                        case "Construction": Merken(_konstruktion, id, e); break;
                        case "WindowType": Merken(_fenstertyp, id, e); break;
                        case "Zone": Merken(_zone, id, e); break;
                    }
                }
            }

            private static void Merken(Dictionary<string, XElement> katalog, string id, XElement e)
            {
                if (!katalog.ContainsKey(id)) katalog[id] = e;   // die erste Angabe gilt
            }

            private void Ort(List<XElement> campusse)
            {
                XElement ort = campusse.Select(c => Kind(c, "Location")).FirstOrDefault(l => l != null);
                double? nord = null;
                if (ort != null)
                {
                    _abbild.Ort = Text(Kind(ort, "Name")) ?? Text(Kind(ort, "ZipcodeOrPostalCode"));
                    _abbild.BreiteGrad = Zahl(Kind(ort, "Latitude"), "Location");
                    _abbild.LaengeGrad = Zahl(Kind(ort, "Longitude"), "Location");
                    nord = Zahl(Kind(ort, "CADModelAzimuth"), "Location");
                }
                _abbild.NordwinkelGrad = nord;

                // Nicht still aufaddieren (3.2, Probe 21): Vorzeichen, Nullbezug und Drehsinn sind
                // nicht gegengemessen — ein falsches Vorzeichen drehte das Gebäude unbemerkt.
                if (!nord.HasValue) Datei(PruefStufe.Warnung, "KEIN_NORDEN");
                else if (nord.Value != 0.0) Datei(PruefStufe.Warnung, "NORDDREHUNG", Wert(nord.Value));
            }

            // --------------------------------------------------------------
            //  Gebäude und Räume
            // --------------------------------------------------------------

            private void Gebaeude(XElement building)
            {
                int index = _abbild.Gebaeude.Count;
                var g = new AbbildGebaeude
                {
                    Kennung = Kennung(building, "Building"),
                    Name = Text(Kind(building, "Name")),
                    Art = Attr(building, "buildingType"),
                };
                _abbild.Gebaeude.Add(g);

                foreach (XElement space in Kinder(building, "Space"))
                {
                    AbbildRaum r = Raum(space, g);
                    g.Raeume.Add(r);
                    if (r.Kennung.Length > 0 && !_raumGebaeude.ContainsKey(r.Kennung)) _raumGebaeude[r.Kennung] = index;
                }

                // Zonenvorschlag (3.3): X1 nur, wenn die Räume auf WENIGER Zonen zeigen, als es Räume
                // sind — 1:1 ist keine Gliederung, sondern ein Begleitobjekt (D13); sonst X2, wenn
                // mehr als ein Geschoss Räume trägt; sonst X4. Gewählt wird in G4c immer X4.
                g.ZahlZonen = g.Raeume.Select(r => r.ZonenKennung).Where(z => z != null && _zone.ContainsKey(z))
                               .Distinct(StringComparer.Ordinal).Count();
                g.ZahlGeschosseMitRaeumen = g.Raeume.Select(r => r.GeschossKennung).Where(s => s != null)
                                             .Distinct(StringComparer.Ordinal).Count();
                g.Zonenvorschlag = g.ZahlZonen >= 1 && g.ZahlZonen < g.Raeume.Count ? GebaeudeImportProfil.ZONENREGEL_X1
                                 : g.ZahlGeschosseMitRaeumen > 1 ? GebaeudeImportProfil.ZONENREGEL_X2
                                 : GebaeudeImportProfil.ZONENREGEL_X4;
                if (g.Zonenvorschlag != GebaeudeImportProfil.ZONENREGEL_X4)
                    g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "ZONENVORSCHLAG", g.Zonenvorschlag,
                        GebaeudeImportProfil.ZONENREGEL_X4, Ganz(g.ZahlZonen), Ganz(g.Raeume.Count),
                        Ganz(g.ZahlGeschosseMitRaeumen)));
            }

            private AbbildRaum Raum(XElement space, AbbildGebaeude g)
            {
                string id = Kennung(space, "Space");
                var r = new AbbildRaum
                {
                    Kennung = id,
                    Name = Text(Kind(space, "Name")),
                    Zustandsangabe = Attr(space, "conditionType"),
                    Raumtyp = Attr(space, "spaceType"),
                    ZonenKennung = Attr(space, "zoneIdRef"),
                    GeschossKennung = Attr(space, "buildingStoreyIdRef"),
                };
                r.FlaecheM2 = MitEinheit(Kind(space, "Area"), _flaeche, _flaecheGilt, GbxmlEinheiten.Flaeche, id);
                r.VolumenM3 = MitEinheit(Kind(space, "Volume"), _volumen, _volumenGilt, GbxmlEinheiten.Volumen, id);
                r.LuftwechselJeH = Zahl(Kind(space, "AirChangesPerHour"), id);
                r.LichtWm2 = PflichtEinheit(Kind(space, "LightPowerPerArea"), GbxmlEinheiten.LeistungJeFlaeche, id);
                r.GeraeteWm2 = PflichtEinheit(Kind(space, "EquipPowerPerArea"), GbxmlEinheiten.LeistungJeFlaeche, id);
                Personen(Kind(space, "PeopleNumber"), r);

                // Beheizt (3.3): Unconditioned, Vented, NaturallyVentedOnly → unbeheizt, alles andere
                // beheizt; ohne Attribut entscheidet der Name (Musterliste des IFC-Wegs).
                if (r.Zustandsangabe != null)
                {
                    r.BeheiztQuelle = BeheiztQuelle.Attribut;
                    r.Beheizt = !(Gleich(r.Zustandsangabe, "Unconditioned") || Gleich(r.Zustandsangabe, "Vented")
                                  || Gleich(r.Zustandsangabe, "NaturallyVentedOnly"));
                }
                else
                {
                    string treffer = Raumnamenregel.Treffer(r.Name ?? r.Kennung);
                    r.Beheizt = treffer == null;
                    r.BeheiztQuelle = treffer == null ? BeheiztQuelle.Annahme : BeheiztQuelle.Name;
                    if (treffer != null)
                        g.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "UNBEHEIZT_NAME", id, r.Name ?? "", treffer));
                }

                // Stufe G6c: der Name des Geschosses für die Zonenregel X2.
                if (r.GeschossKennung != null)
                    r.GeschossName = Text(Kind(space.Parent?.Elements().FirstOrDefault(e => e.Name.LocalName == "BuildingStorey"
                                                   && Attr(e, "id") == r.GeschossKennung), "Name"));

                if (r.ZonenKennung != null)
                {
                    if (_zone.TryGetValue(r.ZonenKennung, out XElement zone))
                    {
                        r.ZonenName = Text(Kind(zone, "Name"));
                        r.SollHeizenC = MitEinheit(Kind(zone, "DesignHeatT"), _temperatur, _temperaturGilt, GbxmlEinheiten.Temperatur, r.ZonenKennung);
                        r.SollKuehlenC = MitEinheit(Kind(zone, "DesignCoolT"), _temperatur, _temperaturGilt, GbxmlEinheiten.Temperatur, r.ZonenKennung);
                    }
                    else VerweisLeer("zoneIdRef", r.ZonenKennung, id);
                }
                return r;
            }

            /// <summary><c>PeopleNumber</c> in drei Einheiten (Befund R 5.1): umrechnen, nicht raten.</summary>
            private void Personen(XElement e, AbbildRaum r)
            {
                if (e == null) return;
                double? w = Zahl(e, r.Kennung);
                if (!w.HasValue) return;
                string einheit = Attr(e, "unit");
                if (einheit == GbxmlEinheiten.PERSONEN_ANZAHL) { r.Personen = w; return; }
                double? jePerson = einheit == null ? null : GbxmlEinheiten.FlaecheJePerson(w.Value, einheit);
                if (jePerson.HasValue) r.FlaecheJePersonM2 = jePerson;
                else EinheitUnbekannt("PeopleNumber", einheit ?? "");
            }

            // --------------------------------------------------------------
            //  Flächen und Öffnungen
            // --------------------------------------------------------------

            private void Flaeche(XElement s)
            {
                string typ = Attr(s, "surfaceType");
                string id = Attr(s, "id") ?? "";
                if (typ != null && Uebergangen.Contains(typ))
                {
                    _uebergangen[typ] = _uebergangen.TryGetValue(typ, out int n) ? n + 1 : 1;
                    return;
                }
                if (typ == null || !Flaechenarten.TryGetValue(typ, out (Bauteilart Art, Randbedingung Rand) art))
                {
                    Datei(PruefStufe.Warnung, "TYP_UNBEKANNT", typ ?? "", id);
                    return;
                }
                KennungPruefen(id, "Surface");

                var b = new AbbildBauteil
                {
                    Kennung = id, Quelltyp = "Surface", Name = Text(Kind(s, "Name")), Quellart = typ,
                    Art = art.Art, Randbedingung = art.Rand,
                };

                foreach (XElement a in Kinder(s, "AdjacentSpaceId"))
                {
                    string raum = Attr(a, "spaceIdRef");
                    b.Nachbarn.Add(new AbbildNachbar(raum, Attr(a, "surfaceType")));
                    if (raum == null || !_raumGebaeude.ContainsKey(raum))
                        b.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "NACHBAR_UNBEKANNT", id, raum ?? ""));
                }
                // Ein Innenbauteil mit nur einem Nachbarn: Die andere Seite nennt die Datei nicht.
                if (art.Rand == Randbedingung.Innen && b.Nachbarn.Count == 1 && _raumGebaeude.ContainsKey(b.Nachbarn[0].Kennung))
                    b.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "NACHBAR_UNBEKANNT", id, ""));

                Geometrie(s, b);

                string kref = Attr(s, "constructionIdRef");
                if (kref == null) _ohneAufbau.Add(id);
                else if (!_konstruktion.ContainsKey(kref)) VerweisLeer("constructionIdRef", kref, id);
                else
                {
                    b.Aufbau = Aufbau(kref);
                    if (b.Aufbau.UWertWm2K.HasValue) { b.UWertWm2K = b.Aufbau.UWertWm2K; b.UWertQuelle = "Construction"; }
                }

                foreach (XElement o in Kinder(s, "Opening"))
                {
                    AbbildBauteil oeffnung = Oeffnung(o, b);
                    if (oeffnung != null) b.Oeffnungen.Add(oeffnung);
                }

                List<int> gebaeude = b.Nachbarn.Select(n => _raumGebaeude.TryGetValue(n.Kennung, out int gi) ? gi : -1)
                                               .Where(gi => gi >= 0).Distinct().ToList();
                if (gebaeude.Count == 0)
                {
                    // Ohne bekannten Nachbarraum zählt die Fläche nirgends — sie darf aber nicht still
                    // verschwinden (3.5): Meldung dateiweit, samt allem, was an ihr hängt.
                    if (b.Nachbarn.Count == 0) Datei(PruefStufe.Warnung, "OHNE_NACHBAR", id, typ);
                    _abbild.Meldungen.AddRange(b.Meldungen);
                    foreach (AbbildBauteil o in b.Oeffnungen) _abbild.Meldungen.AddRange(o.Meldungen);
                    _abbild.BauteileOhneGebaeude.Add(b);
                    return;
                }
                foreach (int gi in gebaeude) _abbild.Gebaeude[gi].Bauteile.Add(b);
            }

            private AbbildBauteil Oeffnung(XElement o, AbbildBauteil wirt)
            {
                string typ = Attr(o, "openingType");
                string id = Attr(o, "id") ?? "";
                if (typ == "Air")
                {
                    _uebergangen["Air"] = _uebergangen.TryGetValue("Air", out int n) ? n + 1 : 1;
                    return null;
                }
                if (typ == null || !Oeffnungsarten.TryGetValue(typ, out Bauteilart art))
                {
                    Datei(PruefStufe.Warnung, "TYP_UNBEKANNT", typ ?? "", id);
                    return null;
                }
                KennungPruefen(id, "Opening");

                var b = new AbbildBauteil
                {
                    Kennung = id, Quelltyp = "Opening", Name = Text(Kind(o, "Name")), Quellart = typ,
                    Art = art, Randbedingung = wirt.Randbedingung,
                };
                Geometrie(o, b);

                // U-Wert: am Vorkommnis, sonst am Fenstertyp, sonst am Aufbau (Türen) — beide Orte
                // prüfen (3.4); das Vorkommnis hat Vorrang.
                double? u = PflichtEinheit(Kind(o, "U-value"), GbxmlEinheiten.UWert, id);
                string quelle = u.HasValue ? "Opening" : null;

                XElement fenstertyp = null;
                string wref = Attr(o, "windowTypeIdRef");
                if (wref != null && !_fenstertyp.TryGetValue(wref, out fenstertyp)) VerweisLeer("windowTypeIdRef", wref, id);
                if (!u.HasValue && fenstertyp != null)
                {
                    u = PflichtEinheit(Kind(fenstertyp, "U-value"), GbxmlEinheiten.UWert, wref);
                    if (u.HasValue) quelle = "WindowType";
                }

                string kref = Attr(o, "constructionIdRef");
                if (kref != null)
                {
                    if (!_konstruktion.ContainsKey(kref)) VerweisLeer("constructionIdRef", kref, id);
                    else
                    {
                        b.Aufbau = Aufbau(kref);
                        if (!u.HasValue && b.Aufbau.UWertWm2K.HasValue) { u = b.Aufbau.UWertWm2K; quelle = "Construction"; }
                    }
                }
                b.UWertWm2K = u;
                b.UWertQuelle = quelle;
                b.GWert = Energiedurchlass(o, id) ?? (fenstertyp != null ? Energiedurchlass(fenstertyp, wref) : null);
                return b;
            }

            /// <summary>
            /// Der g-Wert aus <c>SolarHeatGainCoeff</c>: bei mehreren Angaben die bei 0° Einfall, sonst
            /// die ohne Winkelangabe — ein Wert bei schrägem Einfall ist kein g-Wert und bleibt weg.
            /// </summary>
            private double? Energiedurchlass(XElement e, string wo)
            {
                XElement treffer = null;
                foreach (XElement shgc in Kinder(e, "SolarHeatGainCoeff"))
                {
                    string winkel = Attr(shgc, "solarIncidentAngle");
                    if (winkel == null) { treffer ??= shgc; continue; }
                    if (double.TryParse(winkel, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && w == 0.0)
                    {
                        treffer = shgc;
                        break;
                    }
                }
                return treffer == null ? null : PflichtEinheit(treffer, GbxmlEinheiten.Anteil, wo);
            }

            /// <summary>
            /// Fläche, Azimut und Neigung: aus <c>RectangularGeometry</c>; was dort fehlt, aus
            /// <c>PlanarGeometry/PolyLoop</c> (Flächeninhalt und Normale nach Newell — reine
            /// Vektorrechnung, kein Geometriekern); fehlt beides, ist die Geometrie nicht da (3.8).
            /// </summary>
            private void Geometrie(XElement e, AbbildBauteil b)
            {
                double? breite = null, hoehe = null, azimut = null, neigung = null;
                XElement rechteck = Kind(e, "RectangularGeometry");
                if (rechteck != null)
                {
                    breite = MitEinheit(Kind(rechteck, "Width"), _laenge, _laengeGilt, GbxmlEinheiten.Laenge, b.Kennung);
                    hoehe = MitEinheit(Kind(rechteck, "Height"), _laenge, _laengeGilt, GbxmlEinheiten.Laenge, b.Kennung);
                    azimut = Zahl(Kind(rechteck, "Azimuth"), b.Kennung);
                    neigung = Zahl(Kind(rechteck, "Tilt"), b.Kennung);
                }
                double? flaeche = breite > 0.0 && hoehe > 0.0 ? breite * hoehe : null;

                if (!flaeche.HasValue || !azimut.HasValue || !neigung.HasValue)
                {
                    XElement ring = Kind(Kind(e, "PlanarGeometry"), "PolyLoop");
                    if (ring != null && Polygon(ring, b.Kennung, out double polyFlaeche, out double? polyAzimut, out double polyNeigung))
                    {
                        if (!flaeche.HasValue) flaeche = polyFlaeche;
                        if (!neigung.HasValue) neigung = polyNeigung;
                        if (!azimut.HasValue) azimut = polyAzimut;
                    }
                }

                if (!flaeche.HasValue)
                    b.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "GEOMETRIE_FEHLT", b.Kennung));
                b.BruttoflaecheM2 = flaeche;
                b.NeigungGrad = neigung;
                b.AzimutGrad = azimut.HasValue ? Normiert(azimut.Value) : (double?)null;
            }

            /// <summary>
            /// Newell: n = Σ (y_i − y_j)(z_i + z_j), (z_i − z_j)(x_i + x_j), (x_i − x_j)(y_i + y_j);
            /// |n| = doppelte Fläche. Neigung = acos(n_z/|n|); Azimut = atan2(n_x, n_y) mit +y = Nord,
            /// +x = Ost, für eine waagerechte Normale unbestimmt.
            /// </summary>
            private bool Polygon(XElement ring, string wo, out double flaeche, out double? azimut, out double neigung)
            {
                flaeche = 0.0;
                azimut = null;
                neigung = 0.0;
                var punkte = new List<double[]>();
                foreach (XElement cp in Kinder(ring, "CartesianPoint"))
                {
                    double?[] k = Kinder(cp, "Coordinate")
                        .Select(c => MitEinheit(c, _laenge, _laengeGilt, GbxmlEinheiten.Laenge, wo)).ToArray();
                    if (k.Length < 3 || k.Take(3).Any(x => !x.HasValue)) return false;
                    punkte.Add(new[] { k[0].Value, k[1].Value, k[2].Value });
                }
                if (punkte.Count < 3) return false;

                double nx = 0.0, ny = 0.0, nz = 0.0;
                for (int i = 0; i < punkte.Count; i++)
                {
                    double[] a = punkte[i], c = punkte[(i + 1) % punkte.Count];
                    nx += (a[1] - c[1]) * (a[2] + c[2]);
                    ny += (a[2] - c[2]) * (a[0] + c[0]);
                    nz += (a[0] - c[0]) * (a[1] + c[1]);
                }
                double betrag = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (!(betrag > 0.0)) return false;

                flaeche = betrag / 2.0;
                neigung = Math.Acos(Math.Max(-1.0, Math.Min(1.0, nz / betrag))) * 180.0 / Math.PI;
                double waagerecht = Math.Sqrt(nx * nx + ny * ny);
                if (waagerecht > 1e-9 * betrag)
                    azimut = Normiert(Math.Atan2(nx, ny) * 180.0 / Math.PI);
                return true;
            }

            private static double Normiert(double azimut)
            {
                double a = azimut % 360.0;
                return a < 0.0 ? a + 360.0 : a;
            }

            // --------------------------------------------------------------
            //  Aufbauten und Schichten (3.6)
            // --------------------------------------------------------------

            private AbbildAufbau Aufbau(string kref)
            {
                if (_aufbauten.TryGetValue(kref, out AbbildAufbau vorhanden)) return vorhanden;

                XElement k = _konstruktion[kref];
                var a = new AbbildAufbau
                {
                    Kennung = KennungPruefen(kref, "Construction"),
                    Name = Text(Kind(k, "Name")),
                    UWertWm2K = PflichtEinheit(Kind(k, "U-value"), GbxmlEinheiten.UWert, kref),
                    Richtung = Schichtrichtung.AussenNachInnen,
                    RichtungAngenommen = true,
                };
                _aufbauten[kref] = a;

                bool luecke = false;
                foreach (XElement li in Kinder(k, "LayerId"))
                {
                    string lref = Attr(li, "layerIdRef");
                    if (lref == null || !_schicht.TryGetValue(lref, out XElement layer))
                    {
                        VerweisLeer("layerIdRef", lref ?? "", kref);
                        luecke = true;
                        continue;
                    }
                    foreach (XElement mi in Kinder(layer, "MaterialId"))
                    {
                        string mref = Attr(mi, "materialIdRef");
                        if (mref == null || !_material.TryGetValue(mref, out XElement material))
                        {
                            VerweisLeer("materialIdRef", mref ?? "", lref);
                            luecke = true;
                            continue;
                        }
                        a.Schichten.Add(Schicht(mref, material));
                    }
                }

                if (a.Schichten.Count == 0) a.Status = Aufbaustatus.OhneAufbau;
                else if (!luecke && a.Schichten.All(s => s.Vollstaendig)) a.Status = Aufbaustatus.Vollstaendig;
                else if (!luecke && a.Schichten.All(s => s.HatWiderstand))
                {
                    // Eine einzige Schicht ohne Masse macht die Speicherfähigkeit des GANZEN Aufbaus
                    // unbrauchbar — die Masse fehlt dort, wo sie sitzt (3.6, Punkt 2).
                    a.Status = Aufbaustatus.Masselos;
                    Datei(PruefStufe.Warnung, "AUFBAU_MASSELOS", kref);
                }
                else
                {
                    a.Status = Aufbaustatus.Unvollstaendig;
                    Datei(PruefStufe.Warnung, "AUFBAU_UNVOLLSTAENDIG", kref);
                }
                return a;
            }

            private AbbildSchicht Schicht(string mref, XElement m)
            {
                KennungPruefen(mref, "Material");
                return new AbbildSchicht
                {
                    BaustoffKennung = mref,
                    Name = Text(Kind(m, "Name")),
                    DickeM = Positiv(MitEinheit(Kind(m, "Thickness"), _laenge, _laengeGilt, GbxmlEinheiten.Laenge, mref), "Thickness", mref),
                    LambdaWmK = Positiv(PflichtEinheit(Kind(m, "Conductivity"), GbxmlEinheiten.Leitfaehigkeit, mref), "Conductivity", mref),
                    RhoKgM3 = Positiv(PflichtEinheit(Kind(m, "Density"), GbxmlEinheiten.Dichte, mref), "Density", mref),
                    CpJkgK = Positiv(PflichtEinheit(Kind(m, "SpecificHeat"), GbxmlEinheiten.Waermekapazitaet, mref), "SpecificHeat", mref),
                    RWertM2KW = Positiv(PflichtEinheit(Kind(m, "R-value"), GbxmlEinheiten.RWert, mref), "R-value", mref),
                };
            }

            /// <summary>Ein Stoffwert ≤ 0 ist kein Wert, sondern eine Fehlstelle (3.6, Plausibilitätsband).</summary>
            private double? Positiv(double? wert, string groesse, string wo)
            {
                if (!wert.HasValue || wert.Value > 0.0) return wert;
                if (_gemeldet.Add("F|" + wo + "|" + groesse))
                    Datei(PruefStufe.Warnung, "STOFFWERT_FEHLSTELLE", wo, groesse, Wert(wert.Value));
                return null;
            }

            // --------------------------------------------------------------
            //  Zahlen und Einheiten
            // --------------------------------------------------------------

            /// <summary>Eine Größe mit optionalem <c>unit</c>: das lokale schlägt das globale (3.2).</summary>
            private double? MitEinheit(XElement e, string global, bool globalGilt, Func<double, string, double?> umrechnen, string wo)
            {
                if (e == null) return null;
                double? w = Zahl(e, wo);
                if (!w.HasValue) return null;
                string einheit = Attr(e, "unit");
                if (einheit == null)
                {
                    if (!globalGilt) return null;   // die ungültige globale Angabe ist schon gemeldet
                    einheit = global;
                }
                double? si = umrechnen(w.Value, einheit);
                if (!si.HasValue) EinheitUnbekannt(e.Name.LocalName, einheit);
                return si;
            }

            /// <summary>Eine Größe mit Pflichtangabe <c>unit</c>; fehlt sie, ist die Einheit unbekannt.</summary>
            private double? PflichtEinheit(XElement e, Func<double, string, double?> umrechnen, string wo)
            {
                if (e == null) return null;
                double? w = Zahl(e, wo);
                if (!w.HasValue) return null;
                string einheit = Attr(e, "unit");
                double? si = einheit == null ? null : umrechnen(w.Value, einheit);
                if (!si.HasValue) EinheitUnbekannt(e.Name.LocalName, einheit ?? "");
                return si;
            }

            private void EinheitUnbekannt(string groesse, string einheit)
            {
                if (_gemeldet.Add("E|" + groesse + "|" + einheit))
                    Datei(PruefStufe.Warnung, "EINHEIT_UNBEKANNT", groesse, einheit);
            }

            private double? Zahl(XElement e, string wo)
            {
                string t = Text(e);
                if (t == null) return null;
                if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                    && !double.IsNaN(w) && !double.IsInfinity(w))
                    return w;
                if (_gemeldet.Add("Z|" + e.Name.LocalName + "|" + wo))
                    Datei(PruefStufe.Warnung, "ZAHL_UNLESBAR", e.Name.LocalName, t, wo ?? "");
                return null;
            }

            // --------------------------------------------------------------
            //  Kennungen, Verweise, Meldungen
            // --------------------------------------------------------------

            private string Kennung(XElement e, string quelltyp) => KennungPruefen(Attr(e, "id") ?? "", quelltyp);

            /// <summary>Eine Kennung über 64 Zeichen wird für die Persistenz gekürzt (7.2) — hier nur vermerkt (Probe 24).</summary>
            private string KennungPruefen(string id, string quelltyp)
            {
                if (id.Length > Quellkennung.MAX_LAENGE && _gemeldet.Add("K|" + id))
                    Datei(PruefStufe.Info, "KENNUNG_GEKUERZT", quelltyp, Quellkennung.Kuerzen(id), Ganz(id.Length));
                return id;
            }

            private void VerweisLeer(string art, string ziel, string wo)
            {
                if (_gemeldet.Add("V|" + art + "|" + ziel))
                    Datei(PruefStufe.Warnung, "VERWEIS_LEER", art, ziel, wo ?? "");
            }

            private void Datei(PruefStufe stufe, string name, params string[] werte)
                => _abbild.Meldungen.Add(new PruefMeldung(stufe, P + name, werte));

            private static string Beispiele(List<string> kennungen)
                => kennungen.Count <= 3 ? string.Join(", ", kennungen) : string.Join(", ", kennungen.Take(3)) + ", …";
        }

        // ==================================================================
        //  XML-Hilfen — über den lokalen Namen, mit oder ohne Namensraum
        // ==================================================================

        private static IEnumerable<XElement> Kinder(XElement e, string name)
            => e == null ? Enumerable.Empty<XElement>() : e.Elements().Where(k => k.Name.LocalName == name);

        private static XElement Kind(XElement e, string name) => Kinder(e, name).FirstOrDefault();

        private static string Attr(XElement e, string name)
        {
            if (e == null) return null;
            foreach (XAttribute a in e.Attributes())
                if (a.Name.LocalName == name && (a.Name.Namespace == XNamespace.None || a.Name.NamespaceName == NAMENSRAUM))
                {
                    string w = a.Value.Trim();
                    return w.Length == 0 ? null : w;
                }
            return null;
        }

        private static string Text(XElement e)
        {
            if (e == null) return null;
            string t = e.Value.Trim();
            return t.Length == 0 ? null : t;
        }

        private static bool Gleich(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static string Wert(double w) => w.ToString("R", CultureInfo.InvariantCulture);

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
