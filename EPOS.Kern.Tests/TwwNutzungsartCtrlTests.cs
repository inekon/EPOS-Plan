using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Katalogpflege der Brauchwasser-Nutzungsarten</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.2, 3.3; Stufe Z0, Posten P7): Sperren benutzter und
    /// ausgelieferter Zeilen, „Speichern unter" als neue Zeile, Neu, Ändern, Löschen.
    /// Alle Werte erfunden, jede Probe in einer eigenen leeren Datei (<see cref="TwwTestdatenbank"/>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwNutzungsartCtrlTests
    {
        private static readonly Provenienz Fiktiv =
            new Provenienz(TwwTestdatenbank.QUELLE, "Ausgabe 1", "T1", Herkunftsart.Fiktiv);

        private static TwwNutzungsartEntwurf Entwurf(string name, int satz, string version = "T1") => new TwwNutzungsartEntwurf
        {
            Bezeichner = name,
            Katalogversion = version,
            Bezug = ZapfBezugsart.Betten,
            Bedarf = new[] { 1.0, 2.0, 4.0 },
            BedarfMin = new double?[] { 0.5, null, 3.0 },
            BedarfMax = new double?[] { 1.5, null, 5.0 },
            BedarfHerkunft = Fiktiv,
            Bezugstemperaturen = new Temperaturbezug(50.0, 10.0),
            Grenze = ZapfBilanzgrenze.MitVerteilung,
            Kalender = ZapfKalenderart.Betrieb,
            Ferienfaktor = 0.5,
            Monatsfaktoren = Enumerable.Range(1, 12).Select(m => m <= 6 ? 0.5 : 1.5).ToArray(),
            JahresgangHerkunft = Fiktiv with { Art = Herkunftsart.Eigenkonstruktion },
            Wochenfaktoren = new[] { 0.1, 0.1, 0.2, 0.2, 0.2, 0.1, 0.1 },
            WochengangHerkunft = Fiktiv with { Ausgabe = null },
            IdTagesgangsatz = satz
        };

        // =================================================================================
        // 1 — Neu und Lesen
        // =================================================================================

        [Fact]
        public void Neu_schreibt_eine_eigene_Zeile_die_Wert_fuer_Wert_zurueckkommt()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");

            TwwKatalogErgebnis e = TwwNutzungsartCtrl.Neu(Entwurf("  Nutzung Neu  ", satz));
            Assert.True(e.Ok);
            Assert.True(e.Id > 0);

            Nutzungsart n = TwwNutzungsartCtrl.Lies(e.Id);
            Assert.Equal("Nutzung Neu", n.Name);                 // getrimmt
            Assert.Equal("T1", n.Katalogversion);
            Assert.Equal(ZapfBezugsart.Betten, n.Bezug);
            Assert.Equal(new[] { 1.0, 2.0, 4.0 }, n.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(new double?[] { 0.5, null, 3.0 }, n.Herkunft.Bandbreite.Min);
            Assert.Equal(new double?[] { 1.5, null, 5.0 }, n.Herkunft.Bandbreite.Max);
            Assert.Equal(Fiktiv, n.Herkunft.Bedarf);
            Assert.Equal(Herkunftsart.Eigenkonstruktion, n.Herkunft.Jahresgang.Art);
            Assert.Null(n.Herkunft.Wochengang.Ausgabe);
            Assert.Equal(50.0, n.Bezugstemperaturen.ZapftemperaturC);
            Assert.Equal(10.0, n.Bezugstemperaturen.KaltwasserC);
            Assert.Equal(ZapfBilanzgrenze.MitVerteilung, n.Grenze);
            Assert.Equal(ZapfKalenderart.Betrieb, n.Kalender);
            Assert.Equal(0.5, n.Ferienfaktor);
            Assert.Equal(Entwurf("x", satz).Monatsfaktoren, n.Monatsfaktoren);
            Assert.Equal(new[] { 0.1, 0.1, 0.2, 0.2, 0.2, 0.1, 0.1 }, n.Wochenfaktoren);
            Assert.Equal(satz, n.Tagesgaenge.Id);
            Assert.Equal(ZapfKatalogstatus.Eigen, n.Status);
            Assert.False(n.ReadOnly);
            Assert.Null(n.IdVorlage);
            Assert.Null(n.Freigabe);
            Assert.Null(DataRepository.ExecuteScalar("SELECT \"Beleg\" FROM \"Tab_TwwNutzungsart_STAMM\" WHERE \"ID\" = ?",
                                                     new DbParam("@id", e.Id)));

            // Der Entwurf aus der gelesenen Zeile traegt dieselben Werte (Rundreise).
            TwwNutzungsartEntwurf zurueck = TwwNutzungsartEntwurf.Aus(n);
            Assert.Equal("Nutzung Neu", zurueck.Bezeichner);
            Assert.Equal(satz, zurueck.IdTagesgangsatz);
            Assert.NotSame(n.BedarfJeNiveauKwhJeEinheitTag, zurueck.Bedarf);
        }

        [Fact]
        public void Neu_lehnt_belegten_Namen_fehlenden_Satz_und_unvollstaendigen_Entwurf_benannt_ab()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            Assert.True(TwwNutzungsartCtrl.Neu(Entwurf("Nutzung A", satz)).Ok);

            Assert.Equal(TwwKatalogAusgang.NameBelegt, TwwNutzungsartCtrl.Neu(Entwurf("Nutzung A", satz)).Ausgang);
            Assert.True(TwwNutzungsartCtrl.Neu(Entwurf("Nutzung A", satz, "T2")).Ok);   // neue Version desselben Namens
            Assert.Equal(TwwKatalogAusgang.TagesgangsatzFehlt, TwwNutzungsartCtrl.Neu(Entwurf("Nutzung B", 9999)).Ausgang);

            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.Neu(Entwurf(" ", satz)).Ausgang);
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.Neu(Entwurf("Nutzung C", satz) with { Monatsfaktoren = new double[11] }).Ausgang);
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.Neu(Entwurf("Nutzung C", satz) with { BedarfHerkunft = null }).Ausgang);
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.Neu(Entwurf("Nutzung C", satz) with { Bezug = (ZapfBezugsart)8 }).Ausgang);

            Assert.Equal(2, TwwNutzungsartCtrl.Liste().Count);
        }

        // =================================================================================
        // 2 — Die Sperren
        // =================================================================================

        [Fact]
        public void Eine_ausgelieferte_Zeile_laesst_sich_weder_aendern_noch_loeschen()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Auslieferung A", "T1", satz, TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);

            Assert.True(TwwNutzungsartCtrl.IstReadOnly(id));
            Assert.False(TwwNutzungsartCtrl.IstBenutzt(id));

            TwwNutzungsartEntwurf geaendert = TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(id)) with { Bedarf = new[] { 7.0, 8.0, 9.0 } };
            Assert.Equal(TwwKatalogAusgang.ReadOnlyGesperrt, TwwNutzungsartCtrl.Aendern(id, geaendert).Ausgang);
            Assert.Equal(TwwKatalogAusgang.ReadOnlyGesperrt, TwwNutzungsartCtrl.Loeschen(id).Ausgang);

            Nutzungsart unveraendert = TwwNutzungsartCtrl.Lies(id);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, unveraendert.BedarfJeNiveauKwhJeEinheitTag);
        }

        [Fact]
        public void Eine_benutzte_Zeile_laesst_sich_weder_aendern_noch_loeschen()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Eigen A", "T1", satz);
            TwwTestdatenbank.ZoneAnlegen(1, id, "Zone", 10.0);

            Assert.False(TwwNutzungsartCtrl.IstReadOnly(id));
            Assert.True(TwwNutzungsartCtrl.IstBenutzt(id));

            TwwNutzungsartEntwurf geaendert = TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(id)) with { Bedarf = new[] { 7.0, 8.0, 9.0 } };
            Assert.Equal(TwwKatalogAusgang.BenutztGesperrt, TwwNutzungsartCtrl.Aendern(id, geaendert).Ausgang);
            Assert.Equal(TwwKatalogAusgang.BenutztGesperrt, TwwNutzungsartCtrl.Loeschen(id).Ausgang);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, TwwNutzungsartCtrl.Lies(id).BedarfJeNiveauKwhJeEinheitTag);

            TwwNutzungsartZeile z = Assert.Single(TwwNutzungsartCtrl.Liste());
            Assert.True(z.Benutzt);
            Assert.False(z.ReadOnly);
            Assert.Equal(Herkunftsart.Fiktiv, z.BedarfHerkunft);

            // Der Weg an der Sperre vorbei ist „Speichern unter": eine neue, unbenutzte Zeile.
            TwwKatalogErgebnis kopie = TwwNutzungsartCtrl.SpeichernUnter(id, geaendert with { Katalogversion = "T2" });
            Assert.True(kopie.Ok);
            Assert.False(TwwNutzungsartCtrl.IstBenutzt(kopie.Id));
            Assert.Equal(id, TwwNutzungsartCtrl.Lies(kopie.Id).IdVorlage);
            Assert.Equal(new[] { 7.0, 8.0, 9.0 }, TwwNutzungsartCtrl.Lies(kopie.Id).BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, TwwNutzungsartCtrl.Lies(id).BedarfJeNiveauKwhJeEinheitTag);
        }

        [Fact]
        public void Eine_freie_Zeile_aendert_sich_an_Ort_und_Stelle_und_verliert_ihre_Freigabe()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Eigen A", "T1", satz);
            int andere = TwwTestdatenbank.NutzungsartAnlegen("Eigen B", "T1", satz);
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_TwwNutzungsart_STAMM\" SET \"Freigabe\" = 'geprueft' WHERE \"ID\" = ?",
                                           new DbParam("@id", id));

            TwwNutzungsartEntwurf e = TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(id)) with { Bedarf = new[] { 7.0, 8.0, 9.0 } };
            TwwKatalogErgebnis erg = TwwNutzungsartCtrl.Aendern(id, e);
            Assert.True(erg.Ok);
            Assert.Equal(id, erg.Id);

            Nutzungsart n = TwwNutzungsartCtrl.Lies(id);
            Assert.Equal(new[] { 7.0, 8.0, 9.0 }, n.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Null(n.Freigabe);
            Assert.Equal(ZapfKatalogstatus.Eigen, n.Status);
            Assert.Equal(TwwTestdatenbank.QUELLE, n.Herkunft.Bedarf.Quelle);

            // Der Name einer ANDEREN Zeile ist belegt; nichts wird geschrieben.
            Assert.Equal(TwwKatalogAusgang.NameBelegt,
                         TwwNutzungsartCtrl.Aendern(id, e with { Bezeichner = "Eigen B", Bedarf = new[] { 0.0, 0.0, 0.0 } }).Ausgang);
            Assert.Equal(new[] { 7.0, 8.0, 9.0 }, TwwNutzungsartCtrl.Lies(id).BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal("Eigen B", TwwNutzungsartCtrl.Lies(andere).Name);

            Assert.Equal(TwwKatalogAusgang.NichtGefunden, TwwNutzungsartCtrl.Aendern(9999, e).Ausgang);
            Assert.Equal(TwwKatalogAusgang.NichtGefunden, TwwNutzungsartCtrl.Loeschen(9999).Ausgang);
        }

        // =================================================================================
        // 3 — Speichern unter
        // =================================================================================

        [Fact]
        public void Speichern_unter_legt_aus_einer_gesperrten_Zeile_eine_neue_eigene_Version_an()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int alt = TwwTestdatenbank.NutzungsartAnlegen("Auslieferung A", "T1", satz, TwwSchema.STATUS_AUSLIEFERUNG,
                                                          readOnly: true, beleg: "Sekundaerquelle intern");
            int zone = TwwTestdatenbank.ZoneAnlegen(1, alt, "Zone", 10.0);

            Nutzungsart vorlage = TwwNutzungsartCtrl.Lies(alt);
            TwwNutzungsartEntwurf e = TwwNutzungsartEntwurf.Aus(vorlage) with { Katalogversion = "T1-eigen", Bedarf = new[] { 7.0, 8.0, 9.0 } };

            TwwKatalogErgebnis erg = TwwNutzungsartCtrl.SpeichernUnter(alt, e);
            Assert.True(erg.Ok);
            Assert.NotEqual(alt, erg.Id);

            Nutzungsart neu = TwwNutzungsartCtrl.Lies(erg.Id);
            Assert.Equal("Auslieferung A", neu.Name);
            Assert.Equal("T1-eigen", neu.Katalogversion);
            Assert.Equal(new[] { 7.0, 8.0, 9.0 }, neu.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(ZapfKatalogstatus.Eigen, neu.Status);
            Assert.False(neu.ReadOnly);
            Assert.Equal(alt, neu.IdVorlage);
            Assert.Null(neu.Freigabe);
            // Provenienz und Herkunftsart der Vorlage uebernommen, Beleg ebenso.
            Assert.Equal(vorlage.Herkunft.Bedarf, neu.Herkunft.Bedarf);
            Assert.Equal(vorlage.Herkunft.Jahresgang, neu.Herkunft.Jahresgang);
            Assert.Equal(vorlage.Herkunft.Wochengang, neu.Herkunft.Wochengang);
            Assert.Equal(vorlage.Monatsfaktoren, neu.Monatsfaktoren);
            Assert.Equal(satz, neu.Tagesgaenge.Id);
            Assert.Equal("Sekundaerquelle intern", DataRepository.ExecuteScalar(
                "SELECT \"Beleg\" FROM \"Tab_TwwNutzungsart_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", erg.Id)));

            // Die Vorlage bleibt, wie sie war, und die Zone zeigt weiter auf sie.
            Nutzungsart danach = TwwNutzungsartCtrl.Lies(alt);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, danach.BedarfJeNiveauKwhJeEinheitTag);
            Assert.True(danach.ReadOnly);
            Assert.Equal(ZapfKatalogstatus.Auslieferung, danach.Status);
            Assert.Equal(alt, ZapfprofilCtrl.Lies(1).Zonen.Single(z => z.Id == zone).IdNutzungsart);

            // Dieselbe Version unter demselben Namen ist belegt; eine fehlende Vorlage benannt.
            Assert.Equal(TwwKatalogAusgang.NameBelegt, TwwNutzungsartCtrl.SpeichernUnter(alt, e).Ausgang);
            Assert.Equal(TwwKatalogAusgang.NichtGefunden,
                         TwwNutzungsartCtrl.SpeichernUnter(9999, e with { Katalogversion = "T9" }).Ausgang);
        }

        [Fact]
        public void Eine_freie_Zeile_laesst_sich_loeschen_und_ihre_Nachfolger_verlieren_nur_den_Verweis()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int alt = TwwTestdatenbank.NutzungsartAnlegen("Eigen A", "T1", satz);
            int neu = TwwNutzungsartCtrl.SpeichernUnter(alt,
                          TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(alt)) with { Katalogversion = "T2" }).Id;
            Assert.Equal(alt, TwwNutzungsartCtrl.Lies(neu).IdVorlage);

            TwwKatalogErgebnis erg = TwwNutzungsartCtrl.Loeschen(alt);
            Assert.True(erg.Ok);
            Assert.Null(TwwNutzungsartCtrl.Lies(alt));
            Assert.Null(TwwNutzungsartCtrl.Lies(neu).IdVorlage);
            Assert.Equal(new[] { neu }, TwwNutzungsartCtrl.Liste().Select(z => z.Id).ToArray());
        }

        [Fact]
        public void Ohne_Tabellen_lehnt_die_Pflege_benannt_ab()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);

            Assert.Empty(TwwNutzungsartCtrl.Liste());
            Assert.Null(TwwNutzungsartCtrl.Lies(1));
            Assert.False(TwwNutzungsartCtrl.IstBenutzt(1));
            Assert.False(TwwNutzungsartCtrl.IstReadOnly(1));
            Assert.Equal(TwwKatalogAusgang.TabellenFehlen, TwwNutzungsartCtrl.Neu(Entwurf("A", 1)).Ausgang);
            Assert.Equal(TwwKatalogAusgang.TabellenFehlen, TwwNutzungsartCtrl.Aendern(1, Entwurf("A", 1)).Ausgang);
            Assert.Equal(TwwKatalogAusgang.TabellenFehlen, TwwNutzungsartCtrl.SpeichernUnter(1, Entwurf("A", 1)).Ausgang);
            Assert.Equal(TwwKatalogAusgang.TabellenFehlen, TwwNutzungsartCtrl.Loeschen(1).Ausgang);
            Assert.Empty(TwwNutzungsartCtrl.Katalogfilterzeilen());
        }

        // =================================================================================
        // 4 — Katalogliste und Registry (P8)
        // =================================================================================

        [Fact]
        public void Die_Katalogliste_fuellt_die_sechs_Spalten_des_Profils_und_schluesselt_ueber_die_ID()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int eigen = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int aus = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T2", satz, TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);

            Katalogfilterprofil profil = Katalogfilterprofil.FuerTwwNutzungsart();
            Assert.Equal(Katalogfilterprofil.SCHLUESSEL_TWW_NUTZUNGSART, profil.Schluessel);
            Assert.Equal(new[] { Katalogfilterprofil.SpBezeichner, Katalogfilterprofil.SpBezugsart, Katalogfilterprofil.SpKalender,
                                 Katalogfilterprofil.SpHerkunft, Katalogfilterprofil.SpKatalogversion, Katalogfilterprofil.SpStatus },
                         profil.Spalten.Select(s => s.Schluessel).ToArray());
            Assert.Equal("KFLT_SP_NUTZUNGSART", profil.Spalten[0].Titel);

            var zeilen = TwwNutzungsartCtrl.Katalogfilterzeilen();
            Assert.Equal(new[] { eigen.ToString(), aus.ToString() }, zeilen.Select(z => z.Schluessel).ToArray());
            Assert.Equal("Nutzung A", zeilen[0].Text(Katalogfilterprofil.SpBezeichner));
            Assert.Equal("T2", zeilen[1].Text(Katalogfilterprofil.SpKatalogversion));
            Assert.Equal("Personen", zeilen[0].Text(Katalogfilterprofil.SpBezugsart));
            Assert.Equal("Wohnen", zeilen[0].Text(Katalogfilterprofil.SpKalender));
            Assert.Equal("Fiktiv", zeilen[0].Text(Katalogfilterprofil.SpHerkunft));
            Assert.Equal("Eigen", zeilen[0].Text(Katalogfilterprofil.SpStatus));
            Assert.False(zeilen[0].Geschuetzt);
            Assert.True(zeilen[1].Geschuetzt);

            // Mit Uebersetzer: Schluessel ZPG_<GRUPPE>_<Name>; ein fehlender Text faellt auf den Namen zurueck.
            var uebersetzt = TwwNutzungsartCtrl.Katalogfilterzeilen(k => k == "ZPG_STATUS_Auslieferung" ? "<ausgeliefert>" : null);
            Assert.Equal("<ausgeliefert>", uebersetzt[1].Text(Katalogfilterprofil.SpStatus));
            Assert.Equal("Eigen", uebersetzt[0].Text(Katalogfilterprofil.SpStatus));
        }

        [Fact]
        public void Die_Dublettenpruefung_haelt_zwei_Versionen_und_zaehlt_die_Verwendung_ueber_die_ID()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int v1 = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int v2 = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T2", satz);
            TwwTestdatenbank.ZoneAnlegen(1, v2, "Zone", 10.0);

            KatalogDefinition k = KatalogRegistry.Finde("TWW_NUTZUNGSART");
            ScanErgebnis scan = DublettenPruefung.ScanKatalog(k);
            Assert.Null(scan.Fehler);
            Assert.Equal(2, scan.Saetze.Count);
            Assert.Empty(scan.Namensgruppen);             // gleicher Bezeichner, zwei Versionen: natuerlicher Schluessel verschieden
            Assert.Empty(scan.Inhaltsgruppen);            // die Katalogversion unterscheidet sie

            // Die Leerkopien-Regel loescht keine der beiden Versionen.
            BereinigungsErgebnis b = KatalogBereinigung.LeereKopienBereinigen(k);
            Assert.Equal(0, b.Geloescht);
            Assert.Equal(2, TwwNutzungsartCtrl.Liste().Count);

            // Die Verwendung zaehlt ueber die ID, nicht ueber den Namen.
            VerwendungsPruefung vp = k.VerwendungsPruefungen[0];
            Assert.Equal(0, KatalogBereinigung.VerwendungZaehlen(vp, scan.Saetze.Single(s => s.Id == v1), out string f1));
            Assert.Equal(1, KatalogBereinigung.VerwendungZaehlen(vp, scan.Saetze.Single(s => s.Id == v2), out string f2));
            Assert.Null(f1);
            Assert.Null(f2);

            // Tagesgangsatz: Datenblock gelesen, die Nutzungsarten zaehlen als Verwendung.
            KatalogDefinition ks = KatalogRegistry.Finde("TWW_TAGESGANGSATZ");
            ScanErgebnis scanSatz = DublettenPruefung.ScanKatalog(ks);
            Assert.Null(scanSatz.Fehler);
            Assert.Equal(2, KatalogBereinigung.VerwendungZaehlen(ks.VerwendungsPruefungen[0], scanSatz.Saetze.Single(), out _));
        }
    }
}
