namespace E_learning.Model.cloudeDB
{
    public class RedisModel
    {
        public string key;
        public string value;
        public TimeSpan expirationInSeconds; 
        public RedisModel(string key, string value, TimeSpan expirationInSeconds)
        {
            this.key = key;
            this.value = value;
            this.expirationInSeconds = expirationInSeconds;
        }
        public RedisModel() { }
    }
}
