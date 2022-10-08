namespace De.Hochstaetter.Fronius.Models.Wifi;

[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum WifiEncryption
{
    None = 0,
    Wep,
    WpaPsk,
    Wpa2Psk,
    WpaWpa2Psk,
    Wpa2Enterprise,
    Wpa3Psk,
    Wpa2Wpa3Psk,
    WapiPsk,
    Max
}

[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "CommentTypo")]
public enum WifiCipher
{
    None = 0,
    Wep40, /*< the cipher type is WEP40 */
    Wep104, /*< the cipher type is WEP104 */
    Tkip, /*< the cipher type is TKIP */
    Ccmp, /*< the cipher type is CCMP */
    TkipCcmp, /*< the cipher type is TKIP and CCMP */
    AesCmac128, /*< the cipher type is AES-CMAC-128 */
    Sms4, /*< the cipher type is SMS4 */
    Gcmp, /*< the cipher type is GCMP */
    Gcmp256, /*< the cipher type is GCMP-256 */
    AesGmac128, /*< the cipher type is AES-GMAC-128 */
    AesGmac256, /*< the cipher type is AES-GMAC-256 */
    Unknown, /*< the cipher type is unknown */
}