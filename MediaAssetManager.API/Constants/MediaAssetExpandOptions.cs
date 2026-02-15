using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaAssetManager.API.Constants
{
    /// <summary>
    /// Defines the allowed expand options for media asset queries.
    /// </summary>
    public static class MediaAssetExpandOptions
    {
        public const string User = "user";
        public const string VideoMetadata = "videoMetadata";

        /// <summary>
        /// Array of all allowed expand values for validation.
        /// </summary>
        public static readonly string[] All = [User, VideoMetadata];
    }
}