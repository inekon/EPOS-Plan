<?php
/**
 * Konto-Startseite (ersetzt myaccount/dashboard.php) — deutscher
 * Begrüßungstext, Kontakt nur per E-Mail.
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

$benutzer = wp_get_current_user();
?>

<p>
	<?php printf( 'Hallo <strong>%s</strong>', esc_html( $benutzer->display_name ) ); ?>
	(nicht <strong><?php echo esc_html( $benutzer->display_name ); ?></strong>?
	<a href="<?php echo esc_url( wc_logout_url() ); ?>">Abmelden</a>)
</p>

<p>
	Hier können Sie Ihre
	<a href="<?php echo esc_url( wc_get_endpoint_url( 'orders' ) ); ?>">Bestellungen</a> einsehen, Ihren
	<a href="<?php echo esc_url( wc_get_endpoint_url( Epos_Woocommerce::ENDPUNKT ) ); ?>">Lizenzschlüssel</a> verwalten, Ihre
	<a href="<?php echo esc_url( wc_get_endpoint_url( 'edit-address' ) ); ?>">Adressen</a> pflegen und Ihre
	<a href="<?php echo esc_url( wc_get_endpoint_url( 'edit-account' ) ); ?>">Kontodetails</a> bearbeiten.
</p>
