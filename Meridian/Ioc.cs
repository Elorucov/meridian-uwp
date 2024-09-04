using GalaSoft.MvvmLight.Ioc;
using Meridian.Services.VK;
using System.IO;
using VkLib;
using Windows.Storage;
using Meridian.Services;
using Meridian.Services.Discovery;

namespace Meridian
{
    public class Ioc
    {
        private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Meridian.db");

        public static void Setup()
        {
            //using official VK app for VKMessenger appId and secret (because audio API for this app is available and returns mp3)
            SimpleIoc.Default.Register(() => new Vk(appId: "51453752", clientSecret: "4UyuCUsdK8pVCNoeQuGi", apiVersion: "5.91", userAgent: "com.vk.vkclient/5709 (unknown, iOS 14.6, iPhone9,1, Scale/2.0)"));

            SimpleIoc.Default.Register<VkTracksService>();
            SimpleIoc.Default.Register<VkUserService>();
            SimpleIoc.Default.Register<CacheService>();
            SimpleIoc.Default.Register<ImageService>();
            SimpleIoc.Default.Register<DiscoveryService>();
            SimpleIoc.Default.Register<MusicResolveService>();
            SimpleIoc.Default.Register<ScrobblingService>();
        }

        public static T Resolve<T>()
        {
            return SimpleIoc.Default.GetInstance<T>();
        }
    }
}