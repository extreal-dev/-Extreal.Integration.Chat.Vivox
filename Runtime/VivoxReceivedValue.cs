namespace Extreal.Integration.Chat.Vivox
{
    public struct VivoxReceivedValue<T>
    {
        public string AccountName { get; }
        public string ChannelName { get; }
        public T ReceivedValue { get; }

        public VivoxReceivedValue(string accountName, string channelName, T receivedValue)
        {
            AccountName = accountName;
            ChannelName = channelName;
            ReceivedValue = receivedValue;
        }
    }
}
