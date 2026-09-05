namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// Was <c>KiEinstellungenDialog</c> nach OK zurueckgibt.
/// </summary>
/// <remarks>
/// <para>
/// Es sind genau die beiden Eigenschaften, die der Vorlaeufer
/// <c>Form_KiEinstellungen</c> herausgab (<c>ApiSchluessel</c> <c>:60</c> und
/// <c>WegBErzwingen</c> <c>:63</c>) - der Dialog SPEICHERT nichts; das Schreiben
/// nach OK und die Rueckmeldung im Chatfenster bleiben beim Aufrufer.
/// </para>
/// <para>
/// <b>Regel S-1.</b> Der Schluessel geht hier heraus und nirgends sonst: Er ist
/// kein Zustand einer Seite, kein Parameter, den ein Bildschirmfoto verträgt, und
/// er wird nicht in eine Browserablage geschrieben.
/// </para>
/// </remarks>
/// <param name="ApiSchluessel">Der eingetippte Schluessel, getrimmt; leer = keiner.</param>
/// <param name="WegBErzwingen">Steht „Rueckfallweg B erzwingen"?</param>
public sealed record KiEinstellungenErgebnis(string ApiSchluessel, bool WegBErzwingen);
