using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ANWENDERENTSCHEID 19.09.2026 — DER HINWEIS AUF DIE VORLAGE.</b>
    ///
    /// <para>Ändert sich die Bemessung einer Position der STANDARDVORLAGE, werden die
    /// längst angelegten Projektpositionen NICHT nachgeführt: Ein gepflegtes Projekt
    /// ändert seine gerechnete Wirtschaftlichkeit nicht still. Damit die Abweichung
    /// trotzdem sichtbar wird, liefert die Projektzeile die Bemessung der Vorlage mit,
    /// und der Kostendialog nennt sie unter der Herleitung — mit dem Handgriff
    /// „übernehmen".</para>
    ///
    /// <para><b>Der Verknüpfungsweg ist die BEZEICHNUNG</b> — genau der, über den die
    /// Übernahme die Projektzeile anlegt (<c>KostenVorlagenUebernahmeCtrl</c>:
    /// Bezeichnung → <c>StammIdSicher</c> → <c>Tab_ProjektWerte.StammID</c>; die
    /// Leseabfrage holt über denselben Verbund die Bezeichnung zurück).</para>
    ///
    /// <para>Die Lage steht in der Testdatenbank schon da: Am Heizkessel von Projekt
    /// 1030 trägt „Vollwartung / Wartung Kessel" die 2.000 €/a als festen Jahresbetrag,
    /// während die Standardvorlage „je kWh thermisch" führt (E30/1, #542: Datenpflege B3).
    /// Bis dahin hielt „Hilfsenergiekosten (Strom)" die Lage („% der Endenergiekosten"
    /// gegen „% des Endenergiebedarfs"); seit der Datenpflege B5 stimmt sie mit der
    /// Vorlage überein. Eine Position ohne Vorlagenposition legt der Fall auf seiner
    /// Arbeitskopie selbst an (die Altzeile „Heizkessel" ist mit B3 entfallen).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenVorlagenhinweisTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>BHKW-Kaskade mit Gaskessel.</summary>
        private const int PROJEKT = 1030;

        /// <summary><c>Tab_KostenKomponente.ID</c> des Heizkessels.</summary>
        private const int KOMPONENTE_KESSEL = 2;

        /// <summary>Die Zeile, deren Bemessung von der Vorlage abweicht (fester Jahresbetrag
        /// gegen „je kWh thermisch").</summary>
        private const string POS_WARTUNG = "Vollwartung / Wartung Kessel";

        /// <summary>Eine Zeile, deren Bemessung mit der Vorlage übereinstimmt.</summary>
        private const string POS_HILFSSTROM = "Hilfsenergiekosten (Strom)";

        /// <summary>Die Hauptposition — sie steht in KEINER Vorlagenposition.</summary>
        private const string POS_HAUPT = "Heizkessel";

        // =====================================================================
        // 1 — Die Auskunft der Projektzeile
        // =====================================================================

        /// <summary>
        /// Jede Projektzeile, die die Standardvorlage kennt, trägt deren Bemessung als
        /// Auskunft mit — auch die, die damit übereinstimmt. Ob daraus ein HINWEIS
        /// wird, entscheidet erst die Herleitung; ohne Vorlagenposition bleibt das
        /// Feld leer.
        /// </summary>
        [Fact]
        public void Die_Projektzeile_traegt_die_Bemessung_der_Standardvorlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Dictionary<string, KostenProjektPositionenCtrl.Zeile> zeilen = Zeilen();

            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                         zeilen[POS_HILFSSTROM].VorlagenBemessung);
            Assert.Equal("Standard", zeilen[POS_HILFSSTROM].VorlagenName);
            Assert.Equal("Standard", zeilen[POS_WARTUNG].VorlagenName);

            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
                         zeilen[POS_WARTUNG].VorlagenBemessung);

            Assert.Equal("", zeilen[POS_HAUPT].VorlagenBemessung);
            Assert.Equal("", zeilen[POS_HAUPT].VorlagenName);
        }

        // =====================================================================
        // 2 — Der Hinweis: nur bei Abweichung
        // =====================================================================

        /// <summary>
        /// Weicht die Bemessung ab, nennt die Herleitung die Vorlage im Klartext und
        /// gibt den Persistenzwert für die Übernahme mit. Die Zeilen ohne Abweichung
        /// und ohne Vorlagenposition bleiben stumm — sonst stünde unter jeder Zeile
        /// des Rasters ein Satz, der nichts sagt.
        /// </summary>
        [Fact]
        public void Nur_eine_abweichende_Bemessung_bekommt_den_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Dictionary<string, KostenProjektPositionenCtrl.Zeile> zeilen = Zeilen();

            KostenHerleitung.Angabe wartung = Bilde(zeilen[POS_WARTUNG]);
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, wartung.VorlagenBemessung);
            Assert.Contains("Standard", wartung.VorlagenZeile, StringComparison.Ordinal);
            Assert.Contains(BemessungKatalog.Anzeige(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
                                                     KOMPONENTE_KESSEL),
                            wartung.VorlagenZeile, StringComparison.Ordinal);

            Assert.Equal("", Bilde(zeilen[POS_HILFSSTROM]).VorlagenZeile);
            Assert.Equal("", Bilde(zeilen[POS_HAUPT]).VorlagenZeile);
        }

        /// <summary>
        /// Verglichen wird mit der Bemessung, die die Zeile GERADE trägt — nicht mit
        /// der zuletzt gespeicherten. Wer im Dialog auf den Vorlagenwert umstellt,
        /// sieht den Hinweis sofort verschwinden; das ist genau das, was „übernehmen"
        /// tut, bevor der Schreibweg läuft.
        /// </summary>
        [Fact]
        public void Auf_den_Vorlagenwert_umgestellt_verschwindet_der_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenProjektPositionenCtrl.Zeile z = Zeilen()[POS_WARTUNG];
            Assert.NotEqual("", Bilde(z).VorlagenZeile);

            z.Raster.Bemessung = z.VorlagenBemessung;
            Assert.Equal("", Bilde(z).VorlagenZeile);
        }

        /// <summary>
        /// Im STAMMKONTEXT gibt es keine Projektzeile — und damit auch keinen
        /// Vorlagenhinweis: Dort wird die Vorlage selbst gepflegt, sie kann nicht von
        /// sich abweichen.
        /// </summary>
        [Fact]
        public void Ohne_Projektzeile_gibt_es_keinen_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KostenProjektPositionenCtrl.Zeile z = Zeilen()[POS_HILFSSTROM];
            KostenHerleitung.Angabe a =
                KostenHerleitung.Bilde(z.Raster, KOMPONENTE_KESSEL, null, false, true);
            Assert.Equal("", a.VorlagenZeile);
            Assert.Equal("", a.VorlagenBemessung);
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>Die Betriebszeilen des Heizkessels, nach Bezeichnung.</summary>
        private static Dictionary<string, KostenProjektPositionenCtrl.Zeile> Zeilen()
        {
            // Die Hauptposition „Heizkessel" (Stamm 79) steht in keiner Vorlagenposition;
            // seit der Datenpflege B3 legt der Fall sie auf seiner Arbeitskopie selbst an.
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, " +
                "EingegebenerWert, Bestcase, Worstcase, Nutzungsdauer, Worstcase_Nutzungsdauer, " +
                "Bestcase_Nutzungsdauer, Einheit, Gruppe, Kostenart, Bemessung, IstErloes, ID_Anlage, " +
                "IstPflicht) VALUES (?, 79, ?, 2, 0, 0, 0, 0, 0, 0, '€', 'Wartung Kessel', " +
                "'BETRIEBSGEBUNDEN', 'BETRAG', 0, 11334, 0)",
                new DbParam("@p", DbParamTyp.Integer) { Wert = PROJEKT },
                new DbParam("@k", DbParamTyp.Integer) { Wert = KOMPONENTE_KESSEL });

            var karte = new Dictionary<string, KostenProjektPositionenCtrl.Zeile>(
                StringComparer.Ordinal);
            foreach (KostenProjektPositionenCtrl.Zeile z in
                     KostenProjektPositionenCtrl.Lies(PROJEKT, KOMPONENTE_KESSEL,
                                                      DbWerte.KOSTEN_KATEGORIE_BETRIEB))
                karte[z.Raster.Bezeichnung ?? ""] = z;
            Assert.True(karte.ContainsKey(POS_WARTUNG),
                        "Die Wartungszeile des Kessels fehlt in der Testdatenbank.");
            Assert.True(karte.ContainsKey(POS_HAUPT), "Die Hauptposition fehlt auf der Kopie.");
            return karte;
        }

        private static KostenHerleitung.Angabe Bilde(KostenProjektPositionenCtrl.Zeile z)
        {
            return KostenHerleitung.Bilde(z.Raster, KOMPONENTE_KESSEL, z, true, true);
        }
    }
}
