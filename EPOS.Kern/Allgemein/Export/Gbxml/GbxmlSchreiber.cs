using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der gbXML-Schreiber</b> (Stufe G7a, Datenaustauschkonzept 5.2; ADR-004) — das Spiegelbild von
    /// <see cref="GbxmlLeser"/>: LINQ to XML, reine BCL, kein Paket, kein Serialisierer, keine
    /// Schemaprüfung im Kern (die läuft nur im Test gegen die lokale Schemakopie, D17).
    ///
    /// <para><b>Er schreibt das Abbild, wie es ist.</b> Die Regeln, die aus EPOS-Zeilen ein Abbild
    /// machen — Umkehrtabelle, Übergangsfälle, Ersatzschichtung, Kennungen, Texte in der Sprache des
    /// Profils —, stehen im Ablauf. Der Schreiber prüft nur, was die Datei schemawidrig oder
    /// unlesbar machte, und wirft dann (<see cref="InvalidOperationException"/>, ein Programmfehler des
    /// Ablaufs): genau ein Gebäude, mindestens <see cref="GbxmlVokabular.MINDESTZAHL_FLAECHEN"/> Flächen,
    /// jede Fläche mit Aufbau und ein bis zwei bekannten Nachbarräumen, jede Kennung ein NCName und
    /// eindeutig, jede Zahl endlich, Azimut in 0° … 360°, Neigung in 0° … 180°, Breite × Höhe =
    /// Bruttofläche.</para>
    ///
    /// <para><b>Aufbau der Datei</b> (nach der Schemakopie; jede Folge dort ist eine offene Auswahl):
    /// <c>Campus</c> mit <c>Name</c>, <c>Description</c>, <c>Location</c> (nur mit PLZ),
    /// <c>Building</c> samt <c>Space</c> und danach alle <c>Surface</c> samt <c>Opening</c>; dann an der
    /// Wurzel <c>Construction</c>, <c>Layer</c>, <c>Material</c>, <c>WindowType</c>, <c>Zone</c> und
    /// <c>DocumentHistory</c>. Kataloge in der Reihenfolge ihres ersten Auftretens; jede Kennung
    /// einmal.</para>
    ///
    /// <para><b>Schichtfolge:</b> gbXML zählt die erste Schicht außen (Datenaustauschkonzept 3.7). Steht
    /// ein Aufbau innen → außen im Abbild (<see cref="Schichtrichtung.InnenNachAussen"/>, die Zählung von
    /// <c>Tab_Bauteilschicht.Reihenfolge</c>), kehrt der Schreiber die Folge um.</para>
    ///
    /// <para><b>Byteform:</b> UTF-8 ohne BOM, Zeilenende <c>\n</c>, zwei Leerzeichen Einzug; Zahlen
    /// kulturfrei in der kürzesten rundlaufenden Form. Die Datei entsteht erst im Speicher und geht
    /// als Ganzes in den Zielstrom — bei Abbruch oder Fehler bleibt er unberührt. Der einzige
    /// Zeitstempel steht in <c>DocumentHistory/CreatedBy/@date</c>.</para>
    /// </summary>
    internal sealed class GbxmlSchreiber : IGebaeudeSchreiber
    {
        /// <summary>
        /// Der Versionswert der Wurzel. <b>Bewusst 6.01, nicht 8.01:</b> Die Schemakopie
        /// <c>GreenBuildingXML_Ver8.01.xsd</c> führt in <c>versionEnum</c> als höchsten Wert <c>6.01</c> —
        /// der im Änderungsprotokoll zu 7.03/7.04 angekündigte Nachtrag wurde nie ausgeführt, und
        /// <c>version="8.01"</c> ist nach dem eigenen Schema ungültig (Befund R 1.1, Datenaustauschkonzept
        /// 5.2). Nicht „korrigieren".
        /// </summary>
        internal const string VERSION = "6.01";

        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;

        /// <inheritdoc />
        public GebaeudeExportBilanz Schreiben(GebaeudeAbbild abbild, Stream ziel, GebaeudeExportProfil profil, CancellationToken abbruch)
        {
            if (abbild == null) throw new ArgumentNullException(nameof(abbild));
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            abbruch.ThrowIfCancellationRequested();

            var lauf = new Lauf(abbild, profil, abbruch);
            XDocument dokument = lauf.Bauen();
            byte[] bytes = Serialisieren(dokument);
            abbruch.ThrowIfCancellationRequested();
            ziel.Write(bytes, 0, bytes.Length);
            ziel.Flush();
            return new GebaeudeExportBilanz(lauf.Flaechen, lauf.Oeffnungen, lauf.Aufbauten, lauf.Ersatzaufbauten,
                                            lauf.Meldungen, bytes.Length);
        }

        /// <summary>Die Byteform: UTF-8 ohne BOM, <c>\n</c>, zwei Leerzeichen Einzug.</summary>
        internal static byte[] Serialisieren(XDocument dokument)
        {
            var einstellungen = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false,
                CloseOutput = false,
            };
            using (var speicher = new MemoryStream())
            {
                using (XmlWriter schreiber = XmlWriter.Create(speicher, einstellungen))
                    dokument.Save(schreiber);
                return speicher.ToArray();
            }
        }

        /// <summary>
        /// Eine Zahl kulturfrei in der kürzesten rundlaufenden Form (<c>"R"</c>, invariant). Weil viele
        /// Größen im Schema <c>xsd:decimal</c> sind und <c>xsd:decimal</c> keinen Exponenten kennt, wird
        /// eine Exponentenform (<c>1E-05</c>) ziffernerhaltend in Dezimalschreibweise ausgeschrieben
        /// (<c>0.00001</c>) — derselbe Wert, dieselben Ziffern.
        /// </summary>
        /// <exception cref="InvalidOperationException">Die Zahl ist nicht endlich.</exception>
        internal static string Zahl(double wert)
        {
            if (double.IsNaN(wert) || double.IsInfinity(wert))
                throw new InvalidOperationException("gbXML-Export: Eine Zahl ist nicht endlich.");
            string s = wert.ToString("R", CultureInfo.InvariantCulture);
            int e = s.IndexOfAny(new[] { 'E', 'e' });
            return e < 0 ? s : Ausschreiben(s, e);
        }

        private static string Ausschreiben(string s, int e)
        {
            string mantisse = s.Substring(0, e);
            int exponent = int.Parse(s.Substring(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            string vorzeichen = "";
            if (mantisse.StartsWith("-", StringComparison.Ordinal))
            {
                vorzeichen = "-";
                mantisse = mantisse.Substring(1);
            }
            int punkt = mantisse.IndexOf('.');
            string ziffern = punkt < 0 ? mantisse : mantisse.Remove(punkt, 1);
            int stelle = (punkt < 0 ? mantisse.Length : punkt) + exponent;
            string ergebnis;
            if (stelle <= 0) ergebnis = "0." + new string('0', -stelle) + ziffern;
            else if (stelle >= ziffern.Length) ergebnis = ziffern + new string('0', stelle - ziffern.Length);
            else ergebnis = ziffern.Substring(0, stelle) + "." + ziffern.Substring(stelle);
            return vorzeichen + ergebnis;
        }

        /// <summary>
        /// Der Zeitstempel als <c>xsd:dateTime</c>, auf Sekunden: ohne Zone bei unbestimmter Art, mit
        /// <c>Z</c> bei UTC, mit Versatz bei Ortszeit.
        /// </summary>
        internal static string Zeitstempel(DateTime zeit)
        {
            string text = zeit.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            switch (zeit.Kind)
            {
                case DateTimeKind.Utc: return text + "Z";
                case DateTimeKind.Local: return text + zeit.ToString("zzz", CultureInfo.InvariantCulture);
                default: return text;
            }
        }

        // ==================================================================
        //  Ein Schreibdurchgang — der Zustand eines Laufs
        // ==================================================================

        private sealed class Lauf
        {
            private readonly GebaeudeAbbild _abbild;
            private readonly GebaeudeExportProfil _profil;
            private readonly CancellationToken _abbruch;

            private readonly HashSet<string> _kennungen = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _raeume = new HashSet<string>(StringComparer.Ordinal);

            private readonly List<XElement> _konstruktionen = new List<XElement>();
            private readonly List<XElement> _schichten = new List<XElement>();
            private readonly List<XElement> _stoffe = new List<XElement>();
            private readonly List<XElement> _fenstertypen = new List<XElement>();
            private readonly List<XElement> _zonen = new List<XElement>();

            private readonly Dictionary<string, AbbildAufbau> _aufbauJeKennung = new Dictionary<string, AbbildAufbau>(StringComparer.Ordinal);
            private readonly Dictionary<string, string> _stoffJeSchicht = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly Dictionary<string, AbbildSchicht> _schichtJeStoff = new Dictionary<string, AbbildSchicht>(StringComparer.Ordinal);

            internal Lauf(GebaeudeAbbild abbild, GebaeudeExportProfil profil, CancellationToken abbruch)
            {
                _abbild = abbild;
                _profil = profil;
                _abbruch = abbruch;
            }

            internal int Flaechen { get; private set; }
            internal int Oeffnungen { get; private set; }
            internal int Aufbauten { get; private set; }
            internal int Ersatzaufbauten { get; private set; }
            internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

            internal XDocument Bauen()
            {
                if (_abbild.Gebaeude.Count != 1)
                    throw Fehler("Das Abbild trägt " + _abbild.Gebaeude.Count.ToString(CultureInfo.InvariantCulture)
                                 + " Gebäude; exportiert wird genau eines.");
                AbbildGebaeude g = _abbild.Gebaeude[0];
                List<AbbildBauteil> flaechen = g.Bauteile.ToList();
                if (flaechen.Count < GbxmlVokabular.MINDESTZAHL_FLAECHEN)
                    throw Fehler("Das Gebäude trägt " + flaechen.Count.ToString(CultureInfo.InvariantCulture) + " Flächen; das Schema verlangt mindestens "
                                 + GbxmlVokabular.MINDESTZAHL_FLAECHEN.ToString(CultureInfo.InvariantCulture) + ".");

                var campus = new XElement(NS + "Campus", new XAttribute("id", Kennung(_abbild.CampusKennung, "Campus")));
                Text(campus, "Name", g.Name);
                Text(campus, "Description", g.Beschreibung);
                if (!string.IsNullOrWhiteSpace(_abbild.Plz))
                    campus.Add(new XElement(NS + "Location",
                        new XElement(NS + "ZipcodeOrPostalCode", Klartext(_abbild.Plz.Trim())),
                        new XElement(NS + "CADModelAzimuth", Zahl(_abbild.NordwinkelGrad ?? 0.0))));
                campus.Add(Gebaeude(g));

                foreach (AbbildBauteil f in flaechen)
                {
                    _abbruch.ThrowIfCancellationRequested();
                    campus.Add(Flaeche(f));
                }

                var wurzel = new XElement(NS + "gbXML",
                    new XAttribute("xmlns", GbxmlLeser.NAMENSRAUM),
                    new XAttribute("version", VERSION),
                    new XAttribute("temperatureUnit", GbxmlVokabular.Celsius),
                    new XAttribute("lengthUnit", GbxmlVokabular.Meters),
                    new XAttribute("areaUnit", GbxmlVokabular.SquareMeters),
                    new XAttribute("volumeUnit", GbxmlVokabular.CubicMeters),
                    new XAttribute("useSIUnitsForResults", "true"),
                    campus);
                wurzel.Add(_konstruktionen, _schichten, _stoffe, _fenstertypen, _zonen);
                wurzel.Add(Dokumentgeschichte());
                return new XDocument(wurzel);
            }

            // --------------------------------------------------------------
            //  Gebäude, Räume, Zonen
            // --------------------------------------------------------------

            private XElement Gebaeude(AbbildGebaeude g)
            {
                string art = string.IsNullOrWhiteSpace(g.Art) ? GbxmlVokabular.Unknown : g.Art.Trim();
                if (!GbxmlVokabular.Gebaeudearten.Contains(art, StringComparer.Ordinal))
                    throw Fehler("Die Gebäudeart „" + art + "\" ist kein Wert von buildingTypeEnum.");
                var building = new XElement(NS + "Building",
                    new XAttribute("id", Kennung(g.Kennung, "Building")),
                    new XAttribute("buildingType", art));
                Text(building, "Name", g.Name);
                double flaeche = g.Raeume.Where(r => r.FlaecheM2.HasValue).Sum(r => r.FlaecheM2.Value);
                building.Add(new XElement(NS + "Area", Zahl(flaeche)));

                // Erst alle Räume bekannt machen, dann die Zonen in der Reihenfolge ihres ersten Raums.
                foreach (AbbildRaum r in g.Raeume) _raeume.Add(r.Kennung ?? "");
                var zonen = new HashSet<string>(StringComparer.Ordinal);
                foreach (AbbildRaum r in g.Raeume)
                {
                    building.Add(Raum(r));
                    if (r.ZonenKennung != null && zonen.Add(r.ZonenKennung)) _zonen.Add(Zone(r));
                }
                return building;
            }

            private XElement Raum(AbbildRaum r)
            {
                string zustand = r.Zustandsangabe ?? (r.Beheizt ? GbxmlVokabular.Heated : GbxmlVokabular.Unconditioned);
                if (!GbxmlVokabular.Nutzungszustaende.Contains(zustand, StringComparer.Ordinal))
                    throw Fehler("Der Nutzungszustand „" + zustand + "\" des Raums " + r.Kennung + " ist kein Wert von conditionTypeEnum.");

                var space = new XElement(NS + "Space", new XAttribute("id", Kennung(r.Kennung, "Space")));
                if (r.ZonenKennung != null) space.Add(new XAttribute("zoneIdRef", r.ZonenKennung));
                space.Add(new XAttribute("conditionType", zustand));
                Text(space, "Name", r.Name);
                Text(space, "Description", r.Beschreibung);
                Groesse(space, "Area", r.FlaecheM2, null);
                Groesse(space, "Volume", r.VolumenM3, null);
                Groesse(space, "AirChangesPerHour", r.LuftwechselJeH, null);
                if (r.Personen.HasValue) Groesse(space, "PeopleNumber", r.Personen, GbxmlVokabular.NumberOfPeople);
                else Groesse(space, "PeopleNumber", r.FlaecheJePersonM2, GbxmlVokabular.SquareMPerPerson);
                Groesse(space, "LightPowerPerArea", r.LichtWm2, GbxmlVokabular.WattPerSquareMeter);
                Groesse(space, "EquipPowerPerArea", r.GeraeteWm2, GbxmlVokabular.WattPerSquareMeter);
                return space;
            }

            private XElement Zone(AbbildRaum r)
            {
                var zone = new XElement(NS + "Zone", new XAttribute("id", Kennung(r.ZonenKennung, "Zone")));
                Text(zone, "Name", r.Name);
                Text(zone, "Description", r.ZonenBeschreibung);
                Groesse(zone, "DesignHeatT", r.SollHeizenC, null);
                Groesse(zone, "DesignCoolT", r.SollKuehlenC, null);
                return zone;
            }

            // --------------------------------------------------------------
            //  Flächen und Öffnungen
            // --------------------------------------------------------------

            private XElement Flaeche(AbbildBauteil f)
            {
                if (f.Quellart == null || !GbxmlVokabular.Flaechenarten.ContainsKey(f.Quellart))
                    throw Fehler("Die Fläche " + f.Kennung + " trägt keine schreibbare Flächenart (" + (f.Quellart ?? "keine") + ").");
                if (f.Aufbau == null)
                    throw Fehler("Die Fläche " + f.Kennung + " hat keinen Aufbau; das Schema verlangt constructionIdRef.");
                if (f.Nachbarn.Count < 1 || f.Nachbarn.Count > 2)
                    throw Fehler("Die Fläche " + f.Kennung + " hat " + f.Nachbarn.Count.ToString(CultureInfo.InvariantCulture)
                                 + " Nachbarräume; das Schema erlaubt einen oder zwei.");

                var surface = new XElement(NS + "Surface",
                    new XAttribute("id", Kennung(f.Kennung, "Surface")),
                    new XAttribute("surfaceType", f.Quellart),
                    new XAttribute("constructionIdRef", Aufbau(f.Aufbau)));
                Flaechen++;
                Text(surface, "Name", f.Name);
                foreach (AbbildNachbar n in f.Nachbarn)
                {
                    if (!_raeume.Contains(n.Kennung))
                        throw Fehler("Die Fläche " + f.Kennung + " nennt den Raum „" + n.Kennung + "\", den das Gebäude nicht trägt.");
                    var nachbar = new XElement(NS + "AdjacentSpaceId", new XAttribute("spaceIdRef", n.Kennung));
                    if (n.Sicht != null)
                    {
                        if (!GbxmlVokabular.Flaechenarten.ContainsKey(n.Sicht))
                            throw Fehler("Die Sicht „" + n.Sicht + "\" an der Fläche " + f.Kennung + " ist keine schreibbare Flächenart.");
                        nachbar.Add(new XAttribute("surfaceType", n.Sicht));
                    }
                    surface.Add(nachbar);
                }
                surface.Add(Rechteck(f));
                foreach (AbbildBauteil o in f.Oeffnungen) surface.Add(Oeffnung(o));
                return surface;
            }

            private XElement Oeffnung(AbbildBauteil o)
            {
                if (o.Quellart == null || !GbxmlVokabular.Oeffnungsarten.ContainsKey(o.Quellart))
                    throw Fehler("Die Öffnung " + o.Kennung + " trägt keine schreibbare Öffnungsart (" + (o.Quellart ?? "keine") + ").");

                var opening = new XElement(NS + "Opening",
                    new XAttribute("id", Kennung(o.Kennung, "Opening")),
                    new XAttribute("openingType", o.Quellart));
                Oeffnungen++;
                if (o.FenstertypKennung != null)
                {
                    opening.Add(new XAttribute("windowTypeIdRef", Kennung(o.FenstertypKennung, "WindowType")));
                    _fenstertypen.Add(Fenstertyp(o));
                }
                if (o.Aufbau != null) opening.Add(new XAttribute("constructionIdRef", Aufbau(o.Aufbau)));
                Text(opening, "Name", o.Name);
                if (o.FenstertypKennung == null)
                {
                    // Ohne Fenstertyp (Tür): U und g stehen an der Öffnung selbst.
                    Groesse(opening, "U-value", o.UWertWm2K, GbxmlVokabular.WPerSquareMeterK);
                    Groesse(opening, "SolarHeatGainCoeff", o.GWert, GbxmlVokabular.Fraction);
                }
                opening.Add(Rechteck(o));
                return opening;
            }

            private XElement Fenstertyp(AbbildBauteil o)
            {
                var fenstertyp = new XElement(NS + "WindowType", new XAttribute("id", o.FenstertypKennung));
                Text(fenstertyp, "Name", o.Name);
                Groesse(fenstertyp, "U-value", o.UWertWm2K, GbxmlVokabular.WPerSquareMeterK);
                Groesse(fenstertyp, "SolarHeatGainCoeff", o.GWert, GbxmlVokabular.Fraction);
                return fenstertyp;
            }

            /// <summary><c>RectangularGeometry</c>: Azimut, Neigung, Breite und Höhe, soweit das Abbild sie trägt.</summary>
            private XElement Rechteck(AbbildBauteil b)
            {
                bool mitFlaeche = b.BruttoflaecheM2.HasValue;
                bool mitMassen = b.BreiteM.HasValue || b.HoeheM.HasValue;
                if (mitFlaeche || mitMassen)
                {
                    if (!(b.BreiteM > 0.0) || !(b.HoeheM > 0.0) || !mitFlaeche)
                        throw Fehler("Das Bauteil " + b.Kennung + " braucht Bruttofläche, Breite und Höhe zusammen (Breite, Höhe > 0).");
                    double flaeche = b.BreiteM.Value * b.HoeheM.Value;
                    if (Math.Abs(flaeche - b.BruttoflaecheM2.Value) > 1e-9 * Math.Max(1.0, Math.Abs(b.BruttoflaecheM2.Value)))
                        throw Fehler("Am Bauteil " + b.Kennung + " ist Breite × Höhe nicht die Bruttofläche.");
                }
                if (b.AzimutGrad is double az && !(az >= 0.0 && az <= 360.0))
                    throw Fehler("Der Azimut des Bauteils " + b.Kennung + " liegt nicht in 0° … 360°.");
                if (b.NeigungGrad is double ng && !(ng >= 0.0 && ng <= 180.0))
                    throw Fehler("Die Neigung des Bauteils " + b.Kennung + " liegt nicht in 0° … 180°.");

                var rechteck = new XElement(NS + "RectangularGeometry");
                Groesse(rechteck, "Azimuth", b.AzimutGrad, null);
                Groesse(rechteck, "Tilt", b.NeigungGrad, null);
                Groesse(rechteck, "Width", b.BreiteM, null);
                Groesse(rechteck, "Height", b.HoeheM, null);
                return rechteck;
            }

            // --------------------------------------------------------------
            //  Aufbauten, Schichten, Baustoffe
            // --------------------------------------------------------------

            /// <summary>Schreibt den Aufbau beim ersten Auftreten und liefert seine Kennung.</summary>
            private string Aufbau(AbbildAufbau a)
            {
                string kennung = a.Kennung;
                if (_aufbauJeKennung.TryGetValue(kennung ?? "", out AbbildAufbau vorhanden))
                {
                    if (!ReferenceEquals(vorhanden, a) && !GleicherAufbau(vorhanden, a))
                        throw Fehler("Die Aufbaukennung " + kennung + " steht für zwei verschiedene Aufbauten.");
                    return kennung;
                }

                var construction = new XElement(NS + "Construction", new XAttribute("id", Kennung(kennung, "Construction")));
                _aufbauJeKennung[kennung] = a;
                Text(construction, "Name", a.Name);
                Text(construction, "Description", a.Beschreibung);
                Groesse(construction, "U-value", a.UWertWm2K, GbxmlVokabular.WPerSquareMeterK);
                foreach (AbbildSchicht s in AussenNachInnen(a))
                    construction.Add(new XElement(NS + "LayerId", new XAttribute("layerIdRef", Schicht(s))));
                _konstruktionen.Add(construction);
                Aufbauten++;
                if (a.IstErsatz) Ersatzaufbauten++;
                return kennung;
            }

            /// <summary>Die Schichten in der Zählrichtung von gbXML: die erste außen.</summary>
            private static IEnumerable<AbbildSchicht> AussenNachInnen(AbbildAufbau a)
                => a.Richtung == Schichtrichtung.InnenNachAussen ? Enumerable.Reverse(a.Schichten) : a.Schichten;

            private static bool GleicherAufbau(AbbildAufbau x, AbbildAufbau y)
                => x.Name == y.Name && x.Beschreibung == y.Beschreibung && x.UWertWm2K == y.UWertWm2K && x.IstErsatz == y.IstErsatz
                   && AussenNachInnen(x).Select(s => s.Kennung).SequenceEqual(AussenNachInnen(y).Select(s => s.Kennung), StringComparer.Ordinal);

            /// <summary>Schreibt Schicht und Baustoff beim ersten Auftreten und liefert die Kennung der Schicht.</summary>
            private string Schicht(AbbildSchicht s)
            {
                string schicht = s.Kennung;
                if (string.IsNullOrEmpty(schicht)) throw Fehler("Eine Schicht zum Baustoff " + s.BaustoffKennung + " trägt keine Kennung.");
                // Der Baustoff zuerst: Er prüft bei jedem Auftreten, dass seine Kennung dieselben Stoffwerte trägt.
                string stoff = Stoff(s);
                if (_stoffJeSchicht.TryGetValue(schicht, out string stoffVorher))
                {
                    if (!string.Equals(stoffVorher, stoff, StringComparison.Ordinal))
                        throw Fehler("Die Schichtkennung " + schicht + " steht für zwei verschiedene Baustoffe.");
                    return schicht;
                }
                _schichten.Add(new XElement(NS + "Layer", new XAttribute("id", Kennung(schicht, "Layer")),
                    new XElement(NS + "MaterialId", new XAttribute("materialIdRef", stoff))));
                _stoffJeSchicht[schicht] = stoff;
                return schicht;
            }

            private string Stoff(AbbildSchicht s)
            {
                string kennung = s.BaustoffKennung;
                if (_schichtJeStoff.TryGetValue(kennung ?? "", out AbbildSchicht vorher))
                {
                    if (!GleicherStoff(vorher, s))
                        throw Fehler("Die Baustoffkennung " + kennung + " steht für zwei verschiedene Stoffwerte.");
                    return kennung;
                }
                var material = new XElement(NS + "Material", new XAttribute("id", Kennung(kennung, "Material")));
                _schichtJeStoff[kennung] = s;
                Text(material, "Name", s.Name);
                Groesse(material, "Thickness", s.DickeM, null);
                Groesse(material, "Conductivity", s.LambdaWmK, GbxmlVokabular.WPerMeterK);
                Groesse(material, "Density", s.RhoKgM3, GbxmlVokabular.KgPerCubicM);
                Groesse(material, "SpecificHeat", s.CpJkgK, GbxmlVokabular.JPerKgK);
                Groesse(material, "R-value", s.RWertM2KW, GbxmlVokabular.SquareMeterKPerW);
                _stoffe.Add(material);
                return kennung;
            }

            private static bool GleicherStoff(AbbildSchicht x, AbbildSchicht y)
                => x.Name == y.Name && x.DickeM == y.DickeM && x.LambdaWmK == y.LambdaWmK && x.RhoKgM3 == y.RhoKgM3
                   && x.CpJkgK == y.CpJkgK && x.RWertM2KW == y.RWertM2KW;

            // --------------------------------------------------------------
            //  DocumentHistory — Programmangaben, keine Anwender- und Lizenzdaten
            // --------------------------------------------------------------

            private XElement Dokumentgeschichte()
            {
                string programm = Kennung(GebaeudeExportKennung.PROGRAMM, "ProgramInfo");
                string person = Kennung(GebaeudeExportKennung.PERSON, "PersonInfo");
                return new XElement(NS + "DocumentHistory",
                    new XElement(NS + "ProgramInfo", new XAttribute("id", programm),
                        new XElement(NS + "ProductName", _profil.Programmname),
                        new XElement(NS + "Version", Klartext(_profil.Programmversion))),
                    new XElement(NS + "PersonInfo", new XAttribute("id", person),
                        new XElement(NS + "FirstName", _profil.Programmname),
                        new XElement(NS + "LastName", Klartext(_profil.Programmversion))),
                    new XElement(NS + "CreatedBy",
                        new XAttribute("personId", person),
                        new XAttribute("programId", programm),
                        new XAttribute("date", Zeitstempel(_profil.Uhr()))));
            }

            // --------------------------------------------------------------
            //  Hilfen
            // --------------------------------------------------------------

            /// <summary>Eine Kennung (<c>xsd:ID</c>): nicht leer, ein NCName, im Dokument eindeutig.</summary>
            private string Kennung(string kennung, string wo)
            {
                if (string.IsNullOrEmpty(kennung)) throw Fehler("Ein " + wo + " trägt keine Kennung.");
                try
                {
                    XmlConvert.VerifyNCName(kennung);
                }
                catch (XmlException)
                {
                    throw Fehler("Die Kennung „" + kennung + "\" (" + wo + ") ist kein NCName.");
                }
                if (!_kennungen.Add(kennung)) throw Fehler("Die Kennung „" + kennung + "\" (" + wo + ") steht zweimal im Dokument.");
                return kennung;
            }

            private static void Text(XElement ziel, string name, string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                ziel.Add(new XElement(NS + name, Klartext(text.Trim())));
            }

            /// <summary>Eine Größe mit optionaler Einheit; ohne Wert entfällt das Element.</summary>
            private static void Groesse(XElement ziel, string name, double? wert, string einheit)
            {
                if (!wert.HasValue) return;
                var e = new XElement(NS + name, Zahl(wert.Value));
                if (einheit != null) e.Add(new XAttribute("unit", einheit));
                ziel.Add(e);
            }

            private static InvalidOperationException Fehler(string text) => new InvalidOperationException("gbXML-Export: " + text);
        }

        /// <summary>Ein Text ohne Zeichen, die XML nicht tragen kann (Steuerzeichen, einzelne Ersatzhälften).</summary>
        internal static string Klartext(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            StringBuilder sb = null;
            for (int i = 0; i < text.Length; i++)
            {
                char z = text[i];
                if (char.IsHighSurrogate(z) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    sb?.Append(z).Append(text[i + 1]);
                    i++;
                    continue;
                }
                if (XmlConvert.IsXmlChar(z)) { sb?.Append(z); continue; }
                if (sb == null) sb = new StringBuilder(text, 0, i, text.Length);
            }
            return sb == null ? text : sb.ToString();
        }
    }
}
