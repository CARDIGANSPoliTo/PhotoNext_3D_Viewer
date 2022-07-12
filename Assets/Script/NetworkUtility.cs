using System;

[Serializable]
public struct MongoInformation {
    public string IPAddress;
    public int    Port;
    public string DBName;
    public string CollectionName;
}

[Serializable]
public struct TCPInformation {
    public string IPAddress;
    public int    Port;
}
