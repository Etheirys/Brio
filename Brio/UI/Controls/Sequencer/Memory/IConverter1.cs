namespace ImSequencer.Memory
{
    public interface IConverter<TIn, TOut>
    {
        public TOut Convert(TIn value);
    }
}