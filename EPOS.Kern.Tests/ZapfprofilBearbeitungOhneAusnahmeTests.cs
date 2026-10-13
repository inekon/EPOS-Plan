using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Bearbeitungsfluss des Zapfprofils wirft keine Ausnahme</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 2.2, N8 (b): Hinweis / Zone trägt 0 / benannter Abbruch). Zwischenstände,
    /// die beim gewöhnlichen Bearbeiten des Dialogs entstehen — eine neue Zone ohne Nutzungsart, eine
    /// leere Bezugsmenge, eine Wohnungszeile ohne Anzahl, ein halber Ferienzeitraum, ein Messwert ohne
    /// Einheit … —, ergeben in Vorschau, Ladevorschlag und Auslegung dieselbe benannte Ablehnung wie
    /// bisher, aber <b>ohne <c>throw</c></b>: Die Vorprüfung des Kerns (<see cref="Zonenvorpruefung"/>)
    /// nimmt die Zone vorher heraus. Gezählt wird jede Ausnahme des eigenen Fadens über
    /// <see cref="AppDomain.FirstChanceException"/> — auch eine, die gleich wieder gefangen wird
    /// (Visual Studio hält an jeder an); <see cref="OperationCanceledException"/> ausgenommen.
    ///
    /// <para>Zwei Ebenen: der Rechenweg selbst (<see cref="ZapfprofilRechner.Rechnen"/>) mit dem
    /// fiktiven Katalog der <see cref="ZapfprofilTestbau"/>, und die Hülle des Dialogs
    /// (<see cref="ZapfprofilHuelle.Vorschau"/>, <see cref="ZapfprofilHuelle.Ladevorschlag"/>,
    /// <see cref="ZapfprofilHuelle.AuslegungStart"/>) auf der Arbeitskopie der Testdatenbank mit dem
    /// Referenzprojekt 1045. Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilBearbeitungOhneAusnahmeTests : IDisposable
    {
        private const int PROJEKT = 1045;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Beobachter
        // =================================================================================

        /// <summary>
        /// Führt <paramref name="handlung"/> aus und sammelt jede Ausnahme, die auf diesem Faden
        /// entsteht — geworfen und gefangen ebenso wie ungefangen. Der Abbruch eines Laufs zählt nicht.
        /// </summary>
        private static List<string> Ausnahmen(Action handlung)
        {
            var liste = new List<string>();
            int faden = Environment.CurrentManagedThreadId;
            EventHandler<FirstChanceExceptionEventArgs> beobachter = (_, e) =>
            {
                if (Environment.CurrentManagedThreadId != faden) return;
                if (e.Exception is OperationCanceledException) return;
                liste.Add(e.Exception.GetType().Name + ": " + e.Exception.Message);
            };
            AppDomain.CurrentDomain.FirstChanceException += beobachter;
            try { handlung(); }
            finally { AppDomain.CurrentDomain.FirstChanceException -= beobachter; }
            return liste;
        }

        private static void KeineAusnahme(string fall, List<string> ausnahmen)
            => Assert.True(ausnahmen.Count == 0, fall + ": " + string.Join(" | ", ausnahmen));

        // =================================================================================
        // Ebene Rechenweg (ohne Datenbank)
        // =================================================================================

        /// <summary>Der fiktive Katalog: 1 = Personen, 2 = Wohneinheiten.</summary>
        private static readonly Nutzungsart[] Katalog =
        {
            ZapfprofilTestbau.Art(1, ZapfBezugsart.Personen),
            ZapfprofilTestbau.Art(2, ZapfBezugsart.Wohneinheiten)
        };

        /// <summary>Eine Zone im Zwischenstand je Fall — und die Ablehnung, die sie trägt (Code, Satzkennung).</summary>
        private static (ZonenStand Zone, ZapfEingabefehler? Code, string Kennung) Fall(string fall)
        {
            ZonenStand z = ZapfprofilTestbau.Zone("Zone B", idArt: 1, menge: 10.0, id: 2);
            switch (fall)
            {
                case "neue_zone":
                    return (new ZonenStand { Id = 2, Name = "Zone B", Reihenfolge = 2 },
                            ZapfEingabefehler.NutzungsartFehlt, "EINGABE_NUTZUNGSART_NICHT_GEWAEHLT");
                case "bezugsmenge_leer":
                    return (z with { Bezugsmenge = 0.0 }, ZapfEingabefehler.BezugsmengeFehlt, "EINGABE_BEZUGSMENGE_NICHT_POSITIV");
                case "wohnung_ohne_anzahl_we":
                    return (z with { IdNutzungsart = 2, Wohnungen = new[] { new WohnungstypStand { Id = 1, Anzahl = 0 } } },
                            ZapfEingabefehler.BezugsmengeFehlt, "EINGABE_WOHNUNGSTYP_ANZAHL");
                case "wohnung_ohne_anzahl_personen":
                    return (z with { Wohnungen = new[] { new WohnungstypStand { Id = 1, Anzahl = 0, Personen = 2.0 } } },
                            ZapfEingabefehler.BezugsmengeFehlt, "EINGABE_WOHNUNGSTYP_ANZAHL");
                case "wohnung_ohne_belegung":
                    return (z with { Wohnungen = new[] { new WohnungstypStand { Id = 1, Anzahl = 2, Raumzahl = 9.0 } } },
                            ZapfEingabefehler.BelegungFehlt, "EINGABE_BELEGUNG_FEHLT");
                case "ferien_nur_beginn":
                    return (z with { Ferienbeginn = new int?[] { 100, null, null, null } }, null, null);
                case "messwert_ohne_einheit":
                    return (z with { Jahresmesswert = 1000.0 }, ZapfEingabefehler.MesswertUngueltig, "EINGABE_MESSWERT_OHNE_EINHEIT");
                case "messwert_kwh_ohne_grenze":
                    return (z with { Jahresmesswert = 1000.0, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr },
                            ZapfEingabefehler.MesswertUngueltig, "EINGABE_MESSWERT_OHNE_GRENZE");
                case "messwert_null":
                    return (z with { Jahresmesswert = 0.0, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr },
                            ZapfEingabefehler.MesswertUngueltig, "EINGABE_MESSWERT_NICHT_POSITIV");
                case "messwert_volumen_mit_grenze":
                    return (z with { Jahresmesswert = 50.0, JahresmesswertEinheit = ZapfMesswerteinheit.KubikmeterJeJahr,
                                     JahresmesswertBilanzgrenze = ZapfBilanzgrenze.MitVerteilung },
                            ZapfEingabefehler.MesswertUngueltig, "EINGABE_MESSWERT_VOLUMEN_GRENZE");
                case "messwert_grenze3_ohne_verlust":
                    return (z with { Jahresmesswert = 1000.0, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
                                     JahresmesswertBilanzgrenze = ZapfBilanzgrenze.MitSpeicher },
                            ZapfEingabefehler.MesswertUngueltig, "EINGABE_MESSWERT_SPEICHERVERLUST_FEHLT");
                case "messwert_verlust_zu_gross":
                    return (z with { Jahresmesswert = 1000.0, JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
                                     JahresmesswertBilanzgrenze = ZapfBilanzgrenze.MitSpeicher, SpeicherverlustKwhJeJahr = 1000.0 },
                            ZapfEingabefehler.MesswertUngueltig, "EINGABE_MESSWERT_SPEICHERVERLUST_ZU_GROSS");
                case "nutzungsart_unbekannt":
                    return (z with { IdNutzungsart = 999 }, ZapfEingabefehler.NutzungsartFehlt, "EINGABE_NUTZUNGSART_FEHLT");
                case "tagesgangsatz_unbekannt":
                    return (z with { IdTagesgangsatz = 999 }, ZapfEingabefehler.TagesgangsatzFehlt, "EINGABE_TAGESGANGSATZ_FEHLT");
                case "zapf_unter_kaltwasser":
                    return (z with { ZapftemperaturC = 8.0 }, ZapfEingabefehler.TemperaturUngueltig,
                            "EINGABE_SPREIZUNG_NICHT_POSITIV_ZONE");
                case "zapf_unter_kaltwasser_manuell":
                    return (z with { ZapftemperaturC = 8.0, TagesbedarfAuto = false, TagesbedarfManuellKwh = 5.0 },
                            ZapfEingabefehler.TemperaturUngueltig, "EINGABE_ZAPFTEMPERATUR_UNTER_KALTWASSER");
                case "kaltwasser_monat_ueber_zapf":
                    return (z with { ZapftemperaturC = 12.0 }, ZapfEingabefehler.TemperaturUngueltig,
                            "EINGABE_KALTWASSER_MONAT_UEBER_ZAPF");
                case "amplitude_negativ":
                    return (z with { KaltwasserAmplitudeK = -1.0 }, ZapfEingabefehler.TemperaturUngueltig,
                            "EINGABE_KALTWASSER_AMPLITUDE_NEGATIV");
                case "tagesbedarf_manuell_leer":
                    return (z with { TagesbedarfAuto = false }, ZapfEingabefehler.TagesbedarfUngueltig,
                            "EINGABE_TAGESBEDARF_MANUELL_UNGUELTIG");
                case "tagesbedarf_manuell_negativ":
                    return (z with { TagesbedarfAuto = false, TagesbedarfManuellKwh = -1.0 }, ZapfEingabefehler.TagesbedarfUngueltig,
                            "EINGABE_TAGESBEDARF_MANUELL_UNGUELTIG");
                case "bedarf_spez_negativ":
                    return (z with { BedarfSpezKwhJeEinheitTag = -1.0 }, ZapfEingabefehler.RasterUngueltig, "EINGABE_BEDARF_SPEZ_NEGATIV");
                case "auslastung_negativ":
                    return (z with { Auslastung = new double?[] { -1.0, null, null, null, null, null, null, null, null, null, null, null } },
                            ZapfEingabefehler.RasterUngueltig, "EINGABE_RASTER_NEGATIV");
                default:
                    throw new ArgumentException(fall);
            }
        }

        private static Zapfprofileingang Eingang(ProjektStand projekt, params ZonenStand[] zonen)
            => ZapfprofilTestbau.Eingang(projekt, ZapfprofilTestbau.Parameter(), zonen) with
            {
                BelegungJeRaumzahl = new Dictionary<string, double> { ["2"] = 1.8 }
            };

        /// <summary>
        /// Je Zwischenstand: Neben einer vollständigen Zone A steht die Zone B des Falls. Der Rechenweg
        /// wirft nichts, Zone B trägt 0 mit der erwarteten Ablehnung (oder rechnet, wo der Zwischenstand
        /// nichts verhindert), und Zone A rechnet Stunde für Stunde wie allein.
        /// </summary>
        [Theory]
        [InlineData("neue_zone")]
        [InlineData("bezugsmenge_leer")]
        [InlineData("wohnung_ohne_anzahl_we")]
        [InlineData("wohnung_ohne_anzahl_personen")]
        [InlineData("wohnung_ohne_belegung")]
        [InlineData("ferien_nur_beginn")]
        [InlineData("messwert_ohne_einheit")]
        [InlineData("messwert_kwh_ohne_grenze")]
        [InlineData("messwert_null")]
        [InlineData("messwert_volumen_mit_grenze")]
        [InlineData("messwert_grenze3_ohne_verlust")]
        [InlineData("messwert_verlust_zu_gross")]
        [InlineData("nutzungsart_unbekannt")]
        [InlineData("tagesgangsatz_unbekannt")]
        [InlineData("zapf_unter_kaltwasser")]
        [InlineData("zapf_unter_kaltwasser_manuell")]
        [InlineData("kaltwasser_monat_ueber_zapf")]
        [InlineData("amplitude_negativ")]
        [InlineData("tagesbedarf_manuell_leer")]
        [InlineData("tagesbedarf_manuell_negativ")]
        [InlineData("bedarf_spez_negativ")]
        [InlineData("auslastung_negativ")]
        public void Der_Rechenweg_lehnt_einen_Zwischenstand_ohne_Ausnahme_ab(string fall)
        {
            (ZonenStand b, ZapfEingabefehler? code, string kennung) = Fall(fall);
            ZonenStand a = ZapfprofilTestbau.Zone("Zone A", idArt: 1, menge: 10.0, id: 1);
            ZapfprofilErgebnis allein = ZapfprofilRechner.Rechnen(Eingang(ZapfprofilTestbau.Projekt(), a), Katalog);

            ZapfprofilErgebnis e = null;
            KeineAusnahme(fall, Ausnahmen(() => e = ZapfprofilRechner.Rechnen(Eingang(ZapfprofilTestbau.Projekt(), a, b), Katalog)));

            if (code.HasValue)
            {
                ZapfAblehnung abl = Assert.Single(e.Ablehnungen);
                Assert.Equal("Zone B", abl.Zone);
                Assert.Equal(code.Value, abl.Grund);
                Assert.Equal(kennung, abl.Kennung);
                Assert.True(e.JeZone[1].Abgelehnt);
                Assert.Equal(0.0, e.JeZone[1].JahresbedarfZapfungKwh);
                Assert.Equal(0.0, e.JeZone[1].Zapfung.StundenKwh.Sum());
            }
            else
            {
                Assert.Empty(e.Ablehnungen);
                Assert.False(e.JeZone[1].Abgelehnt);
                Assert.True(e.JeZone[1].JahresbedarfZapfungKwh > 0);
            }

            // Die vollständige Zone rechnet unverändert.
            Assert.False(e.JeZone[0].Abgelehnt);
            Assert.Equal(allein.JeZone[0].JahresbedarfZapfungKwh, e.JeZone[0].JahresbedarfZapfungKwh);
            Assert.True(allein.JeZone[0].Zapfung.StundenKwh.SequenceEqual(e.JeZone[0].Zapfung.StundenKwh));
            // Eine Zone, die erst die Kalibrierung ablehnt, trug schon zum Zirkulationsansatz bei (2.3).
            if (code.HasValue && !kennung.StartsWith("EINGABE_MESSWERT_SPEICHERVERLUST", StringComparison.Ordinal))
                Assert.True(allein.Zirkulation.StundenKwh.SequenceEqual(e.Zirkulation.StundenKwh));
        }

        /// <summary>
        /// Die Vorprüfung nennt für jeden Fall dieselbe Ablehnung, die der Rechenweg trägt — und
        /// <c>null</c>, wo die Zone rechnet. Die Kalibrierung prüft sie erst mit den Mengen (Phase 3).
        /// </summary>
        [Theory]
        [InlineData("neue_zone")]
        [InlineData("bezugsmenge_leer")]
        [InlineData("wohnung_ohne_belegung")]
        [InlineData("ferien_nur_beginn")]
        [InlineData("messwert_kwh_ohne_grenze")]
        [InlineData("nutzungsart_unbekannt")]
        [InlineData("zapf_unter_kaltwasser_manuell")]
        [InlineData("auslastung_negativ")]
        public void Die_Vorpruefung_nennt_die_Ablehnung_des_Rechenwegs(string fall)
        {
            (ZonenStand b, ZapfEingabefehler? code, string kennung) = Fall(fall);
            Zapfprofileingang e = Eingang(ZapfprofilTestbau.Projekt(), b);
            ZapfAblehnung v = null;
            KeineAusnahme(fall, Ausnahmen(() => v = Zonenvorpruefung.Pruefen(b, e, Katalog)));
            if (!code.HasValue) { Assert.Null(v); return; }
            Assert.NotNull(v);
            ZapfAblehnung r = Assert.Single(ZapfprofilRechner.Rechnen(e, Katalog).Ablehnungen);
            Assert.Equal(r.Zone, v.Zone);
            Assert.Equal(r.Grund, v.Grund);
            Assert.Equal(r.Satz, v.Satz);
        }

        /// <summary>
        /// Projektweite Zwischenstände der Zirkulation: Die Zirkulation trägt 0 mit dem benannten Grund,
        /// die Zonen rechnen; und ein manueller Wert mit einer Methode, die (noch) nicht rechnet, ergibt nur
        /// keinen Vorschlag — alles ohne Ausnahme.
        /// </summary>
        [Fact]
        public void Die_Zirkulation_lehnt_unvollstaendige_Angaben_ohne_Ausnahme_ab()
        {
            ZonenStand a = ZapfprofilTestbau.Zone("Zone A", idArt: 1, menge: 10.0, id: 1);
            var faelle = new (string Fall, ProjektStand Projekt, string Kennung)[]
            {
                ("laenge_fehlt", ZapfprofilTestbau.Projekt() with { ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge },
                 "EINGABE_ZIRKULATION_LAENGE_FEHLT"),
                ("laenge_negativ", ZapfprofilTestbau.Projekt() with { ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge, ZirkLaengeM = -5.0 },
                 "EINGABE_ZIRKULATION_WERT"),
                ("manuell_leer", ZapfprofilTestbau.Projekt() with { ZirkAuto = false }, "EINGABE_ZIRKULATION_MANUELL"),
                ("laufzeit_null", ZapfprofilTestbau.Projekt() with { ZirkLaufzeitH = 0.0 }, "EINGABE_ZIRKULATION_LAUFZEIT"),
                ("manuell_mit_laenge_fehlt", ZapfprofilTestbau.Projekt() with
                 {
                     ZirkAuto = false, ZirkManuellKw = 1.0, ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge
                 }, null)
            };
            foreach (var (fall, projekt, kennung) in faelle)
            {
                ZapfprofilErgebnis e = null;
                KeineAusnahme(fall, Ausnahmen(() => e = ZapfprofilRechner.Rechnen(Eingang(projekt, a), Katalog)));
                Assert.False(e.JeZone[0].Abgelehnt);
                if (kennung == null)
                {
                    Assert.Empty(e.Ablehnungen);
                    Assert.True(double.IsNaN(e.SchaetzhilfeZirkulation.Vorschlag));
                    continue;
                }
                ZapfAblehnung abl = Assert.Single(e.Ablehnungen);
                Assert.Equal("", abl.Zone);
                Assert.Equal(ZapfEingabefehler.ZirkulationUngueltig, abl.Grund);
                Assert.Equal(kennung, abl.Kennung);
                Assert.Null(e.Zirkulationsansatz);
            }
        }

        // =================================================================================
        // Ebene Hülle (Testdatenbank, Projekt 1045)
        // =================================================================================

        /// <summary>Der Zwischenstand je Fall im DTO des Dialogs — die Zone hängt hinten an die Zonen von 1045.</summary>
        private static ZapfprofilZoneDaten DtoFall(string fall, ZapfprofilZoneDaten vorlage, int idPersonen, int idWohneinheiten)
        {
            ZapfprofilZoneDaten z = vorlage.Kopie();
            z.Id = 0;
            z.Name = "Zone Probe";
            z.Angaben = (vorlage.Angaben ?? new ZapfprofilZonenangabenDaten()).Kopie();
            z.Angaben.Jahresmesswert = null;
            z.Angaben.Wohnungen.Clear();
            switch (fall)
            {
                case "neue_zone":
                    return new ZapfprofilZoneDaten { Name = "Zone Probe", Angaben = new ZapfprofilZonenangabenDaten() };
                case "bezugsmenge_leer":
                    z.Bezugsmenge = null;
                    return z;
                case "wohnung_ohne_anzahl_we":
                    z.IdNutzungsart = idWohneinheiten;
                    z.Angaben.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 4 });
                    z.Angaben.Wohnungen.Add(new ZapfprofilWohnungDaten());
                    return z;
                case "wohnung_ohne_anzahl_personen":
                    z.IdNutzungsart = idPersonen;
                    z.Angaben.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 4, Personen = 2.0 });
                    z.Angaben.Wohnungen.Add(new ZapfprofilWohnungDaten());
                    return z;
                case "wohnung_ohne_belegung":
                    z.IdNutzungsart = idPersonen;
                    z.Angaben.PersonenJeWe = null;
                    z.Angaben.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 2, Raumzahl = 99.0 });
                    return z;
                case "ferien_nur_beginn":
                    z.Angaben.Ferien[0].BeginnTag = 1;
                    z.Angaben.Ferien[0].BeginnMonat = 8;
                    z.Angaben.Ferien[0].EndeTag = null;
                    z.Angaben.Ferien[0].EndeMonat = null;
                    return z;
                case "messwert_ohne_einheit":
                    z.Angaben.Jahresmesswert = 5000.0;
                    z.Angaben.JahresmesswertEinheit = null;
                    return z;
                case "messwert_kwh_ohne_grenze":
                    z.Angaben.Jahresmesswert = 5000.0;
                    z.Angaben.JahresmesswertEinheit = ZapfprofilMesswerteinheit.KwhJeJahr;
                    z.Angaben.JahresmesswertBilanzgrenze = null;
                    return z;
                case "nutzungsart_unbekannt":
                    z.IdNutzungsart = 987654;
                    return z;
                case "tagesgangsatz_unbekannt":
                    z.Angaben.IdTagesgangsatz = 987654;
                    return z;
                case "zapf_unter_kaltwasser":
                    z.Angaben.ZapftemperaturC = 5.0;
                    z.Angaben.TagesbedarfAuto = true;
                    return z;
                default:
                    throw new ArgumentException(fall);
            }
        }

        private static readonly (string Fall, string Kennung)[] DtoFaelle =
        {
            ("neue_zone", "EINGABE_NUTZUNGSART_NICHT_GEWAEHLT"),
            ("bezugsmenge_leer", "EINGABE_BEZUGSMENGE_NICHT_POSITIV"),
            ("wohnung_ohne_anzahl_we", "EINGABE_WOHNUNGSTYP_ANZAHL"),
            ("wohnung_ohne_anzahl_personen", "EINGABE_WOHNUNGSTYP_ANZAHL"),
            ("wohnung_ohne_belegung", "EINGABE_BELEGUNG_FEHLT"),
            ("ferien_nur_beginn", null),
            ("messwert_ohne_einheit", "EINGABE_MESSWERT_OHNE_EINHEIT"),
            ("messwert_kwh_ohne_grenze", "EINGABE_MESSWERT_OHNE_GRENZE"),
            ("nutzungsart_unbekannt", "EINGABE_NUTZUNGSART_FEHLT"),
            ("tagesgangsatz_unbekannt", "EINGABE_TAGESGANGSATZ_FEHLT"),
            ("zapf_unter_kaltwasser", "EINGABE_SPREIZUNG_NICHT_POSITIV_ZONE")
        };

        /// <summary>
        /// Die Hülle des Dialogs auf 1045: Vorschau, Ladevorschlag und Auslegung zu jedem Zwischenstand
        /// werfen nichts; die Vorschau ist gerechnet und nennt die Zone mit ihrer Ablehnung, die Auslegung
        /// nimmt sie benannt aus ihrer Gruppe, und die Zonen von 1045 rechnen wie ohne sie.
        /// </summary>
        [Fact]
        public void Die_Huelle_rechnet_Zwischenstaende_ohne_Ausnahme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand stand = ZapfprofilCtrl.Lies(PROJEKT);
            ZapfprofilEingabeDaten basis = ZapfprofilHuelle.AlsEingabe(stand);
            Assert.NotEmpty(basis.Zonen);
            IReadOnlyList<Nutzungsart> katalog = ZapfprofilCtrl.Katalog();
            int idPersonen = katalog.First(n => n.Bezug == ZapfBezugsart.Personen).Id;
            // Der Testkatalog führt keine Nutzungsart je Wohneinheit; den Fall hält dann die Ebene Rechenweg.
            int idWohneinheiten = katalog.FirstOrDefault(n => n.Bezug == ZapfBezugsart.Wohneinheiten)?.Id ?? 0;

            List<ZapfprofilNutzungsartDaten> katalogDaten = ZapfprofilHuelle.Katalog();

            ZapfprofilVorschauDaten ohne = null;
            KeineAusnahme("Ausgang", Ausnahmen(() => ohne = ZapfprofilHuelle.Vorschau(PROJEKT, basis, stand)));
            // Auch der gespeicherte Stand selbst: Die Wohnzone von 1045 trägt keine Wohnungstabelle — die
            // DIN 4708 ist dann „nicht rechenbar", benannt und ohne Ausnahme.
            KeineAusnahme("Ausgang (Ladevorschlag)", Ausnahmen(() => ZapfprofilHuelle.Ladevorschlag(PROJEKT, basis, stand)));
            KeineAusnahme("Ausgang (Auslegung)", Ausnahmen(() =>
                ZapfprofilHuelle.AuslegungStart(PROJEKT, basis, stand, ZapfprofilStufe.Einfach)));
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, ohne.Zustand);
            int zonen = basis.Zonen.Count;

            foreach (var (fall, kennung) in DtoFaelle)
            {
                if (fall == "wohnung_ohne_anzahl_we" && idWohneinheiten == 0) continue;
                ZapfprofilEingabeDaten eingabe = basis.Kopie();
                eingabe.Zonen.Add(DtoFall(fall, basis.Zonen[0], idPersonen, idWohneinheiten));

                ZapfprofilVorschauDaten v = null;
                KeineAusnahme(fall + " (Vorschau)", Ausnahmen(() => v = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, stand)));
                Assert.True(v.Zustand == ZapfprofilVorschauZustand.Gerechnet, fall + ": " + v.Grund);
                if (kennung == null)
                {
                    Assert.DoesNotContain(v.Meldungen, m => m.Art == ZapfprofilMeldungsart.Ablehnung && m.Zone == "Zone Probe");
                    Assert.False(v.Zonen[zonen].Abgelehnt, fall);
                }
                else
                {
                    ZapfprofilMeldung m = Assert.Single(v.Meldungen,
                        x => x.Art == ZapfprofilMeldungsart.Ablehnung && x.Zone == "Zone Probe");
                    Assert.Equal(ZapfSatz.PRAEFIX + kennung, m.Kennung);
                    Assert.True(v.Zonen[zonen].Abgelehnt, fall);
                    Assert.Equal(0.0, v.Zonen[zonen].JahresbedarfZapfungKwh);
                }
                for (int i = 0; i < zonen; i++)
                    Assert.Equal(ohne.Zonen[i].JahresbedarfZapfungKwh, v.Zonen[i].JahresbedarfZapfungKwh);

                KeineAusnahme(fall + " (Ladevorschlag)", Ausnahmen(() => ZapfprofilHuelle.Ladevorschlag(PROJEKT, eingabe, stand)));
                KeineAusnahme(fall + " (Prüfung)", Ausnahmen(() => ZapfprofilHuelle.Pruefen(eingabe, katalogDaten)));

                ZapfprofilAuslegungStartDaten start = null;
                KeineAusnahme(fall + " (Auslegung)", Ausnahmen(() =>
                    start = ZapfprofilHuelle.AuslegungStart(PROJEKT, eingabe, stand, ZapfprofilStufe.Einfach)));
                Assert.True(start.Ergebnis.Zustand == ZapfprofilAuslegungZustand.Gerechnet, fall + ": " + start.Ergebnis.Grund);
                if (kennung != null)
                    Assert.Contains(start.Ergebnis.Meldungen,
                        x => x.Kennung == "ZPG_AUS_ZONE_ABGELEHNT" && x.Zone == "Zone Probe");
                else
                    Assert.DoesNotContain(start.Ergebnis.Meldungen, x => x.Zone == "Zone Probe");
            }
        }

        /// <summary>
        /// Die zwei Sätze der Nutzungsart: „noch keine gewählt" für eine neue Zone, „nicht (mehr) im
        /// Katalog" samt Nummer für eine Id, die es nicht gibt — beide unter dem Code
        /// <see cref="ZapfEingabefehler.NutzungsartFehlt"/>.
        /// </summary>
        [Fact]
        public void Die_Nutzungsart_hat_zwei_Saetze()
        {
            Zapfprofileingang e = Eingang(ZapfprofilTestbau.Projekt());
            ZapfAblehnung neu = Zonenvorpruefung.Pruefen(new ZonenStand { Name = "Zone 3" }, e, Katalog);
            ZapfAblehnung alt = Zonenvorpruefung.Pruefen(ZapfprofilTestbau.Zone("Büro", idArt: 17), e, Katalog);
            Assert.Equal(ZapfEingabefehler.NutzungsartFehlt, neu.Grund);
            Assert.Equal(ZapfEingabefehler.NutzungsartFehlt, alt.Grund);
            Assert.Equal("Für die Zone „Zone 3“ ist noch keine Nutzungsart gewählt.", neu.Klartext);
            Assert.Equal("Die Nutzungsart Nr. 17 der Zone „Büro“ steht nicht (mehr) im Katalog — bitte eine Nutzungsart wählen.",
                         alt.Klartext);
        }
    }
}
