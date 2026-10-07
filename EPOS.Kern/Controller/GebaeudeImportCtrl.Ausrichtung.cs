using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Ausrichtung eines importierten Gebäudes</b> (Abstimmungspapier G5, Abschnitt 7, N5/N6): Lesen des gespeicherten
    /// Nordwinkels samt Herkunft und nachträgliches Drehen. Nur über <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    ///
    /// <para><b>Drehen (N5):</b> Eine neue Richtung der Planoberseite α ergibt den Nordwinkel (360° − α) mod 360°. Der Azimut
    /// jedes Bauteils des Gebäudes in <c>Tab_Bauteil</c> — auch eines von Hand angelegten — wird um den Unterschied zum
    /// gespeicherten Nordwinkel gedreht (Azimut − (neu − alt), normiert auf [0, 360)); ein Azimut NULL bleibt NULL, die
    /// Neigung bleibt. Die Raumgrundrisse stehen in Modellkoordinaten und folgen dem neuen Nordwinkel der Quelle von selbst
    /// (<see cref="Nordangabe"/>). Bauteile und Quelle ändern sich in <b>einem</b> Vorgang.</para>
    ///
    /// <para><b>Herkunft (N6):</b> Die Spalte <see cref="SPALTE_NORDWINKEL_HERKUNFT"/> kommt mit einem eigenen Schemaschritt.
    /// Steht sie, wird sie gelesen und geschrieben; ohne sie gilt: Wert vorhanden → Datei, NULL → Annahme.</para>
    /// </summary>
    public sealed partial class GebaeudeImportCtrl
    {
        /// <summary>G5-N (N6): die Herkunftsspalte des Nordwinkels an <c>Tab_Importquelle</c> — <c>DATEI</c>, <c>EINGABE</c>, <c>ANNAHME</c>.</summary>
        internal const string SPALTE_NORDWINKEL_HERKUNFT = "Nordwinkel_Herkunft";

        /// <summary>Die Werte der Herkunftsspalte.</summary>
        internal const string HERKUNFT_DATEI = "DATEI", HERKUNFT_EINGABE = "EINGABE", HERKUNFT_ANNAHME = "ANNAHME";

        /// <summary>Prüfhaken: läuft im Vorgang nach dem Drehen der Bauteile, vor dem Schreiben der Quelle (Abbruchprobe).</summary>
        internal static Action AusrichtungPruefhaken;

        /// <summary>Die gespeicherte Ausrichtung eines Gebäudes: Quelle, Nordwinkel und seine Herkunft.</summary>
        public sealed record Ausrichtung(int IdImportquelle, double? NordwinkelGrad, Nordwinkelherkunft Herkunft)
        {
            /// <summary>Die Richtung der Planoberseite [°] (N1); ohne Nordwinkel 0° (Annahme Planoberseite = Nord).</summary>
            public double PlanoberseiteGrad => Nordrichtung.PlanoberseiteAusNordwinkel(NordwinkelGrad ?? 0.0) ?? 0.0;
        }

        /// <summary>Ergebnis einer Änderung der Ausrichtung: gedrehte Bauteile und der neue Nordwinkel.</summary>
        public sealed record Ausrichtungsergebnis(bool Ok, string Meldung, int GedrehteBauteile, double NordwinkelGrad);

        /// <summary>Steht die Herkunftsspalte an der Quelle?</summary>
        internal static bool NordherkunftVorhanden()
            => DataRepository.SpalteVorhanden(RaumgrundrissSchema.TAB_QUELLE, SPALTE_NORDWINKEL_HERKUNFT);

        /// <summary>Die Herkunft aus Wert und Spaltentext (N6): ein bekannter Text gilt, sonst Wert → Datei, NULL → Annahme.</summary>
        internal static Nordwinkelherkunft Nordherkunft(double? nordwinkelGrad, string spaltenwert)
        {
            switch ((spaltenwert ?? "").Trim())
            {
                case HERKUNFT_DATEI: return Nordwinkelherkunft.Datei;
                case HERKUNFT_EINGABE: return Nordwinkelherkunft.Eingabe;
                case HERKUNFT_ANNAHME: return Nordwinkelherkunft.Annahme;
                default: return nordwinkelGrad.HasValue ? Nordwinkelherkunft.Datei : Nordwinkelherkunft.Annahme;
            }
        }

        /// <summary>Der Spaltentext einer Herkunft.</summary>
        internal static string HerkunftWert(Nordwinkelherkunft h)
            => h == Nordwinkelherkunft.Eingabe ? HERKUNFT_EINGABE : h == Nordwinkelherkunft.Datei ? HERKUNFT_DATEI : HERKUNFT_ANNAHME;

        /// <summary>
        /// <b>Die Ausrichtung eines Gebäudes</b> — aus seiner jüngsten Importquelle; <c>null</c> = das Gebäude hat keine Quelle.
        /// Ohne die Nordwinkelspalte (Stand vor dem Schritt) gilt die Annahme.
        /// </summary>
        internal Ausrichtung LesenAusrichtung(int idGebaeude)
        {
            object id = DataRepository.ExecuteScalar(
                "SELECT MAX(\"ID\") FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude));
            if (id == null || id == DBNull.Value) return null;
            int idQuelle = Convert.ToInt32(id, CultureInfo.InvariantCulture);
            if (!RaumgrundrissSchema.NordwinkelVorhanden()) return new Ausrichtung(idQuelle, null, Nordwinkelherkunft.Annahme);
            bool mitHerkunft = NordherkunftVorhanden();
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"" + RaumgrundrissSchema.SPALTE_NORDWINKEL + "\"" + (mitHerkunft ? ", \"" + SPALTE_NORDWINKEL_HERKUNFT + "\"" : "") +
                " FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" WHERE \"ID\" = ?", new DbParam("@q", idQuelle));
            if (t == null || t.Rows.Count == 0) return new Ausrichtung(idQuelle, null, Nordwinkelherkunft.Annahme);
            double? nord = RaumgrundrissSchema.Normiert(BaustoffCtrl.ZahlAus(t.Rows[0], RaumgrundrissSchema.SPALTE_NORDWINKEL));
            string herkunft = mitHerkunft ? BaustoffCtrl.TextAus(t.Rows[0], SPALTE_NORDWINKEL_HERKUNFT) : null;
            return new Ausrichtung(idQuelle, nord, Nordherkunft(nord, herkunft));
        }

        /// <summary>
        /// <b>Ändert die Ausrichtung eines importierten Gebäudes</b> (N5): dreht den Azimut aller Bauteile des Gebäudes um den
        /// Unterschied zwischen neuem und gespeichertem Nordwinkel und schreibt den neuen Nordwinkel (Herkunft Eingabe) an die
        /// jüngste Quelle — in einem Vorgang; ein Fehler lässt nichts halb gedreht. Ein Gebäude ohne Quelle wird benannt
        /// abgelehnt.
        /// </summary>
        /// <param name="idGebaeude">Das Gebäude.</param>
        /// <param name="planoberseiteGrad">Die neue Richtung der Planoberseite (+y der Datei) [°], 0° = Nord, im Uhrzeigersinn.</param>
        /// <param name="vorgang">Der Vorgang des Aufrufers; <c>null</c> = ein eigener.</param>
        internal Ausrichtungsergebnis AusrichtungAendern(int idGebaeude, double planoberseiteGrad, DbVorgang vorgang = null)
        {
            if (!(Nordrichtung.NordwinkelAusPlanoberseite(planoberseiteGrad) is double neu))
                return new Ausrichtungsergebnis(false, MyResource.Resource.GEB_AUSRICHTUNG_UNGUELTIG, 0, 0.0);
            if (!RaumgrundrissSchema.NordwinkelVorhanden())
                return new Ausrichtungsergebnis(false, string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_NICHT_GESPEICHERT,
                                                                     RaumgrundrissSchema.SPALTE_NORDWINKEL), 0, 0.0);
            Ausrichtung alt = LesenAusrichtung(idGebaeude);
            if (alt == null) return new Ausrichtungsergebnis(false, MyResource.Resource.GEB_AUSRICHTUNG_KEINE_QUELLE, 0, 0.0);
            double altGrad = alt.NordwinkelGrad ?? 0.0;

            using Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    DataTable t = v.Lese(
                        "SELECT b.\"ID\", b.\"Azimut\" FROM \"" + SchemaKatalog.TAB_BAUTEIL + "\" b " +
                        "INNER JOIN \"" + SchemaKatalog.TAB_ZONE + "\" z ON z.\"ID\" = b.\"ID_Zone\" " +
                        "WHERE z.\"ID_Gebaeude\" = ? AND b.\"Azimut\" IS NOT NULL ORDER BY b.\"ID\"",
                        new DbParam("@g", idGebaeude));
                    var neueWerte = new List<(int Id, double Azimut)>();
                    foreach (DataRow r in t.Rows)
                        if (Nordrichtung.Gedreht(BaustoffCtrl.ZahlAus(r, "Azimut"), altGrad, neu) is double a)
                            neueWerte.Add((Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture), a));
                    foreach ((int id, double azimut) in neueWerte)
                        v.Ausfuehren("UPDATE \"" + SchemaKatalog.TAB_BAUTEIL + "\" SET \"Azimut\" = ? WHERE \"ID\" = ?",
                                     new DbParam("@a", DbParamTyp.Double) { Wert = azimut }, new DbParam("@i", id));
                    AusrichtungPruefhaken?.Invoke();
                    NordwinkelSchreiben(v, alt.IdImportquelle, neu, Nordwinkelherkunft.Eingabe);
                    v.Commit();
                    return new Ausrichtungsergebnis(true,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.GEB_AUSRICHTUNG_GEDREHT, neueWerte.Count,
                                      Gradtext(Nordrichtung.Normiert(planoberseiteGrad) ?? 0.0)),
                        neueWerte.Count, neu);
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new Ausrichtungsergebnis(false, string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HERKUNFT_MSG_NICHT_GESPEICHERT,
                                                                     ex.Message), 0, altGrad);
            }
        }

        private static string Gradtext(double w) => w.ToString("0.#", CultureInfo.CurrentCulture);
    }
}
