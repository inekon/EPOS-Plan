using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DER SYNONYMTABELLE - Schemaschritt des Namensabgleichs (BaustoffabgleichSchema).
    //
    // WAS HIER STEHT. Je Zeile ein Materialname, wie ihn Autorensysteme und Planer schreiben, bereits
    // NORMALISIERT nach N1/N2 (Kleinschreibung, Umlaute aufgeloest, Trennzeichen als Leerzeichen, ohne
    // Marken - der Nachweis haelt jede Zeile gegen Baustoffabgleich.Normalisieren), seine Sprache, der
    // herstellerneutrale Baustoff der Baustoffsaat (Ids 1 bis 65) und die Quelle. Feste Ids 1 bis n;
    // ReadOnly = 1. Kein Name steht zweimal (der eindeutige Index verlangte es), und keiner gleicht
    // dem Bezeichner eines Katalogbaustoffs (der traefe schon mit N3).
    //
    // HERKUNFT DER NAMEN. (1) Die gemessenen Namen des Befunds P, § 3.6 (Archicad- und Revit-Exporte:
    // Leichtbeton, Stahlbeton, Kalksandstein, Holz, Ortbeton - bewehrt, Mauerwerk - Naturstein) und
    // die dort vorgesehenen Synonyme (reinforced concrete, brick, insulation); (2) die Materialnamen
    // der Importproben des Repositoriums; (3) gaengige Namen der deutschen und englischen Vorlagen der
    // Autorensysteme und die Fachbegriffe nach DIN 4108-4 bzw. DIN EN ISO 10456 fuer die 65
    // herstellerneutralen Stoffe. KEINE Hersteller- oder Markennamen; Namen, die ein Produkt oder eine
    // Marke meinen, gehoeren nicht hierher (die Herstellerzeilen des Katalogs trifft N3 nur beim
    // genauen Namen).
    //
    // WAHL DES VERTRETERS. Wo ein allgemeiner Name eine Stoffreihe meint, zeigt er auf deren mittlere
    // Stufe: Daemmung/insulation -> Mineralwolle lambdaD 0,035 (Befund P), Kalksandstein -> 1800,
    // Porenbeton -> 500, Hochlochziegel -> 1200, Ziegel/brick -> Vollziegel 1800 (Befund P),
    // Naturstein -> Sandstein, Erdreich -> Sand und Kies (lambda 2,0 wie die Vorgabe der
    // DIN EN ISO 13370). Nennt der Name eine Rohdichte (KS 1400), waehlt der Abgleich die Stufe selbst.
    // Der Anwender kann jeden Treffer mit einer eigenen Zuordnung (N7) ueberstimmen.
    //
    // NACHTRAEGE. SaatSchreiben ueberschreibt nie und legt nur an, was unter seiner Id (bzw. seinem
    // Materialnamen) fehlt; eine spaeter ergaenzte Zeile erreicht eine bestehende Installation erst
    // mit einem eigenen Schemaschritt, der SaatSchreiben erneut ruft.
    // ====================================================================================

    /// <summary>Eine Zeile der Synonymsaat — Materialname (normalisiert), Sprache, Katalogbaustoff, Quelle.</summary>
    public sealed class BaustoffsynonymSaat
    {
        public BaustoffsynonymSaat(int id, string materialname, string sprache, int idBaustoff, string quelle)
        {
            Id = id;
            Materialname = materialname;
            Sprache = sprache;
            IdBaustoff = idBaustoff;
            Quelle = quelle;
        }

        /// <summary>Feste Saat-Id — sie bleibt über alle Auslieferungen gleich.</summary>
        public int Id { get; }

        /// <summary>Der Materialname, normalisiert nach N1/N2.</summary>
        public string Materialname { get; }

        /// <summary>Sprache (<c>de</c>, <c>en</c>).</summary>
        public string Sprache { get; }

        /// <summary>Der herstellerneutrale Baustoff der Baustoffsaat (<c>Tab_Baustoff_STAMM.ID</c>).</summary>
        public int IdBaustoff { get; }

        /// <summary>Woher der Name stammt.</summary>
        public string Quelle { get; }
    }

    /// <summary>Die Synonymsaat der Auslieferung — die Tabelle selbst.</summary>
    public static class BaustoffsynonymSaattabelle
    {
        private const string DE = "de";
        private const string EN = "en";

        private const string BEFUND = "Befund P, § 3.6: Materialname einer Messdatei";
        private const string BEFUND_PLAN = "Befund P, § 3.6: vorgesehenes Synonym";
        private const string PROBE = "Materialname einer Importprobe des Repositoriums";
        private const string VORLAGE_DE = "Materialname einer Autorenvorlage (deutsch)";
        private const string VORLAGE_EN = "Materialname einer Autorenvorlage (englisch)";
        private const string FACH_DE = "Fachbegriff nach DIN 4108-4";
        private const string FACH_EN = "Fachbegriff nach DIN EN ISO 10456 (englisch)";

        private static BaustoffsynonymSaat S(int id, string name, string sprache, int baustoff, string quelle)
            => new BaustoffsynonymSaat(id, name, sprache, baustoff, quelle);

        /// <summary>Alle Synonyme in Id-Reihenfolge.</summary>
        public static readonly IReadOnlyList<BaustoffsynonymSaat> Alle = new[]
        {
            // ---- Putze und Mörtel (1 Kalkzementputz, 2 Gipsputz 1200, 3 Leichtputz 1000, 4 Zementputz)
            S(1, "putz", DE, 1, PROBE),
            S(2, "aussenputz", DE, 1, PROBE),
            S(3, "innenputz", DE, 2, PROBE),
            S(4, "kalkputz", DE, 1, FACH_DE),
            S(5, "kalk zementputz", DE, 1, VORLAGE_DE),
            S(6, "putz kalk zement", DE, 1, VORLAGE_DE),
            S(7, "mineralputz", DE, 1, FACH_DE),
            S(8, "kalkgipsputz", DE, 2, FACH_DE),
            S(9, "plaster", EN, 2, VORLAGE_EN),
            S(10, "gypsum plaster", EN, 2, FACH_EN),
            S(11, "lime plaster", EN, 1, FACH_EN),
            S(12, "cement plaster", EN, 4, FACH_EN),
            S(13, "render", EN, 1, FACH_EN),
            S(14, "stucco", EN, 1, VORLAGE_EN),
            S(15, "lightweight plaster", EN, 3, FACH_EN),

            // ---- Estriche (5 Zementestrich, 6 Calciumsulfatestrich, 7 CaSO4-Fließestrich, 8 Gussasphaltestrich)
            S(16, "estrich", DE, 5, PROBE),
            S(17, "anhydritestrich", DE, 6, FACH_DE),
            S(18, "fliessestrich", DE, 7, FACH_DE),
            S(19, "gussasphalt", DE, 8, FACH_DE),
            S(20, "screed", EN, 5, VORLAGE_EN),
            S(21, "cement screed", EN, 5, FACH_EN),
            S(22, "anhydrite screed", EN, 6, FACH_EN),
            S(23, "mastic asphalt", EN, 8, FACH_EN),

            // ---- Beton (9 Normalbeton 2400, 10 Stahlbeton, 12 Leichtbeton 1200)
            S(24, "beton", DE, 9, PROBE),
            S(25, "ortbeton", DE, 10, BEFUND),
            S(26, "beton ortbeton", DE, 10, VORLAGE_DE),
            S(27, "betonfertigteil", DE, 10, FACH_DE),
            S(28, "fertigteilbeton", DE, 10, FACH_DE),
            S(29, "sichtbeton", DE, 10, FACH_DE),
            S(30, "leichtbeton", DE, 12, BEFUND),
            S(31, "beton unbewehrt", DE, 9, FACH_DE),
            S(32, "unbewehrter beton", DE, 9, FACH_DE),
            S(33, "concrete", EN, 9, VORLAGE_EN),
            S(34, "reinforced concrete", EN, 10, BEFUND_PLAN),
            S(35, "concrete cast in place", EN, 10, VORLAGE_EN),
            S(36, "cast in place concrete", EN, 10, VORLAGE_EN),
            S(37, "precast concrete", EN, 10, FACH_EN),
            S(38, "concrete precast", EN, 10, VORLAGE_EN),
            S(39, "lightweight concrete", EN, 12, FACH_EN),
            S(40, "plain concrete", EN, 9, FACH_EN),

            // ---- Mauerwerk (13 Vollziegel 1800, 14 Klinker 2000, 15 Hochlochziegel 1200, 20 Kalksandstein 1800,
            //      24 Porenbeton 500, 26 Leichtbeton-Hohlblock 800, 27 Sandstein, 28 Kalkstein mittelhart)
            S(41, "ziegel", DE, 13, FACH_DE),
            S(42, "mauerziegel", DE, 13, FACH_DE),
            S(43, "ziegelmauerwerk", DE, 13, FACH_DE),
            S(44, "mauerwerk ziegel", DE, 13, VORLAGE_DE),
            S(45, "hochlochziegel", DE, 15, FACH_DE),
            S(46, "hlz", DE, 15, FACH_DE),
            S(47, "verblender", DE, 14, FACH_DE),
            S(48, "vormauerziegel", DE, 14, FACH_DE),
            S(49, "kalksandstein", DE, 20, BEFUND),
            S(50, "ks", DE, 20, FACH_DE),
            S(51, "ks mauerwerk", DE, 20, FACH_DE),
            S(52, "kalksandsteinmauerwerk", DE, 20, FACH_DE),
            S(53, "mauerwerk kalksandstein", DE, 20, VORLAGE_DE),
            S(54, "porenbeton", DE, 24, FACH_DE),
            S(55, "gasbeton", DE, 24, FACH_DE),
            S(56, "porenbetonstein", DE, 24, FACH_DE),
            S(57, "mauerwerk porenbeton", DE, 24, VORLAGE_DE),
            S(58, "hohlblockstein", DE, 26, FACH_DE),
            S(59, "bims", DE, 26, FACH_DE),
            S(60, "naturstein", DE, 27, BEFUND),
            S(61, "mauerwerk naturstein", DE, 27, BEFUND),
            S(62, "kalkstein", DE, 28, FACH_DE),
            S(63, "brick", EN, 13, BEFUND_PLAN),
            S(64, "masonry brick", EN, 13, VORLAGE_EN),
            S(65, "common brick", EN, 13, FACH_EN),
            S(66, "face brick", EN, 14, FACH_EN),
            S(67, "clinker brick", EN, 14, FACH_EN),
            S(68, "calcium silicate brick", EN, 20, FACH_EN),
            S(69, "sand lime brick", EN, 20, FACH_EN),
            S(70, "aerated concrete", EN, 24, FACH_EN),
            S(71, "autoclaved aerated concrete", EN, 24, FACH_EN),
            S(72, "aac", EN, 24, FACH_EN),
            S(73, "lightweight concrete block", EN, 26, FACH_EN),
            S(74, "natural stone", EN, 27, FACH_EN),
            S(75, "sandstone", EN, 27, FACH_EN),
            S(76, "limestone", EN, 28, FACH_EN),

            // ---- Holz und Holzwerkstoffe (29 Nadelholz 500, 30 Laubholz 700, 31 Sperrholz 500, 32 OSB-Platte,
            //      33 Spanplatte 600, 34 Holzfaserplatte MDF 800)
            S(77, "holz", DE, 29, BEFUND),
            S(78, "weichholz", DE, 29, FACH_DE),
            S(79, "hartholz", DE, 30, FACH_DE),
            S(80, "holz weichholz", DE, 29, VORLAGE_DE),
            S(81, "holz hartholz", DE, 30, VORLAGE_DE),
            S(82, "konstruktionsvollholz", DE, 29, FACH_DE),
            S(83, "kvh", DE, 29, FACH_DE),
            S(84, "brettschichtholz", DE, 29, FACH_DE),
            S(85, "bsh", DE, 29, FACH_DE),
            S(86, "holzschalung", DE, 29, FACH_DE),
            S(87, "osb", DE, 32, FACH_DE),
            S(88, "spanplatte", DE, 33, FACH_DE),
            S(89, "mdf", DE, 34, FACH_DE),
            S(90, "wood", EN, 29, VORLAGE_EN),
            S(91, "timber", EN, 29, FACH_EN),
            S(92, "softwood", EN, 29, FACH_EN),
            S(93, "hardwood", EN, 30, FACH_EN),
            S(94, "plywood", EN, 31, FACH_EN),
            S(95, "wood sheathing plywood", EN, 31, VORLAGE_EN),
            S(96, "oriented strand board", EN, 32, FACH_EN),
            S(97, "particle board", EN, 33, FACH_EN),
            S(98, "particleboard", EN, 33, FACH_EN),
            S(99, "chipboard", EN, 33, FACH_EN),
            S(100, "fibreboard", EN, 34, FACH_EN),
            S(101, "fiberboard", EN, 34, FACH_EN),

            // ---- Dämmstoffe (36 Mineralwolle λD 0,035, 39 EPS λD 0,035, 41 XPS, 42 PUR, 43 Holzfaser,
            //      44 Zellulose, 45 Schaumglas, 46 Perlite, 47 Holzwolle-Leichtbauplatte)
            S(102, "daemmung", DE, 36, BEFUND_PLAN),
            S(103, "waermedaemmung", DE, 36, FACH_DE),
            S(104, "innendaemmung", DE, 36, PROBE),
            S(105, "dachdaemmung", DE, 36, PROBE),
            S(106, "kellerdeckendaemmung", DE, 36, PROBE),
            S(107, "aussendaemmung", DE, 36, FACH_DE),
            S(108, "trittschalldaemmung", DE, 36, FACH_DE),
            S(109, "mineralwolle", DE, 36, FACH_DE),
            S(110, "steinwolle", DE, 36, FACH_DE),
            S(111, "glaswolle", DE, 36, FACH_DE),
            S(112, "mineralfaser", DE, 36, FACH_DE),
            S(113, "mineralfaserdaemmung", DE, 36, FACH_DE),
            S(114, "daemmung mineralwolle", DE, 36, VORLAGE_DE),
            S(115, "eps", DE, 39, FACH_DE),
            S(116, "polystyrol", DE, 39, FACH_DE),
            S(117, "eps hartschaum", DE, 39, FACH_DE),
            S(118, "daemmung eps", DE, 39, VORLAGE_DE),
            S(119, "xps", DE, 41, FACH_DE),
            S(120, "perimeterdaemmung", DE, 41, FACH_DE),
            S(121, "pur", DE, 42, FACH_DE),
            S(122, "pir", DE, 42, FACH_DE),
            S(123, "polyurethan", DE, 42, FACH_DE),
            S(124, "holzfaser", DE, 43, FACH_DE),
            S(125, "holzfaserdaemmung", DE, 43, FACH_DE),
            S(126, "zellulose", DE, 44, FACH_DE),
            S(127, "zellulosedaemmung", DE, 44, FACH_DE),
            S(128, "schaumglas", DE, 45, FACH_DE),
            S(129, "perlite", DE, 46, FACH_DE),
            S(130, "holzwolle", DE, 47, FACH_DE),
            S(131, "holzwolleplatte", DE, 47, FACH_DE),
            S(132, "insulation", EN, 36, BEFUND_PLAN),
            S(133, "thermal insulation", EN, 36, FACH_EN),
            S(134, "mineral wool", EN, 36, FACH_EN),
            S(135, "stone wool", EN, 36, FACH_EN),
            S(136, "rock wool", EN, 36, FACH_EN),
            S(137, "glass wool", EN, 36, FACH_EN),
            S(138, "batt insulation", EN, 36, VORLAGE_EN),
            S(139, "insulation batt", EN, 36, VORLAGE_EN),
            S(140, "rigid insulation", EN, 39, VORLAGE_EN),
            S(141, "insulation thermal barriers rigid insulation", EN, 39, VORLAGE_EN),
            S(142, "insulation thermal barriers semi rigid insulation", EN, 36, VORLAGE_EN),
            S(143, "expanded polystyrene", EN, 39, FACH_EN),
            S(144, "extruded polystyrene", EN, 41, FACH_EN),
            S(145, "polyurethane", EN, 42, FACH_EN),
            S(146, "polyisocyanurate", EN, 42, FACH_EN),
            S(147, "wood fibre insulation", EN, 43, FACH_EN),
            S(148, "wood fiber insulation", EN, 43, FACH_EN),
            S(149, "cellulose", EN, 44, FACH_EN),
            S(150, "cellular glass", EN, 45, FACH_EN),
            S(151, "foam glass", EN, 45, FACH_EN),
            S(152, "wood wool", EN, 47, FACH_EN),

            // ---- Platten (48 Gipskartonplatte 700, 49 Gipsfaserplatte 1200, 50 Zementgebundene Spanplatte)
            S(153, "gipskarton", DE, 48, FACH_DE),
            S(154, "gipskartonplatte", DE, 48, FACH_DE),
            S(155, "gkb", DE, 48, FACH_DE),
            S(156, "gipsfaser", DE, 49, FACH_DE),
            S(157, "gipsfaserplatte", DE, 49, FACH_DE),
            S(158, "gypsum board", EN, 48, FACH_EN),
            S(159, "gypsum wall board", EN, 48, VORLAGE_EN),
            S(160, "plasterboard", EN, 48, FACH_EN),
            S(161, "drywall", EN, 48, FACH_EN),
            S(162, "gypsum fibre board", EN, 49, FACH_EN),
            S(163, "gypsum fiber board", EN, 49, FACH_EN),
            S(164, "cement bonded particle board", EN, 50, FACH_EN),

            // ---- Bodenbeläge (51 Keramikfliese, 52 Parkett, 53 PVC-Bodenbelag, 55 Teppichboden)
            S(165, "fliese", DE, 51, FACH_DE),
            S(166, "fliesen", DE, 51, FACH_DE),
            S(167, "keramik", DE, 51, FACH_DE),
            S(168, "pvc", DE, 53, FACH_DE),
            S(169, "vinyl", DE, 53, FACH_DE),
            S(170, "teppich", DE, 55, FACH_DE),
            S(171, "ceramic tile", EN, 51, FACH_EN),
            S(172, "tile", EN, 51, FACH_EN),
            S(173, "tiles", EN, 51, FACH_EN),
            S(174, "parquet", EN, 52, FACH_EN),
            S(175, "vinyl flooring", EN, 53, FACH_EN),
            S(176, "carpet", EN, 55, FACH_EN),

            // ---- Abdichtungen (56 Bitumenbahn, 57 Kunststoffbahn PVC-P, 58 Elastomerbahn EPDM, 59 PE-Folie)
            S(177, "bitumen", DE, 56, FACH_DE),
            S(178, "abdichtung", DE, 56, FACH_DE),
            S(179, "abdichtung bitumen", DE, 56, VORLAGE_DE),
            S(180, "dachabdichtung", DE, 56, FACH_DE),
            S(181, "dachbahn", DE, 56, FACH_DE),
            S(182, "kunststoffbahn", DE, 57, FACH_DE),
            S(183, "epdm", DE, 58, FACH_DE),
            S(184, "folie", DE, 59, FACH_DE),
            S(185, "polyethylenfolie", DE, 59, FACH_DE),
            S(186, "dampfsperre", DE, 59, FACH_DE),
            S(187, "dampfbremse", DE, 59, FACH_DE),
            S(188, "bituminous membrane", EN, 56, FACH_EN),
            S(189, "roofing felt", EN, 56, FACH_EN),
            S(190, "pvc membrane", EN, 57, FACH_EN),
            S(191, "vapor retarder", EN, 59, VORLAGE_EN),
            S(192, "vapour retarder", EN, 59, FACH_EN),
            S(193, "vapor barrier", EN, 59, FACH_EN),
            S(194, "vapour barrier", EN, 59, FACH_EN),

            // ---- Metalle und Glas (60 Stahl, 61 Aluminium, 62 Zink, 63 Floatglas)
            S(195, "baustahl", DE, 60, FACH_DE),
            S(196, "alu", DE, 61, FACH_DE),
            S(197, "titanzink", DE, 62, FACH_DE),
            S(198, "glas", DE, 63, FACH_DE),
            S(199, "steel", EN, 60, FACH_EN),
            S(200, "structural steel", EN, 60, FACH_EN),
            S(201, "aluminum", EN, 61, FACH_EN),
            S(202, "zinc", EN, 62, FACH_EN),
            S(203, "glass", EN, 63, FACH_EN),
            S(204, "float glass", EN, 63, FACH_EN),

            // ---- Erdreich (64 Ton und Schluff, 65 Sand und Kies)
            S(205, "erdreich", DE, 65, FACH_DE),
            S(206, "ton", DE, 64, FACH_DE),
            S(207, "kies", DE, 65, FACH_DE),
            S(208, "sand", DE, 65, FACH_DE),
            S(209, "soil", EN, 65, FACH_EN),
            S(210, "earth", EN, 65, FACH_EN),
            S(211, "clay", EN, 64, FACH_EN),
            S(212, "gravel", EN, 65, FACH_EN),
        };
    }
}
