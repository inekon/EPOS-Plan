using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schreibweg des Kalibriervorschlags</b> (Umsetzungskonzept Zapfprofilgenerator 4.8 und
    /// Kapitel 7 Zeile Z5, „Kalibrierung der Nichtwohn-Parameter"; Stufe Z5, Gruppe 2, Punkt 3).
    ///
    /// <para><see cref="Messkalibrierung.Nichtwohnparameter"/> rechnet aus einer gemessenen Reihe
    /// einen <see cref="Nichtwohnvorschlag"/> — Tagesbedarf je Einheit, Wochenfaktoren und die
    /// Tagesgänge je Tagtyp. Der Vorschlag wird nie gespeichert; erst
    /// <see cref="TwwNutzungsartCtrl.VorschlagUebernehmen"/> legt daraus eine <b>Anwenderkopie</b>
    /// der Nutzungsart an (Status <c>EIGEN</c>, Katalogversion „&lt;Version&gt;-E&lt;n&gt;“) samt
    /// eigenem Tagesgangsatz und den Zapfkategorien der Vorlage. Die Vorlage bleibt in jedem Fall
    /// unberührt — auch eine freie: Eine Kalibrierung ist der Stand eines Objekts, nicht eine
    /// Berichtigung des Katalogs (K7). Die Sperren gelten wie bei „Speichern unter": Der natürliche
    /// Schlüssel (Bezeichner, Katalogversion) muss frei sein.</para>
    ///
    /// <para><b>Was aus der Messung kommt und was bleibt.</b> Aus der Messung kommen der Bedarf
    /// (Wertgruppe <c>Bedarf</c>) und die Wochenfaktoren (Wertgruppe <c>Wochengang</c>) sowie jeder
    /// Tagesgang, den der Vorschlag führt; sie tragen Herkunftsart <c>VERFAHREN</c> mit der Quelle
    /// „Kalibriert aus Messreihe &lt;Bezeichnung&gt;“ und der Katalogversion der Kopie. Der
    /// <b>Jahresgang</b> bleibt der der Vorlage samt Provenienz: Eine Messreihe über Wochen oder
    /// Monate trägt keinen Jahresgang. Ein Tagtyp, den der Vorschlag nicht führt (kein Messtag
    /// dieses Tagtyps), bleibt der Tagesgang der Vorlage mit dessen Provenienz. Die
    /// <b>Bandbreite</b> der Bedarfswerte entfällt: Sie galt für die Katalogwerte.</para>
    ///
    /// <para><b>Die drei Niveaus.</b> Der Vorschlag nennt einen Tagesbedarf; er wird das
    /// <b>mittlere</b> Niveau. Niedrig und hoch behalten ihr Verhältnis zum mittleren Wert der
    /// Vorlage (ist deren mittleres Niveau nicht positiv, tragen alle drei den gemessenen Wert).</para>
    ///
    /// <para><b>Temperaturen.</b> Der gemessene Tagesbedarf gilt bei den Temperaturen der Messung.
    /// Nennt der Aufrufer sie (<c>temperaturen</c>), stehen sie als Bezugstemperaturen der Kopie —
    /// dann rechnet die Zone mit Temperaturfaktor 1 und gibt die Messenergie genau wieder; ohne
    /// Angabe bleiben die Bezugstemperaturen der Vorlage.</para>
    ///
    /// <para>Alles in EINEM <see cref="DbVorgang"/> (<see cref="TwwNutzungsartCtrl.Ausfuehren"/>):
    /// Satz, Tagesgänge, Nutzungsart und Kategorien stehen zusammen oder gar nicht.</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        /// <summary>
        /// Die Quelle einer aus einer Messreihe kalibrierten Wertgruppe; <c>{0}</c> ist die
        /// Bezeichnung der Messreihe (Provenienz, kein Oberflächentext).
        /// </summary>
        internal const string QUELLE_KALIBRIERT = "Kalibriert aus Messreihe {0}";

        /// <summary>
        /// <b>Die größte Länge der Quelle einer kalibrierten Wertgruppe</b> [Zeichen]. Die
        /// Bezeichnung einer Messreihe kommt vom Anwender und ist in der Länge nicht begrenzt; die
        /// Quelle steht als Herkunfts-Kurztext in der Oberfläche und in jedem Katalogpaket. Ein
        /// längerer Text wird deshalb beschnitten und mit einem Auslassungszeichen geschlossen —
        /// sichtbar gekürzt, nicht still abgehackt.
        /// </summary>
        internal const int QUELLE_LAENGE = 80;

        /// <summary>
        /// Die Quelle einer kalibrierten Wertgruppe aus der Bezeichnung <paramref name="bezeichnung"/>,
        /// auf <see cref="QUELLE_LAENGE"/> Zeichen beschnitten (<see cref="QUELLE_KALIBRIERT"/>).
        /// </summary>
        internal static string Kalibrierquelle(string bezeichnung)
        {
            string t = string.Format(CultureInfo.InvariantCulture, QUELLE_KALIBRIERT, (bezeichnung ?? "").Trim());
            return t.Length <= QUELLE_LAENGE ? t : t.Substring(0, QUELLE_LAENGE - 1) + "…";
        }

        /// <summary>
        /// <b>Übernimmt den Kalibriervorschlag</b> <paramref name="vorschlag"/> der Nutzungsart
        /// <paramref name="idNutzungsart"/> in eine neue Anwenderkopie (Klassenkommentar).
        ///
        /// <para><paramref name="bezeichnung"/> ist die Bezeichnung der Messreihe (Pflicht, steht in
        /// der Quelle jeder kalibrierten Wertgruppe). <paramref name="katalogversion"/> ist der
        /// natürliche Schlüssel der Kopie; ohne Angabe gilt
        /// <see cref="FreieKopieversion"/>. <paramref name="temperaturen"/> sind die
        /// Bezugstemperaturen der Kopie (ohne Angabe die der Vorlage).</para>
        ///
        /// <para>Ausgänge: <see cref="TwwKatalogAusgang.TabellenFehlen"/>,
        /// <see cref="TwwKatalogAusgang.NichtGefunden"/> (Nutzungsart),
        /// <see cref="TwwKatalogAusgang.TagesgangsatzFehlt"/> (Satz der Vorlage fehlt oder ein
        /// Tagtyp steht weder im Vorschlag noch in der Vorlage),
        /// <see cref="TwwKatalogAusgang.EntwurfUnvollstaendig"/> (kein Vorschlag, keine
        /// Bezeichnung), <see cref="TwwKatalogAusgang.RasterUngueltig"/> (Vorschlag verletzt Σ 1,
        /// 24 Stunden, sieben Wochentage oder Tagesbedarf &gt; 0),
        /// <see cref="TwwKatalogAusgang.NameBelegt"/>. Bei Erfolg trägt das Ergebnis die Id der
        /// neuen Nutzungsart.</para>
        /// </summary>
        internal static TwwKatalogErgebnis VorschlagUebernehmen(int idNutzungsart, Nichtwohnvorschlag vorschlag,
                                                               string bezeichnung, string katalogversion = null,
                                                               Temperaturbezug temperaturen = null)
        {
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANG_STAMM))
                return new TwwKatalogErgebnis(TwwKatalogAusgang.TabellenFehlen, idNutzungsart);
            if (vorschlag == null || string.IsNullOrWhiteSpace(bezeichnung))
                return new TwwKatalogErgebnis(TwwKatalogAusgang.EntwurfUnvollstaendig, idNutzungsart);
            if (!VorschlagGueltig(vorschlag))
                return new TwwKatalogErgebnis(TwwKatalogAusgang.RasterUngueltig, idNutzungsart);

            string version = string.IsNullOrWhiteSpace(katalogversion)
                ? FreieKopieversion(idNutzungsart)
                : katalogversion.Trim();
            if (string.IsNullOrEmpty(version))
                return new TwwKatalogErgebnis(TwwKatalogAusgang.NichtGefunden, idNutzungsart);

            var kalibriert = new Provenienz(Kalibrierquelle(bezeichnung), null, version, Herkunftsart.Verfahren);

            int neu = 0;
            TwwKatalogErgebnis erg = Ausfuehren(idNutzungsart, v =>
            {
                TwwNutzungsartEntwurf bezug = Bezugszeile(v, idNutzungsart);
                if (bezug == null) return TwwKatalogAusgang.NichtGefunden;

                DataTable kopf = v.Lese("SELECT Bezeichner FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                                        " WHERE ID = ?", new DbParam("@id", bezug.IdTagesgangsatz));
                if (kopf == null || kopf.Rows.Count == 0) return TwwKatalogAusgang.TagesgangsatzFehlt;
                string satzName = ZapfprofilCtrl.Text(kopf.Rows[0], "Bezeichner");

                if (NameVergeben(v, bezug.Bezeichner, version, null)) return TwwKatalogAusgang.NameBelegt;
                if (v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                             " WHERE Bezeichner = ? AND Katalogversion = ?",
                             new DbParam("@b", satzName), new DbParam("@k", version)) != null)
                    return TwwKatalogAusgang.NameBelegt;

                // --- Der neue Tagesgangsatz: gemessene Gänge, sonst die der Vorlage -----------
                (double[][] alt, Provenienz[] altHerkunft) = TagesgangZeilenLesen(v, bezug.IdTagesgangsatz);
                var gaenge = new double[Tagesgangsatz.TAGTYPEN][];
                var herkunft = new Provenienz[Tagesgangsatz.TAGTYPEN];
                for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                {
                    double[] gemessen = Gemessen(vorschlag, t + 1);
                    gaenge[t] = gemessen ?? alt[t];
                    herkunft[t] = gemessen != null ? kalibriert : altHerkunft[t];
                    if (gaenge[t] == null) return TwwKatalogAusgang.TagesgangsatzFehlt;
                }

                int satzNeu = v.EinfuegenUndId(
                    "INSERT INTO " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                    " (Bezeichner, Katalogversion, Status, Beleg, ReadOnly) VALUES (?, ?, ?, NULL, 0)",
                    new[] { new DbParam("@b", satzName), new DbParam("@k", version),
                            new DbParam("@s", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)) });
                for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                    TagesgangEinfuegen(v, satzNeu, t + 1, gaenge[t], herkunft[t]);

                // --- Die Anwenderkopie der Nutzungsart ---------------------------------------
                TwwNutzungsartEntwurf e = bezug with
                {
                    Katalogversion = version,
                    Bedarf = Bedarfsstufen(bezug.Bedarf, vorschlag.TagesbedarfJeEinheitKwh),
                    BedarfMin = new double?[NutzungsartRaster.NIVEAUS],
                    BedarfMax = new double?[NutzungsartRaster.NIVEAUS],
                    BedarfHerkunft = kalibriert,
                    Bezugstemperaturen = temperaturen ?? bezug.Bezugstemperaturen,
                    Wochenfaktoren = Raster(vorschlag.Wochenfaktoren, NutzungsartRaster.WOCHENTAGE),
                    WochengangHerkunft = kalibriert,
                    IdTagesgangsatz = satzNeu
                };
                if (!Vollstaendig(e) || !RasterGueltig(e)) return TwwKatalogAusgang.RasterUngueltig;

                var p = Fachwerte(e);
                p.Add(new DbParam("@vorlage", idNutzungsart));
                p.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Eigen)));
                // Kein interner Beleg: Die Werte sind nicht mehr die der Vorlage.
                p.Add(new DbParam("@beleg", null));
                neu = v.EinfuegenUndId(SQL_INSERT, p.ToArray());
                // Die Kopie rechnet stochastisch wie die Vorlage: ihre Zapfkategorien kommen mit.
                KategorienKopieren(v, idNutzungsart, neu);
                return TwwKatalogAusgang.Ausgefuehrt;
            });

            return erg.Ok ? new TwwKatalogErgebnis(TwwKatalogAusgang.Ausgefuehrt, neu) : erg;
        }

        // =================================================================================

        /// <summary>
        /// Der vorgeschlagene Tagesgang des Tagtyps <paramref name="tagtyp"/> (1 … 4) als 24
        /// Stundenanteile — <c>null</c>, wenn der Vorschlag ihn nicht führt (kein Messtag).
        /// </summary>
        private static double[] Gemessen(Nichtwohnvorschlag vorschlag, int tagtyp)
        {
            if (vorschlag.Tagesgaenge == null) return null;
            foreach (Tagesgangvorschlag g in vorschlag.Tagesgaenge)
                if ((int)g.Tagtyp == tagtyp) return Raster(g.Anteile, Tagesgangsatz.STUNDEN);
            return null;
        }

        /// <summary>Die Werte einer Liste als Feld der Länge <paramref name="laenge"/>; <c>null</c> bei anderer Länge.</summary>
        private static double[] Raster(System.Collections.Generic.IReadOnlyList<double> werte, int laenge)
        {
            if (werte == null || werte.Count != laenge) return null;
            var feld = new double[laenge];
            for (int i = 0; i < laenge; i++) feld[i] = werte[i];
            return feld;
        }

        /// <summary>
        /// Die drei Niveaus der Kopie: Der gemessene Tagesbedarf ist das mittlere Niveau, niedrig
        /// und hoch behalten ihr Verhältnis zum mittleren Wert der Vorlage (Klassenkommentar).
        /// </summary>
        private static double[] Bedarfsstufen(double[] vorlage, double gemessen)
        {
            var neu = new double[NutzungsartRaster.NIVEAUS];
            double mittel = vorlage != null && vorlage.Length == NutzungsartRaster.NIVEAUS ? vorlage[1] : 0.0;
            for (int i = 0; i < neu.Length; i++)
                neu[i] = mittel > 0.0 ? gemessen * vorlage[i] / mittel : gemessen;
            neu[1] = gemessen;
            return neu;
        }

        /// <summary>
        /// Die Regeln des Vorschlags, bevor ein Vorgang beginnt: Tagesbedarf je Einheit endlich und
        /// positiv, sieben Wochenfaktoren mit Σ 1, mindestens ein Tagesgang, jeder mit 24
        /// nicht negativen Stundenanteilen und Σ 1 (Toleranz <see cref="TOLERANZ"/>).
        /// </summary>
        private static bool VorschlagGueltig(Nichtwohnvorschlag vorschlag)
        {
            if (!NichtNegativ(vorschlag.TagesbedarfJeEinheitKwh) || vorschlag.TagesbedarfJeEinheitKwh <= 0.0) return false;
            double[] woche = Raster(vorschlag.Wochenfaktoren, NutzungsartRaster.WOCHENTAGE);
            if (woche == null || !SummeEins(woche, NutzungsartRaster.WOCHENTAGE)) return false;
            if (vorschlag.Tagesgaenge == null || vorschlag.Tagesgaenge.Count == 0) return false;
            foreach (Tagesgangvorschlag g in vorschlag.Tagesgaenge)
            {
                if ((int)g.Tagtyp < 1 || (int)g.Tagtyp > Tagesgangsatz.TAGTYPEN) return false;
                double[] gang = Raster(g.Anteile, Tagesgangsatz.STUNDEN);
                if (gang == null || !SummeEins(gang, Tagesgangsatz.STUNDEN)) return false;
            }
            return true;
        }
    }
}
