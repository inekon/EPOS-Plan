using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die MASKENBRÜCKE (Auftrag #200, Stufe S2 des Konzepts „Der Hilfe-Assistent im
    /// Dialog", 3.3).
    ///
    /// <para><b>Was hier bewiesen wird.</b> An-, Abmelden und Lesen; dass eine zweite
    /// Anmeldung derselben Maske die erste ablöst und ein spätes Abmelden die
    /// Nachfolgerin NICHT wegräumt; dass Abmelden idempotent ist; dass ein werfender
    /// Getter nur SEIN Feld leer lässt und nicht die ganze Auskunft kippt; dass der
    /// Feldblock wörtlich derselbe ist, den die Anfrage mitnimmt — und dass Anmelden und
    /// Lesen nebeneinander laufen dürfen.</para>
    ///
    /// <para><b>Die serielle Sammlung ist Absicht.</b> Die Brücke ist prozessweiter
    /// Zustand wie <c>Dienste.*</c>; <see cref="KiMaskenbruecke.Leeren"/> in einem Fall
    /// träfe sonst eine Anmeldung aus einer nebenher laufenden Klasse
    /// (Befund iU5‑O‑1).</para>
    ///
    /// <para>Die Klasse pinnt die Kultur über die gemeinsame <see cref="Kulturvorrichtung"/>
    /// (seit Auftrag #230; vorher nur <c>CurrentCulture</c>, s. u.): Der Feldblock formatiert
    /// Zahlen in der Anwenderkultur, und „12,5" gegen „12.5" wäre sonst eine Frage des
    /// Läufers — UND er schreibt Wahrheitswerte („Ja") und den Leer-Text („(leer)") aus
    /// <c>Resource.</c>, die <c>CurrentUICulture</c> folgen. Bis #230 pinnte die Klasse nur
    /// <c>CurrentCulture</c>: Auf dem Windows-Läufer (en-US) blieb <c>CurrentUICulture</c>
    /// unberührt, und zwei Fälle bekamen die englischen Satellitentexte
    /// („Yes" statt „Ja", „(empty)" statt „(leer)").</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KiMaskenbrueckeTests : IDisposable
    {
        private const string MASKE = "Form_Pruefstand";

        private readonly Kulturvorrichtung _kultur = new();
        private readonly Action<string> _senkeVorher = KiMaskenbruecke.Protokollsenke;

        public KiMaskenbrueckeTests()
        {
            KiMaskenbruecke.Leeren();
        }

        public void Dispose()
        {
            KiMaskenbruecke.Leeren();
            KiMaskenbruecke.Protokollsenke = _senkeVorher;
            _kultur.Dispose();
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        private static KiDialogFeld Feld(string name, string pfad, string anzeige,
                                         KiParameterTyp typ = KiParameterTyp.Zahl,
                                         string einheit = null)
            => new KiDialogFeld(name, pfad, anzeige, typ, "Erläuterung zu " + anzeige + ".",
                                einheit: einheit, leerErlaubt: true);

        private static KiDialog Maske(params KiDialogFeld[] felder)
            => new KiDialog(MASKE, "Prüfstand", felder);

        private static KiFeldzugang Zugang(KiDialogFeld feld, Func<object> lesen,
                                           Action<object> setzen = null)
            => new KiFeldzugang(feld, lesen, setzen);

        // =====================================================================
        //  An- und Abmelden
        // =====================================================================

        [Fact]
        public void Eine_angemeldete_Maske_steht_in_Offene_und_ist_die_aktive()
        {
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");
            object marke = KiMaskenbruecke.Anmelden(MASKE, Maske(f),
                                                    new[] { Zugang(f, () => 12.5) });

            Assert.NotNull(marke);
            Assert.True(KiMaskenbruecke.IstAngemeldet(MASKE));
            Assert.Equal(new[] { MASKE }, KiMaskenbruecke.Offene());
            Assert.Equal(MASKE, KiMaskenbruecke.AktiveMaske());
        }

        [Fact]
        public void Ohne_Felder_wird_gar_nicht_erst_angemeldet()
        {
            // Eine Maske ohne einen einzigen lesbaren Wert hat nichts mitzuteilen; sie
            // liesse im Chat den Schalter erscheinen und truege nichts bei.
            Assert.Null(KiMaskenbruecke.Anmelden(MASKE, Maske(), Array.Empty<KiFeldzugang>()));
            Assert.False(KiMaskenbruecke.IstAngemeldet(MASKE));
            Assert.Empty(KiMaskenbruecke.Offene());
        }

        [Fact]
        public void Abmelden_ist_idempotent_und_kennt_keine_unbekannte_Maske()
        {
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");
            object marke = KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => 1.0) });

            KiMaskenbruecke.Abmelden(MASKE, marke);
            KiMaskenbruecke.Abmelden(MASKE, marke);        // zweimal ist kein Fehler
            KiMaskenbruecke.Abmelden("GibtEsNicht");       // unbekannt ebenso wenig
            KiMaskenbruecke.Abmelden(null);
            KiMaskenbruecke.Abmelden("");

            Assert.False(KiMaskenbruecke.IstAngemeldet(MASKE));
            Assert.Empty(KiMaskenbruecke.Offene());
            Assert.Equal("", KiMaskenbruecke.AktiveMaske());
        }

        [Fact]
        public void Die_zweite_Anmeldung_loest_die_erste_ab_und_deren_Abmelden_raeumt_sie_nicht_weg()
        {
            // Unter Blazor entsteht beim Wiederaufbau einer Ueberlagerung die zweite
            // Instanz, waehrend die erste noch nicht entsorgt ist. Raeumte ihr Dispose
            // den Eintrag, staende die offene Maske ohne Anmeldung da.
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");

            object alt = KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => 1.0) });
            object neu = KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => 2.0) });

            Assert.NotSame(alt, neu);
            Assert.Single(KiMaskenbruecke.Offene());
            Assert.Equal("2", KiMaskenbruecke.Lesen(MASKE)[0].Text);

            KiMaskenbruecke.Abmelden(MASKE, alt);          // die scheidende Instanz

            Assert.True(KiMaskenbruecke.IstAngemeldet(MASKE));
            Assert.Equal("2", KiMaskenbruecke.Lesen(MASKE)[0].Text);

            KiMaskenbruecke.Abmelden(MASKE, neu);
            Assert.False(KiMaskenbruecke.IstAngemeldet(MASKE));
        }

        [Fact]
        public void Die_aktive_Maske_ist_die_zuletzt_angemeldete()
        {
            // Eine Ueberlagerung liegt UEBER ihrem Wirt; gemeint ist immer die obere.
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");

            KiMaskenbruecke.Anmelden("Form_Wirt",
                                     new KiDialog("Form_Wirt", "Wirt", new[] { f }),
                                     new[] { Zugang(f, () => 1.0) });
            KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => 2.0) });

            Assert.Equal(MASKE, KiMaskenbruecke.AktiveMaske());
            Assert.Equal("2", KiMaskenbruecke.Lesen()[0].Text);   // ohne Namen: die aktive
        }

        // =====================================================================
        //  Lesen
        // =====================================================================

        [Fact]
        public void Lesen_liefert_Deklaration_Rohwert_und_Text()
        {
            KiDialogFeld zahl = Feld("leistung", "Daten.Leistung", "Leistung",
                                     KiParameterTyp.Zahl, "kW");
            KiDialogFeld haken = Feld("brennwert", "Daten.Brennwert", "Brennwertkessel",
                                      KiParameterTyp.Wahrheitswert);
            KiDialogFeld leer = Feld("vorlauf", "Daten.Vorlauf", "Vorlauf",
                                     KiParameterTyp.Ganzzahl, "°C");

            KiMaskenbruecke.Anmelden(MASKE, Maske(zahl, haken, leer), new[]
            {
                Zugang(zahl, () => 12.5, w => { }),
                Zugang(haken, () => true),
                Zugang(leer, () => null)
            });

            IReadOnlyList<KiFeldwert> werte = KiMaskenbruecke.Lesen(MASKE);

            Assert.Equal(3, werte.Count);

            Assert.Equal("leistung", werte[0].Name);
            Assert.Equal("Leistung", werte[0].Anzeigename);
            Assert.Equal("kW", werte[0].Einheit);
            Assert.Equal(KiParameterTyp.Zahl, werte[0].Typ);
            Assert.Equal(12.5, Assert.IsType<double>(werte[0].Rohwert));
            Assert.Equal("12,5", werte[0].Text);            // Anwenderkultur, nicht invariant
            Assert.True(werte[0].Setzbar);                  // der Setzer ist da (S3)

            // Ein Wahrheitswert wird AUSGESCHRIEBEN: Der Block geht in eine Anfrage, die
            // auf Deutsch beantwortet wird; „True" waere dort ein Fremdkoerper.
            Assert.Equal("Ja", werte[1].Text);
            Assert.False(werte[1].Setzbar);                 // ohne Setzer: nur lesbar

            Assert.True(werte[2].IstLeer);
            Assert.Null(werte[2].Rohwert);
        }

        [Fact]
        public void Ein_werfender_Getter_laesst_nur_SEIN_Feld_leer()
        {
            // Ein Dialog kann waehrend des Lesens abgeraeumt werden. Eine
            // Assistentenauskunft darf unvollstaendig sein - sie darf nie sprengen.
            KiDialogFeld kaputt = Feld("kaputt", "Daten.Kaputt", "Kaputt");
            KiDialogFeld heil = Feld("heil", "Daten.Heil", "Heil");

            KiMaskenbruecke.Anmelden(MASKE, Maske(kaputt, heil), new[]
            {
                Zugang(kaputt, () => throw new InvalidOperationException("weg")),
                Zugang(heil, () => 7.0)
            });

            IReadOnlyList<KiFeldwert> werte = KiMaskenbruecke.Lesen(MASKE);

            Assert.True(werte[0].IstLeer);
            Assert.Equal("7", werte[1].Text);
        }

        [Fact]
        public void Eine_nicht_angemeldete_Maske_liest_leer_statt_zu_werfen()
        {
            Assert.Empty(KiMaskenbruecke.Lesen("GibtEsNicht"));
            Assert.Empty(KiMaskenbruecke.Lesen());
            Assert.Null(KiMaskenbruecke.Dialogdaten("GibtEsNicht"));
            Assert.Null(KiMaskenbruecke.Katalogeintrag("GibtEsNicht"));
        }

        [Fact]
        public void Der_Feldzugang_findet_sein_Feld_ueber_den_logischen_Namen()
        {
            // Der Einstieg des Setzweges der Stufe S3 (Auftrag #201).
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");
            double geschrieben = 0.0;

            KiMaskenbruecke.Anmelden(MASKE, Maske(f),
                                     new[] { Zugang(f, () => 1.0, w => geschrieben = (double)w) });

            KiFeldzugang zugang = KiMaskenbruecke.Feldzugang(MASKE, "leistung");

            Assert.NotNull(zugang);
            Assert.True(zugang.Setzbar);
            zugang.Setzen(42.0);
            Assert.Equal(42.0, geschrieben);

            Assert.Null(KiMaskenbruecke.Feldzugang(MASKE, "gibtesnicht"));
            Assert.Null(KiMaskenbruecke.Feldzugang("GibtEsNicht", "leistung"));
        }

        // =====================================================================
        //  Der Feldblock
        // =====================================================================

        [Fact]
        public void Der_Feldblock_nennt_Maske_Feldzahl_Einheit_und_Wert()
        {
            KiDialogFeld zahl = Feld("leistung", "Daten.Leistung", "Leistung",
                                     KiParameterTyp.Zahl, "kW");
            KiDialogFeld leer = Feld("vorlauf", "Daten.Vorlauf", "Vorlauf",
                                     KiParameterTyp.Ganzzahl, "°C");

            KiMaskenbruecke.Anmelden(MASKE, Maske(zahl, leer), new[]
            {
                Zugang(zahl, () => 12.5),
                Zugang(leer, () => null)
            });

            KiDialogdaten daten = KiMaskenbruecke.Dialogdaten(MASKE);

            Assert.NotNull(daten);
            Assert.True(daten.Belegt);
            Assert.Equal(MASKE, daten.Maskenname);
            Assert.Equal("Prüfstand", daten.Anzeigename);
            Assert.Equal(2, daten.Feldzahl);

            Assert.Contains("Prüfstand", daten.Text, StringComparison.Ordinal);
            Assert.Contains("Leistung [kW]: 12,5", daten.Text, StringComparison.Ordinal);

            // Ein leeres Feld wird BENANNT und nicht verschwiegen - sonst saehe der
            // Anwender in der Vorschau einen Anzeigefehler.
            Assert.Contains("Vorlauf [°C]: (leer)", daten.Text, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Block_liest_bei_JEDEM_Aufruf_neu()
        {
            // Der Getter zeigt auf den Dialogzustand, nicht auf eine Kopie vom Anmelden.
            double stand = 1.0;
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");

            KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => stand) });

            Assert.Contains("Leistung: 1", KiMaskenbruecke.Dialogdaten(MASKE).Text,
                            StringComparison.Ordinal);
            stand = 99.0;
            Assert.Contains("Leistung: 99", KiMaskenbruecke.Dialogdaten(MASKE).Text,
                            StringComparison.Ordinal);
        }

        // =====================================================================
        //  Das Protokoll
        // =====================================================================

        [Fact]
        public void Die_Protokollzeile_nennt_Maske_und_Feldzahl()
        {
            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");
            KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => 1.0) });

            var gemerkt = new List<string>();
            KiMaskenbruecke.Protokollsenke = gemerkt.Add;

            KiMaskenbruecke.Vermerken(KiMaskenbruecke.Dialogdaten(MASKE));

            string zeile = Assert.Single(gemerkt);
            Assert.Equal(zeile, KiMaskenbruecke.LetzteProtokollzeile);

            // Das Format ist das JEDER Protokollzeile (Fachkonzept 3.6) - ein Freitext
            // daneben zerstoerte es fuer den Leser.
            KiProtokollEintrag eintrag = KiProtokoll.Lies(zeile);
            Assert.NotNull(eintrag);
            Assert.Equal("dialog_daten", eintrag.Aktion);
            Assert.Equal(Schutzstufe.Lesen, eintrag.Stufe);
            Assert.Equal(KiStatus.Ausgefuehrt, eintrag.Status);
            Assert.Contains(MASKE, eintrag.Parameter, StringComparison.Ordinal);
            Assert.Contains("\"felder\":1", eintrag.Parameter, StringComparison.Ordinal);
            Assert.Contains("Prüfstand", eintrag.Ergebnis, StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Daten_wird_nichts_vermerkt_und_eine_werfende_Senke_kippt_nichts()
        {
            var gemerkt = new List<string>();
            KiMaskenbruecke.Protokollsenke = _ => throw new InvalidOperationException("Platte voll");

            KiMaskenbruecke.Vermerken(null);
            KiMaskenbruecke.Vermerken(new KiDialogdaten());

            Assert.Empty(gemerkt);

            KiDialogFeld f = Feld("leistung", "Daten.Leistung", "Leistung");
            KiMaskenbruecke.Anmelden(MASKE, Maske(f), new[] { Zugang(f, () => 1.0) });

            // Ein Protokollfehler darf eine Anfrage nicht kippen.
            KiMaskenbruecke.Vermerken(KiMaskenbruecke.Dialogdaten(MASKE));
        }

        // =====================================================================
        //  Nebenläufigkeit
        // =====================================================================

        [Fact]
        public async Task Anmelden_Lesen_und_Abmelden_laufen_nebeneinander_ohne_Ausnahme()
        {
            // Angemeldet wird auf dem Renderer-Faden, gelesen im Zweifel aus einem
            // Hintergrundlauf des Chats. Eine herausgereichte Sammlung, die sich unter
            // dem Leser veraendert, waere die naechste seltene Ausnahme.
            const int Faeden = 8;
            const int Runden = 200;

            var fehler = new List<Exception>();
            var aufgaben = new List<Task>();

            for (int nummer = 0; nummer < Faeden; nummer++)
            {
                int eigen = nummer;
                aufgaben.Add(Task.Run(() =>
                {
                    try
                    {
                        string name = "Form_Faden" + eigen.ToString(CultureInfo.InvariantCulture);
                        KiDialogFeld f = Feld("wert", "Daten.Wert", "Wert");
                        var d = new KiDialog(name, "Faden " + eigen, new[] { f });

                        for (int i = 0; i < Runden; i++)
                        {
                            object marke = KiMaskenbruecke.Anmelden(
                                name, d, new[] { Zugang(f, () => (double)eigen) });

                            foreach (string offen in KiMaskenbruecke.Offene())
                                KiMaskenbruecke.Lesen(offen);

                            KiMaskenbruecke.Dialogdaten(name);
                            KiMaskenbruecke.Abmelden(name, marke);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (fehler) fehler.Add(ex);
                    }
                }));
            }

            await Task.WhenAll(aufgaben);

            Assert.True(fehler.Count == 0,
                        fehler.Count == 0 ? "" : fehler[0].ToString());
            Assert.Empty(KiMaskenbruecke.Offene());
        }
    }
}
