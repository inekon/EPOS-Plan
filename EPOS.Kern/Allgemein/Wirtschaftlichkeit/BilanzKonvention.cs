using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die aufgelösten Bilanzierungsregeln eines Rechenlaufs — Leitentscheidungen
    /// <b>L12</b> (Methodenwechsel zum 01.01.2027) und <b>L13</b>
    /// (Bilanzierungskonvention für Biomasse) aus
    /// <c>Konzept_BHKW_Kosten_Erloese.md</c>.
    ///
    /// <para>
    /// <b>Reine Funktion über DTOs (L9).</b> Die Klasse liest die Datenbank nicht
    /// selbst; sie bekommt den bereits geladenen <see cref="GesetzKatalog"/> und den
    /// Parametersatz und liefert daraus einen unveränderlichen Zustand. Damit ist sie
    /// aus dem Rechenkern heraus verwendbar und ohne Datenbank prüfbar.
    /// </para>
    ///
    /// <para>
    /// <b>L12 — ein Schalter, nicht zwei.</b> Zum 01.01.2027 entfällt der
    /// Verdrängungsstrommix (2,8 bzw. 860 g CO₂-Äq/kWh) ersatzlos; die
    /// Stromgutschriftmethode für eingespeisten KWK-Strom ist abgeschafft (GModG,
    /// BGBl. 2026 I Nr. 226 — Grundlagen, Abschnitt 7.4). Umgeschaltet wird über
    /// <b>dasselbe Gültig-ab-Datum aus dem Katalog</b>: Die 2027er-Jahreszeile von
    /// <c>EF_NACHWEIS_VERDRAENGUNGSSTROMMIX</c> führt bewusst KEINEN Wert (Etappe E1),
    /// und genau dieses Fehlen ist der Schalter. Keine Jahreszahl im Code.
    /// </para>
    ///
    /// <para>
    /// <b>Und L11 bleibt strikt.</b> Gelesen wird von der Nachweiszeile ausschließlich,
    /// <i>ob</i> sie einen Wert führt — der Wert 860 belegt <b>keine</b> Variable dieser
    /// Klasse und erreicht keine Bilanzrechnung. Der einzige Faktor, den L12 in die
    /// Bilanz einspeist, ist <see cref="SubstitutionsfaktorGJeKWh"/> aus der Klasse
    /// <c>EF_BILANZ</c>.
    /// </para>
    ///
    /// <para>
    /// <b>L13 — zwei getrennte Angaben.</b> Die <see cref="BiomasseKonvention"/>
    /// entscheidet, ob biogenes Verbrennungs-CO₂ in der <b>Klimabilanz</b> angesetzt
    /// wird; der <see cref="NachhaltigkeitsnachweisBiomasse"/> entscheidet, ob der
    /// Nullansatz des § 8 EBeV 2030 in der <b>BEHG-Abgabe</b> zulässig ist. Das sind
    /// verschiedene Fragen an verschiedene Regelwerke und deshalb zwei Felder.
    /// </para>
    /// </summary>
    public sealed class BilanzKonvention
    {
        /// <summary>
        /// Stichtagsjahr, das gilt, wenn das Projekt kein Bilanzjahr führt: das letzte
        /// Jahr des alten Rechtsstands.
        ///
        /// <para><b>Bewusst eine feste Zahl und bewusst nicht <c>DateTime.Now.Year</c>.</b>
        /// Ein gespeichertes Projekt muss 2029 dieselben Zahlen liefern wie 2026
        /// (Grundlagen 7.1, Grund 2); ein Jahr aus der Systemuhr bräche das an jedem
        /// Jahreswechsel. Zugleich hält die Vorgabe jede Bestandsrechnung unverändert.
        /// Wer den neuen Rechtsstand will, trägt das Bilanzjahr ein — die Auswahl ist
        /// sichtbar und steht im Bericht.</para>
        /// </summary>
        public const int BILANZJAHR_RUECKFALL = 2026;

        private BilanzKonvention() { }

        /// <summary>Stichtagsjahr, mit dem der Katalog gelesen wurde.</summary>
        public int BilanzJahr { get; private set; }

        /// <summary>true, wenn <see cref="BilanzJahr"/> aus dem Rückfall stammt.</summary>
        public bool BilanzJahrAusRueckfall { get; private set; }

        /// <summary>Gewählte Methode, Steuerwert <c>DbWerte.EMISSIONSMETHODE_*</c>.</summary>
        public string MethodeWahl { get; private set; }

        /// <summary>Tatsächlich angewandte Methode; bei <c>KATALOG</c> die aufgelöste.</summary>
        public string MethodeWirksam { get; private set; }

        /// <summary>true, wenn der Katalog den Verdrängungsstrommix zum Bilanzjahr als
        /// entfallen führt (Jahreszeile vorhanden, aber ohne Wert).</summary>
        public bool VerdraengungEntfallen { get; private set; }

        /// <summary>Gültig-ab-Jahr der Katalogzeile, die den Schalter gestellt hat;
        /// 0 = keine Zeile gefunden (dann bleibt es beim Bestandsweg).</summary>
        public int SchalterJahrVon { get; private set; }

        /// <summary>Substitutionsfaktor [g CO₂-Äq/kWh]; <c>null</c> = nicht gepflegt.</summary>
        public double? SubstitutionsfaktorGJeKWh { get; private set; }

        /// <summary>Konvention Biomasse, Steuerwert <c>DbWerte.BIOMASSE_KONVENTION_*</c>.</summary>
        public string BiomasseKonvention { get; private set; }

        /// <summary>true = Nachhaltigkeitsnachweis nach § 8 EBeV 2030 liegt vor.</summary>
        public bool NachhaltigkeitsnachweisBiomasse { get; private set; }

        /// <summary>Biogenes Verbrennungs-CO₂ [g/kWh]; <c>null</c> = nicht gepflegt.</summary>
        public double? BiogenVerbrennungGJeKWh { get; private set; }

        /// <summary>Fossiler Standardwert der EBeV 2030 für flüssige Biomasse [g/kWh];
        /// <c>null</c> = nicht gepflegt.</summary>
        public double? EbevPflanzenoelGJeKWh { get; private set; }

        /// <summary>
        /// Nicht-fataler Hinweis, wenn eine Angabe fehlt oder sich zwei Katalogzeilen
        /// widersprechen; <c>null</c> = nichts anzumerken.
        /// </summary>
        public string Hinweis { get; private set; }

        // =====================================================================
        // Ableitung
        // =====================================================================

        /// <summary>
        /// Löst die Regeln für einen Parametersatz auf. <paramref name="katalog"/> darf
        /// <c>null</c> sein — dann gilt der Bestandsweg (Stromgutschrift), weil ein
        /// fehlender Katalog keine Rechnung umstellen darf.
        /// </summary>
        public static BilanzKonvention Bestimme(WirtschaftlichkeitParameter p, GesetzKatalog katalog)
        {
            var k = new BilanzKonvention
            {
                MethodeWahl = Wert(p == null ? null : p.EmissionsMethode, DbWerte.EMISSIONSMETHODE_KATALOG),
                BiomasseKonvention = Wert(p == null ? null : p.BiomasseKonvention,
                                          DbWerte.BIOMASSE_KONVENTION_NULL),
                NachhaltigkeitsnachweisBiomasse = p == null || p.NachhaltigkeitsnachweisBiomasse
            };

            int jahr = p != null ? p.BilanzJahr : 0;
            k.BilanzJahrAusRueckfall = jahr <= 0;
            k.BilanzJahr = k.BilanzJahrAusRueckfall ? BILANZJAHR_RUECKFALL : jahr;

            // --- L12: der eine Schalter -------------------------------------------
            // Gelesen wird NUR, ob die zum Bilanzjahr gueltige Zeile einen Wert fuehrt.
            // Der Wert selbst (860 g/kWh, Nachweisgroesse) wird nie uebernommen — L11.
            GesetzParameter ef = katalog == null ? null
                : katalog.WertMitHerkunft(DbWerte.GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX, k.BilanzJahr);
            k.SchalterJahrVon = ef == null ? 0 : ef.JahrVon;
            k.VerdraengungEntfallen = ef != null && !ef.Wert.HasValue;

            if (string.Equals(k.MethodeWahl, DbWerte.EMISSIONSMETHODE_KATALOG, StringComparison.Ordinal))
                k.MethodeWirksam = k.VerdraengungEntfallen
                    ? DbWerte.EMISSIONSMETHODE_OHNE_GUTSCHRIFT
                    : DbWerte.EMISSIONSMETHODE_STROMGUTSCHRIFT;
            else
                k.MethodeWirksam = k.MethodeWahl;

            // Gegenprobe gegen die zweite Verdraengungszeile (Primaerenergie). Sie wird
            // NICHT ausgewertet — sie darf den Schalter nicht stellen, sonst gaebe es
            // zwei, die auseinanderlaufen koennen (L12). Ein Widerspruch wird gemeldet.
            if (katalog != null)
            {
                GesetzParameter pef = katalog.WertMitHerkunft(
                    DbWerte.GESETZ_PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX, k.BilanzJahr);
                bool pefEntfallen = pef != null && !pef.Wert.HasValue;
                if (pef != null && pefEntfallen != k.VerdraengungEntfallen)
                    k.Hinweis = Anfuegen(k.Hinweis,
                        "Katalog widersprüchlich: Emissions- und Primärenergiezeile des " +
                        "Verdrängungsstrommix entfallen zu verschiedenen Stichtagen. " +
                        "Maßgeblich ist die Emissionszeile.");
            }

            // --- Faktoren der beiden Wahlmoeglichkeiten ----------------------------
            if (katalog != null)
            {
                k.SubstitutionsfaktorGJeKWh =
                    katalog.Wert(DbWerte.GESETZ_EF_BILANZ_SUBSTITUTION_STROM, k.BilanzJahr);
                k.BiogenVerbrennungGJeKWh =
                    katalog.Wert(DbWerte.GESETZ_EF_BILANZ_BIOGEN_VERBRENNUNG, k.BilanzJahr);
                k.EbevPflanzenoelGJeKWh =
                    katalog.Wert(DbWerte.GESETZ_EF_BILANZ_EBEV_PFLANZENOEL, k.BilanzJahr);
            }

            if (k.Substitution && !k.SubstitutionsfaktorGJeKWh.HasValue)
                k.Hinweis = Anfuegen(k.Hinweis,
                    "Substitutionsmethode gewählt, aber kein Substitutionsfaktor im Katalog — " +
                    "die Gutschrift entfällt.");
            if (k.BiogenVerbrennungAnsetzen && !k.BiogenVerbrennungGJeKWh.HasValue)
                k.Hinweis = Anfuegen(k.Hinweis,
                    "Biogenes Verbrennungs-CO₂ soll angesetzt werden, aber es steht kein Faktor " +
                    "im Katalog — es bleibt beim Nullansatz.");
            if (!k.NachhaltigkeitsnachweisBiomasse && !k.EbevPflanzenoelGJeKWh.HasValue)
                k.Hinweis = Anfuegen(k.Hinweis,
                    "Kein Nachhaltigkeitsnachweis angegeben, aber kein EBeV-Standardwert im " +
                    "Katalog — die BEHG-Abgabe bleibt unverändert.");

            return k;
        }

        // =====================================================================
        // Abfragen der Rechenwege
        // =====================================================================

        /// <summary>Bestandsweg: KWK-Strom wird im Kraftwerkspark verdrängt.</summary>
        public bool Stromgutschrift
        {
            get
            {
                return string.Equals(MethodeWirksam, DbWerte.EMISSIONSMETHODE_STROMGUTSCHRIFT,
                                     StringComparison.Ordinal);
            }
        }

        /// <summary>Rechtsstand ab 01.01.2027: keine Verdrängungsgutschrift.</summary>
        public bool OhneGutschrift
        {
            get
            {
                return string.Equals(MethodeWirksam, DbWerte.EMISSIONSMETHODE_OHNE_GUTSCHRIFT,
                                     StringComparison.Ordinal);
            }
        }

        /// <summary>Methodische Wahl: Gutschrift über den UBA-Substitutionsfaktor.</summary>
        public bool Substitution
        {
            get
            {
                return string.Equals(MethodeWirksam, DbWerte.EMISSIONSMETHODE_SUBSTITUTION,
                                     StringComparison.Ordinal);
            }
        }

        /// <summary>true, wenn biogenes Verbrennungs-CO₂ in die Klimabilanz eingeht.</summary>
        public bool BiogenVerbrennungAnsetzen
        {
            get
            {
                return string.Equals(BiomasseKonvention, DbWerte.BIOMASSE_KONVENTION_VERBRENNUNG,
                                     StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Der wirksame Zuschlag auf biogene Brennstoffe [g/kWh] — 0 beim Nullansatz
        /// und ebenso, wenn der Faktor nicht gepflegt ist (dann steht der Grund in
        /// <see cref="Hinweis"/>, statt dass eine 0 ihn verschweigt).
        /// </summary>
        public double BiogenZuschlagGJeKWh
        {
            get
            {
                return BiogenVerbrennungAnsetzen && BiogenVerbrennungGJeKWh.HasValue
                    ? BiogenVerbrennungGJeKWh.Value : 0.0;
            }
        }

        /// <summary>
        /// Der Emissionsfaktor [g/kWh], mit dem flüssige Biomasse ohne
        /// Nachhaltigkeitsnachweis in die BEHG-Abgabe eingeht; 0 = unverändert.
        /// </summary>
        public double BehgOhneNachweisGJeKWh
        {
            get
            {
                return !NachhaltigkeitsnachweisBiomasse && EbevPflanzenoelGJeKWh.HasValue
                    ? EbevPflanzenoelGJeKWh.Value : 0.0;
            }
        }

        // =====================================================================
        // Biogene Träger — die EINE Regel für beide Rechner
        // =====================================================================

        /// <summary>
        /// Ist dieser Brennstoff biogen? Entschieden wird über die Kategorie aus
        /// <c>Tab_BrennstoffKategorien</c> — Holz (5), Pellets (6), Rapsöl (8) und
        /// Tierische Fette (9) — sowie über den Bezeichner „Biogas", der in der
        /// Gaskategorie (1) steht. Dieselbe Bauform, mit der
        /// <c>KostenEmissionRechner</c> die BEHG-Pflichtigkeit bestimmt.
        ///
        /// <para><b>Bewusst NICHT erfasst sind die Bio-Heizöl-Blends</b> (Kategorie 2,
        /// „Heizöl Bio 5" bis „Heizöl Bio 20"). Ihr biogener Anteil steckt im
        /// Katalogfaktor (295/280/266/250 statt 310 g/kWh), das Datenmodell führt ihn
        /// aber nicht als eigene Größe. Ihn aus dem Namen zu lesen wäre geraten; die
        /// Konvention lässt diese Träger deshalb unangetastet und sagt es im Bericht.</para>
        /// </summary>
        public static bool IstBiogen(int idKategorie, string bezeichner)
        {
            if (idKategorie == 5 || idKategorie == 6 || idKategorie == 8 || idKategorie == 9)
                return true;
            return idKategorie == 1 && bezeichner != null &&
                   bezeichner.Trim().Equals("Biogas", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ist dieser biogene Brennstoff zugleich ein <b>BEHG-Brennstoff</b>? Nur die
        /// flüssige Biomasse — Rapsöl (8) und Tierische Fette (9) — steht in Anlage 2
        /// Teil 4 der EBeV 2030 („Pflanzenöl, auch Tierfette und Altspeiseöl",
        /// 74,0 t CO₂/TJ). Feste Biomasse, Biogas und Klärgas sind keine
        /// BEHG-Brennstoffe (Grundlagen 7.7); für sie ändert der fehlende
        /// Nachhaltigkeitsnachweis nichts.
        /// </summary>
        public static bool IstBehgBiogen(int idKategorie)
        {
            return idKategorie == 8 || idKategorie == 9;
        }

        // =====================================================================
        // Ausweis
        // =====================================================================

        /// <summary>
        /// Die Bilanzierungsregeln als eine Zeile für Reiter, Word und Excel — der
        /// AUSWEIS, den L12 und L13 verlangen. Anzeigetexte kommen ausschließlich aus
        /// <c>MyResource</c>; die Steuerwerte selbst erscheinen nie.
        /// </summary>
        public string Ausweis(CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            string methode;
            if (Substitution)
                methode = string.Format(kultur, MyResource.Resource.BILANZ_METHODE_SUBSTITUTION,
                    SubstitutionsfaktorGJeKWh.HasValue
                        ? SubstitutionsfaktorGJeKWh.Value.ToString("N0", kultur)
                        : MyResource.Resource.BILANZ_OHNE_WERT);
            else if (OhneGutschrift) methode = MyResource.Resource.BILANZ_METHODE_OHNE_GUTSCHRIFT;
            else methode = MyResource.Resource.BILANZ_METHODE_STROMGUTSCHRIFT;

            string herkunft = string.Equals(MethodeWahl, DbWerte.EMISSIONSMETHODE_KATALOG,
                                            StringComparison.Ordinal)
                ? (SchalterJahrVon > 0
                    ? string.Format(kultur, MyResource.Resource.BILANZ_HERKUNFT_KATALOG,
                                    SchalterJahrVon.ToString(CultureInfo.InvariantCulture))
                    : MyResource.Resource.BILANZ_HERKUNFT_KATALOG_LEER)
                : MyResource.Resource.BILANZ_HERKUNFT_WAHL;

            string konvention = BiogenVerbrennungAnsetzen
                ? string.Format(kultur, MyResource.Resource.BILANZ_BIOMASSE_VERBRENNUNG,
                    BiogenVerbrennungGJeKWh.HasValue
                        ? BiogenVerbrennungGJeKWh.Value.ToString("N0", kultur)
                        : MyResource.Resource.BILANZ_OHNE_WERT)
                : MyResource.Resource.BILANZ_BIOMASSE_NULL;

            string nachweis = NachhaltigkeitsnachweisBiomasse
                ? MyResource.Resource.BILANZ_NACHWEIS_JA
                : MyResource.Resource.BILANZ_NACHWEIS_NEIN;

            string jahr = BilanzJahr.ToString(CultureInfo.InvariantCulture) +
                          (BilanzJahrAusRueckfall ? " " + MyResource.Resource.BILANZ_JAHR_RUECKFALL : "");

            string t = string.Format(kultur, MyResource.Resource.BILANZ_AUSWEIS,
                                     jahr, methode, herkunft, konvention, nachweis);
            if (Hinweis != null) t += " — " + Hinweis;
            return t;
        }

        // =====================================================================

        private static string Wert(string wert, string vorgabe)
        {
            return string.IsNullOrEmpty(wert) ? vorgabe : wert.Trim();
        }

        private static string Anfuegen(string bisher, string neu)
        {
            return string.IsNullOrEmpty(bisher) ? neu : bisher + " | " + neu;
        }
    }
}
