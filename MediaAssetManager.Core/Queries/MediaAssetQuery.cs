using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaAssetManager.Core.Queries
{
    public class MediaAssetQuery
    {
        public string? FileName { get; set; }
        public string? Title { get; set; }
        public long? MinFileSizeBytes { get; set; }
        public long? MaxFileSizeBytes { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }

        public MediaAssetSortBy SortBy { get; set; } = MediaAssetSortBy.UploadedAt;
        public bool SortDescending { get; set; } = true;

        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }
}
