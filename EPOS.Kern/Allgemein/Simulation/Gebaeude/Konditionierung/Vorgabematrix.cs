using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Zelle der Vorgabe-Matrix</b> (Konzept Konditionierungsprofile 3.3, 5.6).
    /// Unveränderlich; <see cref="Leer"/> heißt „keine Angabe" und damit „wie die Ebene darüber"
    /// (Zone → Gebäude → Vorgabe des Programms, Konzept 3.4).
    ///
    /// <para><b>Ein Ort je Zelle:</b> Für Gebäude, Zone und Katalogbau kommt der Zahlenwert aus der
    /// Bestandsspalte, wo es eine gibt; die Vorgabetabelle trägt die Zellen ohne Bestandsspalte und
    /// das „aus" einer Bestandszelle — eine Zeile mit <c>Aus = 1</c> hat Vorrang vor dem Zahlenwert
    /// der Spalte (Konzept 5.6).</para>
    /// </summary>
    public sealed class Matrixzelle
    {
        private Matrixzelle(bool belegt, bool aus, double wert, int? von, int? bis, double? bedingtK)
        {
            Belegt = belegt;
            Aus = aus;
            Wert = wert;
            Von = von;
            Bis = bis;
            BedingtK = bedingtK;
        }

        /// <summary>Die leere Zelle — „wie die Ebene darüber".</summary>
        public static Matrixzelle Leer { get; } = new Matrixzelle(false, false, double.NaN, null, null, null);

        /// <summary>Trägt die Zelle eine Angabe (Wert oder „aus")?</summary>
        public bool Belegt { get; }

        /// <summary>Steht die Zelle auf „aus" (P2)? Dann ist <see cref="Wert"/> ohne Bedeutung.</summary>
        public bool Aus { get; }

        /// <summary>Der Wert der Zelle; <see cref="double.NaN"/>, wenn nicht belegt oder „aus".</summary>
        public double Wert { get; }

        /// <summary>Stunde 0 … 23 der Nachtzeile, Tag 1 … 365 der Saisonzeile, sonst <c>null</c>.</summary>
        public int? Von { get; }

        /// <summary>Wie <see cref="Von"/>, das andere Ende.</summary>
        public int? Bis { get; }

        /// <summary>ΔT der Nachtauskühlung [K] an Lüftung/Nacht; <c>null</c> heißt 2 K (P9).</summary>
        public double? BedingtK { get; }

        /// <summary>Eine Zelle mit einem Wert.</summary>
        public static Matrixzelle AusWert(double wert, int? von = null, int? bis = null, double? bedingtK = null)
            => double.IsFinite(wert)
                ? new Matrixzelle(true, false, wert, von, bis, bedingtK)
                : new Matrixzelle(false, false, double.NaN, von, bis, bedingtK);

        /// <summary>Eine Zelle auf „aus".</summary>
        public static Matrixzelle Abgeschaltet(int? von = null, int? bis = null, double? bedingtK = null)
            => new Matrixzelle(true, true, double.NaN, von, bis, bedingtK);

        /// <summary>Eine Zelle nur mit Zeiten (Nachtfenster oder Saison) und ohne Wert.</summary>
        public static Matrixzelle NurZeiten(int? von, int? bis, double? bedingtK = null)
            => new Matrixzelle(false, false, double.NaN, von, bis, bedingtK);

        /// <summary>
        /// <b>Die Kaskade je Zelle</b> (F2): Ist diese Zelle in irgendeinem Feld belegt, gilt sie;
        /// sonst die des Gebäudes. Jedes Feld einzeln — eine Zone darf das Nachtfenster erben und
        /// den Wert setzen (Konzept 3.4).
        /// </summary>
        public Matrixzelle Erben(Matrixzelle darueber)
        {
            if (darueber == null) return this;
            bool belegt = Belegt || darueber.Belegt;
            bool aus = Belegt ? Aus : darueber.Aus;
            double wert = Belegt ? Wert : darueber.Wert;
            return new Matrixzelle(belegt, aus, wert,
                                   Von ?? darueber.Von, Bis ?? darueber.Bis, BedingtK ?? darueber.BedingtK);
        }

        /// <summary>Sprachunabhängige Kurzfassung.</summary>
        public override string ToString()
            => (!Belegt ? "—" : Aus ? DbWerte.KOND_WOCHE_AUS : Wert.ToString("G6", CultureInfo.InvariantCulture)) +
               (Von.HasValue || Bis.HasValue
                   ? " [" + Z(Von) + "…" + Z(Bis) + "]"
                   : string.Empty);

        private static string Z(int? n) => n.HasValue ? n.Value.ToString(CultureInfo.InvariantCulture) : "—";
    }

    /// <summary>
    /// <b>Eine Spalte der Vorgabe-Matrix</b> — die sechs Zeilen einer Größe (Konzept 3.3).
    /// Unveränderlich.
    /// </summary>
    public sealed class Matrixspalte
    {
        /// <summary>Baut die Spalte; jede Zelle darf <c>null</c> sein und wird dann <see cref="Matrixzelle.Leer"/>.</summary>
        public Matrixspalte(Konditionierungsgroesse groesse, Matrixzelle nennwert, Matrixzelle tag,
                            Matrixzelle nacht, Matrixzelle wochenende, Matrixzelle ferien, Matrixzelle saison)
        {
            Groesse = groesse;
            Nennwert = nennwert ?? Matrixzelle.Leer;
            Tag = tag ?? Matrixzelle.Leer;
            Nacht = nacht ?? Matrixzelle.Leer;
            Wochenende = wochenende ?? Matrixzelle.Leer;
            Ferien = ferien ?? Matrixzelle.Leer;
            Saison = saison ?? Matrixzelle.Leer;
        }

        /// <summary>Die Größe dieser Spalte.</summary>
        public Konditionierungsgroesse Groesse { get; }

        /// <summary>Zeile Nennwert: Infiltration [1/h] bei der Lüftung, Nennleistung [W] bei den Lasten.</summary>
        public Matrixzelle Nennwert { get; }

        /// <summary>Zeile Tag.</summary>
        public Matrixzelle Tag { get; }

        /// <summary>Zeile Nacht — <see cref="Matrixzelle.Von"/>/<see cref="Matrixzelle.Bis"/> sind das Nachtfenster der Spalte (F19).</summary>
        public Matrixzelle Nacht { get; }

        /// <summary>Zeile Wochenende — Samstag und Sonntag ganztägig.</summary>
        public Matrixzelle Wochenende { get; }

        /// <summary>Zeile Ferien — der Wert; die datierten Zeiträume stehen neben der Matrix.</summary>
        public Matrixzelle Ferien { get; }

        /// <summary>Zeile Saison — <see cref="Matrixzelle.Von"/>/<see cref="Matrixzelle.Bis"/> sind Start und Ende der Heiz- bzw. Kühlperiode (E53).</summary>
        public Matrixzelle Saison { get; }

        /// <summary>Die Zelle einer Zeile über ihr Kennwort (<see cref="DbWerte.KOND_ZEILEN"/>); <c>null</c> bei unbekanntem Kennwort.</summary>
        public Matrixzelle Zeile(string kennwort)
        {
            switch (kennwort)
            {
                case DbWerte.KOND_ZEILE_NENNWERT: return Nennwert;
                case DbWerte.KOND_ZEILE_TAG: return Tag;
                case DbWerte.KOND_ZEILE_NACHT: return Nacht;
                case DbWerte.KOND_ZEILE_WOCHENENDE: return Wochenende;
                case DbWerte.KOND_ZEILE_FERIEN: return Ferien;
                case DbWerte.KOND_ZEILE_SAISON: return Saison;
                default: return null;
            }
        }

        /// <summary>Die Kaskade je Zelle (F2) — Spalte der Zone über der des Gebäudes.</summary>
        public Matrixspalte Erben(Matrixspalte darueber)
            => darueber == null
                ? this
                : new Matrixspalte(Groesse,
                                   Nennwert.Erben(darueber.Nennwert), Tag.Erben(darueber.Tag),
                                   Nacht.Erben(darueber.Nacht), Wochenende.Erben(darueber.Wochenende),
                                   Ferien.Erben(darueber.Ferien), Saison.Erben(darueber.Saison));

        /// <summary>
        /// Trägt die Spalte irgendeine <b>neue</b> Angabe, die es ohne Kalender nicht gäbe? Das ist
        /// die Frage der Bauvorschrift der Byte-Gleichheit (Konzept 6): Ist sie mit <c>false</c>
        /// beantwortet, nimmt der Eingang wörtlich den Bestandszweig.
        /// </summary>
        public bool TraegtNeues(Func<Matrixzelle, bool> istBestand)
        {
            foreach (string z in DbWerte.KOND_ZEILEN)
            {
                Matrixzelle zelle = Zeile(z);
                if (zelle == null) continue;
                if (istBestand != null && istBestand(zelle)) continue;
                if (zelle.Belegt || zelle.Von.HasValue || zelle.Bis.HasValue || zelle.BedingtK.HasValue)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// <b>Die wirksame Vorgabe-Matrix eines Eigentümers</b> (Konzept Konditionierungsprofile 3.3 und
    /// 6): fünf Spalten mit je sechs Zeilen, gebildet aus den <b>Bestandsspalten</b> des Gebäudes
    /// bzw. der Zone und den <b>Vorgabezeilen</b> nach der Regel „ein Ort je Zelle", dazu die drei
    /// Einzelangaben neben der Matrix (Ferienzeiträume, Maximalraumtemperatur, Merker).
    ///
    /// <para><b>Ohne Datenbank:</b> Die Bestandsfelder kommen als <see cref="Matrixeingang"/>
    /// herein, die Vorgabezeilen als Liste — so ist die Matrix ohne Datei prüfbar, und der
    /// Rechenkern liest sie im Lauf genauso wie der Controller im Dialog.</para>
    ///
    /// <para><b>Die Kaskade Zone → Gebäude</b> läuft über <see cref="Erben"/>, je Zelle (F2).</para>
    /// </summary>
    public sealed class Vorgabematrix
    {
        private readonly Matrixspalte[] _spalten;

        private Vorgabematrix(Matrixspalte[] spalten, Matrixeingang bestand)
        {
            _spalten = spalten;
            Bestand = bestand;
        }

        /// <summary>Die Bestandsfelder, aus denen die Matrix gebildet wurde (Ferienzeiträume, Merker, Profil).</summary>
        public Matrixeingang Bestand { get; }

        /// <summary>Die Spalte einer Größe.</summary>
        public Matrixspalte Spalte(Konditionierungsgroesse g) => _spalten[(int)g];

        /// <summary>Die Heizspalte.</summary>
        public Matrixspalte Heizsoll => Spalte(Konditionierungsgroesse.Heizsoll);

        /// <summary>Die Kühlspalte.</summary>
        public Matrixspalte Kuehlsoll => Spalte(Konditionierungsgroesse.Kuehlsoll);

        /// <summary>Die Lüftungsspalte.</summary>
        public Matrixspalte Lueftung => Spalte(Konditionierungsgroesse.Lueftung);

        /// <summary>Die Gerätespalte.</summary>
        public Matrixspalte Geraete => Spalte(Konditionierungsgroesse.Geraete);

        /// <summary>Die Personenspalte.</summary>
        public Matrixspalte Personen => Spalte(Konditionierungsgroesse.Personen);

        /// <summary>
        /// <b>Bildet die wirksame Matrix</b> aus Bestandsfeldern und Vorgabezeilen. Eine Vorgabezeile
        /// mit <c>Aus = 1</c> schlägt den Zahlenwert der Bestandsspalte; wo die Zeile eine
        /// Bestandsspalte hat, liefert <b>sie</b> den Wert, und die Vorgabezeile <b>ergänzt</b> nur
        /// (Zeiten, ΔT) — wo sie keine hat, gilt der Wert der Vorgabezeile (Konzept 5.6). Welche
        /// Zeile eine Bestandsspalte hat, sagt <see cref="Matrixzellenort"/>.
        /// </summary>
        /// <param name="bestand">Die Bestandsfelder des Gebäudes, der Zone oder des Katalogbaus.</param>
        /// <param name="vorgaben">Die Vorgabezeilen genau dieses Eigentümers; darf <c>null</c> sein.</param>
        /// <param name="art">
        /// Wem die Matrix gehört. Gebäude, Zone und Katalogbau führen dieselben Bestandsspalten;
        /// eine <b>Vorlage</b> führt keine — dort trägt die Vorgabezeile jede Zelle (Konzept 5.7).
        /// </param>
        public static Vorgabematrix Bilden(Matrixeingang bestand, IEnumerable<Vorgabezeile> vorgaben,
                                           Kalendereigentuemer art = Kalendereigentuemer.Gebaeude)
        {
            if (bestand == null) throw new ArgumentNullException(nameof(bestand));

            // Die Vorgabezeilen nach Groesse und Zeile ablegen - hoechstens eine je Paar
            // (Eindeutigkeit, Konzept 5.6); eine zweite ueberschreibt die erste nicht, sondern
            // bleibt liegen: Der Controller haelt die Eindeutigkeit, hier gilt die erste.
            var zellen = new Matrixzelle[5, 6];
            if (vorgaben != null)
                foreach (Vorgabezeile v in vorgaben)
                {
                    if (v == null) continue;
                    if (!Konditionierungsgroessen.AusKennwort(v.Groesse, out Konditionierungsgroesse g)) continue;
                    int zi = Zeilenindex(v.Zeile);
                    if (zi < 0 || zellen[(int)g, zi] != null) continue;
                    zellen[(int)g, zi] = v.Aus
                        ? Matrixzelle.Abgeschaltet(v.Von, v.Bis, v.BedingtK)
                        : v.Wert.HasValue
                            ? Matrixzelle.AusWert(v.Wert.Value, v.Von, v.Bis, v.BedingtK)
                            : Matrixzelle.NurZeiten(v.Von, v.Bis, v.BedingtK);
                }

            var spalten = new Matrixspalte[5];
            spalten[(int)Konditionierungsgroesse.Heizsoll] = Heizspalte(bestand, zellen, art);
            spalten[(int)Konditionierungsgroesse.Kuehlsoll] = Kuehlspalte(bestand, zellen, art);
            spalten[(int)Konditionierungsgroesse.Lueftung] = Lueftungsspalte(bestand, zellen, art);
            spalten[(int)Konditionierungsgroesse.Geraete] = Geraetespalte(bestand, zellen, art);
            spalten[(int)Konditionierungsgroesse.Personen] = Personenspalte(bestand, zellen);
            return new Vorgabematrix(spalten, bestand);
        }

        /// <summary>
        /// <b>Das Nachtfenster einer Spalte</b> (F19) — die EINE Stelle, an der es gebildet wird:
        /// die Zeiten der Nachtzeile der Spalte, leer das der Heizspalte, leer die Nachtzeit des
        /// Gebäudes (<c>Nachtabsenkung_Beginn</c>/<c>_Ende</c>, beide leer = 22 bis 6 Uhr).
        ///
        /// <para>Sie gilt dem Generator (<see cref="Standardfahrplan"/>, Standardwoche) und der
        /// <b>Nachtauskühlung</b> (Stufe KP1b, Konzept 3.7) zugleich — beide müssen dasselbe Fenster
        /// sehen, sonst stünde der bedingte Anteil neben den Stunden, in denen der Nachtwert steht.</para>
        /// </summary>
        /// <param name="groesse">Die Spalte, deren Nachtfenster gesucht ist.</param>
        /// <param name="von">Beginn [Uhr 0 … 23] oder <c>null</c>.</param>
        /// <param name="bis">Ende [Uhr 0 … 23] oder <c>null</c>; beide leer heißt die Vorgabe 22–6 Uhr.</param>
        public void Nachtfenster(Konditionierungsgroesse groesse, out int? von, out int? bis)
        {
            Matrixspalte s = Spalte(groesse);
            von = s.Nacht.Von ?? Heizsoll.Nacht.Von ?? Bestand.NachtBeginn;
            bis = s.Nacht.Bis ?? Heizsoll.Nacht.Bis ?? Bestand.NachtEnde;
        }

        /// <summary>
        /// <b>Die Kaskade Zone → Gebäude</b> (F2): Diese Matrix (die der Zone) über der des Gebäudes,
        /// je Zelle. Die Ferienzeiträume und die Merker kommen vom Gebäude — sie gelten für alle
        /// Spalten (Konzept 3.4).
        /// </summary>
        public Vorgabematrix Erben(Vorgabematrix gebaeude)
        {
            if (gebaeude == null) return this;
            var spalten = new Matrixspalte[5];
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                spalten[(int)g] = Spalte(g).Erben(gebaeude.Spalte(g));
            return new Vorgabematrix(spalten, Bestand.Erben(gebaeude.Bestand));
        }

        // =================================================================
        //  Die fünf Spalten aus den Bestandsfeldern (Konzept 3.3)
        // =================================================================

        // HEIZEN: vier Sollwerte, Nachtzeit und Ferien sind Bestandsspalten; nur die Saison ist neu.
        private static Matrixspalte Heizspalte(Matrixeingang b, Matrixzelle[,] neu, Kalendereigentuemer art)
        {
            int i = (int)Konditionierungsgroesse.Heizsoll;
            const Konditionierungsgroesse g = Konditionierungsgroesse.Heizsoll;
            return new Matrixspalte(g,
                nennwert: neu[i, 0],
                tag: Bestandszelle(art, g, DbWerte.KOND_ZEILE_TAG, b.SollTag, neu[i, 1]),
                nacht: Bestandszelle(art, g, DbWerte.KOND_ZEILE_NACHT, b.SollNacht, neu[i, 2],
                                     b.NachtBeginn, b.NachtEnde),
                wochenende: Bestandszelle(art, g, DbWerte.KOND_ZEILE_WOCHENENDE, b.SollWochenende, neu[i, 3]),
                ferien: Bestandszelle(art, g, DbWerte.KOND_ZEILE_FERIEN, b.SollFerien, neu[i, 4]),
                saison: neu[i, 5]);
        }

        // KUEHLEN: Kuehl_Sollwert und Kuehl_Sollwert_Nacht sind Bestandsspalten (P13); die Zeiten der
        // Nacht, Wochenende, Ferien und Saison sind neu.
        private static Matrixspalte Kuehlspalte(Matrixeingang b, Matrixzelle[,] neu, Kalendereigentuemer art)
        {
            int i = (int)Konditionierungsgroesse.Kuehlsoll;
            const Konditionierungsgroesse g = Konditionierungsgroesse.Kuehlsoll;
            return new Matrixspalte(g,
                nennwert: neu[i, 0],
                tag: Bestandszelle(art, g, DbWerte.KOND_ZEILE_TAG, b.KuehlSollwert, neu[i, 1]),
                nacht: Bestandszelle(art, g, DbWerte.KOND_ZEILE_NACHT, b.KuehlSollwertNacht, neu[i, 2]),
                wochenende: neu[i, 3],
                ferien: neu[i, 4],
                saison: neu[i, 5]);
        }

        // LUEFTUNG: Luftwechsel_Infiltration ist der Nennwert, Luftwechsel_Nutzer der Tagwert
        // (absolut in 1/h, F15); Nacht, Wochenende und Ferien sind neu.
        private static Matrixspalte Lueftungsspalte(Matrixeingang b, Matrixzelle[,] neu, Kalendereigentuemer art)
        {
            int i = (int)Konditionierungsgroesse.Lueftung;
            const Konditionierungsgroesse g = Konditionierungsgroesse.Lueftung;
            return new Matrixspalte(g,
                nennwert: Bestandszelle(art, g, DbWerte.KOND_ZEILE_NENNWERT, b.LuftwechselInfiltration, neu[i, 0]),
                tag: Bestandszelle(art, g, DbWerte.KOND_ZEILE_TAG, b.LuftwechselNutzer, neu[i, 1]),
                nacht: neu[i, 2],
                wochenende: neu[i, 3],
                ferien: neu[i, 4],
                saison: neu[i, 5]);
        }

        // GERAETE: Interne_Waermegewinne ist der Nennwert [W] (nach P1 Gesamtwert minus
        // Personenmittel); alle Anteilszeilen sind neu, leer heisst 100 %.
        private static Matrixspalte Geraetespalte(Matrixeingang b, Matrixzelle[,] neu, Kalendereigentuemer art)
        {
            int i = (int)Konditionierungsgroesse.Geraete;
            const Konditionierungsgroesse g = Konditionierungsgroesse.Geraete;
            return new Matrixspalte(g,
                nennwert: Bestandszelle(art, g, DbWerte.KOND_ZEILE_NENNWERT, b.InterneWaermegewinne, neu[i, 0]),
                tag: neu[i, 1], nacht: neu[i, 2], wochenende: neu[i, 3], ferien: neu[i, 4], saison: neu[i, 5]);
        }

        // PERSONEN: alles neu; der Nennwert wird aus Bewohner x 70 W vorgeschlagen (Konzept 3.1),
        // der Vorschlag steht im Controller, nicht in der Matrix.
        private static Matrixspalte Personenspalte(Matrixeingang b, Matrixzelle[,] neu)
        {
            int i = (int)Konditionierungsgroesse.Personen;
            return new Matrixspalte(Konditionierungsgroesse.Personen,
                nennwert: neu[i, 0], tag: neu[i, 1], nacht: neu[i, 2],
                wochenende: neu[i, 3], ferien: neu[i, 4], saison: neu[i, 5]);
        }

        /// <summary>
        /// Eine Zelle, die am Eigentümer eine Bestandsspalte haben KANN: Die Vorgabezeile trägt
        /// „aus" (Vorrang) und die neuen Angaben, der Zahlenwert kommt aus der Bestandsspalte —
        /// <b>ein Ort je Zelle</b> (Konzept 5.6).
        ///
        /// <para><b>Die Weiche</b> steht in <see cref="Matrixzellenort"/>: Gibt es für
        /// <paramref name="art"/> eine Bestandsspalte, liefert allein sie den Wert — ein
        /// Zahlenwert in der Vorgabezeile wird NICHT gelesen, und die Schreibwege legen dort auch
        /// keinen ab. Gibt es keine — an einer Vorlage nirgends (Konzept 5.7) —, gilt der Wert
        /// der Vorgabezeile.</para>
        /// </summary>
        private static Matrixzelle Bestandszelle(Kalendereigentuemer art, Konditionierungsgroesse groesse,
                                                 string zeile, double? bestand, Matrixzelle vorgabe,
                                                 int? von = null, int? bis = null)
        {
            int? v = vorgabe?.Von ?? von;
            int? bi = vorgabe?.Bis ?? bis;
            if (vorgabe != null && vorgabe.Aus)
                return Matrixzelle.Abgeschaltet(v, bi, vorgabe.BedingtK);

            double? wert = Matrixzellenort.HatBestandsspalte(art, groesse, zeile)
                ? bestand
                : vorgabe != null && vorgabe.Belegt ? vorgabe.Wert : bestand;
            return wert.HasValue && double.IsFinite(wert.Value)
                ? Matrixzelle.AusWert(wert.Value, v, bi, vorgabe?.BedingtK)
                : Matrixzelle.NurZeiten(v, bi, vorgabe?.BedingtK);
        }

        private static int Zeilenindex(string kennwort)
        {
            for (int i = 0; i < DbWerte.KOND_ZEILEN.Count; i++)
                if (string.Equals(kennwort, DbWerte.KOND_ZEILEN[i], StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
