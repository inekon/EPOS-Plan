using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ANWENDERENTSCHEID 19.09.2026 — „Weg B rechnet mit dem Trägerpreis der
    /// Anlage".</b>
    ///
    /// <para>„% des Endenergiebedarfs" (Weg B) bewertet die Endenergiemenge einer
    /// Anlage mit STROM, denn Hilfsstrom ist Strom. WELCHER Strompreis das ist, hängt
    /// an der Anlage: Bezieht sie selbst Strom und trägt sie einen eigenen Stromträger
    /// (Wärmepumpe, Heizstab, Elektrokessel), gilt der Arbeitspreis DIESES Trägers —
    /// derselbe, mit dem Weg A („% der Endenergiekosten") dieselbe Menge bewertet.
    /// Eine Anlage mit Brennstoffträger (BHKW, Heizkessel auf Gas oder Öl) bewertet
    /// ihren Hilfsstrom weiter mit dem Stromträger des PROJEKTS.</para>
    ///
    /// <para><b>Warum das Projekt 1023.</b> Es führt ZWEI Wärmepumpen mit je einer
    /// Modulzeile im gespeicherten Lauf. Nur so lassen sich eigener und
    /// Projekt-Stromträger überhaupt auseinanderhalten: Die Wärmepumpe steht in der
    /// Rangfolge des Projektträgers ganz vorn, die ERSTE bestimmt ihn also — und die
    /// zweite kann einen anderen tragen. Der zweite Träger und sein Preis entstehen im
    /// Fall selbst; die Testdatenbank bleibt unangetastet (eigene Arbeitskopie je
    /// Fall).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class HilfsstromStrompreisJeAnlageTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Zwei Wärmepumpen, beide im gespeicherten Lauf; Stromträger
        /// „Elektrische Energie" mit Arbeitspreis.</summary>
        private const int PROJEKT_ZWEI_WP = 1023;

        /// <summary>Die ERSTE Wärmepumpe (kleinste Id) — sie bestimmt den
        /// Stromträger des Projekts.</summary>
        private const int ANLAGE_WP_1 = 11203;

        /// <summary>Die ZWEITE Wärmepumpe — sie bekommt den abweichenden Träger.</summary>
        private const int ANLAGE_WP_2 = 11204;

        /// <summary>Die einzige Betriebszeile des Projekts an der Wärmepumpe
        /// (Komponente 1, Kategorie 2, Anlage <see cref="ANLAGE_WP_1"/>).</summary>
        private const int Z_WP = 101600075;

        /// <summary>„Elektrische Energie" — der Träger, den das Projekt führt.</summary>
        private const int TRAEGER_STROM_PROJEKT = 60;

        /// <summary>„Elektrische Energie 2" — der zweite Stromträger des Katalogs.</summary>
        private const int TRAEGER_STROM_ZWEIT = 58;

        /// <summary>Der Arbeitspreis, den der Fall dem zweiten Träger gibt —
        /// deutlich unter dem des Projektträgers (0,46746 €/kWh).</summary>
        private const double PREIS_ZWEIT = 0.11;

        /// <summary>Stromverbrauch + Heizstab der zweiten Wärmepumpe im
        /// gespeicherten Lauf: (23,18 + 36,09) MWh.</summary>
        private const double BEDARF_WP_2_KWH = (23.18 + 36.09) * 1000.0;

        // =====================================================================
        // 1 — Weg B an einer Stromanlage: der Preis IHRES Trägers
        // =====================================================================

        /// <summary>
        /// Die zweite Wärmepumpe trägt einen anderen Stromträger als das Projekt.
        /// Weg B bewertet ihre Endenergie mit dem Arbeitspreis DIESES Trägers — nicht
        /// mit dem des Projektträgers.
        ///
        /// <para><b>Die Gegenprobe steht daneben:</b> Weg A derselben Anlage liefert
        /// DIESELBE Zahl. Zwei Wege, ein Preis — sonst stünden an einer Zeile zwei
        /// Bewertungen desselben Stroms.</para>
        /// </summary>
        [Fact]
        public void Weg_B_bewertet_die_Waermepumpe_mit_dem_Preis_ihres_eigenen_Traegers()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweitenTraegerSetzen(PREIS_ZWEIT);

            // Der Projektträger ist der der ERSTEN Wärmepumpe — die zweite steht daneben.
            Assert.Equal(TRAEGER_STROM_PROJEKT,
                         ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(PROJEKT_ZWEI_WP));

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(PROJEKT_ZWEI_WP);
            Assert.NotNull(a);
            double? projektpreis = a.StrompreisJeKwh;
            Assert.True(projektpreis.HasValue && projektpreis.Value > PREIS_ZWEIT,
                        "Der Projektträger führt keinen höheren Arbeitspreis.");

            EndenergieAufloeser.Groesse g =
                a.FuerPosition(EndenergieAufloeser.KOMPONENTE_WAERMEPUMPE, ANLAGE_WP_2);
            Assert.NotNull(g);
            Assert.Equal(BEDARF_WP_2_KWH, g.BedarfKwh, 6);
            Assert.True(g.EigenerStromtraeger, "Der eigene Träger der Anlage wurde nicht erkannt.");
            Assert.Equal(PREIS_ZWEIT, g.BewertungspreisJeKwh.Value, 9);
            Assert.Equal(g.BedarfKwh * PREIS_ZWEIT, g.KostenEuro.Value, 6);

            // Und dasselbe am ganzen Weg der Zeile: Die Betriebszeile zeigt auf die
            // zweite Wärmepumpe, also gilt deren Preis.
            ZeileAufAnlage(Z_WP, ANLAGE_WP_2);
            double wegB = Basis(Z_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF);
            double wegA = Basis(Z_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN);

            Assert.Equal(BEDARF_WP_2_KWH * PREIS_ZWEIT, wegB, 6);
            Assert.Equal(wegA, wegB, 6);
            Assert.NotEqual(BEDARF_WP_2_KWH * projektpreis.Value, wegB, 6);
        }

        /// <summary>
        /// Die ERSTE Wärmepumpe trägt den Träger, der zugleich der des Projekts ist —
        /// dann ist die anlagenscharfe Auflösung ergebnisneutral. Der Fall pinnt, dass
        /// der Entscheid nur dort greift, wo die Anlage wirklich einen ANDEREN Träger
        /// führt.
        /// </summary>
        [Fact]
        public void Derselbe_Traeger_an_der_Anlage_aendert_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweitenTraegerSetzen(PREIS_ZWEIT);

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(PROJEKT_ZWEI_WP);
            Assert.NotNull(a);

            EndenergieAufloeser.Groesse g =
                a.FuerPosition(EndenergieAufloeser.KOMPONENTE_WAERMEPUMPE, ANLAGE_WP_1);
            Assert.NotNull(g);
            Assert.True(g.EigenerStromtraeger);
            Assert.Equal(a.StrompreisJeKwh.Value, g.BewertungspreisJeKwh.Value, 9);
        }

        // =====================================================================
        // 2 — Die Brennstoffanlage bleibt beim Stromträger des Projekts
        // =====================================================================

        /// <summary>
        /// Der Gaskessel der BHKW-Kaskade (Projekt 1030) trägt einen BRENNSTOFF. Sein
        /// Hilfsstrom kommt aus dem Netzbezug des Projekts, also bewertet Weg B mit
        /// dem Stromträger des Projekts — unverändert.
        /// </summary>
        [Fact]
        public void Eine_Brennstoffanlage_bewertet_mit_dem_Strompreis_des_Projekts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EndenergieAufloeser a = EndenergieAufloeser.FuerProjekt(1030);
            Assert.NotNull(a);

            EndenergieAufloeser.Groesse g =
                a.FuerPosition(BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL, 11334);
            Assert.NotNull(g);
            Assert.False(g.EigenerStromtraeger,
                         "Ein Brennstoffkessel darf keinen eigenen Stromträger tragen.");
            Assert.Equal(a.StrompreisJeKwh.Value, g.BewertungspreisJeKwh.Value, 9);

            // Weg A derselben Anlage bewertet mit dem BRENNSTOFFpreis — zwei Preise,
            // zwei Zahlen. Das ist der Unterschied zur Stromanlage oben.
            Assert.NotEqual(g.BedarfKwh * g.BewertungspreisJeKwh.Value, g.KostenEuro.Value, 6);
        }

        // =====================================================================
        // 3 — Die ABHILFE nennt den Träger, an dem der Preis fehlt
        // =====================================================================

        /// <summary>
        /// Fehlt dem EIGENEN Träger der Anlage der Arbeitspreis, heißt der Steuerwert
        /// <c>PREIS</c> und der Satz nennt den Energieträger — den der Anwender an der
        /// Anlage gewählt hat.
        /// </summary>
        [Fact]
        public void Ohne_Preis_des_eigenen_Traegers_nennt_die_Abhilfe_den_Energietraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZweitenTraegerSetzen(null);   // der zweite Träger ohne Arbeitspreis
            ZeileAufAnlage(Z_WP, ANLAGE_WP_2);

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS,
                         Grund(Z_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS,
                         Grund(Z_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));

            string text = KostenHerleitung.GrundText(WirtschaftlichkeitCtrl.BASISGRUND_PREIS);
            Assert.Contains("Energieträger", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Stromträger", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Trägt die Anlage KEINEN eigenen Stromträger, bewertet Weg B mit dem des
        /// Projekts — und fehlt DESSEN Arbeitspreis, nennt die Abhilfe genau ihn. Weg A
        /// bleibt daneben beim allgemeinen Satz: Er vermisst den Träger der Anlage.
        /// </summary>
        [Fact]
        public void Ohne_Preis_des_Projekttraegers_nennt_die_Abhilfe_den_Stromtraeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PreisSetzen(TRAEGER_STROM_PROJEKT, null);   // der Projektträger ohne Arbeitspreis

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS,
                         Grund(Z_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS,
                         Grund(Z_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));

            string strom = KostenHerleitung.GrundText(WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS);
            Assert.Contains("Stromträger", strom, StringComparison.Ordinal);
            Assert.NotEqual(KostenHerleitung.GrundText(WirtschaftlichkeitCtrl.BASISGRUND_PREIS), strom);
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>
        /// Richtet die Lage ein: Die erste Wärmepumpe trägt den Stromträger des
        /// Projekts, die zweite den Träger „Elektrische Energie 2" mit
        /// <paramref name="preis"/> (<c>null</c> = kein Arbeitspreis).
        /// </summary>
        private static void ZweitenTraegerSetzen(double? preis)
        {
            TraegerZuordnen(TRAEGER_STROM_ZWEIT);
            PreisSetzen(TRAEGER_STROM_ZWEIT, preis);
            AnlagenTraeger(ANLAGE_WP_1, TRAEGER_STROM_PROJEKT);
            AnlagenTraeger(ANLAGE_WP_2, TRAEGER_STROM_ZWEIT);
        }

        /// <summary>Ordnet dem Projekt einen Träger zu, sofern er noch fehlt.</summary>
        private static void TraegerZuordnen(int carrierId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT_ZWEI_WP),
                new DbParam("@c", carrierId));
            if (o != null && o != DBNull.Value && Convert.ToInt32(o) > 0) return;

            object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM energy_project_settings");
            int id = ((max == null || max == DBNull.Value) ? 0 : Convert.ToInt32(max)) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID, ID_Projekt, [ID_Energieträger]) " +
                "VALUES (?, ?, ?)",
                new DbParam("@id", id),
                new DbParam("@p", PROJEKT_ZWEI_WP),
                new DbParam("@c", carrierId));
        }

        /// <summary>
        /// Setzt den Arbeitspreis eines Trägers im Projekt; <c>null</c> = keiner.
        ///
        /// <para>Die Vorrangkette des Preises ist Projektwert → PREISSTAND → Katalog
        /// (<c>KostenEmissionRechner.LadeTraeger</c>). „Kein Preis" heißt deshalb: die
        /// Projektspalte leer UND kein Preisstand — sonst rechnet die Kette mit dem
        /// Stand weiter, und der Fall prüfte nichts.</para>
        /// </summary>
        private static void PreisSetzen(int carrierId, double? preis)
        {
            DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_work = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                preis.HasValue ? new DbParam("@w", preis.Value) : new DbParam("@w", DBNull.Value),
                new DbParam("@p", PROJEKT_ZWEI_WP),
                new DbParam("@c", carrierId));

            if (preis.HasValue) return;
            DataRepository.ExecuteSQL(
                "DELETE FROM energy_price WHERE ID_Projekt = ? AND carrier_id = ?",
                new DbParam("@p", PROJEKT_ZWEI_WP),
                new DbParam("@c", carrierId));
        }

        /// <summary>Hängt den Trägerverweis an eine Anlagenzeile.</summary>
        private static void AnlagenTraeger(int idAnlage, int carrierId)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET [" + SchemaKatalog.SPALTE_ID_CARRIER + "] = ? " +
                "WHERE ID = ?",
                new DbParam("@c", carrierId),
                new DbParam("@id", idAnlage));
        }

        /// <summary>Hängt eine Positionszeile an eine andere Anlage.</summary>
        private static void ZeileAufAnlage(int positionsId, int idAnlage)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] = ? " +
                "WHERE ID = ?",
                new DbParam("@a", idAnlage),
                new DbParam("@id", positionsId));
        }

        /// <summary>Die frische Bezugsgröße einer Zeile zu einer Bemessungsart.</summary>
        private static double Basis(int positionsId, string bemessung)
        {
            string grund;
            double? b = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bemessung, out grund);
            Assert.True(b.HasValue, "Keine Bezugsgröße zu " + bemessung + ": " + grund);
            return b.Value;
        }

        /// <summary>Der Grund, wenn es keine Bezugsgröße gibt.</summary>
        private static string Grund(int positionsId, string bemessung)
        {
            string grund;
            double? b = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bemessung, out grund);
            Assert.False(b.HasValue, "Unerwartete Bezugsgröße zu " + bemessung + ": " + b);
            return grund;
        }
    }
}
