using System.Reflection;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dienste;

/// <summary>
/// Der EINE Weg, mit dem ein Razor-Dialog seine Felder beim Hilfe-Assistenten anmeldet
/// (Auftrag #200, Stufe S2 des Konzepts „Der Hilfe-Assistent im Dialog", 3.3).
///
/// <para><b>Drei Zeilen im Dialog, und nicht mehr.</b> Ein Feld einzeln anzumelden hiesse,
/// den Dialogkatalog ein zweites Mal zu schreiben — einmal als Deklaration im Kern und
/// einmal als Getter-Liste in der Komponente. Stattdessen liest dieser Helfer die
/// Deklaration und löst jeden Eigenschaftspfad per Reflection am Daten-Objekt auf:</para>
///
/// <code>
/// private KiMaskenanmeldung? _kiMaske;
/// protected override void OnInitialized()
///     =&gt; _kiMaske = KiMaskenanmeldung.Fuer(KiMaskennamen.HEIZKESSEL, () =&gt; Daten);
/// public void Dispose() =&gt; _kiMaske?.Dispose();
/// </code>
///
/// <para><b>Die Quelle ist ein DELEGAT und keine Instanz.</b> Nicht jeder Dialog führt sein
/// Daten-Objekt über die ganze Lebensdauer: Der Photovoltaik-Dialog meldet die GEWÄHLTE
/// Zeile an, und die wechselt mit jedem Klick in der Projektliste. Ein festgehaltenes
/// Objekt zeigte dem Assistenten die Zeile von vorhin. Der Delegat wird bei JEDEM Lesen
/// gerufen; liefert er <c>null</c>, sind die Felder leer — derselbe Zustand, den der
/// Anwender auf der Maske sieht.</para>
///
/// <para><b>Reflection einmal beim Anmelden, nicht bei jedem Lesen.</b> Die
/// <see cref="PropertyInfo"/> wird hier aufgelöst und im Delegaten festgehalten; das Lesen
/// selbst ist danach ein <c>GetValue</c> ohne Suche.</para>
///
/// <para><b>Was nicht auflöst, wird nicht angemeldet</b> — und fällt im Wächter auf, nicht
/// beim Anwender: <see cref="Pruefe"/> nennt jeden Eigenschaftspfad des Katalogs, den es am
/// Daten-Objekt nicht gibt, und <c>EPOS.UI.Tests/Dialoge/Hilfe/KiDialogkatalogTests</c>
/// hält die Liste für alle fünf Masken leer. Ein Dialog soll an einem Tippfehler im Katalog
/// nicht aufgehen können.</para>
/// </summary>
public sealed class KiMaskenanmeldung : IDisposable
{
    private readonly string _maskenname;
    private readonly object? _marke;
    private bool _abgemeldet;

    private KiMaskenanmeldung(string maskenname, object? marke)
    {
        _maskenname = maskenname;
        _marke = marke;
    }

    /// <summary>Der Katalogschlüssel der angemeldeten Maske.</summary>
    public string Maskenname => _maskenname;

    /// <summary>Ist überhaupt etwas angemeldet worden?</summary>
    public bool Angemeldet => _marke is not null && !_abgemeldet;

    /// <summary>
    /// Meldet die Maske mit allen Feldern ihres Katalogeintrags an.
    /// </summary>
    /// <typeparam name="T">Der Typ des Daten-Objekts; sein Name steht im Katalog vor dem Punkt.</typeparam>
    /// <param name="maskenname">Der Katalogschlüssel, eine Konstante aus <see cref="KiMaskennamen"/>.</param>
    /// <param name="quelle">Liefert das aktuelle Daten-Objekt; darf <c>null</c> liefern.</param>
    /// <returns>
    /// Die Anmeldung — sie wird beim Schließen des Dialogs verworfen. Kennt der Katalog die
    /// Maske nicht, kommt trotzdem ein Objekt zurück; es ist dann nur nicht
    /// <see cref="Angemeldet"/>.
    /// </returns>
    public static KiMaskenanmeldung Fuer<T>(string maskenname, Func<T?> quelle) where T : class
    {
        if (quelle is null) throw new ArgumentNullException(nameof(quelle));

        KiDialog? eintrag = KiDialoge.Katalog.Finde(maskenname);
        if (eintrag is null) return new KiMaskenanmeldung(maskenname ?? "", null);

        var zugaenge = new List<KiFeldzugang>(eintrag.Felder.Count);

        foreach (KiDialogFeld feld in eintrag.Felder)
        {
            PropertyInfo? eigenschaft = Eigenschaft(typeof(T), feld);
            if (eigenschaft is null) continue;

            // Die PropertyInfo steckt im Abschluss - gesucht wird einmal, gelesen oft.
            Func<object?> lesen = () =>
            {
                T? stand = quelle();
                return stand is null ? null : eigenschaft.GetValue(stand);
            };

            // Der Setzer ist Stufe S3 (Auftrag #201) und heute unbenutzt. Er entsteht
            // trotzdem hier, damit die Setzbarkeit eines Feldes AN DER EIGENSCHAFT haengt
            // und nicht an einer zweiten Liste: Eine abgeleitete Groesse (die Diagnose der
            // Flotte) hat keinen Setzer, und genau das soll der Assistent sehen.
            Action<object?>? setzen = eigenschaft.CanWrite
                ? wert =>
                  {
                      T? stand = quelle();
                      if (stand is not null) eigenschaft.SetValue(stand, wert);
                  }
                : null;

            zugaenge.Add(new KiFeldzugang(feld, lesen, setzen));
        }

        object? marke = KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, zugaenge);
        return new KiMaskenanmeldung(eintrag.Maskenname, marke);
    }

    /// <summary>
    /// Der WÄCHTER: die Eigenschaftspfade des Katalogeintrags, die es am Daten-Objekt
    /// nicht gibt. Leere Liste = alles löst auf.
    /// </summary>
    /// <remarks>
    /// Geprüft wird beides — der Typname VOR dem Punkt (gehören Katalog und Daten-Objekt
    /// zusammen?) und der Eigenschaftsname dahinter. Der Typname allein wäre Schmuck; ohne
    /// ihn fiele ein an die falsche Maske gehängtes Daten-Objekt erst auf, wenn zufällig
    /// eine Eigenschaft gleich heisst.
    /// </remarks>
    public static IReadOnlyList<string> Pruefe(string maskenname, Type datentyp)
    {
        if (datentyp is null) throw new ArgumentNullException(nameof(datentyp));

        KiDialog? eintrag = KiDialoge.Katalog.Finde(maskenname);
        if (eintrag is null) return new[] { "Maske '" + (maskenname ?? "") + "' steht nicht im Katalog." };

        var fehlt = new List<string>();

        foreach (KiDialogFeld feld in eintrag.Felder)
            if (Eigenschaft(datentyp, feld) is null)
                fehlt.Add(feld.Eigenschaftspfad);

        return fehlt;
    }

    /// <summary>Meldet die Maske ab. Mehrfaches Abmelden ist ausdrücklich erlaubt.</summary>
    public void Dispose()
    {
        if (_abgemeldet || _marke is null) return;
        _abgemeldet = true;
        KiMaskenbruecke.Abmelden(_maskenname, _marke);
    }

    /// <summary>
    /// Die Eigenschaft hinter dem Pfad; <c>null</c>, wenn der Typname nicht passt oder es
    /// die Eigenschaft nicht gibt.
    /// </summary>
    private static PropertyInfo? Eigenschaft(Type datentyp, KiDialogFeld feld)
    {
        if (!string.Equals(feld.Datentyp, datentyp.Name, StringComparison.Ordinal)) return null;

        return datentyp.GetProperty(feld.Eigenschaft,
                                    BindingFlags.Public | BindingFlags.Instance);
    }
}
