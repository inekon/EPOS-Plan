using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Katalogwachen auf dem Dialogweg</b> (Umsetzungskonzept Zapfprofilgenerator 3.2,
    /// Kapitel 6 (b)/(c), 5.4; Stufe Z4, Gruppe 3): Was <see cref="TwwKatalogWacheTests"/> für die
    /// Testdatenbank und <see cref="TwwKatalogSperreTests"/> für die allgemeine Katalogpflege
    /// festhalten, gilt auch für den Katalogdialog „Brauchwasser-Nutzungsarten" — geprüft über
    /// seine Hülle (<see cref="ZapfprofilHuelle.KatalogGaben"/>), also über genau die Wege, die der
    /// Anwender geht:
    /// <list type="bullet">
    /// <item>Der Dialog legt NIE eine Zeile der Auslieferung an (Status <c>AUSLIEFERUNG</c> oder
    /// <c>ReadOnly = 1</c>) — weder „Neu…", „Speichern unter…" noch der Katalogimport; importierte
    /// Zeilen tragen Status und Herkunftsart <c>IMPORT</c> (<c>FREI</c>/<c>FIKTIV</c> bleiben), so
    /// dass die Auslieferungsvorlage sie wieder entfernt.</item>
    /// <item>Der Dialog ändert keine vorhandene Zeile, die gesperrt ist (Auslieferung, benutzt):
    /// Ändern, Löschen, Kategorien und Import lassen sie Byte für Byte stehen.</item>
    /// </list>
    /// Mit der Arbeitskopie der Testdatenbank bzw. einer leeren Tww-Datenbank; Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwKatalogDialogwegWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static string Paket()
            => Path.Combine(ZapfZufallTests.Probenordner(), "Katalogpaket", TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv");

        /// <summary>Alle Zeilen der Tww-Kataloge als Text je (Tabelle, Id) — der Abdruck des Bestands.</summary>
        private static Dictionary<string, string> Abdruck()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string t in new[]
                     {
                         TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, TwwSchema.TAB_TWW_TAGESGANG_STAMM,
                         TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.TAB_TWW_PARAMETER_STAMM
                     })
            {
                if (!DataRepository.TabelleVorhanden(t)) continue;
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + t);
                foreach (DataRow r in dt.Rows)
                    d[t + "#" + Convert.ToString(r["ID"], CultureInfo.InvariantCulture)] =
                        string.Join("|", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName + "=" + Convert.ToString(r[c], CultureInfo.InvariantCulture)));
            }
            return d;
        }

        [Fact]
        public void Der_Dialogweg_legt_nie_eine_Auslieferungszeile_an_und_laesst_den_Bestand_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Dictionary<string, string> vorher = Abdruck();

            // Katalogimport, „Neu…" und „Speichern unter…" über die Hülle.
            TwwImportberichtDaten import = ZapfprofilHuelle.KatalogImportieren(Paket());
            Assert.False(import.Abgebrochen, import.Abbruch);
            Assert.Equal(2, import.NeueIds.Count);

            int erste = ZapfprofilHuelle.KatalogZeilen()[0].Id;
            TwwNutzungsartEntwurfDaten neu = ZapfprofilHuelle.EditorLaden(erste, TwwEditorModus.Neu).Entwurf;
            neu.Bezeichner = "Wache Neu (erfunden)";
            Assert.True(ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Neu, neu).Ok);
            TwwNutzungsartEntwurfDaten unter = ZapfprofilHuelle.EditorLaden(erste, TwwEditorModus.SpeichernUnter).Entwurf;
            Assert.True(ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.SpeichernUnter, unter).Ok);

            Dictionary<string, string> nachher = Abdruck();

            // Der Bestand steht unverändert.
            foreach (KeyValuePair<string, string> z in vorher)
            {
                Assert.True(nachher.ContainsKey(z.Key), "fehlt: " + z.Key);
                Assert.Equal(z.Value, nachher[z.Key]);
            }

            // Keine neue Zeile der Auslieferung; importierte Wertgruppen tragen IMPORT, FREI oder FIKTIV.
            DataTable n = DataRepository.GetDataTable("SELECT * FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM);
            Assert.DoesNotContain(n.Rows.Cast<DataRow>(), r => (string)r["Status"] == TwwSchema.STATUS_AUSLIEFERUNG);
            foreach (string t in new[] { TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM })
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + t + " WHERE Status = 'AUSLIEFERUNG' OR ReadOnly <> 0"), CultureInfo.InvariantCulture));
            foreach (int id in import.NeueIds)
            {
                DataRow r = DataRepository.GetDataTable("SELECT * FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE ID = ?",
                                                        new DbParam("@id", id)).Rows[0];
                Assert.Equal(TwwSchema.STATUS_IMPORT, r["Status"]);
                foreach (string g in new[] { "Bedarf_", "Jahresgang_", "Wochengang_" })
                    Assert.Contains((string)r[g + "Herkunftsart"], new[] { TwwSchema.HERKUNFT_IMPORT, TwwSchema.HERKUNFT_FREI, TwwSchema.HERKUNFT_FIKTIV });
            }
        }

        [Fact]
        public void Die_Katalogsperre_gilt_auch_auf_dem_Dialogweg()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Probesatz (erfunden)", "PROBE-1");
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Probenutzung A (erfunden)", "PROBE-1", satz,
                                                                status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen("Probenutzung B (erfunden)", "PROBE-1", satz);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone 1", 5.0);
            TwwTestdatenbank.KategorieAnlegen(benutzt, "Kurz", 1, 2.0, 1, 1.0, 0.5);
            Dictionary<string, string> vorher = Abdruck();

            // Ändern: nur als „Speichern unter"; der Schreibweg lehnt das Ändern an Ort und Stelle ab.
            foreach (int id in new[] { geliefert, benutzt })
            {
                TwwNutzungsartEditorDaten ed = ZapfprofilHuelle.EditorLaden(id, TwwEditorModus.Aendern);
                Assert.Equal(TwwEditorModus.SpeichernUnter, ed.Modus);
                Assert.NotEqual("", ed.Hinweis);
                ed.Entwurf.Katalogversion = "PROBE-1";
                Assert.False(ZapfprofilHuelle.EntwurfSpeichern(TwwEditorModus.Aendern, ed.Entwurf).Ok);
                Assert.False(ZapfprofilHuelle.KatalogLoeschen(id).Ok);
            }

            // Die Kategorien einer benutzten Nutzungsart: neue Version, die alte bleibt.
            ZapfprofilKategorienErgebnis k = ZapfprofilHuelle.KategorienSpeichern(benutzt,
                new[] { new ZapfprofilKategorieDaten { Name = "Lang", VolumenstromLJeMin = 8, StreuungLJeMin = 1, DauerMin = 5, Anteil = 1 } },
                "PROBE-1-E1");
            Assert.True(k.Ok);
            Assert.True(k.NeueZeile);

            // Der Import desselben Namens: Die Auslieferung bleibt, die benutzte Zeile bekommt eine „(Import n)".
            TwwImportberichtDaten import = ZapfprofilHuelle.KatalogImportieren(Paket());
            Assert.False(import.Abgebrochen, import.Abbruch);

            Dictionary<string, string> nachher = Abdruck();
            foreach (KeyValuePair<string, string> z in vorher)
                Assert.Equal(z.Value, nachher[z.Key]);
            Assert.True(TwwNutzungsartCtrl.IstReadOnly(geliefert));
            Assert.True(TwwNutzungsartCtrl.IstBenutzt(benutzt));
        }
    }
}
