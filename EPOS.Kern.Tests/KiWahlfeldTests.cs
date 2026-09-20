using System;
using System.Collections.Generic;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// WAHLFELDER an der Maskenbrücke (KI-F1b, Anwenderentscheid KI‑D‑Q6): Ein
    /// Auswahlfeld wird über den ANGEZEIGTEN TEXT gesetzt, gespeichert wird der
    /// Schlüssel im Typ der Zieleigenschaft.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Anlass.</b> Die freigegebenen Masken boten nur Zahlen, Texte und Schalter;
    /// Energieträger, Gerät, Modul, Bodentyp und Preisreihe fehlten, weil der Katalog
    /// keine Auswahl kannte. Der Anwender hat entschieden: „Es sollen alle Eingabefelder
    /// der Dialoge gesetzt werden können."
    /// </para>
    /// <para>
    /// <b>Die Einträge kommen aus der MASKE, nicht aus dem Katalog</b> — deshalb steht
    /// hier ein Lieferant, der bei jedem Zugriff gerufen wird, und kein fester Satz.
    /// </para>
    /// </remarks>
    [Collection("Testdatenbank")]
    public class KiWahlfeldTests : IDisposable
    {
        private const string MASKE = "Form_Wahlpruefstand";

        private readonly Kulturvorrichtung _kultur = new();

        public KiWahlfeldTests() => KiMaskenbruecke.Leeren();

        public void Dispose()
        {
            KiMaskenbruecke.Leeren();
            _kultur.Dispose();
        }

        // ================================================================== Hilfen

        /// <summary>Die Zieleigenschaft: eine Id, wie sie eine Anlagenzeile trägt.</summary>
        private sealed class Stand
        {
            internal int CarrierId;
            internal string Betriebsart = "";
        }

        private static IReadOnlyList<KiWahleintrag> Traeger() => new[]
        {
            new KiWahleintrag("3", "Erdgas H"),
            new KiWahleintrag("7", "Heizöl EL"),
            new KiWahleintrag("11", "Holzpellets")
        };

        private static KiDialogFeld Wahlfeld(string name = "energietraeger",
                                             string pfad = "Stand.CarrierId")
            => new KiDialogFeld(name, pfad, "Energieträger:", KiParameterTyp.Wahl,
                                "Der Energieträger der Anlage.");

        private static KiFeldzugang Zugang(Stand stand, KiDialogFeld feld,
                                           Func<IReadOnlyList<KiWahleintrag>> eintraege)
            => new KiFeldzugang(feld,
                                () => stand.CarrierId,
                                wert => stand.CarrierId = Convert.ToInt32(wert),
                                typeof(int),
                                eintraege);

        private static string Setze(KiFeldzugang zugang, string text)
        {
            KiFeldumsetzung u = KiFeldwandler.Wandle(zugang, text);
            if (!u.Ok) return u.Grund;
            zugang.Setzen(u.Wert);
            return null;
        }

        // ================================================= Setzen über den Text

        [Fact]
        public void Der_angezeigte_Text_setzt_den_Schluessel()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(), Traeger);

            Assert.Null(Setze(z, "Erdgas H"));
            Assert.Equal(3, stand.CarrierId);
        }

        [Fact]
        public void Auch_der_Schluessel_selbst_wird_angenommen()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(), Traeger);

            Assert.Null(Setze(z, "7"));
            Assert.Equal(7, stand.CarrierId);
        }

        [Fact]
        public void Umlaut_und_Schreibweise_spielen_keine_Rolle()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(), Traeger);

            Assert.Null(Setze(z, "heizoel el"));
            Assert.Equal(7, stand.CarrierId);
        }

        [Fact]
        public void Ein_eindeutiger_Anfang_genuegt()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(), Traeger);

            Assert.Null(Setze(z, "Holz"));
            Assert.Equal(11, stand.CarrierId);
        }

        // ================================================= Die zwei Absagen

        [Fact]
        public void Ein_unbekannter_Eintrag_wird_benannt_abgelehnt_und_nennt_die_Auswahl()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(), Traeger);

            string grund = Setze(z, "Kernfusion");

            Assert.NotNull(grund);
            Assert.Contains("Erdgas H (3)", grund, StringComparison.Ordinal);
            Assert.Equal(0, stand.CarrierId);          // nichts gesetzt
        }

        [Fact]
        public void Ein_mehrdeutiger_Eintrag_wird_nicht_geraten()
        {
            var stand = new Stand();
            IReadOnlyList<KiWahleintrag> zwei = new[]
            {
                new KiWahleintrag("3", "Erdgas H"),
                new KiWahleintrag("4", "Erdgas L")
            };
            KiFeldzugang z = Zugang(stand, Wahlfeld(), () => zwei);

            string grund = Setze(z, "Erdgas");

            Assert.NotNull(grund);
            Assert.Contains("Erdgas H (3)", grund, StringComparison.Ordinal);
            Assert.Contains("Erdgas L (4)", grund, StringComparison.Ordinal);
            Assert.Equal(0, stand.CarrierId);
        }

        [Fact]
        public void Ohne_Auswahl_laesst_sich_ein_Wahlfeld_nicht_setzen()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(),
                                    () => Array.Empty<KiWahleintrag>());

            Assert.NotNull(Setze(z, "Erdgas H"));
            Assert.Equal(0, stand.CarrierId);
        }

        [Fact]
        public void Ein_werfender_Lieferant_kippt_die_Auskunft_nicht()
        {
            var stand = new Stand();
            KiFeldzugang z = Zugang(stand, Wahlfeld(),
                                    () => throw new InvalidOperationException("Liste weg"));

            Assert.Empty(z.Wahleintraege());
            Assert.NotNull(Setze(z, "Erdgas H"));
        }

        // ================================================= Der Steuerwert als Schlüssel

        /// <summary>
        /// <b>Ein Wahlfeld muss keine Id tragen.</b> Wo die Maske einen STEUERWERT führt
        /// (<c>DbWerte.WP_BETRIEBSART_*</c>), sind Schlüssel und Text derselbe Text —
        /// und gesetzt wird er als Zeichenkette.
        /// </summary>
        [Fact]
        public void Ein_Steuerwert_wird_als_Zeichenkette_gesetzt()
        {
            var stand = new Stand();
            var feld = new KiDialogFeld("betriebsart", "Stand.Betriebsart", "Betriebsart:",
                                        KiParameterTyp.Wahl, "Die Betriebsart der Anlage.");

            IReadOnlyList<KiWahleintrag> arten = new[]
            {
                new KiWahleintrag("alternativ"),
                new KiWahleintrag("parallel"),
                new KiWahleintrag("teilparallel")
            };

            var z = new KiFeldzugang(feld,
                                     () => stand.Betriebsart,
                                     wert => stand.Betriebsart = (string)wert,
                                     typeof(string),
                                     () => arten);

            Assert.Null(Setze(z, "Teilparallel"));
            Assert.Equal("teilparallel", stand.Betriebsart);
        }

        // ================================================= Lesen zeigt den Text

        [Fact]
        public void Gelesen_wird_der_TEXT_und_daneben_der_Schluessel()
        {
            var stand = new Stand { CarrierId = 7 };
            KiDialogFeld feld = Wahlfeld();
            KiFeldzugang z = Zugang(stand, feld, Traeger);

            KiMaskenbruecke.Anmelden(MASKE, new KiDialog(MASKE, "Prüfstand", new[] { feld }),
                                     new[] { z });

            IReadOnlyList<KiFeldwert> werte = KiMaskenbruecke.Lesen(MASKE);

            Assert.Single(werte);
            Assert.True(werte[0].IstWahl);
            Assert.Equal("Heizöl EL", werte[0].Text);
            Assert.Equal("7", werte[0].Schluessel);
            Assert.Equal(3, werte[0].Eintraege.Count);

            // Der Bestätigungsblock zeigt denselben Text (Feldsicherung 11.5).
            Assert.Equal("Heizöl EL", KiMaskenbruecke.Feldtext(z));
        }

        /// <summary>
        /// <b>Ein Schlüssel ohne Eintrag bleibt stehen.</b> Eine Anlage kann auf einen
        /// Katalogsatz zeigen, den die Liste gerade nicht führt; dann ist die rohe Id
        /// die Wahrheit über den Dialog — und die gehört gezeigt, nicht verschwiegen.
        /// </summary>
        [Fact]
        public void Ein_Schluessel_ausserhalb_der_Liste_wird_gezeigt_statt_verschwiegen()
        {
            var stand = new Stand { CarrierId = 99 };
            KiFeldzugang z = Zugang(stand, Wahlfeld(), Traeger);

            Assert.Equal("99", KiMaskenbruecke.Feldtext(z));
        }

        // ================================================= Tolerante Feldsuche

        [Fact]
        public void Die_Bruecke_findet_ein_Feld_unter_einem_tolerant_gelesenen_Namen()
        {
            var stand = new Stand();
            KiDialogFeld feld = Wahlfeld();
            KiFeldzugang z = Zugang(stand, feld, Traeger);

            KiMaskenbruecke.Anmelden(MASKE, new KiDialog(MASKE, "Prüfstand", new[] { feld }),
                                     new[] { z });

            Assert.NotNull(KiMaskenbruecke.Feldzugang(MASKE, "Energieträger"));
            Assert.NotNull(KiMaskenbruecke.Feldzugang(MASKE, "energietraeger"));
            Assert.NotNull(KiMaskenbruecke.Feldzugang(MASKE, "traeger"));
            Assert.Null(KiMaskenbruecke.Feldzugang(MASKE, "gibtesnicht"));
        }

        [Fact]
        public void Ein_mehrdeutiger_Feldname_nennt_die_Kandidaten_statt_zu_raten()
        {
            var stand = new Stand();
            KiDialogFeld vorlauf = new KiDialogFeld("vorlauf", "Stand.CarrierId", "Vorlauf:",
                                                    KiParameterTyp.Ganzzahl, "Die Vorlauftemperatur.");
            KiDialogFeld ruecklauf = new KiDialogFeld("ruecklauf", "Stand.Betriebsart", "Rücklauf:",
                                                      KiParameterTyp.Ganzzahl, "Die Rücklauftemperatur.");

            KiMaskenbruecke.Anmelden(
                MASKE, new KiDialog(MASKE, "Prüfstand", new[] { vorlauf, ruecklauf }),
                new[]
                {
                    new KiFeldzugang(vorlauf, () => stand.CarrierId),
                    new KiFeldzugang(ruecklauf, () => stand.CarrierId)
                });

            KiFeldtreffer treffer = KiMaskenbruecke.Feldsuche(MASKE, "lauf");

            Assert.False(treffer.Eindeutig);
            Assert.True(treffer.Mehrdeutig);
            Assert.Equal(2, treffer.Kandidaten.Count);

            // „vorlauftemperatur" bleibt dagegen eindeutig.
            Assert.Equal("vorlauf", KiMaskenbruecke.Feldzugang(MASKE, "vorlauftemperatur").Name);
        }
    }
}
