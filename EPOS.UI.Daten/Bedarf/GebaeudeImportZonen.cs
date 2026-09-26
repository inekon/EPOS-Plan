using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Zonierung eines Gebäudeimports als Daten des Dialogs</b> (Stufe G6c, Welle C;
    /// Mehrzonenkonzept 6.4) — plattformfrei neben der <see cref="GebaeudeImportHuelle"/>, ohne
    /// Datenbank. Sie übersetzt <see cref="GebaeudeZonierung"/> und den Bauteilvorschlag in die DTO des
    /// <c>GebaeudeImportDialog</c>: die wählbaren Regeln, die Bilanz (Zonen, beheizte Fläche, beheiztes
    /// Volumen, Σ Außenfläche, Σ Trennfläche), die schwerste Meldung, die Obergrenze mit dem Vorschlag
    /// einer gröberen Regel, die Zonen samt Räumen und die Flächen je Zone als Zeilen einer
    /// <c>Katalogliste</c>.
    ///
    /// <para><b>Nur mit Wahl.</b> Trägt die Datei für das Gebäude nur eine Regel (eine Zone je Gebäude),
    /// gibt es keine Zonierungsdaten — der Dialog zeigt den Einzonenweg, wie er ist. Ergibt die gewählte
    /// Regel nur eine Zone, ist <see cref="GebaeudeZonierungDaten.Einzonig"/> gesetzt und es gibt weder
    /// Zonen- noch Flächenliste.</para>
    ///
    /// <para><b>Handänderungen an Zonen</b> trägt der Kern nicht (Welle A kennt Regel und Raumhaken, kein
    /// Zusammenlegen oder Trennen). Der Haken „beheizt" einer Zone ist deshalb ein Haken je Raum: Der
    /// Dialog stellt alle Räume der Zone um, und die Zonierung bildet sich neu.</para>
    /// </summary>
    internal static class GebaeudeImportZonen
    {
        /// <summary>Der Schlüssel des Profils der Liste „Flächen je Zone".</summary>
        internal const string PROFIL_FLAECHEN = "GEBIMPORT_FLAECHEN";

        internal const string SP_ZONE = "ZONE";
        internal const string SP_ART = "ART";
        internal const string SP_FLAECHE = "FLAECHE";
        internal const string SP_AZIMUT = "AZIMUT";
        internal const string SP_NEIGUNG = "NEIGUNG";
        internal const string SP_RAND = "RAND";
        internal const string SP_NACHBARZONE = "NACHBARZONE";
        internal const string SP_UWERT = "UWERT";
        internal const string SP_AUFBAU = "AUFBAU";
        internal const string SP_HERKUNFT = "HERKUNFT";
        internal const string SP_BEFUND = "BEFUND";

        private static string Leer => MyResource.Resource.GIMP_WERT_LEER;

        /// <summary>
        /// Die Zonierung als Daten des Dialogs; <c>null</c>, wenn die Datei für das Gebäude nur eine Regel
        /// trägt (dann bleibt der Einzonenweg, wie er ist).
        /// </summary>
        /// <param name="z">Die Zonierung der Anfrage.</param>
        /// <param name="v">Der Bauteilvorschlag darauf; <c>null</c> = keiner.</param>
        /// <param name="haken">Die Haken der Raumliste (Raumkennung → beheizt).</param>
        internal static GebaeudeZonierungDaten ZonierungDaten(GebaeudeZonierung z, GebaeudeBauteilvorschlag v,
                                                              IReadOnlyDictionary<string, bool> haken)
        {
            if (z == null || z.Gebaeude == null || z.Regeln.Count <= 1) return null;
            bool mehr = GebaeudeImportHuelle.Mehrzonig(z);
            bool mitVorschlag = mehr && v != null && v.Mehrzonig && v.Zonen.Count == z.Zonen.Count;
            IReadOnlyDictionary<string, bool> h = haken ?? new Dictionary<string, bool>();

            var zonen = new List<GebaeudeZonenzeileDaten>();
            for (int i = 0; i < z.Zonen.Count; i++)
            {
                Importzone iz = z.Zonen[i];
                zonen.Add(new GebaeudeZonenzeileDaten
                {
                    Name = mitVorschlag ? v.Zonen[i].Bezeichner : iz.Name,
                    Regel = z.Regel,
                    Raeume = iz.Raeume.Count.ToString(CultureInfo.CurrentCulture),
                    Flaeche = MitEinheit(iz.FlaecheM2, "m²"),
                    Volumen = MitEinheit(iz.VolumenM3, "m³"),
                    Beheizt = iz.IstBeheizt,
                    Hinweis = Zonenhinweis(iz),
                    Raumliste = iz.Raeume.Select(r => Raumdaten(z, r, h)).ToList(),
                });
            }

            bool zuViele = z.ZuVieleZonen;
            return new GebaeudeZonierungDaten
            {
                Regeln = z.Regeln.Select(r => new GebaeudeZonenregelDaten(r, GebaeudeZuordnungsModell.ZonenregelText(r))).ToList(),
                Regel = z.Regel,
                RegelText = GebaeudeZuordnungsModell.ZonenregelText(z.Regel),
                Einzonig = !mehr,
                Bilanz = Bilanz(z, v, mehr),
                Schwerste = Schwerste(z, v),
                ZuViele = zuViele,
                Vorschlagsregel = zuViele ? z.Vorschlagsregel : null,
                VorschlagsregelText = zuViele && z.Vorschlagsregel != null ? GebaeudeZuordnungsModell.ZonenregelText(z.Vorschlagsregel) : "",
                Zonen = zonen,
                Flaechenprofil = mitVorschlag ? Flaechenprofil() : null,
                Flaechen = mitVorschlag ? Flaechen(z, v) : Array.Empty<GebaeudeFlaechenzeileDaten>(),
            };
        }

        // ==================================================================
        //  Zonen und Räume
        // ==================================================================

        private static string Zonenhinweis(Importzone iz)
        {
            var teile = new List<string>();
            if (iz.Zugeschlagen.Count > 0)
                teile.Add(Formatieren(MyResource.Resource.GIMP_ZONE_ZUGESCHLAGEN, string.Join(", ", iz.Zugeschlagen)));
            if (iz.ZuKlein) teile.Add(MyResource.Resource.GIMP_ZONE_ZU_KLEIN);
            if (iz.OhneAussen) teile.Add(MyResource.Resource.GIMP_ZONE_OHNE_AUSSEN);
            return string.Join("; ", teile);
        }

        private static GebaeudeZonenraumDaten Raumdaten(GebaeudeZonierung z, AbbildRaum r, IReadOnlyDictionary<string, bool> haken)
        {
            var zeile = new GebaeudeRaumzeile(r, haken);
            string geschoss = z.Gebaeude.Geschosse.FirstOrDefault(s => s.Kennung == r.GeschossKennung)?.Anzeigename
                              ?? (string.IsNullOrWhiteSpace(r.GeschossName) ? Leer : r.GeschossName.Trim());
            string regel = zeile.Uebersteuert || string.IsNullOrWhiteSpace(r.Beheizungsregel) ? Leer : r.Beheizungsregel;
            return new GebaeudeZonenraumDaten(r.Kennung, zeile.Anzeigename, geschoss, MitEinheit(r.FlaecheM2, "m²"), regel,
                                              GebaeudeZuordnungsModell.RaumGrundText(zeile), zeile.BeheiztLautDatei);
        }

        // ==================================================================
        //  Bilanz und Meldung
        // ==================================================================

        /// <summary>
        /// Die Bilanz: Zonen, beheizte Fläche und beheiztes Volumen aus den Zonen; Σ Außenfläche und
        /// Σ Trennfläche aus den Zeilen des Vorschlags (Randbedingung Außenluft/Erdreich bzw. Nachbarzone,
        /// Öffnungen eingeschlossen), ohne Zeilen aus den Flächen der Zonierung (brutto, je Paar die
        /// größere Beschreibung); ohne beides ein Strich.
        /// </summary>
        private static GebaeudeZonenbilanzDaten Bilanz(GebaeudeZonierung z, GebaeudeBauteilvorschlag v, bool mehr)
        {
            List<Importzone> warm = z.Zonen.Where(x => x.IstBeheizt).ToList();
            double? flaeche = warm.Any(x => x.FlaecheM2.HasValue) ? warm.Sum(x => x.FlaecheM2 ?? 0.0) : (double?)null;
            double? volumen = warm.Any(x => x.VolumenM3.HasValue) ? warm.Sum(x => x.VolumenM3 ?? 0.0) : (double?)null;

            double? aussen = null, trenn = null;
            if (v != null && v.Zeilen.Count > 0)
            {
                aussen = v.Zeilen.Where(r => r.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_AUSSENLUFT
                                             || r.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ERDREICH).Sum(r => r.Bauteil.Flaeche);
                trenn = v.Zeilen.Where(r => r.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).Sum(r => r.Bauteil.Flaeche);
            }
            else if (mehr)
            {
                aussen = z.Flaechen.Where(f => f.Rand == Zonenrand.Aussenluft || f.Rand == Zonenrand.Erdreich).Sum(f => f.BruttoM2 ?? 0.0);
                trenn = z.Trennungen.Sum(t => Math.Max(t.FlaecheA, t.FlaecheB));
            }

            return new GebaeudeZonenbilanzDaten(
                z.Zonen.Count.ToString(CultureInfo.CurrentCulture),
                MitEinheit(flaeche, "m²"), MitEinheit(volumen, "m³"), MitEinheit(aussen, "m²"), MitEinheit(trenn, "m²"));
        }

        /// <summary>
        /// Die schwerste Meldung ab Stufe Warnung aus Zonierung und Vorschlag; bei zu vielen Zonen die
        /// Warnung mit dem Vorschlag der gröberen Regel (M12), damit Banner und Knopf dasselbe sagen.
        /// Unter den Warnungen geht die entkoppelte Zonierung ohne Raumgrenzen vor (6.5): Sie sagt, warum
        /// eine Regel mit mehreren Zonen hier schlechter rechnet als eine Zone.
        /// </summary>
        private static GebaeudeImportMeldung Schwerste(GebaeudeZonierung z, GebaeudeBauteilvorschlag v)
        {
            PruefMeldung m = null;
            if (z.ZuVieleZonen) m = Meldung(z.Meldungen, GebaeudeZonierung.ZU_VIELE_ZONEN_VORSCHLAG);
            if (m == null)
            {
                List<PruefMeldung> alle = z.Meldungen.Concat((v?.Meldungen ?? Array.Empty<PruefMeldung>()).Where(x => !z.Meldungen.Contains(x))).ToList();
                m = alle.FirstOrDefault(x => x.Stufe == PruefStufe.Fehler)
                    ?? Meldung(alle, GebaeudeZonierung.GRENZEN_ENTKOPPELT)
                    ?? alle.FirstOrDefault(x => x.Stufe == PruefStufe.Warnung);
            }
            if (m == null) return null;
            return MeldungDaten(m);
        }

        /// <summary>Die erste Meldung, deren Schlüssel mit dem Namen der Zonierung endet (der Präfix trägt das Format).</summary>
        private static PruefMeldung Meldung(IEnumerable<PruefMeldung> meldungen, string name)
            => meldungen.FirstOrDefault(x => x.Schluessel != null && x.Schluessel.EndsWith("_" + name, StringComparison.Ordinal));

        private static GebaeudeImportMeldung MeldungDaten(PruefMeldung m)
        {
            return new GebaeudeImportMeldung(
                m.Stufe == PruefStufe.Fehler ? WarnStufe.Fehler : WarnStufe.Warnung,
                GebaeudeZuordnungsModell.StufeText(m.Stufe), GebaeudeZuordnungsModell.MeldungText(m), m.Schluessel);
        }

        // ==================================================================
        //  Flächen je Zone
        // ==================================================================

        /// <summary>
        /// Die Spalten der Liste „Flächen je Zone": Zone, Bauteil (die elastische Spalte), Art, Fläche,
        /// Azimut, Neigung, Randbedingung, Nachbarzone, U-Wert, Aufbau, Herkunft und Befund. Zone,
        /// Bauteil, Fläche, Randbedingung, U-Wert und Befund stehen immer; die übrigen weichen, wenn die
        /// Liste schmal wird.
        /// </summary>
        internal static Katalogfilterprofil Flaechenprofil()
        {
            return Katalogfilterprofil.AusSpalten(PROFIL_FLAECHEN, new[]
            {
                new Katalogspalte(SP_ZONE, MyResource.Resource.GIMP_DLG_SP_ZONE),
                new Katalogspalte(Katalogfilterprofil.SpBezeichner, MyResource.Resource.GIMP_DLG_SP_BAUTEIL),
                new Katalogspalte(SP_ART, MyResource.Resource.GIMP_DLG_SP_ART, rang: Katalogspaltenrang.BeiPlatz),
                new Katalogspalte(SP_FLAECHE, MyResource.Resource.GIMP_DLG_SP_FLAECHE, "m²", Katalogspaltenart.Zahl),
                new Katalogspalte(SP_AZIMUT, MyResource.Resource.GIMP_DLG_SP_AZIMUT, "°", Katalogspaltenart.Zahl,
                                  rang: Katalogspaltenrang.BeiPlatz),
                new Katalogspalte(SP_NEIGUNG, MyResource.Resource.GIMP_DLG_SP_NEIGUNG, "°", Katalogspaltenart.Zahl,
                                  rang: Katalogspaltenrang.Breit),
                new Katalogspalte(SP_RAND, MyResource.Resource.GIMP_DLG_SP_RAND),
                new Katalogspalte(SP_NACHBARZONE, MyResource.Resource.GIMP_FL_SP_NACHBARZONE, rang: Katalogspaltenrang.BeiPlatz),
                new Katalogspalte(SP_UWERT, MyResource.Resource.GIMP_DLG_SP_UWERT, "W/(m²K)", Katalogspaltenart.Zahl),
                new Katalogspalte(SP_AUFBAU, MyResource.Resource.GIMP_FL_SP_AUFBAU, rang: Katalogspaltenrang.Breit),
                new Katalogspalte(SP_HERKUNFT, MyResource.Resource.GIMP_DLG_SP_HERKUNFT, rang: Katalogspaltenrang.Breit),
                new Katalogspalte(SP_BEFUND, MyResource.Resource.GIMP_FL_SP_BEFUND),
            });
        }

        /// <summary>
        /// Die Flächen des Vorschlags je Zone — die Zeilen, die als Bauteile geschrieben würden, in ihrer
        /// Reihenfolge — mit den Befunden aus der Zonierung: ohne Gegenstück (die innere Grenze rechnet
        /// gegen unbeheizt), Fläche geschätzt (ohne Polygon nach der Zahl der Grenzen geteilt) und ohne
        /// U-Wert (weder ein Wert noch ein Aufbau). Eine Öffnung trägt die Befunde ihres Wirts.
        /// </summary>
        internal static IReadOnlyList<GebaeudeFlaechenzeileDaten> Flaechen(GebaeudeZonierung z, GebaeudeBauteilvorschlag v)
        {
            var jeTeil = new Dictionary<(int, string), List<Zonenflaeche>>();
            void Merken(int zone, string kennung, Zonenflaeche f)
            {
                if (kennung == null) return;
                if (!jeTeil.TryGetValue((zone, kennung), out List<Zonenflaeche> liste)) jeTeil[(zone, kennung)] = liste = new List<Zonenflaeche>();
                liste.Add(f);
            }
            foreach (Zonenflaeche f in z.Flaechen)
            {
                Merken(f.Zone, f.Bauteil?.Kennung, f);
                foreach (AbbildBauteil o in f.Oeffnungen) Merken(f.Zone, o.Kennung, f);
            }

            var zonenname = new Dictionary<int, string>();
            foreach (ZoneModel zm in v.Zonen) zonenname[zm.ID] = zm.Bezeichner;
            IReadOnlyDictionary<int, BauteilaufbauModel> aufbauten = v.AufbautenJeId;

            var zeilen = new List<GebaeudeFlaechenzeileDaten>(v.Zeilen.Count);
            for (int i = 0; i < v.Zeilen.Count; i++)
            {
                GebaeudeBauteilzeile zl = v.Zeilen[i];
                BauteilModel b = zl.Bauteil;
                List<Zonenflaeche> teile = zl.Kennung != null && jeTeil.TryGetValue((zl.Zone, zl.Kennung), out List<Zonenflaeche> t) ? t : null;
                bool ohneGegenstueck = teile != null && teile.Any(f => f.OhneGegenstueck);
                bool geschaetzt = teile != null && teile.Any(f => f.Aufgeteilt);
                bool ohneU = !b.U_Wert.HasValue && !b.ID_Aufbau.HasValue;

                var befund = new List<string>();
                if (ohneGegenstueck) befund.Add(MyResource.Resource.GIMP_FL_BEFUND_OHNE_GEGENSTUECK);
                if (ohneU) befund.Add(MyResource.Resource.GIMP_FL_BEFUND_OHNE_UWERT);
                if (geschaetzt) befund.Add(MyResource.Resource.GIMP_FL_BEFUND_GESCHAETZT);

                Importherkunft herkunft = GebaeudeZuordnungsModell.HerkunftAusSchluessel(b.Herkunft);
                string zone = zl.Zone >= 0 && zl.Zone < v.Zonen.Count ? v.Zonen[zl.Zone].Bezeichner : Leer;
                string nachbar = b.ID_Nachbarzone is int n && zonenname.TryGetValue(n, out string nn) ? nn : "";
                string aufbau = b.ID_Aufbau is int a && aufbauten.TryGetValue(a, out BauteilaufbauModel am) ? am.Bezeichner : "";

                var zeile = new Katalogfilterzeile(i, b.Bezeichner ?? "")
                    .MitText(Katalogfilterprofil.SpBezeichner, b.Bezeichner ?? "")
                    .MitText(SP_ZONE, zone)
                    .MitText(SP_ART, BauteilaufbauCtrl.BauteilartText(b.Bauteilart))
                    .MitZahl(SP_FLAECHE, b.Flaeche, 2)
                    .MitZahl(SP_AZIMUT, b.Azimut.HasValue ? Azimut(b.Azimut.Value) : (double?)null, 1)
                    .MitZahl(SP_NEIGUNG, b.Neigung, 1)
                    .MitText(SP_RAND, GebaeudeImportHuelle.RandText(b.Randbedingung))
                    .MitText(SP_NACHBARZONE, nachbar)
                    .Mit(SP_UWERT, b.U_Wert.HasValue ? Katalogwert.AusZahl(b.U_Wert, 3)
                                   : b.ID_Aufbau.HasValue ? Katalogwert.AusText(MyResource.Resource.GIMP_BT_AUS_SCHICHTEN)
                                   : Katalogwert.Leer)
                    .MitText(SP_AUFBAU, aufbau)
                    .MitText(SP_HERKUNFT, GebaeudeZuordnungsModell.HerkunftText(herkunft))
                    .MitText(SP_BEFUND, string.Join("; ", befund));
                zeile.Schluessel = i.ToString(CultureInfo.InvariantCulture);
                zeilen.Add(new GebaeudeFlaechenzeileDaten(zeile, befund.Count > 0, ohneGegenstueck, ohneU));
            }
            return zeilen;
        }

        /// <summary>Der Azimut auf eine Nachkommastelle, was auf 360° rundet, als 0° (nur die Anzeige).</summary>
        internal static double Azimut(double azimut)
        {
            double gerundet = Math.Round(azimut, 1, MidpointRounding.AwayFromZero);
            if (gerundet >= 360) gerundet -= 360;
            return gerundet + 0.0;
        }

        private static string MitEinheit(double? wert, string einheit)
            => wert.HasValue ? GebaeudeZuordnungsModell.ZahlText(Math.Round(wert.Value, 2)) + " " + einheit : Leer;

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
