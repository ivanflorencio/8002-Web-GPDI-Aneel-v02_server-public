﻿namespace APIGestor.Security
{
    public class User
    {
        public string UserID { get; set; }
        public string Password { get; set; }
    }

    public static class Roles
    {
        public const string ROLE_API_GESTOR = "Acesso-APIGestor";
    }

    public class TokenConfigurations
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int Seconds { get; set; }
    }

    public class Token
    {
        public bool Authenticated { get; set; }
        public string Created { get; set; }
        public string Expiration { get; set; }
        public string AccessToken { get; set; }
        public string Message { get; set; }
    }
}