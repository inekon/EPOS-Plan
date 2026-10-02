using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Textbündel der Konditionierung</b> (Stufe KP2, Welle U0a; Teilkonzept
    /// Konditionierungsprofile 7.7): <see cref="KonditionierungTexte"/> in <c>EPOS.UI</c>, seine
    /// Füllung <c>KonditionierungTexteHuelle.Texte()</c> in <c>EPOS.UI.Daten</c>, die beiden
    /// Ressourcendateien und der Abschnitt „Konditionierungsprofile“ des Glossars
    /// (<c>Dokumentation/aktuell/Glossar_Lokalisierung.md</c>, § 13).
    ///
    /// <para><b>Warum.</b> Ein Bündel fällt still auf seinen deutschen Rückfall zurück, wenn ein
    /// Schlüssel fehlt oder die Füllung eine Eigenschaft vergisst — die Oberfläche bliebe dann auch
    /// in Englisch deutsch, und kein anderer Fall merkte es. Diese Klasse hält die Kette
    /// <b>Eigenschaft → Schlüssel → beide Ressourcen → Glossar</b> zusammen: (a) jede Eigenschaft
    /// nennt ihren Schlüssel im Kommentar und wird in <c>Texte()</c> gefüllt — gelesen aus dem
    /// Quelltext und zur Laufzeit gegengeprüft; (b) unter de-DE gleicht jeder gefüllte Wert dem
    /// Rückfall; (c) unter en-US ist jeder Wert gesetzt, ohne deutsche Umlaute, und die Begriffe
    /// des Glossars tragen ihre Glossarübersetzung. Dazu die Präfixe der Schlüssel, die
    /// Platzhalter, die Kennungen des Schemas (jede hat ihren Anzeigetext) und der Wortlaut der
    /// Texte, den der Auftrag festlegt.</para>
    ///
    /// <para>Ohne Datenbank. Muster: <c>BerichtsvorlagenTextbuendelWacheTests</c> (englische
    /// Ressource ohne Rückgriff auf die neutrale) und <c>HuellenTextschluesselWacheTests</c>. Die
    /// Kultur ist gepinnt: de-DE für die Klasse, en-US in den Fällen, die Englisch lesen.</para>
    /// </summary>
    public sealed class KonditionierungTexteTests : IDisposable
    {
        private const string BUENDEL = "EPOS.UI/Dialoge/Bedarf/KonditionierungTexte.cs";
        private const string FUELLUNG = "EPOS.UI.Daten/Bedarf/KonditionierungTexteHuelle.cs";
        private const string GLOSSAR = "Dokumentation/aktuell/Glossar_Lokalisierung.md";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // (a) Jede Eigenschaft wird gefüllt
        // =================================================================================

        [Fact]
        public void Jede_Eigenschaft_nennt_ihren_Schluessel_und_Texte_fuellt_sie_mit_genau_diesem()
        {
            List<(string Eigenschaft, string Schluessel)> buendel = BuendelQuelle();
            List<(string Eigenschaft, string Schluessel)> fuellung = FuellungQuelle();
            string[] reflektiert = Eigenschaften().Select(p => p.Name).ToArray();

            // Gegenprobe: Der Leser sieht das Bündel überhaupt — ein kaputter Regulärausdruck fände nichts.
            Assert.True(reflektiert.Length >= 100, "Nur " + reflektiert.Length + " Eigenschaften im Bündel.");
            Assert.Equal(reflektiert.Length, buendel.Count);

            Assert.Equal(buendel.Count, buendel.Select(b => b.Eigenschaft).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(buendel.Count, buendel.Select(b => b.Schluessel).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(fuellung.Count, fuellung.Select(f => f.Eigenschaft).Distinct(StringComparer.Ordinal).Count());

            var ungefuellt = reflektiert.Except(fuellung.Select(f => f.Eigenschaft)).ToList();
            var ohneEigenschaft = fuellung.Select(f => f.Eigenschaft).Except(reflektiert).ToList();
            Assert.True(ungefuellt.Count == 0, "Diese Eigenschaften füllt Texte() nicht: " + string.Join(", ", ungefuellt));
            Assert.True(ohneEigenschaft.Count == 0, "Texte() füllt Eigenschaften, die es nicht gibt: " + string.Join(", ", ohneEigenschaft));

            Dictionary<string, string> imKommentar = buendel.ToDictionary(b => b.Eigenschaft, b => b.Schluessel, StringComparer.Ordinal);
            var abweichend = fuellung.Where(f => imKommentar[f.Eigenschaft] != f.Schluessel)
                                     .Select(f => f.Eigenschaft + ": Kommentar " + imKommentar[f.Eigenschaft] + ", Texte() " + f.Schluessel)
                                     .ToList();
            Assert.True(abweichend.Count == 0, string.Join("\n", abweichend));
        }

        [Fact]
        public void Das_Buendel_traegt_nur_Zeichenketten_und_keinen_Zustand()
        {
            var fremd = typeof(KonditionierungTexte).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                    .Where(p => p.PropertyType != typeof(string))
                                                    .Select(p => p.Name).ToList();
            Assert.True(fremd.Count == 0, "Kein Zustand im Textbündel: " + string.Join(", ", fremd));
        }

        /// <summary>
        /// Die Laufzeitprobe zu (a): Unter en-US unterscheidet sich jeder gefüllte Wert vom deutschen
        /// Vorgabewert — bis auf die wenigen Texte, die in beiden Sprachen gleich lauten. Eine vergessene
        /// Zeile in <c>Texte()</c> ließe den deutschen Vorgabewert stehen und fiele hier auf.
        /// </summary>
        [Fact]
        public void Unter_en_US_ist_jede_Eigenschaft_aus_der_englischen_Ressource_gefuellt()
        {
            var vorgabe = new KonditionierungTexte();
            using (new Kulturvorrichtung("en-US"))
            {
                KonditionierungTexte en = KonditionierungTexteHuelle.Texte();

                var deutschGeblieben = Eigenschaften().Select(p => p.Name)
                    .Where(n => Wert(en, n) == Wert(vorgabe, n) && !SprachneutraleTexte.Contains(n))
                    .ToList();
                Assert.True(deutschGeblieben.Count == 0, "Unter en-US noch deutsch: " + string.Join(", ", deutschGeblieben));

                // Gegenprobe: Die Ausnahmeliste ist nicht veraltet — jede ihrer Eigenschaften lautet wirklich gleich.
                foreach (string n in SprachneutraleTexte)
                    Assert.Equal(Wert(vorgabe, n), Wert(en, n));
            }
        }

        /// <summary>Die Texte, die in Deutsch und Englisch gleich lauten (Eigenname, Gedankenstrich als Platzhalter).</summary>
        private static readonly string[] SprachneutraleTexte =
        {
            "LabelInfiltration", "PlatzhalterLeer", "SpalteName", "LabelVorlageName"
        };

        // =================================================================================
        // (b) Unter de-DE gleicht jeder Wert dem Rückfall
        // =================================================================================

        [Fact]
        public void Unter_de_DE_gleicht_jeder_gefuellte_Wert_dem_Rueckfall()
        {
            var vorgabe = new KonditionierungTexte();
            KonditionierungTexte de = KonditionierungTexteHuelle.Texte();

            var abweichend = Eigenschaften().Select(p => p.Name)
                .Where(n => !string.Equals(Wert(de, n), Wert(vorgabe, n), StringComparison.Ordinal))
                .Select(n => n + ": „" + Wert(de, n) + "“ ≠ Rückfall „" + Wert(vorgabe, n) + "“")
                .ToList();
            Assert.True(abweichend.Count == 0, string.Join("\n", abweichend));
        }

        // =================================================================================
        // (c) Unter en-US ist jeder Wert gesetzt; die Glossarbegriffe tragen ihre Übersetzung
        // =================================================================================

        [Fact]
        public void Unter_en_US_ist_jeder_Wert_gesetzt_nicht_leer_und_ohne_deutsche_Umlaute()
        {
            using (new Kulturvorrichtung("en-US"))
            {
                KonditionierungTexte en = KonditionierungTexteHuelle.Texte();
                var funde = new List<string>();
                foreach (PropertyInfo p in Eigenschaften())
                {
                    string wert = Wert(en, p.Name);
                    if (string.IsNullOrWhiteSpace(wert)) funde.Add(p.Name + ": leer");
                    else if (wert.IndexOfAny("äöüÄÖÜß".ToCharArray()) >= 0) funde.Add(p.Name + ": deutsche Umlaute in „" + wert + "“");
                }
                Assert.True(funde.Count == 0, string.Join("\n", funde));

                // Die drei Begriffe, die der Auftrag ausdrücklich nennt.
                Assert.Equal("Conditioning", en.Reiter);
                Assert.Equal("Template", en.ZeileVorlage);
                Assert.Equal("off", en.ZelleAus);
            }
        }

        /// <summary>
        /// Die Texte des Bündels tragen die Übersetzung, die im Glossar steht — gelesen aus dem Glossar
        /// selbst. Links die Eigenschaft, rechts die deutsche Zelle des Glossars; der englische Wert
        /// enthält die Glossarübersetzung (bei „a / b“ eine der beiden), Groß- und Kleinschreibung frei.
        /// </summary>
        private static readonly (string Eigenschaft, string Begriff)[] Glossarbegriffe =
        {
            ("Reiter", "Konditionierung"),
            ("Matrix", "Vorgabe-Matrix"),
            ("GroesseHeizen", "Heizen"),
            ("GroesseKuehlen", "Kühlen"),
            ("GroesseLueftung", "Lüftung"),
            ("GroesseGeraete", "Geräte"),
            ("GroessePersonen", "Personen"),
            ("SpalteHeizen", "Heizen"),
            ("SpalteKuehlen", "Kühlen"),
            ("SpalteLueftung", "Lüftung"),
            ("SpalteGeraete", "Geräte"),
            ("SpaltePersonen", "Personen"),
            ("ZeileVorlage", "Vorlage"),
            ("LabelVorlageAuswahl", "Vorlage"),
            ("ZeileNennwert", "Nennwert"),
            ("ZeileFerien", "Ferien"),
            ("ZeileSaison", "Saison"),
            ("LabelSaisonStart", "Saison"),
            ("LabelFerienzeitraeume", "Ferienzeitraum"),
            ("LabelMaxRaumtemperatur", "Maximalraumtemperatur"),
            ("LabelSommerlueftung", "Sommerlüftung"),
            ("LabelInfiltration", "Infiltration"),
            ("LabelNutzerlueftung", "Nutzerlüftung"),
            ("LabelNachtauskuehlung", "Nachtauskühlung"),
            ("LabelNachtfenster", "Nachtfenster"),
            ("PlatzhalterVorgabe", "Vorgabe (eines leeren Felds)"),
            ("ZelleAus", "aus (Zellzustand)"),
            ("ZustandGebaeude", "erben / vom Gebäude"),
            ("KnopfErben", "erben / vom Gebäude"),
            ("KnopfKalenderAnlegen", "Kalender anlegen"),
            ("KnopfMatrixErneut", "Matrix erneut anwenden"),
            ("KnopfZuruecknehmen", "Zurücknehmen"),
            ("KnopfAlsVorlage", "Als Vorlage speichern"),
            ("KnopfVorlagenVerwalten", "Vorlagen verwalten"),
            ("KnopfKatalogErneut", "Aus dem Katalog erneut übernehmen"),
            ("KnopfUebernehmenAnpassen", "Vom Gebäude übernehmen und anpassen"),
            ("KnopfDuplizieren", "Duplizieren"),
            ("KnopfUmbenennen", "Umbenennen"),
            ("KnopfKopierenNach", "Kopieren nach …"),
            ("LabelKomfortsollwert", "Komfortsollwert"),
            ("KnopfZeitstruktur", "Zeitstruktur übernehmen"),
            ("LabelGrundangabe", "Grundangabe"),
            ("LabelStandardwoche", "Standardwoche"),
            ("LabelWochenraster", "Wochenraster"),
            ("LabelZeitfenster", "Zeitfenster"),
            ("LabelPerioden", "Periode"),
            ("SpalteRang", "Rang"),
            ("ArtFeiertag", "Feiertag"),
            ("ArtBetriebspause", "Betriebspause"),
            ("LabelWerkzeugFeiertage", "Feiertag"),
            ("LabelWieAnwesenheit", "Anwesenheit"),
            ("LabelTeppichbild", "Teppichbild"),
            ("TextTeppichbild", "Bezugsjahr"),
            ("MeldungGemeinjahr", "Gemeinjahr"),
            ("LabelVorlageNutzung", "Nutzung (einer Vorlage)"),
            ("LabelVorlageAusgeliefert", "ausgeliefert / eigen"),
            ("LabelVorlageEigen", "ausgeliefert / eigen"),
            ("TextJahresmittel", "Jahresmittel"),
            ("TextHerleitungGeraete", "Interne Wärmegewinne"),
            // KP2 U5 (E57): die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen“.
            ("HinweisVorlageAlle", "Vorlage in allen Größen übernehmen"),
        };

        [Fact]
        public void Die_Begriffe_des_Buendels_tragen_ihre_Glossaruebersetzung()
        {
            Dictionary<string, string> glossar = Glossar();
            using (new Kulturvorrichtung("en-US"))
            {
                KonditionierungTexte en = KonditionierungTexteHuelle.Texte();
                var funde = new List<string>();
                foreach ((string eigenschaft, string begriff) in Glossarbegriffe)
                {
                    if (!glossar.TryGetValue(begriff, out string glossarEn))
                    {
                        funde.Add(eigenschaft + ": im Glossar fehlt die Zeile „" + begriff + "“");
                        continue;
                    }
                    string wert = Wert(en, eigenschaft);
                    bool trifft = glossarEn.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries)
                                           .Any(teil => wert.IndexOf(teil.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!trifft) funde.Add(eigenschaft + ": „" + wert + "“ trägt nicht „" + glossarEn + "“ (Glossar, Zeile „" + begriff + "“)");
                }
                Assert.True(funde.Count == 0, string.Join("\n", funde));
            }
        }

        /// <summary>
        /// Die zwölf Begriffe aus Konzept 7.7 stehen wortgleich im Glossar, und die Begriffe, die der
        /// Auftrag zusätzlich nennt, haben dort eine Zeile.
        /// </summary>
        [Fact]
        public void Das_Glossar_fuehrt_die_zwoelf_Begriffe_des_Konzepts_und_die_uebrigen_des_Auftrags()
        {
            Dictionary<string, string> glossar = Glossar();
            var zwoelf = new (string De, string En)[]
            {
                ("Konditionierung", "conditioning"), ("Vorgabe-Matrix", "defaults matrix"), ("Vorlage", "template"),
                ("Standardwoche", "standard week"), ("Periode", "period"), ("Heizperiode", "heating period"),
                ("Kühlperiode", "cooling period"), ("Nachtauskühlung", "night purge ventilation"),
                ("Aufheizzeit", "preheat time"), ("Aufheizleistung", "preheat power"),
                ("Aufheizreserve", "preheat reserve"), ("Aufheizoptimierung", "preheat optimisation"),
            };
            foreach ((string de, string en) in zwoelf)
            {
                Assert.True(glossar.TryGetValue(de, out string wert), "Im Glossar fehlt „" + de + "“.");
                Assert.Equal(en, wert);
            }

            string[] weitere =
            {
                "Ferienzeitraum", "Feiertag", "Kalender", "Kalenderkarte", "Zeitfenster", "Wochenraster", "Teppichbild",
                "aus", "Anwesenheit", "Nennwert", "Nachtfenster", "Betriebspause", "Saison", "Zeitstruktur übernehmen",
                "Matrix erneut anwenden", "Zurücknehmen", "erben", "Heizen", "Kühlen", "Lüftung", "Geräte", "Personen",
                "Nutzerlüftung", "Infiltration"
            };
            var fehlen = weitere.Where(b => !glossar.Keys.Any(k => k == b || k.StartsWith(b + " ", StringComparison.Ordinal))).ToList();
            Assert.True(fehlen.Count == 0, "Im Glossar fehlen: " + string.Join(", ", fehlen));

            // „Feiertag“ wie § 14: public holiday.
            Assert.Equal("public holiday", glossar["Feiertag"]);
        }

        // =================================================================================
        // Die Ressourcen: beide Sprachen, gleiche Platzhalter
        // =================================================================================

        [Fact]
        public void Jeder_Schluessel_steht_in_beiden_Ressourcendateien_mit_gleichen_Platzhaltern()
        {
            List<(string Eigenschaft, string Schluessel)> buendel = BuendelQuelle();
            var vorgabe = new KonditionierungTexte();
            ResourceSet deutsch = Satz(CultureInfo.InvariantCulture);
            ResourceSet englisch = Satz(CultureInfo.GetCultureInfo("en-US"));

            var funde = new List<string>();
            foreach ((string eigenschaft, string schluessel) in buendel)
            {
                string de = deutsch.GetString(schluessel) ?? "";
                string en = englisch.GetString(schluessel) ?? "";
                if (de.Trim().Length == 0) funde.Add(schluessel + ": fehlt in Resource.resx");
                if (en.Trim().Length == 0) funde.Add(schluessel + ": fehlt in Resource.en-US.resx");
                if (de.Length > 0 && !string.Equals(de, Wert(vorgabe, eigenschaft), StringComparison.Ordinal))
                    funde.Add(schluessel + ": Vorgabewert im Bündel ≠ Text in Resource.resx");
                if (de.Length > 0 && en.Length > 0 && !Platzhalter(de).SequenceEqual(Platzhalter(en)))
                    funde.Add(schluessel + ": Platzhalter „" + de + "“ / „" + en + "“");
                foreach (string text in new[] { de, en })
                {
                    if (text.Length == 0) continue;
                    try { string.Format(CultureInfo.InvariantCulture, text, "a", "b", "c", "d", "e", "f"); }
                    catch (FormatException) { funde.Add(schluessel + ": Formatzeichenfolge fehlerhaft „" + text + "“"); }
                }
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        /// <summary>
        /// Die Gegenprobe zur Wache: Ein erfundener Schlüssel fällt durch — die englische Ressource wird
        /// wirklich OHNE Rückgriff auf die neutrale gelesen (sonst hielte der deutsche Text eine fehlende
        /// Übersetzung für vorhanden).
        /// </summary>
        [Fact]
        public void Ein_fehlender_Schluessel_faellt_auf()
        {
            Assert.Null(Satz(CultureInfo.GetCultureInfo("en-US")).GetString("KOND_LBL_GIBT_ES_NICHT"));
            Assert.Equal("Conditioning", Satz(CultureInfo.GetCultureInfo("en-US")).GetString("KOND_LBL_REITER"));
            Assert.Equal("Konditionierung", Satz(CultureInfo.InvariantCulture).GetString("KOND_LBL_REITER"));
        }

        [Fact]
        public void Die_Schluessel_folgen_der_Art_der_Eigenschaft_und_treffen_keinen_Schluessel_des_Kerns()
        {
            var funde = new List<string>();
            foreach ((string eigenschaft, string schluessel) in BuendelQuelle())
            {
                string soll = eigenschaft.StartsWith("Knopf", StringComparison.Ordinal) ? "KOND_BTN_"
                            : Regex.IsMatch(eigenschaft, "^(Hinweis|Grund|Meldung|Text|Zustand|Platzhalter|Wert[A-Z])") ? "KOND_TXT_"
                            : "KOND_LBL_";
                if (!schluessel.StartsWith(soll, StringComparison.Ordinal))
                    funde.Add(eigenschaft + ": " + schluessel + " statt " + soll + "…");
                // Die Meldungen und Wochentagstexte des Kerns (KP1) haben eigene Präfixe.
                if (schluessel.StartsWith("KOND_MSG_", StringComparison.Ordinal) || schluessel.StartsWith("KOND_TEXT_", StringComparison.Ordinal)
                    || schluessel.StartsWith("KOND_FRAGE_", StringComparison.Ordinal))
                    funde.Add(eigenschaft + ": " + schluessel + " gehört einem anderen Präfix");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        // =================================================================================
        // Anzeigetexte und Persistenzwerte
        // =================================================================================

        /// <summary>
        /// Jede Kennung des Schemas (Größe, Zeile, Art, Nutzung) hat ihren Anzeigetext im Bündel — und
        /// umgekehrt. Die Kennungen selbst sind Persistenzwerte (Glossar § 10) und bleiben deutsch und
        /// ASCII; angezeigt wird der Text aus der Ressource.
        /// </summary>
        [Fact]
        public void Jede_Kennung_des_Schemas_hat_einen_Anzeigetext_und_bleibt_selbst_unuebersetzt()
        {
            var zuordnung = new (string Name, IReadOnlyList<string> Kennungen, (string Kennung, string Eigenschaft)[] Anzeige)[]
            {
                ("Größen", DbWerte.KOND_GROESSEN, new[]
                {
                    (DbWerte.KOND_GROESSE_HEIZSOLL, "GroesseHeizen"), (DbWerte.KOND_GROESSE_KUEHLSOLL, "GroesseKuehlen"),
                    (DbWerte.KOND_GROESSE_LUEFTUNG, "GroesseLueftung"), (DbWerte.KOND_GROESSE_GERAETE, "GroesseGeraete"),
                    (DbWerte.KOND_GROESSE_PERSONEN, "GroessePersonen")
                }),
                ("Zeilen", DbWerte.KOND_ZEILEN, new[]
                {
                    (DbWerte.KOND_ZEILE_NENNWERT, "ZeileNennwert"), (DbWerte.KOND_ZEILE_TAG, "ZeileTag"),
                    (DbWerte.KOND_ZEILE_NACHT, "ZeileNacht"), (DbWerte.KOND_ZEILE_WOCHENENDE, "ZeileWochenende"),
                    (DbWerte.KOND_ZEILE_FERIEN, "ZeileFerien"), (DbWerte.KOND_ZEILE_SAISON, "ZeileSaison")
                }),
                ("Arten", DbWerte.KOND_ARTEN, new[]
                {
                    (DbWerte.KOND_ART_ZEITRAUM, "ArtZeitraum"), (DbWerte.KOND_ART_FERIEN, "ArtFerien"),
                    (DbWerte.KOND_ART_FEIERTAG, "ArtFeiertag"), (DbWerte.KOND_ART_BETRIEBSPAUSE, "ArtBetriebspause")
                }),
                ("Nutzungen", DbWerte.KOND_NUTZUNGEN, new[]
                {
                    (DbWerte.KOND_NUTZUNG_WOHNEN, "NutzungWohnen"), (DbWerte.KOND_NUTZUNG_BUERO, "NutzungBuero"),
                    (DbWerte.KOND_NUTZUNG_SCHULE, "NutzungSchule"), (DbWerte.KOND_NUTZUNG_SONSTIGE, "NutzungSonstige")
                }),
            };

            var namen = new HashSet<string>(Eigenschaften().Select(p => p.Name), StringComparer.Ordinal);
            foreach ((string name, IReadOnlyList<string> kennungen, (string Kennung, string Eigenschaft)[] anzeige) in zuordnung)
            {
                Assert.True(kennungen.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(anzeige.Select(a => a.Kennung).OrderBy(k => k, StringComparer.Ordinal)),
                    name + ": Das Schema führt Kennungen ohne Anzeigetext oder umgekehrt: " + string.Join(", ", kennungen));
                foreach ((string kennung, string eigenschaft) in anzeige)
                {
                    Assert.Contains(eigenschaft, namen);
                    Assert.Matches("^[A-Z0-9_]+$", kennung);          // Persistenzwerte: ASCII, GROSS
                }
            }

            // Das Kennwort „aus“ der Woche bleibt deutsch — auch wenn der Zellzustand englisch „off“ heißt.
            Assert.Equal("aus", DbWerte.KOND_WOCHE_AUS);
            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("off", KonditionierungTexteHuelle.Texte().ZelleAus);
                Assert.Equal("aus", DbWerte.KOND_WOCHE_AUS);
            }
        }

        // =================================================================================
        // Der Wortlaut, den der Auftrag festlegt
        // =================================================================================

        [Fact]
        public void Die_Kartenzustaende_lesen_sich_wie_im_Konzept_in_beiden_Sprachen()
        {
            KonditionierungTexte de = KonditionierungTexteHuelle.Texte();
            Assert.Equal("aus der Matrix", de.ZustandMatrix);
            Assert.Equal("aus Vorlage Büro", string.Format(CultureInfo.CurrentCulture, de.ZustandVorlage, "Büro"));
            Assert.Equal("angelegt, 3 eigene Perioden", string.Format(CultureInfo.CurrentCulture, de.ZustandAngelegt, 3));
            Assert.Equal("angelegt, 1 eigene Periode", de.ZustandAngelegtEine);
            Assert.Equal("vom Gebäude", de.ZustandGebaeude);

            using (new Kulturvorrichtung("en-US"))
            {
                KonditionierungTexte en = KonditionierungTexteHuelle.Texte();
                Assert.Equal("from the matrix", en.ZustandMatrix);
                // Der Vorlagenname ist ein Datenwert und bleibt deutsch.
                Assert.Equal("from template Büro", string.Format(CultureInfo.CurrentCulture, en.ZustandVorlage, "Büro"));
                Assert.Equal("created, 3 own periods", string.Format(CultureInfo.CurrentCulture, en.ZustandAngelegt, 3));
                Assert.Equal("created, 1 own period", en.ZustandAngelegtEine);
                Assert.Equal("from the building", en.ZustandGebaeude);
            }
        }

        [Fact]
        public void Die_Texte_des_Auftrags_stehen_wortgleich_im_Deutschen()
        {
            KonditionierungTexte de = KonditionierungTexteHuelle.Texte();

            // Spalten samt Einheit, Platzhalter, Zellzustand
            Assert.Equal("Heizen °C", de.SpalteHeizen);
            Assert.Equal("Kühlen °C", de.SpalteKuehlen);
            Assert.Equal("Lüftung 1/h", de.SpalteLueftung);
            Assert.Equal("Geräte W bzw. %", de.SpalteGeraete);
            Assert.Equal("Personen W bzw. %", de.SpaltePersonen);
            Assert.Equal("keine", de.PlatzhalterKeine);
            Assert.Equal("—", de.PlatzhalterLeer);
            Assert.Equal("aus", de.ZelleAus);

            // Die Heizzellen behalten die Feldnamen des Gebäudedialogs.
            Assert.Equal("Soll am Wochenende (ganztägig)", de.FeldHeizenWochenende);
            Assert.Equal("Soll in Ferien (ganztägig)", de.FeldHeizenFerien);

            // Knöpfe
            Assert.Equal("Kalender anlegen", de.KnopfKalenderAnlegen);
            Assert.Equal("Verwerfen", de.KnopfVerwerfen);
            Assert.Equal("Matrix erneut anwenden…", de.KnopfMatrixErneut);
            Assert.Equal("Zurücknehmen", de.KnopfZuruecknehmen);
            Assert.Equal("Übernehmen", de.KnopfUebernehmen);
            Assert.Equal("Als Vorlage speichern…", de.KnopfAlsVorlage);
            Assert.Equal("Vorlagen verwalten", de.KnopfVorlagenVerwalten);
            Assert.Equal("Aus dem Katalog erneut übernehmen…", de.KnopfKatalogErneut);

            // Hinweise und Gründe
            Assert.Equal("Unter VDI 6007 ist die Matrix Vorgabe; angelegte Kalender gehen vor.", de.HinweisVdi6007);
            Assert.Equal("Ausgelieferte Vorlage — nur lesbar; Duplizieren legt eine bearbeitbare Kopie an.", de.GrundVorlageGesperrt);
            Assert.Contains("TT.MM. im Gemeinjahr; den 29.02. gibt es nicht", de.MeldungGemeinjahr);
            Assert.Contains("Speichern unter", de.HinweisLesemodus);
            Assert.Contains("bearbeitbare Kopie", de.HinweisLesemodus);
            Assert.Contains("Tagesbilanz", de.HinweisTagesbilanz);
            Assert.Contains("Kühlbetrieb", de.GrundKuehlenGesperrt);
            Assert.Contains("sofort gespeichert", de.HinweisVorlageSofort);

            // Platzhalter der Herleitungen und der Bildunterschrift
            Assert.Contains("{4}", de.TextJahresmittel);
            Assert.Contains("{0}", de.TextTeppichbild);
        }

        [Fact]
        public void Die_Nutzung_einer_Vorlage_ist_ein_uebersetzter_Anzeigewert()
        {
            KonditionierungTexte de = KonditionierungTexteHuelle.Texte();
            Assert.Equal("Wohnen", de.NutzungWohnen);
            Assert.Equal("Büro", de.NutzungBuero);
            Assert.Equal("Schule", de.NutzungSchule);
            using (new Kulturvorrichtung("en-US"))
            {
                KonditionierungTexte en = KonditionierungTexteHuelle.Texte();
                Assert.Equal("Residential", en.NutzungWohnen);
                Assert.Equal("Office", en.NutzungBuero);
                Assert.Equal("School", en.NutzungSchule);
                Assert.Equal("Other", en.NutzungSonstige);
            }
        }

        // =================================================================================
        // Leser und Hilfen
        // =================================================================================

        private static PropertyInfo[] Eigenschaften()
            => typeof(KonditionierungTexte).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.PropertyType == typeof(string))
                                           .OrderBy(p => p.Name, StringComparer.Ordinal)
                                           .ToArray();

        private static string Wert(KonditionierungTexte texte, string eigenschaft)
            => (string)typeof(KonditionierungTexte).GetProperty(eigenschaft).GetValue(texte);

        private static readonly Regex KommentarSchluessel = new(@"<c>(?<k>KOND_(?:LBL|BTN|TXT)_[A-Z0-9_]+)</c>", RegexOptions.Compiled);
        private static readonly Regex EigenschaftZeile = new(@"^public string (?<n>\w+) \{ get; set; \}", RegexOptions.Compiled);
        private static readonly Regex FuellZeile = new(@"^t\.(?<p>\w+) = Text_\(""(?<k>[^""]+)"", t\.\k<p>\);$", RegexOptions.Compiled);

        /// <summary>Eigenschaft und Schlüssel aus dem Kommentar jeder Eigenschaft des Bündels.</summary>
        private static List<(string Eigenschaft, string Schluessel)> BuendelQuelle()
        {
            var liste = new List<(string, string)>();
            bool imKoerper = false;
            string schluessel = null;
            foreach (string roh in File.ReadAllLines(Pfad(BUENDEL)))
            {
                string zeile = roh.Trim();
                if (!imKoerper) { imKoerper = zeile.StartsWith("public sealed class KonditionierungTexte", StringComparison.Ordinal); continue; }
                if (zeile.StartsWith("///", StringComparison.Ordinal))
                {
                    Match m = KommentarSchluessel.Match(zeile);
                    if (m.Success && schluessel == null) schluessel = m.Groups["k"].Value;
                    continue;
                }
                Match p = EigenschaftZeile.Match(zeile);
                if (!p.Success) continue;
                Assert.True(schluessel != null, "Die Eigenschaft " + p.Groups["n"].Value + " nennt keinen Schlüssel im Kommentar.");
                liste.Add((p.Groups["n"].Value, schluessel));
                schluessel = null;
            }
            return liste;
        }

        /// <summary>Eigenschaft und Schlüssel aus den Zeilen <c>t.X = Text_("KEY", t.X);</c> der Füllung.</summary>
        private static List<(string Eigenschaft, string Schluessel)> FuellungQuelle()
        {
            var liste = new List<(string, string)>();
            foreach (string roh in File.ReadAllLines(Pfad(FUELLUNG)))
            {
                Match m = FuellZeile.Match(roh.Trim());
                if (m.Success) liste.Add((m.Groups["p"].Value, m.Groups["k"].Value));
            }
            return liste;
        }

        /// <summary>
        /// Das Glossar als Wörterbuch: deutsche Zelle → englische Zelle, aus allen Tafeln; die erste
        /// Zeile je deutscher Zelle gilt.
        /// </summary>
        private static Dictionary<string, string> Glossar()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string zeile in File.ReadAllLines(Pfad(GLOSSAR)))
            {
                if (!zeile.StartsWith("| ", StringComparison.Ordinal)) continue;
                string[] z = zeile.Split('|');
                if (z.Length < 4) continue;
                string de = z[1].Trim(), en = z[2].Trim();
                if (de.Length == 0 || de == "DE" || de.StartsWith("---", StringComparison.Ordinal)) continue;
                if (!d.ContainsKey(de)) d[de] = en;
            }
            Assert.True(d.Count >= 250, "Nur " + d.Count + " Glossarzeilen gelesen.");
            return d;
        }

        private static ResourceSet Satz(CultureInfo kultur)
        {
            ResourceSet satz = Resource.ResourceManager.GetResourceSet(kultur, true, false);
            Assert.NotNull(satz);
            return satz;
        }

        private static string[] Platzhalter(string text)
            => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

        private static string Pfad(string repoRelativ)
            => Path.Combine(Wurzel(), repoRelativ.Replace('/', Path.DirectorySeparatorChar));

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
