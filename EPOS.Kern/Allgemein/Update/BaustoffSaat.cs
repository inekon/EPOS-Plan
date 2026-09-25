using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DES BAUSTOFFKATALOGS - Schemaschritt S-A (BaustoffSchema), Stufe G3.
    //
    // ZWEI TABELLEN, EINE SAAT. Die Normsaat (Ids 1 bis 65) traegt herstellerneutrale Norm-
    // und Richtwerte nach DIN 4108-4:2020-11 und DIN EN ISO 10456:2010-05; die Herstellersaat
    // (Ids 1001 bis 1067) die Bemessungswerte der wichtigsten Hersteller aus ihren
    // Datenblaettern, Leistungserklaerungen und Zulassungen (Recherche 24.09.2026). Beide
    // schreibt BaustoffSchema.SaatSchreiben in EINEM Zug mit ReadOnly = 1 und Herkunft VORGABE;
    // die Ids liegen unter BaustoffSchema.SAAT_ID_GRENZE und bleiben ueber alle Auslieferungen
    // gleich.
    //
    // BEI DEN DAEMMSTOFFEN nennt der Bezeichner der Normsaat den Nennwert lambda_D ("λD 0,035"),
    // die Spalte Lambda traegt den Bemessungswert nach DIN 4108-4, Tab. 2. Die Herstellersaat
    // traegt durchweg den Bemessungswert (lambda_B bzw. lambda_R), den der Hersteller
    // veroeffentlicht; bei XPS gilt er fuer 100 mm, was der Bezeichner nennt.
    //
    // DIE BELEGE stehen je Herstellerzeile als Kommentar daneben (Abruf 24.09.2026); die
    // Bemerkungen der Recherche gehen nicht in die Datenbank. Hersteller- und Produktnamen
    // gehoeren in den KATALOG, nie ins Wiki - der Waechter WikiProduktdatenWacheTests haelt die
    // Wiki-Quellen gegen die Herstellerzeilen der Testdatenbank.
    //
    // NACHTRAEGE. SaatSchreiben ueberschreibt nie und legt nur an, was unter seiner Id (bzw.
    // unter Hersteller und Bezeichner) fehlt. Eine spaeter ergaenzte Zeile erreicht eine
    // bestehende Installation deshalb erst, wenn ein eigener Schemaschritt SaatSchreiben
    // erneut ruft; eine geaenderte Zeile erreicht sie nie von selbst. Die Quellen der Zeilen
    // 1041 und 1066 (Herkunft der Rohdichte aus der Umweltproduktdeklaration, E39) zieht
    // deshalb BaustoffQuellenBerichtigung als eigener Schemaschritt in bestehenden
    // Datenbanken nach.
    // ====================================================================================

    /// <summary>
    /// Eine Zeile der Baustoffsaat — ein Norm-, Richt- oder Herstellerwert mit Quelle. Die Id ist
    /// fest und bleibt über alle Auslieferungen gleich (Muster <see cref="NutzungsdauerSaat"/>).
    /// </summary>
    public sealed class BaustoffSaat
    {
        public BaustoffSaat(int id, string hersteller, string gruppe, string bezeichner, double lambda,
                            double rho, double cp, string quelle)
        {
            Id = id;
            Hersteller = hersteller;
            Gruppe = gruppe;
            Bezeichner = bezeichner;
            Lambda = lambda;
            Rho = rho;
            Cp = cp;
            Quelle = quelle;
        }

        /// <summary>Feste Saat-Id — sie bleibt über alle Auslieferungen gleich.</summary>
        public int Id { get; }

        /// <summary>Hersteller; <c>null</c> = herstellerneutral (Norm- oder Richtwert).</summary>
        public string Hersteller { get; }

        /// <summary>Ordnungsgruppe (Mauerwerk, Beton, Dämmstoffe, …).</summary>
        public string Gruppe { get; }

        /// <summary>Name des Stoffes; bei Normzeilen trägt er die Klasse, bei Herstellerzeilen das Produkt.</summary>
        public string Bezeichner { get; }

        /// <summary>Wärmeleitfähigkeit [W/(m·K)] — der Bemessungswert.</summary>
        public double Lambda { get; }

        /// <summary>Rohdichte [kg/m³].</summary>
        public double Rho { get; }

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)].</summary>
        public double Cp { get; }

        /// <summary>Regelwerk mit Tabelle und Zeile, bzw. Datenblatt mit Stand.</summary>
        public string Quelle { get; }
    }

    /// <summary>
    /// <b>Die zwei Saattabellen des Baustoffkatalogs</b> — Norm und Hersteller. Gelesen wird
    /// sie über <see cref="BaustoffSchema.Saat"/>.
    /// </summary>
    public static class BaustoffSaattabelle
    {
        /// <summary>
        /// Die 65 Normzeilen (Ids 1 bis 65) — herstellerneutral, je mit ihrer Quelle. Kupfer,
        /// Bronze, Messing und Blei fehlen, weil ihre Rohdichte über dem Band des Imports liegt
        /// (Mehrzonenkonzept 3.5).
        /// </summary>
        public static readonly IReadOnlyList<BaustoffSaat> Norm = new[]
        {
            // ---- Putze und Mörtel --------------------------------------------------
            new BaustoffSaat( 1, null, "Putze und Mörtel", "Kalkzementputz", 1.0, 1800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.1.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat( 2, null, "Putze und Mörtel", "Gipsputz 1200", 0.43, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.1.2; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat( 3, null, "Putze und Mörtel", "Leichtputz 1000", 0.38, 1000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.1.4; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat( 4, null, "Putze und Mörtel", "Zementputz", 1.0, 1800.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Estriche ----------------------------------------------------------
            new BaustoffSaat( 5, null, "Estriche", "Zementestrich", 1.4, 2000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat( 6, null, "Estriche", "Calciumsulfatestrich", 1.2, 2100.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.3; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat( 7, null, "Estriche", "Calciumsulfat-Fließestrich", 1.4, 2100.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.4; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat( 8, null, "Estriche", "Gussasphaltestrich", 0.9, 2300.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.1; DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Beton -------------------------------------------------------------
            new BaustoffSaat( 9, null, "Beton", "Normalbeton 2400", 2.0, 2400.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(10, null, "Beton", "Stahlbeton", 2.5, 2400.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(11, null, "Beton", "Stahlbeton 1 % Bewehrung", 2.3, 2300.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(12, null, "Beton", "Leichtbeton 1200", 0.62, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 2.2; DIN EN ISO 10456:2010-05, Tab. 4"),

            // ---- Mauerwerk ---------------------------------------------------------
            new BaustoffSaat(13, null, "Mauerwerk", "Vollziegel 1800", 0.81, 1800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(14, null, "Mauerwerk", "Klinker 2000", 0.96, 2000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(15, null, "Mauerwerk", "Hochlochziegel 1200", 0.5, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(16, null, "Mauerwerk", "Hochlochziegel HLzA/B 800", 0.39, 800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(17, null, "Mauerwerk", "Hochlochziegel HLzW 700", 0.24, 700.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.4; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(18, null, "Mauerwerk", "Kalksandstein 1400", 0.7, 1400.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(19, null, "Mauerwerk", "Kalksandstein 1600", 0.79, 1600.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(20, null, "Mauerwerk", "Kalksandstein 1800", 0.99, 1800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(21, null, "Mauerwerk", "Kalksandstein 2000", 1.1, 2000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(22, null, "Mauerwerk", "Porenbeton 350", 0.11, 350.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(23, null, "Mauerwerk", "Porenbeton 400", 0.13, 400.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(24, null, "Mauerwerk", "Porenbeton 500", 0.16, 500.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(25, null, "Mauerwerk", "Porenbeton 600", 0.19, 600.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(26, null, "Mauerwerk", "Leichtbeton-Hohlblock 800", 0.35, 800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.4.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(27, null, "Mauerwerk", "Sandstein", 2.3, 2600.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(28, null, "Mauerwerk", "Kalkstein mittelhart", 1.4, 2000.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Holz und Holzwerkstoffe -------------------------------------------
            new BaustoffSaat(29, null, "Holz und Holzwerkstoffe", "Nadelholz 500", 0.13, 500.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(30, null, "Holz und Holzwerkstoffe", "Laubholz 700", 0.18, 700.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(31, null, "Holz und Holzwerkstoffe", "Sperrholz 500", 0.13, 500.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(32, null, "Holz und Holzwerkstoffe", "OSB-Platte", 0.13, 650.0, 1700.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(33, null, "Holz und Holzwerkstoffe", "Spanplatte 600", 0.14, 600.0, 1700.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(34, null, "Holz und Holzwerkstoffe", "Holzfaserplatte MDF 800", 0.18, 800.0, 1700.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Dämmstoffe --------------------------------------------------------
            new BaustoffSaat(35, null, "Dämmstoffe", "Mineralwolle λD 0,032", 0.033, 40.0, 1030.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.1, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(36, null, "Dämmstoffe", "Mineralwolle λD 0,035", 0.036, 40.0, 1030.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.1, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(37, null, "Dämmstoffe", "Mineralwolle λD 0,040", 0.041, 40.0, 1030.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.1, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(38, null, "Dämmstoffe", "EPS-Hartschaum λD 0,032", 0.033, 20.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.2, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(39, null, "Dämmstoffe", "EPS-Hartschaum λD 0,035", 0.036, 20.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.2, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(40, null, "Dämmstoffe", "EPS-Hartschaum λD 0,040", 0.041, 20.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.2, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(41, null, "Dämmstoffe", "XPS-Hartschaum λD 0,035", 0.036, 35.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.3, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(42, null, "Dämmstoffe", "PUR-Hartschaum λD 0,023", 0.024, 30.0, 1400.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.4, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(43, null, "Dämmstoffe", "Holzfaserdämmstoff λD 0,040", 0.042, 140.0, 2000.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.10, Fn. b; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(44, null, "Dämmstoffe", "Zellulose λD 0,040", 0.041, 50.0, 1600.0,
                             "DIN 4108-4:2020-11, Tab. 5, Z. 2.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(45, null, "Dämmstoffe", "Schaumglas λD 0,040", 0.041, 120.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.6, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(46, null, "Dämmstoffe", "Perlite-Schüttung λD 0,050", 0.052, 90.0, 900.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.13, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(47, null, "Dämmstoffe", "Holzwolle-Leichtbauplatte λD 0,090", 0.095, 400.0, 1470.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.7.1, Fn. b; DIN EN ISO 10456:2010-05, Tab. 4"),

            // ---- Platten -----------------------------------------------------------
            new BaustoffSaat(48, null, "Platten", "Gipskartonplatte 700", 0.21, 700.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 3.4; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(49, null, "Platten", "Gipsfaserplatte 1200", 0.43, 1200.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(50, null, "Platten", "Zementgebundene Spanplatte", 0.23, 1200.0, 1500.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Bodenbeläge -------------------------------------------------------
            new BaustoffSaat(51, null, "Bodenbeläge", "Keramikfliese", 1.3, 2300.0, 840.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(52, null, "Bodenbeläge", "Parkett", 0.18, 700.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(53, null, "Bodenbeläge", "PVC-Bodenbelag", 0.25, 1700.0, 1400.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(54, null, "Bodenbeläge", "Linoleum", 0.17, 1200.0, 1400.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(55, null, "Bodenbeläge", "Teppichboden", 0.06, 200.0, 1300.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Abdichtungen ------------------------------------------------------
            new BaustoffSaat(56, null, "Abdichtungen", "Bitumenbahn", 0.17, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 7.3.1; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(57, null, "Abdichtungen", "Kunststoffbahn PVC-P", 0.14, 1200.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(58, null, "Abdichtungen", "Elastomerbahn EPDM", 0.25, 1150.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(59, null, "Abdichtungen", "PE-Folie", 0.33, 920.0, 2200.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Metalle und Glas --------------------------------------------------
            new BaustoffSaat(60, null, "Metalle und Glas", "Stahl", 50.0, 7800.0, 450.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(61, null, "Metalle und Glas", "Aluminium", 160.0, 2800.0, 880.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(62, null, "Metalle und Glas", "Zink", 110.0, 7200.0, 380.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(63, null, "Metalle und Glas", "Floatglas", 1.0, 2500.0, 750.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Erdreich ----------------------------------------------------------
            new BaustoffSaat(64, null, "Erdreich", "Erdreich Ton und Schluff", 1.5, 1500.0, 2000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(65, null, "Erdreich", "Erdreich Sand und Kies", 2.0, 2000.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
        };

        /// <summary>
        /// Die 67 Herstellerzeilen (Ids 1001 bis 1067) — Mauerwerk, Dämmstoffe, Platten und
        /// Wärmedämmputze; je mit Quelle (Datenblatt und Stand) und dem Beleg als Kommentar.
        /// </summary>
        public static readonly IReadOnlyList<BaustoffSaat> Hersteller = new[]
        {
            // ---- Mauerwerk ---------------------------------------------------------
            // Beleg: https://www.hplush.de/files/downloads/hplush/porenbeton/produktdatenblatt/FINAL-H+H-Datenblatt-PB-Plansteine-2024.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1001, "H+H", "Mauerwerk", "H+H Planstein PP2-0,35/0,08", 0.08, 350.0, 1000.0,
                             "H+H Deutschland, Technisches Datenblatt PB-Plansteine, Version 1: 07/2024"),
            // Beleg: https://www.hplush.de/files/downloads/hplush/porenbeton/produktdatenblatt/FINAL-H+H-Datenblatt-PB-Plansteine-2024.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1002, "H+H", "Mauerwerk", "H+H Planstein PP4-0,50/0,12", 0.12, 500.0, 1000.0,
                             "H+H Deutschland, Technisches Datenblatt PB-Plansteine, Version 1: 07/2024"),
            // Beleg: https://www.hplush.de/files/downloads/hplush/porenbeton/produktdatenblatt/FINAL-H+H-Datenblatt-PB-Plansteine-2024.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1003, "H+H", "Mauerwerk", "H+H Planstein PP6-0,65/0,18", 0.18, 650.0, 1000.0,
                             "H+H Deutschland, Technisches Datenblatt PB-Plansteine, Version 1: 07/2024"),
            // Beleg: https://www.klb-klimaleichtblock.de/klb-bl%C3%B6cke-kalopor.html (abgerufen 2026-09-24)
            new BaustoffSaat(1004, "KLB Klimaleichtblock", "Mauerwerk", "KLB Kalopor Ultra", 0.07, 350.0, 1000.0,
                             "KLB Klimaleichtblock, Produktseite KLB-Kalopor (Z-17.1-1020)"),
            // Beleg: https://www.klb-klimaleichtblock.de/plan-bl%C3%B6cke-heavy-und-sk.html (abgerufen 2026-09-24)
            new BaustoffSaat(1005, "KLB Klimaleichtblock", "Mauerwerk", "KLB-SK08", 0.08, 400.0, 1000.0,
                             "KLB Klimaleichtblock, Produktseite KLB-Plan-Blöcke Heavy und SK (Z-17.1-1078)"),
            // Beleg: https://www.klb-klimaleichtblock.de/plan-bl%C3%B6cke-sw1.html (abgerufen 2026-09-24)
            new BaustoffSaat(1006, "KLB Klimaleichtblock", "Mauerwerk", "KLB Plan-Block SW1 0,45", 0.1, 450.0, 1000.0,
                             "KLB Klimaleichtblock, Produktseite KLB-Plan-Blöcke SW1 (Z-17.1-730)"),
            // Beleg: https://www.ks-original.de/produkt/ks-l-rp-sfk-12-rdk-14-8-df-t-240 (abgerufen 2026-09-24)
            new BaustoffSaat(1007, "KS-Original", "Mauerwerk", "KS L-R(P) 12-1,4", 0.7, 1400.0, 1000.0,
                             "KS-Original, Produktseite KS L-R(P) SFK 12 RDK 1,4"),
            // Beleg: https://www.ks-original.de/produkt/ks-rp-sfk-12-rdk-18-8-df-t-240 (abgerufen 2026-09-24)
            new BaustoffSaat(1008, "KS-Original", "Mauerwerk", "KS R(P) 12-1,8", 0.99, 1800.0, 1000.0,
                             "KS-Original, Produktseite KS R(P) SFK 12 RDK 1,8"),
            // Beleg: https://www.ks-original.de/produkt/ks-rp-sfk-20-rdk-20-8-df-t-240 (abgerufen 2026-09-24)
            new BaustoffSaat(1009, "KS-Original", "Mauerwerk", "KS R(P) 20-2,0", 1.1, 2000.0, 1000.0,
                             "KS-Original, Produktseite KS R(P) SFK 20 RDK 2,0"),
            // Beleg: https://liapor.com/medien/lia_downloads/datei/12_11_tab_superkplus_waerme.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1010, "Liapor", "Mauerwerk", "Liapor Super-K-Plus 0,45 (LM Ultra)", 0.11, 450.0, 1000.0,
                             "Liapor, Tabelle Wärmedämmung Liapor Super-K-Plus nach Z-17.1-815"),
            // Beleg: https://liapor.com/medien/lia_downloads/datei/12_11_tab_superkplus_waerme.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1011, "Liapor", "Mauerwerk", "Liapor Super-K-Plus 0,60 (LM Ultra)", 0.14, 600.0, 1000.0,
                             "Liapor, Tabelle Wärmedämmung Liapor Super-K-Plus nach Z-17.1-815"),
            // Beleg: https://www.schlagmann.de/media/upload/service/datenblaetter/Planziegel-aussen/T7.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1012, "Schlagmann Poroton", "Mauerwerk", "Schlagmann Poroton-T7", 0.07, 550.0, 1000.0,
                             "Schlagmann Poroton, Datenblatt POROTON-T7 (Z-17.21-1216), Stand 21.07.2026"),
            // Beleg: https://www.schlagmann.de/media/upload/service/datenblaetter/Planziegel-aussen/T8.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1013, "Schlagmann Poroton", "Mauerwerk", "Schlagmann Poroton-T8", 0.08, 600.0, 1000.0,
                             "Schlagmann Poroton, Datenblatt POROTON-T8 (Z-17.21-1238), Stand 01.01.2026"),
            // Beleg: https://www.schlagmann.de/media/upload/service/datenblaetter/Planziegel-aussen/S8.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1014, "Schlagmann Poroton", "Mauerwerk", "Schlagmann Poroton-S8", 0.08, 750.0, 1000.0,
                             "Schlagmann Poroton, Datenblatt POROTON-S8 (Z-17.21-1234), Stand 12.03.2026"),
            // Beleg: https://www.schlagmann.de/media/upload/service/datenblaetter/Planziegel-aussen/S9.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1015, "Schlagmann Poroton", "Mauerwerk", "Schlagmann Poroton-S9", 0.09, 850.0, 1000.0,
                             "Schlagmann Poroton, Datenblatt POROTON-S9 (Z-17.1-1181), Stand 01.01.2026"),
            // Beleg: https://www.schlagmann.de/media/upload/service/datenblaetter/Planziegel-innen/T1-2.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1016, "Schlagmann Poroton", "Mauerwerk", "Schlagmann Poroton-Planziegel-T1,2", 0.5, 1200.0, 1000.0,
                             "Schlagmann Poroton, Datenblatt Planziegel-T1,2 (Z-17.1-868), Stand 01.01.2026"),
            // Beleg: https://unipor.de/produkte/einfamilienhaus/w07-coriso-365 (abgerufen 2026-09-24)
            new BaustoffSaat(1017, "Unipor", "Mauerwerk", "Unipor W07 Coriso", 0.07, 650.0, 1000.0,
                             "Unipor, Produktseite W07 CORISO 36,5 (Z-17.1-1056)"),
            // Beleg: https://unipor.de/produkte/coriso-ziegel/ws075-coriso-365 (abgerufen 2026-09-24)
            new BaustoffSaat(1018, "Unipor", "Mauerwerk", "Unipor WS075 Coriso", 0.075, 800.0, 1000.0,
                             "Unipor, Produktseite WS075 CORISO 36,5 (Z-17.21-1289)"),
            // Beleg: https://unipor.de/produkte/coriso-ziegel/ws08-coriso-365 (abgerufen 2026-09-24)
            new BaustoffSaat(1019, "Unipor", "Mauerwerk", "Unipor WS08 Coriso", 0.08, 700.0, 1000.0,
                             "Unipor, Produktseite WS08 CORISO 36,5 (Z-17.1-1114)"),
            // Beleg: https://www.wienerberger.de/content/dam/wienerberger/germany/marketing/documents-magazines/technical/technical-product-info-sheet/wall/pon-filled/DE_MKT_DOC_TEC_PON_T7-36,5-MW_1060.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1020, "Wienerberger", "Mauerwerk", "Wienerberger Poroton-T7-MW", 0.07, 550.0, 1000.0,
                             "Wienerberger, Technisches Datenblatt Poroton-T7-36,5-MW (Z-17.1-1060), o. D."),
            // Beleg: https://www.wienerberger.de/content/dam/wienerberger/germany/marketing/documents-magazines/technical/technical-product-info-sheet/wall/pon-unfilled/DE_MKT_DOC_TEC_PON_Plan-T8-36,5_1085.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1021, "Wienerberger", "Mauerwerk", "Wienerberger Poroton-Plan-T8", 0.08, 600.0, 1000.0,
                             "Wienerberger, Technisches Datenblatt Poroton-Planziegel-T8-36,5 (Z-17.1-1085), o. D."),
            // Beleg: https://www.wienerberger.de/content/dam/wienerberger/germany/marketing/documents-magazines/technical/technical-product-info-sheet/wall/pon-filled/DE_MKT_DOC_TEC_PON_S10-36,5-MW_1101.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1022, "Wienerberger", "Mauerwerk", "Wienerberger Poroton-S10-MW", 0.1, 800.0, 1000.0,
                             "Wienerberger, Technisches Datenblatt Poroton-S10-36,5-MW (Z-17.1-1101), o. D."),
            // Beleg: https://www.wienerberger.de/content/dam/wienerberger/germany/marketing/documents-magazines/technical/technical-product-info-sheet/wall/pon-unfilled/DE_MKT_DOC_TEC_PON_Plan-T14-30,0_651.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1023, "Wienerberger", "Mauerwerk", "Wienerberger Poroton-Plan-T14", 0.14, 700.0, 1000.0,
                             "Wienerberger, Technisches Datenblatt Poroton-Planziegel-T14-30,0 (Z-17.1-651), o. D."),
            // Beleg: https://technik.xella.de/media/ressources/silka/technische-produktdatenblaetter/Silka-Classic-Planstein-12-18.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1024, "Xella", "Mauerwerk", "Silka Classic Planstein 12-1,8", 0.99, 1800.0, 1000.0,
                             "Xella, Technisches Produktdatenblatt Silka Classic Ratio-Planstein 12-1,8, Stand 08/2026"),
            // Beleg: https://technik.xella.de/media/ressources/silka/technische-produktdatenblaetter/Silka-Solid-XL-Plus-20-20.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1025, "Xella", "Mauerwerk", "Silka Solid XL Plus 20-2,0", 1.1, 2000.0, 1000.0,
                             "Xella, Technisches Produktdatenblatt Silka Solid XL Plus 20-2,0, Stand 08/2026"),
            // Beleg: https://technik.xella.de/media/ressources/ytong/technische-produktdatenblaetter/Ytong-ThermUltra-Planblock-2-030-007.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1026, "Xella", "Mauerwerk", "Ytong ThermUltra PP2-0,30", 0.07, 300.0, 1000.0,
                             "Xella, Technisches Merkblatt Ytong ThermUltra Planblock PP 2-0,30 (0,07), Stand 01/2026"),
            // Beleg: https://storefrontapi.commerce.xella.com/medias/sys_master/root/h28/hd5/8904134164510/Ytong-ThermSuper-Planblock-2-035-008/Ytong-ThermSuper-Planblock-2-035-008.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1027, "Xella", "Mauerwerk", "Ytong ThermSuper PP2-0,35", 0.08, 350.0, 1000.0,
                             "Xella, Technisches Merkblatt Ytong ThermSuper Planblock PP 2-0,35 (0,08), Stand 01/2022"),
            // Beleg: https://technik.xella.de/media/ressources/ytong/technische-produktdatenblaetter/Ytong-ThermStandard-Planblock-2-035-009.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1028, "Xella", "Mauerwerk", "Ytong ThermStandard PP2-0,35", 0.09, 350.0, 1000.0,
                             "Xella, Technisches Merkblatt Ytong ThermStandard Planblock PP 2-0,35 (0,09), Stand 02/2023"),
            // Beleg: https://storefrontapi.commerce.xella.com/medias/sys_master/root/h06/h68/8904137146398/Ytong-ThermStrong-Planblock-4-050-010/Ytong-ThermStrong-Planblock-4-050-010.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1029, "Xella", "Mauerwerk", "Ytong ThermStrong PP4-0,50", 0.1, 500.0, 1000.0,
                             "Xella, Technisches Merkblatt Ytong ThermStrong Planblock PP 4-0,50 (0,10), Stand 01/2022"),

            // ---- Dämmstoffe --------------------------------------------------------
            // Beleg: https://www.austrotherm.de/fileadmin/user_upload/DE/Downloads/Produktdatenblaetter/XPS_2025/2025.12_Austrotherm_XPS_TOP_30_SF_DE_mit_fcd-Wert.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1030, "Austrotherm", "Dämmstoffe", "Austrotherm XPS TOP 30 SF (100 mm)", 0.036, 30.0, 1450.0,
                             "Austrotherm, Produktdatenblatt XPS TOP 30 SF, Stand 12/2025"),
            // Beleg: https://www.styrodur.de/wp-content/uploads/2025/12/db-260611-styrodur-2800c-datenblatt.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1031, "Bachl (Styrodur)", "Dämmstoffe", "Styrodur 2800 C (100 mm)", 0.036, 32.5, 1450.0,
                             "Bachl, Technische Daten Styrodur 2800 C, Stand 02/2026"),
            // Beleg: https://www.styrodur.de/wp-content/uploads/2026/03/db-260611-styrodur-3035cs-datenblatt.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1032, "Bachl (Styrodur)", "Dämmstoffe", "Styrodur 3035 CS (100 mm)", 0.036, 32.5, 1450.0,
                             "Bachl, Technische Daten Styrodur 3035 CS, Stand 06/2026"),
            // Beleg: https://www.foamglas.com/-/media/project/foamglas/public/corporate/foamglascom/files/brochures/technical-data-overview/technische-daten-de.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.foamglas.com/de-de/produkte/fgbt3slabs (abgerufen 2026-09-24)
            new BaustoffSaat(1033, "Foamglas (Owens Corning)", "Dämmstoffe", "Foamglas T3+", 0.037, 95.0, 1000.0,
                             "Foamglas, Technische Daten Foamglas-Platten (DE) und Produktseite T3+"),
            // Beleg: https://www.foamglas.com/-/media/project/foamglas/public/corporate/foamglascom/files/brochures/technical-data-overview/technische-daten-de.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.foamglas.com/de-de/produkte/fgbt4slabs (abgerufen 2026-09-24)
            new BaustoffSaat(1034, "Foamglas (Owens Corning)", "Dämmstoffe", "Foamglas T4+", 0.042, 110.0, 1000.0,
                             "Foamglas, Technische Daten Foamglas-Platten (DE) und Produktseite T4+"),
            // Beleg: https://www.foamglas.com/-/media/project/foamglas/public/corporate/foamglascom/files/brochures/technical-data-overview/technische-daten-de.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.foamglas.com/de-de/produkte/fgbs3slabs (abgerufen 2026-09-24)
            new BaustoffSaat(1035, "Foamglas (Owens Corning)", "Dämmstoffe", "Foamglas S3", 0.046, 123.0, 1000.0,
                             "Foamglas, Technische Daten Foamglas-Platten (DE) und Produktseite S3"),
            // Beleg: https://www.foamglas.com/-/media/project/foamglas/public/corporate/foamglascom/files/brochures/technical-data-overview/technische-daten-de.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.foamglas.com/de-de/produkte/fgbfslabs (abgerufen 2026-09-24)
            new BaustoffSaat(1036, "Foamglas (Owens Corning)", "Dämmstoffe", "Foamglas F", 0.052, 165.0, 1000.0,
                             "Foamglas, Technische Daten Foamglas-Platten (DE) und Produktseite F"),
            // Beleg: https://www.gutex.de/Downloads/PDF/Technisches%20Datenblatt/Gutex_Thermoflex_TDB_de.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1037, "Gutex", "Dämmstoffe", "Gutex Thermoflex", 0.038, 50.0, 2100.0,
                             "Gutex, Technisches Datenblatt Thermoflex, Stand 2026-09"),
            // Beleg: https://www.gutex.de/Downloads/PDF/Technisches%20Datenblatt/Gutex_Thermowall_TDB_de.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1038, "Gutex", "Dämmstoffe", "Gutex Thermowall", 0.042, 160.0, 2100.0,
                             "Gutex, Technisches Datenblatt Thermowall (Z-33.47-660), Stand 2026-09"),
            // Beleg: https://www.gutex.de/Downloads/PDF/Technisches%20Datenblatt/Gutex_Ultratherm_TDB_de.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1039, "Gutex", "Dämmstoffe", "Gutex Ultratherm", 0.044, 180.0, 2100.0,
                             "Gutex, Technisches Datenblatt Ultratherm, Stand 2025-12"),
            // Beleg: https://www.gutex.de/Downloads/PDF/Technisches%20Datenblatt/Gutex_Multiplex-top_TDB_de.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1040, "Gutex", "Dämmstoffe", "Gutex Multiplex-top", 0.047, 220.0, 2100.0,
                             "Gutex, Technisches Datenblatt Multiplex-top, Stand 2026-09"),
            // Beleg: https://www.kingspan.com/content/dam/kingspan/kil/products/kooltherm-k5-kice/kingspan-kooltherm-k5-product-leaflet-de-de.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.kingspan.com/content/dam/kingspan/kil/products/kooltherm-k5-kice/kingspan-kooltherm-k5-fdes-120mm.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1041, "Kingspan Insulation", "Dämmstoffe", "Kingspan Kooltherm K5", 0.022, 35.0, 1400.0,
                             "Kingspan, Produktblatt Kooltherm K5 WDVS-Dämmplatte (DE), Version 15, 07/2026; Rohdichte aus FDES 120 mm"),
            // Beleg: https://knauf.com/de-DE/p/produkt/klemmplatte-kp-035-hb-21511_4314 (abgerufen 2026-09-24)
            new BaustoffSaat(1042, "Knauf Insulation", "Dämmstoffe", "Knauf Insulation Klemmplatte KP-035/HB", 0.035, 50.0, 1030.0,
                             "Knauf Insulation, Produktseite Klemmplatte KP-035/HB (Steinwolle), Datenblatt 09/2026"),
            // Beleg: https://knauf.com/de-DE/p/produkt/feuerschutz-daemmplatte-dpf-100-21525_4314 (abgerufen 2026-09-24)
            new BaustoffSaat(1043, "Knauf Insulation", "Dämmstoffe", "Knauf Insulation Feuerschutz-Dämmplatte DPF-100", 0.035, 100.0, 1030.0,
                             "Knauf Insulation, Produktseite Feuerschutz-Dämmplatte DPF-100 (Steinwolle)"),
            // Beleg: https://www.rockwool.com/siteassets/rw-d/datenblatter/schragdach/db-klemmrock-035-rockwool.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.rockwool.com/siteassets/rw-d/nachhaltigkeit-und-gebaudezertifizierungen/umwelt-produktdeklarationen-epd/wu-epd-low-density-rockwool.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1044, "Rockwool", "Dämmstoffe", "Rockwool Klemmrock 035", 0.035, 38.0, 1030.0,
                             "Rockwool, Technisches Datenblatt Klemmrock 035, Ausgabe 04/2025; EPD niedriger Rohdichtebereich"),
            // Beleg: https://www.rockwool.com/siteassets/rw-d/datenblatter/fassade-wdvs/db-coverrock-x-2-rockwool.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.rockwool.com/siteassets/rw-d/nachhaltigkeit-und-gebaudezertifizierungen/umwelt-produktdeklarationen-epd/wu-umwelt-produktdeklaration-epd-mittlere-rd-rockwool.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1045, "Rockwool", "Dämmstoffe", "Rockwool Coverrock X-2", 0.035, 99.0, 1030.0,
                             "Rockwool, Technisches Datenblatt Putzträgerplatte Coverrock X-2, Ausgabe 08/2026; EPD mittlerer Rohdichtebereich"),
            // Beleg: https://www.rockwool.com/siteassets/rw-d/datenblatter/flachdach/db-hardrock-040-rockwool.pdf (abgerufen 2026-09-24)
            // Beleg: https://www.rockwool.com/siteassets/rw-d/nachhaltigkeit-und-gebaudezertifizierungen/umwelt-produktdeklarationen-epd/wu-epd-high-density-rockwool.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1046, "Rockwool", "Dämmstoffe", "Rockwool Hardrock 040", 0.04, 159.0, 1030.0,
                             "Rockwool, Technisches Datenblatt Dachdämmplatte Hardrock 040, Ausgabe 04/2026; EPD hoher Rohdichtebereich"),
            // Beleg: https://www.steico.com/fileadmin/user_upload/importer/downloads/produktinformationen_holzfaser-dmmstoffe/STEICO_technical-data-sheet_flex-036_DEU-AUT-CHE_de_i.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1047, "Steico", "Dämmstoffe", "Steico flex 036", 0.038, 50.0, 2100.0,
                             "Steico, Technisches Merkblatt STEICOflex 036 (DEU/AUT/CHE), 2026-09-21, Version 8"),
            // Beleg: https://www.steico.com/fileadmin/user_upload/importer/downloads/produktinformationen_holzfaser-dmmstoffe/STEICO_technical-data-sheet_therm-dry_DEU-AUT-CHE_de_i.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1048, "Steico", "Dämmstoffe", "Steico therm dry", 0.039, 110.0, 2100.0,
                             "Steico, Technisches Merkblatt STEICOtherm dry (DEU/AUT/CHE), 2026-09-21, Version 6"),
            // Beleg: https://www.steico.com/fileadmin/user_upload/importer/downloads/produktinformationen_holzfaser-dmmstoffe/STEICO_technical-data-sheet_special-dry_DEU-AUT-CHE_de_i.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1049, "Steico", "Dämmstoffe", "Steico special dry", 0.042, 140.0, 2100.0,
                             "Steico, Technisches Merkblatt STEICOspecial dry (DEU/AUT/CHE), 2026-09-21, Version 7"),
            // Beleg: https://www.sto.de/webdocs/productdocuments/TechnicalDataSheet_Sto-Polystyrol-Hartschaumplatte_PS15SE_034_0101_DE_08_00.PDF (abgerufen 2026-09-24)
            new BaustoffSaat(1050, "Sto", "Dämmstoffe", "Sto-Polystyrol-Hartschaumplatte PS15SE 034", 0.034, 15.0, 1450.0,
                             "Sto, Technisches Merkblatt Sto-Polystyrol-Hartschaumplatte PS15SE 034, Rev. 8, 10.10.2024"),
            // Beleg: https://www.sto.de/webdocs/productdocuments/TechnicalDataSheet_Sto-Sockelplatte_PS30SE_032_0101_DE_09_00.PDF (abgerufen 2026-09-24)
            new BaustoffSaat(1051, "Sto", "Dämmstoffe", "Sto-Sockelplatte PS30SE 032", 0.032, 30.0, 1450.0,
                             "Sto, Technisches Merkblatt Sto-Sockelplatte PS30SE 032, Rev. 9, 13.01.2023"),
            // Beleg: https://www.ursa.de/siteassets/ursa-content/pdf/pl-produktkatalog.pdf?v=4ad6a4 (abgerufen 2026-09-24)
            new BaustoffSaat(1052, "URSA", "Dämmstoffe", "URSA PUREFLOC Frame (raumausfüllend)", 0.035, 35.0, 1030.0,
                             "URSA, Produktkatalog Wärmedämmstoffe, Stand Januar 2026, URSA PUREFLOC Frame (ETA-18/0889)"),
            // Beleg: https://storefrontapi.commerce.xella.com/medias/sys_master/root/h0a/hbc/8903257030686/multipor-produktdatenblatt-mineraldaemmplatte/multipor-produktdatenblatt-mineraldaemmplatte.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1053, "Xella", "Dämmstoffe", "Multipor Mineraldämmplatte (WTR/WI)", 0.042, 90.0, 1300.0,
                             "Xella, Technisches Merkblatt Multipor Mineraldämmplatte (ETA-05/0093), Stand 12/2021"),

            // ---- Platten -----------------------------------------------------------
            // Beleg: https://binary.egger.link/pis/dcac4dc2-808c-4c85-a04f-391e93a487e8/EGGER-OSB-DOP-734-04-de (abgerufen 2026-09-24)
            new BaustoffSaat(1054, "Egger", "Platten", "Egger OSB 3", 0.13, 600.0, 1700.0,
                             "Egger, Leistungserklärung EGGER OSB 3 (DOP-734-04)"),
            // Beleg: https://binary.egger.link/pis/120cb779-e714-458a-844c-4bf7dd83364d/Leistungserklaerung-DHF-DOP-506 (abgerufen 2026-09-24)
            new BaustoffSaat(1055, "Egger", "Platten", "Egger DHF", 0.1, 615.0, 1700.0,
                             "Egger, Leistungserklärung EGGER DHF (DOP-506-04)"),
            // Beleg: https://www.fermacell.com/de-de/produkte/wand/gipsfaserplatten?texture=standard (abgerufen 2026-09-24)
            new BaustoffSaat(1056, "Fermacell", "Platten", "Fermacell Gipsfaser-Platte", 0.32, 1150.0, 1100.0,
                             "Fermacell (James Hardie), Produktseite Gipsfaserplatte"),
            // Beleg: https://www.fermacell.com/de-de/produkte/wand/powerpanel-h2o (abgerufen 2026-09-24)
            new BaustoffSaat(1057, "Fermacell", "Platten", "Fermacell Powerpanel H2O", 0.17, 1000.0, 1000.0,
                             "Fermacell (James Hardie), Produktseite Powerpanel H2O (ETA-07/0087)"),
            // Beleg: https://www.fermacell.com/de-de/produkte/wand/firepanel-a1-brandschutzplatte (abgerufen 2026-09-24)
            new BaustoffSaat(1058, "Fermacell", "Platten", "Fermacell Firepanel A1", 0.38, 1200.0, 1000.0,
                             "Fermacell (James Hardie), Produktseite Firepanel A1"),
            // Beleg: https://downloads.knauf.de/wmv/?id=2950 (abgerufen 2026-09-24)
            new BaustoffSaat(1059, "Knauf", "Platten", "Knauf Ausbauplatte GKB", 0.21, 680.0, 1000.0,
                             "Knauf Gips, Technisches Blatt Ausbauplatte GKB K7109_DSP.de, 09/2025"),
            // Beleg: https://downloads.knauf.de/wmv/?id=3052 (abgerufen 2026-09-24)
            new BaustoffSaat(1060, "Knauf", "Platten", "Knauf Diamant GKFI", 0.27, 1000.0, 1000.0,
                             "Knauf Gips, Technisches Blatt Diamant GKFI K7110_DSP.de, 10/2025"),
            // Beleg: https://rigips.de/Dokumente/produktdatenblatt/produktdatenblatt-rigips-bauplatte-rb-12-5.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1061, "Rigips", "Platten", "Rigips Bauplatte RB 12,5", 0.25, 680.0, 960.0,
                             "Saint-Gobain Rigips, Produktdatenblatt Rigips Bauplatte RB 12,5, Stand 02.07.26"),
            // Beleg: https://www.rigips.de/Dokumente/produktdatenblatt/produktdatenblatt-rigips-feuerschutzplatte-rf-15.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1062, "Rigips", "Platten", "Rigips Feuerschutzplatte RF 15", 0.25, 800.0, 960.0,
                             "Saint-Gobain Rigips, Produktdatenblatt Rigips Feuerschutzplatte RF 15, Stand 21.03.22"),
            // Beleg: https://www.rigips.de/Dokumente/produktdatenblatt/produktdatenblatt-rigips-habito-12-5.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1063, "Rigips", "Platten", "Rigips Habito 12,5", 0.25, 975.0, 960.0,
                             "Saint-Gobain Rigips, Produktdatenblatt Rigips Habito 12,5, Stand 08.07.26"),
            // Beleg: https://www.rigips.de/Dokumente/produktdatenblatter-pdb/produktdatenblatt-rigidur-h-18-0.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1064, "Rigips", "Platten", "Rigidur H 18", 0.35, 1200.0, 1000.0,
                             "Saint-Gobain Rigips, Produktdatenblatt Rigidur H 18,0, Stand 10.04.26"),
            // Beleg: https://www.swisskrono.com/fileadmin/esign/OSB_Produktdatenblatt_DE_349333.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1065, "Swiss Krono", "Platten", "Swiss Krono OSB/3 EN300", 0.13, 600.0, 1700.0,
                             "Swiss Krono, Produktdatenblatt OSB/3 EN300 – Charakteristische Werte nach DIN EN 13986, Stand 06/2019"),

            // ---- Putze und Mörtel --------------------------------------------------
            // Beleg: https://baumit.de/files/de/pdf_files/pds_thermoextra__dmmputz_dp_85_bde_de_58265.pdf (abgerufen 2026-09-24)
            // Beleg: https://baumit.de/files/de/pdf_files/epd-vdp-20230402-ibo1-de.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1066, "Baumit", "Putze und Mörtel", "Baumit DämmPutz DP 85", 0.07, 230.0, 1000.0,
                             "Baumit, Produktdatenblatt DämmPutz DP 85, 18.09.2025; Rohdichte Mindestwert A2-s1,d0 nach VDPM-EPD"),
            // Beleg: https://baumit.de/files/de/pdf_files/pds_nhl_thermo___nhl_thermoputzbde_de_57140.pdf (abgerufen 2026-09-24)
            new BaustoffSaat(1067, "Baumit", "Putze und Mörtel", "Baumit NHL Thermo", 0.08, 400.0, 1000.0,
                             "Baumit, Produktdatenblatt NHL Thermo, 08.11.2025"),
        };
    }
}
