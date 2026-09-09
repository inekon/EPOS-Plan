<?php
/**
 * Benutzerprofil-Felder für Rechnung und Softwarelizenz.
 *
 * Ergänzt das Konto-Formular (WooCommerce "Kontodetails", von Avada auf den
 * Konto-Seiten gerendert) um Firma und Rechnungsangaben. Gespeichert wird in
 * den WooCommerce-Rechnungsfeldern (billing_*), damit die Daten ohne weitere
 * Zuordnung in Bestellungen und Rechnungen zur Verfügung stehen.
 * Dieselben Felder erscheinen zusätzlich im wp-admin-Benutzerprofil.
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Profil {

	/** Felddefinitionen: Meta-Key => [Beschriftung, Pflicht, Typ] */
	public static function felder() {
		return array(
			'billing_company'   => array( 'Firmenname', false, 'text' ),
			'billing_vat_id'    => array( 'USt-IdNr.', false, 'text' ),
			'billing_address_1' => array( 'Straße und Hausnummer', false, 'text' ),
			'billing_postcode'  => array( 'PLZ', false, 'text' ),
			'billing_city'      => array( 'Ort', false, 'text' ),
			'billing_country'   => array( 'Land', false, 'land' ),
			'billing_phone'     => array( 'Telefon', false, 'tel' ),
		);
	}

	public static function init() {
		// Frontend: Ultimate-Member-Konto-Seite (/account/, Tab "Konto")
		add_action( 'um_after_account_general', array( __CLASS__, 'um_felder' ), 100 );
		add_action( 'um_account_pre_update_profile', array( __CLASS__, 'um_speichern' ), 10, 2 );

		// Frontend: WooCommerce-Kontodetails-Formular (/mein-konto/)
		add_action( 'woocommerce_edit_account_form', array( __CLASS__, 'frontend_felder' ) );
		add_action( 'woocommerce_save_account_details', array( __CLASS__, 'frontend_speichern' ) );

		// wp-admin: Benutzerprofil
		add_action( 'show_user_profile', array( __CLASS__, 'admin_felder' ) );
		add_action( 'edit_user_profile', array( __CLASS__, 'admin_felder' ) );
		add_action( 'personal_options_update', array( __CLASS__, 'admin_speichern' ) );
		add_action( 'edit_user_profile_update', array( __CLASS__, 'admin_speichern' ) );
	}

	/* ------------------------------------------------- Frontend (Ultimate Member) */

	/** Felder im Tab "Konto" der UM-Konto-Seite ausgeben. */
	public static function um_felder() {
		$user_id = get_current_user_id();
		echo '<div class="um-field" style="margin-top:1.2em"><strong>Firma und Rechnungsangaben</strong>'
			. '<div style="font-size:0.9em;color:#666;margin:4px 0 10px">Optional — diese Angaben werden für Rechnungen und die Software-Lizenz verwendet.</div></div>';

		foreach ( self::felder() as $key => $feld ) {
			list( $beschriftung, , $typ ) = $feld;
			$wert = (string) get_user_meta( $user_id, $key, true );

			echo '<div class="um-field um-field-text" style="margin-bottom:12px">';
			printf( '<div class="um-field-label"><label for="%1$s">%2$s</label></div>', esc_attr( $key ), esc_html( $beschriftung ) );
			echo '<div class="um-field-area">';
			if ( 'land' === $typ ) {
				echo self::land_auswahl( $key, $wert ); // phpcs:ignore
			} else {
				printf(
					'<input type="%1$s" class="um-form-field" name="%2$s" id="%2$s" value="%3$s" autocomplete="off" style="width:100%%">',
					esc_attr( 'tel' === $typ ? 'tel' : 'text' ),
					esc_attr( $key ),
					esc_attr( $wert )
				);
			}
			echo '</div></div>';
		}
	}

	/** Beim Speichern des Tabs "Konto" mitschreiben. */
	public static function um_speichern( $changes, $user_id ) {
		self::speichern( $user_id );
	}

	/* ------------------------------------------------- Frontend (WooCommerce) */

	public static function frontend_felder() {
		$user_id = get_current_user_id();
		echo '<fieldset style="margin-top:1.5em"><legend>Firma und Rechnungsangaben</legend>';
		echo '<p style="font-size:0.9em;color:#666">Optional — diese Angaben werden für Rechnungen und die Software-Lizenz verwendet.</p>';

		foreach ( self::felder() as $key => $feld ) {
			list( $beschriftung, $pflicht, $typ ) = $feld;
			$wert = (string) get_user_meta( $user_id, $key, true );

			echo '<p class="woocommerce-form-row woocommerce-form-row--wide form-row form-row-wide">';
			printf( '<label for="%1$s">%2$s%3$s</label>', esc_attr( $key ), esc_html( $beschriftung ), $pflicht ? ' <span class="required">*</span>' : '' );

			if ( 'land' === $typ ) {
				echo self::land_auswahl( $key, $wert ); // phpcs:ignore
			} else {
				printf(
					'<input type="%1$s" class="woocommerce-Input woocommerce-Input--text input-text" name="%2$s" id="%2$s" value="%3$s" autocomplete="off">',
					esc_attr( 'tel' === $typ ? 'tel' : 'text' ),
					esc_attr( $key ),
					esc_attr( $wert )
				);
			}
			echo '</p>';
		}
		echo '</fieldset>';
	}

	public static function frontend_speichern( $user_id ) {
		self::speichern( $user_id );
	}

	/* ------------------------------------------------- wp-admin-Profil */

	/**
	 * Felder, die WooCommerce im wp-admin-Profil selbst ausgibt.
	 * Sie werden hier nicht ein zweites Mal gerendert, weil bei gleichnamigen
	 * Formularfeldern nur der zuletzt uebertragene Wert ankommt und der
	 * WooCommerce-Block weiter unten steht.
	 */
	private static function wc_admin_doppelfelder() {
		return array( 'billing_company', 'billing_address_1', 'billing_postcode', 'billing_city', 'billing_country', 'billing_phone' );
	}

	/** Felddefinitionen fuer das wp-admin-Profil, ohne die WooCommerce-Doppelfelder. */
	private static function admin_feldliste() {
		$felder = self::felder();
		if ( class_exists( 'WooCommerce' ) && current_user_can( 'manage_woocommerce' ) ) {
			foreach ( self::wc_admin_doppelfelder() as $key ) {
				unset( $felder[ $key ] );
			}
		}
		return $felder;
	}

	public static function admin_felder( $user ) {
		$felder = self::admin_feldliste();
		if ( empty( $felder ) ) {
			return;
		}
		echo '<h2>EPOS-Plan — Firma und Rechnungsangaben</h2>';
		if ( count( $felder ) < count( self::felder() ) ) {
			echo '<p class="description">Firmenname, Adresse, PLZ, Ort, Land und Telefon werden weiter unten im Abschnitt &bdquo;Rechnungsadresse des Kunden&ldquo; gepflegt.</p>';
		}
		echo '<table class="form-table">';
		wp_nonce_field( 'epos_profil', 'epos_profil_nonce' );
		foreach ( $felder as $key => $feld ) {
			list( $beschriftung, , $typ ) = $feld;
			$wert = (string) get_user_meta( $user->ID, $key, true );
			echo '<tr><th><label for="' . esc_attr( $key ) . '">' . esc_html( $beschriftung ) . '</label></th><td>';
			if ( 'land' === $typ ) {
				echo self::land_auswahl( $key, $wert ); // phpcs:ignore
			} else {
				printf( '<input type="text" class="regular-text" name="%1$s" id="%1$s" value="%2$s">', esc_attr( $key ), esc_attr( $wert ) );
			}
			echo '</td></tr>';
		}
		echo '</table>';
	}

	public static function admin_speichern( $user_id ) {
		if ( ! current_user_can( 'edit_user', $user_id ) ) {
			return;
		}
		if ( ! isset( $_POST['epos_profil_nonce'] ) || ! wp_verify_nonce( $_POST['epos_profil_nonce'], 'epos_profil' ) ) {
			return;
		}
		self::speichern( $user_id, self::admin_feldliste() );
	}

	/* ------------------------------------------------- gemeinsam */

	private static function speichern( $user_id, $felder = null ) {
		if ( null === $felder ) {
			$felder = self::felder();
		}
		foreach ( $felder as $key => $feld ) {
			if ( ! isset( $_POST[ $key ] ) ) {
				continue;
			}
			$wert = sanitize_text_field( wp_unslash( $_POST[ $key ] ) );
			if ( 'billing_country' === $key ) {
				$wert = strtoupper( substr( preg_replace( '/[^a-zA-Z]/', '', $wert ), 0, 2 ) );
			}
			update_user_meta( $user_id, $key, $wert );
		}
	}

	/**
	 * Länderauswahl: WooCommerce-Länderliste, falls verfügbar; sonst eine
	 * kurze Liste der relevanten Länder. Gespeichert wird der ISO-Code.
	 */
	private static function land_auswahl( $name, $wert ) {
		$laender = array( 'DE' => 'Deutschland', 'AT' => 'Österreich', 'CH' => 'Schweiz', 'LU' => 'Luxemburg', 'NL' => 'Niederlande', 'FR' => 'Frankreich', 'BE' => 'Belgien', 'DK' => 'Dänemark', 'PL' => 'Polen', 'CZ' => 'Tschechien' );
		if ( function_exists( 'WC' ) && WC()->countries ) {
			$alle = WC()->countries->get_countries();
			if ( is_array( $alle ) && ! empty( $alle ) ) {
				$laender = $alle;
			}
		}
		if ( '' === $wert ) {
			$wert = 'DE';
		}
		$html = '<select name="' . esc_attr( $name ) . '" id="' . esc_attr( $name ) . '">';
		foreach ( $laender as $code => $land ) {
			$html .= '<option value="' . esc_attr( $code ) . '"' . selected( $wert, $code, false ) . '>' . esc_html( $land ) . '</option>';
		}
		return $html . '</select>';
	}
}
