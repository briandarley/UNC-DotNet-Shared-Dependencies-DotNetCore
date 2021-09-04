using System;

namespace UNC.HttpClient.Models
{
    public class TokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
        public DateTime EmpireDateTime => DateTime.Now.AddSeconds(expires_in);
        public override string ToString()
        {
            return $"{token_type} {access_token}";
        }
    }
}
