//using Bit.Core.Models;
using System.Collections.Concurrent;

namespace Bit.Notifications;
public class FileNameGenerator
{
    public static string SanitizeFileName(string name)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        string validName = string.Concat(name.Where(ch => !invalidChars.Contains(ch)));

        // If the filename is empty after removing invalid characters, use a default name
        if (string.IsNullOrWhiteSpace(validName))
        {
            validName = "DefaultFileName";
        }

        return validName;
    }
}
public enum HubConnectionManagerFileAction
{
    DeleteFile,
    RenameFile,
    DoNothing
}

public class HubConnectionManager: IHubConnectionManager
{
    //private string _baseNotificationsUrl;
    private string _baseDirPath;
    private string _baseTempDirPath;

    private readonly ConcurrentDictionary<string, ConnectionInfo> keyToValue;
    private readonly ConcurrentDictionary<string, string> valueToKey;
    private readonly object lockObject = new object();

    //public System.Collections.Concurrent.ConcurrentStack<PushNotificationData<AuthRequestPushNotification>> _notificationStack =
    //   new System.Collections.Concurrent.ConcurrentStack<PushNotificationData<AuthRequestPushNotification>>();

    public HubConnectionManager()
    {
        keyToValue = new ConcurrentDictionary<string, ConnectionInfo>();
        valueToKey = new ConcurrentDictionary<string, string>();
    }
    public Core.Settings.GlobalSettings GlobalSettings { get; set; }

    public void AddConnection(string key, string value, double clockDriftAdjust=0)
    {
        var l_time = DateTime.UtcNow.AddSeconds(clockDriftAdjust);
        var connectionInfo = new ConnectionInfo(value, l_time);
        lock (lockObject)
        {
            keyToValue.TryAdd(key, connectionInfo);
            valueToKey.TryAdd(value, key);
        }

        Console.WriteLine("HubConnectionManager::AddConnection ... {0} with token {1}  :: {2}", key, value, l_time);
    }

    public void RemoveConnection(string key)
    {
        lock (lockObject)
        {
            if (keyToValue.TryRemove(key, out ConnectionInfo connectionInfo))
            {
                valueToKey.TryRemove(connectionInfo.ConnectionId, out _);
            }
        }
    }

    public void RemoveConnectionByValue(string value)
    {
        lock (lockObject)
        {
            if (valueToKey.TryRemove(value, out string key))
            {
                keyToValue.TryRemove(key, out _);
            }
        }
    }

    public string FindValueByKey(string key)
    {
        if (keyToValue.TryGetValue(key, out ConnectionInfo connectionInfo))
        {
            return connectionInfo.ConnectionId;
        }
        return default(string);
    }

    public string FindKeyByValue(string value)
    {
        valueToKey.TryGetValue(value, out string key);
        return key;
    }

    public List<string> GetAllKeys()
    {
        return keyToValue.Keys.ToList();
    }
    public bool HasKey(string key)
    {
        return keyToValue.ContainsKey(key);
    }
    public DateTime GetConnectionTimestamp(string key)
    {
        if (keyToValue.TryGetValue(key, out ConnectionInfo connectionInfo))
        {
            return connectionInfo.ConnectionTimestamp;
        }
        return default(DateTime);
    }

    private void VerifyBasePaths()
    {
        _baseDirPath = GlobalSettings.NotificationsSaveFolder.BaseDirectory;
        _baseTempDirPath = $"{_baseDirPath}/temp";
        //_baseNotificationsUrl = GlobalSettings.NotificationsSaveFolder.BaseUrl;
    }

    private Task InitAsync()
    {
        try
        {
            VerifyBasePaths();

            if (!Directory.Exists(_baseDirPath))
            {
                Directory.CreateDirectory(_baseDirPath);
            }

            if (!Directory.Exists(_baseTempDirPath))
            {
                Directory.CreateDirectory(_baseTempDirPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal Error: cannot create file/folders needed for notifications .... : {ex.Message}");
            Console.WriteLine($"Fatal Error: Verify both these folders: {_baseDirPath} as well as .... : {_baseTempDirPath}");
            return Task.FromResult(0);
        }

        return Task.FromResult(1);
    }

    //delete all files in a specific folder with the extension .json that are older than 30 minutes
    public async Task DeleteExpiredRequests(string strPrefix, double dMinutesSince)
    {
        if (dMinutesSince > 0) //this really shoud be a negative value ie want to delete requests that are from x minutes in the past
            dMinutesSince = -1;

        await InitAsync();

        DateTime cutoffTime = DateTime.Now.AddMinutes(dMinutesSince);

        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_baseDirPath);
            FileInfo[] files = directoryInfo.GetFiles(strPrefix + "*.json");

            foreach (FileInfo file in files)
            {
                if (file.LastWriteTime < cutoffTime)
                {
                    file.Delete();
                    //Console.WriteLine($"Deleted file: {file.Name}");
                }
            }
        }
        catch (Exception)
        {
            //Console.WriteLine($"An error occurred: {e.Message}");
        }
    }

    public async Task<bool> DumpToFile(string prefix, string strID, string strJson)
    {
        if (this.GlobalSettings == null)
            return false;

        await InitAsync();

        // now can write to file
        //
        //
        // the payload ID is part of the temp file name
        // first dump all contents to a file but use a tempfilename
        // once done writing, save and rename file to correct <payloadID>.json

        var connectionID = FindKeyByValue(strID);
        string l_fileName = prefix + strID;

        TemporaryFileSaver l_tmp = new TemporaryFileSaver(l_fileName, _baseDirPath);
        DateTime lastWriteTimeUtc;
        bool bRetVal= l_tmp.SaveTextToFile(strJson, out lastWriteTimeUtc);

        if (bRetVal && (connectionID != null))
        { 
            ConnectionInfo connVal;
            if (keyToValue.TryGetValue(connectionID, out connVal))
            {
                connVal.FileName = l_fileName;
                connVal.FileLastWriteTimeUTC = lastWriteTimeUtc;

                //AddOrUpdate not needed because we get a reference to the object
                //keyToValue.AddOrUpdate(connectionID, connVal, (key, oldValue) => connVal);
            }
        }

        return bRetVal;
    }

    // ReadFromFile will read if file last write time is older than timestamp (cutoffUTCTime)
    public async Task<string> ReadFromFile(string prefix, string token, DateTime cutoffUTCTime, HubConnectionManagerFileAction fileAction)
    {
        if (this.GlobalSettings == null)
            return null;

        await InitAsync();

        // now can read from files
        // once done reading file, close and delete.
        string l_fileName = prefix + token;
        string finalFilePath = Path.Combine(_baseDirPath, l_fileName + ".json");
        if (File.Exists(finalFilePath)==false) //file does not exist
            return null;

        // Get the last write time of the file
        DateTime lastWriteTime = File.GetLastWriteTime(finalFilePath);
        DateTime lastWriteTimeUtc = lastWriteTime.ToUniversalTime();
        //Console.WriteLine($"Last Write Time: {lastWriteTime}");

        // we must have written to file after cutoff time.  ie the websocket message was sent after our connection was started
        // aka file was created before websocket created then do not read/send
        // therefore the write time of the file should be greater than connection ID creation time
        // if it is less then no need to read it
        if (lastWriteTimeUtc < cutoffUTCTime)
        {
            //Console.WriteLine($"Skipping {token} Last Write Time: {lastWriteTime} is less than websocket cutoff: {cutoffUTCTime} ");
            return null;
        }

        // if I already processed this file, then don't read it again.
        DateTime l_lastProcessedTime = DateTime.MinValue;
        var connectionID = FindKeyByValue(token);
        ConnectionInfo connVal;
        if (keyToValue.TryGetValue(connectionID, out connVal))
        {
            l_lastProcessedTime = connVal.FileLastWriteTimeUTC;
        }

        if (connVal == null)
            return null;

        if (l_lastProcessedTime >= lastWriteTime)
            return null;

        string outJson = null;
        try
        {
            outJson = File.ReadAllText(finalFilePath);

            if (connVal != null)
            {
                connVal.FileName = l_fileName;
                connVal.FileLastWriteTimeUTC = lastWriteTimeUtc;

                //AddOrUpdate not needed because we get a reference to the object
                //keyToValue.AddOrUpdate(connectionID, connVal, (key, oldValue) => connVal);
            }
        }
        catch (FileNotFoundException)// fex)
        {
            // this is not really a fatal error since may want to try reading a file and if it doesn't
            // exist might not necessarily need to treat as an error.
            // Console.WriteLine($"ReadFromFile:: A file not found read error occurred: {fex.Message}");
            outJson = null;
        }
        catch (Exception)
        {
            outJson = null;
        }

        try
        {
            switch (fileAction)
            {
                case HubConnectionManagerFileAction.DeleteFile:
                    File.Delete(finalFilePath);
                    // Add code to delete the file here
                    break;

                case HubConnectionManagerFileAction.RenameFile:
                {
                    // Add code to keep the file here
                    DateTime dateTime = DateTime.Now;
                    //string currentDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string hexString = dateTime.Ticks.ToString("X");
                    //File.Move(finalFilePath, finalFilePath + "_" + hexString + ".done");
                    File.Move(finalFilePath, Path.Combine(_baseTempDirPath, hexString + ".done"));
                }
                    break;

                case HubConnectionManagerFileAction.DoNothing:
                default:
                    break;
            }
        }
        catch (Exception)
        {
        }

        Console.WriteLine($"ReadFromFile: {token} with last wite time: {lastWriteTime}");

        return outJson;
    }

    private class TemporaryFileSaver
    {
        private string tempFilePath;
        private string finalFileName = "myRenamedFile";
        private string finalPath = "/tmp";

        public TemporaryFileSaver(string uniqueIdentifier, string outPath)
        {
            // Generate a temporary file name with a unique identifier
            //string uniqueIdentifier = Guid.NewGuid().ToString();
            //this.tempFilePath = Path.Combine(Path.GetTempPath(), $"tempfile_{uniqueIdentifier}.txt");
            this.tempFilePath = Path.Combine(outPath, $"tempfile_{uniqueIdentifier}.txt");
            finalFileName = uniqueIdentifier + ".json";
            finalPath = outPath;
        }

        public bool SaveTextToFile(string text, out DateTime lastWriteTimeUtc)
        {
            lastWriteTimeUtc = DateTime.UtcNow;

            try
            {
                // Write the text to the temporary file
                File.WriteAllText(tempFilePath, text);

                // Rename the file to the desired name
                string finalFilePath = GetFinalFilePath();

                try
                {
                    if (File.Exists(finalFilePath))
                    {
                        File.Delete(finalFilePath);
                    }
                }
                catch (Exception)
                { }

                File.Move(tempFilePath, finalFilePath);

                // Update the internal path to the renamed file
                this.tempFilePath = finalFilePath;

                DateTime lastWriteTime = File.GetLastWriteTime(tempFilePath);
                lastWriteTimeUtc = lastWriteTime.ToUniversalTime();

            }
            catch (Exception)// ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }

            return true;
        }

        public string GetFinalFilePath()
        {
            return Path.Combine(finalPath, finalFileName);
        }
    }

    public class ConnectionInfo
    {
        public string ConnectionId { get; }
        public DateTime ConnectionTimestamp { get; }

        public string FileName { get; set; }
        public DateTime FileLastWriteTimeUTC { get; set; }

        public ConnectionInfo(string value, DateTime timestamp)
        {
            ConnectionId = value;
            ConnectionTimestamp = timestamp;
        }

        public ConnectionInfo(string value, DateTime timestamp, string fileName)
        {
            ConnectionId = value;
            ConnectionTimestamp = timestamp;
            FileName = fileName;
            FileLastWriteTimeUTC = DateTime.MinValue;
        }
    }

} // HubConnectionManager
