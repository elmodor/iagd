namespace IAGrim.Overwrites.RegisterWindowDataAndType;

public class RegisterWindow {
    public sealed class DataAndType
    {
        public int Type { get; }
        public byte[] Data { get; }
        public string StringData { get; }

        public DataAndType(int type, byte[] data, string stringData)
        {
            Type = type;
            Data = data;
            StringData = stringData;
        }
    }
}
