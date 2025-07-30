namespace E_learning.Model.cloudeDB
{
    public class GoogleModel
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }

        public GoogleModel(string clientId, string clientSecret, string redirectUri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            RedirectUri = redirectUri;
        }
        public GoogleModel()
        {
            // Default constructor for deserialization
        }
    }
}
