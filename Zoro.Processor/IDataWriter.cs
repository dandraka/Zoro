namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Writes masked data of type <typeparamref name="T"/> to a destination.
    /// </summary>
    internal interface IDataWriter<in T>
    {
        void Write(T data);
    }
}
