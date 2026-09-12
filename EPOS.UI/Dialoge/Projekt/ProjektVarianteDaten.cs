namespace EPOS.UI.Dialoge.Projekt;

/// <summary>
/// Die ANTWORT des Dialogs „Als Variante speichern" (Auftrag #237, Anwenderwunsch
/// 12.09.2026).
///
/// <para><b>Warum ein Satz und nicht bloss eine Zeichenkette.</b> Bis #237 war die
/// Antwort EIN Text — der Bezeichner —, und <c>NamensDialogHuelle.FragenMitHinweis</c>
/// gab ihn als <c>string?</c> zurueck (<c>null</c> = abgebrochen). Seit dem
/// Anwenderwunsch kann der Anwender zusaetzlich sagen, WELCHES Projekt kopiert werden
/// soll; damit sind es zwei Angaben, die zusammengehoeren. Ein zweiter
/// <c>out</c>-Parameter waere die schlechtere Fassung: Er koennte belegt sein, obwohl
/// das Kaestchen gar nicht angehakt war.</para>
/// </summary>
/// <param name="Bezeichner">Der getrimmte Variantenbezeichner; nie leer.</param>
/// <param name="IdQuelle">
/// Das zu kopierende Projekt, oder <c>0</c> fuer den bisherigen Weg „der Stamm selbst".
/// Nur belegt, wenn das Kaestchen angehakt UND ein Projekt gewaehlt war.
/// </param>
public readonly record struct ProjektVarianteWahl(string Bezeichner, int IdQuelle);
