namespace Bit.Notifications;

public interface IHubConnectionManager
{
    void AddConnection(string key, string value, double clockDriftAdjustment);
    void RemoveConnection(string key);
    void RemoveConnectionByValue(string value);
    string FindValueByKey(string key);
    string FindKeyByValue(string value);
    List<string> GetAllKeys();
    bool HasKey(string key);
}
