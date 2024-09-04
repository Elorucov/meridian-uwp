using Meridian.Interfaces;
using Meridian.Model;
using Meridian.Utils.Helpers;
using System;
using System.Threading.Tasks;
using VkLib;

namespace Meridian.Services
{
    public class ScrobblingService
    {
        private Vk _vk;

        public ScrobblingService(Vk vk)
        {
            _vk = vk;
        }

        public async Task SetMusicStatus(AudioVk audio)
        {
            long aid = 0;
            long oid = 0;
            if (audio != null)
            {
                aid = long.Parse(audio.Id);
                oid = long.Parse(audio.OwnerId);
            }

            var result = await _vk.Audio.SetBroadcast(aid, oid);
        }
    }
}
