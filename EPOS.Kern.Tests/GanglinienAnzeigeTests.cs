using System;
using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die beiden Anzeigehelfer des Lastgangimports, seit iU9-W12.0c und W12.0e im
    /// Kern: <see cref="GanglinienProtokollText"/> (Schluessel → Text) und
    /// <see cref="GanglinienOptionenModell"/> (Listenplatz ↔ Steuerwert).
    ///
    /// <para>Beide sind der Ort, an dem die Drei-Schichten-Regel des Hauses
    /// haengt: Die Engine liefert SCHLUESSEL, der Dialog fuehrt WERTE, und der Text
    /// steht nur daneben. Wo ein Text geprueft wird, ist die Oberflaechensprache
    /// festgelegt (Regel seit iU9-W8).</para>
    /// </summary>
    public class GanglinienAnzeigeTests
    {
        // ================================================ GanglinienProtokollText

        [Fact]
        public void Text_setzt_die_Werte_in_die_Vorlage()
        {
            using var _ = new DeutscheOberflaeche();

            // IMPORT_PROT_SCHALTJAHR: "{0} Werte ... {1} ... {2}"
            string text = GanglinienProtokollText.Text(
                new PruefMeldung(PruefStufe.Info, "IMPORT_PROT_SCHALTJAHR", "8784", "8760", "24"));

            Assert.Contains("8784", text);
            Assert.Contains("8760", text);
            Assert.DoesNotContain("{0}", text);
        }

        /// <summary>
        /// Fehlt der Schluessel im Katalog, kommt die sprachneutrale Kurzfassung —
        /// besser als ein leeres Feld.
        /// </summary>
        [Fact]
        public void Text_faellt_bei_unbekanntem_Schluessel_auf_die_Kurzfassung_zurueck()
        {
            string text = GanglinienProtokollText.Text(
                new PruefMeldung(PruefStufe.Warnung, "GIBT_ES_NICHT_IM_KATALOG", "a", "b"));

            Assert.Equal("GIBT_ES_NICHT_IM_KATALOG: a; b", text);
            Assert.Equal("", GanglinienProtokollText.Text(null));
        }

        [Fact]
        public void StufeText_nennt_die_drei_Stufen()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal("Fehler", GanglinienProtokollText.StufeText(PruefStufe.Fehler));
            Assert.Equal("Warnung", GanglinienProtokollText.StufeText(PruefStufe.Warnung));
            Assert.Equal("Info", GanglinienProtokollText.StufeText(PruefStufe.Info));
        }

        /// <summary>
        /// Die Farben des Vorlaeufers (<c>176,0,32</c> und <c>160,96,0</c>) sind
        /// CSS-Klassen geworden — <c>System.Drawing</c> ist im Kern verboten.
        /// </summary>
        [Fact]
        public void StufeKlasse_tritt_an_die_Stelle_der_drei_Farben()
        {
            Assert.Equal("epos-stufe--fehler", GanglinienProtokollText.StufeKlasse(PruefStufe.Fehler));
            Assert.Equal("epos-stufe--warnung", GanglinienProtokollText.StufeKlasse(PruefStufe.Warnung));
            Assert.Equal("epos-stufe--info", GanglinienProtokollText.StufeKlasse(PruefStufe.Info));
        }

        // ================================================ GanglinienOptionenModell

        /// <summary>
        /// Zu jedem Steuerwert genau eine Beschriftung, in derselben Reihenfolge —
        /// sonst waehlt der Dialog etwas anderes, als er beschriftet.
        /// </summary>
        [Fact]
        public void Zu_jeder_Werteliste_gehoert_eine_gleich_lange_Textliste()
        {
            Assert.Equal(GanglinienOptionenModell.Trennzeichenwerte.Length,
                         GanglinienOptionenModell.TrennzeichenTexte().Count);
            Assert.Equal(GanglinienOptionenModell.Dezimalwerte.Length,
                         GanglinienOptionenModell.DezimalTexte().Count);
            Assert.Equal(GanglinienOptionenModell.Einheitswerte.Length,
                         GanglinienOptionenModell.EinheitTexte().Count);
            Assert.Equal(GanglinienOptionenModell.Rasterwerte.Length,
                         GanglinienOptionenModell.RasterTexte().Count);
            Assert.Equal(GanglinienOptionenModell.Konventionswerte.Length,
                         GanglinienOptionenModell.KonventionTexte().Count);
        }

        [Fact]
        public void Die_Steuerwerte_stehen_in_der_Reihenfolge_des_Vorlaeufers()
        {
            Assert.Equal(new[] { ';', ',', '\t', '|', '\0' }, GanglinienOptionenModell.Trennzeichenwerte);
            Assert.Equal(new[] { ',', '.' }, GanglinienOptionenModell.Dezimalwerte);
            Assert.Equal(new[] { GanglinienEinheit.Kilowatt, GanglinienEinheit.KilowattstundeJeIntervall },
                         GanglinienOptionenModell.Einheitswerte);
            Assert.Equal(new[] { GanglinienRaster.Unbekannt, GanglinienRaster.Stunde,
                                 GanglinienRaster.Viertelstunde, GanglinienRaster.Minute },
                         GanglinienOptionenModell.Rasterwerte);
            Assert.Equal(new[] { IntervallKonvention.Automatisch, IntervallKonvention.Anfang,
                                 IntervallKonvention.Ende },
                         GanglinienOptionenModell.Konventionswerte);
        }

        /// <summary>
        /// Die Rueckfallplaetze sind die des Vorlaeufers (:203-213): Trennzeichen 4
        /// (<c>'\0'</c>, einspaltig), Dezimaltrenner 1 (Punkt).
        /// </summary>
        [Fact]
        public void Index_und_Wert_bilden_hin_und_zurueck_ab()
        {
            Assert.Equal(0, GanglinienOptionenModell.Index(GanglinienOptionenModell.Trennzeichenwerte, ';',
                                                          GanglinienOptionenModell.RueckfallTrennzeichen));
            Assert.Equal(2, GanglinienOptionenModell.Index(GanglinienOptionenModell.Trennzeichenwerte, '\t',
                                                          GanglinienOptionenModell.RueckfallTrennzeichen));
            Assert.Equal(4, GanglinienOptionenModell.Index(GanglinienOptionenModell.Trennzeichenwerte, '#',
                                                          GanglinienOptionenModell.RueckfallTrennzeichen));

            Assert.Equal('\t', GanglinienOptionenModell.Wert(GanglinienOptionenModell.Trennzeichenwerte, 2, '\0'));
            Assert.Equal('\0', GanglinienOptionenModell.Wert(GanglinienOptionenModell.Trennzeichenwerte, 99, '\0'));
            Assert.Equal('.', GanglinienOptionenModell.Wert(GanglinienOptionenModell.Dezimalwerte, -1, '.'));
        }

        [Fact]
        public void Grenzen_haelt_den_Index_in_der_Liste()
        {
            Assert.Equal(-1, GanglinienOptionenModell.Grenzen(3, 0));
            Assert.Equal(0, GanglinienOptionenModell.Grenzen(-5, 4));
            Assert.Equal(2, GanglinienOptionenModell.Grenzen(2, 4));
            Assert.Equal(3, GanglinienOptionenModell.Grenzen(9, 4));
        }

        /// <summary>
        /// Die Zeitspaltenliste beginnt mit „(keine)"; ihr Platz ist deshalb um eins
        /// gegenueber <c>GanglinienImportOptionen.ZeitSpalte</c> verschoben.
        /// </summary>
        [Fact]
        public void Die_Zeitspaltenliste_traegt_keine_an_erster_Stelle()
        {
            using var _ = new DeutscheOberflaeche();

            List<string> zeit = GanglinienOptionenModell.ZeitspaltenTexte(3);
            List<string> wert = GanglinienOptionenModell.SpaltenTexte(3);

            Assert.Equal(4, zeit.Count);
            Assert.Equal("(keine)", zeit[0]);
            Assert.Equal(3, wert.Count);
            Assert.Equal("Spalte 1", wert[0]);
            Assert.Equal("Spalte 3", wert[2]);
            Assert.Equal(wert[0], zeit[1]);

            // Null Spalten gibt es nicht - mindestens eine.
            Assert.Single(GanglinienOptionenModell.SpaltenTexte(0));
        }

        /// <summary>
        /// <b>Befund W12-B15, woertlich behalten.</b> Die Auswahlliste der
        /// Stammdatenverwaltung hat ZWEI Eintraege, die Abbildung kennt DREI. Platz 2
        /// ist damit unerreichbar — er bleibt trotzdem stehen, weil er die Aussage des
        /// Vorlaeufers ist.
        /// </summary>
        [Fact]
        public void RasterAusIndex_kennt_drei_Plaetze_die_Liste_aber_nur_zwei()
        {
            Assert.Equal(GanglinienRaster.Stunde, GanglinienOptionenModell.RasterAusIndex(0));
            Assert.Equal(GanglinienRaster.Viertelstunde, GanglinienOptionenModell.RasterAusIndex(1));
            Assert.Equal(GanglinienRaster.Minute, GanglinienOptionenModell.RasterAusIndex(2));
            Assert.Equal(GanglinienRaster.Unbekannt, GanglinienOptionenModell.RasterAusIndex(-1));
            Assert.Equal(GanglinienRaster.Unbekannt, GanglinienOptionenModell.RasterAusIndex(3));

            Assert.Equal(2, GanglinienOptionenModell.AdminRasterTexte().Count);
        }

        [Fact]
        public void Die_Rasterliste_der_Verwaltung_zeigt_Stunden_und_Viertelstunden()
        {
            using var _ = new DeutscheOberflaeche();

            List<string> texte = GanglinienOptionenModell.AdminRasterTexte();
            Assert.Equal("Stundenwerte", texte[0]);
            Assert.Equal("Viertelstundenwerte", texte[1]);
        }

        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }
    }
}
