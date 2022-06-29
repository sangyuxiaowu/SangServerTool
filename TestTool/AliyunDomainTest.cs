
using Microsoft.Extensions.Configuration;
using SangServerTool.Domain;

namespace TestTool
{
    public class AliyunDomainTest
    {
        private AliyunDomain _al;
        private IConfigurationRoot config;
        [SetUp]
        public void Setup()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddUserSecrets<AliyunDomainTest>();
            config = configBuilder.Build();
            _al = new AliyunDomain(config["AK"], config["SK"]);
        }

        [Test]
        public async Task GetRecords_Test()
        {
            var s = await _al.GetRecordsAsync(config["TEST_TXT"], "TXT");
            Assert.IsNotNull(s);
        }
    }
}