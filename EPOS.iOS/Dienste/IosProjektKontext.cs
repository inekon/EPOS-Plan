using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IProjektKontext"/>: das gerade geoeffnete
/// Projekt.
///
/// <para><b>Der Unterschied zu Windows.</b> Dort haengt der Kontext an der
/// Startmaske (<c>Form_Start.m_ID_Projekt</c>); <c>FormStartProjektKontext</c>
/// reicht nur durch. Auf iOS gibt es keine Startmaske - die Huelle fuehrt den
/// Kontext selbst. Fachlich ist das dieselbe Zusage: Es gibt genau EINE
/// Wahrheit fuer „welches Projekt ist offen".</para>
///
/// <para><b><see cref="Vorhanden"/> ist <c>true</c>, sobald die Anwendung
/// laeuft.</b> Das ist die Aussage der Schnittstelle: „Es gibt einen fuehrenden
/// Kontext" - auch dann, wenn gerade kein Projekt offen ist. Erst ohne
/// Oberflaeche (Referenzlauf, Konsolenwerkzeug) duerfen Aufrufer ersatzweise
/// <c>Tab_Applikation</c> lesen. Genau diese Fallgabelung trifft
/// <c>KiAktionenProjekt.AktivesProjektErmitteln</c>.</para>
///
/// <para><b><see cref="Uebernehmen"/> geht denselben Weg wie unter Windows:</b>
/// fehlenden Namen ueber <c>ProjektCtrl.ReadSingle</c> nachschlagen, den
/// Kontext setzen, <c>Tab_Applikation</c> fortschreiben
/// (<c>ApplikationCtrl.Update</c> - das ist wortgleich
/// <c>Form_Start.ZuletztGeoeffnetMerken</c>) und <see cref="Gewechselt"/>
/// ausloesen.</para>
///
/// <para>Diese Datei kennt keine iOS-API und laesst sich ohne Mac pruefen.</para>
/// </summary>
public sealed class IosProjektKontext : IProjektKontext
{
    private int _id;
    private string _name = "";
    private string _klimazone = "";

    /// <inheritdoc/>
    public bool Vorhanden => true;

    /// <inheritdoc/>
    public int Id => _id;

    /// <inheritdoc/>
    public string Name => _name ?? "";

    /// <inheritdoc/>
    public string Klimazone => _klimazone ?? "";

    /// <inheritdoc/>
    public bool Uebernehmen(int id, string name)
    {
        string projekt = name ?? "";
        int nummer = id;
        int klimaId = 0;

        try
        {
            var ctrl = new ProjektCtrl();

            // Der NAME ist der fuehrende Schluessel des Bestands, die ID der
            // Rueckfall - dieselbe Reihenfolge wie in FormStartProjektKontext.
            if (!string.IsNullOrWhiteSpace(projekt)) ctrl.ReadSingle(projekt);
            else if (nummer > 0) ctrl.ReadSingle(nummer);
            else return false;

            if (ctrl.rows <= 0) return false;

            projekt = ctrl.m_szProjektname ?? "";
            nummer = ctrl.m_ID;
            klimaId = ctrl.m_ID_Klimaregion;
        }
        catch
        {
            return false;
        }

        _id = nummer;
        _name = projekt;
        _klimazone = KlimaregionName(klimaId);

        ZuletztGeoeffnetMerken();

        Action? h = Gewechselt;
        h?.Invoke();
        return true;
    }

    /// <inheritdoc/>
    public event Action? Gewechselt;

    /// <summary>
    /// Schreibt das zuletzt geoeffnete Projekt in <c>Tab_Applikation</c> fort -
    /// wortgleich zu <c>Form_Start.ZuletztGeoeffnetMerken</c>.
    /// </summary>
    private void ZuletztGeoeffnetMerken()
    {
        try
        {
            var app = new ApplikationCtrl
            {
                m_ID_Projekt = _id,
                m_szProjektname = _name
            };
            app.Update();
        }
        catch
        {
            // Eine nicht fortgeschriebene Merkzeile ist ein Schoenheitsfehler,
            // kein Grund, den Projektwechsel zurueckzunehmen.
        }
    }

    /// <summary>
    /// Der Anzeigename einer Klimaregion; <c>""</c>, wenn er sich nicht lesen
    /// laesst. Dieselbe Abfrage wie <c>Form_Start.GetKlimaregion</c>, nur
    /// parametrisiert.
    /// </summary>
    private static string KlimaregionName(int idKlimaregion)
    {
        if (idKlimaregion <= 0) return "";

        try
        {
            object wert = DataRepository.ExecuteScalar(
                "SELECT Name FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = ?",
                new[] { new DbParam("@id", idKlimaregion) });

            return wert == null || wert == DBNull.Value ? "" : (Convert.ToString(wert) ?? "");
        }
        catch
        {
            return "";
        }
    }
}
