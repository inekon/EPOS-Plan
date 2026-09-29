using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Konzept Navigation Berichte &amp; Kosten, Variante A — Etappen A2 und A3.</b>
    ///
    /// <para><b>A3 „zuletzt erstellt":</b> <see cref="BerichtCtrl.MerkeErstellt"/> legt den
    /// Zeitpunkt eines erfolgreich erstellten Berichts im JSON der Tabelle
    /// <c>Berichtskonfiguration</c> ab — ohne Schemaschritt. Er ist Protokoll, keine Eingabe:
    /// <see cref="BerichtCtrl.Speichere"/> behält ihn, wenn die übergebene Konfiguration keinen
    /// neueren trägt, und die übrige Auswahl bleibt beim Merken, wie sie gespeichert ist.</para>
    ///
    /// <para><b>A2 Kurzstände:</b> Die Rahmenhülle <c>BerichteKostenHuelle</c> nennt je Reiter,
    /// was sie OHNE Rechnung weiß — Übersicht und Kosten nach dem Laden ihrer Seite,
    /// Wirtschaftlichkeit aus den gespeicherten Ergebnissen, Bericht aus der Konfiguration —
    /// und meldet einen neuen Stand über <see cref="SeitenZustand.KurzstandGeaendert"/>.</para>
    ///
    /// <para>Geschrieben wird nur an einer Arbeitskopie der Testdatenbank
    /// (<see cref="TestDatenbank"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BerichteKostenKurzstandTests : IDisposable
    {
        /// <summary>Vergleichsgruppe mit gespeicherten Ergebnissen: Stamm 1019, Varianten „Test1“ (1023) und „Test2“ (1024).</summary>
        private const int GRUPPE = 1019;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        // =====================================================================
        //  A3 — der Zeitpunkt im JSON der Konfiguration
        // =====================================================================

        [Fact]
        public void Der_Zeitpunkt_steht_nur_im_JSON_wenn_es_ihn_gibt()
        {
            string ohne = BerichtsKonfiguration.Standard().NachJson();
            Assert.DoesNotContain("ZuletztErstellt", ohne, StringComparison.Ordinal);

            BerichtsKonfiguration mit = BerichtsKonfiguration.Standard();
            mit.ZuletztErstellt = BerichtsKonfiguration.Zeitstempel(new DateTime(2026, 9, 27, 14, 5, 30));
            string json = mit.NachJson();
            Assert.Contains("\"ZuletztErstellt\":\"2026-09-27T14:05:30\"", json, StringComparison.Ordinal);
            Assert.Equal(new DateTime(2026, 9, 27, 14, 5, 30), BerichtsKonfiguration.AusJson(json).ZuletztErstelltAm);
        }

        /// <summary>Duldsam wie der übrige Leseweg: Ein unlesbarer Wert kostet nur den Zeitpunkt, nie die Auswahl.</summary>
        [Fact]
        public void Ein_unlesbarer_Zeitpunkt_kostet_nur_sich_selbst()
        {
            BerichtsKonfiguration k = BerichtsKonfiguration.AusJson(
                "{\"ZuletztErstellt\":17,\"Ausgabe\":\"Excel\",\"AktiveBausteine\":[\"anhang\"]}");
            Assert.Equal("17", k.ZuletztErstellt);
            Assert.Null(k.ZuletztErstelltAm);
            Assert.Equal("Excel", k.Ausgabe);
            Assert.Equal(new[] { "anhang" }, k.AktiveBausteine);

            Assert.Null(BerichtsKonfiguration.AusJson("{\"ZuletztErstellt\":{\"x\":1}}").ZuletztErstellt);
            Assert.Null(BerichtsKonfiguration.LiesZeitstempel("27.09.2026 14:05"));
        }

        [Fact]
        public void MerkeErstellt_legt_den_Zeitpunkt_ab_und_laesst_die_Auswahl_stehen()
        {
            if (!_db.Vorhanden) return;
            int id = ProjektAnlegen("Zuletzt erstellt A3");
            var ctrl = new BerichtCtrl();

            Assert.Null(ctrl.ZuletztErstellt(id));

            BerichtsKonfiguration auswahl = BerichtsKonfiguration.Standard();
            auswahl.ZielOrdner = "Probeordner A3";
            auswahl.Ausgabe = "Beide";
            Assert.True(ctrl.Speichere(id, auswahl));

            var zeitpunkt = new DateTime(2026, 9, 27, 14, 5, 30);
            Assert.True(ctrl.MerkeErstellt(id, zeitpunkt));

            Assert.Equal(zeitpunkt, ctrl.ZuletztErstellt(id));
            BerichtsKonfiguration geladen = ctrl.Lade(id);
            Assert.Equal("Probeordner A3", geladen.ZielOrdner);
            Assert.Equal("Beide", geladen.Ausgabe);
        }

        /// <summary>
        /// Das Merken der Häkchen (vor jedem Lauf) und der Vorlagenwahl baut seine Konfiguration
        /// ohne den Zeitpunkt — gespeichert bleibt er trotzdem.
        /// </summary>
        [Fact]
        public void Speichere_behaelt_den_gemerkten_Zeitpunkt()
        {
            if (!_db.Vorhanden) return;
            int id = ProjektAnlegen("Zuletzt erstellt A3 — Speichern");
            var ctrl = new BerichtCtrl();
            var zeitpunkt = new DateTime(2026, 9, 27, 9, 0, 0);
            Assert.True(ctrl.MerkeErstellt(id, zeitpunkt));

            BerichtsKonfiguration neu = BerichtsKonfiguration.Standard();
            neu.ZielOrdner = "anderer Ordner";
            Assert.Null(neu.ZuletztErstellt);
            Assert.True(ctrl.Speichere(id, neu));

            Assert.Equal(zeitpunkt, ctrl.ZuletztErstellt(id));
            Assert.Equal("anderer Ordner", ctrl.Lade(id).ZielOrdner);
        }

        [Fact]
        public void Der_spaetere_Zeitpunkt_gilt()
        {
            if (!_db.Vorhanden) return;
            int id = ProjektAnlegen("Zuletzt erstellt A3 — später");
            var ctrl = new BerichtCtrl();

            var frueh = new DateTime(2026, 9, 26, 18, 12, 0);
            var spaet = new DateTime(2026, 9, 27, 8, 30, 0);
            Assert.True(ctrl.MerkeErstellt(id, frueh));
            Assert.True(ctrl.MerkeErstellt(id, spaet));
            Assert.Equal(spaet, ctrl.ZuletztErstellt(id));

            // Eine Konfiguration mit älterem Zeitpunkt überschreibt den gespeicherten nicht.
            BerichtsKonfiguration alt = BerichtsKonfiguration.Standard();
            alt.ZuletztErstellt = BerichtsKonfiguration.Zeitstempel(frueh);
            Assert.True(ctrl.Speichere(id, alt));
            Assert.Equal(spaet, ctrl.ZuletztErstellt(id));
        }

        [Fact]
        public void Ohne_Stammprojekt_wird_nichts_gemerkt()
        {
            var ctrl = new BerichtCtrl();
            Assert.False(ctrl.MerkeErstellt(0, DateTime.Now));
            Assert.Null(ctrl.ZuletztErstellt(0));
        }

        // =====================================================================
        //  A2 — die Kurzstände der Rahmenhülle
        // =====================================================================

        [Fact]
        public void Vor_dem_Laden_der_Uebersicht_nennt_ihr_Reiter_nichts_und_danach_die_Gruppe()
        {
            if (!_db.Vorhanden) return;
            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(GRUPPE, "");
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben();
            var status = (Func<string, Reiterstatus>)gaben["Status"];
            var zustand = (SeitenZustand)gaben[SeitenZustand.PARAMETER];
            int gemeldet = 0;
            zustand.KurzstandGeaendert += () => gemeldet++;

            Assert.Null(status(BerichteKostenSeite.SEITE_UEBERSICHT));
            Assert.Null(status(BerichteKostenSeite.SEITE_KOSTEN));

            var seiten = (Func<string, IReadOnlyDictionary<string, object>>)gaben["SeitenGaben"];
            ((Func<UebersichtStand>)seiten(BerichteKostenSeite.SEITE_UEBERSICHT)["Laden"])();

            Reiterstatus uebersicht = status(BerichteKostenSeite.SEITE_UEBERSICHT);
            Assert.NotNull(uebersicht);
            Assert.Contains("3 Versionen", uebersicht.Text, StringComparison.Ordinal);
            Assert.True(gemeldet >= 1, "Der neue Kurzstand wurde nicht gemeldet.");

            // Ein zweites Laden mit demselben Stand meldet nichts Neues.
            int vorher = gemeldet;
            ((Func<UebersichtStand>)seiten(BerichteKostenSeite.SEITE_UEBERSICHT)["Laden"])();
            Assert.Equal(vorher, gemeldet);

            // Der Stammname für das Ende der Reiterzeile.
            Assert.False(string.IsNullOrEmpty(((Func<string>)gaben["Stamm"])()));
            Assert.Equal(R.BK_LBL_STAMM_KURZ, gaben["StammBeschriftung"]);
        }

        [Fact]
        public void Die_Kosten_nennen_Traeger_und_Befunde_nach_dem_Laden()
        {
            if (!_db.Vorhanden) return;
            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(GRUPPE, "");
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben();
            var status = (Func<string, Reiterstatus>)gaben["Status"];
            var seiten = (Func<string, IReadOnlyDictionary<string, object>>)gaben["SeitenGaben"];

            ((Func<UebersichtStand>)seiten(BerichteKostenSeite.SEITE_UEBERSICHT)["Laden"])();
            ((Func<KostenStand>)seiten(BerichteKostenSeite.SEITE_KOSTEN)["Laden"])();

            Reiterstatus kosten = status(BerichteKostenSeite.SEITE_KOSTEN);
            Assert.NotNull(kosten);
            Assert.Contains("Träger", kosten.Text, StringComparison.Ordinal);
            if (kosten.Stufe == Statusstufe.Warnung)
            {
                Assert.Contains("Warnung", kosten.Text, StringComparison.Ordinal);
                Assert.True(int.Parse(kosten.Kurz, CultureInfo.InvariantCulture) > 0);
            }
        }

        /// <summary>
        /// Die Wirtschaftlichkeit liest nur die GESPEICHERTEN Ergebnisse — die Seite selbst entsteht
        /// dabei nicht: Die beste Variante der Gruppe 1019 ist „Test1“ mit positiver Differenz.
        /// </summary>
        [Fact]
        public void Die_Wirtschaftlichkeit_nennt_die_beste_Variante_aus_den_gespeicherten_Ergebnissen()
        {
            if (!_db.Vorhanden) return;
            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(GRUPPE, "");
            var status = (Func<string, Reiterstatus>)huelle.Gaben()["Status"];

            Reiterstatus wirtschaft = status(BerichteKostenSeite.SEITE_WIRTSCHAFT);

            Assert.NotNull(wirtschaft);
            Assert.StartsWith("beste: Test1, +", wirtschaft.Text, StringComparison.Ordinal);
            Assert.Contains("€", wirtschaft.Text, StringComparison.Ordinal);
            if (wirtschaft.Stufe == Statusstufe.Warnung)
                Assert.Equal(R.BK_STATUS_VERALTET, wirtschaft.Kurz);
            else
                Assert.StartsWith("+", wirtschaft.Kurz, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Bericht_nennt_den_gemerkten_Zeitpunkt()
        {
            if (!_db.Vorhanden) return;
            var huelle = new BerichteKostenHuelle();
            huelle.SetzeProjekt(GRUPPE, "");
            var status = (Func<string, Reiterstatus>)huelle.Gaben()["Status"];

            Reiterstatus vorher = status(BerichteKostenSeite.SEITE_BERICHT);
            Assert.Equal(R.BK_STATUS_BERICHT_KEINER, vorher.Text);
            Assert.Equal(Statusstufe.Normal, vorher.Stufe);

            Assert.True(new BerichtCtrl().MerkeErstellt(GRUPPE, new DateTime(2026, 9, 27, 14, 5, 30)));

            // Nach dem Projektkontext (auch nach einem Lauf) liest die Zeile neu.
            huelle.SetzeProjekt(GRUPPE, "");
            Reiterstatus nachher = status(BerichteKostenSeite.SEITE_BERICHT);
            Assert.Equal("zuletzt 27.09.26 14:05", nachher.Text);
            Assert.Equal("27.09. 14:05", nachher.Kurz);
        }

        [Fact]
        public void Der_Kurzstand_des_Berichts_hat_zwei_Sprachen()
        {
            var zeit = new DateTime(2026, 9, 27, 14, 5, 30);
            Reiterstatus deutsch = BerichteKostenHuelle.BerichtKurzstand(zeit);
            Assert.Equal("zuletzt 27.09.26 14:05", deutsch.Text);

            using (new Kulturvorrichtung("en-US"))
            {
                Reiterstatus englisch = BerichteKostenHuelle.BerichtKurzstand(zeit);
                Assert.Equal("last 2026-09-27 14:05", englisch.Text);
                Assert.Equal("09-27 14:05", englisch.Kurz);
                Assert.Equal("none created yet", BerichteKostenHuelle.BerichtKurzstand(null).Text);
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Legt ein leeres Projekt in der Arbeitskopie an und liefert seine Kennung.</summary>
        private static int ProjektAnlegen(string name)
        {
            return DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                new[] { new DbParam("@name", name) });
        }
    }
}
