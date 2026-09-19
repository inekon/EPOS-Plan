using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KONZEPT § 2.16 — die Vergütung je Variante.</b> Die PV-Vergütungsangaben
    /// standen je Projekt, gelesen wurde die Zeile des jeweiligen Stands: Eine Variante
    /// bekam sie genau dann, wenn sie NACH der Pflege des Stamms angelegt wurde — der
    /// Kopierlauf nahm die Zeile mit und fror sie ein. Von außen war das nicht zu
    /// erkennen. Ab hier ist es eine Wahl.
    ///
    /// <para><b>Was hier festgehalten wird.</b> Sechs Dinge:
    /// <list type="number">
    ///   <item><description>Der Schemaschritt 93 steht, und die Testdatenbank führt die
    ///     Spalte leer — keine Zeile, also auch keine Wahl.</description></item>
    ///   <item><description><see cref="ProjektPhotovoltaikCtrl.LiesAufgeloest"/> löst
    ///     alle vier Lagen auf: Stamm, Variante mit eigener Zeile, Variante die
    ///     übernimmt, Gruppe ohne Zeile.</description></item>
    ///   <item><description>Eine eigene Zeile wirkt NUR bei ihrer Variante; die
    ///     Schwester übernimmt weiter.</description></item>
    ///   <item><description>Eine Stammänderung erreicht die übernehmende Variante, die
    ///     eigene nicht.</description></item>
    ///   <item><description>Der Löschfall: Wer den Stamm löscht, verliert die Vergütung
    ///     nicht — die übernehmenden Varianten bekommen sie vorher als eigene
    ///     Zeile.</description></item>
    ///   <item><description>Der Kopierlauf lässt die PV-Zeile aus, und die Herkunft
    ///     reist im Nachweisumschlag mit.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvVerguetungJeVarianteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Der Stamm der Prüfgruppe — ein Projekt der Testdatenbank mit zwei
        /// eingetragenen Varianten (<c>Tab_Variante</c>).</summary>
        private const int STAMM = 1019;

        /// <summary>Die zwei Varianten des Stamms 1019.</summary>
        private const int VARIANTE_A = 1023;
        private const int VARIANTE_B = 1024;

        // =================================================================
        // 1 — Schemaschritt 93
        // =================================================================

        /// <summary>
        /// Der Zielstand trägt den Schritt 93, und die eine Spalte hängt an
        /// <c>Tab_ProjektPhotovoltaik</c>. Die Liste ist die EINE Quelle, aus der sich
        /// Migration, Werkzeug und Testdatenbank bedienen; der Typ ist der NULLBARE
        /// Wahrheitswert — mit <c>NOT NULL DEFAULT 0</c> gäbe es die dritte Aussage
        /// nicht.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_93()
        {
            Assert.True(SchemaStand.Zielversion >= 93,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 93.");
            Assert.Single(SchemaKatalog.Schritt93_VerguetungJeVariante);
            Assert.Equal(SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK,
                         SchemaKatalog.Schritt93_VerguetungJeVariante[0].Tabelle);
            Assert.Equal(SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM,
                         SchemaKatalog.Schritt93_VerguetungJeVariante[0].Name);
            Assert.Equal("YESNO_NULL",
                         SchemaKatalog.Schritt93_VerguetungJeVariante[0].TypDefinition);

            // Der Typ uebersetzt in ein nullbares 0/1 - kein NOT NULL, keine Vorgabe.
            string typ = StilleDb.SqliteSpaltenTyp(SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM,
                                                   "YESNO_NULL");
            Assert.Contains("IN (0,1)", typ);
            Assert.DoesNotContain("NOT NULL", typ);
            Assert.DoesNotContain("DEFAULT", typ);
        }

        /// <summary>
        /// Die Testdatenbank führt die Spalte — und <c>Tab_ProjektPhotovoltaik</c> keine
        /// einzige Zeile. Beide DML der Ableitung fassen deshalb nichts an; das ist die
        /// Ergebnisneutralität, auf der die Byte-Gleichheit des Referenzlaufs beruht.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_fuehrt_die_Spalte_und_keine_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(PvVerguetungJeVariante.SpalteVorhanden());
            Assert.Equal(0, Zaehle("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK));
            Assert.Equal(0, PvVerguetungJeVariante.OhneWahl());
            Assert.Equal(0, PvVerguetungJeVariante.OhneZeileBeiAktivemStamm());
        }

        /// <summary>
        /// Die Ableitung ist wiederholbar und ergebnisneutral: Eine vorhandene Zeile
        /// wird zur EIGENEN (0) und behält jeden ihrer Werte; eine Variante ohne Zeile
        /// bekommt bei aktivem Stamm eine eigene, INAKTIVE Spur. Ein zweiter Lauf fasst
        /// nichts mehr an.
        /// </summary>
        [Fact]
        public void Die_Ableitung_kennzeichnet_eigene_Zeilen_und_legt_die_inaktive_Spur()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektPhotovoltaikCtrl();
            Schreibe(ctrl, STAMM, aktiv: true, aw: 7.5, uebernahme: false);

            // Variante A bekommt eine eigene Zeile, deren Wahl noch NULL ist - genau der
            // Bestand vor dem Schritt.
            Schreibe(ctrl, VARIANTE_A, aktiv: true, aw: 9.0, uebernahme: false);
            DataRepository.ExecuteNonQuery(
                "UPDATE " + SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK + " SET [" +
                SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM + "] = NULL WHERE ID_Projekt = " + VARIANTE_A);
            // Variante B hat keine Zeile.
            Loesche(VARIANTE_B);

            Assert.Equal(1, PvVerguetungJeVariante.OhneWahl());
            Assert.Equal(1, PvVerguetungJeVariante.OhneZeileBeiAktivemStamm());

            foreach (KeyValuePair<string, string> a in PvVerguetungJeVariante.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            ProjektPhotovoltaikModel a1 = ctrl.Lies(VARIANTE_A);
            Assert.NotNull(a1);
            Assert.False(a1.UebernahmeStamm);
            Assert.True(a1.Aktiv);
            Assert.Equal(9.0, a1.AwOverride);

            ProjektPhotovoltaikModel b1 = ctrl.Lies(VARIANTE_B);
            Assert.NotNull(b1);
            Assert.False(b1.UebernahmeStamm);
            Assert.False(b1.Aktiv);          // die ergebnisneutrale Spur

            // Wiederholbar.
            Assert.Equal(0, PvVerguetungJeVariante.OhneWahl());
            Assert.Equal(0, PvVerguetungJeVariante.OhneZeileBeiAktivemStamm());
        }

        // =================================================================
        // 2 — Die Auflösung
        // =================================================================

        /// <summary>
        /// Alle vier Lagen: Der Stamm nimmt seine eigene Zeile; eine Variante ohne Zeile
        /// die des Stamms; eine Variante mit gesetztem Kennzeichen ebenfalls; eine
        /// Variante mit eigener, geltender Zeile ihre eigene. Ohne jede Zeile bleibt es
        /// beim Flat-Pfad (<c>Modell</c> ist <c>null</c>).
        /// </summary>
        [Fact]
        public void Die_Aufloesung_kennt_vier_Lagen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektPhotovoltaikCtrl();
            Loesche(STAMM); Loesche(VARIANTE_A); Loesche(VARIANTE_B);

            // (a) Keine Zeile in der ganzen Gruppe - Flat-Pfad.
            Assert.Null(ctrl.LiesAufgeloest(VARIANTE_A).Modell);
            Assert.Null(ctrl.LiesAufgeloest(STAMM).Modell);

            // (b) Nur der Stamm pflegt.
            Schreibe(ctrl, STAMM, aktiv: true, aw: 7.5, uebernahme: false);

            PvVerguetungStand stamm = ctrl.LiesAufgeloest(STAMM);
            Assert.False(stamm.Uebernommen);
            Assert.Equal(STAMM, stamm.IdQuelle);
            Assert.Equal(7.5, stamm.Modell.AwOverride);

            PvVerguetungStand ohneZeile = ctrl.LiesAufgeloest(VARIANTE_A);
            Assert.True(ohneZeile.Uebernommen);
            Assert.Equal(STAMM, ohneZeile.IdQuelle);
            Assert.Equal(7.5, ohneZeile.Modell.AwOverride);

            // (c) Variante mit eigener, geltender Zeile.
            Schreibe(ctrl, VARIANTE_A, aktiv: true, aw: 9.0, uebernahme: false);
            PvVerguetungStand eigen = ctrl.LiesAufgeloest(VARIANTE_A);
            Assert.False(eigen.Uebernommen);
            Assert.Equal(VARIANTE_A, eigen.IdQuelle);
            Assert.Equal(9.0, eigen.Modell.AwOverride);

            // (d) Dieselbe Zeile, Kennzeichen gesetzt - der Stamm gilt wieder, die Zeile
            //     bleibt als Rueckweg stehen.
            Assert.True(ctrl.SetzeUebernahme(VARIANTE_A, true));
            PvVerguetungStand zurueck = ctrl.LiesAufgeloest(VARIANTE_A);
            Assert.True(zurueck.Uebernommen);
            Assert.Equal(7.5, zurueck.Modell.AwOverride);
            Assert.NotNull(ctrl.Lies(VARIANTE_A));
            Assert.Equal(9.0, ctrl.Lies(VARIANTE_A).AwOverride);
        }

        /// <summary>
        /// Die eigene Zeile wirkt NUR bei ihrer Variante — die Schwester derselben
        /// Gruppe übernimmt weiter, und der Stamm bleibt unberührt.
        /// </summary>
        [Fact]
        public void Eine_eigene_Zeile_wirkt_nur_bei_ihrer_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektPhotovoltaikCtrl();
            Loesche(STAMM); Loesche(VARIANTE_A); Loesche(VARIANTE_B);

            Schreibe(ctrl, STAMM, aktiv: true, aw: 7.5, uebernahme: false);
            Schreibe(ctrl, VARIANTE_A, aktiv: true, aw: 9.0, uebernahme: false);

            Assert.Equal(9.0, ctrl.LiesAufgeloest(VARIANTE_A).Modell.AwOverride);
            Assert.Equal(7.5, ctrl.LiesAufgeloest(VARIANTE_B).Modell.AwOverride);
            Assert.Equal(7.5, ctrl.LiesAufgeloest(STAMM).Modell.AwOverride);
        }

        /// <summary>
        /// Eine Stammänderung erreicht die übernehmende Variante beim nächsten Lesen —
        /// die eigene nicht. Das ist der Unterschied zur eingefrorenen Kopie von früher.
        /// </summary>
        [Fact]
        public void Eine_Stammaenderung_erreicht_nur_die_uebernehmende_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektPhotovoltaikCtrl();
            Loesche(STAMM); Loesche(VARIANTE_A); Loesche(VARIANTE_B);

            Schreibe(ctrl, STAMM, aktiv: true, aw: 7.5, uebernahme: false);
            Schreibe(ctrl, VARIANTE_A, aktiv: true, aw: 9.0, uebernahme: false);

            Schreibe(ctrl, STAMM, aktiv: true, aw: 6.0, uebernahme: false);

            Assert.Equal(6.0, ctrl.LiesAufgeloest(VARIANTE_B).Modell.AwOverride);
            Assert.Equal(9.0, ctrl.LiesAufgeloest(VARIANTE_A).Modell.AwOverride);
        }

        /// <summary>
        /// „Eigene Vergütung" legt die STAMMWERTE vor (Anwenderentscheid VV‑Q6) — der
        /// Anwender will eine Abweichung von einer bekannten Basis, keinen leeren Satz.
        /// Geschrieben wird dabei nichts.
        /// </summary>
        [Fact]
        public void Eigene_Werte_legen_die_Stammwerte_vor()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektPhotovoltaikCtrl();
            Loesche(STAMM); Loesche(VARIANTE_A); Loesche(VARIANTE_B);
            Schreibe(ctrl, STAMM, aktiv: true, aw: 7.5, uebernahme: false);

            ProjektPhotovoltaikModel vorlage = ctrl.VorlageAusStamm(VARIANTE_A);
            Assert.Equal(7.5, vorlage.AwOverride);
            Assert.Equal(VARIANTE_A, vorlage.ID_Projekt);
            Assert.False(vorlage.UebernahmeStamm);
            Assert.True(vorlage.Aktiv);
            Assert.Null(ctrl.Lies(VARIANTE_A));      // nichts geschrieben

            Assert.True(ctrl.Speichern(vorlage));
            Assert.Equal(7.5, ctrl.LiesAufgeloest(VARIANTE_A).Modell.AwOverride);
            Assert.False(ctrl.LiesAufgeloest(VARIANTE_A).Uebernommen);
        }

        // =================================================================
        // 3 — Löschfall und Kopierlauf
        // =================================================================

        /// <summary>
        /// Wer den Stamm löscht, nimmt den übernehmenden Varianten nicht still ihre
        /// Vergütung: Sie bekommen die Stammwerte vorher als EIGENE Zeile. Eine Variante
        /// mit eigenen Werten bleibt unberührt, und die Zeile des gelöschten Projekts
        /// fällt mit.
        /// </summary>
        [Fact]
        public void Das_Loeschen_des_Stamms_gibt_den_Varianten_die_Werte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new ProjektPhotovoltaikCtrl();
            Loesche(STAMM); Loesche(VARIANTE_A); Loesche(VARIANTE_B);

            Schreibe(ctrl, STAMM, aktiv: true, aw: 7.5, uebernahme: false);
            Schreibe(ctrl, VARIANTE_A, aktiv: true, aw: 9.0, uebernahme: false);   // eigene
            // Variante B uebernimmt (keine Zeile).

            new ProjektCtrl().Delete(STAMM);

            Assert.Null(ctrl.Lies(STAMM));                       // Loeschweitergabe
            Assert.Equal(9.0, ctrl.Lies(VARIANTE_A).AwOverride); // unberuehrt

            ProjektPhotovoltaikModel b = ctrl.Lies(VARIANTE_B);
            Assert.NotNull(b);
            Assert.Equal(7.5, b.AwOverride);
            Assert.False(b.UebernahmeStamm);
            Assert.True(b.Aktiv);
        }

        /// <summary>
        /// Der Kopierlauf lässt <c>Tab_ProjektPhotovoltaik</c> aus (feste Ausnahme wie
        /// <c>Berichtskonfiguration</c>) — eine neue Variante erbt damit „übernehmen"
        /// statt einer eingefrorenen Kopie des Anlegetags.
        /// </summary>
        [Fact]
        public void Der_Kopierlauf_laesst_die_PV_Zeile_aus()
        {
            Assert.True(ProjektDuplizierenCtrl.IstAusnahmeTabelle(
                            SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK),
                        "Tab_ProjektPhotovoltaik steht nicht in der Ausnahmeliste des Kopierlaufs.");
        }

        // =================================================================
        // 4 — Die Herkunft im Nachweis
        // =================================================================

        /// <summary>
        /// Die Herkunft reist im Nachweisumschlag mit (Fassung 3) — ohne neue Spalte in
        /// <c>Tab_Ergebnis</c>. Ein Umschlag ÄLTERER Fassung liest sich als „eigene
        /// Werte": genau die Aussage, die ein vor Schemaschritt 93 gebuchter Stand
        /// trägt.
        /// </summary>
        [Fact]
        public void Die_Herkunft_reist_im_Nachweisumschlag()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                PvVerguetungUebernommen = true,
                PvVerguetungQuelle = "Musterprojekt Gewerbepark"
            };

            string grund;
            string text = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.NotNull(text);

            ErgebnisNachweisUmschlag u = ErgebnisNachweisUmschlag.Lesen(text);
            Assert.NotNull(u);
            Assert.Equal(ErgebnisNachweisUmschlag.FASSUNG, u.Version);

            var zurueck = new WirtschaftlichkeitErgebnis();
            u.Uebernimm(zurueck);
            Assert.True(zurueck.PvVerguetungUebernommen);
            Assert.Equal("Musterprojekt Gewerbepark", zurueck.PvVerguetungQuelle);

            // Fassung 2 kennt das Feld nicht - "eigene Werte".
            ErgebnisNachweisUmschlag alt = ErgebnisNachweisUmschlag.Lesen(
                ErgebnisNachweisUmschlag.PRAEFIX + "{\"Version\":2}");
            Assert.NotNull(alt);
            var altZiel = new WirtschaftlichkeitErgebnis();
            alt.Uebernimm(altZiel);
            Assert.False(altZiel.PvVerguetungUebernommen);
            Assert.Equal("", altZiel.PvVerguetungQuelle);
        }

        /// <summary>
        /// Die Zeilendefinition trägt die Nachweiszeile „PV-Vergütung: Herkunft" — Word
        /// und Excel lesen dieselbe Definition. Sie steht in Block A, beim
        /// anzulegenden Wert, und nennt je Spalte „eigene Werte" oder „übernommen von
        /// ‹Stamm›".
        /// </summary>
        [Fact]
        public void Die_Zeilendefinition_nennt_die_Herkunft()
        {
            var eigen = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM,
                PvVerguetungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE,
                PvAnzulegenderWert = 6.04
            };
            var uebernommen = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = VARIANTE_A,
                PvVerguetungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE,
                PvAnzulegenderWert = 6.04,
                PvVerguetungUebernommen = true,
                PvVerguetungQuelle = "Musterprojekt Gewerbepark"
            };

            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(
                new List<WirtschaftlichkeitErgebnis> { eigen, uebernommen }, null);

            WirtZeile herkunft = zeilen.Find(z => z.Schluessel == "PV_HERKUNFT");
            Assert.NotNull(herkunft);
            Assert.Equal(WirtZeile.BLOCK_A, herkunft.Block);

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_PV_HERK_EIGEN, herkunft.Text(eigen));
            Assert.Contains("Musterprojekt Gewerbepark", herkunft.Text(uebernommen));
        }

        // =================================================================
        // Werkzeug
        // =================================================================

        private static void Schreibe(ProjektPhotovoltaikCtrl ctrl, int idProjekt,
                                     bool aktiv, double aw, bool uebernahme)
        {
            ProjektPhotovoltaikModel m = ctrl.LiesOderVorbelegt(idProjekt);
            m.ID_Projekt = idProjekt;
            m.Aktiv = aktiv;
            m.AwOverride = aw;
            m.Inbetriebnahme = new DateTime(2026, 1, 1);
            m.UebernahmeStamm = uebernahme;
            Assert.True(ctrl.Speichern(m));
        }

        private static void Loesche(int idProjekt)
        {
            DataRepository.ExecuteNonQuery(
                "DELETE FROM " + SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK +
                " WHERE ID_Projekt = " + idProjekt.ToString(CultureInfo.InvariantCulture));
        }

        private static int Zaehle(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }
    }
}
