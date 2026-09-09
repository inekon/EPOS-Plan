<?php
/**
 * Lizenzschlüssel: Erzeugung, Prüfsumme, Parsen, Hash.
 *
 * Format:  EPOS-T-NNNNN-XXXX-XXXX-PP
 *   T     = Typkennung: D (Demo), P (Person), F (Firma)
 *   NNNNN = Lizenznummer (öffentlich, = Post-ID des Lizenzdatensatzes)
 *   XXXX-XXXX = Geheimteil (Zufall, serverseitig nur als Hash gespeichert)
 *   PP    = Prüfzeichen über den ganzen Schlüssel
 *
 * Zeichenvorrat ohne verwechselbare Zeichen (kein O/0, I/1).
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Schluessel {

	const ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // 32 Zeichen

	const TYPEN = array(
		'demo'   => 'D',
		'person' => 'P',
		'firma'  => 'F',
	);

	/**
	 * Neuen Schlüssel für eine Lizenz erzeugen.
	 *
	 * Seit 1.4.1: Ein fehlender oder unbekannter Typ ist ein Fehler, kein leerer
	 * Rumpf. Bis dahin entstand aus einer Lizenz ohne gespeicherten Typ ein Schlüssel
	 * „EPOS--NNNNN-…“, den parsen() bei der Aktivierung als Formatfehler abweist.
	 *
	 * @return array{schluessel:string,geheimteil:string,hash:string}|WP_Error
	 */
	public static function erzeugen( $typ, $lizenz_nummer ) {
		if ( ! is_string( $typ ) || ! isset( self::TYPEN[ $typ ] ) ) {
			return new WP_Error( 'typ', 'Der Lizenztyp fehlt oder ist unbekannt. Bitte die Lizenzdaten mit Typ (Demo, Person oder Firma) speichern und den Schlüssel danach erzeugen.' );
		}
		$kennung = self::TYPEN[ $typ ];
		$geheim  = self::zufallsblock( 4 ) . '-' . self::zufallsblock( 4 );
		$rumpf   = sprintf( 'EPOS-%s-%05d-%s', $kennung, (int) $lizenz_nummer, $geheim );
		$pruef   = self::pruefzeichen( $rumpf );

		return array(
			'schluessel' => $rumpf . '-' . $pruef,
			'geheimteil' => $geheim,
			'hash'       => password_hash( $geheim, PASSWORD_DEFAULT ),
		);
	}

	/**
	 * Schlüssel zerlegen und Prüfsumme kontrollieren.
	 *
	 * @return array{typ:string,nummer:int,geheimteil:string}|WP_Error
	 */
	public static function parsen( $eingabe ) {
		$s = strtoupper( trim( (string) $eingabe ) );
		$s = preg_replace( '/\s+/', '', $s );

		if ( ! preg_match( '/^EPOS-([DPF])-(\d{5})-([A-Z2-9]{4})-([A-Z2-9]{4})-([A-Z2-9]{2})$/', $s, $m ) ) {
			return new WP_Error( 'schluessel_format', 'Der Lizenzschlüssel hat nicht das erwartete Format.' );
		}

		$rumpf = sprintf( 'EPOS-%s-%s-%s-%s', $m[1], $m[2], $m[3], $m[4] );
		if ( self::pruefzeichen( $rumpf ) !== $m[5] ) {
			return new WP_Error( 'schluessel_pruefsumme', 'Der Lizenzschlüssel enthält einen Tippfehler (Prüfsumme falsch).' );
		}

		$typen = array_flip( self::TYPEN );

		return array(
			'typ'        => $typen[ $m[1] ],
			'nummer'     => (int) $m[2],
			'geheimteil' => $m[3] . '-' . $m[4],
		);
	}

	/**
	 * Geheimteil gegen gespeicherten Hash prüfen.
	 */
	public static function pruefen( $geheimteil, $hash ) {
		return is_string( $hash ) && '' !== $hash && password_verify( $geheimteil, $hash );
	}

	/**
	 * Zwei Prüfzeichen über den Schlüsselrumpf (CRC32 → Alphabet).
	 */
	public static function pruefzeichen( $rumpf ) {
		$crc = crc32( $rumpf );
		$a   = self::ALPHABET[ $crc % 32 ];
		$b   = self::ALPHABET[ (int) floor( $crc / 32 ) % 32 ];
		return $a . $b;
	}

	private static function zufallsblock( $laenge ) {
		$out = '';
		for ( $i = 0; $i < $laenge; $i++ ) {
			$out .= self::ALPHABET[ random_int( 0, 31 ) ];
		}
		return $out;
	}
}
