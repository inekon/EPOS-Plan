using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DER TYPAUFBAUTEN - Schemaschritt 192 (TypaufbauSchema), Welle BA-2 (Konzept Bauteilaufbau
    // beim Import 5.3, Entscheid E95-3/4/5/7).
    //
    // WOZU. Ein importiertes Aussenbauteil ohne vollstaendigen Aufbau (Stufe B: U der Datei; Stufe C: U der
    // Baualtersklasse) rechnete masselos (R1 = R/6). Der Import gibt ihm statt dessen einen ERSATZAUFBAU: die
    // Projektkopie eines Typaufbaus dieser Saat, die Daemmschicht (bzw. bei den Typen ohne Daemmung das lambda
    // der tragenden Schicht) auf das Ziel-U abgeglichen (Ersatzaufbau.Bilden). Die Saat ist KATALOG
    // (Tab_Bauteilaufbau_STAMM/Tab_Bauteilschicht_STAMM, ReadOnly = 1, Herkunft VORGABE), herstellerneutral.
    //
    // DIE ACHSEN. Bauteiltyp (Aussenwand, Dach bzw. oberste Decke, Boden gegen Erdreich bzw. unbeheizt) x
    // Bauart (massiv / leicht, Gebaeudebauweise) x Baualtersklasse (Baujahrregel: bis F ungedaemmt, ab G
    // gedaemmt - E95-4). Die Klasse waehlt nur zwischen den Varianten MIT und OHNE Daemmung; die Daemmdicke
    // selbst folgt dem Ziel-U. Innenbauteile bekommen keinen Typaufbau (E95-5: Klassenweg mit der Bauweise).
    // Neun Typen; "monolithisch" und "innen gedaemmt" waehlt der Import nicht selbst - sie stehen fuer die
    // Wahl je Aufbau (E95-4, Oberflaeche BA-3) bereit.
    //
    // DIE SCHICHTEN innen -> aussen, drei bis vier relevante (Konzept 4.1: Lage der Daemmung erhalten, die
    // raumseitigen Schichten bis zur ersten Daemmung einzeln). Jede Schicht zeigt auf einen Baustoff der
    // NORMSAAT (BaustoffSaattabelle.Norm, Ids 1 bis 65); lambda, rho und c sind die Werte dieser Zeile, also
    // DIN 4108-4:2020-11 bzw. DIN EN ISO 10456:2010-05 (die Quelle je Wert steht an der Baustoffzeile und
    // hier je Schicht im Kommentar). Kein Hersteller, kein Produkt.
    //
    // DIE CODES sind sprachneutral und stehen als Kopie in Tab_Bauteilaufbau(.._STAMM).Typaufbau (CHECK auf
    // genau diese Liste); NULL = ein echter Aufbau. Ein neuer Code braucht einen eigenen Schemaschritt.
    // ====================================================================================

    /// <summary>Wie ein Typaufbau auf ein Ziel-U abgeglichen wird.</summary>
    public enum Typabgleich
    {
        /// <summary>Die Dicke der Dämmschicht folgt dem Ziel-U (Band 0 … <see cref="TypaufbauSaat.DAEMMDICKE_MAX_M"/>).</summary>
        Daemmdicke = 0,

        /// <summary>Das λ der tragenden Schicht folgt dem Ziel-U (Band <see cref="TypaufbauSaat.LAMBDA_FAKTOR_MIN"/> … <see cref="TypaufbauSaat.LAMBDA_FAKTOR_MAX"/> des Katalogwerts); die Masse bleibt.</summary>
        Lambda = 1,
    }

    /// <summary>Eine Schicht eines Typaufbaus: Baustoff der Normsaat und Dicke [m].</summary>
    public sealed class TypaufbauSchicht
    {
        public TypaufbauSchicht(int idBaustoff, double dicke_M)
        {
            IdBaustoff = idBaustoff;
            Dicke_M = dicke_M;
        }

        /// <summary>Die Id des Baustoffs in der Normsaat (<see cref="BaustoffSaattabelle.Norm"/>).</summary>
        public int IdBaustoff { get; }

        /// <summary>Die Dicke der Saat [m]; bei der Abgleichschicht die Vorgabe ohne Abgleich.</summary>
        public double Dicke_M { get; }

        /// <summary>Der Baustoff der Normsaat (λ, ρ, c und Quelle).</summary>
        public BaustoffSaat Baustoff => BaustoffSaattabelle.Norm.First(b => b.Id == IdBaustoff);
    }

    /// <summary>
    /// <b>Ein Typaufbau</b> der Katalogsaat — Code, Bauteilart, Name, Schichten innen → außen und die
    /// Abgleichschicht (Kopf der Datei).
    /// </summary>
    public sealed class TypaufbauSaat
    {
        /// <summary>Größte Dämmdicke des Abgleichs [m] — darüber gilt die Vorgabedicke mit Vermerk.</summary>
        public const double DAEMMDICKE_MAX_M = 0.40;

        /// <summary>
        /// Kleinste Dämmdicke des Abgleichs [m] — darunter entfällt die Dämmschicht (ihr Widerstand liegt unter
        /// 0,03 m²K/W, und das Stoffwertband der Reduktion beginnt bei 0,5 mm).
        /// </summary>
        public const double DAEMMDICKE_MIN_M = 0.001;

        /// <summary>Kleinster Faktor auf das λ der tragenden Schicht beim Abgleich [–].</summary>
        public const double LAMBDA_FAKTOR_MIN = 0.5;

        /// <summary>Größter Faktor auf das λ der tragenden Schicht beim Abgleich [–].</summary>
        public const double LAMBDA_FAKTOR_MAX = 3.0;

        public TypaufbauSaat(string code, string bauteilart, string bezeichner, string beschreibung, Typabgleich abgleich,
                             int abgleichschicht, params TypaufbauSchicht[] schichten)
        {
            Code = code;
            Bauteilart = bauteilart;
            Bezeichner = bezeichner;
            Beschreibung = beschreibung;
            Abgleich = abgleich;
            Abgleichschicht = abgleichschicht;
            Schichten = schichten;
        }

        /// <summary>Der sprachneutrale Code (Spalte <c>Typaufbau</c>).</summary>
        public string Code { get; }

        /// <summary>Die Bauteilart des Katalogs (<see cref="DbWerte.BAUTEILART_AUSSENWAND"/>, …).</summary>
        public string Bauteilart { get; }

        /// <summary>Der Name im Katalog.</summary>
        public string Bezeichner { get; }

        /// <summary>Die Beschreibung im Katalog (Lage der Dämmung, Abgleich).</summary>
        public string Beschreibung { get; }

        /// <summary>Wie der Typ auf ein Ziel-U abgeglichen wird.</summary>
        public Typabgleich Abgleich { get; }

        /// <summary>Die Stelle (0 = raumseitig) der Schicht, die der Abgleich anfasst.</summary>
        public int Abgleichschicht { get; }

        /// <summary>Die Schichten innen → außen.</summary>
        public IReadOnlyList<TypaufbauSchicht> Schichten { get; }

        /// <summary>Trägt der Typ eine Dämmschicht (Abgleich über ihre Dicke)?</summary>
        public bool Gedaemmt => Abgleich == Typabgleich.Daemmdicke;
    }

    /// <summary>
    /// <b>Die Saat der Typaufbauten</b> (Kopf der Datei) — eine Quelle für Schemaschritt 192, Ersatzaufbau
    /// des Imports und Tests.
    /// </summary>
    public static class TypaufbauSaattabelle
    {
        // ---- Codes ----------------------------------------------------------------------------------
        public const string AW_MASSIV_AUSSENGEDAEMMT = "AW_MASSIV_AUSSENGEDAEMMT";
        public const string AW_MONOLITHISCH = "AW_MONOLITHISCH";
        public const string AW_MASSIV_UNGEDAEMMT = "AW_MASSIV_UNGEDAEMMT";
        public const string AW_MASSIV_INNENGEDAEMMT = "AW_MASSIV_INNENGEDAEMMT";
        public const string AW_HOLZLEICHTBAU = "AW_HOLZLEICHTBAU";
        public const string DA_STAHLBETON_GEDAEMMT = "DA_STAHLBETON_GEDAEMMT";
        public const string DA_SPARRENDACH = "DA_SPARRENDACH";
        public const string BO_DAEMMUNG_OBEN = "BO_DAEMMUNG_OBEN";
        public const string BO_DAEMMUNG_UNTEN = "BO_DAEMMUNG_UNTEN";

        /// <summary>Die Quelle jedes Typaufbaus im Katalog (die Stoffwerte stehen an den Baustoffen).</summary>
        public const string QUELLE = "Typaufbau EPOS (Konzept Bauteilaufbau 5.3); Stoffwerte DIN 4108-4:2020-11 / DIN EN ISO 10456:2010-05";

        // Baustoffe der Normsaat (BaustoffSaattabelle.Norm) mit ihrer Quelle:
        private const int GIPSPUTZ = 2;          // λ 0,43  ρ 1200 c 1000 — DIN 4108-4 Tab. 1 Z. 1.1.2; ISO 10456 Tab. 3
        private const int KALKZEMENTPUTZ = 1;    // λ 1,0   ρ 1800 c 1000 — DIN 4108-4 Tab. 1 Z. 1.1.1; ISO 10456 Tab. 4
        private const int LEICHTPUTZ = 3;        // λ 0,38  ρ 1000 c 1000 — DIN 4108-4 Tab. 1 Z. 1.1.4; ISO 10456 Tab. 4
        private const int ZEMENTESTRICH = 5;     // λ 1,4   ρ 2000 c 1000 — DIN 4108-4 Tab. 1 Z. 1.3.2; ISO 10456 Tab. 4
        private const int STAHLBETON = 10;       // λ 2,5   ρ 2400 c 1000 — ISO 10456 Tab. 3
        private const int VOLLZIEGEL = 13;       // λ 0,81  ρ 1800 c 1000 — DIN 4108-4 Tab. 1 Z. 4.1.2; ISO 10456 Tab. 4
        private const int HLZ_W_700 = 17;        // λ 0,24  ρ 700  c 1000 — DIN 4108-4 Tab. 1 Z. 4.1.4; ISO 10456 Tab. 4
        private const int KALKSANDSTEIN = 20;    // λ 0,99  ρ 1800 c 1000 — DIN 4108-4 Tab. 1 Z. 4.2; ISO 10456 Tab. 4
        private const int OSB = 32;              // λ 0,13  ρ 650  c 1700 — ISO 10456 Tab. 3
        private const int MINERALWOLLE = 36;     // λ 0,036 ρ 40   c 1030 — DIN 4108-4 Tab. 2 Z. 5.1 (λD 0,035); ISO 10456 Tab. 4
        private const int EPS = 39;              // λ 0,036 ρ 20   c 1450 — DIN 4108-4 Tab. 2 Z. 5.2 (λD 0,035); ISO 10456 Tab. 4
        private const int HOLZFASER = 43;        // λ 0,042 ρ 140  c 2000 — DIN 4108-4 Tab. 2 Z. 5.10 (λD 0,040); ISO 10456 Tab. 4
        private const int GIPSKARTON = 48;       // λ 0,21  ρ 700  c 1000 — DIN 4108-4 Tab. 1 Z. 3.4; ISO 10456 Tab. 3
        private const int BITUMENBAHN = 56;      // λ 0,17  ρ 1200 c 1000 — DIN 4108-4 Tab. 1 Z. 7.3.1; ISO 10456 Tab. 3

        private static TypaufbauSchicht S(int baustoff, double dicke) => new TypaufbauSchicht(baustoff, dicke);

        /// <summary>Die neun Typaufbauten, in der Folge des Katalogs.</summary>
        public static readonly IReadOnlyList<TypaufbauSaat> Alle = new[]
        {
            // ---- Außenwand ----------------------------------------------------------------------------
            new TypaufbauSaat(AW_MASSIV_AUSSENGEDAEMMT, DbWerte.BAUTEILART_AUSSENWAND,
                "Typaufbau Außenwand massiv, außen gedämmt",
                "Innenputz, Kalksandstein 17,5 cm, Wärmedämmung außen (Abgleich), Außenputz — Vorgabe ab Baualtersklasse G",
                Typabgleich.Daemmdicke, 2,
                S(GIPSPUTZ, 0.015), S(KALKSANDSTEIN, 0.175), S(EPS, 0.14), S(KALKZEMENTPUTZ, 0.01)),
            new TypaufbauSaat(AW_MONOLITHISCH, DbWerte.BAUTEILART_AUSSENWAND,
                "Typaufbau Außenwand monolithisch",
                "Innenputz, Hochlochziegel 36,5 cm (λ abgeglichen), Leichtputz — Wahl je Aufbau",
                Typabgleich.Lambda, 1,
                S(GIPSPUTZ, 0.015), S(HLZ_W_700, 0.365), S(LEICHTPUTZ, 0.02)),
            new TypaufbauSaat(AW_MASSIV_UNGEDAEMMT, DbWerte.BAUTEILART_AUSSENWAND,
                "Typaufbau Außenwand massiv, ungedämmt",
                "Innenputz, Vollziegel 36,5 cm (λ abgeglichen), Außenputz — Vorgabe bis Baualtersklasse F",
                Typabgleich.Lambda, 1,
                S(KALKZEMENTPUTZ, 0.015), S(VOLLZIEGEL, 0.365), S(KALKZEMENTPUTZ, 0.02)),
            new TypaufbauSaat(AW_MASSIV_INNENGEDAEMMT, DbWerte.BAUTEILART_AUSSENWAND,
                "Typaufbau Außenwand massiv, innen gedämmt",
                "Gipskartonplatte, Wärmedämmung innen (Abgleich), Vollziegel 36,5 cm, Außenputz — Wahl je Aufbau",
                Typabgleich.Daemmdicke, 1,
                S(GIPSKARTON, 0.0125), S(MINERALWOLLE, 0.06), S(VOLLZIEGEL, 0.365), S(KALKZEMENTPUTZ, 0.02)),
            new TypaufbauSaat(AW_HOLZLEICHTBAU, DbWerte.BAUTEILART_AUSSENWAND,
                "Typaufbau Außenwand Holzleichtbau",
                "Gipskartonplatte, OSB-Platte, Gefachdämmung (Abgleich), Holzfaserdämmplatte — Vorgabe bei Bauart leicht",
                Typabgleich.Daemmdicke, 2,
                S(GIPSKARTON, 0.0125), S(OSB, 0.015), S(MINERALWOLLE, 0.16), S(HOLZFASER, 0.06)),

            // ---- Dach und oberste Geschossdecke -------------------------------------------------------
            new TypaufbauSaat(DA_STAHLBETON_GEDAEMMT, DbWerte.BAUTEILART_DACH,
                "Typaufbau Dach Stahlbeton, gedämmt",
                "Deckenputz, Stahlbeton 20 cm, Wärmedämmung oben (Abgleich), Abdichtung — Vorgabe bei Bauart massiv",
                Typabgleich.Daemmdicke, 2,
                S(GIPSPUTZ, 0.01), S(STAHLBETON, 0.20), S(EPS, 0.16), S(BITUMENBAHN, 0.01)),
            new TypaufbauSaat(DA_SPARRENDACH, DbWerte.BAUTEILART_DACH,
                "Typaufbau Dach Sparrendach",
                "Gipskartonplatte, Zwischensparrendämmung (Abgleich), Holzfaser-Unterdeckplatte — Vorgabe bei Bauart leicht",
                Typabgleich.Daemmdicke, 1,
                S(GIPSKARTON, 0.0125), S(MINERALWOLLE, 0.20), S(HOLZFASER, 0.04)),

            // ---- Boden gegen Erdreich bzw. unbeheizt --------------------------------------------------
            new TypaufbauSaat(BO_DAEMMUNG_OBEN, DbWerte.BAUTEILART_BODENPLATTE,
                "Typaufbau Bodenplatte, Dämmung oben",
                "Zementestrich, Wärmedämmung unter dem Estrich (Abgleich), Stahlbeton 20 cm — Vorgabe gegen Erdreich",
                Typabgleich.Daemmdicke, 1,
                S(ZEMENTESTRICH, 0.05), S(EPS, 0.08), S(STAHLBETON, 0.20)),
            new TypaufbauSaat(BO_DAEMMUNG_UNTEN, DbWerte.BAUTEILART_BODENPLATTE,
                "Typaufbau Kellerdecke, Dämmung unten",
                "Zementestrich, Stahlbeton 18 cm, Wärmedämmung unterseitig (Abgleich) — Vorgabe gegen unbeheizt und Außenluft",
                Typabgleich.Daemmdicke, 2,
                S(ZEMENTESTRICH, 0.05), S(STAHLBETON, 0.18), S(MINERALWOLLE, 0.10)),
        };

        /// <summary>Alle Codes in der Folge der Saat — die Werteliste der CHECK-Klausel.</summary>
        public static IReadOnlyList<string> Codes => Alle.Select(t => t.Code).ToArray();

        /// <summary>Der Typaufbau zu einem Code; <c>null</c> = keiner.</summary>
        public static TypaufbauSaat Zu(string code)
            => string.IsNullOrEmpty(code) ? null : Alle.FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.Ordinal));

        /// <summary>
        /// Der Typaufbau als Aufbaumodell (Werte der Normsaat als Kopie, <c>ID_Baustoff</c> = Id der Normsaat,
        /// Herkunft <see cref="DbWerte.HERKUNFT_VORGABE"/>, Code in <c>Typaufbau</c>).
        /// </summary>
        public static BauteilaufbauModel AlsModell(TypaufbauSaat t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            var m = new BauteilaufbauModel
            {
                Bezeichner = t.Bezeichner,
                Beschreibung = t.Beschreibung,
                Bauteilart = t.Bauteilart,
                Quelle = QUELLE,
                Herkunft = DbWerte.HERKUNFT_VORGABE,
                Typaufbau = t.Code,
            };
            foreach (TypaufbauSchicht s in t.Schichten)
            {
                BaustoffSaat b = s.Baustoff;
                m.Schichten.Add(new BauteilschichtModel
                {
                    Reihenfolge = m.Schichten.Count + 1,
                    ID_Baustoff = b.Id,
                    Dicke = s.Dicke_M,
                    Lambda = b.Lambda,
                    Rho = b.Rho,
                    Cp = b.Cp,
                });
            }
            return m;
        }
    }
}
