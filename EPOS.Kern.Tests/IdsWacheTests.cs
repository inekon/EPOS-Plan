using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache der Exportzusage</b> (Stufe G7c, Teil 2; Datenaustauschkonzept 6.4): Die IDS-Datei der
    /// Auslieferung <c>Setup/Vorlage/EPOS_Export.ids</c> (IDS 1.0) wird gegen frisch geschriebene IFC-Dateien des
    /// Probenabbilds gehalten — selbst geprüft über xBIM, ohne fremden IDS-Prüfer: Jede Spezifikation findet ihre
    /// Entität so oft, wie <c>applicability</c> verlangt; jede Anforderung <c>required</c> steht an jeder Instanz
    /// (Attribut belegt, gegebenenfalls mit dem genannten Wert; Eigenschaft oder Menge im genannten Satz mit dem
    /// genannten Datentyp); eine Anforderung <c>optional</c> hat, wo sie steht, den genannten Datentyp. Dazu die
    /// Gegenrichtung: Jeder eigene Satz (<c>EPOS_*</c>) und jeder Standardsatz der Datei steht in der IDS.
    /// </summary>
    public sealed class IdsWacheTests : IDisposable
    {
        private static readonly XNamespace IDS = "http://standards.buildingsmart.org/IDS";
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        /// <summary>Eine Anforderung der IDS.</summary>
        private sealed record Anforderung(bool Attribut, string Satz, string Name, string Datentyp, string Wert, bool Pflicht);

        /// <summary>Eine Spezifikation der IDS.</summary>
        private sealed record Spezifikation(string Name, string Entitaet, int Mindestens, List<Anforderung> Anforderungen);

        // ==================================================================
        //  Die Proben
        // ==================================================================

        /// <summary>Die Probendateien: ein- und zweizonig, mit und ohne Ergebnis, mit Wärmebrücke, Baujahr und Klimaort.</summary>
        private static IEnumerable<(string Name, byte[] Datei)> Proben()
        {
            GebaeudeExportProfil profil = IfcExportProbe.Profil();
            ZoneModel z = ExportSatzProbe.Schichtenhaus();
            z.Bauteile.Single(b => b.ID == 1001).Psi_L = 2.0;
            ProjektGebaeudeModel geb = ExportSatzProbe.Gebaeude();
            geb.Baujahr = 1990;
            geb.Luftwechsel_Nutzer = 0.5;
            var ergebnis = new ErgebnisGebaeudeModel { ID_Gebaeude = ExportSatzProbe.GEB, HeizwaermeMwh = 10.0, SpitzeKw = 7.0,
                                                       MittlereRaumtemperaturC = 20.5, KuehlenergieMwh = 2.0 };
            GebaeudeExportSatz einzonig = ExportSatzProbe.Satz(z, gebaeude: geb, kuehlbetrieb: true);
            yield return ("einzonig ohne Ergebnis", Datei(einzonig, profil));
            yield return ("einzonig mit Ergebnis", Datei(einzonig.MitErgebnis(ergebnis, new DateTime(2026, 9, 30), "Probenregion"), profil));

            ZoneModel a = ExportSatzProbe.Schichtenhaus();
            a.Nutzflaeche = 80.0;
            var b = new ZoneModel { ID = 602, ID_Gebaeude = ExportSatzProbe.GEB, Rang = 2, Bezeichner = "Anbau", Nutzflaeche = 40.0, IstBeheizt = true };
            a.Bauteile.Add(ExportSatzProbe.B(1013, DbWerte.BAUTEILART_INNENWAND, 12.0, DbWerte.RANDBEDINGUNG_ZONE,
                                             ExportSatzProbe.AUFBAU_DECKE, neigung: 90.0, nachbarzone: 602));
            yield return ("zweizonig ohne Ergebnis", Datei(ExportSatzProbe.Satz(a, weitere: new[] { b }), profil));
            yield return ("U-Wert-Haus", Datei(ExportSatzProbe.Satz(ExportSatzProbe.UWertHaus()), profil));
        }

        private static byte[] Datei(GebaeudeExportSatz satz, GebaeudeExportProfil profil)
            => ExportSatzProbe.Datei(ExportSatzProbe.Plan(satz, profil), profil);

        // ==================================================================
        //  Die Wache
        // ==================================================================

        [Fact]
        public void Die_IDS_ist_IDS_1_0_fuer_IFC4_mit_deutscher_Beschreibung()
        {
            XDocument d = XDocument.Load(IdsPfad());
            Assert.Equal(IDS + "ids", d.Root.Name);
            XElement info = d.Root.Element(IDS + "info");
            Assert.False(string.IsNullOrWhiteSpace((string)info.Element(IDS + "title")));
            Assert.Contains("Exportzusage", (string)info.Element(IDS + "title"), StringComparison.Ordinal);
            Assert.Contains("ohne Geometrie", (string)info.Element(IDS + "description"), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace((string)info.Element(IDS + "purpose")));
            List<XElement> spez = d.Root.Element(IDS + "specifications").Elements(IDS + "specification").ToList();
            Assert.All(spez, s => Assert.Equal("IFC4", (string)s.Attribute("ifcVersion")));
            Assert.All(spez, s => Assert.False(string.IsNullOrWhiteSpace((string)s.Attribute("description"))));
            Assert.Equal(new[] { "IFCPROJECT", "IFCSITE", "IFCBUILDING", "IFCSPACE", "IFCWALL", "IFCSLAB", "IFCWINDOW", "IFCDOOR", "IFCMATERIAL" },
                         Spezifikationen().Select(s => s.Entitaet).ToArray());
            // Keine Geometrieanforderung: kein Attribut Representation, ObjectPlacement oder ConnectionGeometry.
            Assert.DoesNotContain(Spezifikationen().SelectMany(s => s.Anforderungen),
                                  a => a.Attribut && (a.Name == "Representation" || a.Name == "ObjectPlacement" || a.Name == "ConnectionGeometry"));
        }

        [Fact]
        public void Jede_Anforderung_der_IDS_steht_an_jeder_Instanz_der_Probendateien()
        {
            List<Spezifikation> spez = Spezifikationen();
            var funde = new List<string>();
            int geprueft = 0;
            foreach ((string probe, byte[] datei) in Proben())
                using (MemoryModel m = IfcExportProbe.Modell(datei))
                    foreach (Spezifikation s in spez)
                    {
                        List<IPersistEntity> instanzen = m.Instances.Where(i => i.ExpressType.ExpressNameUpper == s.Entitaet).ToList();
                        if (instanzen.Count < s.Mindestens) funde.Add(probe + ": " + s.Entitaet + " fehlt");
                        foreach (IPersistEntity i in instanzen)
                            foreach (Anforderung a in s.Anforderungen)
                            {
                                geprueft++;
                                string befund = Pruefen(m, i, a);
                                if (befund != null) funde.Add(probe + ": " + s.Entitaet + " #" + i.EntityLabel + " " + a.Satz + "." + a.Name + " — " + befund);
                            }
                    }
            Assert.True(funde.Count == 0, string.Join("\n", funde.Take(40)));
            Assert.True(geprueft > 1000, "Nur " + geprueft + " Prüfungen.");
        }

        [Fact]
        public void Jeder_Satz_und_jede_Eigenschaft_der_Probendateien_steht_in_der_IDS()
        {
            var bekannt = new HashSet<string>(Spezifikationen().SelectMany(s => s.Anforderungen.Where(a => !a.Attribut)
                                                                                     .Select(a => s.Entitaet + ":" + a.Satz + "." + a.Name)),
                                              StringComparer.Ordinal);
            var fehlend = new SortedSet<string>(StringComparer.Ordinal);
            foreach ((string _, byte[] datei) in Proben())
                using (MemoryModel m = IfcExportProbe.Modell(datei))
                    foreach (IPersistEntity i in m.Instances)
                    {
                        string ent = i.ExpressType.ExpressNameUpper;
                        if (!Spezifikationen().Any(s => s.Entitaet == ent)) continue;
                        foreach ((string satz, string name, _) in Werte(m, i))
                            if (!bekannt.Contains(ent + ":" + satz + "." + name)) fehlend.Add(ent + ":" + satz + "." + name);
                    }
            Assert.True(fehlend.Count == 0, "Nicht in der IDS: " + string.Join(", ", fehlend));
        }

        /// <summary>Gegenprobe: Eine Datei, an der eine Pflichteigenschaft fehlt, fällt durch.</summary>
        [Fact]
        public void Gegenprobe_eine_fehlende_Pflichteigenschaft_wird_gemeldet()
        {
            Spezifikation wand = Spezifikationen().Single(s => s.Entitaet == "IFCWALL");
            var erfunden = new Anforderung(false, "EPOS_Bauteil", "GibtEsNicht", "IFCLABEL", null, true);
            var falscherTyp = new Anforderung(false, "EPOS_Bauteil", "Kennung", "IFCLABEL", null, true);
            var falscherWert = new Anforderung(true, null, "PredefinedType", null, "ROOF", true);
            using (MemoryModel m = IfcExportProbe.Modell(Proben().First().Datei))
            {
                IPersistEntity w = m.Instances.First(i => i.ExpressType.ExpressNameUpper == wand.Entitaet);
                Assert.NotNull(Pruefen(m, w, erfunden));
                Assert.NotNull(Pruefen(m, w, falscherTyp));
                IPersistEntity slab = m.Instances.First(i => i.ExpressType.ExpressNameUpper == "IFCSLAB" && ((IIfcSlab)i).PredefinedType != IfcSlabTypeEnum.ROOF);
                Assert.NotNull(Pruefen(m, slab, falscherWert));
                Assert.Null(Pruefen(m, w, wand.Anforderungen.First(a => a.Name == "Kennung")));
            }
        }

        // ==================================================================
        //  Prüfen
        // ==================================================================

        /// <summary>Prüft eine Anforderung an einer Instanz; <c>null</c> = erfüllt, sonst der Befund.</summary>
        private static string Pruefen(IModel m, IPersistEntity i, Anforderung a)
        {
            if (a.Attribut)
            {
                PropertyInfo p = i.GetType().GetProperty(a.Name);
                if (p == null) return "Attribut unbekannt";
                object w = p.GetValue(i);
                string text = w?.ToString();
                if (string.IsNullOrWhiteSpace(text)) return a.Pflicht ? "Attribut leer" : null;
                if (a.Wert != null && !string.Equals(text, a.Wert, StringComparison.OrdinalIgnoreCase)) return "Wert " + text + " statt " + a.Wert;
                return null;
            }
            List<(string Satz, string Name, string Typ)> treffer = Werte(m, i).Where(x => x.Satz == a.Satz && x.Name == a.Name).ToList();
            if (treffer.Count == 0) return a.Pflicht ? "fehlt" : null;
            if (treffer.Count > 1) return "mehrfach";
            if (a.Datentyp != null && treffer[0].Typ != a.Datentyp) return "Datentyp " + treffer[0].Typ + " statt " + a.Datentyp;
            return null;
        }

        /// <summary>Die Eigenschaften und Mengen einer Instanz: Satz, Name und Datentyp (IDS-Schreibweise).</summary>
        private static IEnumerable<(string Satz, string Name, string Typ)> Werte(IModel m, IPersistEntity i)
        {
            if (i is IIfcObject o)
            {
                foreach (IIfcRelDefinesByProperties rel in o.IsDefinedBy)
                {
                    if (rel.RelatingPropertyDefinition is IIfcPropertySet ps)
                        foreach (IIfcPropertySingleValue p in ps.HasProperties.OfType<IIfcPropertySingleValue>())
                            yield return (ps.Name.ToString(), p.Name.ToString(), p.NominalValue?.GetType().Name.ToUpperInvariant());
                    else if (rel.RelatingPropertyDefinition is IIfcElementQuantity q)
                        foreach (IIfcPhysicalQuantity x in q.Quantities)
                            yield return (q.Name.ToString(), x.Name.ToString(), x switch
                            {
                                IIfcQuantityArea _ => "IFCAREAMEASURE",
                                IIfcQuantityLength _ => "IFCLENGTHMEASURE",
                                IIfcQuantityVolume _ => "IFCVOLUMEMEASURE",
                                _ => x.GetType().Name.ToUpperInvariant(),
                            });
                }
            }
            else if (i is IIfcMaterial stoff)
            {
                foreach (IIfcMaterialProperties mp in m.Instances.OfType<IIfcMaterialProperties>().Where(x => x.Material == stoff))
                    foreach (IIfcPropertySingleValue p in mp.Properties.OfType<IIfcPropertySingleValue>())
                        yield return (mp.Name.ToString(), p.Name.ToString(), p.NominalValue?.GetType().Name.ToUpperInvariant());
            }
        }

        // ==================================================================
        //  Die IDS lesen
        // ==================================================================

        private static List<Spezifikation> Spezifikationen()
        {
            XDocument d = XDocument.Load(IdsPfad());
            var r = new List<Spezifikation>();
            foreach (XElement s in d.Root.Element(IDS + "specifications").Elements(IDS + "specification"))
            {
                XElement anw = s.Element(IDS + "applicability");
                string entitaet = (string)anw.Element(IDS + "entity").Element(IDS + "name").Element(IDS + "simpleValue");
                var anf = new List<Anforderung>();
                foreach (XElement f in s.Element(IDS + "requirements").Elements())
                {
                    bool pflicht = ((string)f.Attribute("cardinality") ?? "required") == "required";
                    if (f.Name == IDS + "attribute")
                        anf.Add(new Anforderung(true, null, (string)f.Element(IDS + "name").Element(IDS + "simpleValue"), null,
                                                (string)f.Element(IDS + "value")?.Element(IDS + "simpleValue"), pflicht));
                    else if (f.Name == IDS + "property")
                        anf.Add(new Anforderung(false, (string)f.Element(IDS + "propertySet").Element(IDS + "simpleValue"),
                                                (string)f.Element(IDS + "baseName").Element(IDS + "simpleValue"), (string)f.Attribute("dataType"),
                                                null, pflicht));
                    else throw new InvalidOperationException("Unbekannte Anforderung " + f.Name);
                }
                r.Add(new Spezifikation((string)s.Attribute("name"), entitaet, (int)anw.Attribute("minOccurs"), anf));
            }
            return r;
        }

        /// <summary>Der Pfad der IDS-Datei im Arbeitsbaum (über den eigenen Quellort, sonst vom Laufordner aufwärts).</summary>
        private static string IdsPfad([CallerFilePath] string eigeneDatei = null)
        {
            var kandidaten = new List<DirectoryInfo>();
            if (!string.IsNullOrEmpty(eigeneDatei)) kandidaten.Add(new FileInfo(eigeneDatei).Directory?.Parent);
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent) kandidaten.Add(d);
            foreach (DirectoryInfo d in kandidaten.Where(x => x != null))
            {
                string pfad = Path.Combine(d.FullName, "Setup", "Vorlage", GebaeudeExportAblauf.IDS_DATEI);
                if (File.Exists(pfad)) return pfad;
            }
            throw new FileNotFoundException("Setup/Vorlage/" + GebaeudeExportAblauf.IDS_DATEI + " nicht gefunden.");
        }
    }
}
