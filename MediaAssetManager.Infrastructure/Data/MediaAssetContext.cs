using Microsoft.EntityFrameworkCore;

namespace MediaAssetManager.Infrastructure.Data
{
    public class MediaAssetContext : DbContext
    {
        public MediaAssetContext(DbContextOptions<MediaAssetContext> options)
            : base(options)
        {
        }
    }
}
