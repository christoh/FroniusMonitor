namespace De.Hochstaetter.Fronius.Models.ToshibaAc
{
    public class ToshibaAcDataReceivedEventArgs : EventArgs
    {
        public ToshibaAcDataReceivedEventArgs(IReadOnlyList<ToshibaAcMapping> mapping)
        {
            Mapping = mapping;
        }

        public IReadOnlyList<ToshibaAcMapping> Mapping { get; }
    }
}
