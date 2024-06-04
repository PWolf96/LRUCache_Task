using LRUCache_Task_FINBOURNE.Cache;
using Microsoft.Extensions.Configuration;

namespace LRUCache_UnitTests
{
    public class CacheTests
    {
        private LRUCache<int, string> _cache;

        [SetUp]
        public void Setup()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            _cache = LRUCache<int, string>.GetInstance(configuration);
        }

        [Test]
        public void GetTest_AddItem_ShouldReturnItem()
        {
            _cache.Add(1, "item1", TimeSpan.FromMinutes(1));
            var item = _cache.Get(1);
            Assert.That(item, Is.EqualTo("item1"));
        }

        [Test]
        public void EvictionTest_AddMoreItems_ShouldNotReturnFirstItem()
        {
            //Adding 12 items
            for (int i = 0; i <= 12; i++)
            {
                _cache.Add(i, $"item{i}", TimeSpan.FromMinutes(1));
            }

            //Sleep for 1 second to allow for item to be removed
            Thread.Sleep(1000);
            var item = _cache.Get(0);

            Assert.That(item, Is.EqualTo(default(string)));
        }

        [Test]
        public void TestExpiration_GetItemAfterExpiration_ShouldReturnNull()
        {

            _cache.Add(1, "item1", TimeSpan.FromSeconds(1));

            Thread.Sleep(TimeSpan.FromSeconds(2));

            var item = _cache.Get(1);
            Assert.That(item, Is.EqualTo(default(string)));
        }

        [Test]
        public void TestEvictionEvent_AddMoreItems_ShouldFireEvent()
        {

            bool eventFired = false;

            _cache.OnEviction += (key, value) => { eventFired = true; };

            for (int i = 0; i <= 12; i++)
            {
                _cache.Add(i, $"item{i}", TimeSpan.FromMinutes(1));
            }
            //Sleep for 1 second to allow for item to be removed
            Thread.Sleep(1000);

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void TestCapacity_AddSignificantlyMore_ShouldNormalizeToLowerThreshold()
        {

            for (int i = 0; i < 100; i++)
            {
                _cache.Add(i, $"item{i}", TimeSpan.FromMinutes(1));
            }

            Thread.Sleep(15000);

            Assert.That(_cache.Size(), Is.EqualTo(8));
        }
    }
}