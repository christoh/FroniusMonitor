namespace De.Hochstaetter.HomeAutomationServer.Crypto
{
    internal class AesKeyProvider : IAesKeyProvider
    {
        public byte[] GetAesKey() => new byte[16];
    }
}
