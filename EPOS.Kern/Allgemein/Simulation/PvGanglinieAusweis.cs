using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Ausweis einer rechnenden PV-Ganglinie</b> für Ergebnisreiter und Bericht: Rechnet die
    /// Photovoltaik über eine Ganglinie statt über das Modulmodell, nennen beide die Quelle —
    /// „Ganglinie ‚Name‘ (Viertelstundenwerte), Nennleistung … kWp" — statt der Modul- und
    /// Wechselrichterangaben, und die Felder des Modulmodells stehen als „entfällt (Ganglinie)".
    ///
    /// <para>Ausweis, kein Rechenweg: Er geht in keine Ergebnistabelle, keinen CSV-Export und keine
    /// Kennzahl. Die Nennleistung ist die Kennleistung des Laufs
    /// (<see cref="PvGanglinieWeiche.Stand.KennleistungKwp"/>: gepflegt, sonst die Spitze der Reihe).</para>
    /// </summary>
    public sealed class PvGanglinieAusweis
    {
        /// <summary>Der Bezeichner der Ganglinie.</summary>
        public string Bezeichner = "";

        /// <summary>true = Viertelstundenwerte, false = Stundenwerte.</summary>
        public bool Viertelstunden;

        /// <summary>Die Kennleistung des Laufs [kWp].</summary>
        public double NennleistungKwp;

        /// <summary>true = die Nennleistung ist gepflegt; false = der Lauf nimmt die Spitze der Reihe.</summary>
        public bool NennleistungGepflegt;

        /// <summary>Der Ausweis eines Stands der Weiche; <c>null</c>, wenn die Ganglinie nicht rechnet.</summary>
        public static PvGanglinieAusweis Aus(PvGanglinieWeiche.Stand s)
        {
            if (s == null || !s.RechnetGanglinie) return null;
            return new PvGanglinieAusweis
            {
                Bezeichner = s.Bezeichner ?? "",
                Viertelstunden = s.Viertelstunden,
                NennleistungKwp = s.KennleistungKwp,
                NennleistungGepflegt = s.NennleistungKwp.HasValue && s.NennleistungKwp.Value > 0
            };
        }

        /// <summary>
        /// „Ganglinie ‚Name‘ (Viertelstundenwerte/Stundenwerte), Nennleistung … kWp" in der Sprache
        /// <paramref name="kultur"/> (Texte und Zahlformat); ohne gepflegte Nennleistung mit dem Zusatz,
        /// dass sie die Spitze der Reihe ist.
        /// </summary>
        public string Text(CultureInfo kultur)
        {
            CultureInfo k = kultur ?? CultureInfo.CurrentUICulture;
            string raster = R(Viertelstunden ? "PVG_AUSWEIS_VIERTEL" : "PVG_AUSWEIS_STUNDE", k);
            string muster = R(NennleistungGepflegt ? "PVG_AUSWEIS_QUELLE" : "PVG_AUSWEIS_QUELLE_SPITZE", k);
            return string.Format(k, muster, Bezeichner, raster, NennleistungKwp);
        }

        /// <summary>„entfällt (Ganglinie)" — ein Feld des Modulmodells, das bei Ganglinie nicht gilt.</summary>
        public static string Entfaellt(CultureInfo kultur) => R("PVG_AUSWEIS_ENTFAELLT", kultur ?? CultureInfo.CurrentUICulture);

        private static string R(string schluessel, CultureInfo kultur)
        {
            try { return MyResource.Resource.ResourceManager.GetString(schluessel, kultur) ?? schluessel; }
            catch { return schluessel; }
        }
    }
}
