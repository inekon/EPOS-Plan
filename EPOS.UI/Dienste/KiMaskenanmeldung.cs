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
/// hält die Liste für jede Maske des Katalogs leer. Ein Dialog soll an einem Tippfehler im Katalog
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
    /// <param name="wahlquellen">
    /// Je WAHLFELD ein Lieferant seiner Einträge (KI-F1b, KI-D-Q6) — für Listen, die
    /// nur der Dialog kennt (die Energieträgervarianten des Heizkessels, die Geräte
    /// eines Katalogs). Fehlt einer, sucht die Anmeldung die Begleiteigenschaft
    /// <c>&lt;Eigenschaft&gt;Wahl</c> am Daten-Objekt.
    /// </param>
    public static KiMaskenanmeldung Fuer<T>(string maskenname, Func<T?> quelle,
                                            KiMaskenhaken? haken = null,
                                            params (string Feld, Func<IReadOnlyList<KiWahleintrag>> Eintraege)[] wahlquellen)
        where T : class
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
                KiFeldsammlung? spalte = Spalte(feld, quelle, wahlquellen);
                if (spalte is not null) spalten.Add(spalte);
                continue;
            }

            PropertyInfo? eigenschaft = Eigenschaft(typeof(T), feld);
            if (eigenschaft is null)
            {
                // DIE FELDTAFEL (Welle #456): Das Daten-Objekt fuehrt seine Felder als
                // Daten, und der Teil nach dem Punkt ist ihr Schluessel. Benannte
                // Eigenschaften gehen vor - erst was per Reflection nicht aufloest,
                // fragt die Tafel.
                KiFeldzugang? tafel = Tafelzugang(feld, quelle, wahlquellen);
                if (tafel is not null) zugaenge.Add(tafel);
                continue;
            }

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
                      if (stand is not null) Setze(eigenschaft, stand, wert);
                  }
                : null;

            // Der ZIELTYP geht mit (Auftrag #201): Was aus der Modellantwort kommt, ist
            // Text; was die Eigenschaft annimmt, ist double?, int, bool oder string. Die
            // Umsetzung macht der Kern (KiFeldwandler) - er braucht dafür den Typ, und
            // der steht nur hier fest.
            zugaenge.Add(new KiFeldzugang(feld, lesen, setzen, eigenschaft.PropertyType,
                                          Wahlquelle(feld, quelle, wahlquellen)));
        }

        object? marke = KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, zugaenge, haken, spalten);
        return new KiMaskenanmeldung(eintrag.Maskenname, marke);
    }

    /// <summary>
    /// Der Zugang zu einem Feld einer <see cref="IKiFeldtafel"/>; <c>null</c>, wenn das
    /// Daten-Objekt keine Tafel ist oder der Typname vor dem Punkt nicht passt.
    /// </summary>
    /// <remarks>
    /// <para><b>Der Zieltyp kommt aus dem KATALOG</b> (<see cref="Tafeltyp"/>): Eine Tafel
    /// hat keine Eigenschaft, deren <c>PropertyType</c> ihn verriete. Die Deklaration
    /// sagt ohnehin, was das Feld ist — Zahl, Ganzzahl, Wahrheitswert oder Text.</para>
    /// <para><b>Nur lesbar bleibt nur lesbar</b>: Ein Feld, das der Katalog
    /// <c>nurLesen</c> deklariert, bekommt keinen Setzer — dieselbe Regel wie beim
    /// Reflection-Weg. Die Tafel sichert es ein zweites Mal ab.</para>
    /// </remarks>
    private static KiFeldzugang? Tafelzugang<T>(
        KiDialogFeld feld, Func<T?> quelle,
        (string Feld, Func<IReadOnlyList<KiWahleintrag>> Eintraege)[] wahlquellen) where T : class
    {
        if (!IstTafel(typeof(T), feld)) return null;

        string schluessel = feld.Eigenschaft;

        Func<object?> lesen = () => quelle() is IKiFeldtafel tafel ? tafel.Lesen(schluessel) : null;

        Action<object?>? setzen = feld.NurLesen
            ? null
            : wert =>
              {
                  if (quelle() is IKiFeldtafel tafel) tafel.Setzen(schluessel, wert);
              };

        return new KiFeldzugang(feld, lesen, setzen, Tafeltyp(feld.Typ),
                                Wahlquelle(feld, quelle, wahlquellen));
    }

    /// <summary>
    /// Setzt eine Eigenschaft per Reflection — und reicht eine ABLEHNUNG des Setzers
    /// ungewickelt weiter.
    /// </summary>
    /// <remarks>
    /// <b>Warum nicht <c>SetValue</c> allein</b> (Welle #456): Die Reflection wickelt jede
    /// Ausnahme des Setzers in eine <see cref="TargetInvocationException"/>, und deren
    /// Meldung lautet „Exception has been thrown by the target of an invocation". Genau
    /// die stünde dann in der Absage des Assistenten — statt des Grundes, den der Dialog
    /// nennt („erst speichern oder verwerfen").
    /// </remarks>
    private static void Setze(PropertyInfo eigenschaft, object ziel, object? wert)
    {
        try
        {
            eigenschaft.SetValue(ziel, wert);
        }
        catch (TargetInvocationException huelle) when (huelle.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(huelle.InnerException).Throw();
        }
    }

    /// <summary>Löst dieses Feld über die Feldtafel des Daten-Objekts auf?</summary>
    private static bool IstTafel(Type datentyp, KiDialogFeld feld)
        => typeof(IKiFeldtafel).IsAssignableFrom(datentyp)
           && string.Equals(feld.Datentyp, datentyp.Name, StringComparison.Ordinal)
           && feld.Eigenschaft.Length > 0;

    /// <summary>
    /// Der CLR-Typ, den ein Tafelfeld beim Setzen annimmt — aus dem Feldtyp des Katalogs.
    /// </summary>
    public static Type Tafeltyp(KiParameterTyp typ) => typ switch
    {
        KiParameterTyp.Zahl => typeof(double?),
        KiParameterTyp.Ganzzahl => typeof(int?),
        KiParameterTyp.Wahrheitswert => typeof(bool),
        _ => typeof(string)
    };

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
    private static KiFeldsammlung? Spalte<T>(
        KiDialogFeld feld, Func<T?> quelle,
        (string Feld, Func<IReadOnlyList<KiWahleintrag>> Eintraege)[] wahlquellen) where T : class
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
                ? (zeile, wert) => Setze(spalte, zeile, wert)
                : null,
            kennzeichen is null
                ? null
                : zeile => Convert.ToString(kennzeichen.GetValue(zeile), CultureInfo.CurrentCulture) ?? "",
            schreibbar is null ? null : zeile => (bool)(schreibbar.GetValue(zeile) ?? true),
            spalte.PropertyType,
            Wahlquelle(feld, quelle, wahlquellen));
    }

    /// <summary>
    /// Der Lieferant der Einträge eines WAHLFELDES (KI-F1b, KI-D-Q6); <c>null</c>, wenn
    /// das Feld keine Wahl ist oder keine Quelle auflöst.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zwei Wege, und der ausdrückliche geht vor.</b> Steht die Liste am Daten-Objekt
    /// (eine Sichtklasse führt neben <c>Bodentyp</c> die Eigenschaft
    /// <c>BodentypWahl</c>), findet die Anmeldung sie von selbst — das ist der Regelfall
    /// und braucht im Dialog keine Zeile. Kennt die Liste dagegen nur der Dialog (die
    /// Energieträgervarianten des Heizkessels stehen in einem privaten Feld), reicht er
    /// sie beim Anmelden als Lieferant herein.
    /// </para>
    /// <para>
    /// <b>Gerufen wird bei JEDEM Zugriff</b>, nie einmal beim Anmelden: Eine
    /// Gerätewahl hängt am gewählten Hersteller, eine Energieträgerliste am Gewerk. Eine
    /// eingefrorene Liste zeigte dem Assistenten die Auswahl von vorhin.
    /// </para>
    /// </remarks>
    private static Func<IReadOnlyList<KiWahleintrag>>? Wahlquelle<T>(
        KiDialogFeld feld, Func<T?> quelle,
        (string Feld, Func<IReadOnlyList<KiWahleintrag>> Eintraege)[] wahlquellen) where T : class
    {
        if (!feld.IstWahl) return null;

        if (wahlquellen is not null)
            foreach (var paar in wahlquellen)
                if (paar.Eintraege is not null &&
                    string.Equals(paar.Feld, feld.Name, StringComparison.Ordinal))
                    return paar.Eintraege;

        PropertyInfo? begleiter = Begleiter(typeof(T), feld);
        if (begleiter is null) return null;

        return () =>
        {
            T? stand = quelle();
            return stand is null
                       ? Array.Empty<KiWahleintrag>()
                       : begleiter.GetValue(stand) as IReadOnlyList<KiWahleintrag>
                         ?? Array.Empty<KiWahleintrag>();
        };
    }

    /// <summary>
    /// Die Begleiteigenschaft <c>&lt;Eigenschaft&gt;Wahl</c> am Daten-Objekt, wenn sie
    /// eine Eintragsliste liefert; sonst <c>null</c>.
    /// </summary>
    private static PropertyInfo? Begleiter(Type datentyp, KiDialogFeld feld)
    {
        PropertyInfo? begleiter = datentyp.GetProperty(feld.Eigenschaft + WAHLZUSATZ,
                                                       BindingFlags.Public | BindingFlags.Instance);

        return begleiter is not null &&
               typeof(IReadOnlyList<KiWahleintrag>).IsAssignableFrom(begleiter.PropertyType)
                   ? begleiter
                   : null;
    }

    /// <summary>Nachsilbe der Begleiteigenschaft eines Wahlfeldes.</summary>
    private const string WAHLZUSATZ = "Wahl";

    /// <summary>
    /// Baut aus einer Liste der Maske die Einträge eines Wahlfeldes (KI-F1b).
    /// </summary>
    /// <remarks>
    /// Sie steht hier und nicht in jedem Dialog, weil sonst jede Maske ihre eigene
    /// Umsetzung von „Id und Text" schriebe — und die Nullprüfung je Mal neu.
    /// </remarks>
    public static IReadOnlyList<KiWahleintrag> Eintraege<TZeile>(
        IEnumerable<TZeile>? zeilen,
        Func<TZeile, object?> schluessel,
        Func<TZeile, string?> text)
    {
        if (zeilen is null) return Array.Empty<KiWahleintrag>();

        var liste = new List<KiWahleintrag>();
        foreach (TZeile zeile in zeilen)
        {
            if (zeile is null) continue;
            liste.Add(new KiWahleintrag(
                Convert.ToString(schluessel(zeile), CultureInfo.InvariantCulture),
                text(zeile)));
        }

        return liste;
    }

    /// <summary>
    /// Die Einträge einer Liste, deren Schlüssel ZUGLEICH der angezeigte Text ist
    /// (KI-F1b) — Steuerwerte, Baujahre, Vorlaufstufen.
    /// </summary>
    public static IReadOnlyList<KiWahleintrag> Eintraege<TZeile>(
        IEnumerable<TZeile>? zeilen, Func<TZeile, object?> schluessel)
        => Eintraege(zeilen, schluessel, _ => null);

    /// <summary>
    /// Die Einträge einer Liste, deren SCHLÜSSEL der Listenplatz ist — die Bauform der
    /// festen Klapplisten des Hauses (KI-F1b).
    /// </summary>
    public static IReadOnlyList<KiWahleintrag> Eintraege(IReadOnlyList<string>? texte)
    {
        if (texte is null) return Array.Empty<KiWahleintrag>();

        var liste = new List<KiWahleintrag>(texte.Count);
        for (int i = 0; i < texte.Count; i++)
            liste.Add(new KiWahleintrag(i.ToString(CultureInfo.InvariantCulture), texte[i]));

        return liste;
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
    public static IReadOnlyList<string> Pruefe(string maskenname, Type datentyp,
                                               params string[] wahlquellen)
    {
        if (datentyp is null) throw new ArgumentNullException(nameof(datentyp));

        KiDialog? eintrag = KiDialoge.Katalog.Finde(maskenname);
        if (eintrag is null) return new[] { "Maske '" + (maskenname ?? "") + "' steht nicht im Katalog." };

        var fehlt = new List<string>();

        foreach (KiDialogFeld feld in eintrag.Felder)
        {
            // Ein Feld einer FELDTAFEL loest zur Laufzeit ueber ihren Schluessel auf; hier
            // steht nur die Typprobe vor dem Punkt. Den Feldbestand haelt der Waechter
            // der Tafel (Welle #456: „die Verwaltung deklariert genau die Felder ihres
            // Profils"), nicht die Reflection.
            bool loest = feld.IstSpalte
                ? SpalteLoest(datentyp, feld)
                : Eigenschaft(datentyp, feld) is not null || IstTafel(datentyp, feld);

            if (!loest)
            {
                fehlt.Add(feld.Eigenschaftspfad);
                continue;
            }

            // JEDES Wahlfeld muss seine Eintragsquelle aufloesen (KI-F1b, KI-D-Q6):
            // entweder die Begleiteigenschaft am Daten-Objekt oder ein Lieferant, den
            // der Dialog beim Anmelden hereinreicht. Ohne Quelle stuende ein Feld im
            // Katalog, das der Assistent lesen, aber nie setzen koennte.
            if (feld.IstWahl && Begleiter(datentyp, feld) is null && !Genannt(wahlquellen, feld.Name))
                fehlt.Add(feld.Eigenschaftspfad + " (" + WAHLZUSATZ + ")");
        }

        return fehlt;
    }

    /// <summary>Steht der Feldname unter den ausdrücklich gemeldeten Lieferanten?</summary>
    private static bool Genannt(string[] wahlquellen, string feldname)
    {
        if (wahlquellen is null) return false;

        foreach (string name in wahlquellen)
            if (string.Equals(name, feldname, StringComparison.Ordinal)) return true;

        return false;
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
