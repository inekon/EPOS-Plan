using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die neuen Felder der Vorlagenwahl in <see cref="BerichtsKonfiguration"/> (Konzept
    /// Berichtsvorlagen 10.3, Zeile „Abweichung“; BV-E1, Teil A3): ältere JSONs bleiben lesbar,
    /// ohne Abweichung bleibt das JSON wie bisher, die Felder überstehen den Rundlauf — auch über
    /// die Tabelle <c>Berichtskonfiguration</c> —, und ein falsch geschriebenes neues Feld kostet
    /// nicht die übrige Auswahl.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtsKonfigurationJsonTests
    {
        /// <summary>So schrieb die Fassung vor BV-E1 die Konfiguration (ohne Vorlagenfelder).</summary>
        private const string ALT =
            "{\"VariantenIds\":[3,5],\"AktiveBausteine\":[\"deckblatt\",\"anhang\"],\"NeuRechnen\":true," +
            "\"Ausgabe\":\"Beide\",\"ZielOrdner\":\"C:\\\\Berichte\"}";

        [Fact]
        public void Aeltere_JSONs_bleiben_lesbar_ohne_Abweichung()
        {
            BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(ALT);
            Assert.Equal(new[] { 3, 5 }, k.VariantenIds);
            Assert.Equal(new[] { "deckblatt", "anhang" }, k.AktiveBausteine);
            Assert.Equal("Beide", k.Ausgabe);
            Assert.Equal("C:\\Berichte", k.ZielOrdner);
            Assert.Null(k.VorlageWordQuelle);
            Assert.Null(k.VorlageWordDatei);
            Assert.Null(BerichtsvorlagenCtrl.AbweichungId(k));

            // Noch ältere Fassungen kannten nicht einmal alle heutigen Felder.
            BerichtsKonfiguration sehrAlt = BerichtsKonfiguration.AusJson("{\"AktiveBausteine\":[\"vergleich\"]}");
            Assert.Equal(new[] { "vergleich" }, sehrAlt.AktiveBausteine);
            Assert.Equal("Word", sehrAlt.Ausgabe);
            Assert.Null(sehrAlt.VorlageWordQuelle);
        }

        [Fact]
        public void Ohne_Abweichung_schreibt_das_JSON_keine_Vorlagenfelder()
        {
            string json = BerichtsKonfiguration.Standard().NachJson();
            Assert.DoesNotContain("VorlageWord", json);
            Assert.Equal(json, BerichtsKonfiguration.AusJson(json).NachJson());
            Assert.Equal(BerichtsKonfiguration.AusJson(ALT).NachJson(),
                         BerichtsKonfiguration.AusJson(BerichtsKonfiguration.AusJson(ALT).NachJson()).NachJson());
        }

        [Fact]
        public void Die_neuen_Felder_ueberstehen_den_Rundlauf()
        {
            BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(ALT);
            k.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
            k.VorlageWordDatei = "Angebot „Müller“.docx";
            string json = k.NachJson();
            Assert.Contains("\"VorlageWordQuelle\":\"eigen\"", json);

            BerichtsKonfiguration zurueck = BerichtsKonfiguration.AusJson(json);
            Assert.Equal("eigen", zurueck.VorlageWordQuelle);
            Assert.Equal("Angebot „Müller“.docx", zurueck.VorlageWordDatei);
            Assert.Equal(new[] { 3, 5 }, zurueck.VariantenIds);
            Assert.Equal("Beide", zurueck.Ausgabe);
            Assert.Equal("eigen:Angebot „Müller“.docx", BerichtsvorlagenCtrl.AbweichungId(zurueck));

            k.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_STANDARD;
            k.VorlageWordDatei = null;
            BerichtsKonfiguration standard = BerichtsKonfiguration.AusJson(k.NachJson());
            Assert.Equal("standard", standard.VorlageWordQuelle);
            Assert.Null(standard.VorlageWordDatei);
            Assert.DoesNotContain("VorlageWordDatei", k.NachJson());
        }

        [Fact]
        public void Ein_falsch_geschriebenes_neues_Feld_kostet_nicht_die_Auswahl()
        {
            BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(
                "{\"Ausgabe\":\"Excel\",\"VariantenIds\":[7],\"VorlageWordQuelle\":5,\"VorlageWordDatei\":{\"x\":[1,2]}}");
            Assert.Equal("Excel", k.Ausgabe);
            Assert.Equal(new[] { 7 }, k.VariantenIds);
            Assert.Equal("5", k.VorlageWordQuelle);
            Assert.Null(k.VorlageWordDatei);
            Assert.Null(BerichtsvorlagenCtrl.AbweichungId(k));   // unbekannte Quelle = keine Abweichung

            BerichtsKonfiguration wahr = BerichtsKonfiguration.AusJson("{\"VorlageWordQuelle\":true,\"VorlageWordDatei\":null}");
            Assert.Equal("true", wahr.VorlageWordQuelle);
            Assert.Null(wahr.VorlageWordDatei);
            Assert.Equal("1.5", BerichtsKonfiguration.AusJson("{\"VorlageWordQuelle\":1.5}").VorlageWordQuelle);
            Assert.Equal(new[] { "x" }, BerichtsKonfiguration.AusJson("{\"VorlageWordDatei\":[1],\"AktiveBausteine\":[\"x\"]}").AktiveBausteine);
        }

        /// <summary>
        /// Das Szenario des Wirtschaftlichkeitsberichts (Fachvorgabe E31, Nach #582): gespeichert wird der Schlüssel aus
        /// <see cref="WirtschaftlichkeitSzenario"/>; ein fehlendes, leeres oder unbekanntes Feld und ein Wert anderer Art
        /// lesen sich als Erwartet, ohne die übrige Auswahl zu kosten — Altbestand bleibt gültig.
        /// </summary>
        [Fact]
        public void Das_Szenario_liest_sich_duldsam_und_uebersteht_den_Rundlauf()
        {
            // Fehlendes Feld: der Altbestand.
            Assert.Equal(WirtschaftlichkeitSzenario.ERWARTET, BerichtsKonfiguration.AusJson(ALT).Szenario);
            Assert.Equal(WirtschaftlichkeitSzenario.ERWARTET, BerichtsKonfiguration.Standard().Szenario);

            // Unbekannter Wert, leer, null und ein Wert anderer Art.
            foreach (string wert in new[] { "\"Mittel\"", "\"\"", "null", "2", "{\"x\":1}", "\"worst\"" })
            {
                BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(
                    "{\"Ausgabe\":\"Excel\",\"VariantenIds\":[7],\"Szenario\":" + wert + "}");
                Assert.Equal(WirtschaftlichkeitSzenario.ERWARTET, k.Szenario);
                Assert.Equal("Excel", k.Ausgabe);
                Assert.Equal(new[] { 7 }, k.VariantenIds);
            }

            // Gültige Schlüssel überstehen den Rundlauf; gespeichert wird der Schlüssel selbst.
            foreach (string sz in WirtschaftlichkeitSzenario.Alle)
            {
                BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(ALT);
                k.Szenario = sz;
                string json = k.NachJson();
                Assert.Contains("\"Szenario\":\"" + sz + "\"", json);
                BerichtsKonfiguration zurueck = BerichtsKonfiguration.AusJson(json);
                Assert.Equal(sz, zurueck.Szenario);
                Assert.Equal(new[] { 3, 5 }, zurueck.VariantenIds);
                Assert.Equal("Beide", zurueck.Ausgabe);
            }
        }

        /// <summary>
        /// Die Szenariodarstellung (Etappe VB‑E1): ein eigenes Feld neben dem Szenario. Fehlend, leer, unbekannt oder von
        /// anderer Art liest es sich als einzeln, ohne die übrige Auswahl zu kosten; geschrieben wird es nur in
        /// VALERI-Darstellung — das JSON einer Einzelwahl bleibt byte-gleich. Das Szenario daneben bleibt unberührt.
        /// </summary>
        [Fact]
        public void Die_Szenariodarstellung_liest_sich_duldsam_und_uebersteht_den_Rundlauf()
        {
            // Fehlendes Feld: der Altbestand und die Vorgabe.
            Assert.Equal(BerichtsKonfiguration.DARSTELLUNG_EINZELN, BerichtsKonfiguration.AusJson(ALT).Szenariodarstellung);
            Assert.False(BerichtsKonfiguration.AusJson(ALT).IstValeri);
            Assert.Equal(BerichtsKonfiguration.DARSTELLUNG_EINZELN, BerichtsKonfiguration.Standard().Szenariodarstellung);

            // Unbekannt, leer, null und ein Wert anderer Art.
            foreach (string wert in new[] { "\"Drei\"", "\"\"", "null", "2", "{\"x\":1}", "[1]" })
            {
                BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(
                    "{\"Ausgabe\":\"Excel\",\"VariantenIds\":[7],\"Szenario\":\"" + WirtschaftlichkeitSzenario.BEST + "\",\"Szenariodarstellung\":" + wert + "}");
                Assert.Equal(BerichtsKonfiguration.DARSTELLUNG_EINZELN, k.Szenariodarstellung);
                Assert.Equal(WirtschaftlichkeitSzenario.BEST, k.Szenario);
                Assert.Equal("Excel", k.Ausgabe);
                Assert.Equal(new[] { 7 }, k.VariantenIds);
            }

            // Duldsam in der Schreibweise.
            Assert.True(BerichtsKonfiguration.AusJson("{\"Szenariodarstellung\":\" valeri \"}").IstValeri);

            // Einzeln: das JSON bleibt das der Fassung ohne Feld.
            BerichtsKonfiguration einzeln = BerichtsKonfiguration.AusJson(ALT);
            string ohneFeld = einzeln.NachJson();
            Assert.DoesNotContain("Szenariodarstellung", ohneFeld);
            einzeln.Szenariodarstellung = "EINZELN";
            Assert.Equal(ohneFeld, einzeln.NachJson());

            // VALERI übersteht den Rundlauf; das Szenario daneben bleibt, wie es ist.
            BerichtsKonfiguration valeri = BerichtsKonfiguration.AusJson(ALT);
            valeri.Szenario = WirtschaftlichkeitSzenario.WORST;
            valeri.Szenariodarstellung = BerichtsKonfiguration.DARSTELLUNG_VALERI;
            string json = valeri.NachJson();
            Assert.Contains("\"Szenariodarstellung\":\"VALERI\"", json);
            BerichtsKonfiguration zurueck = BerichtsKonfiguration.AusJson(json);
            Assert.True(zurueck.IstValeri);
            Assert.Equal(WirtschaftlichkeitSzenario.WORST, zurueck.Szenario);
            Assert.Equal(new[] { 3, 5 }, zurueck.VariantenIds);
            Assert.Equal("Beide", zurueck.Ausgabe);
        }

        /// <summary>Der Rundlauf über die Tabelle <c>Berichtskonfiguration</c> mit <see cref="BerichtCtrl"/> (unverändert).</summary>
        [Fact]
        public void Speichern_und_Laden_ueber_die_Tabelle_behaelt_die_Vorlagenwahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                new[] { new DbParam("@name", "Vorlagenwahl BV-E1") });
            Assert.True(id > 0, "Das Probeprojekt liess sich nicht anlegen.");

            var ctrl = new BerichtCtrl();
            BerichtsKonfiguration ohne = BerichtsKonfiguration.Standard();
            Assert.True(ctrl.Speichere(id, ohne));
            Assert.Null(ctrl.Lade(id).VorlageWordQuelle);

            BerichtsKonfiguration mit = BerichtsKonfiguration.Standard();
            mit.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
            mit.VorlageWordDatei = "Kurzbericht.docx";
            Assert.True(ctrl.Speichere(id, mit));
            BerichtsKonfiguration geladen = ctrl.Lade(id);
            Assert.Equal("eigen", geladen.VorlageWordQuelle);
            Assert.Equal("Kurzbericht.docx", geladen.VorlageWordDatei);
            Assert.Equal(mit.AktiveBausteine, geladen.AktiveBausteine);
        }
    }
}
