namespace Extreal.Integration.Chat.Vivox
{
    public struct VivoxReceivedValue<T>
    {
        public string UserId { get; }
        public string ChannelName { get; }
        public T ReceivedValue { get; }

        public VivoxReceivedValue(string userId, string channelName, T receivedValue)
        {
            UserId = userId;
            ChannelName = channelName;
            ReceivedValue = receivedValue;
        }
    }
}
