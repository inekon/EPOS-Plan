using System.Globalization;
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
    /// <param name="haken">
    /// Was der Dialog über seine Felder hinaus beisteuert (Auftrag #201, Stufe S3):
    /// Auffrischen nach dem Setzen, seine eigene Plausibilitätsprüfung, der Schreibschutz
    /// seines Satzes, sein Speicherweg, seine Rechenwege. <c>null</c> = nichts davon; die
    /// jeweilige Aktion lehnt dann benannt ab, statt still nichts zu tun.
    /// </param>
    /// <returns>
    /// Die Anmeldung — sie wird beim Schließen des Dialogs verworfen. Kennt der Katalog die
    /// Maske nicht, kommt trotzdem ein Objekt zurück; es ist dann nur nicht
    /// <see cref="Angemeldet"/>.
    /// </returns>
    public static KiMaskenanmeldung Fuer<T>(string maskenname, Func<T?> quelle,
                                            KiMaskenhaken? haken = null) where T : class
    {
        if (quelle is null) throw new ArgumentNullException(nameof(quelle));

        KiDialog? eintrag = KiDialoge.Katalog.Finde(maskenname);
        if (eintrag is null) return new KiMaskenanmeldung(maskenname ?? "", null);

        var zugaenge = new List<KiFeldzugang>(eintrag.Felder.Count);
        var spalten = new List<KiFeldsammlung>();

        foreach (KiDialogFeld feld in eintrag.Felder)
        {
            // EINE SPALTE geht einen anderen Weg: Sie loest nicht EINE Eigenschaft an
            // EINEM Objekt auf, sondern je Zeile eine - und das bei jedem Zugriff neu,
            // weil Zeilen entstehen und vergehen, solange der Dialog offen steht.
            if (feld.IstSpalte)
            {
                KiFeldsammlung? spalte = Spalte(feld, quelle);
                if (spalte is not null) spalten.Add(spalte);
                continue;
            }

            PropertyInfo? eigenschaft = Eigenschaft(typeof(T), feld);
            if (eigenschaft is null) continue;

            // Die PropertyInfo steckt im Abschluss - gesucht wird einmal, gelesen oft.
            Func<object?> lesen = () =>
            {
                T? stand = quelle();
                return stand is null ? null : eigenschaft.GetValue(stand);
            };

            // Der Setzer trägt seit Auftrag #201 (Stufe S3) den Setzweg. Er entsteht
            // hier, damit die Setzbarkeit eines Feldes AN DER EIGENSCHAFT haengt und nicht
            // an einer zweiten Liste: Eine abgeleitete Groesse (die Diagnose der Flotte)
            // hat keinen Setzer, und genau das soll der Assistent sehen.
            // `NurLesen` schlägt `CanWrite`: Eine Überschrift und ein fertig
            // formatierter Betrag tragen einen Setzer, weil die Hülle sie füllt — nicht,
            // weil der Anwender sie ändern könnte. Ohne diese Zeile böte der Assistent
            // genau solche Felder zum Setzen an (Auftrag vom 14.09.2026).
            Action<object?>? setzen = eigenschaft.CanWrite && !feld.NurLesen
                ? wert =>
                  {
                      T? stand = quelle();
                      if (stand is not null) eigenschaft.SetValue(stand, wert);
                  }
                : null;

            // Der ZIELTYP geht mit (Auftrag #201): Was aus der Modellantwort kommt, ist
            // Text; was die Eigenschaft annimmt, ist double?, int, bool oder string. Die
            // Umsetzung macht der Kern (KiFeldwandler) - er braucht dafür den Typ, und
            // der steht nur hier fest.
            zugaenge.Add(new KiFeldzugang(feld, lesen, setzen, eigenschaft.PropertyType));
        }

        object? marke = KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, zugaenge, haken, spalten);
        return new KiMaskenanmeldung(eintrag.Maskenname, marke);
    }

    /// <summary>
    /// Die Sammlung hinter einer Spaltendeklaration
    /// (<c>KostenKomponenteStand.Zeilen[].Nutzungsdauer</c>); <c>null</c>, wenn der Pfad
    /// am Daten-Objekt nicht aufloest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zweimal Reflection, beide Male EINMAL.</b> Die Liste (<c>Zeilen</c>) haengt am
    /// Daten-Objekt, die Spalte (<c>Nutzungsdauer</c>) am ZEILENtyp. Beide
    /// <see cref="PropertyInfo"/> werden hier aufgeloest und in den Abschluessen
    /// festgehalten; zur Laufzeit bleibt je Zugriff ein <c>GetValue</c> ohne Suche -
    /// dieselbe Regel wie beim flachen Feld.
    /// </para>
    /// <para>
    /// <b>Der ZEILENTYP kommt aus <c>IEnumerable&lt;X&gt;</c></b> und nicht aus dem ersten
    /// Element: Eine Maske, deren Raster gerade leer ist, soll ihre Spalten trotzdem
    /// anmelden koennen - sonst haette der Assistent sie erst, nachdem der Anwender von
    /// Hand eine Zeile angelegt hat.
    /// </para>
    /// <para>
    /// <b><c>Schreibbar</c> ist Bestandsregel, keine Erfindung.</b> Fuehrt der Zeilentyp
    /// eine Eigenschaft dieses Namens, entscheidet SIE, ob der Assistent in die Zeile
    /// schreiben darf - genau wie sie im Dialog die Eingabefelder sperrt. Fuehrt er
    /// keine, sind alle Zeilen aenderbar.
    /// </para>
    /// </remarks>
    private static KiFeldsammlung? Spalte<T>(KiDialogFeld feld, Func<T?> quelle) where T : class
    {
        if (!string.Equals(feld.Datentyp, typeof(T).Name, StringComparison.Ordinal)) return null;

        PropertyInfo? liste = typeof(T).GetProperty(feld.Sammlung,
                                                    BindingFlags.Public | BindingFlags.Instance);
        if (liste is null) return null;

        Type? zeilentyp = Zeilentyp(liste.PropertyType);
        if (zeilentyp is null) return null;

        PropertyInfo? spalte = zeilentyp.GetProperty(feld.Eigenschaft,
                                                     BindingFlags.Public | BindingFlags.Instance);
        if (spalte is null) return null;

        PropertyInfo? kennzeichen = feld.Zeilenkennzeichen.Length == 0
            ? null
            : zeilentyp.GetProperty(feld.Zeilenkennzeichen, BindingFlags.Public | BindingFlags.Instance);

        PropertyInfo? schreibbar = zeilentyp.GetProperty(SCHREIBBAR,
                                                         BindingFlags.Public | BindingFlags.Instance);
        if (schreibbar is not null && schreibbar.PropertyType != typeof(bool)) schreibbar = null;

        return new KiFeldsammlung(
            feld,
            () =>
            {
                T? stand = quelle();
                return stand is null ? null : Zeilen(liste.GetValue(stand));
            },
            zeile => spalte.GetValue(zeile),
            spalte.CanWrite && !feld.NurLesen
                ? (zeile, wert) => spalte.SetValue(zeile, wert)
                : null,
            kennzeichen is null
                ? null
                : zeile => Convert.ToString(kennzeichen.GetValue(zeile), CultureInfo.CurrentCulture) ?? "",
            schreibbar is null ? null : zeile => (bool)(schreibbar.GetValue(zeile) ?? true),
            spalte.PropertyType);
    }

    /// <summary>Name der Zeileneigenschaft, die den Schreibschutz einer Zeile traegt.</summary>
    private const string SCHREIBBAR = "Schreibbar";

    /// <summary>Der Elementtyp einer <c>IEnumerable&lt;X&gt;</c>; <c>null</c>, wenn es keine ist.</summary>
    private static Type? Zeilentyp(Type listentyp)
    {
        if (listentyp == typeof(string)) return null;

        foreach (Type schnittstelle in listentyp.GetInterfaces())
            if (schnittstelle.IsGenericType &&
                schnittstelle.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return schnittstelle.GetGenericArguments()[0];

        return null;
    }

    /// <summary>Die Zeilen als Liste; nie <c>null</c>.</summary>
    private static IReadOnlyList<object> Zeilen(object? wert)
    {
        if (wert is not System.Collections.IEnumerable folge) return Array.Empty<object>();

        var zeilen = new List<object>();
        foreach (object? glied in folge)
            if (glied is not null) zeilen.Add(glied);

        return zeilen;
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
            if (feld.IstSpalte ? !SpalteLoest(datentyp, feld) : Eigenschaft(datentyp, feld) is null)
                fehlt.Add(feld.Eigenschaftspfad);

        return fehlt;
    }

    /// <summary>
    /// Löst eine SPALTE am Daten-Objekt auf — Typname, Sammlung, Zeilentyp und die
    /// Eigenschaft darin?
    /// </summary>
    /// <remarks>
    /// Das Zeilenkennzeichen wird MITGEPRÜFT: Ein Tippfehler darin fiele sonst nirgends
    /// auf — die Anmeldung fällt still auf die Zeilennummer zurück, und in der
    /// Bestätigung stünde „Nutzungsdauer 3" statt „Nutzungsdauer (Zubehör)". Richtig,
    /// aber nicht das, was der Katalog sagt.
    /// </remarks>
    private static bool SpalteLoest(Type datentyp, KiDialogFeld feld)
    {
        if (!string.Equals(feld.Datentyp, datentyp.Name, StringComparison.Ordinal)) return false;

        PropertyInfo? liste = datentyp.GetProperty(feld.Sammlung,
                                                   BindingFlags.Public | BindingFlags.Instance);
        if (liste is null) return false;

        Type? zeilentyp = Zeilentyp(liste.PropertyType);
        if (zeilentyp is null) return false;

        if (zeilentyp.GetProperty(feld.Eigenschaft, BindingFlags.Public | BindingFlags.Instance) is null)
            return false;

        return feld.Zeilenkennzeichen.Length == 0
            || zeilentyp.GetProperty(feld.Zeilenkennzeichen,
                                     BindingFlags.Public | BindingFlags.Instance) is not null;
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
