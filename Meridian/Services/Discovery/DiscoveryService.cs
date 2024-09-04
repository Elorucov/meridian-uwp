using Meridian.Model.Discovery;
using System.Collections.Generic;
using System.Threading.Tasks;
using VkLib;

namespace Meridian.Services.Discovery
{
    public class DiscoveryService
    {
        private Vk _vk;

        public DiscoveryService(Vk vk)
        {
            _vk = vk;
        }

        public async Task<List<DiscoveryArtist>> SearchArtists(string query)
        {
            return null;
        }

        public async Task<List<DiscoveryAlbum>> SearchAlbums(string query)
        {
            return null;
        }

        public async Task<List<DiscoveryTrack>> GetAlbumTracks(string albumId)
        {
            return null;
        }

        public async Task<List<DiscoveryTrack>> GetArtistTopTracks(string artistId, int count = 0, int offset = 0)
        {
            return null;
        }

        public async Task<List<DiscoveryAlbum>> GetArtistAlbums(string artistId)
        {
            return null;
        }

        public async Task<List<DiscoveryArtist>> GetArtistRelated(string artistId)
        {
            return null;
        }
    }
}