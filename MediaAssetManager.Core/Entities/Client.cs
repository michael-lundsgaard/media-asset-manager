using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaAssetManager.Core.Entities
{
    /// <summary>
    /// Client entity - represents an OAuth client application that can authenticate and interact with the API.
    /// </summary>
    /// <remarks>
    /// This is separate from User to allow for multiple clients per user and better security practices (hashed secrets).
    /// </remarks>
    public class Client
    {
        public int ClientId { get; set; }
        public string ClientPublicId { get; set; } = string.Empty; // The actual clientId (GUID)
        public string ClientSecretHash { get; set; } = string.Empty; // BCrypt hashed
        public string ClientName { get; set; } = string.Empty; // "Discord Bot", "Web App"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}