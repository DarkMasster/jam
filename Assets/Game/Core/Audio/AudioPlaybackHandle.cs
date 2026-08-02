namespace Jam.Core.Audio
{
    public readonly struct AudioPlaybackHandle
    {
        public AudioPlaybackHandle(int id)
        {
            Id = id;
        }

        public int Id { get; }
        public bool IsValid => Id > 0;
    }
}
