using UnityEngine;

namespace Extreal.Integration.Chat.Vivox
{
    [CreateAssetMenu(
        menuName = "Extreal.Integration/Chat/Vivox/" + nameof(VivoxConnectionConfig),
        fileName = nameof(VivoxConnectionConfig))]
    public class VivoxConnectionConfig : ScriptableObject
    {
#pragma warning disable CC0052
        [SerializeField] private string apiEndPoint = "https://GETFROMPORTAL.www.vivox.com/api2";
        [SerializeField] private string domain = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
        [SerializeField] private string issuer = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
        [SerializeField] private string tokenKey = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
#pragma warning restore CC0052

        public string ApiEndPoint => apiEndPoint;
        public string Domain => domain;
        public string Issuer => issuer;
        public string TokenKey => tokenKey;
    }
}
