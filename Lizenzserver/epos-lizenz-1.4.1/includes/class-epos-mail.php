<?php
/**
 * E-Mail-Versand des Lizenzschlüssels (mit angehängter .lic-Datei).
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Mail {

	/**
	 * Lizenzschlüssel per E-Mail zustellen; die .lic-Datei liegt bei.
	 */
	public static function schluessel_senden( $email, $schluessel, $firma, $ist_trial = false ) {
		$betreff = $ist_trial
			? 'Ihr EPOS-Plan Test-Lizenzschlüssel'
			: 'Ihr EPOS-Plan Lizenzschlüssel';

		$portal = home_url( '/' );

		$text  = "Guten Tag,\n\n";
		$text .= $ist_trial
			? "vielen Dank für Ihr Interesse an EPOS-Plan. Ihr Test-Lizenzschlüssel lautet:\n\n"
			: "anbei Ihr Lizenzschlüssel für EPOS-Plan:\n\n";
		$text .= "    " . $schluessel . "\n\n";
		$text .= "Aktivierung: EPOS-Plan starten, Menü Lizenz wählen und den Schlüssel eingeben —\n";
		$text .= "oder einfach die angehängte Lizenzdatei (EPOS-Plan.lic) laden bzw. doppelklicken.\n\n";
		$text .= "Bitte bewahren Sie diesen Schlüssel gut auf: Aus Sicherheitsgründen wird er auf dem\n";
		$text .= "Server nicht im Klartext gespeichert und kann nicht erneut zugestellt werden.\n";
		$text .= "Bei Verlust können Sie im Lizenzportal jederzeit einen neuen erzeugen:\n";
		$text .= $portal . "\n\n";
		$text .= "Mit freundlichen Grüßen\nINEKON GmbH · EPOS-Plan\n";

		$anhaenge = array();
		$lic      = Epos_Token::lic_datei( $schluessel, $firma, $email );
		if ( ! is_wp_error( $lic ) ) {
			$tmp = wp_tempnam( 'EPOS-Plan.lic' );
			if ( $tmp ) {
				file_put_contents( $tmp, $lic );
				$ziel = dirname( $tmp ) . '/EPOS-Plan.lic';
				if ( @rename( $tmp, $ziel ) ) {
					$anhaenge[] = $ziel;
				} else {
					$anhaenge[] = $tmp;
				}
			}
		}

		$ergebnis = wp_mail( $email, $betreff, $text, array(), $anhaenge );

		foreach ( $anhaenge as $datei ) {
			@unlink( $datei );
		}

		return $ergebnis;
	}
}
