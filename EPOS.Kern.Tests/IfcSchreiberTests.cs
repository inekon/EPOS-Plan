using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;
using M = Xbim.Ifc4.MeasureResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7c, Teil 1 — der IFC-Schreiber S1</b> (Datenaustauschkonzept 6.2 bis 6.5, Proben 10, 11, 12,
    /// 22, 23): Validator mit Attributprüfung, deterministische Kennungen und Datei, Umlaute über das
    /// Part-21-Escape, Einheitenzuweisung gegen die geschriebenen Maßtypen, Rundlauf über den eigenen
    /// <see cref="IfcLeser"/>, Ein- und Mehrzonenfall, Raumgrenzen, Formatwahl des Profils.
    /// </summary>
    public sealed class IfcSchreiberTests : IDisposable
    {
        private const double Genau = 1e-9;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Vorrichtung
        // ==================================================================

        /// <summary>Das Schichtenhaus über den Ablauf (eine Zone, Platzhalter „unbeheizt").</summary>
        private static GebaeudeAbbild Einzonig(int zone = ExportSatzProbe.ZONE, int gebaeude = ExportSatzProbe.GEB, int versatz = 0)
        {
            ZoneModel z = ExportSatzProbe.Schichtenhaus(zone);
            z.ID_Gebaeude = gebaeude;
            foreach (BauteilModel b in z.Bauteile)
            {
                b.ID += versatz;
                b.ID_Zone = zone;
            }
            GebaeudeExportSatz satz = ExportSatzProbe.Satz(z, gebaeude: ExportSatzProbe.Gebaeude(gebaeude));
            return ExportSatzProbe.Plan(satz, IfcExportProbe.Profil()).Abbild;
        }

        /// <summary>Zwei Zonen mit einer Trennfläche (wie <c>GebaeudeExportAblaufTests</c>).</summary>
        private static GebaeudeAbbild Zweizonig()
        {
            ZoneModel a = ExportSatzProbe.Schichtenhaus();
            a.Nutzflaeche = 80.0;
            var b = new ZoneModel { ID = 602, ID_Gebaeude = ExportSatzProbe.GEB, Rang = 2, Bezeichner = "Anbau", Nutzflaeche = 40.0, IstBeheizt = true };
            a.Bauteile.Add(ExportSatzProbe.B(1013, DbWerte.BAUTEILART_INNENWAND, 12.0, DbWerte.RANDBEDINGUNG_ZONE,
                                             ExportSatzProbe.AUFBAU_DECKE, neigung: 90.0, nachbarzone: 602));
            return ExportSatzProbe.Plan(ExportSatzProbe.Satz(a, weitere: new[] { b }), IfcExportProbe.Profil()).Abbild;
        }

        private static List<string> GlobalIds(byte[] datei)
        {
            using (MemoryModel m = IfcExportProbe.Modell(datei))
                return m.Instances.OfType<IIfcRoot>().Select(r => r.GlobalId.ToString()).ToList();
        }

        // ==================================================================
        //  Probe 10 — Validator
        // ==================================================================

        [Fact]
        public void Probe10_Validator_meldet_nichts_und_die_Datei_entsteht()
        {
            byte[] datei = IfcExportProbe.Schreiben(Einzonig(), IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.True(datei.Length > 0);
            Assert.Equal(datei.Length, bilanz.Bytes);
            Assert.StartsWith("ISO-10303-21;", Encoding.ASCII.GetString(datei, 0, 13));
            using (MemoryModel m = IfcExportProbe.Modell(datei))
                Assert.Empty(IfcSchreiber.Pruefen(m));
        }

        [Fact]
        public void Probe10_eine_entfernte_GlobalId_wird_gemeldet_und_nichts_geschrieben()
        {
            Action<IModel> entfernen = m =>
            {
                using (ITransaction t = m.BeginTransaction("Probe 10"))
                {
                    var wand = m.Instances.OfType<Xbim.Ifc4.SharedBldgElements.IfcWall>().First();
                    wand.GlobalId = default;
                    t.Commit();
                }
            };
            byte[] datei = IfcExportProbe.Schreiben(Einzonig(), IfcExportProbe.Profil(), null, entfernen, out GebaeudeExportBilanz bilanz);
            Assert.Empty(datei);
            Assert.Equal(0, bilanz.Bytes);
            PruefMeldung schema = Assert.Single(bilanz.Meldungen, m => m.Schluessel == IfcSchreiber.SCHEMA);
            Assert.Contains("IfcWall", schema.Werte[0], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GlobalId", schema.Werte[1], StringComparison.Ordinal);
            PruefMeldung ende = Assert.Single(bilanz.Meldungen, m => m.Schluessel == IfcSchreiber.ABGEBROCHEN);
            Assert.Equal(PruefStufe.Fehler, ende.Stufe);
            Assert.Equal("1", ende.Werte[0]);
        }

        // ==================================================================
        //  Probe 11 — Kennungen
        // ==================================================================

        [Fact]
        public void Probe11_Version5_Pruefwerte()
        {
            // RFC 4122, Anhang-Beispiel (DNS-Namensraum) — derselbe Wert wie Pythons uuid.uuid5.
            Assert.Equal(new Guid("2ed6657d-e927-568b-95e1-2665a8aea6a2"),
                         IfcExportKennung.NamensUuid(new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8"), "www.example.com"));
            // Eingefroren: der EPOS-Namensraum und die Form der Schlüsselpfade.
            Assert.Equal(new Guid("3c9d2e71-5a4b-4f08-9b6e-e705c1a8d2f4"), IfcExportKennung.NAMENSRAUM);
            Assert.Equal(new Guid("c317238e-dab5-5102-8cbe-4a7854b72937"),
                         IfcExportKennung.NamensUuid(IfcExportKennung.NAMENSRAUM, "epos-gebaeude-7#Objekt"));
            Assert.Equal(new Guid("f3ca03c9-cede-5d5b-87a3-fce664f21499"),
                         IfcExportKennung.NamensUuid(IfcExportKennung.NAMENSRAUM, "epos-bauteil-101#SpaceBoundary:2a:epos-raum-11:0"));
            string id = new IfcExportKennung().Vergeben("epos-gebaeude-7", "Objekt").ToString();
            Assert.Equal(22, id.Length);
            Assert.Equal(new Guid("c317238e-dab5-5102-8cbe-4a7854b72937"), Xbim.Ifc4.UtilityResource.IfcGloballyUniqueId.ConvertFromBase64(id));
        }

        [Fact]
        public void Probe11_zweiter_Export_gleiche_GlobalIds_dupliziertes_Projekt_andere()
        {
            List<string> erster = GlobalIds(IfcExportProbe.Schreiben(Einzonig()));
            List<string> zweiter = GlobalIds(IfcExportProbe.Schreiben(Einzonig()));
            Assert.Equal(erster, zweiter);
            Assert.Equal(erster.Count, erster.Distinct(StringComparer.Ordinal).Count());
            Assert.True(erster.Count > 50, "zu wenige IfcRoot-Instanzen: " + erster.Count);
            // Objekte, Sätze, Mengen, Beziehungen und Raumgrenzen tragen alle eine Kennung.
            using (MemoryModel m = IfcExportProbe.Modell(IfcExportProbe.Schreiben(Einzonig())))
            {
                Assert.All(m.Instances.OfType<IIfcRoot>(), r => Assert.Equal(22, r.GlobalId.ToString().Length));
                Assert.All(m.Instances.OfType<IIfcRoot>(), r => Assert.NotNull(r.OwnerHistory));
                Assert.Single(m.Instances.OfType<IIfcOwnerHistory>());
            }

            List<string> kopie = GlobalIds(IfcExportProbe.Schreiben(Einzonig(zone: 9011, gebaeude: 9007, versatz: 50000)));
            Assert.Empty(erster.Intersect(kopie, StringComparer.Ordinal));
        }

        // ==================================================================
        //  Probe 12 — Datei
        // ==================================================================

        [Fact]
        public void Probe12_zweimal_byte_gleich_ausser_dem_Zeitstempel()
        {
            byte[] a = IfcExportProbe.Schreiben(Einzonig());
            byte[] b = IfcExportProbe.Schreiben(Einzonig());
            Assert.Equal(a, b);

            byte[] c = IfcExportProbe.Schreiben(Einzonig(), IfcExportProbe.Profil(new DateTime(2027, 1, 2, 3, 4, 5)));
            string[] za = Encoding.UTF8.GetString(a).Split("\r\n");
            string[] zc = Encoding.UTF8.GetString(c).Split("\r\n");
            Assert.Equal(za.Length, zc.Length);
            List<int> anders = Enumerable.Range(0, za.Length).Where(i => za[i] != zc[i]).ToList();
            // Eine Ablesung der Uhr, zwei Pflichtplätze: FILE_NAME im Kopf und CreationDate der einen IfcOwnerHistory.
            Assert.Equal(2, anders.Count);
            Assert.StartsWith("FILE_NAME (", za[anders[0]]);
            Assert.Contains("2026-09-26T12:00:00", za[anders[0]]);
            Assert.Contains("2027-01-02T03:04:05", zc[anders[0]]);
            Assert.Contains("IFCOWNERHISTORY(", za[anders[1]]);
        }

        [Fact]
        public void Kopf_ohne_MVD_mit_IFC4()
        {
            string text = Encoding.UTF8.GetString(IfcExportProbe.Schreiben(Einzonig()));
            Assert.DoesNotContain("ViewDefinition", text);
            Assert.Contains("FILE_SCHEMA (('IFC4'));", text);
            Assert.DoesNotContain("IFCMATERIALLAYERSETUSAGE", text);
            Assert.DoesNotContain("IFCBUILDINGSTOREY", text);
            Assert.DoesNotContain("IFCCONNECTION", text);
        }

        // ==================================================================
        //  Probe 22 — Umlaute
        // ==================================================================

        [Theory]
        [InlineData("de-DE", "Außenwände (zusammengefasst)")]
        [InlineData("en-US", "External walls (combined)")]
        public void Probe22_Umlaute_und_Ausweis_zeichenweise(string sprache, string erwartet)
        {
            GebaeudeAbbild abbild = GbxmlExportProbe.Abbild();
            AbbildBauteil wand = abbild.Gebaeude[0].Bauteile.First(b => b.Art == Bauteilart.Aussenwand);
            wand.Kennung = GebaeudeExportKennung.KlassenBauteil(GbxmlExportProbe.GEB, 1);
            GebaeudeExportProfil profil = IfcExportProbe.Profil(sprache: sprache);
            byte[] datei = IfcExportProbe.Schreiben(abbild, profil);

            Assert.All(datei, b => Assert.True(b < 0x80, "Nicht-ASCII-Byte in der STEP-Datei"));
            string text = Encoding.ASCII.GetString(datei);
            // Part 21: Zeichen bis U+00FF als \X\hh, darüber als \X2\…\X0\ — xBIM wählt selbst.
            if (sprache == "de-DE") Assert.Contains("Au\\X\\DFenw\\X\\E4nde (zusammengefasst)", text);

            string ausweis = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(nameof(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007), profil.Sprache);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcWall w = m.Instances.OfType<IIfcWall>().Single(x => x.Tag == wand.Kennung);
                Assert.Equal(erwartet.ToCharArray(), w.Name.ToString().ToCharArray());
                Dictionary<string, Dictionary<string, IIfcPropertySingleValue>> e = IfcExportProbe.Eigenschaften(w);
                Assert.Equal(true, e[IfcSchreiber.EPOS_BAUTEIL]["IstZusammenfassung"].NominalValue.Value);
                Assert.Equal(wand.Name, w.Description.ToString());

                IIfcBuilding g = m.Instances.OfType<IIfcBuilding>().Single();
                string validierung = IfcExportProbe.Eigenschaften(g)[IfcSchreiber.EPOS_RECHENLAUF]["Validierung"].NominalValue.ToString();
                Assert.Equal(ausweis.ToCharArray(), validierung.ToCharArray());
            }
        }

        // ==================================================================
        //  Probe 23 — Einheiten
        // ==================================================================

        [Fact]
        public void Probe23_Einheitenzuweisung_gegen_die_geschriebenen_Masstypen()
        {
            GebaeudeAbbild abbild = Einzonig();
            AbbildGebaeude geb = abbild.Gebaeude[0];
            var ergebnisse = new IfcErgebnisse { Rechenzeitpunkt = new DateTime(2026, 9, 25, 8, 0, 0), Wetterdatensatz = "PVGIS-TMY Probe" };
            // Teil 2: Kältebedarf (Energie, KILOWATTHOUR) und Kältelast (Leistung) an Gebäude und Raum.
            ergebnisse.JeKennung[geb.Kennung] = new IfcErgebnis(12000.0, 8000.0, KaeltebedarfKwh: 12000.0, KaeltelastW: 3000.0);
            ergebnisse.JeKennung[geb.Raeume[0].Kennung] = new IfcErgebnis(12000.0, 8000.0, 20.5, 26.0, 12000.0, 3000.0);
            byte[] datei = IfcExportProbe.Schreiben(abbild, IfcExportProbe.Profil(), ergebnisse, null, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Schluessel == IfcSchreiber.OHNE_ERGEBNIS);

            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcUnitAssignment zuweisung = m.Instances.OfType<IIfcProject>().Single().UnitsInContext;
                var si = zuweisung.Units.OfType<IIfcSIUnit>().ToDictionary(u => u.UnitType, u => u);
                Assert.Equal(7, zuweisung.Units.Count());
                Assert.Equal(7, si.Count);
                Assert.All(si.Values, u => Assert.Null(u.Prefix));
                Assert.Equal(IfcSIUnitName.METRE, si[IfcUnitEnum.LENGTHUNIT].Name);
                Assert.Equal(IfcSIUnitName.SQUARE_METRE, si[IfcUnitEnum.AREAUNIT].Name);
                Assert.Equal(IfcSIUnitName.CUBIC_METRE, si[IfcUnitEnum.VOLUMEUNIT].Name);
                Assert.Equal(IfcSIUnitName.JOULE, si[IfcUnitEnum.ENERGYUNIT].Name);
                Assert.Equal(IfcSIUnitName.WATT, si[IfcUnitEnum.POWERUNIT].Name);
                Assert.Equal(IfcSIUnitName.KELVIN, si[IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT].Name);
                Assert.Equal(IfcSIUnitName.RADIAN, si[IfcUnitEnum.PLANEANGLEUNIT].Name);

                IIfcConversionBasedUnit kwh = Assert.Single(m.Instances.OfType<IIfcConversionBasedUnit>(), u => u.Name == IfcSchreiber.KILOWATTSTUNDE);
                Assert.Equal(IfcUnitEnum.ENERGYUNIT, kwh.UnitType);
                Assert.Equal(3.6e6, Convert.ToDouble(kwh.ConversionFactor.ValueComponent.Value, CultureInfo.InvariantCulture));
                Assert.Equal(IfcSIUnitName.JOULE, ((IIfcSIUnit)kwh.ConversionFactor.UnitComponent).Name);

                List<IIfcPropertySingleValue> werte = m.Instances.OfType<IIfcPropertySingleValue>().ToList();
                List<IIfcPropertySingleValue> energie = werte.Where(p => p.NominalValue is M.IfcEnergyMeasure).ToList();
                Assert.Equal(4, energie.Count);
                Assert.Equal(2, energie.Count(p => p.Name == "Kaeltebedarf"));
                Assert.All(energie.Where(p => p.Name == "Kaeltebedarf"), p => Assert.Contains("Entfeuchtung", p.Description.ToString(), StringComparison.Ordinal));
                Assert.All(energie, p => Assert.Same(kwh, p.Unit));
                Assert.All(energie, p => Assert.Equal(12000.0, IfcExportProbe.Zahl(p)));
                // Winkel in Grad mit ausdrücklicher Einheit, Temperaturen in Kelvin ohne (globale Einheit).
                Assert.All(werte.Where(p => p.NominalValue is M.IfcPlaneAngleMeasure),
                           p => Assert.Equal(IfcSchreiber.GRAD, ((IIfcConversionBasedUnit)p.Unit).Name.ToString()));
                List<IIfcPropertySingleValue> temperaturen = werte.Where(p => p.NominalValue is M.IfcThermodynamicTemperatureMeasure).ToList();
                Assert.NotEmpty(temperaturen);
                Assert.All(temperaturen, p => Assert.Null(p.Unit));
                Assert.All(temperaturen, p => Assert.True(IfcExportProbe.Zahl(p) > 250.0));
                // Jede Messgröße, die ohne Unit steht, hat ihre Einheit in der Zuweisung.
                Assert.All(werte.Where(p => p.Unit == null && p.NominalValue is IIfcMeasureValue),
                           p => Assert.True(p.NominalValue is M.IfcThermodynamicTemperatureMeasure || p.NominalValue is M.IfcPowerMeasure
                                            || p.NominalValue is M.IfcThermalTransmittanceMeasure || p.NominalValue is M.IfcReal
                                            || p.NominalValue is M.IfcNormalisedRatioMeasure || p.NominalValue is M.IfcThermalConductivityMeasure
                                            || p.NominalValue is M.IfcSpecificHeatCapacityMeasure || p.NominalValue is M.IfcMassDensityMeasure
                                            || p.NominalValue is M.IfcThermalResistanceMeasure || p.NominalValue is M.IfcInteger
                                            || p.NominalValue is M.IfcNumericMeasure,
                                            p.Name + ": " + p.NominalValue.GetType().Name));

                IIfcBuilding g = m.Instances.OfType<IIfcBuilding>().Single();
                Dictionary<string, IIfcPropertySingleValue> lauf = IfcExportProbe.Eigenschaften(g)[IfcSchreiber.EPOS_RECHENLAUF];
                Assert.Equal("2026-09-25T08:00:00", lauf["Rechenzeitpunkt"].NominalValue.ToString());
                Assert.Equal("PVGIS-TMY Probe", lauf["Wetterdatensatz"].NominalValue.ToString());
                IIfcSpace raum = m.Instances.OfType<IIfcSpace>().Single(s => s.LongName == "Wohnen");
                Dictionary<string, IIfcPropertySingleValue> erg = IfcExportProbe.Eigenschaften(raum)[IfcSchreiber.EPOS_ERGEBNIS];
                Assert.Equal(20.5 + 273.15, IfcExportProbe.Zahl(erg["RaumtemperaturMittel"]), 9);
                Assert.Equal(8000.0, IfcExportProbe.Zahl(erg["Heizlast"]));
            }
        }

        [Fact]
        public void Ohne_Ergebnisse_benannt_weggelassen()
        {
            byte[] datei = IfcExportProbe.Schreiben(Einzonig(), IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz bilanz);
            Assert.Contains(bilanz.Meldungen, m => m.Schluessel == IfcSchreiber.OHNE_ERGEBNIS && m.Stufe == PruefStufe.Info);
            string text = Encoding.ASCII.GetString(datei);
            Assert.DoesNotContain("'EPOS_Ergebnis'", text);
            Assert.DoesNotContain("'Rechenzeitpunkt'", text);
            Assert.Contains("'EPOS_Rechenlauf'", text);
            Assert.Contains(IfcSchreiber.KILOWATTSTUNDE, text);
        }

        // ==================================================================
        //  Standort
        // ==================================================================

        [Theory]
        [InlineData(13.74, 13L, 44L, 24L, 0L)]
        [InlineData(-13.74, -13L, -44L, -24L, 0L)]
        [InlineData(-0.5, 0L, -30L, 0L, 0L)]
        [InlineData(51.0504, 51L, 3L, 1L, 440000L)]
        public void Koordinaten_alle_Glieder_mit_demselben_Vorzeichen(double grad, long g, long m, long s, long mikro)
            => Assert.Equal(new List<long> { g, m, s, mikro }, IfcSchreiber.GradMinutenSekunden(grad));

        [Fact]
        public void Site_mit_Koordinaten_und_Postleitzahl()
        {
            GebaeudeAbbild abbild = GbxmlExportProbe.Abbild();
            abbild.BreiteGrad = 51.05;
            abbild.LaengeGrad = -13.74;
            byte[] datei = IfcExportProbe.Schreiben(abbild, IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, x => x.Schluessel == IfcSchreiber.OHNE_KOORDINATEN);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcSite site = m.Instances.OfType<IIfcSite>().Single();
                Assert.Equal(new long[] { 51, 3, 0, 0 }, ((IEnumerable<long>)site.RefLatitude.Value.Value).ToArray());
                Assert.All(((IEnumerable<long>)site.RefLongitude.Value.Value), x => Assert.True(x <= 0));
                Assert.Equal("01067", ((IIfcPostalAddress)site.SiteAddress).PostalCode.ToString());
            }
            IfcExportProbe.Schreiben(GbxmlExportProbe.Abbild(), IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz ohne);
            Assert.Contains(ohne.Meldungen, x => x.Schluessel == IfcSchreiber.OHNE_KOORDINATEN);
        }

        // ==================================================================
        //  Rundlauf über den eigenen Leser
        // ==================================================================

        [Fact]
        public void Rundlauf_ueber_den_IfcLeser()
        {
            GebaeudeAbbild quelle = Einzonig();
            byte[] datei = IfcExportProbe.Schreiben(quelle);
            GebaeudeAbbild zurueck = IfcExportProbe.Lesen(datei);
            Assert.DoesNotContain(zurueck.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            AbbildGebaeude q = quelle.Gebaeude[0];
            AbbildGebaeude z = Assert.Single(zurueck.Gebaeude);

            AbbildRaum qr = q.Raeume.Single(r => r.Beheizt);
            AbbildRaum zr = z.Raeume.Single(r => r.Name == qr.Name);
            Assert.Equal(qr.FlaecheM2.Value, zr.FlaecheM2.Value, Genau);
            Assert.Equal(qr.VolumenM3.Value, zr.VolumenM3.Value, Genau);
            Assert.Equal(qr.SollHeizenC.Value, zr.SollHeizenC.Value, 9);

            foreach (AbbildBauteil b in q.Bauteile)
            {
                AbbildBauteil x = z.Bauteile.SingleOrDefault(y => y.Name == b.Name);
                Assert.True(x != null, "Bauteil fehlt im Rundlauf: " + b.Name);
                Assert.Equal(b.BruttoflaecheM2.Value, x.BruttoflaecheM2.Value, Genau);
                if (b.UWertWm2K.HasValue) Assert.Equal(b.UWertWm2K.Value, x.UWertWm2K.Value, Genau);
                if (b.Aufbau != null && b.Aufbau.Schichten.Count > 0)
                {
                    IEnumerable<double> dicken = b.Aufbau.Schichten.Select(s => s.DickeM ?? 0.0);
                    if (b.Aufbau.Richtung == Schichtrichtung.InnenNachAussen) dicken = dicken.Reverse();
                    Assert.Equal(dicken.ToArray(), x.Aufbau.Schichten.Select(s => s.DickeM ?? 0.0).ToArray());
                }
                Assert.Equal(b.Oeffnungen.Count, x.Oeffnungen.Count);
            }

            // Was der Leser nicht liest, steht in den EPOS-Sätzen: Azimut, Neigung, Luftwechsel.
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                foreach (AbbildBauteil b in q.Bauteile.Concat(q.Bauteile.SelectMany(o => o.Oeffnungen)))
                {
                    IIfcElement e = m.Instances.OfType<IIfcElement>().Single(x => x.Tag == b.Kennung);
                    Dictionary<string, IIfcPropertySingleValue> eb = IfcExportProbe.Eigenschaften(e)[IfcSchreiber.EPOS_BAUTEIL];
                    if (b.AzimutGrad.HasValue) Assert.Equal(b.AzimutGrad.Value, IfcExportProbe.Zahl(eb["Azimut"]), Genau);
                    else Assert.False(eb.ContainsKey("Azimut"));
                    Assert.Equal(b.NeigungGrad.Value, IfcExportProbe.Zahl(eb["Neigung"]), Genau);
                    Assert.False(string.IsNullOrWhiteSpace(eb["Neigung"].Description.ToString()));
                }
                IIfcSpace s = m.Instances.OfType<IIfcSpace>().Single(x => x.LongName == qr.Name);
                Assert.Equal(qr.LuftwechselJeH.Value, IfcExportProbe.Zahl(IfcExportProbe.Eigenschaften(s)[IfcSchreiber.EPOS_ZONE]["LuftwechselInfiltration"]), Genau);
            }
        }

        // ==================================================================
        //  Ein- und Mehrzonenfall, Raumgrenzen
        // ==================================================================

        [Fact]
        public void Einzonenfall_ohne_IfcZone_mit_Platzhalter_unbeheizt()
        {
            using (MemoryModel m = IfcExportProbe.Modell(IfcExportProbe.Schreiben(Einzonig())))
            {
                Assert.Empty(m.Instances.OfType<IIfcZone>());
                Assert.Empty(m.Instances.OfType<IIfcRelAssignsToGroup>());
                List<IIfcSpace> raeume = m.Instances.OfType<IIfcSpace>().ToList();
                Assert.Equal(2, raeume.Count);
                IIfcSpace kalt = raeume.Single(r => !IfcExportProbe.Eigenschaften(r).ContainsKey("Pset_SpaceThermalRequirements"));
                Assert.Equal(false, IfcExportProbe.Eigenschaften(kalt)[IfcSchreiber.EPOS_ZONE]["IstBeheizt"].NominalValue.Value);
                IIfcSpace warm = raeume.Single(r => r != kalt);
                Assert.Equal(true, IfcExportProbe.Eigenschaften(warm)[IfcSchreiber.EPOS_ZONE]["IstBeheizt"].NominalValue.Value);
                Assert.False(IfcExportProbe.Eigenschaften(warm).ContainsKey(IfcSchreiber.EPOS_RECHENLAUF));
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEXP_IFC_RECHENMODELL_EINZONE,
                             IfcExportProbe.Eigenschaften(m.Instances.OfType<IIfcBuilding>().Single())[IfcSchreiber.EPOS_RECHENLAUF]["Rechenmodell"].NominalValue.ToString());

                // Struktur: Projekt → Site → Gebäude → Räume, Bauteile im Gebäude (in Stufe S3 dazu die Kennzeichnung), kein Geschoss.
                IIfcBuilding g = m.Instances.OfType<IIfcBuilding>().Single();
                Assert.Equal(2, g.IsDecomposedBy.SelectMany(r => r.RelatedObjects).Count());
                Assert.Equal(m.Instances.OfType<IIfcElement>().Count(e => !(e is IIfcOpeningElement)) + m.Instances.OfType<IIfcAnnotation>().Count(),
                             g.ContainsElements.SelectMany(r => r.RelatedElements).Count());
                Assert.All(m.Instances.OfType<IIfcRelAssociatesMaterial>(), r => Assert.IsAssignableFrom<IIfcMaterialLayerSet>(r.RelatingMaterial));
            }
        }

        [Fact]
        public void Raumgrenzen_logisch_aussen_erdreich_innen_wechselseitig()
        {
            GebaeudeAbbild abbild = Einzonig();
            using (MemoryModel m = IfcExportProbe.Modell(IfcExportProbe.Schreiben(abbild)))
            {
                List<IIfcRelSpaceBoundary2ndLevel> grenzen = m.Instances.OfType<IIfcRelSpaceBoundary2ndLevel>().ToList();
                Assert.NotEmpty(grenzen);
                Assert.All(grenzen, r =>
                {
                    Assert.Null(r.ConnectionGeometry);
                    Assert.NotNull(r.RelatedBuildingElement);
                    Assert.Equal(IfcPhysicalOrVirtualEnum.PHYSICAL, r.PhysicalOrVirtualBoundary);
                    Assert.Equal("2ndLevel", r.Name.ToString());
                    Assert.Contains(r.Description.ToString(), new[] { "2a", "2b" });
                    if (r.InternalOrExternalBoundary == IfcInternalOrExternalEnum.INTERNAL && r.CorrespondingBoundary != null)
                        Assert.Same(r, r.CorrespondingBoundary.CorrespondingBoundary);
                });
                IIfcElement aussen = m.Instances.OfType<IIfcElement>().Single(e => e.Tag == "epos-bauteil-1001");
                Assert.Equal(IfcInternalOrExternalEnum.EXTERNAL, grenzen.Single(r => r.RelatedBuildingElement == aussen).InternalOrExternalBoundary);
                IIfcElement boden = m.Instances.OfType<IIfcSlab>().Single(e => e.PredefinedType == IfcSlabTypeEnum.BASESLAB);
                Assert.Equal(IfcInternalOrExternalEnum.EXTERNAL_EARTH, grenzen.Single(r => r.RelatedBuildingElement == boden).InternalOrExternalBoundary);
                IIfcElement kalt = m.Instances.OfType<IIfcElement>().Single(e => e.Tag == "epos-bauteil-1009");
                List<IIfcRelSpaceBoundary2ndLevel> paar = grenzen.Where(r => r.RelatedBuildingElement == kalt).ToList();
                Assert.Equal(2, paar.Count);
                Assert.All(paar, r => Assert.Equal(IfcInternalOrExternalEnum.INTERNAL, r.InternalOrExternalBoundary));
                Assert.Same(paar[1], paar[0].CorrespondingBoundary);
                Assert.Same(paar[0], paar[1].CorrespondingBoundary);
                Assert.NotSame(paar[0].RelatingSpace, paar[1].RelatingSpace);
                Assert.Equal(IfcSchreiber.RAUMGRENZE_LOGISCH,
                             IfcExportProbe.Eigenschaften(kalt)[IfcSchreiber.EPOS_BAUTEIL]["Raumgrenze"].NominalValue.ToString());
            }
        }

        [Fact]
        public void Mehrzonenfall_mit_IfcZone_und_Trennflaeche()
        {
            using (MemoryModel m = IfcExportProbe.Modell(IfcExportProbe.Schreiben(Zweizonig())))
            {
                List<IIfcZone> zonen = m.Instances.OfType<IIfcZone>().ToList();
                Assert.Equal(2, zonen.Count);
                Assert.All(zonen, z => Assert.Single(Assert.Single(z.IsGroupedBy).RelatedObjects.OfType<IIfcSpace>()));
                IIfcElement trenn = m.Instances.OfType<IIfcElement>().Single(e => e.Tag == "epos-bauteil-1013");
                List<IIfcRelSpaceBoundary2ndLevel> paar = m.Instances.OfType<IIfcRelSpaceBoundary2ndLevel>().Where(r => r.RelatedBuildingElement == trenn).ToList();
                Assert.Equal(2, paar.Count);
                Assert.Same(paar[1], paar[0].CorrespondingBoundary);
                Assert.Same(paar[0], paar[1].CorrespondingBoundary);
                Assert.Equal(2, paar.Select(r => r.RelatingSpace).Distinct().Count());
                // Der Ausweis je Zone (6.5): Rechenmodell und Validierung an jedem beheizten Raum.
                foreach (IIfcSpace s in m.Instances.OfType<IIfcSpace>().Where(s => IfcExportProbe.Eigenschaften(s).ContainsKey("Pset_SpaceThermalRequirements")))
                {
                    Dictionary<string, IIfcPropertySingleValue> lauf = IfcExportProbe.Eigenschaften(s)[IfcSchreiber.EPOS_RECHENLAUF];
                    Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEXP_IFC_RECHENMODELL_MEHRZONE, lauf["Rechenmodell"].NominalValue.ToString());
                    Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007, lauf["Validierung"].NominalValue.ToString());
                }
            }
        }

        [Fact]
        public void Ersatzschichtung_gekennzeichnet_und_Bilanz()
        {
            GebaeudeAbbild abbild = GbxmlExportProbe.Abbild();
            byte[] datei = IfcExportProbe.Schreiben(abbild, IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz bilanz);
            AbbildGebaeude g = abbild.Gebaeude[0];
            Assert.Equal(g.Bauteile.Count, bilanz.Flaechen);
            Assert.Equal(g.Bauteile.Sum(b => b.Oeffnungen.Count), bilanz.Oeffnungen);
            Assert.Equal(1, bilanz.Ersatzaufbauten);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                Assert.Equal(bilanz.Aufbauten, m.Instances.OfType<IIfcMaterialLayerSet>().Count());
                AbbildBauteil ersatz = g.Bauteile.Single(b => b.Aufbau.IstErsatz);
                IIfcElement e = m.Instances.OfType<IIfcElement>().Single(x => x.Tag == ersatz.Kennung);
                Assert.Equal(true, IfcExportProbe.Eigenschaften(e)[IfcSchreiber.EPOS_BAUTEIL]["Ersatzschichtung"].NominalValue.Value);
                IIfcMaterialLayerSet satz = (IIfcMaterialLayerSet)e.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Single().RelatingMaterial;
                Assert.Equal(ersatz.Aufbau.Name, satz.LayerSetName.ToString());
                // Die Außenwand steht innen → außen im Abbild; in der Datei liegt die erste Schicht außen.
                AbbildBauteil wand = g.Bauteile.First(b => b.Art == Bauteilart.Aussenwand);
                IIfcElement w = m.Instances.OfType<IIfcElement>().Single(x => x.Tag == wand.Kennung);
                IIfcMaterialLayerSet ws = (IIfcMaterialLayerSet)w.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Single().RelatingMaterial;
                Assert.Equal("Kalkzementputz", ws.MaterialLayers.First().Material.Name.ToString());
                Assert.Equal(IfcSchreiber.SCHICHTRICHTUNG_AUSSEN_INNEN,
                             IfcExportProbe.Eigenschaften(w)[IfcSchreiber.EPOS_BAUTEIL]["Schichtrichtung"].NominalValue.ToString());
            }
        }

        // ==================================================================
        //  Formatwahl
        // ==================================================================

        [Fact]
        public void Formatwahl_Vorgabe_gbXML_und_IFC_auf_Wunsch()
        {
            GebaeudeExportProfil vorgabe = GbxmlExportProbe.Profil();
            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, vorgabe.Format);
            Assert.IsType<GbxmlSchreiber>(vorgabe.SchreiberErzeugen());
            Assert.Equal(GebaeudeExportProfil.DATEIFILTER, vorgabe.Dateifilter);

            GebaeudeExportProfil ifc = IfcExportProbe.Profil();
            Assert.Equal(GebaeudeQuelle.FORMAT_IFC, ifc.Format);
            Assert.IsType<IfcSchreiber>(ifc.SchreiberErzeugen());
            Assert.Equal(GebaeudeExportProfil.DATEIFILTER_IFC, ifc.Dateifilter);

            Assert.Throws<ArgumentException>(() => new GebaeudeExportProfil(CultureInfo.InvariantCulture, () => DateTime.Now, false, "1", format: "PDF"));

            // Über den Ablauf: derselbe Plan, das Format aus dem Profil.
            GebaeudeExportPlan plan = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), ifc);
            using (var ziel = new MemoryStream())
            {
                GebaeudeExportBilanz b = new GebaeudeExportAblauf().Schreiben(plan, ziel, ifc, System.Threading.CancellationToken.None);
                Assert.StartsWith("ISO-10303-21;", Encoding.ASCII.GetString(ziel.ToArray(), 0, 13));
                Assert.True(b.Flaechen > 0 && b.Aufbauten > 0 && b.Oeffnungen > 0);
            }
        }
    }
}
