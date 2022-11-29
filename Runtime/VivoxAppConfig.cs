using UnityEngine;

namespace Extreal.Integration.Chat.Vivox
{
    [CreateAssetMenu(
        menuName = "Extreal.Integration/Chat/Vivox/" + nameof(VivoxAppConfig),
        fileName = nameof(VivoxAppConfig))]
    public class VivoxAppConfig : ScriptableObject
    {
        [SerializeField] private string apiEndPoint;
        [SerializeField] private string domain;
        [SerializeField] private string issuer;
        [SerializeField] private string tokenKey;

        public string ApiEndPoint => apiEndPoint;
        public string Domain => domain;
        public string Issuer => issuer;
        public string TokenKey => tokenKey;
    }
}
