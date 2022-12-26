using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class ChatConfigProvider : MonoBehaviour
    {
        [SerializeField] private ChatConfig chatConfig;

        public ChatConfig ChatConfig => chatConfig;
    }
}
